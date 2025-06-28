using System;

namespace Box2D.NET
{
    [Flags]
    public enum B2BodyFlags
    {
        // This body has fixed translation along the x-axis
        b2_lockLinearX = 0x00000001,

        // This body has fixed translation along the y-axis
        b2_lockLinearY = 0x00000002,

        // This body has fixed rotation
        b2_lockAngularZ = 0x00000004,

        // This flag is used for debug draw
        b2_isFast = 0x00000008,

        // This dynamic body does a final CCD pass against all body types, but not other bullets
        b2_isBullet = 0x00000010,

        // This body has hit the maximum linear or angular velocity
        b2_isSpeedCapped = 0x00000020,

        // This body has no limit on angular velocity
        b2_allowFastRotation = 0x00000040,

        // This body need's to have its AABB increased
        b2_enlargeBounds = 0x00000080,

        // All lock flags
        b2_allLocks = b2_lockAngularZ | b2_lockLinearX | b2_lockLinearY,
    }
}