using Box2D.NET.Core;

namespace Box2D.NET.Primitives
{
#if B2_SIMD_AVX2
    // wide float holds 8 numbers
    typedef __m256 b2FloatW;
#elif B2_SIMD_NEON
    // wide float holds 4 numbers
    typedef float32x4_t b2FloatW;
#elif B2_SIMD_SSE2
    // wide float holds 4 numbers
    typedef __m128 b2FloatW;
#else
    // scalar math
    // TODO: @ikpil, check SIMD
    public struct b2FloatW
    {
        private UnsafeArray4<float> _array;
        public float x { get => _array.v00; set => _array.v00 = value; }
        public float y { get => _array.v01; set => _array.v01 = value; }
        public float z { get => _array.v02; set => _array.v02 = value; }
        public float w { get => _array.v03; set => _array.v03 = value; }


        public b2FloatW(float x, float y, float z, float w)
        {
            _array.v00 = x;
            _array.v01 = y;
            _array.v02 = z;
            _array.v03 = w;
        }

        public ref float this[int index] => ref _array[index];
    }
#endif
}