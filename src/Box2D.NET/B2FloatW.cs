// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Box2D.NET
{
    // scalar math
    [StructLayout(LayoutKind.Sequential)]
    public struct B2FloatW
    {
        public float X;
        public float Y;
        public float Z;
        public float W;


        public B2FloatW(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        // readonly so an "in" or "ref readonly" receiver does not force a defensive
        // copy. This matches Span<T>.this[int], which is also a readonly ref T indexer.
        public readonly ref float this[int index] => ref MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in X), 4)[index];

        public readonly Span<float> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in X), 4);
        }
    }
}