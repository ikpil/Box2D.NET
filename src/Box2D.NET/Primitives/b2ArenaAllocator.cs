using System;

namespace Box2D.NET.Primitives
{
    // This is a stack-like arena allocator used for fast per step allocations.
    // You must nest allocate/free pairs. The code will Debug.Assert
    // if you try to interleave multiple allocate/free pairs.
    // This allocator uses the heap if space is insufficient.
    // I could remove the need to free entries individually.
    public class b2ArenaAllocator<T> : IArenaAllocator where T : new()
    {
        private static readonly object _lock = new object();
        private static b2ArenaAllocator<T> _singleton;

        public ArraySegment<T> data;
        public int capacity;
        public int index;

        public int allocation;
        public int maxAllocation;

        public b2Array<b2ArenaEntry<T>> entries;

        public static b2ArenaAllocator<T> Touch(b2ArenaAllocator allocator)
        {
            if (null == _singleton)
            {
                lock (_lock)
                {
                    if (null == _singleton)
                    {
                        _singleton = arena_allocator.b2CreateArenaAllocator<T>(16); // TODO: @ikpil, test
                        allocator.Add(_singleton);
                    }
                }
            }

            return _singleton;
        }
    }

    public class b2ArenaAllocator
    {
        public b2ArenaAllocator<T> Touch<T>() where T : new()
        {
            return b2ArenaAllocator<T>.Touch(this);
        }

        // @ikpil, check!!
        public void Add<T>(b2ArenaAllocator<T> alloc) where T : new()
        {
            // ...
        }

        public IArenaAllocator[] GetAll()
        {
            return null;
        }
    }
}