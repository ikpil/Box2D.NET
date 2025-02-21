namespace Box2D.NET.Primitives
{
    /// Chain id references a chain instances. This should be treated as an opaque handle.
    public readonly struct b2ChainId
    {
        public readonly int index1;
        public readonly ushort world0;
        public readonly ushort generation;

        public b2ChainId(int index1, ushort world0, ushort generation)
        {
            this.index1 = index1;
            this.world0 = world0;
            this.generation = generation;
        }
    }
}