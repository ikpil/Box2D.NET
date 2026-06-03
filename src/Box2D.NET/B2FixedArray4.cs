// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CS0169

namespace Box2D.NET
{
    [StructLayout(LayoutKind.Sequential)]
    public struct B2FixedArray4<T> where T : unmanaged
    {
        public const int Size = 4;

        private T _v0000;
        private T _v0001;
        private T _v0002;
        private T _v0003;

        public int Length => Size;

        // readonly so an "in" or "ref readonly" receiver does not force a defensive
        // copy. This matches Span<T>.this[int], which is also a readonly ref T indexer.
        public readonly ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref AsSpanUnsafe()[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly Span<T> AsSpanUnsafe()
        {
            return MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in _v0000), Size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<T> AsReadOnlySpan()
        {
            return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in _v0000), Size);
        }
    }
}