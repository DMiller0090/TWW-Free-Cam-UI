using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TWW_Free_Cam_UI.Dolphin.Camera
{
    public interface ICameraEditable
    {
        public uint ReadCurrentFrame();
        public float ReadCameraEyeX();
        public float ReadCameraEyeY();
        public float ReadCameraEyeZ();
        public void WriteCameraEyeX(float x);
        public void WriteCameraEyeY(float y);
        public void WriteCameraEyeZ(float z);
        public void WriteCameraEye(float x, float y, float z);
        public float ReadCameraCenterX();
        public float ReadCameraCenterY();
        public float ReadCameraCenterZ();
        public void WriteCameraCenterX(float x);
        public void WriteCameraCenterY(float y);
        public void WriteCameraCenterZ(float z);
        public void WriteCameraCenter(float x, float y, float z);
        public void WriteAutofocus(bool disable);
        public ushort ReadCsAngle();
        public void WriteCsAngle(ushort angle);
        public void DisableUI();
        public void EnableUI();
    }
}
