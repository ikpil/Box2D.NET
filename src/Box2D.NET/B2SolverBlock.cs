// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Each block of work has a sync index that gets incremented when a worker claims the block. This ensures only a single worker
    // claims a block, yet lets work be distributed dynamically across multiple workers (work stealing). This also reduces contention
    // on a single block index atomic. For non-iterative stages the sync index is simply set to one. For iterative stages (solver
    // iteration) the same block of work is executed once per iteration and the atomic sync index is shared across iterations, so it
    // increases monotonically.
    public class B2SolverBlock
    {
        public int startIndex;
        public short count;

        public short blockType; // b2SolverBlockType

        // todo consider false sharing of this atomic
        public B2AtomicInt syncIndex;
    }
}
