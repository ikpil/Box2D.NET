﻿namespace Box2D.NET.Primitives
{
    /// Counters that give details of the simulation size.
    public class b2Counters
    {
        public int bodyCount;
        public int shapeCount;
        public int contactCount;
        public int jointCount;
        public int islandCount;
        public int stackUsed;
        public int staticTreeHeight;
        public int treeHeight;
        public int byteCount;
        public int taskCount;
        public readonly int[] colorCounts = new int[12];
    }
}