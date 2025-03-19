// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CS0169

namespace Box2D.NET.Memory
{
    [StructLayout(LayoutKind.Sequential)]
    public struct B2FixedArray12<T> where T : unmanaged
    {
        public const int Size = 12;

        private T _v0000;
        private T _v0001;
        private T _v0002;
        private T _v0003;
        private T _v0004;
        private T _v0005;
        private T _v0006;
        private T _v0007;
        private T _v0008;
        private T _v0009;
        private T _v0010;
        private T _v0011;

        public int Length => Size;

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