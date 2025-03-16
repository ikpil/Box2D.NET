// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Memory;

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
    public struct B2FloatW
    {
        private B2FixedArray4<float> _array;
        public float X { get => _array.v0000; set => _array.v0000 = value; }
        public float Y { get => _array.v0001; set => _array.v0001 = value; }
        public float Z { get => _array.v0002; set => _array.v0002 = value; }
        public float W { get => _array.v0003; set => _array.v0003 = value; }


        public B2FloatW(float x, float y, float z, float w)
        {
            _array.v0000 = x;
            _array.v0001 = y;
            _array.v0002 = z;
            _array.v0003 = w;
        }

        public ref float this[int index] => ref _array[index];
    }
#endif
}
