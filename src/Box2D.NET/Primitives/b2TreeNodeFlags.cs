using System;

namespace Box2D.NET.Primitives
{
    [Flags]
    public enum b2TreeNodeFlags
    {
        b2_allocatedNode = 0x0001,
        b2_enlargedNode = 0x0002,
        b2_leafNode = 0x0004,
    };
}