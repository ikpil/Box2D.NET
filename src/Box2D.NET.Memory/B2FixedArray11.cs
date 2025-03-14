﻿// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CS0169

namespace Box2D.NET.Memory
{
    [StructLayout(LayoutKind.Sequential)]
    public struct B2FixedArray11<T> where T : unmanaged
    {
        public const int Size = 11;

        public T v0000;
        public T v0001;
        public T v0002;
        public T v0003;
        public T v0004;
        public T v0005;
        public T v0006;
        public T v0007;
        public T v0008;
        public T v0009;
        public T v0010;
        
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

            return ref Unsafe.AsRef<T>(Unsafe.Add<T>(Unsafe.AsPointer(ref v0000), index));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Span<T> AsSpan()
        {
            return new Span<T>(Unsafe.AsPointer(ref v0000), Size);
        }
    }
}