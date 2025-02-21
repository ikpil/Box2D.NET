namespace Box2D.NET.Primitives
{
    /// Body id references a body instance. This should be treated as an opaque handle.
    public readonly struct b2BodyId
    {
        public readonly int index1;
        public readonly ushort world0;
        public readonly ushort generation;

        public b2BodyId(int index1, ushort world0, ushort generation)
        {
            this.index1 = index1;
            this.world0 = world0;
            this.generation = generation;
        }
    }
}