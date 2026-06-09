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
            get => ref AsSpanUnsafe()[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Span<T> AsSpanUnsafe()
        {
            return MemoryMarshal.CreateSpan(ref _v0000, Size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<T> AsReadOnlySpan()
        {
            return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in _v0000), Size);
        }

    }
}