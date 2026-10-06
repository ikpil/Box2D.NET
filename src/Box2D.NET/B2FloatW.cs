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
#if NET8_0_OR_GREATER
        private System.Runtime.Intrinsics.Vector256<float> _value;
        public ref float X => ref AsSpan()[0];
        public ref float Y => ref AsSpan()[1];
        public ref float Z => ref AsSpan()[2];
        public ref float W => ref AsSpan()[3];
        public ref float E => ref AsSpan()[4];
        public ref float F => ref AsSpan()[5];
        public ref float G => ref AsSpan()[6];
        public ref float H => ref AsSpan()[7];
#else
        public float X, Y, Z, W, E, F, G, H;
#endif

        public B2FloatW(float x, float y, float z, float w) : this(x, y, z, w, 0, 0, 0, 0) { }

        public B2FloatW(float x, float y, float z, float w, float e, float f, float g, float h)
        {
#if NET8_0_OR_GREATER
            _value = System.Runtime.Intrinsics.Vector256.Create(x, y, z, w, e, f, g, h);
#else
            X = x;
            Y = y;
            Z = z;
            W = w;
            E = e;
            F = f;
            G = g;
            H = h;
#endif
        }

        public readonly ref float this[int index] => ref AsSpan()[index];

        public readonly Span<float> AsSpan()
        {
#if NET8_0_OR_GREATER
            return MemoryMarshal.CreateSpan(ref Unsafe.As<System.Runtime.Intrinsics.Vector256<float>, float>(ref Unsafe.AsRef(in _value)), 8);
#else
            return MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in X), 8);
#endif
        }
    }
}
