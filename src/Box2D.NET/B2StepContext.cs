// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;

namespace Box2D.NET
{
    // Context for a time step. Recreated each time step.
    public class B2StepContext // TODO: @ikpil, check struct or class
    {
        // time step
        public float dt;

        // inverse time step (0 if dt == 0).
        public float inv_dt;

        // sub-step
        public float h;
        public float inv_h;

        public int subStepCount;

        public B2Softness contactSoftness;
        public B2Softness staticSoftness;

        public float restitutionThreshold;
        public float maxLinearVelocity;

        public B2World world;
        public B2ConstraintGraph graph;

        // shortcut to body states from awake set
        public B2BodyState[] states;

        // shortcut to body sims from awake set
        public B2BodySim[] sims;

        // array of all shape ids for shapes that have enlarged AABBs
        public int[] enlargedShapes;
        public int enlargedShapeCount;

        // Array of bullet bodies that need continuous collision handling
        public ArraySegment<int> bulletBodies;
        public B2AtomicInt bulletBodyCount;

        // joint pointers for simplified parallel-for access.
        public ArraySegment<B2JointSim> joints;

        // contact pointers for simplified parallel-for access.
        // - parallel-for collide with no gaps
        // - parallel-for prepare and store contacts with NULL gaps for SIMD remainders
        // despite being an array of pointers, these are contiguous sub-arrays corresponding
        // to constraint graph colors
        public ArraySegment<B2ContactSim> contacts;

        public ArraySegment<B2ContactConstraintSIMD> simdContactConstraints;
        public int activeColorCount;
        public int workerCount;

        public ArraySegment<B2SolverStage> stages;
        public int stageCount;
        public bool enableWarmStarting;

        // todo padding to prevent false sharing
        public B2FixedArray64<byte> dummy1;

        // sync index (16-bits) | stage type (16-bits)
        public B2AtomicU32 atomicSyncBits;

        public B2FixedArray64<byte> dummy2;
    }
}
