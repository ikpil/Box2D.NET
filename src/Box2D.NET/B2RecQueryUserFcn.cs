// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;

namespace Box2D.NET
{
    // The C implementation uses a union for the overlap, cast, and plane function pointers.
    // These properties share one managed delegate so assigning any member replaces the others.
    internal struct B2RecQueryUserFcn
    {
        private Delegate value;

        internal b2OverlapResultFcn overlapFcn
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (b2OverlapResultFcn)value;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this.value = value;
        }

        internal Delegate castFcn
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => value;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this.value = value;
        }

        internal b2PlaneResultFcn planeFcn
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (b2PlaneResultFcn)value;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this.value = value;
        }
    }
}
