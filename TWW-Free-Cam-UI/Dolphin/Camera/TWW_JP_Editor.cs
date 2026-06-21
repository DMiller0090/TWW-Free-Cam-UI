using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TWW_Free_Cam_UI.Dolphin.Camera
{
    public class TWW_JP_Editor : ICameraEditable
    {
        private const uint CAMERAPTR = 0x803AD380;
        private const uint CAMERAOFFSET = 0x34;
        private const uint AUTOFOCUS = 0x248;
        private const uint EYE = 0x260;
        private const uint CENTER = 0x254;
        private const uint CSANGLE = 0x2B0;
        private const uint CURRENTFRAME = 0x803E9D34;

        private uint _cameraPtr = 0;
        public TWW_JP_Editor()
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
            byte value = disable ? (byte)0 : (byte)1;
            Memory.WriteMemory<byte>(_cameraPtr + AUTOFOCUS, value);
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
            Memory.WriteMemory<uint>(0x802CD9B8, 0x38000000);
            Memory.WriteMemory<uint>(0x802CDA68, 0x38000000);
            Memory.WriteMemory<uint>(0x800485C8, 0x38000000);
            Memory.WriteMemory<uint>(0x80048610, 0x4E800020);
            Memory.WriteMemory<uint>(0x80049878, 0x38000000);
            Memory.WriteMemory<uint>(0x8004A6C8, 0x38800000);
        }

        public void EnableUI()
        {
            Memory.WriteMemory<uint>(0x802CD9B8, 0x881B00AC);
            Memory.WriteMemory<uint>(0x802CDA68, 0x881B00AC);
            Memory.WriteMemory<uint>(0x800485C8, 0x880D89A6);
            Memory.WriteMemory<uint>(0x80048610, 0x9421FFD0);
            Memory.WriteMemory<uint>(0x80049878, 0x880D8999);
            Memory.WriteMemory<uint>(0x8004A6C8, 0x888D8999);
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
