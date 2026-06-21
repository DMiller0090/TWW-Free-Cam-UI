using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TWW_Free_Cam_UI.Dolphin
{
    internal enum Address : UInt64
    {
        airMeter = 0x803BDC62,
        xPos = 0x803D78FC,
        yPos = 0x803D7900,
        zPos = 0x803D7904,
        facingDirection = 0x803EA3D2,

        linkPtr = 0x803ad860,
        linkSpeed = 0x34e4,
        linkAccelCheat = 0x803E9DE0,
        linkState = 0x3100,

        korlPtr = 0x803BDC50,
        korlSpeed = 0x254,

        stage = 0x803BD23C,
        speedCapPtr = 0x803BD910,
        speedCap = 0x2A8,
        travelDirection = 0x206,

        animationPtr = 0x803AD860,
        animationIncrement = 0x2F60,
        animationFrame = 0x2F64,

        currentFrame = 0x803E9D34,//0x803DB620,

        displacementPtr = 0x803B02E4,
        displacement = 0x444,

        cameraPtr = 0x803AD380,
        cameraPtr_Offset = 0x34,
        camera_cSAngle = 0x2B0,
        camera_mCenterX = 0x254,
        camera_mCenterY = 0x258,
        camera_mCenterZ = 0x25C,
        camera_mEyeX = 0x260,
        camera_mEyeY = 0x264,
        camera_mEyeZ = 0x268,

        camera_xPos = 0xD8,
        camera_yPos = 0xDC,
        camera_zPos = 0xE0,
        camera_autoFocus = 0x248,

        cosTablePtr = 0x803EAE2C,

        storage = 0x803BD3A3,

        rng_r0 = 0x803EA7D8,
        rng_r1 = 0x803EA7DC,
        rng_r2 = 0x803EA7E0

    }
}
