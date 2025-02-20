namespace Box2D.NET.Primitives
{
    /// Used to warm start the GJK simplex. If you call this function multiple times with nearby
    /// transforms this might improve performance. Otherwise you can zero initialize this.
    /// The distance cache must be initialized to zero on the first call.
    /// Users should generally just zero initialize this structure for each call.
    public class b2SimplexCache // TODO: @ikpil, check class or struct, struct 성격이 강하다
    {
        /// The number of stored simplex points
        public ushort count;

        /// The cached simplex indices on shape A
        public byte[] indexA = new byte[3];

        /// The cached simplex indices on shape B
        public byte[] indexB = new byte[3];
    }
}