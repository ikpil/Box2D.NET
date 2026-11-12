// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Buffers;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2AABBs;
using static Box2D.NET.B2Atomics;

namespace Box2D.NET
{
    public static class B2DynamicTrees
    {
        public const int B2_TREE_STACK_SIZE = 512;

        // Used to mark a node as being moved such that pairs need to be generated and the tree
        // may need to be rebuilt. This mark comes from a few sources:
        // - updating the transform and AABB in the solver from movement
        // - a joint disabling collision
        // - creating a proxy
        // - setting the transform on a body
        public const uint B2_MOVED_NODE = 1u << 30;
        public const uint B2_LEAF_NODE = 1u << 31;
        public const uint B2_NODE_INDEX_MASK = 0xFFFFFFFFu & ~(B2_MOVED_NODE | B2_LEAF_NODE);

        // Used to indicate empty nodes. It also passes as a leaf so that internal node
        // processing will naturally skip it.
        public const uint B2_EMPTY_NODE = B2_NODE_INDEX_MASK | B2_LEAF_NODE;

        public const int B2_ROOT_NODE = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2IsLeaf(in B2TreeNode node)
        {
            return (node.flagIndex & B2_LEAF_NODE) == B2_LEAF_NODE;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2IsNodeMoved(in B2TreeNode node)
        {
            return (node.flagIndex & B2_MOVED_NODE) == B2_MOVED_NODE;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2IsEmptyNode(in B2TreeNode node)
        {
            return node.flagIndex == B2_EMPTY_NODE;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int b2GetLeftChild(in B2TreeNode node)
        {
            return (int)(node.flagIndex & B2_NODE_INDEX_MASK);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int b2GetProxyId(in B2TreeNode node)
        {
            return (int)(node.flagIndex & B2_NODE_INDEX_MASK);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int b2GetRootPair(B2TreeNode[] nodes)
        {
            ref readonly B2TreeNode root = ref nodes[B2_ROOT_NODE];
            return b2IsLeaf(root) ? B2_ROOT_NODE : b2GetLeftChild(root);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2TreeNode b2MakeEmptyNode()
        {
            return new B2TreeNode
            {
                aabb = new B2AABB(
                    new B2Vec2(float.PositiveInfinity, float.PositiveInfinity),
                    new B2Vec2(float.NegativeInfinity, float.NegativeInfinity)
                ),
                flagIndex = B2_EMPTY_NODE,
                height = 0,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2HasTreeMoved(B2DynamicTree tree)
        {
            return b2IsNodeMoved(tree.nodes[B2_ROOT_NODE]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2NeedsRebuild(B2DynamicTree tree)
        {
            return b2IsNodeMoved(tree.nodes[B2_ROOT_NODE]) || tree.dfsOrdered == false;
        }

        // Scalar versions of the AABB SIMD helpers.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2OverlapNode(in B2AABB av, in B2TreeNode node)
        {
            ref readonly B2AABB bv = ref node.aabb;
            return av.lowerBound.X <= bv.upperBound.X && av.lowerBound.Y <= bv.upperBound.Y && bv.lowerBound.X <= av.upperBound.X &&
                   bv.lowerBound.Y <= av.upperBound.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2OverlapV(in B2AABB a, in B2AABB b)
        {
            return a.lowerBound.X <= b.upperBound.X && a.lowerBound.Y <= b.upperBound.Y && b.lowerBound.X <= a.upperBound.X &&
                   b.lowerBound.Y <= a.upperBound.Y;
        }

        // Bit (2 * i + j) is set when pairA[i] overlaps pairB[j]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int b2OverlapPairMask(B2TreeNode[] arrayA, int pairA, B2TreeNode[] arrayB, int pairB)
        {
            int mask = 0;
            mask |= b2OverlapV(arrayA[pairA + 0].aabb, arrayB[pairB + 0].aabb) ? 1 : 0;
            mask |= b2OverlapV(arrayA[pairA + 0].aabb, arrayB[pairB + 1].aabb) ? 2 : 0;
            mask |= b2OverlapV(arrayA[pairA + 1].aabb, arrayB[pairB + 0].aabb) ? 4 : 0;
            mask |= b2OverlapV(arrayA[pairA + 1].aabb, arrayB[pairB + 1].aabb) ? 8 : 0;
            return mask;
        }

        // Bit i is set when av overlaps nodes[pair + i]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int b2OverlapNodePairMask(in B2AABB av, B2TreeNode[] nodes, int pair)
        {
            int mask = 0;
            mask |= b2OverlapNode(av, nodes[pair + 0]) ? 1 : 0;
            mask |= b2OverlapNode(av, nodes[pair + 1]) ? 2 : 0;
            return mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2AABB b2UnionV(in B2AABB a, in B2AABB b)
        {
            return b2AABB_Union(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2AABB b2UnionPairV(B2TreeNode[] nodes, int pair)
        {
            return b2AABB_Union(nodes[pair].aabb, nodes[pair + 1].aabb);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void b2StoreAABBV(ref B2AABB aabb, in B2AABB value, bool condition)
        {
            if (condition)
            {
                aabb = value;
            }
        }

        // The free sibling pair list is chained through the parent index of their first node. A pair comes from the
        // free list when there is one and from the bump pointer otherwise. A rebuild empties the free list, so
        // allocations can just bump.
        private static int b2AllocateSiblingPair(B2DynamicTree tree)
        {
            // Allocate from free list.
            if (tree.pairFreeList != B2_NULL_INDEX)
            {
                int freePair = tree.pairFreeList;
                tree.pairFreeList = tree.parents[freePair];
                return freePair;
            }

            // Extend the bump allocator if needed.
            if (tree.nodeEnd + 2 > tree.nodeCapacity)
            {
                int oldCapacity = tree.nodeCapacity;
                int newCapacity = oldCapacity + (oldCapacity >> 1);
                newCapacity += newCapacity & 1;
                tree.nodes = b2GrowAllocZeroInit(tree.nodes, oldCapacity, newCapacity);
                tree.parents = b2GrowAllocZeroInit(tree.parents, oldCapacity, newCapacity);
                tree.nodeCapacity = newCapacity;

                // The spare has to match, the rebuild allocates it again.
                b2Free(tree.swapNodes, oldCapacity);
                tree.swapNodes = null;
            }

            int pair = tree.nodeEnd;
            tree.nodeEnd += 2;
            return pair;
        }

        private static void b2FreePair(B2DynamicTree tree, int pair)
        {
            B2_ASSERT((pair & 1) == 0 && 2 <= pair && pair < tree.nodeEnd);
            tree.nodes[pair] = b2MakeEmptyNode();
            tree.nodes[pair + 1] = b2MakeEmptyNode();
            tree.parents[pair] = tree.pairFreeList;
            tree.parents[pair + 1] = B2_NULL_INDEX;
            tree.pairFreeList = pair;
        }

        /// Constructing the tree initializes the node pool.
        public static B2DynamicTree b2CreateDynamicTree(int proxyCapacity = 16)
        {
            int capacity = b2MaxInt(proxyCapacity, 16);

            B2DynamicTree tree = new B2DynamicTree();
            tree.Clear();

            // A tree of n proxies has 2n - 1 nodes plus the empty node beside the root
            tree.nodeCapacity = 2 * capacity;
            tree.nodes = b2AllocZeroInit<B2TreeNode>(tree.nodeCapacity);
            tree.parents = b2AllocZeroInit<int>(tree.nodeCapacity);
            tree.pairFreeList = B2_NULL_INDEX;

            // The root and the empty node always exist
            tree.nodes[B2_ROOT_NODE] = b2MakeEmptyNode();
            tree.nodes[B2_ROOT_NODE + 1] = b2MakeEmptyNode();
            tree.parents[B2_ROOT_NODE] = B2_NULL_INDEX;
            tree.parents[B2_ROOT_NODE + 1] = B2_NULL_INDEX;

            // This is the bump index.
            tree.nodeEnd = 2;
            tree.dfsOrdered = true;

            tree.proxyCapacity = capacity;
            tree.proxyCount = 0;
            tree.proxies = b2AllocZeroInit<B2TreeProxy>(tree.proxyCapacity);

            // Build a linked list for the free list.
            for (int i = 0; i < tree.proxyCapacity - 1; ++i)
            {
                tree.proxies[i].node = B2_NULL_INDEX;
                tree.proxies[i].next = i + 1;
            }

            tree.proxies[tree.proxyCapacity - 1].node = B2_NULL_INDEX;
            tree.proxies[tree.proxyCapacity - 1].next = B2_NULL_INDEX;
            tree.proxyFreeList = 0;

            tree.leafIndices = null;
            tree.leafNodes = null;
            tree.leafBoxes = null;
            tree.leafCenters = null;
            tree.binIndices = null;
            tree.rebuildCapacity = 0;

            return tree;
        }

        /// Destroy the tree, freeing the node pool.
        public static void b2DestroyDynamicTree(B2DynamicTree tree)
        {
            b2Free(tree.nodes, tree.nodeCapacity);
            b2Free(tree.parents, tree.nodeCapacity);
            b2Free(tree.proxies, tree.proxyCapacity);
            b2Free(tree.swapNodes, tree.nodeCapacity);
            b2Free(tree.leafIndices, tree.rebuildCapacity);
            b2Free(tree.leafNodes, tree.rebuildCapacity);
            b2Free(tree.leafBoxes, tree.rebuildCapacity);
            b2Free(tree.leafCenters, tree.rebuildCapacity);
            b2Free(tree.binIndices, tree.rebuildCapacity);

            tree.Clear();
        }

        // Allocate a proxy from the pool. Grow the pool if necessary.
        private static int b2AllocateProxy(B2DynamicTree tree)
        {
            // Expand the pool as needed.
            if (tree.proxyFreeList == B2_NULL_INDEX)
            {
                B2_ASSERT(tree.proxyCount == tree.proxyCapacity);

                // The free list is empty. Rebuild a bigger pool.
                // A restored pool can have capacity 1, so guarantee strict growth.
                int oldCapacity = tree.proxyCapacity;
                tree.proxyCapacity += b2MaxInt(oldCapacity >> 1, 1);
                tree.proxies = b2GrowAllocZeroInit(tree.proxies, oldCapacity, tree.proxyCapacity);

                // Build a linked list for the free list.
                for (int i = oldCapacity; i < tree.proxyCapacity - 1; ++i)
                {
                    tree.proxies[i].node = B2_NULL_INDEX;
                    tree.proxies[i].next = i + 1;
                }

                tree.proxies[tree.proxyCapacity - 1].node = B2_NULL_INDEX;
                tree.proxies[tree.proxyCapacity - 1].next = B2_NULL_INDEX;
                tree.proxyFreeList = oldCapacity;
            }

            // Peel a proxy off the free list.
            int proxyIndex = tree.proxyFreeList;
            tree.proxyFreeList = tree.proxies[proxyIndex].next;
            tree.proxies[proxyIndex] = new B2TreeProxy();
            tree.proxies[proxyIndex].node = B2_NULL_INDEX;
            tree.proxies[proxyIndex].next = B2_NULL_INDEX;
            ++tree.proxyCount;
            return proxyIndex;
        }

        // Return a proxy to the pool.
        private static void b2FreeProxy(B2DynamicTree tree, int proxyId)
        {
            B2_ASSERT(0 <= proxyId && proxyId < tree.proxyCapacity);
            B2_ASSERT(0 < tree.proxyCount);
            tree.proxies[proxyId].node = B2_NULL_INDEX;
            tree.proxies[proxyId].next = tree.proxyFreeList;
            tree.proxyFreeList = proxyId;
            --tree.proxyCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int b2GetNodeHeight(in B2TreeNode node)
        {
            return b2IsLeaf(node) ? 0 : node.height;
        }

        // The internal node above a children pair
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static B2TreeNode b2MakeInternalNode(B2TreeNode[] nodes, int pair)
        {
            ref readonly B2TreeNode c1 = ref nodes[pair];
            ref readonly B2TreeNode c2 = ref nodes[pair + 1];

            B2TreeNode node = new B2TreeNode();
            node.aabb = b2AABB_Union(c1.aabb, c2.aabb);
            node.flagIndex = (uint)pair | ((c1.flagIndex | c2.flagIndex) & B2_MOVED_NODE);
            node.height = 1 + b2MaxInt(b2GetNodeHeight(c1), b2GetNodeHeight(c2));
            return node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static B2TreeNode b2MakeLeafNode(in B2AABB aabb, int proxyId, ulong userData, bool moved)
        {
            B2TreeNode node = new B2TreeNode();
            node.aabb = aabb;
            node.flagIndex = (uint)proxyId | B2_LEAF_NODE | (moved ? B2_MOVED_NODE : 0);
            node.shapeIndex = (int)(uint)userData;
            return node;
        }

        // A node landed at a new index, so tell what hangs below it
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void b2LinkChildren(B2DynamicTree tree, int nodeIndex)
        {
            ref readonly B2TreeNode node = ref tree.nodes[nodeIndex];
            if (b2IsLeaf(node))
            {
                tree.proxies[b2GetProxyId(node)].node = nodeIndex;
            }
            else
            {
                int pair = b2GetLeftChild(node);
                tree.parents[pair] = nodeIndex;
                tree.parents[pair + 1] = nodeIndex;
            }
        }

        // The sweep refit visits indices from high to low, so it needs every child above its parent
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool b2IsNodeOrdered(B2TreeNode[] nodes, int nodeIndex)
        {
            ref readonly B2TreeNode node = ref nodes[nodeIndex];
            return b2IsLeaf(node) || nodeIndex < b2GetLeftChild(node);
        }

        // Greedy algorithm for sibling selection using the SAH
        // We have three nodes A-(B,C) and want to add a leaf D, there are three choices.
        // 1: make a new parent for A and D : E-(A-(B,C), D)
        // 2: associate D with B
        //   a: B is a leaf : A-(E-(B,D), C)
        //   b: B is an internal node: A-(B{D},C)
        // 3: associate D with C
        //   a: C is a leaf : A-(B, E-(C,D))
        //   b: C is an internal node: A-(B, C{D})
        // All of these have a clear cost except when B or C is an internal node. Hence we need to be greedy.

        // The cost for cases 1, 2a, and 3a can be computed using the sibling cost formula.
        // cost of sibling H = area(union(H, D)) + increased area of ancestors

        // Suppose B (or C) is an internal node, then the lowest cost would be one of two cases:
        // case1: D becomes a sibling of B
        // case2: D becomes a descendant of B along with a new internal node of area(D).
        private static int b2FindBestSibling(B2DynamicTree tree, in B2AABB boxD)
        {
            B2Vec2 centerD = b2AABB_Center(boxD);
            float areaD = b2Perimeter(boxD);

            B2TreeNode[] nodes = tree.nodes;
            int nodeIndex = B2_ROOT_NODE;

            if (b2IsLeaf(nodes[nodeIndex]))
            {
                return nodeIndex;
            }

            B2AABB rootBox = nodes[nodeIndex].aabb;

            // Area of current node
            float areaBase = b2Perimeter(rootBox);

            // Area of inflated node
            float directCost = b2Perimeter(b2AABB_Union(rootBox, boxD));
            float inheritedCost = 0.0f;

            int bestSibling = nodeIndex;
            float bestCost = directCost;

            // Descend the tree, following a single greedy path.
            for (;;)
            {
                int child1 = b2GetLeftChild(nodes[nodeIndex]);
                int child2 = child1 + 1;

                // Cost of creating a new parent for this node and the new leaf
                float cost = directCost + inheritedCost;

                // Sometimes there are multiple identical costs within tolerance.
                // This breaks the ties using the centroid distance.
                if (cost < bestCost)
                {
                    bestSibling = nodeIndex;
                    bestCost = cost;
                }

                // Inheritance cost seen by children
                inheritedCost += directCost - areaBase;

                bool leaf1 = b2IsLeaf(nodes[child1]);
                bool leaf2 = b2IsLeaf(nodes[child2]);

                // Cost of descending into child 1
                float lowerCost1 = float.MaxValue;
                B2AABB box1 = nodes[child1].aabb;
                float directCost1 = b2Perimeter(b2AABB_Union(box1, boxD));
                float area1 = 0.0f;
                if (leaf1)
                {
                    // Child 1 is a leaf
                    // Cost of creating new node and increasing area of node P
                    float cost1 = directCost1 + inheritedCost;
                    if (cost1 < bestCost)
                    {
                        bestSibling = child1;
                        bestCost = cost1;
                    }
                }
                else
                {
                    // Child 1 is an internal node
                    area1 = b2Perimeter(box1);

                    // Lower bound cost of inserting under child 1. The minimum accounts for two possibilities:
                    // 1. Child1 could be the sibling with cost1 = inheritedCost + directCost1
                    // 2. A descendant of child1 could be the sibling with the lower bound cost of
                    //       cost1 = inheritedCost + (directCost1 - area1) + areaD
                    // This minimum here leads to the minimum of these two costs.
                    lowerCost1 = inheritedCost + directCost1 + b2MinFloat(areaD - area1, 0.0f);
                }

                // Cost of descending into child 2
                float lowerCost2 = float.MaxValue;
                B2AABB box2 = nodes[child2].aabb;
                float directCost2 = b2Perimeter(b2AABB_Union(box2, boxD));
                float area2 = 0.0f;
                if (leaf2)
                {
                    float cost2 = directCost2 + inheritedCost;
                    if (cost2 < bestCost)
                    {
                        bestSibling = child2;
                        bestCost = cost2;
                    }
                }
                else
                {
                    area2 = b2Perimeter(box2);
                    lowerCost2 = inheritedCost + directCost2 + b2MinFloat(areaD - area2, 0.0f);
                }

                if (leaf1 && leaf2)
                {
                    break;
                }

                // Can the cost possibly be decreased?
                if (bestCost <= lowerCost1 && bestCost <= lowerCost2)
                {
                    break;
                }

                if (lowerCost1 == lowerCost2 && leaf1 == false)
                {
                    B2_ASSERT(lowerCost1 < float.MaxValue);
                    B2_ASSERT(lowerCost2 < float.MaxValue);

                    // No clear choice based on lower bound surface area. This can happen when both
                    // children fully contain D. Fall back to node distance.
                    B2Vec2 d1 = b2Sub(b2AABB_Center(box1), centerD);
                    B2Vec2 d2 = b2Sub(b2AABB_Center(box2), centerD);
                    lowerCost1 = b2LengthSquared(d1);
                    lowerCost2 = b2LengthSquared(d2);
                }

                // Descend
                if (lowerCost1 < lowerCost2 && leaf1 == false)
                {
                    nodeIndex = child1;
                    areaBase = area1;
                    directCost = directCost1;
                }
                else
                {
                    nodeIndex = child2;
                    areaBase = area2;
                    directCost = directCost2;
                }
            }

            return bestSibling;
        }

        // Swap a child of A with a grandchild under the other child.
        // Example swapping B with G:
        // (A (B (D E) C (F G)) -> (A (G) C (F B (D E))
        private static void b2SwapNodes(B2DynamicTree tree, int iDown, int iUp)
        {
            B2TreeNode[] nodes = tree.nodes;
            (nodes[iDown], nodes[iUp]) = (nodes[iUp], nodes[iDown]);
            b2LinkChildren(tree, iDown);
            b2LinkChildren(tree, iUp);

            if (b2IsNodeOrdered(nodes, iDown) == false || b2IsNodeOrdered(nodes, iUp) == false)
            {
                tree.dfsOrdered = false;
            }

            // The sibling of the node that went down holds a different subtree now
            int iC = iDown ^ 1;
            nodes[iC] = b2MakeInternalNode(nodes, b2GetLeftChild(nodes[iC]));
        }

        // Perform a left or right rotation if node A is imbalanced.
        // Tree: (A (B (D E) C (F G))
        private static void b2RotateNodes(B2DynamicTree tree, int iA)
        {
            B2TreeNode[] nodes = tree.nodes;
            ref readonly B2TreeNode A = ref nodes[iA];
            B2_ASSERT(b2IsLeaf(A) == false);

            int iB = b2GetLeftChild(A);
            int iC = iB + 1;
            ref readonly B2TreeNode B = ref nodes[iB];
            ref readonly B2TreeNode C = ref nodes[iC];

            bool leafB = b2IsLeaf(B);
            bool leafC = b2IsLeaf(C);
            if (leafB && leafC)
            {
                return;
            }

            int bestDown = B2_NULL_INDEX;
            int bestUp = B2_NULL_INDEX;
            float bestDelta = 0.0f;

            if (leafC == false)
            {
                // Swap B with F or G
                int iF = b2GetLeftChild(C);
                int iG = iF + 1;
                float areaC = b2Perimeter(C.aabb);

                // B <-> F then C (B G)
                float deltaBF = b2Perimeter(b2UnionV(B.aabb, nodes[iG].aabb)) - areaC;
                if (deltaBF < bestDelta)
                {
                    bestDown = iB;
                    bestUp = iF;
                    bestDelta = deltaBF;
                }

                // B <-> G then C (F B)
                float deltaBG = b2Perimeter(b2UnionV(B.aabb, nodes[iF].aabb)) - areaC;
                if (deltaBG < bestDelta)
                {
                    bestDown = iB;
                    bestUp = iG;
                    bestDelta = deltaBG;
                }
            }

            if (leafB == false)
            {
                // Swap C with D or E
                int iD = b2GetLeftChild(B);
                int iE = iD + 1;
                float areaB = b2Perimeter(B.aabb);

                // C <-> D then B (C E)
                float deltaCD = b2Perimeter(b2UnionV(C.aabb, nodes[iE].aabb)) - areaB;
                if (deltaCD < bestDelta)
                {
                    bestDown = iC;
                    bestUp = iD;
                    bestDelta = deltaCD;
                }

                // C <-> E then B (D C)
                float deltaCE = b2Perimeter(b2UnionV(C.aabb, nodes[iD].aabb)) - areaB;
                if (deltaCE < bestDelta)
                {
                    bestDown = iC;
                    bestUp = iE;
                    bestDelta = deltaCE;
                }
            }

            if (bestDown != B2_NULL_INDEX)
            {
                b2SwapNodes(tree, bestDown, bestUp);
            }
        }

        private static void b2InsertLeaf(B2DynamicTree tree, in B2AABB aabb, int proxyId, bool moved, bool shouldRotate)
        {
            B2TreeNode leaf = b2MakeLeafNode(aabb, proxyId, tree.proxies[proxyId].userData, moved);

            if (b2IsEmptyNode(tree.nodes[B2_ROOT_NODE]))
            {
                tree.nodes[B2_ROOT_NODE] = leaf;
                tree.proxies[proxyId].node = B2_ROOT_NODE;
                return;
            }

            int sibling = b2FindBestSibling(tree, aabb);

            // The sibling's position becomes the new parent. The sibling moves down into a new pair
            // beside the leaf, which puts it below its own children when it is internal.
            int pair = b2AllocateSiblingPair(tree);
            B2TreeNode[] nodes = tree.nodes;
            int[] parents = tree.parents;

            nodes[pair] = nodes[sibling];
            nodes[pair + 1] = leaf;
            parents[pair] = sibling;
            parents[pair + 1] = sibling;
            b2LinkChildren(tree, pair);
            tree.proxies[proxyId].node = pair + 1;

            nodes[sibling] = b2MakeInternalNode(nodes, pair);

            if (b2IsNodeOrdered(nodes, sibling) == false || b2IsNodeOrdered(nodes, pair) == false)
            {
                tree.dfsOrdered = false;
            }

            // Walk back up the tree refitting ancestors, the root included.
            int index = sibling;
            while (index != B2_NULL_INDEX)
            {
                if (shouldRotate)
                {
                    b2RotateNodes(tree, index);
                }

                nodes[index] = b2MakeInternalNode(nodes, b2GetLeftChild(nodes[index]));
                index = parents[index];
            }
        }

        private static void b2RemoveLeaf(B2DynamicTree tree, int proxyId)
        {
            B2TreeNode[] nodes = tree.nodes;
            int[] parents = tree.parents;

            int leaf = tree.proxies[proxyId].node;
            B2_ASSERT(0 <= leaf && leaf < tree.nodeEnd);
            B2_ASSERT(b2IsLeaf(nodes[leaf]) && b2GetProxyId(nodes[leaf]) == proxyId);

            if (leaf == B2_ROOT_NODE)
            {
                nodes[B2_ROOT_NODE] = b2MakeEmptyNode();
                return;
            }

            // The sibling takes the parent's position and the pair is freed. This keeps the order,
            // the sibling only moves down.
            int parent = parents[leaf];
            nodes[parent] = nodes[leaf ^ 1];
            b2LinkChildren(tree, parent);
            b2FreePair(tree, leaf & ~1);

            // Update ancestors.
            int index = parents[parent];
            while (index != B2_NULL_INDEX)
            {
                nodes[index] = b2MakeInternalNode(nodes, b2GetLeftChild(nodes[index]));
                index = parents[index];
            }
        }

        // Create a proxy in the tree as a leaf node. We return the index of the node instead of a pointer so that we can grow
        // the node pool.
        public static int b2CreateTreeProxyInternal(B2DynamicTree tree, in B2AABB aabb, ulong categoryBits, ulong userData, bool markMoved)
        {
            B2_VALIDATE(b2IsValidAABB(aabb));

            int proxyId = b2AllocateProxy(tree);

            tree.proxies[proxyId].categoryBits = categoryBits;
            tree.proxies[proxyId].userData = userData;

            bool shouldRotate = true;
            b2InsertLeaf(tree, aabb, proxyId, markMoved, shouldRotate);

            return proxyId;
        }

        /// Create a proxy. Provide an AABB and a userData value.
        public static int b2CreateTreeProxy(B2DynamicTree tree, in B2AABB aabb, ulong categoryBits, ulong userData)
        {
            return b2CreateTreeProxyInternal(tree, aabb, categoryBits, userData, false);
        }

        /// Destroy a proxy. This asserts if the id is invalid.
        public static void b2DestroyTreeProxy(B2DynamicTree tree, int proxyId)
        {
            B2_ASSERT(0 <= proxyId && proxyId < tree.proxyCapacity);

            b2RemoveLeaf(tree, proxyId);
            b2FreeProxy(tree, proxyId);
        }

        /// Get the number of proxies created
        public static int b2DynamicTree_GetProxyCount(B2DynamicTree tree)
        {
            return tree.proxyCount;
        }

        public static void b2DynamicTree_MoveProxyInternal(B2DynamicTree tree, int proxyId, in B2AABB aabb, bool markMoved)
        {
            B2_VALIDATE(b2IsValidAABB(aabb));
            B2_VALIDATE(aabb.upperBound.X - aabb.lowerBound.X < B2_HUGE);
            B2_VALIDATE(aabb.upperBound.Y - aabb.lowerBound.Y < B2_HUGE);
            B2_ASSERT(0 <= proxyId && proxyId < tree.proxyCapacity);

            b2RemoveLeaf(tree, proxyId);

            bool shouldRotate = false;
            b2InsertLeaf(tree, aabb, proxyId, markMoved, shouldRotate);
        }

        /// Move a proxy to a new AABB by removing and reinserting into the tree.
        public static void b2DynamicTree_MoveProxy(B2DynamicTree tree, int proxyId, in B2AABB aabb)
        {
            b2DynamicTree_MoveProxyInternal(tree, proxyId, aabb, false);
        }

        public static void b2DynamicTree_EnlargeProxy(B2DynamicTree tree, int proxyId, in B2AABB aabb)
        {
            B2_VALIDATE(b2IsValidAABB(aabb));
            B2_VALIDATE(aabb.upperBound.X - aabb.lowerBound.X < B2_HUGE);
            B2_VALIDATE(aabb.upperBound.Y - aabb.lowerBound.Y < B2_HUGE);
            B2_ASSERT(0 <= proxyId && proxyId < tree.proxyCapacity);

            B2TreeNode[] nodes = tree.nodes;
            int[] parents = tree.parents;

            int index = tree.proxies[proxyId].node;
            B2_ASSERT(b2IsLeaf(nodes[index]));

            // Caller must ensure this
            B2_VALIDATE(b2AABB_Contains(nodes[index].aabb, aabb) == false);

            nodes[index].aabb = aabb;
            nodes[index].flagIndex |= B2_MOVED_NODE;

            index = parents[index];
            while (index != B2_NULL_INDEX)
            {
                bool changed = b2EnlargeAABB(ref nodes[index].aabb, aabb);

                // This is marked to ensure the root is marked in this loop or the one below.
                nodes[index].flagIndex |= B2_MOVED_NODE;

                index = parents[index];

                if (changed == false)
                {
                    break;
                }
            }

            // Mark all the way up to the root.
            while (index != B2_NULL_INDEX)
            {
                if ((nodes[index].flagIndex & B2_MOVED_NODE) != 0)
                {
                    // Early out because this ancestor was previously ascended and marked as moved.
                    break;
                }

                nodes[index].flagIndex |= B2_MOVED_NODE;
                index = parents[index];
            }
        }

        /// Modify the category bits on a proxy.
        public static void b2DynamicTree_SetCategoryBits(B2DynamicTree tree, int proxyId, ulong categoryBits)
        {
            B2_ASSERT(0 <= proxyId && proxyId < tree.proxyCapacity);
            tree.proxies[proxyId].categoryBits = categoryBits;
        }

        /// Get the category bits on a proxy.
        public static ulong b2DynamicTree_GetCategoryBits(B2DynamicTree tree, int proxyId)
        {
            B2_ASSERT(0 <= proxyId && proxyId < tree.proxyCapacity);
            return tree.proxies[proxyId].categoryBits;
        }

        /// Get the height of the binary tree.
        public static int b2DynamicTree_GetHeight(B2DynamicTree tree)
        {
            return b2GetNodeHeight(tree.nodes[B2_ROOT_NODE]);
        }

        /// Get the ratio of the sum of the internal node areas to the root area.
        // The area ratio is the thing that SAH seeks to minimize. SAH
        // cannot do anything about leaf boxes or the root box. It seeks
        // to minimize the area of all non-root internal nodes. Divide this
        // by the root area to make the metric non-dimensional.
        // So this becomes a meaningful measure of tree quality.
        public static float b2DynamicTree_GetAreaRatio(B2DynamicTree tree)
        {
            if (tree.proxyCount == 0)
            {
                return 0.0f;
            }

            B2TreeNode[] nodes = tree.nodes;
            float rootArea = b2Perimeter(nodes[B2_ROOT_NODE].aabb);
            if (rootArea <= 0.0f)
            {
                return 0.0f;
            }

            // Free nodes and the empty node are leaf tagged.
            float internalArea = 0.0f;
            int nodeEnd = tree.nodeEnd;
            for (int i = 2; i < nodeEnd; ++i)
            {
                if (b2IsLeaf(nodes[i]) == false)
                {
                    internalArea += b2Perimeter(nodes[i].aabb);
                }
            }

            return internalArea / rootArea;
        }

        /// Get the bounding box that contains the entire tree
        public static B2AABB b2DynamicTree_GetRootBounds(B2DynamicTree tree)
        {
            if (tree.proxyCount == 0)
            {
                return new B2AABB(b2Vec2_zero, b2Vec2_zero);
            }

            return tree.nodes[B2_ROOT_NODE].aabb;
        }

#if DEBUG
        private static int b2ValidateSubtree(B2DynamicTree tree, int nodeIndex, ref int leafCount)
        {
            B2_ASSERT(0 <= nodeIndex && nodeIndex < tree.nodeEnd);
            ref readonly B2TreeNode node = ref tree.nodes[nodeIndex];
            B2_ASSERT(b2IsEmptyNode(node) == false);

            if (b2IsLeaf(node))
            {
                int proxyId = b2GetProxyId(node);
                B2_ASSERT(0 <= proxyId && proxyId < tree.proxyCapacity);
                B2_ASSERT(tree.proxies[proxyId].node == nodeIndex);
                B2_ASSERT((int)tree.proxies[proxyId].userData == node.shapeIndex);
                leafCount += 1;
                return 0;
            }

            int pair = b2GetLeftChild(node);
            B2_ASSERT((pair & 1) == 0 && 2 <= pair && pair < tree.nodeEnd);
            B2_ASSERT(tree.parents[pair] == nodeIndex);
            B2_ASSERT(tree.parents[pair + 1] == nodeIndex);
            B2_ASSERT(tree.dfsOrdered == false || nodeIndex < pair);

            ref readonly B2TreeNode c1 = ref tree.nodes[pair];
            ref readonly B2TreeNode c2 = ref tree.nodes[pair + 1];
            B2_ASSERT(b2AABB_Contains(node.aabb, c1.aabb));
            B2_ASSERT(b2AABB_Contains(node.aabb, c2.aabb));
            B2_ASSERT(b2IsNodeMoved(node) == (b2IsNodeMoved(c1) || b2IsNodeMoved(c2)));

            // A bad tree can stack overflow, but that is validation on its own.
            int height1 = b2ValidateSubtree(tree, pair, ref leafCount);
            int height2 = b2ValidateSubtree(tree, pair + 1, ref leafCount);
            int height = 1 + b2MaxInt(height1, height2);
            B2_ASSERT(node.height == height);
            return height;
        }
#endif

        /// Validate the tree.
        public static void b2DynamicTree_Validate(B2DynamicTree tree)
        {
#if DEBUG
            B2_ASSERT(2 <= tree.nodeEnd && tree.nodeEnd <= tree.nodeCapacity);
            B2_ASSERT((tree.nodeEnd & 1) == 0);
            B2_ASSERT(tree.parents[B2_ROOT_NODE] == B2_NULL_INDEX);
            B2_ASSERT(b2IsEmptyNode(tree.nodes[B2_ROOT_NODE + 1]));

            int freePairCount = 0;
            int pair = tree.pairFreeList;
            while (pair != B2_NULL_INDEX)
            {
                B2_ASSERT((pair & 1) == 0 && 2 <= pair && pair < tree.nodeEnd);
                B2_ASSERT(b2IsEmptyNode(tree.nodes[pair]));
                B2_ASSERT(b2IsEmptyNode(tree.nodes[pair + 1]));
                pair = tree.parents[pair];
                ++freePairCount;
                B2_ASSERT(2 * freePairCount < tree.nodeEnd);
            }

            // Validate proxy free list.
            int freeProxyCount = 0;
            int freeIndex = tree.proxyFreeList;
            while (freeIndex != B2_NULL_INDEX)
            {
                B2_ASSERT(0 <= freeIndex && freeIndex < tree.proxyCapacity);
                B2_ASSERT(tree.proxies[freeIndex].node == B2_NULL_INDEX);
                freeIndex = tree.proxies[freeIndex].next;
                ++freeProxyCount;
            }

            B2_ASSERT(tree.proxyCount + freeProxyCount == tree.proxyCapacity);

            B2_ASSERT(tree.nodeEnd == 2 * b2MaxInt(tree.proxyCount, 1) + 2 * freePairCount);

            if (tree.proxyCount == 0)
            {
                B2_ASSERT(b2IsEmptyNode(tree.nodes[B2_ROOT_NODE]));
                return;
            }

            int leafCount = 0;
            b2ValidateSubtree(tree, B2_ROOT_NODE, ref leafCount);
            B2_ASSERT(leafCount == tree.proxyCount);
#else
            B2_UNUSED(tree);
#endif
        }

        public static void b2DynamicTree_ValidateNoMoved(B2DynamicTree tree)
        {
#if DEBUG
            B2TreeNode[] nodes = tree.nodes;
            int nodeEnd = tree.nodeEnd;
            for (int i = 0; i < nodeEnd; ++i)
            {
                B2_ASSERT(b2IsNodeMoved(nodes[i]) == false);
            }
#else
            B2_UNUSED(tree);
#endif
        }

        /// Get the number of bytes used by this tree
        public static int b2DynamicTree_GetByteCount(B2DynamicTree tree)
        {
            // Upstream-compatible logical payload size, not a CLR heap-size estimate.
            int treeHeaderSize = IntPtr.Size == 8 ? 120 : 80;
            int size = treeHeaderSize;
            size += tree.nodeCapacity * B2SizeOf<B2TreeNode>.Size;
            size += tree.nodeCapacity * B2SizeOf<int>.Size;
            size += tree.proxyCapacity * B2SizeOf<B2TreeProxy>.Size;
            size += tree.swapNodes == null ? 0 : tree.nodeCapacity * B2SizeOf<B2TreeNode>.Size;

            // leafIndices
            size += tree.rebuildCapacity * B2SizeOf<int>.Size;
            // leafNodes
            size += tree.rebuildCapacity * B2SizeOf<B2TreeNode>.Size;
            // leafBoxes
            size += tree.rebuildCapacity * B2SizeOf<B2AABB>.Size;
            // leafCenters
            size += tree.rebuildCapacity * B2SizeOf<B2Vec2>.Size;
            // binIndices
            size += tree.rebuildCapacity * B2SizeOf<int>.Size;

            return size;
        }

        /// Get proxy user data
        public static ulong b2DynamicTree_GetUserData(B2DynamicTree tree, int proxyId)
        {
            B2_ASSERT(0 <= proxyId && proxyId < tree.proxyCapacity);
            return tree.proxies[proxyId].userData;
        }

        /// Get the AABB of a proxy
        public static B2AABB b2DynamicTree_GetProxyBounds(B2DynamicTree tree, int proxyId)
        {
            B2_ASSERT(0 <= proxyId && proxyId < tree.proxyCapacity);
            int nodeIndex = tree.proxies[proxyId].node;
            B2_ASSERT(0 <= nodeIndex && nodeIndex < tree.nodeEnd);
            return tree.nodes[nodeIndex].aabb;
        }

        /// Query an AABB for overlapping proxies. The callback class is called for each proxy that overlaps the supplied AABB.
        ///	@return performance data
        public static B2TreeStats b2DynamicTree_OverlapAABB<T>(B2DynamicTree tree, in B2AABB aabb, ulong maskBits, b2TreeQueryCallbackFcn<T> callback, ref T context)
        {
            B2TreeStats result = new B2TreeStats();

            if (tree.proxyCount == 0)
            {
                return result;
            }

            B2TreeNode[] nodes = tree.nodes;

            Span<int> stack = stackalloc int[B2_TREE_STACK_SIZE];
            int stackCount = 0;
            stack[stackCount++] = b2GetRootPair(nodes);

            B2AABB boxv = aabb;

            while (stackCount > 0)
            {
                int pair = stack[--stackCount];
                result.nodeVisits += 1;

                for (int i = 0; i < 2; ++i)
                {
                    ref readonly B2TreeNode node = ref nodes[pair + i];
                    if (b2OverlapNode(boxv, node))
                    {
                        if (b2IsLeaf(node))
                        {
                            // callback to user code with proxy id
                            int proxyId = b2GetProxyId(node);
                            ref readonly B2TreeProxy proxy = ref tree.proxies[proxyId];
                            if ((proxy.categoryBits & maskBits) != 0)
                            {
                                bool proceed = callback(proxyId, proxy.userData, ref context);
                                result.leafVisits += 1;

                                if (proceed == false)
                                {
                                    return result;
                                }
                            }
                        }
                        else
                        {
                            if (stackCount < B2_TREE_STACK_SIZE - 1)
                            {
                                stack[stackCount++] = b2GetLeftChild(node);
                            }
                            else
                            {
                                B2_ASSERT(stackCount < B2_TREE_STACK_SIZE - 1);
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// Query an AABB for overlapping proxies. The callback class is called for each proxy that overlaps the supplied AABB.
        /// No filtering is performed.
        ///	@return performance data
        // This ignores the category bits. Useful for debug draw.
        public static B2TreeStats b2DynamicTree_OverlapAABBAll<T>(B2DynamicTree tree, in B2AABB aabb, b2TreeQueryCallbackFcn<T> callback, ref T context)
        {
            B2TreeStats result = new B2TreeStats();

            if (tree.proxyCount == 0)
            {
                return result;
            }

            B2TreeNode[] nodes = tree.nodes;

            Span<int> stack = stackalloc int[B2_TREE_STACK_SIZE];
            int stackCount = 0;
            stack[stackCount++] = b2GetRootPair(nodes);

            B2AABB boxv = aabb;

            while (stackCount > 0)
            {
                int pair = stack[--stackCount];
                result.nodeVisits += 1;

                for (int i = 0; i < 2; ++i)
                {
                    ref readonly B2TreeNode node = ref nodes[pair + i];
                    if (b2OverlapNode(boxv, node))
                    {
                        if (b2IsLeaf(node))
                        {
                            // callback to user code with proxy id
                            int proxyId = b2GetProxyId(node);
                            ref readonly B2TreeProxy proxy = ref tree.proxies[proxyId];
                            bool proceed = callback(proxyId, proxy.userData, ref context);
                            result.leafVisits += 1;

                            if (proceed == false)
                            {
                                return result;
                            }
                        }
                        else
                        {
                            if (stackCount < B2_TREE_STACK_SIZE - 1)
                            {
                                stack[stackCount++] = b2GetLeftChild(node);
                            }
                            else
                            {
                                B2_ASSERT(stackCount < B2_TREE_STACK_SIZE - 1);
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// Ray cast against the proxies in the tree. This relies on the callback
        /// to perform a exact ray cast in the case were the proxy contains a shape.
        /// The callback also performs the any collision filtering. This has performance
        /// roughly equal to k * log(n), where k is the number of collisions and n is the
        /// number of proxies in the tree.
        /// Bit-wise filtering using mask bits can greatly improve performance in some scenarios.
        ///	However, this filtering may be approximate, so the user should still apply filtering to results.
        /// @param tree the dynamic tree to ray cast
        /// @param input the ray cast input data. The ray extends from p1 to p1 + maxFraction * (p2 - p1)
        /// @param maskBits mask bit hint: `bool accept = (maskBits & node->categoryBits) != 0;`
        /// @param callback a callback class that is called for each proxy that is hit by the ray
        /// @param context user context that is passed to the callback
        ///	@return performance data
        // A lot of optimization work went into this. It beats the slab test by a significant margin.
        // It is faster than having category bits in the nodes because of the cache line friendly node
        // size (64 bytes).
        public static B2TreeStats b2DynamicTree_CastRay<T>(B2DynamicTree tree, in B2RayCastInput input, ulong maskBits, b2TreeRayCastCallbackFcn<T> callback, ref T context)
        {
            B2TreeStats result = new B2TreeStats();

            if (tree.proxyCount == 0)
            {
                return result;
            }

            B2Vec2 p1 = input.origin;
            B2Vec2 d = input.translation;

            B2Vec2 r = b2Normalize(d);

            // v is perpendicular to the segment.
            B2Vec2 v = b2CrossSV(1.0f, r);
            B2Vec2 abs_v = b2Abs(v);

            // Separating axis for segment (Gino, p80).
            // |dot(v, p1 - c)| > dot(|v|, h)
            float maxFraction = input.maxFraction;

            B2Vec2 p2 = b2MulAdd(p1, maxFraction, d);

            // Build a bounding box for the segment.
            B2AABB segmentAABB = new B2AABB(b2Min(p1, p2), b2Max(p1, p2));

            B2AABB boxv = segmentAABB;

            B2TreeNode[] nodes = tree.nodes;

            Span<int> stack = stackalloc int[B2_TREE_STACK_SIZE];
            int stackCount = 0;
            stack[stackCount++] = b2GetRootPair(nodes);

            B2RayCastInput subInput = input;

            Span<int> hit = stackalloc int[2];
            Span<bool> isLeaf = stackalloc bool[2];

            while (stackCount > 0)
            {
                int pair = stack[--stackCount];
                result.nodeVisits += 1;

                int hitCount = 0;
                for (int i = 0; i < 2; ++i)
                {
                    ref readonly B2TreeNode node = ref nodes[pair + i];

                    if (b2OverlapNode(boxv, node) == false)
                    {
                        continue;
                    }

                    // Separating axis for segment (Gino, p80).
                    // |dot(v, p1 - c)| > dot(|v|, h)
                    B2AABB nodeAABB = node.aabb;
                    B2Vec2 c = b2AABB_Center(nodeAABB);
                    B2Vec2 h = b2AABB_Extents(nodeAABB);
                    float term1 = b2AbsFloat(b2Dot(v, b2Sub(p1, c)));
                    float term2 = b2Dot(abs_v, h);
                    if (term2 < term1)
                    {
                        continue;
                    }

                    isLeaf[hitCount] = b2IsLeaf(node);
                    hit[hitCount] = pair + i;
                    hitCount += 1;
                }

                if (hitCount == 2 && isLeaf[0] == false && isLeaf[1] == false)
                {
                    B2Vec2 center1 = b2AABB_Center(nodes[hit[0]].aabb);
                    B2Vec2 center2 = b2AABB_Center(nodes[hit[1]].aabb);
                    float d1 = b2DistanceSquared(center1, p1);
                    float d2 = b2DistanceSquared(center2, p1);

                    // Want to push the closest one last. Both have the same isLeaf, so they don't swap.
                    if (d1 < d2)
                    {
                        (hit[0], hit[1]) = (hit[1], hit[0]);
                    }
                }

                for (int i = 0; i < hitCount; ++i)
                {
                    if (isLeaf[i])
                    {
                        int proxyId = b2GetProxyId(nodes[hit[i]]);
                        ref readonly B2TreeProxy proxy = ref tree.proxies[proxyId];

                        if ((proxy.categoryBits & maskBits) == 0)
                        {
                            continue;
                        }

                        subInput.maxFraction = maxFraction;

                        float value = callback(subInput, proxyId, proxy.userData, ref context);
                        result.leafVisits += 1;

                        // The user may return -1 to indicate this shape should be skipped

                        if (value == 0.0f)
                        {
                            // The client has terminated the ray cast.
                            return result;
                        }

                        if (0.0f < value && value <= maxFraction)
                        {
                            // Update segment bounding box.
                            maxFraction = value;
                            p2 = b2MulAdd(p1, maxFraction, d);
                            segmentAABB.lowerBound = b2Min(p1, p2);
                            segmentAABB.upperBound = b2Max(p1, p2);

                            boxv = segmentAABB;
                        }
                    }
                    else
                    {
                        if (stackCount < B2_TREE_STACK_SIZE - 1)
                        {
                            stack[stackCount++] = b2GetLeftChild(nodes[hit[i]]);
                        }
                        else
                        {
                            B2_ASSERT(stackCount < B2_TREE_STACK_SIZE - 1);
                        }
                    }
                }
            }

            return result;
        }

        /// Cast a box through the tree. This relies on the callback
        /// to perform an exact cast in the case were the proxy contains a shape.
        /// The callback also performs the any collision filtering. This has performance
        /// roughly equal to k * log(n), where k is the number of collisions and n is the
        /// number of proxies in the tree.
        /// @param tree the dynamic tree to ray cast
        /// @param input the box cast input data
        /// @param maskBits filter bits: `bool accept = (maskBits & node->categoryBits) != 0;`
        /// @param callback a callback that is called for each proxy the swept box may hit
        /// @param context user context that is passed to the callback
        ///	@return performance data
        // Follows structure of ray cast with small tweaks to handle a swept box.
        public static B2TreeStats b2DynamicTree_CastBox<T>(B2DynamicTree tree, in B2BoxCastInput input, ulong maskBits, b2TreeBoxCastCallbackFcn<T> callback, ref T context)
        {
            B2TreeStats result = new B2TreeStats();

            if (tree.proxyCount == 0)
            {
                return result;
            }

            B2AABB originAABB = input.box;

            B2Vec2 p1 = b2AABB_Center(originAABB);
            B2Vec2 extension = b2AABB_Extents(originAABB);

            // v is perpendicular to the segment.
            B2Vec2 r = input.translation;
            B2Vec2 v = b2CrossSV(1.0f, r);
            B2Vec2 abs_v = b2Abs(v);

            // Separating axis for segment (Gino, p80).
            // |dot(v, p1 - c)| > dot(|v|, h)

            float maxFraction = input.maxFraction;

            // Build total box for the cast
            B2Vec2 t = b2MulSV(maxFraction, input.translation);
            B2AABB totalAABB = new B2AABB(
                b2Min(originAABB.lowerBound, b2Add(originAABB.lowerBound, t)),
                b2Max(originAABB.upperBound, b2Add(originAABB.upperBound, t))
            );

            B2AABB boxv = totalAABB;

            B2TreeNode[] nodes = tree.nodes;

            Span<int> stack = stackalloc int[B2_TREE_STACK_SIZE];
            int stackCount = 0;
            stack[stackCount++] = b2GetRootPair(nodes);

            B2BoxCastInput subInput = input;

            Span<int> hit = stackalloc int[2];
            Span<bool> isLeaf = stackalloc bool[2];

            while (stackCount > 0)
            {
                int pair = stack[--stackCount];
                result.nodeVisits += 1;

                int hitCount = 0;
                for (int i = 0; i < 2; ++i)
                {
                    ref readonly B2TreeNode node = ref nodes[pair + i];

                    if (b2OverlapNode(boxv, node) == false)
                    {
                        continue;
                    }

                    // Separating axis for segment (Gino, p80).
                    // |dot(v, p1 - c)| > dot(|v|, h)
                    // radius extension is added to the node in this case
                    B2AABB nodeAABB = node.aabb;
                    B2Vec2 c = b2AABB_Center(nodeAABB);
                    B2Vec2 h = b2Add(b2AABB_Extents(nodeAABB), extension);
                    float term1 = b2AbsFloat(b2Dot(v, b2Sub(p1, c)));
                    float term2 = b2Dot(abs_v, h);
                    if (term2 < term1)
                    {
                        continue;
                    }

                    isLeaf[hitCount] = b2IsLeaf(node);
                    hit[hitCount] = pair + i;
                    hitCount += 1;
                }

                if (hitCount == 2 && isLeaf[0] == false && isLeaf[1] == false)
                {
                    B2Vec2 center1 = b2AABB_Center(nodes[hit[0]].aabb);
                    B2Vec2 center2 = b2AABB_Center(nodes[hit[1]].aabb);
                    float d1 = b2DistanceSquared(center1, p1);
                    float d2 = b2DistanceSquared(center2, p1);

                    // Want to push the closest one last. Both have the same isLeaf, so they don't swap.
                    if (d1 < d2)
                    {
                        (hit[0], hit[1]) = (hit[1], hit[0]);
                    }
                }

                for (int i = 0; i < hitCount; ++i)
                {
                    if (isLeaf[i])
                    {
                        int proxyId = b2GetProxyId(nodes[hit[i]]);
                        ref readonly B2TreeProxy proxy = ref tree.proxies[proxyId];

                        if ((proxy.categoryBits & maskBits) == 0)
                        {
                            continue;
                        }

                        subInput.maxFraction = maxFraction;

                        float value = callback(subInput, proxyId, proxy.userData, ref context);
                        result.leafVisits += 1;

                        // The user may return -1 to indicate this shape should be skipped

                        if (value == 0.0f)
                        {
                            // The client has terminated the ray cast.
                            return result;
                        }

                        if (0.0f < value && value < maxFraction)
                        {
                            // Update cast bounding box.
                            maxFraction = value;
                            t = b2MulSV(maxFraction, input.translation);
                            totalAABB.lowerBound = b2Min(originAABB.lowerBound, b2Add(originAABB.lowerBound, t));
                            totalAABB.upperBound = b2Max(originAABB.upperBound, b2Add(originAABB.upperBound, t));
                            boxv = totalAABB;
                        }
                    }
                    else
                    {
                        if (stackCount < B2_TREE_STACK_SIZE - 1)
                        {
                            stack[stackCount++] = b2GetLeftChild(nodes[hit[i]]);
                        }
                        else
                        {
                            B2_ASSERT(stackCount < B2_TREE_STACK_SIZE - 1);
                        }
                    }
                }
            }

            return result;
        }

        // // Median split == 0, Surface area heuristic == 1
        // #define B2_TREE_HEURISTIC 0
        //
        // #if B2_TREE_HEURISTIC == 0

        // Median split heuristic
        internal static int b2PartitionMid(Span<int> indices, Span<B2Vec2> centers, int count)
        {
            // Handle trivial case
            if (count <= 2)
            {
                return count / 2;
            }

            B2Vec2 lowerBound = centers[0];
            B2Vec2 upperBound = centers[0];

            for (int i = 1; i < count; ++i)
            {
                lowerBound = b2Min(lowerBound, centers[i]);
                upperBound = b2Max(upperBound, centers[i]);
            }

            B2Vec2 d = b2Sub(upperBound, lowerBound);
            B2Vec2 c = new B2Vec2(0.5f * (lowerBound.X + upperBound.X), 0.5f * (lowerBound.Y + upperBound.Y));

            // Partition longest axis using the Hoare partition scheme
            // https://en.wikipedia.org/wiki/Quicksort
            // https://nicholasvadivelu.com/2021/01/11/array-partition/
            int i1 = 0, i2 = count;
            if (d.X > d.Y)
            {
                float pivot = c.X;

                while (i1 < i2)
                {
                    while (i1 < i2 && centers[i1].X < pivot)
                    {
                        i1 += 1;
                    }

                    ;

                    while (i1 < i2 && centers[i2 - 1].X >= pivot)
                    {
                        i2 -= 1;
                    }

                    ;

                    if (i1 < i2)
                    {
                        // Swap indices
                        {
                            int temp = indices[i1];
                            indices[i1] = indices[i2 - 1];
                            indices[i2 - 1] = temp;
                        }

                        // Swap centers
                        {
                            B2Vec2 temp = centers[i1];
                            centers[i1] = centers[i2 - 1];
                            centers[i2 - 1] = temp;
                        }

                        i1 += 1;
                        i2 -= 1;
                    }
                }
            }
            else
            {
                float pivot = c.Y;

                while (i1 < i2)
                {
                    while (i1 < i2 && centers[i1].Y < pivot)
                    {
                        i1 += 1;
                    }

                    ;

                    while (i1 < i2 && centers[i2 - 1].Y >= pivot)
                    {
                        i2 -= 1;
                    }

                    ;

                    if (i1 < i2)
                    {
                        // Swap indices
                        {
                            int temp = indices[i1];
                            indices[i1] = indices[i2 - 1];
                            indices[i2 - 1] = temp;
                        }

                        // Swap centers
                        {
                            B2Vec2 temp = centers[i1];
                            centers[i1] = centers[i2 - 1];
                            centers[i2 - 1] = temp;
                        }

                        i1 += 1;
                        i2 -= 1;
                    }
                }
            }

            B2_ASSERT(i1 == i2);

            if (i1 > 0 && i1 < count)
            {
                return i1;
            }

            return count / 2;
        }

        //#else
        public const int B2_BIN_COUNT = 8;


        // "On Fast Construction of SAH-based Bounding Volume Hierarchies" by Ingo Wald
        // Returns the left child count
        internal static int b2PartitionSAH(Span<int> indices, Span<int> binIndices, Span<B2AABB> boxes, int count)
        {
            B2_ASSERT(count > 0);

            Span<B2TreeBin> bins = stackalloc B2TreeBin[B2_BIN_COUNT];
            Span<B2TreePlane> planes = stackalloc B2TreePlane[B2_BIN_COUNT - 1];

            B2Vec2 center = b2AABB_Center(boxes[0]);
            B2AABB centroidAABB;
            centroidAABB.lowerBound = center;
            centroidAABB.upperBound = center;

            for (int i = 1; i < count; ++i)
            {
                center = b2AABB_Center(boxes[i]);
                centroidAABB.lowerBound = b2Min(centroidAABB.lowerBound, center);
                centroidAABB.upperBound = b2Max(centroidAABB.upperBound, center);
            }

            B2Vec2 d = b2Sub(centroidAABB.upperBound, centroidAABB.lowerBound);

            // Find longest axis
            int axisIndex;
            float invD;
            if (d.X > d.Y)
            {
                axisIndex = 0;
                invD = d.X;
            }
            else
            {
                axisIndex = 1;
                invD = d.Y;
            }

            invD = invD > 0.0f ? 1.0f / invD : 0.0f;

            // Initialize bin bounds and count
            for (int i = 0; i < B2_BIN_COUNT; ++i)
            {
                bins[i].aabb.lowerBound = new B2Vec2(float.MaxValue, float.MaxValue);
                bins[i].aabb.upperBound = new B2Vec2(-float.MaxValue, -float.MaxValue);
                bins[i].count = 0;
            }

            // Assign boxes to bins and compute bin boxes
            // TODO_ERIN optimize
            float binCount = B2_BIN_COUNT;
            B2FixedArray2<float> lowerBoundArray = new B2FixedArray2<float>(centroidAABB.lowerBound.X, centroidAABB.lowerBound.Y);

            float minC = lowerBoundArray[axisIndex];
            for (int i = 0; i < count; ++i)
            {
                B2Vec2 c = b2AABB_Center(boxes[i]);
                B2FixedArray2<float> cArray = new B2FixedArray2<float>(c.X, c.Y);
                int binIndex = (int)(binCount * (cArray[axisIndex] - minC) * invD);
                binIndex = b2ClampInt(binIndex, 0, B2_BIN_COUNT - 1);
                binIndices[i] = binIndex;
                bins[binIndex].count += 1;
                bins[binIndex].aabb = b2AABB_Union(bins[binIndex].aabb, boxes[i]);
            }

            int planeCount = B2_BIN_COUNT - 1;

            // Prepare all the left planes, candidates for left child
            planes[0].leftCount = bins[0].count;
            planes[0].leftAABB = bins[0].aabb;
            for (int i = 1; i < planeCount; ++i)
            {
                planes[i].leftCount = planes[i - 1].leftCount + bins[i].count;
                planes[i].leftAABB = b2AABB_Union(planes[i - 1].leftAABB, bins[i].aabb);
            }

            // Prepare all the right planes, candidates for right child
            planes[planeCount - 1].rightCount = bins[planeCount].count;
            planes[planeCount - 1].rightAABB = bins[planeCount].aabb;
            for (int i = planeCount - 2; i >= 0; --i)
            {
                planes[i].rightCount = planes[i + 1].rightCount + bins[i + 1].count;
                planes[i].rightAABB = b2AABB_Union(planes[i + 1].rightAABB, bins[i + 1].aabb);
            }

            // Find best split to minimize SAH
            float minCost = float.MaxValue;
            int bestPlane = 0;
            for (int i = 0; i < planeCount; ++i)
            {
                float leftArea = b2Perimeter(planes[i].leftAABB);
                float rightArea = b2Perimeter(planes[i].rightAABB);
                int leftCount = planes[i].leftCount;
                int rightCount = planes[i].rightCount;

                float cost = leftCount * leftArea + rightCount * rightArea;
                if (cost < minCost)
                {
                    bestPlane = i;
                    minCost = cost;
                }
            }

            // Partition node indices and boxes using the Hoare partition scheme
            // https://en.wikipedia.org/wiki/Quicksort
            // https://nicholasvadivelu.com/2021/01/11/array-partition/
            int i1 = 0, i2 = count;
            while (i1 < i2)
            {
                while (i1 < i2 && binIndices[i1] < bestPlane)
                {
                    i1 += 1;
                }

                ;

                while (i1 < i2 && binIndices[i2 - 1] >= bestPlane)
                {
                    i2 -= 1;
                }

                ;

                if (i1 < i2)
                {
                    // Swap indices
                    {
                        int temp = indices[i1];
                        indices[i1] = indices[i2 - 1];
                        indices[i2 - 1] = temp;
                    }

                    // Swap boxes
                    {
                        B2AABB temp = boxes[i1];
                        boxes[i1] = boxes[i2 - 1];
                        boxes[i2 - 1] = temp;
                    }

                    i1 += 1;
                    i2 -= 1;
                }
            }

            B2_ASSERT(i1 == i2);

            if (i1 > 0 && i1 < count)
            {
                return i1;
            }
            else
            {
                return count / 2;
            }
        }

        // Bump allocate a pair of sibling nodes. Returns index to the first one.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int b2BumpPair(B2DynamicTree tree, int parent)
        {
            int pair = tree.nodeEnd;
            B2_ASSERT(pair + 2 <= tree.nodeCapacity);
            tree.nodeEnd += 2;
            tree.parents[pair] = parent;
            tree.parents[pair + 1] = parent;
            return pair;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void b2SetLeftChild(ref B2TreeNode node, int pair)
        {
            node.flagIndex = (node.flagIndex & ~B2_NODE_INDEX_MASK) | (uint)pair;
        }

        // Copy a retained subtree from the old array into the rebuilt DFS array using
        // a bump allocator.
        private static void b2CopySubtree(B2DynamicTree tree, B2TreeNode node, int newIndex)
        {
            B2TreeNode[] oldNodes = tree.nodes;
            B2TreeNode[] newNodes = tree.swapNodes;
            B2TreeProxy[] proxies = tree.proxies;

            if (b2IsLeaf(node))
            {
                newNodes[newIndex] = node;
                proxies[b2GetProxyId(node)].node = newIndex;
                return;
            }

            Span<B2CopyItem> stack = stackalloc B2CopyItem[B2_TREE_STACK_SIZE];
            int stackCount = 0;

            int oldPair = b2GetLeftChild(node);
            int newPair = b2BumpPair(tree, newIndex);
            b2SetLeftChild(ref node, newPair);

            // Copy the subtree root. This can be a left or right sibling (even or odd).
            newNodes[newIndex] = node;

            for (;;)
            {
                // Copy the siblings.
                newNodes[newPair] = oldNodes[oldPair];
                newNodes[newPair + 1] = oldNodes[oldPair + 1];

                if (b2IsLeaf(newNodes[newPair + 1]))
                {
                    // Hook up proxy on right sibling.
                    int proxyId = b2GetProxyId(newNodes[newPair + 1]);
                    proxies[proxyId].node = newPair + 1;
                }
                else
                {
                    // Push the left child of the right sibling.
                    B2_ASSERT(stackCount < B2_TREE_STACK_SIZE);
                    int leftChild = b2GetLeftChild(newNodes[newPair + 1]);
                    stack[stackCount++] = new B2CopyItem(leftChild, newPair + 1);
                }

                if (b2IsLeaf(newNodes[newPair]) == false)
                {
                    // Descend into left subtree.
                    int leftIndex = newPair;
                    oldPair = b2GetLeftChild(newNodes[newPair]);
                    newPair = b2BumpPair(tree, leftIndex);
                    b2SetLeftChild(ref newNodes[leftIndex], newPair);
                    continue;
                }

                // Hook up proxy on left sibling.
                proxies[b2GetProxyId(newNodes[newPair])].node = newPair;

                if (stackCount == 0)
                {
                    break;
                }

                // Descend a right subtree that was previously pushed.
                B2CopyItem item = stack[--stackCount];
                oldPair = item.oldPair;
                newPair = b2BumpPair(tree, item.newIndex);
                b2SetLeftChild(ref newNodes[item.newIndex], newPair);
            }
        }

        private static void b2PlaceLeaf(B2DynamicTree tree, in B2TreeNode leaf, int newIndex)
        {
            if (b2IsLeaf(leaf))
            {
                tree.swapNodes[newIndex] = leaf;
                tree.proxies[b2GetProxyId(leaf)].node = newIndex;
            }
            else
            {
                b2CopySubtree(tree, leaf, newIndex);
            }
        }

        private static void b2BuildTree(B2DynamicTree tree, int leafCount)
        {
            B2TreeNode[] nodes = tree.swapNodes;
            B2TreeNode[] leaves = tree.leafNodes;
            int[] leafIndices = tree.leafIndices;

            //#if B2_TREE_HEURISTIC == 0
            B2Vec2[] leafCenters = tree.leafCenters;
            //#else
            //B2AABB[] leafBoxes = tree.leafBoxes;
            //int[] binIndices = tree.binIndices;
            //#endif

            // Bump allocation into the swap nodes.
            tree.nodeEnd = 2;
            nodes[B2_ROOT_NODE + 1] = b2MakeEmptyNode();
            tree.parents[B2_ROOT_NODE] = B2_NULL_INDEX;
            tree.parents[B2_ROOT_NODE + 1] = B2_NULL_INDEX;

            if (leafCount == 1)
            {
                b2PlaceLeaf(tree, leaves[leafIndices[0]], B2_ROOT_NODE);
                return;
            }

            Span<B2RebuildItem> stack = stackalloc B2RebuildItem[B2_TREE_STACK_SIZE];
            int top = 0;

            stack[0].nodeIndex = B2_ROOT_NODE;
            stack[0].pair = b2BumpPair(tree, B2_ROOT_NODE);
            stack[0].childCount = -1;
            stack[0].startIndex = 0;
            stack[0].endIndex = leafCount;
            //#if B2_TREE_HEURISTIC == 0
            stack[0].splitIndex = b2PartitionMid(leafIndices, leafCenters, leafCount);
            //#else
            //stack[0].splitIndex = b2PartitionSAH(leafIndices, binIndices, leafBoxes, leafCount);
            //#endif

            while (true)
            {
                ref B2RebuildItem item = ref stack[top];
                item.childCount += 1;

                if (item.childCount == 2)
                {
                    // Both children written, so the parent node can be finalized.
                    nodes[item.nodeIndex] = b2MakeInternalNode(nodes, item.pair);
                    if (top == 0)
                    {
                        break;
                    }

                    top -= 1;
                    continue;
                }

                int slot = item.childCount;
                int startIndex = slot == 0 ? item.startIndex : item.splitIndex;
                int endIndex = slot == 0 ? item.splitIndex : item.endIndex;
                int count = endIndex - startIndex;
                B2_ASSERT(count > 0);

                int nodeIndex = item.pair + slot;
                if (count == 1)
                {
                    b2PlaceLeaf(tree, leaves[leafIndices[startIndex]], nodeIndex);
                    continue;
                }

                // todo fail gracefully if the stack runs out in release
                B2_ASSERT(top < B2_TREE_STACK_SIZE - 1);

                top += 1;
                ref B2RebuildItem newItem = ref stack[top];
                newItem.nodeIndex = nodeIndex;
                newItem.pair = b2BumpPair(tree, nodeIndex);
                newItem.childCount = -1;
                newItem.startIndex = startIndex;
                newItem.endIndex = endIndex;
                newItem.splitIndex = startIndex;
                //#if B2_TREE_HEURISTIC == 0
                newItem.splitIndex += b2PartitionMid(leafIndices.AsSpan(startIndex), leafCenters.AsSpan(startIndex), count);
                //#else
                //newItem.splitIndex += b2PartitionSAH(leafIndices.AsSpan(startIndex), binIndices.AsSpan(startIndex), leafBoxes.AsSpan(startIndex), count);
                //#endif
            }
        }

        /// Rebuild the tree while retaining subtrees that haven't changed. Returns the number of boxes sorted.
        // Rebuild the stale parts of the tree. The entire tree is put into DFS order. This makes
        // refitting much faster. This is done async with threading, so the cost is hidden.
        // Not safe to access tree during this operation.
        public static int b2DynamicTree_Rebuild(B2DynamicTree tree, bool fullBuild)
        {
            int proxyCount = tree.proxyCount;
            if (proxyCount == 0)
            {
                return 0;
            }

            // An unordered tree is rebuilt even when nothing moved, the sweep refit needs the order
            B2TreeNode[] nodes = tree.nodes;
            B2TreeNode root = nodes[B2_ROOT_NODE];
            if (fullBuild == false && b2IsNodeMoved(root) == false && tree.dfsOrdered)
            {
                return 0;
            }

            if (tree.swapNodes == null)
            {
                tree.swapNodes = b2Alloc<B2TreeNode>(tree.nodeCapacity);
            }

            if (proxyCount > tree.rebuildCapacity)
            {
                int oldCapacity = tree.rebuildCapacity;
                int newCapacity = proxyCount + proxyCount / 2;

                tree.leafIndices = b2GrowAlloc(tree.leafIndices, oldCapacity, newCapacity);
                tree.leafNodes = b2GrowAlloc(tree.leafNodes, oldCapacity, newCapacity);
                //#if B2_TREE_HEURISTIC == 0
                tree.leafCenters = b2GrowAlloc(tree.leafCenters, oldCapacity, newCapacity);
                //#else
                //tree.leafBoxes = b2GrowAlloc(tree.leafBoxes, oldCapacity, newCapacity);
                //tree.binIndices = b2GrowAlloc(tree.binIndices, oldCapacity, newCapacity);
                //#endif
                tree.rebuildCapacity = newCapacity;
            }

            int[] leafIndices = tree.leafIndices;
            B2TreeNode[] leaves = tree.leafNodes;
            //#if B2_TREE_HEURISTIC == 0
            B2Vec2[] leafCenters = tree.leafCenters;
            //#else
            //B2AABB[] leafBoxes = tree.leafBoxes;
            //#endif

            // Gather build leaves. A marked internal node is descended and abandoned in the old array.
            // Everything else is a build leaf, kept subtrees included. A leaf root, or an unmarked root
            // on an unordered array, is the one build leaf.
            int leafCount = 0;
            Span<int> stack = stackalloc int[B2_TREE_STACK_SIZE];
            int stackCount = 0;
            if (b2IsLeaf(root) == false && (fullBuild || b2IsNodeMoved(root)))
            {
                stack[stackCount++] = b2GetLeftChild(root);
            }
            else
            {
                B2TreeNode node = root;
                node.flagIndex &= ~B2_MOVED_NODE;
                leafIndices[0] = 0;
                leaves[0] = node;
                //#if B2_TREE_HEURISTIC == 0
                leafCenters[0] = b2AABB_Center(node.aabb);
                //#else
                //leafBoxes[0] = node.aabb;
                //#endif
                leafCount = 1;
            }

            while (stackCount > 0)
            {
                int pair = stack[--stackCount];
                for (int i = 0; i < 2; ++i)
                {
                    B2TreeNode node = nodes[pair + i];
                    if (b2IsLeaf(node) == false && (fullBuild || b2IsNodeMoved(node)))
                    {
                        // todo fail gracefully if the stack runs out in release
                        B2_ASSERT(stackCount < B2_TREE_STACK_SIZE);
                        stack[stackCount++] = b2GetLeftChild(node);
                        continue;
                    }

                    node.flagIndex &= ~B2_MOVED_NODE;
                    leafIndices[leafCount] = leafCount;
                    leaves[leafCount] = node;
                    //#if B2_TREE_HEURISTIC == 0
                    leafCenters[leafCount] = b2AABB_Center(node.aabb);
                    //#else
                    //leafBoxes[leafCount] = node.aabb;
                    //#endif
                    leafCount += 1;
                }
            }

            B2_ASSERT(0 < leafCount && leafCount <= proxyCount);

            b2BuildTree(tree, leafCount);

            // The spare is now the tree and the old array is the spare. The build is dense, so the
            // stale tail of the old array sits above the end and is never read.
            (tree.nodes, tree.swapNodes) = (tree.swapNodes, tree.nodes);
            tree.pairFreeList = B2_NULL_INDEX;
            tree.dfsOrdered = true;

            b2DynamicTree_Validate(tree);
            b2DynamicTree_ValidateNoMoved(tree);

            return leafCount;
        }

        // Set the moved flag on the ancestors of the proxy. Serial use case.
        public static void b2DynamicTree_MarkProxyMovedSerial(B2DynamicTree tree, int proxyId)
        {
            B2_VALIDATE(0 <= proxyId && proxyId < tree.proxyCapacity);

            B2TreeNode[] nodes = tree.nodes;
            int[] parents = tree.parents;

            int index = tree.proxies[proxyId].node;
            B2_VALIDATE(0 <= index && index < tree.nodeEnd);
            B2_VALIDATE(b2IsLeaf(nodes[index]));

            while (index != B2_NULL_INDEX)
            {
                nodes[index].flagIndex |= B2_MOVED_NODE;
                index = parents[index];
            }
        }

        // Update a proxy AABB and flag the ancestors as moved. Thread-safe using atomics.
        public static void b2DynamicTree_MarkProxyMoved(B2DynamicTree tree, int proxyId, in B2AABB aabb)
        {
            B2_VALIDATE(0 <= proxyId && proxyId < tree.proxyCapacity);

            B2TreeNode[] nodes = tree.nodes;
            int[] parents = tree.parents;

            int index = tree.proxies[proxyId].node;
            B2_VALIDATE(0 <= index && index < tree.nodeEnd);

            ref B2TreeNode node = ref nodes[index];
            B2_VALIDATE(b2IsLeaf(node));
            B2_VALIDATE(b2AABB_Contains(node.aabb, aabb) == false);

            // This is not raced since it is the leaf.
            node.aabb = aabb;
            node.flagIndex |= B2_MOVED_NODE;

            index = parents[index];
            while (index != B2_NULL_INDEX)
            {
                node = ref nodes[index];

                // Read first to avoid the FetchOr if possible.
                if ((b2AtomicLoadU32Raw(ref node.flagIndex) & B2_MOVED_NODE) != 0)
                {
                    break;
                }

                uint previousFlags = b2AtomicFetchOrU32(ref node.flagIndex, B2_MOVED_NODE);
                if ((previousFlags & B2_MOVED_NODE) != 0)
                {
                    // Ancestor already visited.
                    break;
                }

                index = parents[index];
            }
        }

        // Clear the moved flags from the entire tree.
        public static void b2DynamicTree_ClearMoved(B2DynamicTree tree)
        {
            B2TreeNode[] nodes = tree.nodes;

            ref B2TreeNode root = ref nodes[B2_ROOT_NODE];
            if (b2IsNodeMoved(root) == false)
            {
                return;
            }

            root.flagIndex &= ~B2_MOVED_NODE;
            if (b2IsLeaf(root))
            {
                return;
            }

            Span<int> stack = stackalloc int[B2_TREE_STACK_SIZE];
            int stackCount = 0;
            stack[stackCount++] = b2GetLeftChild(root);

            while (stackCount > 0)
            {
                int pair = stack[--stackCount];
                for (int i = 0; i < 2; ++i)
                {
                    ref B2TreeNode node = ref nodes[pair + i];
                    if ((node.flagIndex & B2_MOVED_NODE) != 0)
                    {
                        node.flagIndex &= ~B2_MOVED_NODE;
                        if (b2IsLeaf(node) == false)
                        {
                            if (stackCount < B2_TREE_STACK_SIZE)
                            {
                                stack[stackCount++] = b2GetLeftChild(node);
                            }
                            else
                            {
                                // Bad stuff will happen if the moved flags don't get cleared.
                                B2_ASSERT(stackCount < B2_TREE_STACK_SIZE);
                            }
                        }
                    }
                }
            }
        }

        public static int b2DynamicTree_GatherMovedProxies(B2DynamicTree tree, Span<int> proxyIds)
        {
            B2TreeNode[] nodes = tree.nodes;
            ref readonly B2TreeNode root = ref nodes[B2_ROOT_NODE];
            if (b2IsNodeMoved(root) == false)
            {
                return 0;
            }

            if (b2IsLeaf(root))
            {
                proxyIds[0] = b2GetProxyId(root);
                return 1;
            }

            int count = 0;
            Span<int> stack = stackalloc int[B2_TREE_STACK_SIZE];
            int stackCount = 0;
            stack[stackCount++] = b2GetLeftChild(root);

            while (stackCount > 0)
            {
                int pair = stack[--stackCount];
                for (int i = 0; i < 2; ++i)
                {
                    ref readonly B2TreeNode node = ref nodes[pair + i];
                    if (b2IsNodeMoved(node) == false)
                    {
                        continue;
                    }

                    if (b2IsLeaf(node))
                    {
                        proxyIds[count++] = b2GetProxyId(node);
                    }
                    else
                    {
                        if (stackCount < B2_TREE_STACK_SIZE)
                        {
                            stack[stackCount++] = b2GetLeftChild(node);
                        }
                        else
                        {
                            B2_ASSERT(stackCount < B2_TREE_STACK_SIZE);
                        }
                    }
                }
            }

            return count;
        }

        // Slow refit for unit tests.
        private static void b2RefitSubtree(B2TreeNode[] nodes, int rootIndex)
        {
            ref B2TreeNode root = ref nodes[rootIndex];
            if (b2IsLeaf(root) || b2IsNodeMoved(root) == false)
            {
                return;
            }

            // Negative entries indicate deferred unions. A parent is pushed before its children so the
            // LIFO order guarantees both children are finished when the union runs.
            Span<int> stack = stackalloc int[B2_TREE_STACK_SIZE];
            int stackCount = 0;
            // ~0 == -1
            stack[stackCount++] = ~rootIndex;
            stack[stackCount++] = b2GetLeftChild(root);
            stack[stackCount++] = b2GetLeftChild(root) + 1;

            while (stackCount > 0)
            {
                int item = stack[--stackCount];
                if (item < 0)
                {
                    ref B2TreeNode parent = ref nodes[~item];
                    int pair = b2GetLeftChild(parent);
                    parent.aabb = b2UnionV(nodes[pair].aabb, nodes[pair + 1].aabb);
                    continue;
                }

                ref readonly B2TreeNode node = ref nodes[item];
                if (b2IsLeaf(node) || b2IsNodeMoved(node) == false)
                {
                    continue;
                }

                if (stackCount + 3 <= B2_TREE_STACK_SIZE)
                {
                    int pair = b2GetLeftChild(node);
                    stack[stackCount++] = ~item;
                    stack[stackCount++] = pair;
                    stack[stackCount++] = pair + 1;
                }
                else
                {
                    B2_ASSERT(stackCount + 3 <= B2_TREE_STACK_SIZE);
                }
            }
        }

        public static void b2DynamicTree_Refit(B2DynamicTree tree)
        {
            if (b2HasTreeMoved(tree) == false)
            {
                return;
            }

            B2TreeNode[] nodes = tree.nodes;

            if (tree.dfsOrdered == false)
            {
                // This only happens in unit tests.
                b2RefitSubtree(nodes, B2_ROOT_NODE);
                return;
            }

            for (int pair = tree.nodeEnd - 2; pair >= 0; pair -= 2)
            {
                uint flags1 = nodes[pair].flagIndex;
                uint flags2 = nodes[pair + 1].flagIndex;

                // Did either move?
                if (((flags1 | flags2) & B2_MOVED_NODE) == 0)
                {
                    continue;
                }

                // Is the node internal and moved?
                bool refit1 = (flags1 & (B2_LEAF_NODE | B2_MOVED_NODE)) == B2_MOVED_NODE;
                bool refit2 = (flags2 & (B2_LEAF_NODE | B2_MOVED_NODE)) == B2_MOVED_NODE;

                // If the node needs a refit then get the child index. Otherwise use a valid dummy index.
                // The store function won't actually modify the node at the dummy index. This
                // avoids an unpredictable branch. SIMD FTW.
                int children1 = refit1 ? (int)(flags1 & B2_NODE_INDEX_MASK) : pair;
                int children2 = refit2 ? (int)(flags2 & B2_NODE_INDEX_MASK) : pair;

                // Conditionally store the union.
                b2StoreAABBV(ref nodes[pair].aabb, b2UnionPairV(nodes, children1), refit1);
                b2StoreAABBV(ref nodes[pair + 1].aabb, b2UnionPairV(nodes, children2), refit2);
            }
        }
    }
}
