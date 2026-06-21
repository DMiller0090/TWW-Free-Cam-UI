using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TWW_Free_Cam_UI.Dolphin.Camera;

namespace TWW_Free_Cam_UI.Dolphin.Player
{
    public class TWW_JP_Player : IPlayerEditable
    {
        private const uint LOCATION = 0x803D78FC;
        private const uint ROTATION = 0x803EA3D2;
        public ICameraEditable camera { get; set; }
        public TWW_JP_Player()
        {
            camera = new TWW_JP_Editor();
        }
        public (float x, float y, float z) GetLocation()
        {
            float x = Memory.ReadMemory<float>(LOCATION);
            float y = Memory.ReadMemory<float>(LOCATION + 4);
            float z = Memory.ReadMemory<float>(LOCATION + 8);
            return (x, y, z);
        }

        public ushort GetRotation()
        {
            return Memory.ReadMemory<ushort>(ROTATION);
        }

        public void WriteLocation(float x, float y, float z)
        {
            Memory.WriteMemory<float>(LOCATION, x);
            Memory.WriteMemory<float>(LOCATION + 4, y);
            Memory.WriteMemory<float>(LOCATION + 8, z);
        }

        public void WriteRotation(ushort value)
        {
            Memory.WriteMemory<ushort>(ROTATION, value);
        }
    }
}
