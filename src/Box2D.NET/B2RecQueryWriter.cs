// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Per-query writer context: holds user fcn+ctx, the local payload buffer, and the hit counter
    internal struct B2RecQueryWriter
    {
        internal B2RecQueryUserFcn userFcn;
        internal object userContext;
        internal B2RecBuffer buf; // per-call local payload, heap-backed
        internal int countOffset; // offset of the reserved u32 hit-count slot
        internal uint hitCount;
    }
}
