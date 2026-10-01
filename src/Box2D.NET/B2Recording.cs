// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.IO;

namespace Box2D.NET
{
    // Recording state. Operations on this type live in B2Recordings and keep the upstream
    // b2Rec* function names so the C implementation can be searched alongside the port.
    internal sealed class B2Recording
    {
        internal FileStream file;
        internal B2RecBuffer buffer;
        internal int recordStart; // offset of the 3-byte size field for u24 backpatch
        internal object @lock; // serializes query record commits across concurrent query threads
    }
}
