﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Counters that give details of the simulation size.
    public class B2Counters
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
