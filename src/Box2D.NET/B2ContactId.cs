namespace Box2D.NET
{
    /// Contact id references a contact instance. This should be treated as an opaque handled.
    public readonly struct B2ContactId
    {
        public readonly int index1;
        public readonly ushort world0;
        public readonly short padding;
        public readonly uint generation;

        public B2ContactId(int index1, ushort world0, short padding, uint generation)
        {
            this.index1 = index1;
            this.world0 = world0;
            this.padding = padding;
            this.generation = generation;
        }
    }
}