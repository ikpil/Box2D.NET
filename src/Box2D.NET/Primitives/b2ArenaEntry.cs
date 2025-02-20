using System;

namespace Box2D.NET.Primitives
{
    public class b2ArenaEntry<T>
    {
        public ArraySegment<T> data;
        public string name;
        public int size;
        public bool usedMalloc;
    }
}