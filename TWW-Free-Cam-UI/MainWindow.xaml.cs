using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TWW_Free_Cam_UI.Dolphin;
using Microsoft.Win32;
using System.Text.Json;
using System.IO;
using TWW_Free_Cam_UI.Dolphin.Camera;
using Quaternion = System.Numerics.Quaternion;
using TWW_Free_Cam_UI.Dolphin.Player;
namespace TWW_Free_Cam_UI;


/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    // Timer for refreshing the UI
    private DispatcherTimer refreshTimer;
    //private static IPlayerEditable player;
    private static ICameraEditable camera;
    private static uint startPlaybackFrame = 0;
    private static uint prevPlaybackFrame = 0;
    // Collection that backs the DataGrid rows
    public ObservableCollection<CameraScriptRow> ScriptRows { get; set; }
    // Collection to store recorded memory entries.
    // You can define your own class RecordedMemoryEntry as needed.
    public Dictionary<uint, RecordedMemoryEntry> RecordedData { get; set; } = new Dictionary<uint, RecordedMemoryEntry>();

    // Recording state.
    private bool isRecording = false;
    private uint recordStartFrame = 0;
    private uint recordEndFrame = 0;
    // Class to represent a recorded memory entry.
    public class RecordedMemoryEntry
    {
        public ushort csAngle { get; set; }
        //public float x { get; set; }
        //public float y { get; set; }
        //public float z { get; set; }
        //public ushort rotation { get; set; }
        public void Write()
        {
            camera.WriteCsAngle(csAngle);
            //player.WriteLocation(x, y, z);
            //player.WriteRotation(rotation);
        }
    }
    public MainWindow()
    {
        InitializeComponent();

        // Initialize the collection and bind it to the DataGrid
        ScriptRows = new ObservableCollection<CameraScriptRow>();
        dgScript.ItemsSource = ScriptRows;

        // Setup the refresh timer using the value from txtRefreshRate
        refreshTimer = new DispatcherTimer();
        refreshTimer.Interval = TimeSpan.FromMilliseconds(GetRefreshRate());
        refreshTimer.Tick += RefreshTimer_Tick;
    }

    // Connect button event handler
    private void btnConnect_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Attach to the Dolphin emulator process.
            if((string)btnConnect.Content == "Connect")
            {
                Process[] processes = Process.GetProcessesByName("Dolphin");
                if (processes.Length == 0)
                {
                    throw new Exception("No dolphin process running");
                }
                if (Memory.Attach(processes[0]))
                {
                    Memory.ReadMemory((ulong)0x80000000, 8);
                    string gameVersion = Memory.ReadMemoryString((ulong)0x80000000);
                    switch (gameVersion)
                    {
                        case "GZLJ01":
                            //player = new TWW_JP_Player();
                            camera = new TWW_JP_Editor();
                            break;
                        case "GZ2E01":
                            camera = new TP_Eng_Editor();
                            break;
                        case "GZ2P01":
                            camera = new TP_PAL_Editor();
                            break;
                        case "GZ2J01":
                            camera = new TP_JP_Editor();
                            break;
                        default:
                            MessageBox.Show($"Game: {gameVersion} is unsupported!");
                            return;
                    }
                    btnConnect.Content = "Disconnect";
                    refreshTimer.Start();
                }
            }
            else
            {
                Memory.m_hDolphin = IntPtr.Zero;
                refreshTimer.Stop();
                btnConnect.Content = "Connect";
            }

        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to connect: " + ex.Message);
        }
    }
    private bool PlaybackHandler()
    {
        uint currentFrame = camera.ReadCurrentFrame();
        txtCurrentFrame.Text = currentFrame.ToString();

        uint diff = currentFrame - prevPlaybackFrame;
        if (diff > 1)
        {
            Debug.WriteLine(diff);
        }
        if (RecordedData.ContainsKey(currentFrame))
        {
            RecordedData[currentFrame].Write();
        }
        // Calculate how many frames have elapsed since we started playback.
        uint elapsed;
        if (uint.TryParse(txtStartFrame.Text, out uint startFrame))
        {
            if (currentFrame < startFrame)
                return true;
            elapsed = currentFrame - startFrame;
        }
        else
        {
            elapsed = currentFrame - startPlaybackFrame;
        }
        // Write memory using the captured value.
        camera.WriteAutofocus(true);
        // Find which script row we are currently in.
        CameraScriptRow currentRow = null;
        int currentTime = 0;
        foreach (CameraScriptRow row in ScriptRows)
        {
            if (elapsed < row.Frames + currentTime)
            {
                currentRow = row;
                break;
            }
            else
            {
                currentTime += row.Frames;
            }
        }

        // If no row found, we must be beyond the last row. 
        // Force the final positions of the last row.
        if (currentRow == null)
        {
            if (ScriptRows.Count > 0)
            {
                CameraScriptRow lastRow = ScriptRows[ScriptRows.Count - 1];
                camera.WriteCameraCenter(lastRow.EndFocusX, lastRow.EndFocusY, lastRow.EndFocusZ);
                camera.WriteCameraEye(lastRow.EndCamX, lastRow.EndCamY, lastRow.EndCamZ);
            }
            return false;
        }

        int currentElapsed = (int)elapsed - currentTime;
        float u = (currentRow.Frames > 0) ? currentElapsed / (float)currentRow.Frames : 0f;

        // If we've hit or exceeded the end of the current row, force final row positions.
        // This ensures we don't skip the last frame's final location.
        if (u >= 1f)
        {
            camera.WriteCameraCenter(currentRow.EndFocusX, currentRow.EndFocusY, currentRow.EndFocusZ);
            camera.WriteCameraEye(currentRow.EndCamX, currentRow.EndCamY, currentRow.EndCamZ);
            return true;
        }

        // --- Focus Interpolation ---
        cXyz focusStart = new cXyz { x = currentRow.StartFocusX, y = currentRow.StartFocusY, z = currentRow.StartFocusZ };
        cXyz focusEnd = new cXyz { x = currentRow.EndFocusX, y = currentRow.EndFocusY, z = currentRow.EndFocusZ };

        cXyz newFocus = Interpolator.Interpolate(
            focusStart,
            focusEnd,
            currentElapsed,
            currentRow.Frames,
            GetEasingFunction(currentRow.InterpolationType, currentRow)
        );

        // --- Camera (Eye) Interpolation (example horizontal orbit) ---
        cXyz eyeStart = new cXyz { x = currentRow.StartCamX, y = currentRow.StartCamY, z = currentRow.StartCamZ };
        cXyz eyeEnd = new cXyz { x = currentRow.EndCamX, y = currentRow.EndCamY, z = currentRow.EndCamZ };

        Vector3 offsetStart = new Vector3(
            eyeStart.x - currentRow.StartFocusX,
            eyeStart.y - currentRow.StartFocusY,
            eyeStart.z - currentRow.StartFocusZ);
        Vector3 offsetEnd = new Vector3(
            eyeEnd.x - currentRow.EndFocusX,
            eyeEnd.y - currentRow.EndFocusY,
            eyeEnd.z - currentRow.EndFocusZ);

        Vector3 hOffsetStart = new Vector3(offsetStart.X, 0, offsetStart.Z);
        Vector3 hOffsetEnd = new Vector3(offsetEnd.X, 0, offsetEnd.Z);
        float newY = Lerp(offsetStart.Y, offsetEnd.Y, u);

        float radiusStart = hOffsetStart.Length();
        float radiusEnd = hOffsetEnd.Length();
        float radius = Lerp(radiusStart, radiusEnd, u);

        float totalAngle = 0f;
        if (hOffsetStart.Length() > 0 && hOffsetEnd.Length() > 0)
        {
            float dot = Vector3.Dot(Vector3.Normalize(hOffsetStart), Vector3.Normalize(hOffsetEnd));
            dot = Math.Clamp(dot, -1f, 1f);
            totalAngle = (float)Math.Acos(dot);
            Vector3 cross = Vector3.Cross(hOffsetStart, hOffsetEnd);
            if (cross.Y < 0)
                totalAngle = -totalAngle;
        }
        float currentAngle = totalAngle * u;
        Vector3 rotatedHOffset = RotateVector(hOffsetStart, currentAngle, new Vector3(0, 1, 0));
        if (rotatedHOffset.Length() > 0)
            rotatedHOffset = Vector3.Normalize(rotatedHOffset) * radius;

        Vector3 newOffset = new Vector3(rotatedHOffset.X, newY, rotatedHOffset.Z);
        Vector3 newEyeVec = new Vector3(newFocus.x, newFocus.y, newFocus.z) + newOffset;
        cXyz newEye = new cXyz { x = newEyeVec.X, y = newEyeVec.Y, z = newEyeVec.Z };

        camera.WriteCameraEye(newEye.x, newEye.y, newEye.z);
        camera.WriteCameraCenter(newFocus.x, newFocus.y, newFocus.z);

        return true;
    }

    // Linear interpolation helper.
    private float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    // Helper to select the easing function based on the interpolation type.
    // For "Cubic Bezier", uses the Bezier parameters stored in the row.
    private Interpolator.EasingFunction GetEasingFunction(string interpolationType, CameraScriptRow row)
    {
        switch (interpolationType)
        {
            case "Linear":
                return Interpolator.Linear;
            case "Ease In":
                return Interpolator.EaseIn;
            case "Ease Out":
                return Interpolator.EaseOut;
            case "Ease In/Out":
                return Interpolator.EaseInOut;
            case "Cubic Bezier":
                return Interpolator.CubicBezier(row.BezierParam1, row.BezierParam2);
            default:
                return Interpolator.Linear;
        }
    }
    private void RecordHandler()
    {
        uint currentFrame = camera.ReadCurrentFrame();
        if (!RecordedData.ContainsKey(currentFrame))
        {
            //(float x, float y, float z) = player.GetLocation();
            RecordedData[currentFrame] = new RecordedMemoryEntry
            {
                csAngle = camera.ReadCsAngle(),
                //x = x,
                //y = y,
                //z = z,
                //rotation = player.GetRotation()
            };
        }
    }
    // Timer tick event handler: refresh UI values based on game state
    private void RefreshTimer_Tick(object sender, EventArgs e)
    {
        // Capture UI state safely on the UI thread.
        bool disableAutofocus = false;
        bool disableUI = false;
        bool isPlayback = false;

        Dispatcher.Invoke(() =>
        {
            disableAutofocus = chkDisableAutofocus?.IsChecked == true;
            disableUI = chkDisableUI?.IsChecked == true;
            isPlayback = startPlaybackFrame > 0;
        });



        // Update UI elements or perform actions based on the captured state.
        if (disableUI)
            camera.DisableUI();
        else
            camera.EnableUI();

        if (isRecording)
        {
            RecordHandler();
        }
        else if (isPlayback)
        {
            bool running = PlaybackHandler();
            if (!running)
            {
                // Update the play button and playback state on the UI thread.
                Dispatcher.Invoke(() =>
                {
                    btnPlay.Content = "Play";
                    startPlaybackFrame = 0;
                });
            }
        }
        else
        {
            // Update camera coordinate TextBoxes if they are not focused.
            Dispatcher.Invoke(() =>
            {
                camera.WriteAutofocus(disableAutofocus);
                uint currentFrame = camera.ReadCurrentFrame();
                txtCurrentFrame.Text = currentFrame.ToString();
                if (!txtCameraPosX.IsFocused)
                    txtCameraPosX.Text = camera.ReadCameraEyeX().ToString();
                if (!txtCameraPosY.IsFocused)
                    txtCameraPosY.Text = camera.ReadCameraEyeY().ToString();
                if (!txtCameraPosZ.IsFocused)
                    txtCameraPosZ.Text = camera.ReadCameraEyeZ().ToString();
                if (!txtCameraTargetX.IsFocused)
                    txtCameraTargetX.Text = camera.ReadCameraCenterX().ToString();
                if (!txtCameraTargetY.IsFocused)
                    txtCameraTargetY.Text = camera.ReadCameraCenterY().ToString();
                if (!txtCameraTargetZ.IsFocused)
                    txtCameraTargetZ.Text = camera.ReadCameraCenterZ().ToString();
            });
        }
    }


    // Adds a new scripting row; if a previous row exists, prepopulate start coordinates from its end values.
    private void btnAddRow_Click(object sender, RoutedEventArgs e)
    {
        CameraScriptRow newRow = new CameraScriptRow();

        if (ScriptRows.Count > 0)
        {
            // Use the last row's end positions as the new row's start positions
            CameraScriptRow lastRow = ScriptRows[ScriptRows.Count - 1];
            newRow.StartCamX = lastRow.EndCamX;
            newRow.StartCamY = lastRow.EndCamY;
            newRow.StartCamZ = lastRow.EndCamZ;

            newRow.StartFocusX = lastRow.EndFocusX;
            newRow.StartFocusY = lastRow.EndFocusY;
            newRow.StartFocusZ = lastRow.EndFocusZ;
        }
        ScriptRows.Add(newRow);
    }
    private void btnRemoveRow_Click(object sender, RoutedEventArgs e)
    {
        MenuItem menuItem = sender as MenuItem;
        if (ScriptRows.Count > 0)
        {
            ScriptRows.Remove(ScriptRows.Last());
        }
    }
    // Play button event handler: toggles playback on and off.
    private void btnPlay_Click(object sender, RoutedEventArgs e)
    {
        if ((string)btnPlay.Content == "Play")
        {
            btnPlay.Content = "Stop";
            startPlaybackFrame = camera.ReadCurrentFrame();
        }
        else
        {
            btnPlay.Content = "Play";
            startPlaybackFrame = 0;
        }
    }

    private void SetCameraToCoordinates_Click(object sender, RoutedEventArgs e)
    {
        // and update the bound value with the current coordinate from the UI (or directly read from game memory).
        // 'sender' is the MenuItem
        MenuItem menuItem = sender as MenuItem;
        if (menuItem == null) return;

        // Get the ContextMenu that contains this MenuItem.
        ContextMenu contextMenu = menuItem.Parent as ContextMenu;
        if (contextMenu == null) return;

        // The PlacementTarget is the control (the TextBox) that was right-clicked.
        FrameworkElement placementTarget = contextMenu.PlacementTarget as FrameworkElement;
        if (placementTarget == null) return;

        // The Tag property tells us which property this TextBox is bound to.
        string propertyName = placementTarget.Tag as string;
        if (string.IsNullOrEmpty(propertyName))
            return;

        // The DataContext of the TextBox should be the row's data object (e.g., a CameraScriptRow).
        CameraScriptRow rowData = placementTarget.DataContext as CameraScriptRow;
        if (rowData == null)
            return;

        // Here we check if the property name indicates a camera position or focus coordinate.
        try
        {
            if (propertyName.StartsWith("Start"))
            {
                camera.WriteCameraEye(rowData.StartCamX, rowData.StartCamY, rowData.StartCamZ);
                camera.WriteCameraCenter(rowData.StartFocusX, rowData.StartFocusY, rowData.StartFocusZ);
            }
            else if (propertyName.StartsWith("End"))
            {
                camera.WriteCameraEye(rowData.EndCamX, rowData.EndCamY, rowData.EndCamZ);
                camera.WriteCameraCenter(rowData.EndFocusX, rowData.EndFocusY, rowData.EndFocusZ);
            }
        }
        catch (FormatException)
        {
            MessageBox.Show("Invalid coordinate value in the main camera fields.");
        }
    }
    // Context menu event: set coordinates to the current game values.
    private void SetToCurrentCoordinates_Click(object sender, RoutedEventArgs e)
    {
        // and update the bound value with the current coordinate from the UI (or directly read from game memory).
        // 'sender' is the MenuItem
        MenuItem menuItem = sender as MenuItem;
        if (menuItem == null) return;

        // Get the ContextMenu that contains this MenuItem.
        ContextMenu contextMenu = menuItem.Parent as ContextMenu;
        if (contextMenu == null) return;

        // The PlacementTarget is the control (the TextBox) that was right-clicked.
        FrameworkElement placementTarget = contextMenu.PlacementTarget as FrameworkElement;
        if (placementTarget == null) return;

        // The Tag property tells us which property this TextBox is bound to.
        string propertyName = placementTarget.Tag as string;
        if (string.IsNullOrEmpty(propertyName))
            return;

        // The DataContext of the TextBox should be the row's data object (e.g., a CameraScriptRow).
        CameraScriptRow rowData = placementTarget.DataContext as CameraScriptRow;
        if (rowData == null)
            return;

        // Here we check if the property name indicates a camera position or focus coordinate.
        try
        {
            if (propertyName.StartsWith("Start"))
            {
                rowData.StartCamX = float.Parse(txtCameraPosX.Text);
                rowData.StartCamY = float.Parse(txtCameraPosY.Text);
                rowData.StartCamZ = float.Parse(txtCameraPosZ.Text);
                rowData.StartFocusX = float.Parse(txtCameraTargetX.Text);
                rowData.StartFocusY = float.Parse(txtCameraTargetY.Text);
                rowData.StartFocusZ = float.Parse(txtCameraTargetZ.Text);
            }
            else if (propertyName.StartsWith("End"))
            {
                rowData.EndCamX = float.Parse(txtCameraPosX.Text);
                rowData.EndCamY = float.Parse(txtCameraPosY.Text);
                rowData.EndCamZ = float.Parse(txtCameraPosZ.Text);
                rowData.EndFocusX = float.Parse(txtCameraTargetX.Text);
                rowData.EndFocusY = float.Parse(txtCameraTargetY.Text);
                rowData.EndFocusZ = float.Parse(txtCameraTargetZ.Text);
                int rowIndex = ScriptRows.IndexOf(rowData);
                if (rowIndex != ScriptRows.Count() - 1)
                {
                    ScriptRows[rowIndex + 1].StartCamX = rowData.EndCamX;
                    ScriptRows[rowIndex + 1].StartCamY = rowData.EndCamY;
                    ScriptRows[rowIndex + 1].StartCamZ = rowData.EndCamZ;
                    ScriptRows[rowIndex + 1].StartFocusX = rowData.EndFocusX;
                    ScriptRows[rowIndex + 1].StartFocusY = rowData.EndFocusY;
                    ScriptRows[rowIndex + 1].StartFocusZ = rowData.EndFocusZ;
                }
            }

            //if (propertyName.StartsWith("StartCam"))
            //{
            //    rowData.StartCamX = float.Parse(txtCameraPosX.Text);
            //    rowData.StartCamY = float.Parse(txtCameraPosY.Text);
            //    rowData.StartCamZ = float.Parse(txtCameraPosZ.Text);
            //}
            //else if (propertyName.StartsWith("StartFocus"))
            //{
            //    rowData.StartFocusX = float.Parse(txtCameraTargetX.Text);
            //    rowData.StartFocusY = float.Parse(txtCameraTargetY.Text);
            //    rowData.StartFocusZ = float.Parse(txtCameraTargetZ.Text);
            //}
            //else if (propertyName.StartsWith("EndCam"))
            //{
            //    rowData.EndCamX = float.Parse(txtCameraPosX.Text);
            //    rowData.EndCamY = float.Parse(txtCameraPosY.Text);
            //    rowData.EndCamZ = float.Parse(txtCameraPosZ.Text);
            //}
            //else if (propertyName.StartsWith("EndFocus"))
            //{
            //    rowData.EndFocusX = float.Parse(txtCameraTargetX.Text);
            //    rowData.EndFocusY = float.Parse(txtCameraTargetY.Text);
            //    rowData.EndFocusZ = float.Parse(txtCameraTargetZ.Text);
            //}
        }
        catch (FormatException)
        {
            MessageBox.Show("Invalid coordinate value in the main camera fields.");
        }
    }

    // Helper to obtain the refresh rate from the UI
    private int GetRefreshRate()
    {
        if (int.TryParse(txtRefreshRate.Text, out int rate))
        {
            return rate;
        }
        return 100; // Default to 100 ms if parsing fails
    }
    private void txtCameraPosX_LostFocus(object sender, RoutedEventArgs e)
    {
        if (float.TryParse(txtCameraPosX.Text, out float newValue))
        {
            // Optionally check if the value really changed to avoid unnecessary writes.
            float currentValue = camera.ReadCameraEyeX();
            if (currentValue != newValue)
            {
                camera.WriteCameraEyeX(newValue);
            }
        }
    }
    private void txtCameraPosY_LostFocus(object sender, RoutedEventArgs e)
    {
        if (float.TryParse(txtCameraPosY.Text, out float newValue))
        {
            // Optionally check if the value really changed to avoid unnecessary writes.
            float currentValue = camera.ReadCameraEyeY();
            if (currentValue != newValue)
            {
                camera.WriteCameraEyeY(newValue);
            }
        }
    }
    private void txtCameraPosZ_LostFocus(object sender, RoutedEventArgs e)
    {
        if (float.TryParse(txtCameraPosZ.Text, out float newValue))
        {
            // Optionally check if the value really changed to avoid unnecessary writes.
            float currentValue = camera.ReadCameraEyeZ();
            if (currentValue != newValue)
            {
                camera.WriteCameraEyeZ(newValue);
            }
        }
    }
    private void txtCameraTargetX_LostFocus(object sender, RoutedEventArgs e)
    {
        if (float.TryParse(txtCameraTargetX.Text, out float newValue))
        {
            // Optionally check if the value really changed to avoid unnecessary writes.
            float currentValue = camera.ReadCameraCenterX();
            if (currentValue != newValue)
            {
                camera.WriteCameraCenterX(newValue);
            }
        }
    }
    private void txtCameraTargetY_LostFocus(object sender, RoutedEventArgs e)
    {
        if (float.TryParse(txtCameraTargetY.Text, out float newValue))
        {
            // Optionally check if the value really changed to avoid unnecessary writes.
            float currentValue = camera.ReadCameraCenterY();
            if (currentValue != newValue)
            {
                camera.WriteCameraCenterY(newValue);
            }
        }
    }
    private void txtCameraTargetZ_LostFocus(object sender, RoutedEventArgs e)
    {
        if (float.TryParse(txtCameraTargetZ.Text, out float newValue))
        {
            // Optionally check if the value really changed to avoid unnecessary writes.
            float currentValue = camera.ReadCameraCenterZ();
            if (currentValue != newValue)
            {
                camera.WriteCameraCenterZ(newValue);
            }
        }
    }

    private void txtRefreshRate_LostFocus(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(txtRefreshRate.Text, out int newRate))
        {
            // Update the timer's interval directly.
            refreshTimer.Interval = TimeSpan.FromMilliseconds(newRate);
        }
    }
    private void TextBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        TextBox tb = sender as TextBox;
        if (tb != null)
        {
            // Force the ContextMenu's placement target to be the TextBox.
            if (tb.ContextMenu != null)
            {
                tb.ContextMenu.PlacementTarget = tb;
                tb.ContextMenu.IsOpen = true;
            }
            e.Handled = true; // Prevent default menu
        }
    }
    private void DataGridCell_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var cell = sender as DataGridCell;
        if (cell != null && !cell.IsEditing && !cell.IsReadOnly)
        {
            // Move focus to the cell if it's not already focused
            if (!cell.IsFocused)
            {
                cell.Focus();
            }
            // Get the DataGrid parent and begin edit
            var dataGrid = FindVisualParent<DataGrid>(cell);
            if (dataGrid != null)
            {
                dataGrid.BeginEdit(e);
            }
        }
    }

    private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        while (child != null)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            if (parent is T correctlyTyped)
            {
                return correctlyTyped;
            }
            child = parent;
        }
        return null;
    }

    // Directional controls event handlers

    private void btnUp_Click(object sender, RoutedEventArgs e)
    {
        NudgeCamera(new Vector3(0, 1, 0));
    }

    private void btnDown_Click(object sender, RoutedEventArgs e)
    {
        NudgeCamera(new Vector3(0, -1, 0));
    }

    private void btnLeft_Click(object sender, RoutedEventArgs e)
    {
        NudgeCamera(new Vector3(-1, 0, 0));
    }

    private void btnRight_Click(object sender, RoutedEventArgs e)
    {
        NudgeCamera(new Vector3(1, 0, 0));
    }

    private void btnForward_Click(object sender, RoutedEventArgs e)
    {
        NudgeCamera(new Vector3(0, 0, 1));
    }

    private void btnBackward_Click(object sender, RoutedEventArgs e)
    {
        NudgeCamera(new Vector3(0, 0, -1));
    }

    private void txtNudgeSpeed_LostFocus(object sender, RoutedEventArgs e)
    {
        // TODO: Validate and update the nudge speed value.
    }

    private void NudgeCamera(Vector3 directionIndicator)
    {
        // Get the nudge amount from the UI (arc distance for rotations or linear distance for forward/backward).
        if (!float.TryParse(txtNudgeSpeed.Text, out float delta))
        {
            MessageBox.Show("Invalid nudge speed value.");
            return;
        }

        // Read current camera position and focus from the UI.
        Vector3 camPos = new Vector3(
             float.Parse(txtCameraPosX.Text),
             float.Parse(txtCameraPosY.Text),
             float.Parse(txtCameraPosZ.Text));

        Vector3 focusPos = new Vector3(
             float.Parse(txtCameraTargetX.Text),
             float.Parse(txtCameraTargetY.Text),
             float.Parse(txtCameraTargetZ.Text));

        // Compute the forward vector (from camera to focus).
        Vector3 forward = Vector3.Normalize(focusPos - camPos);

        // Define world up.
        Vector3 worldUp = new Vector3(0, 1, 0);

        // Compute the right vector.
        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, worldUp));

        // For an upright camera, assume up is worldUp.
        Vector3 up = worldUp;

        // For left/right/up/down, we rotate the camera's offset (camPos - focusPos).
        // For forward/backward, we translate the camera along the forward vector.
        if (directionIndicator == new Vector3(1, 0, 0))
        {
            // Move right: rotate around the worldUp axis.
            Vector3 offset = camPos - focusPos;
            float radius = offset.Length();
            if (radius == 0) return;
            float angle = delta / radius;  // arc length = radius * angle => angle = delta / radius
            Vector3 rotatedOffset = RotateVector(offset, angle, worldUp);
            WriteCameraPosition(focusPos + rotatedOffset);
        }
        else if (directionIndicator == new Vector3(-1, 0, 0))
        {
            // Move left: rotate around the worldUp axis (negative angle).
            Vector3 offset = camPos - focusPos;
            float radius = offset.Length();
            if (radius == 0) return;
            float angle = -delta / radius;
            Vector3 rotatedOffset = RotateVector(offset, angle, worldUp);
            WriteCameraPosition(focusPos + rotatedOffset);
        }
        else if (directionIndicator == new Vector3(0, -1, 0))
        {
            // Move up: rotate around the right axis.
            Vector3 offset = camPos - focusPos;
            float radius = offset.Length();
            if (radius == 0) return;
            float angle = delta / radius;
            Vector3 rotatedOffset = RotateVector(offset, angle, right);
            WriteCameraPosition(focusPos + rotatedOffset);
        }
        else if (directionIndicator == new Vector3(0, 1, 0))
        {
            // Move down: rotate around the right axis (negative angle).
            Vector3 offset = camPos - focusPos;
            float radius = offset.Length();
            if (radius == 0) return;
            float angle = -delta / radius;
            Vector3 rotatedOffset = RotateVector(offset, angle, right);
            WriteCameraPosition(focusPos + rotatedOffset);
        }
        else if (directionIndicator == new Vector3(0, 0, 1))
        {
            // Forward: translate camera along the forward vector.
            WriteCameraPosition(camPos + forward * delta);
        }
        else if (directionIndicator == new Vector3(0, 0, -1))
        {
            // Backward: translate camera opposite to the forward vector.
            WriteCameraPosition(camPos - forward * delta);
        }
    }

    // Helper method to rotate a vector by a given angle (in radians) around a specified axis.
    private Vector3 RotateVector(Vector3 vector, float angleRadians, Vector3 axis)
    {
        axis = Vector3.Normalize(axis);
        Quaternion rotation = Quaternion.CreateFromAxisAngle(axis, angleRadians);
        return Vector3.Transform(vector, rotation);
    }

    // Helper method to write the new camera position to memory.
    private void WriteCameraPosition(Vector3 newCamPos)
    {
        camera.WriteCameraEye(newCamPos.X, newCamPos.Y, newCamPos.Z);
    }

    // Save the ScriptRows to a user-selected file.
    private void SaveScriptRowsToFile()
    {
        // Create and configure a SaveFileDialog.
        SaveFileDialog dlg = new SaveFileDialog
        {
            FileName = "ScriptRows",          // Default file name
            DefaultExt = ".json",             // Default file extension
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*"  // Filter files by extension
        };

        // Show the dialog.
        bool? result = dlg.ShowDialog();
        if (result == true)
        {
            string filename = dlg.FileName;

            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(ScriptRows, options);
                File.WriteAllText(filename, json);
                MessageBox.Show("Script rows saved successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving script rows: " + ex.Message);
            }
        }
    }

    // Load the ScriptRows from a user-selected file.
    private void LoadScriptRowsFromFile()
    {
        // Create and configure an OpenFileDialog.
        OpenFileDialog dlg = new OpenFileDialog
        {
            DefaultExt = ".json",
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*"
        };

        // Show the dialog.
        bool? result = dlg.ShowDialog();
        if (result == true)
        {
            string filename = dlg.FileName;

            try
            {
                string json = File.ReadAllText(filename);
                var loadedRows = JsonSerializer.Deserialize<ObservableCollection<CameraScriptRow>>(json);
                if (loadedRows != null)
                {
                    ScriptRows.Clear();
                    foreach (var row in loadedRows)
                    {
                        ScriptRows.Add(row);
                    }
                    MessageBox.Show("Script rows loaded successfully.");
                }
                else
                {
                    MessageBox.Show("No data found in file.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading script rows: " + ex.Message);
            }
        }
    }
    private void btnRecordStart_Click(object sender, RoutedEventArgs e)
    {
        // If already recording, clear the current data.
        if (isRecording)
        {
            RecordedData.Clear();
        }
        else
        {
            // Start new recording: clear old data.
            RecordedData.Clear();
            isRecording = true;
            // Capture the starting frame.
            recordStartFrame = camera.ReadCurrentFrame();
            txtRecordStartFrame.Text = recordStartFrame.ToString();
            txtRecordEndFrame.Text = ""; // Clear end frame.
        }
    }

    private void btnRecordStop_Click(object sender, RoutedEventArgs e)
    {
        if (isRecording)
        {
            // Capture the end frame.
            recordEndFrame = camera.ReadCurrentFrame();
            txtRecordEndFrame.Text = recordEndFrame.ToString();
            isRecording = false;
            // TODO: Insert additional logic to finalize the recording.
        }
    }

    private void btnClearRecord_Click(object sender, RoutedEventArgs e)
    {
        // Clear any recorded data.
        RecordedData.Clear();
        txtRecordStartFrame.Text = "";
        txtRecordEndFrame.Text = "";
        // TODO: Additional logic if needed.
    }
    // Event handlers that you can wire to Save/Load buttons in the UI.
    private void btnSave_Click(object sender, RoutedEventArgs e)
    {
        SaveScriptRowsToFile();
    }

    private void btnLoad_Click(object sender, RoutedEventArgs e)
    {
        LoadScriptRowsFromFile();
    }
}
