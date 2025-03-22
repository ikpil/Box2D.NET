// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CS0169

namespace Box2D.NET
{
    [StructLayout(LayoutKind.Sequential)]
    public struct B2FixedArray2<T> where T : unmanaged
    {
        public const int Size = 2;

        private T _v0000;
        private T _v0001;

        public int Length => Size;

        public B2FixedArray2(T v0000, T v0001)
        {
            _v0000 = v0000;
            _v0001 = v0001;
        }

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref AsSpan()[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref _v0000, Size);
        }

    }
}