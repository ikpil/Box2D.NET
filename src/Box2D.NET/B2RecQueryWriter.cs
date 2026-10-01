// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;

namespace Box2D.NET
{
    // Per-query writer context: holds user fcn+ctx, the local payload buffer, and the hit counter
    internal sealed class B2RecQueryWriter : IDisposable
    {
        internal B2RecQueryUserFcn userFcn;
        internal object userContext;
        internal B2RecBuffer buf; // per-call local payload, heap-backed
        internal int countOffset; // offset of the reserved u32 hit-count slot
        internal uint hitCount;
        internal B2ObjectPool<B2RecQueryWriter> owner;

        public void Dispose()
        {
            if (owner == null)
            {
                return;
            }

            B2ObjectPool<B2RecQueryWriter> pool = owner;
            owner = null;
            pool.Return(this);
        }
    }
}
