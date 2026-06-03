// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// These are collision planes that can be fed to b2SolvePlanes. Normally
    /// this is assembled by the user from plane results in b2PlaneResult
    public struct B2CollisionPlane
    {
        /// The collision plane between the mover and some shape
        public B2Plane plane;
        
        /// Setting this to FLT_MAX makes the plane as rigid as possible. Lower values can
        /// make the plane collision soft. Usually in meters.
        public float pushLimit;
        
        /// The push on the mover determined by b2SolvePlanes. Usually in meters.
        public float push;
        
        /// Indicates if b2ClipVector should clip against this plane. Should be false for soft collision.
        public bool clipVelocity;

        public B2CollisionPlane(B2Plane plane, float pushLimit, float push, bool clipVelocity)
        {
            this.plane = plane;
            this.pushLimit = pushLimit;
            this.push = push;
            this.clipVelocity = clipVelocity;
        }
    }
}
