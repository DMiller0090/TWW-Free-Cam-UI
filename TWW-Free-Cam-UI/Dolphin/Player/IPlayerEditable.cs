using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TWW_Free_Cam_UI.Dolphin.Camera;

namespace TWW_Free_Cam_UI.Dolphin.Player
{
    public interface IPlayerEditable
    {
        public ICameraEditable camera { get; set; }
        public (float x, float y, float z) GetLocation();
        public void WriteLocation(float x, float y, float z);
        public ushort GetRotation();
        public void WriteRotation(ushort value);
    }
}
