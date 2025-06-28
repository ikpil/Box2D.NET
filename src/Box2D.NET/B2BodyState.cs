// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Body State
    // The body state is designed for fast conversion to and from SIMD via scatter-gather.
    // Only awake dynamic and kinematic bodies have a body state.
    // This is used in the performance critical constraint solver
    //
    // The solver operates on the body state. The body state array does not hold static bodies. Static bodies are shared
    // across worker threads. It would be okay to read their states, but writing to them would cause cache thrashing across
    // workers, even if the values don't change.
    // This causes some trouble when computing anchors. I rotate joint anchors using the body rotation every sub-step. For static
    // bodies the anchor doesn't rotate. Body A or B could be static and this can lead to lots of branching. This branching
    // should be minimized.
    //
    // Solution 1:
    // Use delta rotations. This means anchors need to be prepared in world space. The delta rotation for static bodies will be
    // identity using a dummy state. Base separation and angles need to be computed. Manifolds will be behind a frame, but that
    // is probably best if bodies move fast.
    //
    // Solution 2:
    // Use full rotation. The anchors for static bodies will be in world space while the anchors for dynamic bodies will be in local
    // space. Potentially confusing and bug prone.
    //
    // Note:
    // I rotate joint anchors each sub-step but not contact anchors. Joint stability improves a lot by rotating joint anchors
    // according to substep progress. Contacts have reduced stability when anchors are rotated during substeps, especially for
    // round shapes.

    // 32 bytes
    public class B2BodyState
    {
        public B2Vec2 linearVelocity; // 8
        public float angularVelocity; // 4
        
        // b2BodyFlags
        public uint flags; // 4

        // Using delta position reduces round-off error far from the origin
        public B2Vec2 deltaPosition; // 8

        // Using delta rotation because I cannot access the full rotation on static bodies in
        // the solver and must use zero delta rotation for static bodies (c,s) = (1,0)
        public B2Rot deltaRotation; // 8

        public static B2BodyState Create(B2BodyState other) // todo : @ikpil how to remove
        {
            var state = new B2BodyState();
            state.CopyFrom(other);
            return state;
        }

        public void Clear()
        {
            linearVelocity = new B2Vec2();
            angularVelocity = 0.0f;
            flags = 0;

            deltaPosition = new B2Vec2();

            deltaRotation = new B2Rot();
        }

        public void CopyFrom(B2BodyState other)
        {
            linearVelocity = other.linearVelocity;
            angularVelocity = other.angularVelocity;
            flags = other.flags;
            
            deltaPosition = other.deltaPosition;
            deltaRotation = other.deltaRotation;
        }
    }
}