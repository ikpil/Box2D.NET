// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Temporary data used to track the rebuild of a tree node
    public struct B2RebuildItem
    {
        public int nodeIndex;
        public int childCount;

        // Leaf indices
        public int startIndex;
        public int splitIndex;
        public int endIndex;
    }
}
