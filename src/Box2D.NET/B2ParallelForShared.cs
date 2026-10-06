// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Shared state for one b2ParallelFor invocation. Workers race on nextBlock to
    // claim work, so a slow chunk can't strand the other threads.
    internal class B2ParallelForShared
    {
        public readonly B2ParallelForTask[] tasks = new B2ParallelForTask[B2Constants.B2_MAX_WORKERS];
        public readonly object[] handles = new object[B2Constants.B2_MAX_WORKERS];
        public System.Exception failure;

        public B2ParallelForShared()
        {
            for (int i = 0; i < tasks.Length; ++i)
            {
                tasks[i] = new B2ParallelForTask { shared = this, workerIndex = i };
            }
        }

        public B2AtomicInt nextBlock;
        public int blockCount;
        public int blockSize;
        public int itemCount;
        public b2ParallelForCallback callback;
        public object context;
    }
}