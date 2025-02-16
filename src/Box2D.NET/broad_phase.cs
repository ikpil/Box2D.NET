// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using static Box2D.NET.table;
using static Box2D.NET.array;
using static Box2D.NET.atomic;
using static Box2D.NET.dynamic_tree;
using static Box2D.NET.core;
using static Box2D.NET.constants;
using static Box2D.NET.contact;
using static Box2D.NET.body;
using static Box2D.NET.world;
using static Box2D.NET.arena_allocator;
using static Box2D.NET.aabb;

namespace Box2D.NET;

/// The broad-phase is used for computing pairs and performing volume queries and ray casts.
/// This broad-phase does not persist pairs. Instead, this reports potentially new pairs.
/// It is up to the client to consume the new pairs and to track subsequent overlap.
public class b2BroadPhase
{
    public b2DynamicTree[] trees;
    public int proxyCount;

    // The move set and array are used to track shapes that have moved significantly
    // and need a pair query for new contacts. The array has a deterministic order.
    // todo perhaps just a move set?
    // todo implement a 32bit hash set for faster lookup
    // todo moveSet can grow quite large on the first time step and remain large
    public b2HashSet moveSet;
    public b2Array<int> moveArray;

    // These are the results from the pair query and are used to create new contacts
    // in deterministic order.
    // todo these could be in the step context
    public ArraySegment<b2MoveResult> moveResults;
    public ArraySegment<b2MovePair> movePairs;
    public int movePairCapacity;
    public b2AtomicInt movePairIndex;

    // Tracks shape pairs that have a b2Contact
    // todo pairSet can grow quite large on the first time step and remain large
    public b2HashSet pairSet;

    public void Clear()
    {
        Debug.Assert(false, "TODO: @ikpil, clear!!");
    }
}

public class b2MovePair
{
    public int shapeIndexA;
    public int shapeIndexB;
    public b2MovePair next;
    public bool heap;
}

public class b2MoveResult
{
    public b2MovePair pairList;
}

public class b2QueryPairContext
{
    public b2World world;
    public b2MoveResult moveResult;
    public b2BodyType queryTreeType;
    public int queryProxyKey;
    public int queryShapeIndex;
}

public class board_phase
{
// // Store the proxy type in the lower 2 bits of the proxy key. This leaves 30 bits for the id.
    public static b2BodyType B2_PROXY_TYPE(int KEY)
    {
        return ((b2BodyType)((KEY) & 3));
    }

    public static int B2_PROXY_ID(int KEY)
    {
        return ((KEY) >> 2);
    }

    public static int B2_PROXY_KEY(int ID, b2BodyType TYPE)
    {
        return (ID << 2) | (int)TYPE;
    }


// This is what triggers new contact pairs to be created
// Warning: this must be called in deterministic order
    public static void b2BufferMove(b2BroadPhase bp, int queryProxy)
    {
        // Adding 1 because 0 is the sentinel
        bool alreadyAdded = b2AddKey(bp.moveSet, (ulong)(queryProxy + 1));
        if (alreadyAdded == false)
        {
            b2Array_Push(bp.moveArray, queryProxy);
        }
    }


// 

// static FILE* s_file = NULL;

    public static void b2CreateBroadPhase(ref b2BroadPhase bp)
    {
        Debug.Assert((int)b2BodyType.b2_bodyTypeCount == 3, "must be three body types");

        // if (s_file == NULL)
        //{
        //	s_file = fopen("pairs01.txt", "a");
        //	fprintf(s_file, "============\n\n");
        // }
        bp = new b2BroadPhase();
        bp.trees = new b2DynamicTree[(int)b2BodyType.b2_bodyTypeCount];
        bp.proxyCount = 0;
        bp.moveSet = b2CreateSet(16);
        bp.moveArray = b2Array_Create<int>(16);
        bp.moveResults = null;
        bp.movePairs = null;
        bp.movePairCapacity = 0;
        bp.movePairIndex = new b2AtomicInt();
        b2AtomicStoreInt(bp.movePairIndex, 0);
        bp.pairSet = b2CreateSet(32);

        for (int i = 0; i < (int)b2BodyType.b2_bodyTypeCount; ++i)
        {
            bp.trees[i] = b2DynamicTree_Create();
        }
    }

    public static void b2DestroyBroadPhase(b2BroadPhase bp)
    {
        for (int i = 0; i < (int)b2BodyType.b2_bodyTypeCount; ++i)
        {
            b2DynamicTree_Destroy(bp.trees[i]);
        }

        b2DestroySet(bp.moveSet);
        b2Array_Destroy(bp.moveArray);
        b2DestroySet(bp.pairSet);

        //memset( bp, 0, sizeof( b2BroadPhase ) );
        bp.Clear();

        // if (s_file != NULL)
        //{
        //	fclose(s_file);
        //	s_file = NULL;
        // }
    }

    public static void b2UnBufferMove(b2BroadPhase bp, int proxyKey)
    {
        bool found = b2RemoveKey(bp.moveSet, (ulong)(proxyKey + 1));

        if (found)
        {
            // Purge from move buffer. Linear search.
            // todo if I can iterate the move set then I don't need the moveArray
            int count = bp.moveArray.count;
            for (int i = 0; i < count; ++i)
            {
                if (bp.moveArray.data[i] == proxyKey)
                {
                    b2Array_RemoveSwap(bp.moveArray, i);
                    break;
                }
            }
        }
    }

    public static int b2BroadPhase_CreateProxy(b2BroadPhase bp, b2BodyType proxyType, b2AABB aabb, ulong categoryBits, int shapeIndex, bool forcePairCreation)
    {
        Debug.Assert(0 <= proxyType && proxyType < b2BodyType.b2_bodyTypeCount);
        int proxyId = b2DynamicTree_CreateProxy(bp.trees[(int)proxyType], aabb, categoryBits, shapeIndex);
        int proxyKey = B2_PROXY_KEY(proxyId, proxyType);
        if (proxyType != b2BodyType.b2_staticBody || forcePairCreation)
        {
            b2BufferMove(bp, proxyKey);
        }

        return proxyKey;
    }

    public static void b2BroadPhase_DestroyProxy(b2BroadPhase bp, int proxyKey)
    {
        Debug.Assert(bp.moveArray.count == (int)bp.moveSet.count);
        b2UnBufferMove(bp, proxyKey);

        --bp.proxyCount;

        b2BodyType proxyType = B2_PROXY_TYPE(proxyKey);
        int proxyId = B2_PROXY_ID(proxyKey);

        Debug.Assert(0 <= proxyType && proxyType <= b2BodyType.b2_bodyTypeCount);
        b2DynamicTree_DestroyProxy(bp.trees[(int)proxyType], proxyId);
    }

    public static void b2BroadPhase_MoveProxy(b2BroadPhase bp, int proxyKey, b2AABB aabb)
    {
        b2BodyType proxyType = B2_PROXY_TYPE(proxyKey);
        int proxyId = B2_PROXY_ID(proxyKey);

        b2DynamicTree_MoveProxy(bp.trees[(int)proxyType], proxyId, aabb);
        b2BufferMove(bp, proxyKey);
    }

    public static void b2BroadPhase_EnlargeProxy(b2BroadPhase bp, int proxyKey, b2AABB aabb)
    {
        Debug.Assert(proxyKey != B2_NULL_INDEX);
        b2BodyType typeIndex = B2_PROXY_TYPE(proxyKey);
        int proxyId = B2_PROXY_ID(proxyKey);

        Debug.Assert(typeIndex != b2BodyType.b2_staticBody);

        b2DynamicTree_EnlargeProxy(bp.trees[(int)typeIndex], proxyId, aabb);
        b2BufferMove(bp, proxyKey);
    }


// This is called from b2DynamicTree::Query when we are gathering pairs.
    static bool b2PairQueryCallback(int proxyId, int shapeId, object context)
    {
        b2QueryPairContext queryContext = context as b2QueryPairContext;
        b2BroadPhase broadPhase = queryContext.world.broadPhase;

        int proxyKey = B2_PROXY_KEY(proxyId, queryContext.queryTreeType);
        int queryProxyKey = queryContext.queryProxyKey;

        // A proxy cannot form a pair with itself.
        if (proxyKey == queryContext.queryProxyKey)
        {
            return true;
        }

        b2BodyType treeType = queryContext.queryTreeType;
        b2BodyType queryProxyType = B2_PROXY_TYPE(queryProxyKey);

        // De-duplication
        // It is important to prevent duplicate contacts from being created. Ideally I can prevent duplicates
        // early and in the worker. Most of the time the moveSet contains dynamic and kinematic proxies, but
        // sometimes it has static proxies.

        // I had an optimization here to skip checking the move set if this is a query into
        // the static tree. The assumption is that the static proxies are never in the move set
        // so there is no risk of duplication. However, this is not true with
        // b2ShapeDef::forceContactCreation, b2ShapeDef::isSensor, or when a static shape is modified.
        // There can easily be scenarios where the static proxy is in the moveSet but the dynamic proxy is not.
        // I could have some flag to indicate that there are any static bodies in the moveSet.

        // Is this proxy also moving?
        if (queryProxyType == b2BodyType.b2_dynamicBody)
        {
            if (treeType == b2BodyType.b2_dynamicBody && proxyKey < queryProxyKey)
            {
                bool moved = b2ContainsKey(broadPhase.moveSet, (ulong)(proxyKey + 1));
                if (moved)
                {
                    // Both proxies are moving. Avoid duplicate pairs.
                    return true;
                }
            }
        }
        else
        {
            Debug.Assert(treeType == b2BodyType.b2_dynamicBody);
            bool moved = b2ContainsKey(broadPhase.moveSet, (ulong)(proxyKey + 1));
            if (moved)
            {
                // Both proxies are moving. Avoid duplicate pairs.
                return true;
            }
        }

        ulong pairKey = B2_SHAPE_PAIR_KEY(shapeId, queryContext.queryShapeIndex);
        if (b2ContainsKey(broadPhase.pairSet, pairKey))
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

        b2World world = queryContext.world;

        b2Shape shapeA = b2Array_Get(world.shapes, shapeIdA);
        b2Shape shapeB = b2Array_Get(world.shapes, shapeIdB);

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
        b2Body bodyA = b2Array_Get(world.bodies, bodyIdA);
        b2Body bodyB = b2Array_Get(world.bodies, bodyIdB);
        if (b2ShouldBodiesCollide(world, bodyA, bodyB) == false)
        {
            return true;
        }

        // Custom user filter
        b2CustomFilterFcn customFilterFcn = queryContext.world.customFilterFcn;
        if (customFilterFcn != null)
        {
            b2ShapeId idA = new b2ShapeId(shapeIdA + 1, world.worldId, shapeA.generation);
            b2ShapeId idB = new b2ShapeId(shapeIdB + 1, world.worldId, shapeB.generation);
            bool shouldCollide = customFilterFcn(idA, idB, queryContext.world.customFilterContext);
            if (shouldCollide == false)
            {
                return true;
            }
        }

        // todo per thread to eliminate atomic?
        int pairIndex = b2AtomicFetchAddInt(broadPhase.movePairIndex, 1);

        b2MovePair pair;
        if (pairIndex < broadPhase.movePairCapacity)
        {
            pair = broadPhase.movePairs[pairIndex];
            pair.heap = false;
        }
        else
        {
            // TODO: @ikpil, check
            //pair = b2Alloc<b2MovePair>(1);( sizeof(  ) );
            pair = new b2MovePair();
            pair.heap = true;
        }

        pair.shapeIndexA = shapeIdA;
        pair.shapeIndexB = shapeIdB;
        pair.next = queryContext.moveResult.pairList;
        queryContext.moveResult.pairList = pair;

        // continue the query
        return true;
    }

// Warning: writing to these globals significantly slows multithreading performance
#if B2_SNOOP_PAIR_COUNTERS
b2TreeStats b2_dynamicStats;
b2TreeStats b2_kinematicStats;
b2TreeStats b2_staticStats;
#endif

    public static void b2FindPairsTask(int startIndex, int endIndex, uint threadIndex, object context)
    {
        b2TracyCZoneNC(b2TracyCZone.pair_task, "Pair", b2HexColor.b2_colorMediumSlateBlue, true);

        B2_UNUSED(threadIndex);

        b2World world = context as b2World;
        b2BroadPhase bp = world.broadPhase;

        b2QueryPairContext queryContext = new b2QueryPairContext();
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

            b2BodyType proxyType = B2_PROXY_TYPE(proxyKey);

            int proxyId = B2_PROXY_ID(proxyKey);
            queryContext.queryProxyKey = proxyKey;

            b2DynamicTree baseTree = bp.trees[(int)proxyType];

            // We have to query the tree with the fat AABB so that
            // we don't fail to create a contact that may touch later.
            b2AABB fatAABB = b2DynamicTree_GetAABB(baseTree, proxyId);
            queryContext.queryShapeIndex = b2DynamicTree_GetUserData(baseTree, proxyId);

            // Query trees. Only dynamic proxies collide with kinematic and static proxies.
            // Using B2_DEFAULT_MASK_BITS so that b2Filter::groupIndex works.
            b2TreeStats stats = new b2TreeStats();
            if (proxyType == b2BodyType.b2_dynamicBody)
            {
                // consider using bits = groupIndex > 0 ? B2_DEFAULT_MASK_BITS : maskBits
                queryContext.queryTreeType = b2BodyType.b2_kinematicBody;
                b2TreeStats statsKinematic = b2DynamicTree_Query(bp.trees[(int)b2BodyType.b2_kinematicBody], fatAABB, B2_DEFAULT_MASK_BITS, b2PairQueryCallback, queryContext);
                stats.nodeVisits += statsKinematic.nodeVisits;
                stats.leafVisits += statsKinematic.leafVisits;

                queryContext.queryTreeType = b2BodyType.b2_staticBody;
                b2TreeStats statsStatic = b2DynamicTree_Query(bp.trees[(int)b2BodyType.b2_staticBody], fatAABB, B2_DEFAULT_MASK_BITS, b2PairQueryCallback, queryContext);
                stats.nodeVisits += statsStatic.nodeVisits;
                stats.leafVisits += statsStatic.leafVisits;
            }

            // All proxies collide with dynamic proxies
            // Using B2_DEFAULT_MASK_BITS so that b2Filter::groupIndex works.
            queryContext.queryTreeType = b2BodyType.b2_dynamicBody;
            b2TreeStats statsDynamic = b2DynamicTree_Query(bp.trees[(int)b2BodyType.b2_dynamicBody], fatAABB, B2_DEFAULT_MASK_BITS, b2PairQueryCallback, queryContext);
            stats.nodeVisits += statsDynamic.nodeVisits;
            stats.leafVisits += statsDynamic.leafVisits;
        }

        b2TracyCZoneEnd(b2TracyCZone.pair_task);
    }

    public static void b2UpdateBroadPhasePairs(b2World world)
    {
        b2BroadPhase bp = world.broadPhase;

        int moveCount = bp.moveArray.count;
        Debug.Assert(moveCount == (int)bp.moveSet.count);

        if (moveCount == 0)
        {
            return;
        }

        b2TracyCZoneNC(b2TracyCZone.update_pairs, "Find Pairs", b2HexColor.b2_colorMediumSlateBlue, true);

        b2ArenaAllocator alloc = world.stackAllocator;

        // todo these could be in the step context
        bp.moveResults = b2AllocateArenaItem<b2MoveResult>(alloc, moveCount, "move results");
        bp.movePairCapacity = 16 * moveCount;
        bp.movePairs = b2AllocateArenaItem<b2MovePair>(alloc, bp.movePairCapacity, "move pairs");
        b2AtomicStoreInt(bp.movePairIndex, 0);

#if B2_SNOOP_TABLE_COUNTERS
	extern b2AtomicInt b2_probeCount;
	b2AtomicStoreInt(&b2_probeCount, 0);
#endif

        int minRange = 64;
        object userPairTask = world.enqueueTaskFcn(b2FindPairsTask, moveCount, minRange, world, world.userTaskContext);
        if (userPairTask != null)
        {
            world.finishTaskFcn(userPairTask, world.userTaskContext);
            world.taskCount += 1;
        }

        // todo_erin could start tree rebuild here

        b2TracyCZoneNC(b2TracyCZone.create_contacts, "Create Contacts", b2HexColor.b2_colorCoral, true);

        // Single-threaded work
        // - Clear move flags
        // - Create contacts in deterministic order
        for (int i = 0; i < moveCount; ++i)
        {
            b2MoveResult result = bp.moveResults[i];
            b2MovePair pair = result.pairList;
            while (pair != null)
            {
                int shapeIdA = pair.shapeIndexA;
                int shapeIdB = pair.shapeIndexB;

                // if (s_file != NULL)
                //{
                //	fprintf(s_file, "%d %d\n", shapeIdA, shapeIdB);
                // }

                b2Shape shapeA = b2Array_Get(world.shapes, shapeIdA);
                b2Shape shapeB = b2Array_Get(world.shapes, shapeIdB);

                b2CreateContact(world, shapeA, shapeB);

                if (pair.heap)
                {
                    b2MovePair temp = pair;
                    pair = pair.next;
                    b2Free([temp], 1);
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
        b2Array_Clear(bp.moveArray);
        b2ClearSet(bp.moveSet);

        b2FreeArenaItem(alloc, bp.movePairs);
        bp.movePairs = null;
        b2FreeArenaItem(alloc, bp.moveResults);
        bp.moveResults = null;

        b2ValidateSolverSets(world);

        b2TracyCZoneEnd(b2TracyCZone.create_contacts);

        b2TracyCZoneEnd(b2TracyCZone.update_pairs);
    }

    public static bool b2BroadPhase_TestOverlap(b2BroadPhase bp, int proxyKeyA, int proxyKeyB)
    {
        int typeIndexA = (int)B2_PROXY_TYPE(proxyKeyA);
        int proxyIdA = B2_PROXY_ID(proxyKeyA);
        int typeIndexB = (int)B2_PROXY_TYPE(proxyKeyB);
        int proxyIdB = B2_PROXY_ID(proxyKeyB);

        b2AABB aabbA = b2DynamicTree_GetAABB(bp.trees[typeIndexA], proxyIdA);
        b2AABB aabbB = b2DynamicTree_GetAABB(bp.trees[typeIndexB], proxyIdB);
        return b2AABB_Overlaps(aabbA, aabbB);
    }

    public static void b2BroadPhase_RebuildTrees(b2BroadPhase bp)
    {
        b2DynamicTree_Rebuild(bp.trees[(int)b2BodyType.b2_dynamicBody], false);
        b2DynamicTree_Rebuild(bp.trees[(int)b2BodyType.b2_kinematicBody], false);
    }

    public static int b2BroadPhase_GetShapeIndex(b2BroadPhase bp, int proxyKey)
    {
        int typeIndex = (int)B2_PROXY_TYPE(proxyKey);
        int proxyId = B2_PROXY_ID(proxyKey);

        return b2DynamicTree_GetUserData(bp.trees[typeIndex], proxyId);
    }

    public static void b2ValidateBroadphase(b2BroadPhase bp)
    {
        b2DynamicTree_Validate(bp.trees[(int)b2BodyType.b2_dynamicBody]);
        b2DynamicTree_Validate(bp.trees[(int)b2BodyType.b2_kinematicBody]);

        // TODO_ERIN validate every shape AABB is contained in tree AABB
    }

    public static void b2ValidateNoEnlarged(b2BroadPhase bp)
    {
#if B2_VALIDATE
	for ( int j = 0; j < (int)b2BodyType.b2_bodyTypeCount; ++j )
    {
        b2DynamicTree tree = bp.trees[j];
		b2DynamicTree_ValidateNoEnlarged( tree );
	}
#else
        B2_UNUSED(bp);
#endif
    }
}