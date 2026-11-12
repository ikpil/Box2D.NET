// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;
using static Box2D.NET.B2Tables;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2DynamicTrees;
using static Box2D.NET.B2CTZs;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Contacts;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2ArenaAllocators;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2ParallelFors;
using static Box2D.NET.B2Profiling;

namespace Box2D.NET
{
    public static class B2BroadPhases
    {
        public const int B2_CANDIDATE_BATCH = 32;
        public const int B2_CROSS_SEED_COUNT = 64;

        // Store the proxy type in the lower 2 bits of the proxy key. This leaves 30 bits for the id.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2BodyType B2_PROXY_TYPE(int KEY)
        {
            return ((B2BodyType)((KEY) & 3));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int B2_PROXY_ID(int KEY)
        {
            return ((KEY) >> 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int B2_PROXY_KEY(int ID, B2BodyType TYPE)
        {
            return (ID << 2) | (int)TYPE;
        }

        public static void b2CreateBroadPhase(ref B2BroadPhase bp, in B2Capacity capacity)
        {
            B2_ASSERT((int)B2BodyType.b2_bodyTypeCount == 3, "must be three body types");

            bp = new B2BroadPhase();
            bp.trees = new B2DynamicTree[(int)B2BodyType.b2_bodyTypeCount];

            bp.pairSet = b2CreateSet(b2MaxInt(32, 2 * capacity.contactCount));

            int staticCapacity = b2MaxInt(16, capacity.staticShapeCount);
            bp.trees[(int)B2BodyType.b2_staticBody] = new B2DynamicTree();
            b2CreateDynamicTreeInternal(bp.trees[(int)B2BodyType.b2_staticBody], staticCapacity);

            int kinematicCapacity = 16;
            bp.trees[(int)B2BodyType.b2_kinematicBody] = new B2DynamicTree();
            b2CreateDynamicTreeInternal(bp.trees[(int)B2BodyType.b2_kinematicBody], kinematicCapacity);

            int dynamicCapacity = b2MaxInt(16, capacity.dynamicShapeCount);
            bp.trees[(int)B2BodyType.b2_dynamicBody] = new B2DynamicTree();
            b2CreateDynamicTreeInternal(bp.trees[(int)B2BodyType.b2_dynamicBody], dynamicCapacity);

            bp.movedSiblings = null;
            bp.crossSeeds = new B2NodePair[2 * B2_CROSS_SEED_COUNT];
        }

        public static void b2CreateBroadPhase(ref B2BroadPhase bp)
        {
            b2CreateBroadPhase(ref bp, new B2Capacity());
        }

        public static void b2DestroyBroadPhase(B2BroadPhase bp)
        {
            for (int i = 0; i < (int)B2BodyType.b2_bodyTypeCount; ++i)
            {
                b2DestroyDynamicTreeInternal(bp.trees[i]);
            }

            b2DestroySet(ref bp.pairSet);

            //memset( bp, 0, sizeof( b2BroadPhase ) );
            bp.Clear();
        }

        public static int b2BroadPhase_CreateProxy(B2BroadPhase bp, B2BodyType proxyType, in B2AABB aabb, ulong categoryBits, int shapeIndex, bool forcePairCreation)
        {
            B2_ASSERT(0 <= proxyType && proxyType < B2BodyType.b2_bodyTypeCount);

            bool mark = (proxyType != B2BodyType.b2_staticBody || forcePairCreation);

            int proxyId = b2CreateTreeProxyInternal(bp.trees[(int)proxyType], aabb, categoryBits, (ulong)shapeIndex, mark);
            int proxyKey = B2_PROXY_KEY(proxyId, proxyType);
            return proxyKey;
        }

        public static void b2BroadPhase_DestroyProxy(B2BroadPhase bp, int proxyKey)
        {
            B2BodyType proxyType = B2_PROXY_TYPE(proxyKey);
            int proxyId = B2_PROXY_ID(proxyKey);

            B2_ASSERT(0 <= proxyType && proxyType < B2BodyType.b2_bodyTypeCount);
            b2DestroyTreeProxy(bp.trees[(int)proxyType], proxyId);
        }

        public static void b2BroadPhase_MoveProxy(B2BroadPhase bp, int proxyKey, in B2AABB aabb)
        {
            B2BodyType proxyType = B2_PROXY_TYPE(proxyKey);
            int proxyId = B2_PROXY_ID(proxyKey);

            bool mark = true;
            b2DynamicTree_MoveProxyInternal(bp.trees[(int)proxyType], proxyId, aabb, mark);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void b2BroadPhase_MarkProxyMovedSerial(B2BroadPhase bp, int proxyKey)
        {
            B2BodyType proxyType = B2_PROXY_TYPE(proxyKey);
            int proxyId = B2_PROXY_ID(proxyKey);
            b2DynamicTree_MarkProxyMovedSerial(bp.trees[(int)proxyType], proxyId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void b2BroadPhase_MarkProxyMoved(B2BroadPhase bp, int proxyKey, in B2AABB aabb)
        {
            B2BodyType proxyType = B2_PROXY_TYPE(proxyKey);
            int proxyId = B2_PROXY_ID(proxyKey);
            b2DynamicTree_MarkProxyMoved(bp.trees[(int)proxyType], proxyId, aabb);
        }

        // Gather the sibling pairs with a moved node. This is done serially it is cache friendly.
        private static int b2GatherMovedSiblings(B2DynamicTree tree, Span<int> pairIndices)
        {
            B2TreeNode[] nodes = tree.nodes;
            int nodeEnd = tree.nodeEnd;

            int count = 0;

            // Skip the root.
            for (int pair = 2; pair < nodeEnd; pair += 2)
            {
                // Push when either sibling moved.
                if (((nodes[pair].flagIndex | nodes[pair + 1].flagIndex) & B2_MOVED_NODE) != 0)
                {
                    pairIndices[count++] = pair;
                }
            }

            return count;
        }

        private static void b2FlushCandidatePairs(B2PairContext context)
        {
            B2World world = context.world;
            B2BroadPhase bp = world.broadPhase;

            int count1 = context.batchCount;
            context.batchCount = 0;

            // Prefetch hash set entries. Less than 1% gain from this but it might
            // matter more for Box3D. Also, if I switch to verstable then this might
            // not be possible.
            // Note: C# has no prefetch, only the hashes are computed here.
            Span<ulong> keys = stackalloc ulong[B2_CANDIDATE_BATCH];
            Span<ulong> hashes = stackalloc ulong[B2_CANDIDATE_BATCH];
            for (int i = 0; i < count1; ++i)
            {
                ref readonly B2CandidatePair candidate = ref context.batch[i];
                keys[i] = B2_SHAPE_PAIR_KEY(candidate.shapeIdA, candidate.shapeIdB);
                hashes[i] = b2KeyHash(keys[i]);
            }

            // Cull existing pairs.
            Span<B2CandidatePair> candidates = stackalloc B2CandidatePair[B2_CANDIDATE_BATCH];
            int count2 = 0;
            for (int i = 0; i < count1; ++i)
            {
                bool pairExists = b2ContainsHashedKey(ref bp.pairSet, keys[i], hashes[i]);
                if (pairExists == false)
                {
                    candidates[count2] = context.batch[i];
                    count2 += 1;
                }
            }

            // Filter candidates.
            ref B2Array<ulong> pairKeys = ref context.taskContext.pairKeys;
            for (int i = 0; i < count2; ++i)
            {
                int shapeIdA = candidates[i].shapeIdA;
                int shapeIdB = candidates[i].shapeIdB;

                B2Shape shapeA = b2Array_Get(ref world.shapes, shapeIdA);
                B2Shape shapeB = b2Array_Get(ref world.shapes, shapeIdB);

                int bodyIdA = shapeA.bodyId;
                int bodyIdB = shapeB.bodyId;

                // Are the shapes on the same body?
                if (bodyIdA == bodyIdB)
                {
                    continue;
                }

                // Sensors are handled elsewhere
                if (shapeA.sensorIndex != B2_NULL_INDEX || shapeB.sensorIndex != B2_NULL_INDEX)
                {
                    continue;
                }

                if (b2ShouldShapesCollide(shapeA.filter, shapeB.filter) == false)
                {
                    continue;
                }

                if (b2CanCollide(shapeA.type, shapeB.type) == false)
                {
                    // For example, no segment vs segment collision
                    continue;
                }

                // Does a joint override collision?
                B2Body bodyA = b2Array_Get(ref world.bodies, bodyIdA);
                B2Body bodyB = b2Array_Get(ref world.bodies, bodyIdB);
                if (b2ShouldBodiesCollide(world, bodyA, bodyB) == false)
                {
                    continue;
                }

                // Custom user filter
                if (shapeA.enableCustomFiltering || shapeB.enableCustomFiltering)
                {
                    b2CustomFilterCallbackFcn customFilterCallbackFcn = world.customFilterCallbackFcn;
                    if (customFilterCallbackFcn != null)
                    {
                        B2ShapeId idA = new B2ShapeId(shapeIdA + 1, world.worldId, shapeA.generation);
                        B2ShapeId idB = new B2ShapeId(shapeIdB + 1, world.worldId, shapeB.generation);
                        bool shouldCollide = customFilterCallbackFcn(idA, idB, world.customFilterContext);
                        if (shouldCollide == false)
                        {
                            continue;
                        }
                    }
                }

                // The pair passed the gauntlet. A new contact will be created.
                b2Array_Push(ref pairKeys, B2_SHAPE_PAIR_KEY(shapeIdA, shapeIdB));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void b2AddCandidatePair(int shapeIdA, int shapeIdB, B2PairContext context)
        {
            // Follow shape index order.
            ref B2CandidatePair candidate = ref context.batch[context.batchCount];
            candidate.shapeIdA = b2MinInt(shapeIdA, shapeIdB);
            candidate.shapeIdB = b2MaxInt(shapeIdA, shapeIdB);
            context.batchCount += 1;
            if (context.batchCount == B2_CANDIDATE_BATCH)
            {
                b2FlushCandidatePairs(context);
            }
        }

        // Did either move and if so do they overlap?
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool b2TestPair(in B2TreeNode a, in B2TreeNode b)
        {
            if (((a.flagIndex | b.flagIndex) & B2_MOVED_NODE) == 0)
            {
                return false;
            }

            return b2AABB_Overlaps(a.aabb, b.aabb);
        }

        private static void b2CollideProxyAndSubtree(in B2TreeNode proxy, B2TreeNode[] nodes, int pair, B2PairContext context)
        {
            uint proxyMark = proxy.flagIndex & B2_MOVED_NODE;
            B2AABB boxv = proxy.aabb;
            int shapeId = proxy.shapeIndex;

            Span<int> stack = stackalloc int[B2_TREE_STACK_SIZE];
            int stackCount = 0;
            stack[stackCount++] = pair;

            while (stackCount > 0)
            {
                pair = stack[--stackCount];

                int movedMask = 3;
                if (proxyMark == 0)
                {
                    movedMask = (b2IsNodeMoved(nodes[pair + 0]) ? 1 : 0) | (b2IsNodeMoved(nodes[pair + 1]) ? 2 : 0);
                }

                int mask = b2OverlapNodePairMask(boxv, nodes, pair) & movedMask;
                while (mask != 0)
                {
                    int i = (int)b2CTZ32((uint)mask);
                    mask &= mask - 1;

                    ref readonly B2TreeNode node = ref nodes[pair + i];
                    if (b2IsLeaf(node))
                    {
                        b2AddCandidatePair(shapeId, node.shapeIndex, context);
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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void b2ExpandPair(B2TreeNode[] arrayA, B2TreeNode[] arrayB, in B2TreeNode nodeA, in B2TreeNode nodeB,
            Span<B2IndexPair> stack, ref int stackCount, B2PairContext context)
        {
            bool leafA = b2IsLeaf(nodeA);
            bool leafB = b2IsLeaf(nodeB);
            if (leafA && leafB)
            {
                b2AddCandidatePair(nodeA.shapeIndex, nodeB.shapeIndex, context);
            }
            else if (leafA)
            {
                b2CollideProxyAndSubtree(nodeA, arrayB, b2GetLeftChild(nodeB), context);
            }
            else if (leafB)
            {
                b2CollideProxyAndSubtree(nodeB, arrayA, b2GetLeftChild(nodeA), context);
            }
            else
            {
                if (stackCount < B2_TREE_STACK_SIZE)
                {
                    stack[stackCount] = new B2IndexPair(b2GetLeftChild(nodeA), b2GetLeftChild(nodeB));
                    stackCount += 1;
                }
                else
                {
                    B2_ASSERT(stackCount < B2_TREE_STACK_SIZE);
                }
            }
        }

        // This collides two sub-trees against each other. They can live in the same dynamic tree.
        // This can only generate pairs cross sub-tree, but not within a sub-tree. This fact means
        // this does not generate duplicate pairs.
        // For example consider the full binary tree A (B (D  E) C (F G))
        // Colliding children of A (B and C) can give pairs (D,F) (D,G) (E,F) and (E,G).
        // Then colliding children of B can give the pair (D,E) and for C (F,G).
        // So no duplicates even when used for self-collision.
        // Whenever a proxy is moved, the flag is propagated up the hierarchy to the root. So
        // self collision gathers all those moved internal nodes and collides their subtrees together.
        // See Real-time collision detection section 6.3.2. This is faster than querying every moved
        // proxy against the whole tree. Scaling is linear instead of linear * log.
        // Many other physics engines do this (Bepu, Rapier, etc). So nothing new here.
        private static void b2CollideCrossPairs(B2TreeNode[] arrayA, B2TreeNode[] arrayB, in B2TreeNode subtreeA, in B2TreeNode subtreeB, B2PairContext context)
        {
            Span<B2IndexPair> stack = stackalloc B2IndexPair[B2_TREE_STACK_SIZE];
            int stackCount = 0;

            // Seed the stack.
            if (b2TestPair(subtreeA, subtreeB))
            {
                b2ExpandPair(arrayA, arrayB, subtreeA, subtreeB, stack, ref stackCount, context);
            }

            while (stackCount > 0)
            {
                B2IndexPair pair = stack[--stackCount];
                int pairA = pair.a;
                int pairB = pair.b;

                int movedA = (b2IsNodeMoved(arrayA[pairA + 0]) ? 0x3 : 0) | (b2IsNodeMoved(arrayA[pairA + 1]) ? 0xC : 0);
                int movedB = (b2IsNodeMoved(arrayB[pairB + 0]) ? 0x5 : 0) | (b2IsNodeMoved(arrayB[pairB + 1]) ? 0xA : 0);

                int mask = b2OverlapPairMask(arrayA, pairA, arrayB, pairB) & (movedA | movedB);
                while (mask != 0)
                {
                    int lane = (int)b2CTZ32((uint)mask);
                    mask &= mask - 1;
                    b2ExpandPair(arrayA, arrayB, arrayA[pairA + (lane >> 1)], arrayB[pairB + (lane & 1)], stack, ref stackCount, context);
                }
            }
        }

        // This takes moved internal nodes and collides their sub-trees against each other.
        private static void b2SelfPairsTask(int startIndex, int endIndex, int workerIndex, object context)
        {
            b2TracyCZoneNC(B2TracyCZone.self_pairs, "Self", B2HexColor.b2_colorCoral, true);

            B2World world = context as B2World;
            B2BroadPhase bp = world.broadPhase;
            B2DynamicTree tree = bp.trees[(int)B2BodyType.b2_dynamicBody];
            B2TreeNode[] nodes = tree.nodes;
            ArraySegment<int> siblingIndices = bp.movedSiblings;

            B2PairContext pairContext = new B2PairContext();
            pairContext.world = world;
            pairContext.taskContext = world.taskContexts.data[workerIndex];

            for (int i = startIndex; i < endIndex; ++i)
            {
                int nodeIndex = siblingIndices[i];
                b2CollideCrossPairs(nodes, nodes, nodes[nodeIndex], nodes[nodeIndex + 1], pairContext);
            }

            b2FlushCandidatePairs(pairContext);

            b2TracyCZoneEnd(B2TracyCZone.self_pairs);
        }

        // This does a serial cross-tree breadth first search until the queue is full. Then it returns
        // the queue pairs as seeds for a parallel search.
        private static int b2GatherCrossSeeds(B2DynamicTree treeA, B2DynamicTree treeB, Span<B2NodePair> seeds)
        {
            B2_ASSERT((B2_CROSS_SEED_COUNT & (B2_CROSS_SEED_COUNT - 1)) == 0, "must be power of 2");

            B2TreeNode[] nodesA = treeA.nodes;
            B2TreeNode[] nodesB = treeB.nodes;

            // Breadth-first search from the two roots as one pair. An empty tree has a sentinel root that
            // survives nothing.
            Span<B2NodePair> queue = stackalloc B2NodePair[2 * B2_CROSS_SEED_COUNT];
            int mask = 2 * B2_CROSS_SEED_COUNT - 1;
            int head = 0;
            int tail = 0;

            ref readonly B2TreeNode rootA = ref nodesA[B2_ROOT_NODE];
            ref readonly B2TreeNode rootB = ref nodesB[B2_ROOT_NODE];
            if (b2TestPair(rootA, rootB))
            {
                queue[tail & mask] = new B2NodePair(rootA, rootB);
                tail += 1;
            }

            int seedCount = 0;
            while (head < tail && seedCount + (tail - head) + 3 < B2_CROSS_SEED_COUNT)
            {
                B2NodePair pair = queue[head & mask];
                head += 1;

                if (b2IsLeaf(pair.a) || b2IsLeaf(pair.b))
                {
                    seeds[seedCount++] = pair;
                    continue;
                }

                int a = b2GetLeftChild(pair.a);
                int b = b2GetLeftChild(pair.b);

                // Nodes have two children each, so four combinations.
                for (int i = 0; i < 2; ++i)
                {
                    for (int j = 0; j < 2; ++j)
                    {
                        if (b2TestPair(nodesA[a + i], nodesB[b + j]))
                        {
                            queue[tail & mask] = new B2NodePair(nodesA[a + i], nodesB[b + j]);
                            tail += 1;
                        }
                    }
                }
            }

            while (head < tail)
            {
                seeds[seedCount] = queue[head & mask];
                seedCount += 1;
                head += 1;
            }

            return seedCount;
        }

        private static void b2CrossPairsTask(int startIndex, int endIndex, int workerIndex, object context)
        {
            b2TracyCZoneNC(B2TracyCZone.cross_pairs, "Cross", B2HexColor.b2_colorCoral, true);

            B2CrossContext crossContext = context as B2CrossContext;
            B2World world = crossContext.world;
            B2BroadPhase bp = world.broadPhase;
            B2TreeNode[] staticNodes = bp.trees[(int)B2BodyType.b2_staticBody].nodes;
            B2TreeNode[] kinematicNodes = bp.trees[(int)B2BodyType.b2_kinematicBody].nodes;
            B2TreeNode[] dynamicNodes = bp.trees[(int)B2BodyType.b2_dynamicBody].nodes;

            B2PairContext pairContext = new B2PairContext();
            pairContext.world = world;
            pairContext.taskContext = world.taskContexts.data[workerIndex];

            for (int i = startIndex; i < endIndex; ++i)
            {
                B2TreeNode[] nodesB = i < crossContext.staticSeedCount ? staticNodes : kinematicNodes;
                ref readonly B2NodePair seed = ref crossContext.seeds[i];
                b2CollideCrossPairs(dynamicNodes, nodesB, seed.a, seed.b, pairContext);
            }

            b2FlushCandidatePairs(pairContext);

            b2TracyCZoneEnd(B2TracyCZone.cross_pairs);
        }

        // LSD radix sort. Faster than qsort when count is large and fast enough otherwise.
        // https://en.wikipedia.org/wiki/Radix_sort
        public static void b2RadixSortKeys(Span<ulong> keys, Span<ulong> tempKeys, int count)
        {
            Span<int> digitCounts = stackalloc int[8 * 256];
            digitCounts.Clear();
            for (int i = 0; i < count; ++i)
            {
                ulong key = keys[i];
                for (int digit = 0; digit < 8; ++digit)
                {
                    digitCounts[digit * 256 + (int)((key >> (8 * digit)) & 0xFF)] += 1;
                }
            }

            Span<ulong> source = keys;
            Span<ulong> target = tempKeys;
            bool sourceIsKeys = true;

            for (int digit = 0; digit < 8; ++digit)
            {
                int shift = 8 * digit;
                Span<int> offsets = digitCounts.Slice(digit * 256, 256);

                if (offsets[(int)((source[0] >> shift) & 0xFF)] == count)
                {
                    // All keys have the same value for this digit. Common for shape pair keys.
                    continue;
                }

                int sum = 0;
                for (int i = 0; i < 256; ++i)
                {
                    int digitCount = offsets[i];
                    offsets[i] = sum;
                    sum += digitCount;
                }

                for (int i = 0; i < count; ++i)
                {
                    ulong key = source[i];
                    target[offsets[(int)((key >> shift) & 0xFF)]++] = key;
                }

                // Swap for the next pass
                Span<ulong> swap = source;
                source = target;
                target = swap;
                sourceIsKeys = !sourceIsKeys;
            }

            if (sourceIsKeys == false)
            {
                source.Slice(0, count).CopyTo(keys);
            }
        }

        public static void b2UpdateTreesTask(object context)
        {
            b2TracyCZoneNC(B2TracyCZone.tree_task, "Rebuild BVH", B2HexColor.b2_colorFireBrick, true);

            B2World world = (B2World)context;
            b2DynamicTree_Rebuild(world.broadPhase.trees[(int)B2BodyType.b2_dynamicBody], false);
            b2DynamicTree_Rebuild(world.broadPhase.trees[(int)B2BodyType.b2_kinematicBody], false);

            b2TracyCZoneEnd(B2TracyCZone.tree_task);
        }

        // Task that can be done in parallel with the narrow-phase
        // - rebuild the collision tree for dynamic and kinematic bodies to keep their query performance good
        private static void b2EnqueueTreeUpdate(B2World world)
        {
            if (world.taskCount < B2_MAX_TASKS)
            {
                world.userTreeTask = world.enqueueTaskFcn(b2UpdateTreesTask, world, world.userTaskContext);
                world.taskCount += 1;
                world.activeTaskCount += world.userTreeTask == null ? 0 : 1;
            }
            else
            {
                world.userTreeTask = null;
                b2UpdateTreesTask(world);
            }
        }

        public static void b2UpdateBroadPhasePairs(B2World world)
        {
            B2BroadPhase bp = world.broadPhase;

            bool needUpdate = b2HasTreeMoved(bp.trees[(int)B2BodyType.b2_staticBody]);
            needUpdate = needUpdate || b2NeedsRebuild(bp.trees[(int)B2BodyType.b2_kinematicBody]);
            needUpdate = needUpdate || b2NeedsRebuild(bp.trees[(int)B2BodyType.b2_dynamicBody]);

            if (needUpdate == false)
            {
                return;
            }

            b2TracyCZoneNC(B2TracyCZone.update_pairs, "Find Pairs", B2HexColor.b2_colorMediumSlateBlue, true);

            B2StackAllocator alloc = world.stack;

            for (int i = 0; i < world.workerCount; ++i)
            {
                b2Array_Clear(ref world.taskContexts.data[i].pairKeys);
            }

            // Generate pairs by querying the dynamic body tree against itself and against
            // the kinematic and static trees.
            {
                // Get the sibling pairs of the dynamic body tree that have moved.
                B2DynamicTree dynamicTree = bp.trees[(int)B2BodyType.b2_dynamicBody];
                int pairCapacity = dynamicTree.nodeEnd / 2;
                bp.movedSiblings = b2StackAlloc<int>(alloc, pairCapacity, "moved pairs");
                int dynamicMoveCount = b2GatherMovedSiblings(dynamicTree, bp.movedSiblings);

                // Get seeds for colliding against the static and kinematic trees.
                B2NodePair[] crossSeeds = bp.crossSeeds;
                int staticSeedCount = b2GatherCrossSeeds(dynamicTree, bp.trees[(int)B2BodyType.b2_staticBody], crossSeeds);
                B2_ASSERT(staticSeedCount <= B2_CROSS_SEED_COUNT);
                int kinematicSeedCount = b2GatherCrossSeeds(dynamicTree, bp.trees[(int)B2BodyType.b2_kinematicBody], crossSeeds.AsSpan(staticSeedCount));
                B2_ASSERT(kinematicSeedCount <= B2_CROSS_SEED_COUNT);
                int crossMoveCount = staticSeedCount + kinematicSeedCount;

                // Collide the dynamic body tree against the static and kinematic trees.
                B2CrossContext crossContext = new B2CrossContext
                {
                    world = world,
                    seeds = crossSeeds,
                    staticSeedCount = staticSeedCount,
                };
                b2ParallelFor(world, b2CrossPairsTask, crossMoveCount, 1, crossContext);

                // Collide the dynamic body tree against itself.
                b2ParallelFor(world, b2SelfPairsTask, dynamicMoveCount, 64, world);
            }

            b2DynamicTree_ClearMoved(bp.trees[(int)B2BodyType.b2_staticBody]);

            b2TracyCZoneEnd(B2TracyCZone.update_pairs);

            b2TracyCZoneNC(B2TracyCZone.create_contacts, "Create Contacts", B2HexColor.b2_colorCoral, true);

            // Update stale trees.
            b2EnqueueTreeUpdate(world);

            // Pairs arrive in deterministic order but scrambled relative to body and shape order
            // sorting them here improves solver performance.
            int pairCount = 0;
            for (int i = 0; i < world.workerCount; ++i)
            {
                pairCount += world.taskContexts.data[i].pairKeys.count;
            }

            ArraySegment<ulong> pairKeys = b2StackAlloc<ulong>(alloc, b2MaxInt(pairCount, 1), "pair keys");
            int keyCount = 0;
            for (int i = 0; i < world.workerCount; ++i)
            {
                ref B2Array<ulong> workerKeys = ref world.taskContexts.data[i].pairKeys;
                if (workerKeys.count > 0)
                {
                    Array.Copy(workerKeys.data, 0, pairKeys.Array, pairKeys.Offset + keyCount, workerKeys.count);
                    keyCount += workerKeys.count;
                }
            }

            B2_ASSERT(keyCount == pairCount);

            if (pairCount > 1)
            {
                ArraySegment<ulong> tempKeys = b2StackAlloc<ulong>(alloc, pairCount, "temp keys");
                b2RadixSortKeys(pairKeys.AsSpan(0, pairCount), tempKeys.AsSpan(0, pairCount), pairCount);
                b2StackFree(alloc, tempKeys);
            }

            for (int i = 0; i < keyCount; ++i)
            {
                int shapeIdA = (int)(pairKeys[i] >> 32);
                int shapeIdB = (int)(pairKeys[i] & 0xFFFFFFFF);
                B2Shape shapeA = b2Array_Get(ref world.shapes, shapeIdA);
                B2Shape shapeB = b2Array_Get(ref world.shapes, shapeIdB);
                b2CreateContact(world, shapeA, shapeB);
            }

            b2StackFree(alloc, pairKeys);

            b2StackFree(alloc, bp.movedSiblings);
            bp.movedSiblings = null;

            b2ValidateSolverSets(world);

            b2TracyCZoneEnd(B2TracyCZone.create_contacts);
        }

        internal static void b2ValidateBroadphase(B2BroadPhase bp)
        {
            b2DynamicTree_Validate(bp.trees[(int)B2BodyType.b2_dynamicBody]);
            b2DynamicTree_Validate(bp.trees[(int)B2BodyType.b2_kinematicBody]);

            // TODO_ERIN validate every shape AABB is contained in tree AABB
        }

        internal static void b2ValidateNoEnlarged(B2BroadPhase bp)
        {
#if DEBUG
            for (int j = 0; j < (int)B2BodyType.b2_bodyTypeCount; ++j)
            {
                B2DynamicTree tree = bp.trees[j];
                b2DynamicTree_ValidateNoMoved(tree);
            }
#else
            B2_UNUSED(bp);
#endif
        }
    }
}
