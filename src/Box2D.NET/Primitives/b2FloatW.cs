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
        public float x { get => _array.v0000; set => _array.v0000 = value; }
        public float y { get => _array.v0001; set => _array.v0001 = value; }
        public float z { get => _array.v0002; set => _array.v0002 = value; }
        public float w { get => _array.v0003; set => _array.v0003 = value; }


        public b2FloatW(float x, float y, float z, float w)
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