﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    // Per thread task storage
    public class B2TaskContext
    {
        // These bits align with the b2ConstraintGraph::contactBlocks and signal a change in contact status
        public B2BitSet contactStateBitSet;

        // Used to track bodies with shapes that have enlarged AABBs. This avoids having a bit array
        // that is very large when there are many static shapes.
        public B2BitSet enlargedSimBitSet;

        // Used to put islands to sleep
        public B2BitSet awakeIslandBitSet;

        // Per worker split island candidate
        public float splitSleepTime;
        public int splitIslandId;
    }
}
