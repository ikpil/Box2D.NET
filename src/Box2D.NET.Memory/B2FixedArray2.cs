// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Box2D.NET.Memory
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
            get => ref GetElement(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe ref T GetElement(int index)
        {
            if (0 > index || Size <= index)
                throw new IndexOutOfRangeException();

            return ref Unsafe.AsRef<T>(Unsafe.Add<T>(Unsafe.AsPointer(ref _v0000), index));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref _v0000, Size);
        }
    }
}