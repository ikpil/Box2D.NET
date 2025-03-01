// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /**@}*/
    /**
     * @defgroup tree Dynamic Tree
     * The dynamic tree is a binary AABB tree to organize and query large numbers of geometric objects
     *
     * Box2D uses the dynamic tree internally to sort collision shapes into a binary bounding volume hierarchy.
     * This data structure may have uses in games for organizing other geometry data and may be used independently
     * of Box2D rigid body simulation.
     *
     * A dynamic AABB tree broad-phase, inspired by Nathanael Presson's btDbvt.
     * A dynamic tree arranges data in a binary tree to accelerate
     * queries such as AABB queries and ray casts. Leaf nodes are proxies
     * with an AABB. These are used to hold a user collision object.
     * Nodes are pooled and relocatable, so I use node indices rather than pointers.
     * The dynamic tree is made available for advanced users that would like to use it to organize
     * spatial game data besides rigid bodies.
     * @{
     */
    /// The dynamic tree structure. This should be considered private data.
    /// It is placed here for performance reasons.
    public class B2DynamicTree
    {
        /// The tree nodes
        public B2TreeNode[] nodes;

        /// The root index
        public int root;

        /// The number of nodes
        public int nodeCount;

        /// The allocated node space
        public int nodeCapacity;

        /// Node free list
        public int freeList;

        /// Number of proxies created
        public int proxyCount;

        /// Leaf indices for rebuild
        public int[] leafIndices;

        /// Leaf bounding boxes for rebuild
        public B2AABB[] leafBoxes;

        /// Leaf bounding box centers for rebuild
        public B2Vec2[] leafCenters;

        /// Bins for sorting during rebuild
        public int[] binIndices;

        /// Allocated space for rebuilding
        public int rebuildCapacity;

        public void Clear()
        {
            nodes = null;
            root = 0;
            nodeCount = 0;
            nodeCapacity = 0;
            freeList = 0;
            proxyCount = 0;
            leafIndices = null;
            leafBoxes = null;
            leafCenters = null;
            binIndices = null;
            rebuildCapacity = 0;
        }
    }
}
