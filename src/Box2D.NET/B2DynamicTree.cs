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
        /// Array of nodes. The root is at index zero and index 1 is empty.
        // Otherwise siblings are paired at even indices. Has holes for free node pairs.
        public B2TreeNode[] nodes;

        /// Parent index per node. The free list is interweaved.
        public int[] parents;

        /// Proxy data split from node array as cold data.
        public B2TreeProxy[] proxies;

        /// Every allocated node has a lower index than this.
        public int nodeEnd;

        /// The allocated node space
        public int nodeCapacity;

        /// Free pairs below nodeEnd
        public int pairFreeList;

        /// Number of proxies created
        public int proxyCount;

        /// The allocated proxy space
        public int proxyCapacity;

        /// Proxy free list
        public int proxyFreeList;

        /// Array of nodes for rebuild.
        public B2TreeNode[] swapNodes;

        /// Leaf indices for rebuild
        public int[] leafIndices;

        /// Leaves for the rebuild. May represent a proxy or a retained subtree.
        public B2TreeNode[] leafNodes;

        /// Leaf bounding boxes for rebuild
        public B2AABB[] leafBoxes;

        /// Leaf bounding box centers for rebuild
        public B2Vec2[] leafCenters;

        /// Bins for sorting during rebuild
        public int[] binIndices;

        /// Allocated space for rebuilding
        public int rebuildCapacity;

        /// Rebuild orders the nodes so the children follow parents. Cache friendly for queries
        /// and refitting. The order can be disrupted by proxy creation.
        public bool dfsOrdered;

        public void Clear()
        {
            nodes = null;
            parents = null;
            proxies = null;
            nodeEnd = 0;
            nodeCapacity = 0;
            pairFreeList = 0;
            proxyCount = 0;
            proxyCapacity = 0;
            proxyFreeList = 0;
            swapNodes = null;
            leafIndices = null;
            leafNodes = null;
            leafBoxes = null;
            leafCenters = null;
            binIndices = null;
            rebuildCapacity = 0;
            dfsOrdered = false;
        }
    }
}
