﻿// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CS0169

namespace Box2D.NET
{
    [StructLayout(LayoutKind.Sequential)]
    public struct B2FixedArray24<T> where T : unmanaged
    {
        public const int Size = 24;

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
        private T _v0012;
        private T _v0013;
        private T _v0014;
        private T _v0015;
        private T _v0016;
        private T _v0017;
        private T _v0018;
        private T _v0019;
        private T _v0020;
        private T _v0021;
        private T _v0022;
        private T _v0023;

        public int Length => Size;

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