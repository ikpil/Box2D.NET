// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Solver work is partitioned into fixed-size blocks that worker threads claim
    // in parallel via atomic CAS on each block's own syncIndex. Three properties
    // of this design matter for performance:
    //
    // 1. Distributed contention. Per-block atomic syncIndex avoids the cache line stampede
    //    that a single shared fetch_add counter would cause. Once a worker
    //    settles into a block range, its CAS targets live in its own L1.
    //
    // 2. Monotonic syncIndex across iterations. Iterative stages (warm start,
    //    solve, relax) reuse the same block array every sub-step iteration.
    //    syncIndex grows each iteration; workers CAS (prev, prev+1), so the
    //    main thread never touches any per-block state between iterations.
    //    Non-iterative stages simply use syncIndex 1.
    //
    // 3. L2 affinity across iterations. Each worker picks a start offset from
    //    its workerIndex, then scans forward and (after wrap) backward:
    //
    //      blocks:   [0] [1] [2] [3] [4] [5] [6] [7]
    //                 ^           ^           ^   ^
    //                 W0          W1          W2  W3   <- start offsets
    //
    //    W0 claims 0,1,2,3 (forward), W1 claims 4,5, etc. Under balanced load
    //    each worker re-hits the same block range every iteration, keeping that
    //    range's hot data resident in its L2. A failed CAS means a neighbour
    //    already claimed the block, so the stealing worker stops -- preserving
    //    locality under mild imbalance while still draining the queue.
    //
    // A graph color stage lays out joint blocks first, then contact blocks:
    //
    //      stage->blocks ->
    //        +------+------+------+------+------+------+------+
    //        |  J0  |  J1  |  J2  |  C0  |  C1  |  C2  |  C3  |
    //        +------+------+------+------+------+------+------+
    //        <-- graphJointBlocks --><---- graphContactBlocks ---->
    //
    // Each block carries its type so the dispatcher routes J-blocks to the joint
    // solver and C-blocks to the SIMD contact solver; both kinds run concurrently
    // within the stage -- no barrier between them. The type tag lives on the
    // block (not the stage) so that mixed-type stages can keep the concurrency.
    public class B2SolverBlock
    {
        public int startIndex;
        public ushort count;

        // b2SolverBlockType
        public short blockType;

        public B2AtomicInt syncIndex;
    }
}
