// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;
using static Box2D.NET.B2Tables;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Atomics;
using static Box2D.NET.B2DynamicTrees;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Buffers;
using static Box2D.NET.B2Profiling;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Contacts;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2ArenaAllocators;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET
{
    public static class B2BoardPhases
    {
        // Warning: writing to these globals significantly slows multithreading performance
#if B2_SNOOP_PAIR_COUNTERS
        private static B2TreeStats b2_dynamicStats = new B2TreeStats();
        private static B2TreeStats b2_kinematicStats = new B2TreeStats();
        private static B2TreeStats b2_staticStats = new B2TreeStats();
#endif

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


        // This is what triggers new contact pairs to be created
        // Warning: this must be called in deterministic order
        public static void b2BufferMove(B2BroadPhase bp, int queryProxy)
        {
            // Adding 1 because 0 is the sentinel
            bool alreadyAdded = b2AddKey(ref bp.moveSet, (ulong)(queryProxy + 1));
            if (alreadyAdded == false)
            {
                b2Array_Push(ref bp.moveArray, queryProxy);
            }
        }


        // 
        // static FILE* s_file = NULL;
        public static void b2CreateBroadPhase(ref B2BroadPhase bp)
        {
            B2_ASSERT((int)B2BodyType.b2_bodyTypeCount == 3, "must be three body types");

            // if (s_file == NULL)
            //{
            //	s_file = fopen("pairs01.txt", "a");
            //	fprintf(s_file, "============\n\n");
            // }
            bp = new B2BroadPhase();
            bp.trees = new B2DynamicTree[(int)B2BodyType.b2_bodyTypeCount];
            bp.moveSet = b2CreateSet(16);
            bp.moveArray = b2Array_Create<int>(16);
            bp.moveResults = null;
            bp.movePairs = null;
            bp.movePairCapacity = 0;
            b2AtomicStoreInt(ref bp.movePairIndex, 0);
            bp.pairSet = b2CreateSet(32);

            for (int i = 0; i < (int)B2BodyType.b2_bodyTypeCount; ++i)
            {
                bp.trees[i] = b2DynamicTree_Create();
            }
        }

        public static void b2DestroyBroadPhase(B2BroadPhase bp)
        {
            for (int i = 0; i < (int)B2BodyType.b2_bodyTypeCount; ++i)
            {
                b2DynamicTree_Destroy(bp.trees[i]);
            }

            b2DestroySet(ref bp.moveSet);
            b2Array_Destroy(ref bp.moveArray);
            b2DestroySet(ref bp.pairSet);

            //memset( bp, 0, sizeof( b2BroadPhase ) );
            bp.Clear();

            // if (s_file != NULL)
            //{
            //	fclose(s_file);
            //	s_file = NULL;
            // }
        }

        public static void b2UnBufferMove(B2BroadPhase bp, int proxyKey)
        {
            bool found = b2RemoveKey(ref bp.moveSet, (ulong)(proxyKey + 1));

            if (found)
            {
                // Purge from move buffer. Linear search.
                // todo if I can iterate the move set then I don't need the moveArray
                int count = bp.moveArray.count;
                for (int i = 0; i < count; ++i)
                {
                    if (bp.moveArray.data[i] == proxyKey)
                    {
                        b2Array_RemoveSwap(ref bp.moveArray, i);
                        break;
                    }
                }
            }
        }

        public static int b2BroadPhase_CreateProxy(B2BroadPhase bp, B2BodyType proxyType, B2AABB aabb, ulong categoryBits, int shapeIndex, bool forcePairCreation)
        {
            B2_ASSERT(0 <= proxyType && proxyType < B2BodyType.b2_bodyTypeCount);
            int proxyId = b2DynamicTree_CreateProxy(bp.trees[(int)proxyType], aabb, categoryBits, (ulong)shapeIndex);
            int proxyKey = B2_PROXY_KEY(proxyId, proxyType);
            if (proxyType != B2BodyType.b2_staticBody || forcePairCreation)
            {
                b2BufferMove(bp, proxyKey);
            }

            return proxyKey;
        }

        public static void b2BroadPhase_DestroyProxy(B2BroadPhase bp, int proxyKey)
        {
            B2_ASSERT(bp.moveArray.count == (int)bp.moveSet.count);
            b2UnBufferMove(bp, proxyKey);

            B2BodyType proxyType = B2_PROXY_TYPE(proxyKey);
            int proxyId = B2_PROXY_ID(proxyKey);

            B2_ASSERT(0 <= proxyType && proxyType <= B2BodyType.b2_bodyTypeCount);
            b2DynamicTree_DestroyProxy(bp.trees[(int)proxyType], proxyId);
        }

        public static void b2BroadPhase_MoveProxy(B2BroadPhase bp, int proxyKey, B2AABB aabb)
        {
            B2BodyType proxyType = B2_PROXY_TYPE(proxyKey);
            int proxyId = B2_PROXY_ID(proxyKey);

            b2DynamicTree_MoveProxy(bp.trees[(int)proxyType], proxyId, aabb);
            b2BufferMove(bp, proxyKey);
        }

        public static void b2BroadPhase_EnlargeProxy(B2BroadPhase bp, int proxyKey, B2AABB aabb)
        {
            B2_ASSERT(proxyKey != B2_NULL_INDEX);
            B2BodyType typeIndex = B2_PROXY_TYPE(proxyKey);
            int proxyId = B2_PROXY_ID(proxyKey);

            B2_ASSERT(typeIndex != B2BodyType.b2_staticBody);

            b2DynamicTree_EnlargeProxy(bp.trees[(int)typeIndex], proxyId, aabb);
            b2BufferMove(bp, proxyKey);
        }


        // This is called from b2DynamicTree::Query when we are gathering pairs.
        public static bool b2PairQueryCallback(int proxyId, ulong userData, ref B2QueryPairContext context)
        {
            int shapeId = (int)userData;

            ref B2QueryPairContext queryContext = ref context;
            B2BroadPhase broadPhase = queryContext.world.broadPhase;

            int proxyKey = B2_PROXY_KEY(proxyId, queryContext.queryTreeType);
            int queryProxyKey = queryContext.queryProxyKey;

            // A proxy cannot form a pair with itself.
            if (proxyKey == queryContext.queryProxyKey)
            {
                return true;
            }

            B2BodyType treeType = queryContext.queryTreeType;
            B2BodyType queryProxyType = B2_PROXY_TYPE(queryProxyKey);

            // De-duplication
            // It is important to prevent duplicate contacts from being created. Ideally I can prevent duplicates
            // early and in the worker. Most of the time the moveSet contains dynamic and kinematic proxies, but
            // sometimes it has static proxies.

            // I had an optimization here to skip checking the move set if this is a query into
            // the static tree. The assumption is that the static proxies are never in the move set
            // so there is no risk of duplication. However, this is not true with
            // b2ShapeDef::invokeContactCreation or when a static shape is modified.
            // There can easily be scenarios where the static proxy is in the moveSet but the dynamic proxy is not.
            // I could have some flag to indicate that there are any static bodies in the moveSet.

            // Is this proxy also moving?
            if (queryProxyType == B2BodyType.b2_dynamicBody)
            {
                if (treeType == B2BodyType.b2_dynamicBody && proxyKey < queryProxyKey)
                {
                    bool moved = b2ContainsKey(ref broadPhase.moveSet, (ulong)(proxyKey + 1));
                    if (moved)
                    {
                        // Both proxies are moving. Avoid duplicate pairs.
                        return true;
                    }
                }
            }
            else
            {
                B2_ASSERT(treeType == B2BodyType.b2_dynamicBody);
                bool moved = b2ContainsKey(ref broadPhase.moveSet, (ulong)(proxyKey + 1));
                if (moved)
                {
                    // Both proxies are moving. Avoid duplicate pairs.
                    return true;
                }
            }

            ulong pairKey = B2_SHAPE_PAIR_KEY(shapeId, queryContext.queryShapeIndex);
            if (b2ContainsKey(ref broadPhase.pairSet, pairKey))
            {
                // contact exists
                return true;
            }

            int shapeIdA, shapeIdB;
            if (proxyKey < queryProxyKey)
            {
                shapeIdA = shapeId;
                shapeIdB = queryContext.queryShapeIndex;
            }
            else
            {
                shapeIdA = queryContext.queryShapeIndex;
                shapeIdB = shapeId;
            }

            B2World world = queryContext.world;

            B2Shape shapeA = b2Array_Get(ref world.shapes, shapeIdA);
            B2Shape shapeB = b2Array_Get(ref world.shapes, shapeIdB);

            int bodyIdA = shapeA.bodyId;
            int bodyIdB = shapeB.bodyId;

            // Are the shapes on the same body?
            if (bodyIdA == bodyIdB)
            {
                return true;
            }

            // Sensors are handled elsewhere
            if (shapeA.sensorIndex != B2_NULL_INDEX || shapeB.sensorIndex != B2_NULL_INDEX)
            {
                return true;
            }

            if (b2ShouldShapesCollide(shapeA.filter, shapeB.filter) == false)
            {
                return true;
            }

            // Does a joint override collision?
            B2Body bodyA = b2Array_Get(ref world.bodies, bodyIdA);
            B2Body bodyB = b2Array_Get(ref world.bodies, bodyIdB);
            if (b2ShouldBodiesCollide(world, bodyA, bodyB) == false)
            {
                return true;
            }

            // Custom user filter
            b2CustomFilterFcn customFilterFcn = queryContext.world.customFilterFcn;
            if (customFilterFcn != null)
            {
                B2ShapeId idA = new B2ShapeId(shapeIdA + 1, world.worldId, shapeA.generation);
                B2ShapeId idB = new B2ShapeId(shapeIdB + 1, world.worldId, shapeB.generation);
                bool shouldCollide = customFilterFcn(idA, idB, queryContext.world.customFilterContext);
                if (shouldCollide == false)
                {
                    return true;
                }
            }

            // todo per thread to eliminate atomic?
            int pairIndex = b2AtomicFetchAddInt(ref broadPhase.movePairIndex, 1);

            B2MovePair pair;
            if (pairIndex < broadPhase.movePairCapacity)
            {
                pair = broadPhase.movePairs[pairIndex];
                pair.heap = false;
            }
            else
            {
                // TODO: @ikpil, check
                //pair = b2Alloc<b2MovePair>(1);( sizeof(  ) );
                pair = new B2MovePair();
                pair.heap = true;
            }

            pair.shapeIndexA = shapeIdA;
            pair.shapeIndexB = shapeIdB;
            pair.next = queryContext.moveResult.pairList;
            queryContext.moveResult.pairList = pair;

            // continue the query
            return true;
        }


        public static void b2FindPairsTask(int startIndex, int endIndex, uint threadIndex, object context)
        {
            b2TracyCZoneNC(B2TracyCZone.pair_task, "Pair", B2HexColor.b2_colorMediumSlateBlue, true);

            B2_UNUSED(threadIndex);

            B2World world = context as B2World;
            B2BroadPhase bp = world.broadPhase;

            B2QueryPairContext queryContext = new B2QueryPairContext();
            queryContext.world = world;

            for (int i = startIndex; i < endIndex; ++i)
            {
                // Initialize move result for this moved proxy
                queryContext.moveResult = bp.moveResults[i];
                queryContext.moveResult.pairList = null;

                int proxyKey = bp.moveArray.data[i];
                if (proxyKey == B2_NULL_INDEX)
                {
                    // proxy was destroyed after it moved
                    continue;
                }

                B2BodyType proxyType = B2_PROXY_TYPE(proxyKey);

                int proxyId = B2_PROXY_ID(proxyKey);
                queryContext.queryProxyKey = proxyKey;

                B2DynamicTree baseTree = bp.trees[(int)proxyType];

                // We have to query the tree with the fat AABB so that
                // we don't fail to create a contact that may touch later.
                B2AABB fatAABB = b2DynamicTree_GetAABB(baseTree, proxyId);
                queryContext.queryShapeIndex = (int)b2DynamicTree_GetUserData(baseTree, proxyId);

                // Query trees. Only dynamic proxies collide with kinematic and static proxies.
                // Using B2_DEFAULT_MASK_BITS so that b2Filter::groupIndex works.
                B2TreeStats stats = new B2TreeStats();
                if (proxyType == B2BodyType.b2_dynamicBody)
                {
                    // consider using bits = groupIndex > 0 ? B2_DEFAULT_MASK_BITS : maskBits
                    queryContext.queryTreeType = B2BodyType.b2_kinematicBody;
                    B2TreeStats statsKinematic = b2DynamicTree_Query(bp.trees[(int)B2BodyType.b2_kinematicBody], fatAABB, B2_DEFAULT_MASK_BITS, b2PairQueryCallback, ref queryContext);
                    stats.nodeVisits += statsKinematic.nodeVisits;
                    stats.leafVisits += statsKinematic.leafVisits;

                    queryContext.queryTreeType = B2BodyType.b2_staticBody;
                    B2TreeStats statsStatic = b2DynamicTree_Query(bp.trees[(int)B2BodyType.b2_staticBody], fatAABB, B2_DEFAULT_MASK_BITS, b2PairQueryCallback, ref queryContext);
                    stats.nodeVisits += statsStatic.nodeVisits;
                    stats.leafVisits += statsStatic.leafVisits;
                }

                // All proxies collide with dynamic proxies
                // Using B2_DEFAULT_MASK_BITS so that b2Filter::groupIndex works.
                queryContext.queryTreeType = B2BodyType.b2_dynamicBody;
                B2TreeStats statsDynamic = b2DynamicTree_Query(bp.trees[(int)B2BodyType.b2_dynamicBody], fatAABB, B2_DEFAULT_MASK_BITS, b2PairQueryCallback, ref queryContext);
                stats.nodeVisits += statsDynamic.nodeVisits;
                stats.leafVisits += statsDynamic.leafVisits;
            }

            b2TracyCZoneEnd(B2TracyCZone.pair_task);
        }

        public static void b2UpdateBroadPhasePairs(B2World world)
        {
            B2BroadPhase bp = world.broadPhase;

            int moveCount = bp.moveArray.count;
            B2_ASSERT(moveCount == (int)bp.moveSet.count);

            if (moveCount == 0)
            {
                return;
            }

            b2TracyCZoneNC(B2TracyCZone.update_pairs, "Find Pairs", B2HexColor.b2_colorMediumSlateBlue, true);

            B2ArenaAllocator alloc = world.arena;

            // todo these could be in the step context
            bp.moveResults = b2AllocateArenaItem<B2MoveResult>(alloc, moveCount, "move results");
            bp.movePairCapacity = 16 * moveCount;
            bp.movePairs = b2AllocateArenaItem<B2MovePair>(alloc, bp.movePairCapacity, "move pairs");
            b2AtomicStoreInt(ref bp.movePairIndex, 0);

#if B2_SNOOP_TABLE_COUNTERS
            B2AtomicInt b2_probeCount = new B2AtomicInt();
            b2AtomicStoreInt(ref b2_probeCount, 0);
#endif

            int minRange = 64;
            object userPairTask = world.enqueueTaskFcn(b2FindPairsTask, moveCount, minRange, world, world.userTaskContext);
            if (userPairTask != null)
            {
                world.finishTaskFcn(userPairTask, world.userTaskContext);
                world.taskCount += 1;
            }

            // todo_erin could start tree rebuild here

            b2TracyCZoneNC(B2TracyCZone.create_contacts, "Create Contacts", B2HexColor.b2_colorCoral, true);

            // Single-threaded work
            // - Clear move flags
            // - Create contacts in deterministic order
            for (int i = 0; i < moveCount; ++i)
            {
                B2MoveResult result = bp.moveResults[i];
                B2MovePair pair = result.pairList;
                while (pair != null)
                {
                    int shapeIdA = pair.shapeIndexA;
                    int shapeIdB = pair.shapeIndexB;

                    // if (s_file != NULL)
                    //{
                    //	fprintf(s_file, "%d %d\n", shapeIdA, shapeIdB);
                    // }

                    B2Shape shapeA = b2Array_Get(ref world.shapes, shapeIdA);
                    B2Shape shapeB = b2Array_Get(ref world.shapes, shapeIdB);

                    b2CreateContact(world, shapeA, shapeB);

                    if (pair.heap)
                    {
                        B2MovePair temp = pair;
                        pair = pair.next;
                        b2Free(temp, 1);
                    }
                    else
                    {
                        pair = pair.next;
                    }
                }

                // if (s_file != NULL)
                //{
                //	fprintf(s_file, "\n");
                // }
            }

            // if (s_file != NULL)
            //{
            //	fprintf(s_file, "count = %d\n\n", pairCount);
            // }

            // Reset move buffer
            b2Array_Clear(ref bp.moveArray);
            b2ClearSet(ref bp.moveSet);

            b2FreeArenaItem(alloc, bp.movePairs);
            bp.movePairs = null;
            b2FreeArenaItem(alloc, bp.moveResults);
            bp.moveResults = null;

            b2ValidateSolverSets(world);

            b2TracyCZoneEnd(B2TracyCZone.create_contacts);

            b2TracyCZoneEnd(B2TracyCZone.update_pairs);
        }

        public static bool b2BroadPhase_TestOverlap(B2BroadPhase bp, int proxyKeyA, int proxyKeyB)
        {
            int typeIndexA = (int)B2_PROXY_TYPE(proxyKeyA);
            int proxyIdA = B2_PROXY_ID(proxyKeyA);
            int typeIndexB = (int)B2_PROXY_TYPE(proxyKeyB);
            int proxyIdB = B2_PROXY_ID(proxyKeyB);

            B2AABB aabbA = b2DynamicTree_GetAABB(bp.trees[typeIndexA], proxyIdA);
            B2AABB aabbB = b2DynamicTree_GetAABB(bp.trees[typeIndexB], proxyIdB);
            return b2AABB_Overlaps(aabbA, aabbB);
        }

        public static void b2BroadPhase_RebuildTrees(B2BroadPhase bp)
        {
            b2DynamicTree_Rebuild(bp.trees[(int)B2BodyType.b2_dynamicBody], false);
            b2DynamicTree_Rebuild(bp.trees[(int)B2BodyType.b2_kinematicBody], false);
        }

        public static int b2BroadPhase_GetShapeIndex(B2BroadPhase bp, int proxyKey)
        {
            int typeIndex = (int)B2_PROXY_TYPE(proxyKey);
            int proxyId = B2_PROXY_ID(proxyKey);

            return (int)b2DynamicTree_GetUserData(bp.trees[typeIndex], proxyId);
        }

        public static void b2ValidateBroadphase(B2BroadPhase bp)
        {
            b2DynamicTree_Validate(bp.trees[(int)B2BodyType.b2_dynamicBody]);
            b2DynamicTree_Validate(bp.trees[(int)B2BodyType.b2_kinematicBody]);

            // TODO_ERIN validate every shape AABB is contained in tree AABB
        }

        public static void b2ValidateNoEnlarged(B2BroadPhase bp)
        {
#if DEBUG
            for (int j = 0; j < (int)B2BodyType.b2_bodyTypeCount; ++j)
            {
                B2DynamicTree tree = bp.trees[j];
                b2DynamicTree_ValidateNoEnlarged(tree);
            }
#else
            B2_UNUSED(bp);
#endif
        }
    }
}