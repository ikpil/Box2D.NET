// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Box2D.NET.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public struct UnsafeArray3<T> where T : unmanaged
    {
        public const int Length = 3;
        
        public T v0000;
        public T v0001;
        public T v0002;

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref GetElement(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe ref T GetElement(int index)
        {
            if (0 > index || Length <= index)
                throw new IndexOutOfRangeException();

            return ref Unsafe.AsRef<T>(Unsafe.Add<T>(Unsafe.AsPointer(ref v0000), index));
        }
    }
}