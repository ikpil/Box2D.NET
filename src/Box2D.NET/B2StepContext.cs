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

        // contact pointers for simplified parallel-for access.
        // - parallel-for collide with no gaps, includes touching and non-touching
        public ArraySegment<B2ContactSim> contactSims;

        // Flat view of the wide contact constraint array used by prepare and store.
        // prepareSpans has activeColorCount + 1 entries, the last being a sentinel
        // at wideContactCount. wideContactConstraints is the contiguous base
        // pointer; per-color slices live at colors[i].wideConstraints.
        public ArraySegment<B2ContactConstraintWide> wideContactConstraints;
        public B2ContactPrepareSpan[] contactPrepareSpans;
        public int wideContactCount;

        public B2JointPrepareSpan[] jointPrepareSpans;
        public int jointCount;

        public int activeColorCount;
        public int workerCount;

        public ArraySegment<B2SolverStage> stages;
        public int stageCount;
        public bool enableWarmStarting;

        // padding to prevent false sharing
        public B2FixedArray64<byte> padding1;

        // This atomic is central to multi-threaded solver task synchronization.
        // It prevents ABA problems by monotonically growing as the solver advances.
        // This means a delayed worker thread will catch up without repeating already completed
        // work (causing a race condition).
        // sync index (16-bits) | stage type (16-bits)
        public B2AtomicU32 atomicSyncBits;

        // padding to prevent false sharing
        public B2FixedArray64<byte> padding2;

        // Race flag claimed by whichever runner reaches b2SolverTask with workerIndex 0 first.
        // The calling thread of b2World_Step also races for this slot so the orchestrator can
        // always make progress, regardless of how the user's task system schedules tasks (out
        // of order, fewer threads than workers, or synchronously inside enqueueTaskFcn). The
        // loser of the race no-ops as workerIndex 0.
        public B2AtomicInt mainClaimed;

        // padding to prevent false sharing
        public B2FixedArray64<byte> padding3;
    }
}
