namespace TWW_Free_Cam_UI
{
    using System.ComponentModel;

    public class CameraScriptRow : INotifyPropertyChanged
    {
        // Flag to prevent recursive updates
        private bool _updatingFramesOrSpeed;
        // Start Camera Position
        private float _startCamX;
        public float StartCamX
        {
            get => _startCamX;
            set
            {
                if (_startCamX != value)
                {
                    _startCamX = value;
                    OnPropertyChanged(nameof(StartCamX));
                    RecalculateSpeed();
                }
            }
        }
        private float _startCamY;
        public float StartCamY
        {
            get => _startCamY;
            set
            {
                if (_startCamY != value)
                {
                    _startCamY = value;
                    OnPropertyChanged(nameof(StartCamY));
                    RecalculateSpeed();
                }
            }
        }
        private float _startCamZ;
        public float StartCamZ
        {
            get => _startCamZ;
            set
            {
                if (_startCamZ != value)
                {
                    _startCamZ = value;
                    OnPropertyChanged(nameof(StartCamZ));
                    RecalculateSpeed();
                }
            }
        }
        // Start Focus Position
        private float _startFocusX;
        public float StartFocusX
        {
            get => _startFocusX;
            set
            {
                if (_startFocusX != value)
                {
                    _startFocusX = value;
                    OnPropertyChanged(nameof(StartFocusX));
                }
            }
        }

        private float _startFocusY;
        public float StartFocusY
        {
            get => _startFocusY;
            set
            {
                if (_startFocusY != value)
                {
                    _startFocusY = value;
                    OnPropertyChanged(nameof(StartFocusY));
                }
            }
        }

        private float _startFocusZ;
        public float StartFocusZ
        {
            get => _startFocusZ;
            set
            {
                if (_startFocusZ != value)
                {
                    _startFocusZ = value;
                    OnPropertyChanged(nameof(StartFocusZ));
                }
            }
        }

        // End Camera Position
        private float _endCamX;
        public float EndCamX
        {
            get => _endCamX;
            set
            {
                if (_endCamX != value)
                {
                    _endCamX = value;
                    OnPropertyChanged(nameof(EndCamX));
                    RecalculateSpeed();
                }
            }
        }
        private float _endCamY;
        public float EndCamY
        {
            get => _endCamY;
            set
            {
                if (_endCamY != value)
                {
                    _endCamY = value;
                    OnPropertyChanged(nameof(EndCamY));
                    RecalculateSpeed();
                }
            }
        }
        private float _endCamZ;
        public float EndCamZ
        {
            get => _endCamZ;
            set
            {
                if (_endCamZ != value)
                {
                    _endCamZ = value;
                    OnPropertyChanged(nameof(EndCamZ));
                    RecalculateSpeed();
                }
            }
        }

        // End Focus Position
        private float _endFocusX;
        public float EndFocusX
        {
            get => _endFocusX;
            set
            {
                if (_endFocusX != value)
                {
                    _endFocusX = value;
                    OnPropertyChanged(nameof(EndFocusX));
                }
            }
        }

        private float _endFocusY;
        public float EndFocusY
        {
            get => _endFocusY;
            set
            {
                if (_endFocusY != value)
                {
                    _endFocusY = value;
                    OnPropertyChanged(nameof(EndFocusY));
                }
            }
        }

        private float _endFocusZ;
        public float EndFocusZ
        {
            get => _endFocusZ;
            set
            {
                if (_endFocusZ != value)
                {
                    _endFocusZ = value;
                    OnPropertyChanged(nameof(EndFocusZ));
                }
            }
        }

        // Frames (editable)
        private int _frames;
        public int Frames
        {
            get => _frames;
            set
            {
                if (_frames != value)
                {
                    _frames = value;
                    OnPropertyChanged(nameof(Frames));
                    if (!_updatingFramesOrSpeed)
                    {
                        _updatingFramesOrSpeed = true;
                        double distance = ComputeDistance();
                        // Update speed based on frames if frames > 0.
                        Speed = (_frames > 0) ? distance / _frames : 0;
                        _updatingFramesOrSpeed = false;
                    }
                }
            }
        }

        // Speed (editable)
        private double _speed;
        public double Speed
        {
            get => _speed;
            set
            {
                if (Math.Abs(_speed - value) > 0.0001)
                {
                    _speed = value;
                    OnPropertyChanged(nameof(Speed));
                    if (!_updatingFramesOrSpeed)
                    {
                        _updatingFramesOrSpeed = true;
                        double distance = ComputeDistance();
                        // If speed > 0, update frames; otherwise set to 0.
                        Frames = (_speed > 0) ? (int)Math.Round(distance / _speed) : 0;
                        _updatingFramesOrSpeed = false;
                    }
                }
            }
        }

        // Interpolation type: "Linear", "Ease In/Out", or "Cubic Bezier"
        private string _interpolationType = "Linear";
        public string InterpolationType
        {
            get => _interpolationType;
            set
            {
                if (_interpolationType != value)
                {
                    _interpolationType = value;
                    OnPropertyChanged(nameof(InterpolationType));
                }
            }
        }

        // Cubic Bezier parameters (if applicable)
        private float _bezierParam1;
        public float BezierParam1
        {
            get => _bezierParam1;
            set
            {
                if (_bezierParam1 != value)
                {
                    _bezierParam1 = value;
                    OnPropertyChanged(nameof(BezierParam1));
                }
            }
        }

        private float _bezierParam2;
        public float BezierParam2
        {
            get => _bezierParam2;
            set
            {
                if (_bezierParam2 != value)
                {
                    _bezierParam2 = value;
                    OnPropertyChanged(nameof(BezierParam2));
                }
            }
        }

        // Optional actor selection: if set, focus values are treated as offsets from the actor's coordinates
        private string _selectedActor;
        public string SelectedActor
        {
            get => _selectedActor;
            set
            {
                if (_selectedActor != value)
                {
                    _selectedActor = value;
                    OnPropertyChanged(nameof(SelectedActor));
                }
            }
        }
        // Compute 3D Euclidean distance between the start and end camera positions.
        private double ComputeDistance()
        {
            double dx = EndCamX - StartCamX;
            double dy = EndCamY - StartCamY;
            double dz = EndCamZ - StartCamZ;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        // Recalculate speed when coordinates change.
        private void RecalculateSpeed()
        {
            if (!_updatingFramesOrSpeed)
            {
                _updatingFramesOrSpeed = true;
                double distance = ComputeDistance();
                Speed = (Frames > 0) ? distance / Frames : 0;
                _updatingFramesOrSpeed = false;
                OnPropertyChanged(nameof(Speed));
            }
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
