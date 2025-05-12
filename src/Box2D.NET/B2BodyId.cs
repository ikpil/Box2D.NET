// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;
using static Box2D.NET.B2Ids;

namespace Box2D.NET
{
    /// Body id references a body instance. This should be treated as an opaque handle.
    public readonly struct B2BodyId
    {
        public readonly int index1;
        public readonly ushort world0;
        public readonly ushort generation;

        public B2BodyId(int index1, ushort world0, ushort generation)
        {
            this.index1 = index1;
            this.world0 = world0;
            this.generation = generation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(B2BodyId a, B2BodyId b)
        {
            ulong ua = b2StoreBodyId(a);
            ulong ub = b2StoreBodyId(b);
            return ua < ub;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(B2BodyId a, B2BodyId b)
        {
            ulong ua = b2StoreBodyId(a);
            ulong ub = b2StoreBodyId(b);
            return ua > ub;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(B2BodyId a, B2BodyId b)
        {
            ulong ua = b2StoreBodyId(a);
            ulong ub = b2StoreBodyId(b);
            return ua == ub;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(B2BodyId a, B2BodyId b)
        {
            return !(a == b);
        }
    }
}