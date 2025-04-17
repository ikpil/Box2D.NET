// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// These are the collision planes returned from b2World_CollideMover
    public struct B2PlaneResult
    {
        /// The collision plane between the mover and a convex shape
        public B2Plane plane;
        
        /// Did the collision register a hit? If not this plane should be ignored.
        public bool hit;

        public B2PlaneResult(B2Plane plane, bool hit)
        {
            this.plane = plane;
            this.hit = hit;
        }
    }
}
