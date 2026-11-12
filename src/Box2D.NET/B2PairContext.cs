// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2BroadPhases;

namespace Box2D.NET
{
    public class B2PairContext
    {
        public B2World world;

        // Owns the pairKeys array for this worker.
        public B2TaskContext taskContext;

        public B2CandidatePair[] batch = new B2CandidatePair[B2_CANDIDATE_BATCH];
        public int batchCount;
    }
}
