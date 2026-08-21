// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Append-only write buffer. Flushed to disk on each Step and on Save/Stop.
    internal struct B2RecBuffer
    {
        internal byte[] data;
        internal int capacity;
        internal int size;
    }
}
