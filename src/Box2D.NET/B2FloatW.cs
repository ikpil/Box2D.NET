// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;

namespace Box2D.NET
{
#if B2_SIMD_AVX2
    // wide float holds 8 numbers
    typedef __m256 B2FloatW;
#elif B2_SIMD_NEON
    // wide float holds 4 numbers
    typedef float32x4_t B2FloatW;
#elif B2_SIMD_SSE2
    // wide float holds 4 numbers
    typedef __m128 B2FloatW;
#else
    // scalar math
    // TODO: @ikpil, check SIMD
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

        public ref float this[int index] => ref MemoryMarshal.CreateSpan(ref X, 4)[index];
    }
#endif
}
