// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;

namespace Box2D.NET
{
    /// A world transform with a world position translation and a float rotation.
    /// Upstream uses double precision translation in large world mode.
    public struct B2WorldTransform
    {
        public B2Pos p;
        public B2Rot q;

        public B2WorldTransform(B2Pos p, B2Rot q)
        {
            this.p = p;
            this.q = q;
        }

        public bool TryWriteBytes(Span<byte> bytes)
        {
            if (!BitConverter.TryWriteBytes(bytes.Slice(0, 4), p.X))
                return false;

            if (!BitConverter.TryWriteBytes(bytes.Slice(4, 4), p.Y))
                return false;

            if (!BitConverter.TryWriteBytes(bytes.Slice(8, 4), q.c))
                return false;

            if (!BitConverter.TryWriteBytes(bytes.Slice(12, 4), q.s))
                return false;

            return true;
        }
    }
}
