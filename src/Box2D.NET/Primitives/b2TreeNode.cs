using static Box2D.NET.constants;

namespace Box2D.NET.Primitives
{
    /// A node in the dynamic tree. This is private data placed here for performance reasons.
    public class b2TreeNode
    {
        /// The node bounding box
        public b2AABB aabb; // 16

        /// Category bits for collision filtering
        public ulong categoryBits; // 8

        // TODO: @ikpil, checking
        // union
        // {
        /// The node parent index (allocated node)
        public int parent;

        /// The node freelist next index (free node)
        public int next;
        //}; // 4

        /// Child 1 index (internal node)
        public int child1; // 4

        // TODO: @ikpil, checking
        //union
        //{
        /// Child 2 index (internal node)
        public int child2;

        /// User data (leaf node)
        public int userData;
        //}; // 4

        public ushort height; // 2
        public ushort flags; // 2

        public void Clear()
        {
            aabb = new b2AABB(); // 16
            categoryBits = 0; // 8
            parent = 0;
            next = 0;
            child1 = 0; // 4
            child2 = 0;
            userData = 0;
            height = 0; // 2
            flags = 0; // 2
        }

        public void Reset()
        {
            aabb = new b2AABB(new b2Vec2(0.0f, 0.0f), new b2Vec2(0.0f, 0.0f));
            categoryBits = B2_DEFAULT_CATEGORY_BITS;
            parent = B2_NULL_INDEX;
            child1 = B2_NULL_INDEX;
            child2 = B2_NULL_INDEX;
            height = 0;
            flags = (ushort)b2TreeNodeFlags.b2_allocatedNode;
        }
    }
}