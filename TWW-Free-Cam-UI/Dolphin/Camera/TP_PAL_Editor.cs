using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TWW_Free_Cam_UI.Dolphin.Camera
{
    public class TP_PAL_Editor : ICameraEditable
    {
        private const uint CAMERAPTR = 0x8040DED4;
        private const uint CAMERAOFFSET = 0x248;
        private const uint AUTOFOCUS = 0x26C;
        private const uint EYE = 0x284;
        private const uint CENTER = 0x278;
        private const uint CSANGLE = 0x2D4;
        private const uint CURRENTFRAME = 0x80452AF4;

        private uint _cameraPtr = 0;
        public TP_PAL_Editor()
        {
            _cameraPtr = Memory.ReadMemory<uint>(CAMERAPTR);
            _cameraPtr = Memory.ReadMemory<uint>(_cameraPtr + CAMERAOFFSET);
            if(_cameraPtr == 0)
            {
                throw new Exception("Unable to read camera pointer address.");
            }
        }

        public uint ReadCurrentFrame()
        {
            return Memory.ReadMemory<uint>(CURRENTFRAME);
        }
        public void WriteAutofocus(bool disable)
        {
            uint value = disable ? (byte)3 : (byte)0;
            Memory.WriteMemory<uint>(_cameraPtr + AUTOFOCUS, value);
        }

        public void WriteCameraCenter(float x, float y, float z)
        {
            Memory.WriteMemory<float>(_cameraPtr + CENTER, x);
            Memory.WriteMemory<float>(_cameraPtr + CENTER + 4, y);
            Memory.WriteMemory<float>(_cameraPtr + CENTER + 8, z);
        }

        public void WriteCameraEye(float x, float y, float z)
        {
            Memory.WriteMemory<float>(_cameraPtr + EYE, x);
            Memory.WriteMemory<float>(_cameraPtr + EYE + 4, y);
            Memory.WriteMemory<float>(_cameraPtr + EYE + 8, z);
        }
        public void DisableUI()
        {
            //TODO
        }

        public void EnableUI()
        {
            //TODO
        }

        public void WriteCameraEyeX(float x)
        {
            Memory.WriteMemory<float>(_cameraPtr + EYE, x);
        }

        public void WriteCameraEyeY(float y)
        {
            Memory.WriteMemory<float>(_cameraPtr + EYE + 4, y);
        }

        public void WriteCameraEyeZ(float z)
        {
            Memory.WriteMemory<float>(_cameraPtr + EYE + 8, z);
        }

        public void WriteCameraCenterX(float x)
        {
            Memory.WriteMemory<float>(_cameraPtr + CENTER, x);
        }

        public void WriteCameraCenterY(float y)
        {
            Memory.WriteMemory<float>(_cameraPtr + CENTER + 4, y);
        }

        public void WriteCameraCenterZ(float z)
        {
            Memory.WriteMemory<float>(_cameraPtr + CENTER + 8, z);
        }

        public float ReadCameraEyeX()
        {
            return Memory.ReadMemory<float>(_cameraPtr + EYE);
        }

        public float ReadCameraEyeY()
        {
            return Memory.ReadMemory<float>(_cameraPtr + EYE + 4);
        }

        public float ReadCameraEyeZ()
        {
            return Memory.ReadMemory<float>(_cameraPtr + EYE + 8);
        }

        public float ReadCameraCenterX()
        {
            return Memory.ReadMemory<float>(_cameraPtr + CENTER);
        }

        public float ReadCameraCenterY()
        {
            return Memory.ReadMemory<float>(_cameraPtr + CENTER + 4);
        }

        public float ReadCameraCenterZ()
        {
            return Memory.ReadMemory<float>(_cameraPtr + CENTER + 8);
        }
        public ushort ReadCsAngle()
        {
            return Memory.ReadMemory<ushort>(_cameraPtr + CSANGLE);
        }
        public void WriteCsAngle(ushort angle)
        {
            Memory.WriteMemory<ushort>(_cameraPtr + CSANGLE, angle);
        }
    }
}
