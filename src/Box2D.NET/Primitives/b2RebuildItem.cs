namespace Box2D.NET.Primitives
{
    // Temporary data used to track the rebuild of a tree node
    public class b2RebuildItem
    {
        public int nodeIndex;
        public int childCount;

        // Leaf indices
        public int startIndex;
        public int splitIndex;
        public int endIndex;
    }
}