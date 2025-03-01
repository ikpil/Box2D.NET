// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// This describes the motion of a body/shape for TOI computation. Shapes are defined with respect to the body origin,
    /// which may not coincide with the center of mass. However, to support dynamics we must interpolate the center of mass
    /// position.
    public struct B2Sweep
    {
        public B2Vec2 localCenter; // Local center of mass position
        public B2Vec2 c1; // Starting center of mass world position
        public B2Vec2 c2; // Ending center of mass world position
        public B2Rot q1; // Starting world rotation
        public B2Rot q2; // Ending world rotation

        public B2Sweep(B2Vec2 localCenter, B2Vec2 c1, B2Vec2 c2, B2Rot q1, B2Rot q2)
        {
            this.localCenter = localCenter;
            this.c1 = c1;
            this.c2 = c2;
            this.q1 = q1;
            this.q2 = q2;
        }
    }
}