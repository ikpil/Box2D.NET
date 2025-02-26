// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;

namespace Box2D.NET.Primitives
{
    // Each stage must be completed before going to the next stage.
    // Non-iterative stages use a stage instance once while iterative stages re-use the same instance each iteration.
    public class b2SolverStage
    {
        public b2SolverStageType type;
        public ArraySegment<b2SolverBlock> blocks;
        public int blockCount;

        public int colorIndex;

        // todo consider false sharing of this atomic
        public b2AtomicInt completionCount;
    }
}
