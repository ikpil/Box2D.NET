// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CS0169

namespace Box2D.NET.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public struct B2FixedArray64<T> where T : unmanaged
    {
        public const int Length = 64;

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
        public T v0011;
        public T v0012;
        public T v0013;
        public T v0014;
        public T v0015;
        public T v0016;
        public T v0017;
        public T v0018;
        public T v0019;
        public T v0020;
        public T v0021;
        public T v0022;
        public T v0023;
        public T v0024;
        public T v0025;
        public T v0026;
        public T v0027;
        public T v0028;
        public T v0029;
        public T v0030;
        public T v0031;
        public T v0032;
        public T v0033;
        public T v0034;
        public T v0035;
        public T v0036;
        public T v0037;
        public T v0038;
        public T v0039;
        public T v0040;
        public T v0041;
        public T v0042;
        public T v0043;
        public T v0044;
        public T v0045;
        public T v0046;
        public T v0047;
        public T v0048;
        public T v0049;
        public T v0050;
        public T v0051;
        public T v0052;
        public T v0053;
        public T v0054;
        public T v0055;
        public T v0056;
        public T v0057;
        public T v0058;
        public T v0059;
        public T v0060;
        public T v0061;
        public T v0062;
        public T v0063;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Span<T> AsSpan()
        {
            return new Span<T>(Unsafe.AsPointer(ref v0000), Length);
        }
    }
}