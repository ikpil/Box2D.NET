﻿namespace Box2D.NET.Primitives
{
    /// World id references a world instance. This should be treated as an opaque handle.
    public readonly struct b2WorldId
    {
        public readonly ushort index1;
        public readonly ushort generation;

        public b2WorldId(ushort index1, ushort generation)
        {
            this.index1 = index1;
            this.generation = generation;
        }
    }
}