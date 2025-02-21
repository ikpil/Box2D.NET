using Box2D.NET.Core;

namespace Box2D.NET.Primitives
{
    /// Used to warm start the GJK simplex. If you call this function multiple times with nearby
    /// transforms this might improve performance. Otherwise you can zero initialize this.
    /// The distance cache must be initialized to zero on the first call.
    /// Users should generally just zero initialize this structure for each call.
    public struct b2SimplexCache
    {
        /// The number of stored simplex points
        public ushort count;

        /// The cached simplex indices on shape A
        public UnsafeArray3<byte> indexA;

        /// The cached simplex indices on shape B
        public UnsafeArray3<byte> indexB;
    }
}