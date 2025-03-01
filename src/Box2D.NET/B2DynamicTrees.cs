﻿// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using Box2D.NET.Primitives;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Cores;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2AABBs;

namespace Box2D.NET
{
    public static class B2DynamicTrees
    {
        public const int B2_TREE_STACK_SIZE = 1024;

        /// This function receives proxies found in the AABB query.
        /// @return true if the query should continue
        public delegate bool b2TreeQueryCallbackFcn(int proxyId, int userData, object context);

        /// This function receives clipped ray cast input for a proxy. The function
        /// returns the new ray fraction.
        /// - return a value of 0 to terminate the ray cast
        /// - return a value less than input->maxFraction to clip the ray
        /// - return a value of input->maxFraction to continue the ray cast without clipping
        public delegate float b2TreeRayCastCallbackFcn(B2RayCastInput input, int proxyId, int userData, object context);

        /// This function receives clipped ray cast input for a proxy. The function
        /// returns the new ray fraction.
        /// - return a value of 0 to terminate the ray cast
        /// - return a value less than input->maxFraction to clip the ray
        /// - return a value of input->maxFraction to continue the ray cast without clipping
        public delegate float b2TreeShapeCastCallbackFcn(B2ShapeCastInput input, int proxyId, int userData, object context);

        public static readonly B2TreeNode b2_defaultTreeNode = new B2TreeNode()
        {
            aabb = new B2AABB(new B2Vec2(0.0f, 0.0f), new B2Vec2(0.0f, 0.0f)),
            categoryBits = B2_DEFAULT_CATEGORY_BITS,
            parent = B2_NULL_INDEX,
            child1 = B2_NULL_INDEX,
            child2 = B2_NULL_INDEX,
            height = 0,
            flags = (ushort)B2TreeNodeFlags.b2_allocatedNode,
        };

        public static bool b2IsLeaf(B2TreeNode node)
        {
            return 0 != (node.flags & (ushort)B2TreeNodeFlags.b2_leafNode);
        }

        public static bool b2IsAllocated(B2TreeNode node)
        {
            return 0 != (node.flags & (ushort)B2TreeNodeFlags.b2_allocatedNode);
        }

        public static ushort b2MaxUInt16(ushort a, ushort b)
        {
            return a > b ? a : b;
        }

        /// Constructing the tree initializes the node pool.
        public static B2DynamicTree b2DynamicTree_Create()
        {
            B2DynamicTree tree = new B2DynamicTree();
            tree.root = B2_NULL_INDEX;

            tree.nodeCapacity = 16;
            tree.nodeCount = 0;
            tree.nodes = b2Alloc<B2TreeNode>(tree.nodeCapacity);
            //memset( tree.nodes, 0, tree.nodeCapacity * sizeof( b2TreeNode ) );
            foreach (var node in tree.nodes)
            {
                node.Clear();
            }

            // Build a linked list for the free list.
            for (int i = 0; i < tree.nodeCapacity - 1; ++i)
            {
                tree.nodes[i].next = i + 1;
            }

            tree.nodes[tree.nodeCapacity - 1].next = B2_NULL_INDEX;
            tree.freeList = 0;

            tree.proxyCount = 0;

            tree.leafIndices = null;
            tree.leafBoxes = null;
            tree.leafCenters = null;
            tree.binIndices = null;
            tree.rebuildCapacity = 0;

            return tree;
        }

        /// Destroy the tree, freeing the node pool.
        public static void b2DynamicTree_Destroy(B2DynamicTree tree)
        {
            b2Free(tree.nodes, tree.nodeCapacity);
            b2Free(tree.leafIndices, tree.rebuildCapacity);
            b2Free(tree.leafBoxes, tree.rebuildCapacity);
            b2Free(tree.leafCenters, tree.rebuildCapacity);
            b2Free(tree.binIndices, tree.rebuildCapacity);

            //memset( tree, 0, sizeof( b2DynamicTree ) );
            tree.Clear();
        }

        // Allocate a node from the pool. Grow the pool if necessary.
        public static int b2AllocateNode(B2DynamicTree tree)
        {
            // Expand the node pool as needed.
            if (tree.freeList == B2_NULL_INDEX)
            {
                Debug.Assert(tree.nodeCount == tree.nodeCapacity);

                // The free list is empty. Rebuild a bigger pool.
                B2TreeNode[] oldNodes = tree.nodes;
                int oldCapacity = tree.nodeCapacity;
                tree.nodeCapacity += oldCapacity >> 1;
                tree.nodes = b2Alloc<B2TreeNode>(tree.nodeCapacity);
                Debug.Assert(oldNodes != null);
                //memcpy( tree->nodes, oldNodes, tree->nodeCount * sizeof( b2TreeNode ) );
                for (int i = 0; i < tree.nodeCount; ++i)
                {
                    tree.nodes[i].CopyFrom(oldNodes[i]);
                }

                //memset( tree->nodes + tree->nodeCount, 0, ( tree->nodeCapacity - tree->nodeCount ) * sizeof( b2TreeNode ) );
                for (int i = tree.nodeCount; i < tree.nodeCapacity; ++i)
                {
                    tree.nodes[i].Clear();
                }

                b2Free(oldNodes, oldCapacity);

                // Build a linked list for the free list. The parent pointer becomes the "next" pointer.
                // todo avoid building freelist?
                for (int i = tree.nodeCount; i < tree.nodeCapacity - 1; ++i)
                {
                    tree.nodes[i].next = i + 1;
                }

                tree.nodes[tree.nodeCapacity - 1].next = B2_NULL_INDEX;
                tree.freeList = tree.nodeCount;
            }

            // Peel a node off the free list.
            int nodeIndex = tree.freeList;
            B2TreeNode node = tree.nodes[nodeIndex];
            tree.freeList = node.next;
            node.CopyFrom(b2_defaultTreeNode);
            ++tree.nodeCount;
            return nodeIndex;
        }

        // Return a node to the pool.
        public static void b2FreeNode(B2DynamicTree tree, int nodeId)
        {
            Debug.Assert(0 <= nodeId && nodeId < tree.nodeCapacity);
            Debug.Assert(0 < tree.nodeCount);
            tree.nodes[nodeId].next = tree.freeList;
            tree.nodes[nodeId].flags = 0;
            tree.freeList = nodeId;
            --tree.nodeCount;
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
        public static int b2FindBestSibling(B2DynamicTree tree, B2AABB boxD)
        {
            B2Vec2 centerD = b2AABB_Center(boxD);
            float areaD = b2Perimeter(boxD);

            B2TreeNode[] nodes = tree.nodes;
            int rootIndex = tree.root;

            B2AABB rootBox = nodes[rootIndex].aabb;

            // Area of current node
            float areaBase = b2Perimeter(rootBox);

            // Area of inflated node
            float directCost = b2Perimeter(b2AABB_Union(rootBox, boxD));
            float inheritedCost = 0.0f;

            int bestSibling = rootIndex;
            float bestCost = directCost;

            // Descend the tree from root, following a single greedy path.
            int index = rootIndex;
            while (nodes[index].height > 0)
            {
                int child1 = nodes[index].child1;
                int child2 = nodes[index].child2;

                // Cost of creating a new parent for this node and the new leaf
                float cost = directCost + inheritedCost;

                // Sometimes there are multiple identical costs within tolerance.
                // This breaks the ties using the centroid distance.
                if (cost < bestCost)
                {
                    bestSibling = index;
                    bestCost = cost;
                }

                // Inheritance cost seen by children
                inheritedCost += directCost - areaBase;

                bool leaf1 = nodes[child1].height == 0;
                bool leaf2 = nodes[child2].height == 0;

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

                    // Need this here due to while condition above
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

                    // Lower bound cost of inserting under child 1.
                    lowerCost1 = inheritedCost + directCost1 + b2MinFloat(areaD - area1, 0.0f);
                }

                // Cost of descending into child 2
                float lowerCost2 = float.MaxValue;
                B2AABB box2 = nodes[child2].aabb;
                float directCost2 = b2Perimeter(b2AABB_Union(box2, boxD));
                float area2 = 0.0f;
                if (leaf2)
                {
                    // Child 2 is a leaf
                    // Cost of creating new node and increasing area of node P
                    float cost2 = directCost2 + inheritedCost;

                    // Need this here due to while condition above
                    if (cost2 < bestCost)
                    {
                        bestSibling = child2;
                        bestCost = cost2;
                    }
                }
                else
                {
                    // Child 2 is an internal node
                    area2 = b2Perimeter(box2);

                    // Lower bound cost of inserting under child 2. This is not the cost
                    // of child 2, it is the best we can hope for under child 2.
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
                    Debug.Assert(lowerCost1 < float.MaxValue);
                    Debug.Assert(lowerCost2 < float.MaxValue);

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
                    index = child1;
                    areaBase = area1;
                    directCost = directCost1;
                }
                else
                {
                    index = child2;
                    areaBase = area2;
                    directCost = directCost2;
                }

                Debug.Assert(nodes[index].height > 0);
            }

            return bestSibling;
        }


        // Perform a left or right rotation if node A is imbalanced.
        // Returns the new root index.
        public static void b2RotateNodes(B2DynamicTree tree, int iA)
        {
            Debug.Assert(iA != B2_NULL_INDEX);

            B2TreeNode[] nodes = tree.nodes;

            B2TreeNode A = nodes[iA];
            if (A.height < 2)
            {
                return;
            }

            int iB = A.child1;
            int iC = A.child2;
            Debug.Assert(0 <= iB && iB < tree.nodeCapacity);
            Debug.Assert(0 <= iC && iC < tree.nodeCapacity);

            B2TreeNode B = nodes[iB];
            B2TreeNode C = nodes[iC];

            if (B.height == 0)
            {
                // B is a leaf and C is internal
                Debug.Assert(C.height > 0);

                int iF = C.child1;
                int iG = C.child2;
                B2TreeNode F = nodes[iF];
                B2TreeNode G = nodes[iG];
                Debug.Assert(0 <= iF && iF < tree.nodeCapacity);
                Debug.Assert(0 <= iG && iG < tree.nodeCapacity);

                // Base cost
                float costBase = b2Perimeter(C.aabb);

                // Cost of swapping B and F
                B2AABB aabbBG = b2AABB_Union(B.aabb, G.aabb);
                float costBF = b2Perimeter(aabbBG);

                // Cost of swapping B and G
                B2AABB aabbBF = b2AABB_Union(B.aabb, F.aabb);
                float costBG = b2Perimeter(aabbBF);

                if (costBase < costBF && costBase < costBG)
                {
                    // Rotation does not improve cost
                    return;
                }

                if (costBF < costBG)
                {
                    // Swap B and F
                    A.child1 = iF;
                    C.child1 = iB;

                    B.parent = iC;
                    F.parent = iA;

                    C.aabb = aabbBG;

                    C.height = (ushort)(1 + b2MaxUInt16(B.height, G.height));
                    A.height = (ushort)(1 + b2MaxUInt16(C.height, F.height));
                    C.categoryBits = B.categoryBits | G.categoryBits;
                    A.categoryBits = C.categoryBits | F.categoryBits;
                    C.flags |= (ushort)((B.flags | G.flags) & (int)B2TreeNodeFlags.b2_enlargedNode);
                    A.flags |= (ushort)((C.flags | F.flags) & (int)B2TreeNodeFlags.b2_enlargedNode);
                }
                else
                {
                    // Swap B and G
                    A.child1 = iG;
                    C.child2 = iB;

                    B.parent = iC;
                    G.parent = iA;

                    C.aabb = aabbBF;

                    C.height = (ushort)(1 + b2MaxUInt16(B.height, F.height));
                    A.height = (ushort)(1 + b2MaxUInt16(C.height, G.height));
                    C.categoryBits = B.categoryBits | F.categoryBits;
                    A.categoryBits = C.categoryBits | G.categoryBits;
                    C.flags |= (ushort)((B.flags | F.flags) & (int)B2TreeNodeFlags.b2_enlargedNode);
                    A.flags |= (ushort)((C.flags | G.flags) & (int)B2TreeNodeFlags.b2_enlargedNode);
                }
            }
            else if (C.height == 0)
            {
                // C is a leaf and B is internal
                Debug.Assert(B.height > 0);

                int iD = B.child1;
                int iE = B.child2;
                B2TreeNode D = nodes[iD];
                B2TreeNode E = nodes[iE];
                Debug.Assert(0 <= iD && iD < tree.nodeCapacity);
                Debug.Assert(0 <= iE && iE < tree.nodeCapacity);

                // Base cost
                float costBase = b2Perimeter(B.aabb);

                // Cost of swapping C and D
                B2AABB aabbCE = b2AABB_Union(C.aabb, E.aabb);
                float costCD = b2Perimeter(aabbCE);

                // Cost of swapping C and E
                B2AABB aabbCD = b2AABB_Union(C.aabb, D.aabb);
                float costCE = b2Perimeter(aabbCD);

                if (costBase < costCD && costBase < costCE)
                {
                    // Rotation does not improve cost
                    return;
                }

                if (costCD < costCE)
                {
                    // Swap C and D
                    A.child2 = iD;
                    B.child1 = iC;

                    C.parent = iB;
                    D.parent = iA;

                    B.aabb = aabbCE;

                    B.height = (ushort)(1 + b2MaxUInt16(C.height, E.height));
                    A.height = (ushort)(1 + b2MaxUInt16(B.height, D.height));
                    B.categoryBits = C.categoryBits | E.categoryBits;
                    A.categoryBits = B.categoryBits | D.categoryBits;
                    B.flags |= (ushort)((C.flags | E.flags) & (int)B2TreeNodeFlags.b2_enlargedNode);
                    A.flags |= (ushort)((B.flags | D.flags) & (int)B2TreeNodeFlags.b2_enlargedNode);
                }
                else
                {
                    // Swap C and E
                    A.child2 = iE;
                    B.child2 = iC;

                    C.parent = iB;
                    E.parent = iA;

                    B.aabb = aabbCD;
                    B.height = (ushort)(1 + b2MaxUInt16(C.height, D.height));
                    A.height = (ushort)(1 + b2MaxUInt16(B.height, E.height));
                    B.categoryBits = C.categoryBits | D.categoryBits;
                    A.categoryBits = B.categoryBits | E.categoryBits;
                    B.flags |= (ushort)((C.flags | D.flags) & (int)B2TreeNodeFlags.b2_enlargedNode);
                    A.flags |= (ushort)((B.flags | E.flags) & (int)B2TreeNodeFlags.b2_enlargedNode);
                }
            }
            else
            {
                int iD = B.child1;
                int iE = B.child2;
                int iF = C.child1;
                int iG = C.child2;

                B2TreeNode D = nodes[iD];
                B2TreeNode E = nodes[iE];
                B2TreeNode F = nodes[iF];
                B2TreeNode G = nodes[iG];

                Debug.Assert(0 <= iD && iD < tree.nodeCapacity);
                Debug.Assert(0 <= iE && iE < tree.nodeCapacity);
                Debug.Assert(0 <= iF && iF < tree.nodeCapacity);
                Debug.Assert(0 <= iG && iG < tree.nodeCapacity);

                // Base cost
                float areaB = b2Perimeter(B.aabb);
                float areaC = b2Perimeter(C.aabb);
                float costBase = areaB + areaC;
                B2RotateType bestRotation = B2RotateType.b2_rotateNone;
                float bestCost = costBase;

                // Cost of swapping B and F
                B2AABB aabbBG = b2AABB_Union(B.aabb, G.aabb);
                float costBF = areaB + b2Perimeter(aabbBG);
                if (costBF < bestCost)
                {
                    bestRotation = B2RotateType.b2_rotateBF;
                    bestCost = costBF;
                }

                // Cost of swapping B and G
                B2AABB aabbBF = b2AABB_Union(B.aabb, F.aabb);
                float costBG = areaB + b2Perimeter(aabbBF);
                if (costBG < bestCost)
                {
                    bestRotation = B2RotateType.b2_rotateBG;
                    bestCost = costBG;
                }

                // Cost of swapping C and D
                B2AABB aabbCE = b2AABB_Union(C.aabb, E.aabb);
                float costCD = areaC + b2Perimeter(aabbCE);
                if (costCD < bestCost)
                {
                    bestRotation = B2RotateType.b2_rotateCD;
                    bestCost = costCD;
                }

                // Cost of swapping C and E
                B2AABB aabbCD = b2AABB_Union(C.aabb, D.aabb);
                float costCE = areaC + b2Perimeter(aabbCD);
                if (costCE < bestCost)
                {
                    bestRotation = B2RotateType.b2_rotateCE;
                    // bestCost = costCE;
                }

                switch (bestRotation)
                {
                    case B2RotateType.b2_rotateNone:
                        break;

                    case B2RotateType.b2_rotateBF:
                        A.child1 = iF;
                        C.child1 = iB;

                        B.parent = iC;
                        F.parent = iA;

                        C.aabb = aabbBG;
                        C.height = (ushort)(1 + b2MaxUInt16(B.height, G.height));
                        A.height = (ushort)(1 + b2MaxUInt16(C.height, F.height));
                        C.categoryBits = B.categoryBits | G.categoryBits;
                        A.categoryBits = C.categoryBits | F.categoryBits;
                        C.flags |= (ushort)((B.flags | G.flags) & (int)B2TreeNodeFlags.b2_enlargedNode);
                        A.flags |= (ushort)((C.flags | F.flags) & (int)B2TreeNodeFlags.b2_enlargedNode);
                        break;

                    case B2RotateType.b2_rotateBG:
                        A.child1 = iG;
                        C.child2 = iB;

                        B.parent = iC;
                        G.parent = iA;

                        C.aabb = aabbBF;
                        C.height = (ushort)(1 + b2MaxUInt16(B.height, F.height));
                        A.height = (ushort)(1 + b2MaxUInt16(C.height, G.height));
                        C.categoryBits = B.categoryBits | F.categoryBits;
                        A.categoryBits = C.categoryBits | G.categoryBits;
                        C.flags |= (ushort)((B.flags | F.flags) & (int)B2TreeNodeFlags.b2_enlargedNode);
                        A.flags |= (ushort)((C.flags | G.flags) & (int)B2TreeNodeFlags.b2_enlargedNode);
                        break;

                    case B2RotateType.b2_rotateCD:
                        A.child2 = iD;
                        B.child1 = iC;

                        C.parent = iB;
                        D.parent = iA;

                        B.aabb = aabbCE;
                        B.height = (ushort)(1 + b2MaxUInt16(C.height, E.height));
                        A.height = (ushort)(1 + b2MaxUInt16(B.height, D.height));
                        B.categoryBits = C.categoryBits | E.categoryBits;
                        A.categoryBits = B.categoryBits | D.categoryBits;
                        B.flags |= (ushort)((C.flags | E.flags) & (int)B2TreeNodeFlags.b2_enlargedNode);
                        A.flags |= (ushort)((B.flags | D.flags) & (int)B2TreeNodeFlags.b2_enlargedNode);
                        break;

                    case B2RotateType.b2_rotateCE:
                        A.child2 = iE;
                        B.child2 = iC;

                        C.parent = iB;
                        E.parent = iA;

                        B.aabb = aabbCD;
                        B.height = (ushort)(1 + b2MaxUInt16(C.height, D.height));
                        A.height = (ushort)(1 + b2MaxUInt16(B.height, E.height));
                        B.categoryBits = C.categoryBits | D.categoryBits;
                        A.categoryBits = B.categoryBits | E.categoryBits;
                        B.flags |= (ushort)((C.flags | D.flags) & (int)B2TreeNodeFlags.b2_enlargedNode);
                        A.flags |= (ushort)((B.flags | E.flags) & (int)B2TreeNodeFlags.b2_enlargedNode);
                        break;

                    default:
                        Debug.Assert(false);
                        break;
                }
            }
        }

        public static void b2InsertLeaf(B2DynamicTree tree, int leaf, bool shouldRotate)
        {
            if (tree.root == B2_NULL_INDEX)
            {
                tree.root = leaf;
                tree.nodes[tree.root].parent = B2_NULL_INDEX;
                return;
            }

            // Stage 1: find the best sibling for this node
            B2AABB leafAABB = tree.nodes[leaf].aabb;
            int sibling = b2FindBestSibling(tree, leafAABB);

            // Stage 2: create a new parent for the leaf and sibling
            int oldParent = tree.nodes[sibling].parent;
            int newParent = b2AllocateNode(tree);

            // warning: node pointer can change after allocation
            B2TreeNode[] nodes = tree.nodes;
            nodes[newParent].parent = oldParent;
            nodes[newParent].userData = -1;
            nodes[newParent].aabb = b2AABB_Union(leafAABB, nodes[sibling].aabb);
            nodes[newParent].categoryBits = nodes[leaf].categoryBits | nodes[sibling].categoryBits;
            nodes[newParent].height = (ushort)(nodes[sibling].height + 1);

            if (oldParent != B2_NULL_INDEX)
            {
                // The sibling was not the root.
                if (nodes[oldParent].child1 == sibling)
                {
                    nodes[oldParent].child1 = newParent;
                }
                else
                {
                    nodes[oldParent].child2 = newParent;
                }

                nodes[newParent].child1 = sibling;
                nodes[newParent].child2 = leaf;
                nodes[sibling].parent = newParent;
                nodes[leaf].parent = newParent;
            }
            else
            {
                // The sibling was the root.
                nodes[newParent].child1 = sibling;
                nodes[newParent].child2 = leaf;
                nodes[sibling].parent = newParent;
                nodes[leaf].parent = newParent;
                tree.root = newParent;
            }

            // Stage 3: walk back up the tree fixing heights and AABBs
            int index = nodes[leaf].parent;
            while (index != B2_NULL_INDEX)
            {
                int child1 = nodes[index].child1;
                int child2 = nodes[index].child2;

                Debug.Assert(child1 != B2_NULL_INDEX);
                Debug.Assert(child2 != B2_NULL_INDEX);

                nodes[index].aabb = b2AABB_Union(nodes[child1].aabb, nodes[child2].aabb);
                nodes[index].categoryBits = nodes[child1].categoryBits | nodes[child2].categoryBits;
                nodes[index].height = (ushort)(1 + b2MaxUInt16(nodes[child1].height, nodes[child2].height));
                nodes[index].flags |= (ushort)((nodes[child1].flags | nodes[child2].flags) & (int)B2TreeNodeFlags.b2_enlargedNode);

                if (shouldRotate)
                {
                    b2RotateNodes(tree, index);
                }

                index = nodes[index].parent;
            }
        }

        public static void b2RemoveLeaf(B2DynamicTree tree, int leaf)
        {
            if (leaf == tree.root)
            {
                tree.root = B2_NULL_INDEX;
                return;
            }

            B2TreeNode[] nodes = tree.nodes;

            int parent = nodes[leaf].parent;
            int grandParent = nodes[parent].parent;
            int sibling;
            if (nodes[parent].child1 == leaf)
            {
                sibling = nodes[parent].child2;
            }
            else
            {
                sibling = nodes[parent].child1;
            }

            if (grandParent != B2_NULL_INDEX)
            {
                // Destroy parent and connect sibling to grandParent.
                if (nodes[grandParent].child1 == parent)
                {
                    nodes[grandParent].child1 = sibling;
                }
                else
                {
                    nodes[grandParent].child2 = sibling;
                }

                nodes[sibling].parent = grandParent;
                b2FreeNode(tree, parent);

                // Adjust ancestor bounds.
                int index = grandParent;
                while (index != B2_NULL_INDEX)
                {
                    B2TreeNode node = nodes[index];
                    B2TreeNode child1 = nodes[node.child1];
                    B2TreeNode child2 = nodes[node.child2];

                    // Fast union using SSE
                    //__m128 aabb1 = _mm_load_ps(&child1.aabb.lowerBound.x);
                    //__m128 aabb2 = _mm_load_ps(&child2.aabb.lowerBound.x);
                    //__m128 lower = _mm_min_ps(aabb1, aabb2);
                    //__m128 upper = _mm_max_ps(aabb1, aabb2);
                    //__m128 aabb = _mm_shuffle_ps(lower, upper, _MM_SHUFFLE(3, 2, 1, 0));
                    //_mm_store_ps(&node.aabb.lowerBound.x, aabb);

                    node.aabb = b2AABB_Union(child1.aabb, child2.aabb);
                    node.categoryBits = child1.categoryBits | child2.categoryBits;
                    node.height = (ushort)(1 + b2MaxUInt16(child1.height, child2.height));

                    index = node.parent;
                }
            }
            else
            {
                tree.root = sibling;
                tree.nodes[sibling].parent = B2_NULL_INDEX;
                b2FreeNode(tree, parent);
            }
        }

        /// Create a proxy. Provide an AABB and a userData value.
        // Create a proxy in the tree as a leaf node. We return the index of the node instead of a pointer so that we can grow
        // the node pool.
        public static int b2DynamicTree_CreateProxy(B2DynamicTree tree, B2AABB aabb, ulong categoryBits, int userData)
        {
            Debug.Assert(-B2_HUGE < aabb.lowerBound.x && aabb.lowerBound.x < B2_HUGE);
            Debug.Assert(-B2_HUGE < aabb.lowerBound.y && aabb.lowerBound.y < B2_HUGE);
            Debug.Assert(-B2_HUGE < aabb.upperBound.x && aabb.upperBound.x < B2_HUGE);
            Debug.Assert(-B2_HUGE < aabb.upperBound.y && aabb.upperBound.y < B2_HUGE);

            int proxyId = b2AllocateNode(tree);
            B2TreeNode node = tree.nodes[proxyId];

            node.aabb = aabb;
            node.userData = userData;
            node.categoryBits = categoryBits;
            node.height = 0;
            node.flags = (ushort)(B2TreeNodeFlags.b2_allocatedNode | B2TreeNodeFlags.b2_leafNode);

            bool shouldRotate = true;
            b2InsertLeaf(tree, proxyId, shouldRotate);

            tree.proxyCount += 1;

            return proxyId;
        }

        /// Destroy a proxy. This asserts if the id is invalid.
        public static void b2DynamicTree_DestroyProxy(B2DynamicTree tree, int proxyId)
        {
            Debug.Assert(0 <= proxyId && proxyId < tree.nodeCapacity);
            Debug.Assert(b2IsLeaf(tree.nodes[proxyId]));

            b2RemoveLeaf(tree, proxyId);
            b2FreeNode(tree, proxyId);

            Debug.Assert(tree.proxyCount > 0);
            tree.proxyCount -= 1;
        }

        /// Get the number of proxies created
        public static int b2DynamicTree_GetProxyCount(B2DynamicTree tree)
        {
            return tree.proxyCount;
        }

        /// Move a proxy to a new AABB by removing and reinserting into the tree.
        public static void b2DynamicTree_MoveProxy(B2DynamicTree tree, int proxyId, B2AABB aabb)
        {
            Debug.Assert(b2IsValidAABB(aabb));
            Debug.Assert(aabb.upperBound.x - aabb.lowerBound.x < B2_HUGE);
            Debug.Assert(aabb.upperBound.y - aabb.lowerBound.y < B2_HUGE);
            Debug.Assert(0 <= proxyId && proxyId < tree.nodeCapacity);
            Debug.Assert(b2IsLeaf(tree.nodes[proxyId]));

            b2RemoveLeaf(tree, proxyId);

            tree.nodes[proxyId].aabb = aabb;

            bool shouldRotate = false;
            b2InsertLeaf(tree, proxyId, shouldRotate);
        }

        /// Enlarge a proxy and enlarge ancestors as necessary.
        public static void b2DynamicTree_EnlargeProxy(B2DynamicTree tree, int proxyId, B2AABB aabb)
        {
            B2TreeNode[] nodes = tree.nodes;

            Debug.Assert(b2IsValidAABB(aabb));
            Debug.Assert(aabb.upperBound.x - aabb.lowerBound.x < B2_HUGE);
            Debug.Assert(aabb.upperBound.y - aabb.lowerBound.y < B2_HUGE);
            Debug.Assert(0 <= proxyId && proxyId < tree.nodeCapacity);
            Debug.Assert(b2IsLeaf(tree.nodes[proxyId]));

            // Caller must ensure this
            Debug.Assert(b2AABB_Contains(nodes[proxyId].aabb, aabb) == false);

            nodes[proxyId].aabb = aabb;

            int parentIndex = nodes[proxyId].parent;
            while (parentIndex != B2_NULL_INDEX)
            {
                bool changed = b2EnlargeAABB(ref nodes[parentIndex].aabb, aabb);
                nodes[parentIndex].flags |= (int)B2TreeNodeFlags.b2_enlargedNode;
                parentIndex = nodes[parentIndex].parent;

                if (changed == false)
                {
                    break;
                }
            }

            while (parentIndex != B2_NULL_INDEX)
            {
                if (0 != (nodes[parentIndex].flags & (int)B2TreeNodeFlags.b2_enlargedNode))
                {
                    // early out because this ancestor was previously ascended and marked as enlarged
                    break;
                }

                nodes[parentIndex].flags |= (int)B2TreeNodeFlags.b2_enlargedNode;
                parentIndex = nodes[parentIndex].parent;
            }
        }

        /// Get the height of the binary tree.
        public static int b2DynamicTree_GetHeight(B2DynamicTree tree)
        {
            if (tree.root == B2_NULL_INDEX)
            {
                return 0;
            }

            return tree.nodes[tree.root].height;
        }

        /// Get the ratio of the sum of the node areas to the root area.
        public static float b2DynamicTree_GetAreaRatio(B2DynamicTree tree)
        {
            if (tree.root == B2_NULL_INDEX)
            {
                return 0.0f;
            }

            B2TreeNode root = tree.nodes[tree.root];
            float rootArea = b2Perimeter(root.aabb);

            float totalArea = 0.0f;
            for (int i = 0; i < tree.nodeCapacity; ++i)
            {
                B2TreeNode node = tree.nodes[i];
                if (b2IsAllocated(node) == false || b2IsLeaf(node) || i == tree.root)
                {
                    continue;
                }

                totalArea += b2Perimeter(node.aabb);
            }

            return totalArea / rootArea;
        }

        // Compute the height of a sub-tree.
        public static int b2ComputeHeight(B2DynamicTree tree, int nodeId)
        {
            Debug.Assert(0 <= nodeId && nodeId < tree.nodeCapacity);
            B2TreeNode node = tree.nodes[nodeId];

            if (b2IsLeaf(node))
            {
                return 0;
            }

            int height1 = b2ComputeHeight(tree, node.child1);
            int height2 = b2ComputeHeight(tree, node.child2);
            return 1 + b2MaxInt(height1, height2);
        }

#if B2_VALIDATE
        public static void b2ValidateStructure(B2DynamicTree tree, int index)
        {
            if (index == B2_NULL_INDEX)
            {
                return;
            }

            if (index == tree.root)
            {
                Debug.Assert(tree.nodes[index].parent == B2_NULL_INDEX);
            }

            B2TreeNode node = tree.nodes[index];

            Debug.Assert(node.flags == 0 || (node.flags & (ushort)B2TreeNodeFlags.b2_allocatedNode) != 0);

            if (b2IsLeaf(node))
            {
                Debug.Assert(node.height == 0);
                return;
            }

            int child1 = node.child1;
            int child2 = node.child2;

            Debug.Assert(0 <= child1 && child1 < tree.nodeCapacity);
            Debug.Assert(0 <= child2 && child2 < tree.nodeCapacity);

            Debug.Assert(tree.nodes[child1].parent == index);
            Debug.Assert(tree.nodes[child2].parent == index);

            if (0 != ((tree.nodes[child1].flags | tree.nodes[child2].flags) & (ushort)B2TreeNodeFlags.b2_enlargedNode))
            {
                Debug.Assert(0 != (node.flags & (ushort)B2TreeNodeFlags.b2_enlargedNode));
            }

            b2ValidateStructure(tree, child1);
            b2ValidateStructure(tree, child2);
        }

        public static void b2ValidateMetrics(B2DynamicTree tree, int index)
        {
            if (index == B2_NULL_INDEX)
            {
                return;
            }

            B2TreeNode node = tree.nodes[index];

            if (b2IsLeaf(node))
            {
                Debug.Assert(node.height == 0);
                return;
            }

            int child1 = node.child1;
            int child2 = node.child2;

            Debug.Assert(0 <= child1 && child1 < tree.nodeCapacity);
            Debug.Assert(0 <= child2 && child2 < tree.nodeCapacity);

            int height1 = tree.nodes[child1].height;
            int height2 = tree.nodes[child2].height;
            int height = 1 + b2MaxInt(height1, height2);
            Debug.Assert(node.height == height);

            // b2AABB aabb = b2AABB_Union(tree.nodes[child1].aabb, tree.nodes[child2].aabb);

            Debug.Assert(b2AABB_Contains(node.aabb, tree.nodes[child1].aabb));
            Debug.Assert(b2AABB_Contains(node.aabb, tree.nodes[child2].aabb));

            // Debug.Assert(aabb.lowerBound.x == node.aabb.lowerBound.x);
            // Debug.Assert(aabb.lowerBound.y == node.aabb.lowerBound.y);
            // Debug.Assert(aabb.upperBound.x == node.aabb.upperBound.x);
            // Debug.Assert(aabb.upperBound.y == node.aabb.upperBound.y);

            ulong categoryBits = tree.nodes[child1].categoryBits | tree.nodes[child2].categoryBits;
            Debug.Assert(node.categoryBits == categoryBits);

            b2ValidateMetrics(tree, child1);
            b2ValidateMetrics(tree, child2);
        }
#endif

        /// Validate this tree. For testing.
        public static void b2DynamicTree_Validate(B2DynamicTree tree)
        {
#if B2_VALIDATE
            if (tree.root == B2_NULL_INDEX)
            {
                return;
            }

            b2ValidateStructure(tree, tree.root);
            b2ValidateMetrics(tree, tree.root);

            int freeCount = 0;
            int freeIndex = tree.freeList;
            while (freeIndex != B2_NULL_INDEX)
            {
                Debug.Assert(0 <= freeIndex && freeIndex < tree.nodeCapacity);
                freeIndex = tree.nodes[freeIndex].next;
                ++freeCount;
            }

            int height = b2DynamicTree_GetHeight(tree);
            int computedHeight = b2ComputeHeight(tree, tree.root);
            Debug.Assert(height == computedHeight);

            Debug.Assert(tree.nodeCount + freeCount == tree.nodeCapacity);
#else
            B2_UNUSED(tree);
#endif
        }

        /// Validate this tree has no enlarged AABBs. For testing.
        public static void b2DynamicTree_ValidateNoEnlarged(B2DynamicTree tree)
        {
#if B2_VALIDATE
            int capacity = tree.nodeCapacity;
            B2TreeNode[] nodes = tree.nodes;
            for (int i = 0; i < capacity; ++i)
            {
                B2TreeNode node = nodes[i];
                if (0 != (node.flags & (ushort)B2TreeNodeFlags.b2_allocatedNode))
                {
                    if ((node.flags & (ushort)B2TreeNodeFlags.b2_enlargedNode) != 0)
                    {
                        int a = 3;
                    }
                    Debug.Assert((node.flags & (ushort)B2TreeNodeFlags.b2_enlargedNode) == 0);
                }
            }
#else
            B2_UNUSED(tree);
#endif
        }

        /// Get the number of bytes used by this tree
        public static int b2DynamicTree_GetByteCount(B2DynamicTree tree)
        {
            // TODO: @ikpil, check
            // int size = sizeof( b2DynamicTree ) + sizeof( b2TreeNode ) * tree.nodeCapacity +
            //               tree.rebuildCapacity * ( sizeof( int ) + sizeof( b2AABB ) + sizeof( B2Vec2 ) + sizeof( int ) );
            //return (int)size;
            return -1;
        }

        /// Get proxy user data
        public static int b2DynamicTree_GetUserData(B2DynamicTree tree, int proxyId)
        {
            return tree.nodes[proxyId].userData;
        }

        /// Get the AABB of a proxy
        public static B2AABB b2DynamicTree_GetAABB(B2DynamicTree tree, int proxyId)
        {
            return tree.nodes[proxyId].aabb;
        }


        /// Query an AABB for overlapping proxies. The callback class is called for each proxy that overlaps the supplied AABB.
        /// @return performance data
        public static B2TreeStats b2DynamicTree_Query(B2DynamicTree tree, B2AABB aabb, ulong maskBits, b2TreeQueryCallbackFcn callback, object context)
        {
            B2TreeStats result = new B2TreeStats();

            if (tree.nodeCount == 0)
            {
                return result;
            }

            int[] stack = new int[B2_TREE_STACK_SIZE];
            int stackCount = 0;
            stack[stackCount++] = tree.root;

            while (stackCount > 0)
            {
                int nodeId = stack[--stackCount];
                if (nodeId == B2_NULL_INDEX)
                {
                    // todo huh?
                    Debug.Assert(false);
                    continue;
                }

                B2TreeNode node = tree.nodes[nodeId];
                result.nodeVisits += 1;

                if (b2AABB_Overlaps(node.aabb, aabb) && (node.categoryBits & maskBits) != 0)
                {
                    if (b2IsLeaf(node))
                    {
                        // callback to user code with proxy id
                        bool proceed = callback(nodeId, node.userData, context);
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
                            stack[stackCount++] = node.child1;
                            stack[stackCount++] = node.child2;
                        }
                        else
                        {
                            Debug.Assert(stackCount < B2_TREE_STACK_SIZE - 1);
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
        /// However, this filtering may be approximate, so the user should still apply filtering to results.
        /// @param tree the dynamic tree to ray cast
        /// @param input the ray cast input data. The ray extends from p1 to p1 + maxFraction * (p2 - p1)
        /// @param maskBits mask bit hint: `bool accept = (maskBits & node->categoryBits) != 0;`
        /// @param callback a callback class that is called for each proxy that is hit by the ray
        /// @param context user context that is passed to the callback
        /// @return performance data
        public static B2TreeStats b2DynamicTree_RayCast(B2DynamicTree tree, B2RayCastInput input, ulong maskBits, b2TreeRayCastCallbackFcn callback, object context)
        {
            B2TreeStats result = new B2TreeStats();

            if (tree.nodeCount == 0)
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

            int[] stack = new int[B2_TREE_STACK_SIZE];
            int stackCount = 0;
            stack[stackCount++] = tree.root;

            B2TreeNode[] nodes = tree.nodes;

            B2RayCastInput subInput = input;

            while (stackCount > 0)
            {
                int nodeId = stack[--stackCount];
                if (nodeId == B2_NULL_INDEX)
                {
                    // todo is this possible?
                    Debug.Assert(false);
                    continue;
                }

                B2TreeNode node = nodes[nodeId];
                result.nodeVisits += 1;

                B2AABB nodeAABB = node.aabb;

                if ((node.categoryBits & maskBits) == 0 || b2AABB_Overlaps(nodeAABB, segmentAABB) == false)
                {
                    continue;
                }

                // Separating axis for segment (Gino, p80).
                // |dot(v, p1 - c)| > dot(|v|, h)
                // radius extension is added to the node in this case
                B2Vec2 c = b2AABB_Center(nodeAABB);
                B2Vec2 h = b2AABB_Extents(nodeAABB);
                float term1 = b2AbsFloat(b2Dot(v, b2Sub(p1, c)));
                float term2 = b2Dot(abs_v, h);
                if (term2 < term1)
                {
                    continue;
                }

                if (b2IsLeaf(node))
                {
                    subInput.maxFraction = maxFraction;

                    float value = callback(subInput, nodeId, node.userData, context);
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
                    }
                }
                else
                {
                    if (stackCount < B2_TREE_STACK_SIZE - 1)
                    {
                        B2Vec2 c1 = b2AABB_Center(nodes[node.child1].aabb);
                        B2Vec2 c2 = b2AABB_Center(nodes[node.child2].aabb);
                        if (b2DistanceSquared(c1, p1) < b2DistanceSquared(c2, p1))
                        {
                            stack[stackCount++] = node.child2;
                            stack[stackCount++] = node.child1;
                        }
                        else
                        {
                            stack[stackCount++] = node.child1;
                            stack[stackCount++] = node.child2;
                        }
                    }
                    else
                    {
                        Debug.Assert(stackCount < B2_TREE_STACK_SIZE - 1);
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
        /// @param tree the dynamic tree to ray cast
        /// @param input the ray cast input data. The ray extends from p1 to p1 + maxFraction * (p2 - p1).
        /// @param maskBits filter bits: `bool accept = (maskBits & node->categoryBits) != 0;`
        /// @param callback a callback class that is called for each proxy that is hit by the shape
        /// @param context user context that is passed to the callback
        /// @return performance data
        public static B2TreeStats b2DynamicTree_ShapeCast(B2DynamicTree tree, B2ShapeCastInput input, ulong maskBits, b2TreeShapeCastCallbackFcn callback, object context)
        {
            B2TreeStats stats = new B2TreeStats();

            if (tree.nodeCount == 0 || input.count == 0)
            {
                return stats;
            }

            B2AABB originAABB = new B2AABB(input.points[0], input.points[0]);
            for (int i = 1; i < input.count; ++i)
            {
                originAABB.lowerBound = b2Min(originAABB.lowerBound, input.points[i]);
                originAABB.upperBound = b2Max(originAABB.upperBound, input.points[i]);
            }

            B2Vec2 radius = new B2Vec2(input.radius, input.radius);

            originAABB.lowerBound = b2Sub(originAABB.lowerBound, radius);
            originAABB.upperBound = b2Add(originAABB.upperBound, radius);

            B2Vec2 p1 = b2AABB_Center(originAABB);
            B2Vec2 extension = b2AABB_Extents(originAABB);

            // v is perpendicular to the segment.
            B2Vec2 r = input.translation;
            B2Vec2 v = b2CrossSV(1.0f, r);
            B2Vec2 abs_v = b2Abs(v);

            // Separating axis for segment (Gino, p80).
            // |dot(v, p1 - c)| > dot(|v|, h)

            float maxFraction = input.maxFraction;

            // Build total box for the shape cast
            B2Vec2 t = b2MulSV(maxFraction, input.translation);
            B2AABB totalAABB = new B2AABB
            {
                lowerBound = b2Min(originAABB.lowerBound, b2Add(originAABB.lowerBound, t)),
                upperBound = b2Max(originAABB.upperBound, b2Add(originAABB.upperBound, t)),
            };

            //b2ShapeCastInput subInput = *input;
            B2ShapeCastInput subInput = input;
            B2TreeNode[] nodes = tree.nodes;

            int[] stack = new int[B2_TREE_STACK_SIZE];
            int stackCount = 0;
            stack[stackCount++] = tree.root;

            while (stackCount > 0)
            {
                int nodeId = stack[--stackCount];
                if (nodeId == B2_NULL_INDEX)
                {
                    // todo is this possible?
                    Debug.Assert(false);
                    continue;
                }

                B2TreeNode node = nodes[nodeId];
                stats.nodeVisits += 1;

                if ((node.categoryBits & maskBits) == 0 || b2AABB_Overlaps(node.aabb, totalAABB) == false)
                {
                    continue;
                }

                // Separating axis for segment (Gino, p80).
                // |dot(v, p1 - c)| > dot(|v|, h)
                // radius extension is added to the node in this case
                B2Vec2 c = b2AABB_Center(node.aabb);
                B2Vec2 h = b2Add(b2AABB_Extents(node.aabb), extension);
                float term1 = b2AbsFloat(b2Dot(v, b2Sub(p1, c)));
                float term2 = b2Dot(abs_v, h);
                if (term2 < term1)
                {
                    continue;
                }

                if (b2IsLeaf(node))
                {
                    subInput.maxFraction = maxFraction;

                    float value = callback(subInput, nodeId, node.userData, context);
                    stats.leafVisits += 1;

                    if (value == 0.0f)
                    {
                        // The client has terminated the ray cast.
                        return stats;
                    }

                    if (0.0f < value && value < maxFraction)
                    {
                        // Update segment bounding box.
                        maxFraction = value;
                        t = b2MulSV(maxFraction, input.translation);
                        totalAABB.lowerBound = b2Min(originAABB.lowerBound, b2Add(originAABB.lowerBound, t));
                        totalAABB.upperBound = b2Max(originAABB.upperBound, b2Add(originAABB.upperBound, t));
                    }
                }
                else
                {
                    if (stackCount < B2_TREE_STACK_SIZE - 1)
                    {
                        B2Vec2 c1 = b2AABB_Center(nodes[node.child1].aabb);
                        B2Vec2 c2 = b2AABB_Center(nodes[node.child2].aabb);
                        if (b2DistanceSquared(c1, p1) < b2DistanceSquared(c2, p1))
                        {
                            stack[stackCount++] = node.child2;
                            stack[stackCount++] = node.child1;
                        }
                        else
                        {
                            stack[stackCount++] = node.child1;
                            stack[stackCount++] = node.child2;
                        }
                    }
                    else
                    {
                        Debug.Assert(stackCount < B2_TREE_STACK_SIZE - 1);
                    }
                }
            }

            return stats;
        }

        // // Median split == 0, Surface area heuristic == 1
        // #define B2_TREE_HEURISTIC 0
        //
        // #if B2_TREE_HEURISTIC == 0

        // Median split heuristic
        public static int b2PartitionMid(Span<int> indices, Span<B2Vec2> centers, int count)
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
            B2Vec2 c = new B2Vec2(0.5f * (lowerBound.x + upperBound.x), 0.5f * (lowerBound.y + upperBound.y));

            // Partition longest axis using the Hoare partition scheme
            // https://en.wikipedia.org/wiki/Quicksort
            // https://nicholasvadivelu.com/2021/01/11/array-partition/
            int i1 = 0, i2 = count;
            if (d.x > d.y)
            {
                float pivot = c.x;

                while (i1 < i2)
                {
                    while (i1 < i2 && centers[i1].x < pivot)
                    {
                        i1 += 1;
                    }

                    ;

                    while (i1 < i2 && centers[i2 - 1].x >= pivot)
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
                float pivot = c.y;

                while (i1 < i2)
                {
                    while (i1 < i2 && centers[i1].y < pivot)
                    {
                        i1 += 1;
                    }

                    ;

                    while (i1 < i2 && centers[i2 - 1].y >= pivot)
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

            Debug.Assert(i1 == i2);

            if (i1 > 0 && i1 < count)
            {
                return i1;
            }

            return count / 2;
        }

        //#else

        public const int B2_BIN_COUNT = 64;


        // "On Fast Construction of SAH-based Bounding Volume Hierarchies" by Ingo Wald
        // Returns the left child count
        public static int b2PartitionSAH(int[] indices, int[] binIndices, B2AABB[] boxes, int count)
        {
            Debug.Assert(count > 0);

            B2TreeBin[] bins = new B2TreeBin[B2_BIN_COUNT];
            B2TreePlane[] planes = new B2TreePlane[B2_BIN_COUNT - 1];

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
            if (d.x > d.y)
            {
                axisIndex = 0;
                invD = d.x;
            }
            else
            {
                axisIndex = 1;
                invD = d.y;
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
            float[] lowerBoundArray = new float[2] { centroidAABB.lowerBound.x, centroidAABB.lowerBound.y };
            float minC = lowerBoundArray[axisIndex];
            for (int i = 0; i < count; ++i)
            {
                B2Vec2 c = b2AABB_Center(boxes[i]);
                float[] cArray = new float[2] { c.x, c.y };
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

            Debug.Assert(i1 == i2);

            if (i1 > 0 && i1 < count)
            {
                return i1;
            }
            else
            {
                return count / 2;
            }
        }


        // Returns root node index
        public static int b2BuildTree(B2DynamicTree tree, int leafCount)
        {
            B2TreeNode[] nodes = tree.nodes;
            int[] leafIndices = tree.leafIndices;

            if (leafCount == 1)
            {
                nodes[leafIndices[0]].parent = B2_NULL_INDEX;
                return leafIndices[0];
            }

            //#if B2_TREE_HEURISTIC == 0
            B2Vec2[] leafCenters = tree.leafCenters;
            // #else
            //     b2AABB* leafBoxes = tree.leafBoxes;
            //     int* binIndices = tree.binIndices;
            // #endif

            // todo large stack item
            B2RebuildItem[] stack = b2Alloc<B2RebuildItem>(B2_TREE_STACK_SIZE);
            int top = 0;

            stack[0].nodeIndex = b2AllocateNode(tree);
            stack[0].childCount = -1;
            stack[0].startIndex = 0;
            stack[0].endIndex = leafCount;
            //#if B2_TREE_HEURISTIC == 0
            stack[0].splitIndex = b2PartitionMid(leafIndices, leafCenters, leafCount);
            // #else
            //     stack[0].splitIndex = b2PartitionSAH( leafIndices, binIndices, leafBoxes, leafCount );
            // #endif

            while (true)
            {
                B2RebuildItem item = stack[top];

                item.childCount += 1;

                if (item.childCount == 2)
                {
                    // This internal node has both children established

                    if (top == 0)
                    {
                        // all done
                        break;
                    }

                    B2RebuildItem parentItem = stack[(top - 1)];
                    B2TreeNode parentNode = nodes[parentItem.nodeIndex];

                    if (parentItem.childCount == 0)
                    {
                        Debug.Assert(parentNode.child1 == B2_NULL_INDEX);
                        parentNode.child1 = item.nodeIndex;
                    }
                    else
                    {
                        Debug.Assert(parentItem.childCount == 1);
                        Debug.Assert(parentNode.child2 == B2_NULL_INDEX);
                        parentNode.child2 = item.nodeIndex;
                    }

                    B2TreeNode node = nodes[item.nodeIndex];

                    Debug.Assert(node.parent == B2_NULL_INDEX);
                    node.parent = parentItem.nodeIndex;

                    Debug.Assert(node.child1 != B2_NULL_INDEX);
                    Debug.Assert(node.child2 != B2_NULL_INDEX);
                    B2TreeNode c1 = nodes[node.child1];
                    B2TreeNode c2 = nodes[node.child2];

                    node.aabb = b2AABB_Union(c1.aabb, c2.aabb);
                    node.height = (ushort)(1 + b2MaxUInt16(c1.height, c2.height));
                    node.categoryBits = c1.categoryBits | c2.categoryBits;

                    // Pop stack
                    top -= 1;
                }
                else
                {
                    int startIndex, endIndex;
                    if (item.childCount == 0)
                    {
                        startIndex = item.startIndex;
                        endIndex = item.splitIndex;
                    }
                    else
                    {
                        Debug.Assert(item.childCount == 1);
                        startIndex = item.splitIndex;
                        endIndex = item.endIndex;
                    }

                    int count = endIndex - startIndex;

                    if (count == 1)
                    {
                        int childIndex = leafIndices[startIndex];
                        B2TreeNode node = nodes[item.nodeIndex];

                        if (item.childCount == 0)
                        {
                            Debug.Assert(node.child1 == B2_NULL_INDEX);
                            node.child1 = childIndex;
                        }
                        else
                        {
                            Debug.Assert(item.childCount == 1);
                            Debug.Assert(node.child2 == B2_NULL_INDEX);
                            node.child2 = childIndex;
                        }

                        B2TreeNode childNode = nodes[childIndex];
                        Debug.Assert(childNode.parent == B2_NULL_INDEX);
                        childNode.parent = item.nodeIndex;
                    }
                    else
                    {
                        Debug.Assert(count > 0);
                        Debug.Assert(top < B2_TREE_STACK_SIZE);

                        top += 1;
                        B2RebuildItem newItem = stack[top];
                        newItem.nodeIndex = b2AllocateNode(tree);
                        newItem.childCount = -1;
                        newItem.startIndex = startIndex;
                        newItem.endIndex = endIndex;
                        // #if B2_TREE_HEURISTIC == 0
                        newItem.splitIndex = b2PartitionMid(leafIndices.AsSpan(startIndex), leafCenters.AsSpan(startIndex), count);
                        // #else
                        //                 newItem.splitIndex =
                        //                     b2PartitionSAH( leafIndices + startIndex, binIndices + startIndex, leafBoxes + startIndex, count );
                        // #endif
                        newItem.splitIndex += startIndex;
                    }
                }
            }

            B2TreeNode rootNode = nodes[stack[0].nodeIndex];
            Debug.Assert(rootNode.parent == B2_NULL_INDEX);
            Debug.Assert(rootNode.child1 != B2_NULL_INDEX);
            Debug.Assert(rootNode.child2 != B2_NULL_INDEX);

            B2TreeNode child1 = nodes[rootNode.child1];
            B2TreeNode child2 = nodes[rootNode.child2];

            rootNode.aabb = b2AABB_Union(child1.aabb, child2.aabb);
            rootNode.height = (ushort)(1 + b2MaxUInt16(child1.height, child2.height));
            rootNode.categoryBits = child1.categoryBits | child2.categoryBits;

            return stack[0].nodeIndex;
        }

        /// Rebuild the tree while retaining subtrees that haven't changed. Returns the number of boxes sorted.
        // Not safe to access tree during this operation because it may grow
        public static int b2DynamicTree_Rebuild(B2DynamicTree tree, bool fullBuild)
        {
            int proxyCount = tree.proxyCount;
            if (proxyCount == 0)
            {
                return 0;
            }

            // Ensure capacity for rebuild space
            if (proxyCount > tree.rebuildCapacity)
            {
                int newCapacity = proxyCount + proxyCount / 2;

                b2Free(tree.leafIndices, tree.rebuildCapacity);
                tree.leafIndices = b2Alloc<int>(newCapacity);

                //#if B2_TREE_HEURISTIC == 0
                b2Free(tree.leafCenters, tree.rebuildCapacity);
                tree.leafCenters = b2Alloc<B2Vec2>(newCapacity);
                // #else
                //         b2Free( tree.leafBoxes, tree.rebuildCapacity * sizeof( b2AABB ) );
                //         tree.leafBoxes = b2Alloc( newCapacity * sizeof( b2AABB ) );
                //         b2Free( tree.binIndices, tree.rebuildCapacity * sizeof( int ) );
                //         tree.binIndices = b2Alloc( newCapacity * sizeof( int ) );
                // #endif
                tree.rebuildCapacity = newCapacity;
            }

            int leafCount = 0;
            int[] stack = new int[B2_TREE_STACK_SIZE];
            int stackCount = 0;

            int nodeIndex = tree.root;
            B2TreeNode[] nodes = tree.nodes;
            B2TreeNode node = nodes[nodeIndex];

            // These are the nodes that get sorted to rebuild the tree.
            // I'm using indices because the node pool may grow during the build.
            int[] leafIndices = tree.leafIndices;

            // #if B2_TREE_HEURISTIC == 0
            B2Vec2[] leafCenters = tree.leafCenters;
            // #else
            //     b2AABB* leafBoxes = tree.leafBoxes;
            // #endif

            // Gather all proxy nodes that have grown and all internal nodes that haven't grown. Both are
            // considered leaves in the tree rebuild.
            // Free all internal nodes that have grown.
            // todo use a node growth metric instead of simply enlarged to reduce rebuild size and frequency
            // this should be weighed against B2_AABB_MARGIN
            while (true)
            {
                if (node.height == 0 || ((node.flags & (int)B2TreeNodeFlags.b2_enlargedNode) == 0 && fullBuild == false))
                {
                    leafIndices[leafCount] = nodeIndex;
                    //#if B2_TREE_HEURISTIC == 0
                    leafCenters[leafCount] = b2AABB_Center(node.aabb);
                    // #else
                    //             leafBoxes[leafCount] = node.aabb;
                    // #endif
                    leafCount += 1;

                    // Detach
                    node.parent = B2_NULL_INDEX;
                }
                else
                {
                    int doomedNodeIndex = nodeIndex;

                    // Handle children
                    nodeIndex = node.child1;

                    if (stackCount < B2_TREE_STACK_SIZE)
                    {
                        stack[stackCount++] = node.child2;
                    }
                    else
                    {
                        Debug.Assert(stackCount < B2_TREE_STACK_SIZE);
                    }

                    node = nodes[nodeIndex];

                    // Remove doomed node
                    b2FreeNode(tree, doomedNodeIndex);

                    continue;
                }

                if (stackCount == 0)
                {
                    break;
                }

                nodeIndex = stack[--stackCount];
                node = nodes[nodeIndex];
            }

#if B2_VALIDATE
            int capacity = tree.nodeCapacity;
            for (int i = 0; i < capacity; ++i)
            {
                if (0 != (nodes[i].flags & (ushort)B2TreeNodeFlags.b2_allocatedNode))
                {
                    Debug.Assert((nodes[i].flags & (ushort)B2TreeNodeFlags.b2_enlargedNode) == 0);
                }
            }
#endif

            Debug.Assert(leafCount <= proxyCount);

            tree.root = b2BuildTree(tree, leafCount);

            b2DynamicTree_Validate(tree);

            return leafCount;
        }
    }
}
