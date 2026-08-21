// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Overlap AABB dispatcher

    // Shared context for the query replay trampolines: walks the recorded hits in order and flags
    // any divergence from the re-issued query
    // Managed callbacks box this once; Unsafe.Unbox recovers the same mutable value by ref.
    internal struct B2RecReplayQueryCtx
    {
        internal B2RecReader rdr;
        internal B2RecRecordedHit[] hits;
        internal int count;
        internal int cursor;
    }
}
