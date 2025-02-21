namespace Box2D.NET.Primitives
{
    /// Joint id references a joint instance. This should be treated as an opaque handle.
    public readonly struct b2JointId
    {
        public readonly int index1;
        public readonly ushort world0;
        public readonly ushort generation;

        public b2JointId(int index1, ushort world0, ushort generation)
        {
            this.index1 = index1;
            this.world0 = world0;
            this.generation = generation;
        }
    }
}