// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    // The body state is designed for fast conversion to and from SIMD via scatter-gather.
    // Only awake dynamic and kinematic bodies have a body state.
    // This is used in the performance critical constraint solver
    //
    // 32 bytes
    public class b2BodyState
    {
        public b2Vec2 linearVelocity; // 8
        public float angularVelocity; // 4
        public int flags; // 4

        // Using delta position reduces round-off error far from the origin
        public b2Vec2 deltaPosition; // 8

        // Using delta rotation because I cannot access the full rotation on static bodies in
        // the solver and must use zero delta rotation for static bodies (c,s) = (1,0)
        public b2Rot deltaRotation; // 8

        public static b2BodyState Create(b2BodyState other)
        {
            var state = new b2BodyState();
            state.CopyFrom(other);
            return state;
        }
        
        public void Clear()
        {
            linearVelocity = new b2Vec2();
            angularVelocity = 0.0f;
            flags = 0;

            deltaPosition = new b2Vec2();

            deltaRotation = new b2Rot();
        }
        
        public void CopyFrom(b2BodyState other)
        {
            linearVelocity = other.linearVelocity;
            angularVelocity = other.angularVelocity;
            flags = other.flags;

            deltaPosition = other.deltaPosition;

            deltaRotation = other.deltaRotation;
        }
    }
}
