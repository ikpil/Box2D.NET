// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT


using System;
using System.Diagnostics;
using Box2D.NET.Primitives;
using static Box2D.NET.array;
using static Box2D.NET.core;
using static Box2D.NET.constants;
using static Box2D.NET.world;
using static Box2D.NET.id_pool;
using static Box2D.NET.solver_set;
using static Box2D.NET.arena_allocator;
using static Box2D.NET.timer;

namespace Box2D.NET
{
// Deterministic solver
//
// Collide all awake contacts
// Use bit array to emit start/stop touching events in defined order, per thread. Try using contact index, assuming contacts are
// created in a deterministic order. bit-wise OR together bit arrays and issue changes:
// - start touching: merge islands - temporary linked list - mark root island dirty - wake all - largest island is root
// - stop touching: increment constraintRemoveCount

// Persistent island for awake bodies, joints, and contacts
// https://en.wikipedia.org/wiki/Component_(graph_theory)
// https://en.wikipedia.org/wiki/Dynamic_connectivity
// map from int to solver set and index
    public class b2Island
    {
        // index of solver set stored in b2World
        // may be B2_NULL_INDEX
        public int setIndex;

        // island index within set
        // may be B2_NULL_INDEX
        public int localIndex;

        public int islandId;

        public int headBody;
        public int tailBody;
        public int bodyCount;

        public int headContact;
        public int tailContact;
        public int contactCount;

        public int headJoint;
        public int tailJoint;
        public int jointCount;

        // Union find
        public int parentIsland;

        // Keeps track of how many contacts have been removed from this island.
        // This is used to determine if an island is a candidate for splitting.
        public int constraintRemoveCount;
    }

// This is used to move islands across solver sets
    public class b2IslandSim
    {
        public int islandId;

        public void CopyFrom(b2IslandSim other)
        {
            islandId = other.islandId;
        }
    }

    public class island
    {
        public static b2Island b2CreateIsland(b2World world, int setIndex)
        {
            Debug.Assert(setIndex == (int)b2SetType.b2_awakeSet || setIndex >= (int)b2SetType.b2_firstSleepingSet);

            int islandId = b2AllocId(world.islandIdPool);

            if (islandId == world.islands.count)
            {
                b2Island emptyIsland = new b2Island();
                b2Array_Push(world.islands, emptyIsland);
            }
            else
            {
                Debug.Assert(world.islands.data[islandId].setIndex == B2_NULL_INDEX);
            }

            b2SolverSet set = b2Array_Get(world.solverSets, setIndex);

            b2Island island = b2Array_Get(world.islands, islandId);
            island.setIndex = setIndex;
            island.localIndex = set.islandSims.count;
            island.islandId = islandId;
            island.headBody = B2_NULL_INDEX;
            island.tailBody = B2_NULL_INDEX;
            island.bodyCount = 0;
            island.headContact = B2_NULL_INDEX;
            island.tailContact = B2_NULL_INDEX;
            island.contactCount = 0;
            island.headJoint = B2_NULL_INDEX;
            island.tailJoint = B2_NULL_INDEX;
            island.jointCount = 0;
            island.parentIsland = B2_NULL_INDEX;
            island.constraintRemoveCount = 0;

            ref b2IslandSim islandSim = ref b2Array_Add(set.islandSims);
            islandSim.islandId = islandId;

            return island;
        }

        public static void b2DestroyIsland(b2World world, int islandId)
        {
            // assume island is empty
            b2Island island = b2Array_Get(world.islands, islandId);
            b2SolverSet set = b2Array_Get(world.solverSets, island.setIndex);
            int movedIndex = b2Array_RemoveSwap(set.islandSims, island.localIndex);
            if (movedIndex != B2_NULL_INDEX)
            {
                // Fix index on moved element
                b2IslandSim movedElement = set.islandSims.data[island.localIndex];
                int movedId = movedElement.islandId;
                b2Island movedIsland = b2Array_Get(world.islands, movedId);
                Debug.Assert(movedIsland.localIndex == movedIndex);
                movedIsland.localIndex = island.localIndex;
            }

            // Free island and id (preserve island revision)
            island.islandId = B2_NULL_INDEX;
            island.setIndex = B2_NULL_INDEX;
            island.localIndex = B2_NULL_INDEX;
            b2FreeId(world.islandIdPool, islandId);
        }

        public static void b2AddContactToIsland(b2World world, int islandId, b2Contact contact)
        {
            Debug.Assert(contact.islandId == B2_NULL_INDEX);
            Debug.Assert(contact.islandPrev == B2_NULL_INDEX);
            Debug.Assert(contact.islandNext == B2_NULL_INDEX);

            b2Island island = b2Array_Get(world.islands, islandId);

            if (island.headContact != B2_NULL_INDEX)
            {
                contact.islandNext = island.headContact;
                b2Contact headContact = b2Array_Get(world.contacts, island.headContact);
                headContact.islandPrev = contact.contactId;
            }

            island.headContact = contact.contactId;
            if (island.tailContact == B2_NULL_INDEX)
            {
                island.tailContact = island.headContact;
            }

            island.contactCount += 1;
            contact.islandId = islandId;

            b2ValidateIsland(world, islandId);
        }

// Link contacts into the island graph when it starts having contact points
// Link a contact into an island.
// This performs union-find and path compression to join islands.
// https://en.wikipedia.org/wiki/Disjoint-set_data_structure
        public static void b2LinkContact(b2World world, b2Contact contact)
        {
            Debug.Assert((contact.flags & (uint)b2ContactFlags.b2_contactTouchingFlag) != 0);

            int bodyIdA = contact.edges[0].bodyId;
            int bodyIdB = contact.edges[1].bodyId;

            b2Body bodyA = b2Array_Get(world.bodies, bodyIdA);
            b2Body bodyB = b2Array_Get(world.bodies, bodyIdB);

            Debug.Assert(bodyA.setIndex != (int)b2SetType.b2_disabledSet && bodyB.setIndex != (int)b2SetType.b2_disabledSet);
            Debug.Assert(bodyA.setIndex != (int)b2SetType.b2_staticSet || bodyB.setIndex != (int)b2SetType.b2_staticSet);

            // Wake bodyB if bodyA is awake and bodyB is sleeping
            if (bodyA.setIndex == (int)b2SetType.b2_awakeSet && bodyB.setIndex >= (int)b2SetType.b2_firstSleepingSet)
            {
                b2WakeSolverSet(world, bodyB.setIndex);
            }

            // Wake bodyA if bodyB is awake and bodyA is sleeping
            if (bodyB.setIndex == (int)b2SetType.b2_awakeSet && bodyA.setIndex >= (int)b2SetType.b2_firstSleepingSet)
            {
                b2WakeSolverSet(world, bodyA.setIndex);
            }

            int islandIdA = bodyA.islandId;
            int islandIdB = bodyB.islandId;

            // Static bodies have null island indices.
            Debug.Assert(bodyA.setIndex != (int)b2SetType.b2_staticSet || islandIdA == B2_NULL_INDEX);
            Debug.Assert(bodyB.setIndex != (int)b2SetType.b2_staticSet || islandIdB == B2_NULL_INDEX);
            Debug.Assert(islandIdA != B2_NULL_INDEX || islandIdB != B2_NULL_INDEX);

            if (islandIdA == islandIdB)
            {
                // Contact in same island
                b2AddContactToIsland(world, islandIdA, contact);
                return;
            }

            // Union-find root of islandA
            b2Island islandA = null;
            if (islandIdA != B2_NULL_INDEX)
            {
                islandA = b2Array_Get(world.islands, islandIdA);
                int parentId = islandA.parentIsland;
                while (parentId != B2_NULL_INDEX)
                {
                    b2Island parent = b2Array_Get(world.islands, parentId);
                    if (parent.parentIsland != B2_NULL_INDEX)
                    {
                        // path compression
                        islandA.parentIsland = parent.parentIsland;
                    }

                    islandA = parent;
                    islandIdA = parentId;
                    parentId = islandA.parentIsland;
                }
            }

            // Union-find root of islandB
            b2Island islandB = null;
            if (islandIdB != B2_NULL_INDEX)
            {
                islandB = b2Array_Get(world.islands, islandIdB);
                int parentId = islandB.parentIsland;
                while (islandB.parentIsland != B2_NULL_INDEX)
                {
                    b2Island parent = b2Array_Get(world.islands, parentId);
                    if (parent.parentIsland != B2_NULL_INDEX)
                    {
                        // path compression
                        islandB.parentIsland = parent.parentIsland;
                    }

                    islandB = parent;
                    islandIdB = parentId;
                    parentId = islandB.parentIsland;
                }
            }

            Debug.Assert(islandA != null || islandB != null);

            // Union-Find link island roots
            if (islandA != islandB && islandA != null && islandB != null)
            {
                Debug.Assert(islandA != islandB);
                Debug.Assert(islandB.parentIsland == B2_NULL_INDEX);
                islandB.parentIsland = islandIdA;
            }

            if (islandA != null)
            {
                b2AddContactToIsland(world, islandIdA, contact);
            }
            else
            {
                b2AddContactToIsland(world, islandIdB, contact);
            }
        }

// Unlink contact from the island graph when it stops having contact points
// This is called when a contact no longer has contact points or when a contact is destroyed.
        public static void b2UnlinkContact(b2World world, b2Contact contact)
        {
            Debug.Assert(contact.islandId != B2_NULL_INDEX);

            // remove from island
            int islandId = contact.islandId;
            b2Island island = b2Array_Get(world.islands, islandId);

            if (contact.islandPrev != B2_NULL_INDEX)
            {
                b2Contact prevContact = b2Array_Get(world.contacts, contact.islandPrev);
                Debug.Assert(prevContact.islandNext == contact.contactId);
                prevContact.islandNext = contact.islandNext;
            }

            if (contact.islandNext != B2_NULL_INDEX)
            {
                b2Contact nextContact = b2Array_Get(world.contacts, contact.islandNext);
                Debug.Assert(nextContact.islandPrev == contact.contactId);
                nextContact.islandPrev = contact.islandPrev;
            }

            if (island.headContact == contact.contactId)
            {
                island.headContact = contact.islandNext;
            }

            if (island.tailContact == contact.contactId)
            {
                island.tailContact = contact.islandPrev;
            }

            Debug.Assert(island.contactCount > 0);
            island.contactCount -= 1;
            island.constraintRemoveCount += 1;

            contact.islandId = B2_NULL_INDEX;
            contact.islandPrev = B2_NULL_INDEX;
            contact.islandNext = B2_NULL_INDEX;

            b2ValidateIsland(world, islandId);
        }

        public static void b2AddJointToIsland(b2World world, int islandId, b2Joint joint)
        {
            Debug.Assert(joint.islandId == B2_NULL_INDEX);
            Debug.Assert(joint.islandPrev == B2_NULL_INDEX);
            Debug.Assert(joint.islandNext == B2_NULL_INDEX);

            b2Island island = b2Array_Get(world.islands, islandId);

            if (island.headJoint != B2_NULL_INDEX)
            {
                joint.islandNext = island.headJoint;
                b2Joint headJoint = b2Array_Get(world.joints, island.headJoint);
                headJoint.islandPrev = joint.jointId;
            }

            island.headJoint = joint.jointId;
            if (island.tailJoint == B2_NULL_INDEX)
            {
                island.tailJoint = island.headJoint;
            }

            island.jointCount += 1;
            joint.islandId = islandId;

            b2ValidateIsland(world, islandId);
        }

// Link a joint into the island graph when it is created
        public static void b2LinkJoint(b2World world, b2Joint joint, bool mergeIslands)
        {
            b2Body bodyA = b2Array_Get(world.bodies, joint.edges[0].bodyId);
            b2Body bodyB = b2Array_Get(world.bodies, joint.edges[1].bodyId);

            if (bodyA.setIndex == (int)b2SetType.b2_awakeSet && bodyB.setIndex >= (int)b2SetType.b2_firstSleepingSet)
            {
                b2WakeSolverSet(world, bodyB.setIndex);
            }
            else if (bodyB.setIndex == (int)b2SetType.b2_awakeSet && bodyA.setIndex >= (int)b2SetType.b2_firstSleepingSet)
            {
                b2WakeSolverSet(world, bodyA.setIndex);
            }

            int islandIdA = bodyA.islandId;
            int islandIdB = bodyB.islandId;

            Debug.Assert(islandIdA != B2_NULL_INDEX || islandIdB != B2_NULL_INDEX);

            if (islandIdA == islandIdB)
            {
                // Joint in same island
                b2AddJointToIsland(world, islandIdA, joint);
                return;
            }

            // Union-find root of islandA
            b2Island islandA = null;
            if (islandIdA != B2_NULL_INDEX)
            {
                islandA = b2Array_Get(world.islands, islandIdA);
                while (islandA.parentIsland != B2_NULL_INDEX)
                {
                    b2Island parent = b2Array_Get(world.islands, islandA.parentIsland);
                    if (parent.parentIsland != B2_NULL_INDEX)
                    {
                        // path compression
                        islandA.parentIsland = parent.parentIsland;
                    }

                    islandIdA = islandA.parentIsland;
                    islandA = parent;
                }
            }

            // Union-find root of islandB
            b2Island islandB = null;
            if (islandIdB != B2_NULL_INDEX)
            {
                islandB = b2Array_Get(world.islands, islandIdB);
                while (islandB.parentIsland != B2_NULL_INDEX)
                {
                    b2Island parent = b2Array_Get(world.islands, islandB.parentIsland);
                    if (parent.parentIsland != B2_NULL_INDEX)
                    {
                        // path compression
                        islandB.parentIsland = parent.parentIsland;
                    }

                    islandIdB = islandB.parentIsland;
                    islandB = parent;
                }
            }

            Debug.Assert(islandA != null || islandB != null);

            // Union-Find link island roots
            if (islandA != islandB && islandA != null && islandB != null)
            {
                Debug.Assert(islandA != islandB);
                Debug.Assert(islandB.parentIsland == B2_NULL_INDEX);
                islandB.parentIsland = islandIdA;
            }

            if (islandA != null)
            {
                b2AddJointToIsland(world, islandIdA, joint);
            }
            else
            {
                b2AddJointToIsland(world, islandIdB, joint);
            }

            // Joints need to have islands merged immediately when they are created
            // to keep the island graph valid.
            // However, when a body type is being changed the merge can be deferred until
            // all joints are linked.
            if (mergeIslands)
            {
                b2MergeAwakeIslands(world);
            }
        }

// Unlink a joint from the island graph when it is destroyed
        public static void b2UnlinkJoint(b2World world, b2Joint joint)
        {
            Debug.Assert(joint.islandId != B2_NULL_INDEX);

            // remove from island
            int islandId = joint.islandId;
            b2Island island = b2Array_Get(world.islands, islandId);

            if (joint.islandPrev != B2_NULL_INDEX)
            {
                b2Joint prevJoint = b2Array_Get(world.joints, joint.islandPrev);
                Debug.Assert(prevJoint.islandNext == joint.jointId);
                prevJoint.islandNext = joint.islandNext;
            }

            if (joint.islandNext != B2_NULL_INDEX)
            {
                b2Joint nextJoint = b2Array_Get(world.joints, joint.islandNext);
                Debug.Assert(nextJoint.islandPrev == joint.jointId);
                nextJoint.islandPrev = joint.islandPrev;
            }

            if (island.headJoint == joint.jointId)
            {
                island.headJoint = joint.islandNext;
            }

            if (island.tailJoint == joint.jointId)
            {
                island.tailJoint = joint.islandPrev;
            }

            Debug.Assert(island.jointCount > 0);
            island.jointCount -= 1;
            island.constraintRemoveCount += 1;

            joint.islandId = B2_NULL_INDEX;
            joint.islandPrev = B2_NULL_INDEX;
            joint.islandNext = B2_NULL_INDEX;

            b2ValidateIsland(world, islandId);
        }

// Merge an island into its root island.
// todo we can assume all islands are awake here
        public static void b2MergeIsland(b2World world, b2Island island)
        {
            Debug.Assert(island.parentIsland != B2_NULL_INDEX);

            int rootId = island.parentIsland;
            b2Island rootIsland = b2Array_Get(world.islands, rootId);
            Debug.Assert(rootIsland.parentIsland == B2_NULL_INDEX);

            // remap island indices
            int bodyId = island.headBody;
            while (bodyId != B2_NULL_INDEX)
            {
                b2Body body = b2Array_Get(world.bodies, bodyId);
                body.islandId = rootId;
                bodyId = body.islandNext;
            }

            int contactId = island.headContact;
            while (contactId != B2_NULL_INDEX)
            {
                b2Contact contact = b2Array_Get(world.contacts, contactId);
                contact.islandId = rootId;
                contactId = contact.islandNext;
            }

            int jointId = island.headJoint;
            while (jointId != B2_NULL_INDEX)
            {
                b2Joint joint = b2Array_Get(world.joints, jointId);
                joint.islandId = rootId;
                jointId = joint.islandNext;
            }

            // connect body lists
            Debug.Assert(rootIsland.tailBody != B2_NULL_INDEX);
            b2Body tailBody = b2Array_Get(world.bodies, rootIsland.tailBody);
            Debug.Assert(tailBody.islandNext == B2_NULL_INDEX);
            tailBody.islandNext = island.headBody;

            Debug.Assert(island.headBody != B2_NULL_INDEX);
            b2Body headBody = b2Array_Get(world.bodies, island.headBody);
            Debug.Assert(headBody.islandPrev == B2_NULL_INDEX);
            headBody.islandPrev = rootIsland.tailBody;

            rootIsland.tailBody = island.tailBody;
            rootIsland.bodyCount += island.bodyCount;

            // connect contact lists
            if (rootIsland.headContact == B2_NULL_INDEX)
            {
                // Root island has no contacts
                Debug.Assert(rootIsland.tailContact == B2_NULL_INDEX && rootIsland.contactCount == 0);
                rootIsland.headContact = island.headContact;
                rootIsland.tailContact = island.tailContact;
                rootIsland.contactCount = island.contactCount;
            }
            else if (island.headContact != B2_NULL_INDEX)
            {
                // Both islands have contacts
                Debug.Assert(island.tailContact != B2_NULL_INDEX && island.contactCount > 0);
                Debug.Assert(rootIsland.tailContact != B2_NULL_INDEX && rootIsland.contactCount > 0);

                b2Contact tailContact = b2Array_Get(world.contacts, rootIsland.tailContact);
                Debug.Assert(tailContact.islandNext == B2_NULL_INDEX);
                tailContact.islandNext = island.headContact;

                b2Contact headContact = b2Array_Get(world.contacts, island.headContact);
                Debug.Assert(headContact.islandPrev == B2_NULL_INDEX);
                headContact.islandPrev = rootIsland.tailContact;

                rootIsland.tailContact = island.tailContact;
                rootIsland.contactCount += island.contactCount;
            }

            if (rootIsland.headJoint == B2_NULL_INDEX)
            {
                // Root island has no joints
                Debug.Assert(rootIsland.tailJoint == B2_NULL_INDEX && rootIsland.jointCount == 0);
                rootIsland.headJoint = island.headJoint;
                rootIsland.tailJoint = island.tailJoint;
                rootIsland.jointCount = island.jointCount;
            }
            else if (island.headJoint != B2_NULL_INDEX)
            {
                // Both islands have joints
                Debug.Assert(island.tailJoint != B2_NULL_INDEX && island.jointCount > 0);
                Debug.Assert(rootIsland.tailJoint != B2_NULL_INDEX && rootIsland.jointCount > 0);

                b2Joint tailJoint = b2Array_Get(world.joints, rootIsland.tailJoint);
                Debug.Assert(tailJoint.islandNext == B2_NULL_INDEX);
                tailJoint.islandNext = island.headJoint;

                b2Joint headJoint = b2Array_Get(world.joints, island.headJoint);
                Debug.Assert(headJoint.islandPrev == B2_NULL_INDEX);
                headJoint.islandPrev = rootIsland.tailJoint;

                rootIsland.tailJoint = island.tailJoint;
                rootIsland.jointCount += island.jointCount;
            }

            // Track removed constraints
            rootIsland.constraintRemoveCount += island.constraintRemoveCount;

            b2ValidateIsland(world, rootId);
        }

// Iterate over all awake islands and merge any that need merging
// Islands that get merged into a root island will be removed from the awake island array
// and returned to the pool.
// todo this might be faster if b2IslandSim held the connectivity data
        public static void b2MergeAwakeIslands(b2World world)
        {
            b2TracyCZoneNC(b2TracyCZone.merge_islands, "Merge Islands", b2HexColor.b2_colorMediumTurquoise, true);

            b2SolverSet awakeSet = b2Array_Get(world.solverSets, (int)b2SetType.b2_awakeSet);
            b2IslandSim[] islandSims = awakeSet.islandSims.data;
            int awakeIslandCount = awakeSet.islandSims.count;

            // Step 1: Ensure every child island points to its root island. This avoids merging a child island with
            // a parent island that has already been merged with a grand-parent island.
            for (int i = 0; i < awakeIslandCount; ++i)
            {
                int islandId = islandSims[i].islandId;

                b2Island island = b2Array_Get(world.islands, islandId);

                // find the root island
                int rootId = islandId;
                b2Island rootIsland = island;
                while (rootIsland.parentIsland != B2_NULL_INDEX)
                {
                    b2Island parent = b2Array_Get(world.islands, rootIsland.parentIsland);
                    if (parent.parentIsland != B2_NULL_INDEX)
                    {
                        // path compression
                        rootIsland.parentIsland = parent.parentIsland;
                    }

                    rootId = rootIsland.parentIsland;
                    rootIsland = parent;
                }

                if (rootIsland != island)
                {
                    island.parentIsland = rootId;
                }
            }

            // Step 2: merge every awake island into its parent (which must be a root island)
            // Reverse to support removal from awake array.
            for (int i = awakeIslandCount - 1; i >= 0; --i)
            {
                int islandId = islandSims[i].islandId;
                b2Island island = b2Array_Get(world.islands, islandId);

                if (island.parentIsland == B2_NULL_INDEX)
                {
                    continue;
                }

                b2MergeIsland(world, island);

                // this call does a remove swap from the end of the island sim array
                b2DestroyIsland(world, islandId);
            }

            b2ValidateConnectivity(world);

            b2TracyCZoneEnd(b2TracyCZone.merge_islands);
        }

        public const int B2_CONTACT_REMOVE_THRESHOLD = 1;

        public static void b2SplitIsland(b2World world, int baseId)
        {
            b2Island baseIsland = b2Array_Get(world.islands, baseId);
            int setIndex = baseIsland.setIndex;

            if (setIndex != (int)b2SetType.b2_awakeSet)
            {
                // can only split awake island
                return;
            }

            if (baseIsland.constraintRemoveCount == 0)
            {
                // this island doesn't need to be split
                return;
            }

            b2ValidateIsland(world, baseId);

            int bodyCount = baseIsland.bodyCount;

            b2Body[] bodies = world.bodies.data;
            b2ArenaAllocator alloc = world.stackAllocator;

            // No lock is needed because I ensure the allocator is not used while this task is active.
            ArraySegment<int> stack = b2AllocateArenaItem<int>(alloc, bodyCount * sizeof(int), "island stack");
            ArraySegment<int> bodyIds = b2AllocateArenaItem<int>(alloc, bodyCount * sizeof(int), "body ids");

            // Build array containing all body indices from @base island. These
            // serve as seed bodies for the depth first search (DFS).
            int index = 0;
            int nextBody = baseIsland.headBody;
            while (nextBody != B2_NULL_INDEX)
            {
                bodyIds[index++] = nextBody;
                b2Body body = bodies[nextBody];

                // Clear visitation mark
                body.isMarked = false;

                nextBody = body.islandNext;
            }

            Debug.Assert(index == bodyCount);

            // Clear contact island flags. Only need to consider contacts
            // already in the @base island.
            int nextContactId = baseIsland.headContact;
            while (nextContactId != B2_NULL_INDEX)
            {
                b2Contact contact = b2Array_Get(world.contacts, nextContactId);
                contact.isMarked = false;
                nextContactId = contact.islandNext;
            }

            // Clear joint island flags.
            int nextJoint = baseIsland.headJoint;
            while (nextJoint != B2_NULL_INDEX)
            {
                b2Joint joint = b2Array_Get(world.joints, nextJoint);
                joint.isMarked = false;
                nextJoint = joint.islandNext;
            }

            // Done with the @base split island.
            b2DestroyIsland(world, baseId);

            // Each island is found as a depth first search starting from a seed body
            for (int i = 0; i < bodyCount; ++i)
            {
                int seedIndex = bodyIds[i];
                b2Body seed = bodies[seedIndex];
                Debug.Assert(seed.setIndex == setIndex);

                if (seed.isMarked == true)
                {
                    // The body has already been visited
                    continue;
                }

                int stackCount = 0;
                stack[stackCount++] = seedIndex;
                seed.isMarked = true;

                // Create new island
                // No lock needed because only a single island can split per time step. No islands are being used during the constraint
                // solve. However, islands are touched during body finalization.
                b2Island island = b2CreateIsland(world, setIndex);

                int islandId = island.islandId;

                // Perform a depth first search (DFS) on the constraint graph.
                while (stackCount > 0)
                {
                    // Grab the next body off the stack and add it to the island.
                    int bodyId = stack[--stackCount];
                    b2Body body = bodies[bodyId];
                    Debug.Assert(body.setIndex == (int)b2SetType.b2_awakeSet);
                    Debug.Assert(body.isMarked == true);

                    // Add body to island
                    body.islandId = islandId;
                    if (island.tailBody != B2_NULL_INDEX)
                    {
                        bodies[island.tailBody].islandNext = bodyId;
                    }

                    body.islandPrev = island.tailBody;
                    body.islandNext = B2_NULL_INDEX;
                    island.tailBody = bodyId;

                    if (island.headBody == B2_NULL_INDEX)
                    {
                        island.headBody = bodyId;
                    }

                    island.bodyCount += 1;

                    // Search all contacts connected to this body.
                    int contactKey = body.headContactKey;
                    while (contactKey != B2_NULL_INDEX)
                    {
                        int contactId = contactKey >> 1;
                        int edgeIndex = contactKey & 1;

                        b2Contact contact = b2Array_Get(world.contacts, contactId);
                        Debug.Assert(contact.contactId == contactId);

                        // Next key
                        contactKey = contact.edges[edgeIndex].nextKey;

                        // Has this contact already been added to this island?
                        if (contact.isMarked)
                        {
                            continue;
                        }

                        // Is this contact enabled and touching?
                        if ((contact.flags & (uint)b2ContactFlags.b2_contactTouchingFlag) == 0)
                        {
                            continue;
                        }

                        contact.isMarked = true;

                        int otherEdgeIndex = edgeIndex ^ 1;
                        int otherBodyId = contact.edges[otherEdgeIndex].bodyId;
                        b2Body otherBody = bodies[otherBodyId];

                        // Maybe add other body to stack
                        if (otherBody.isMarked == false && otherBody.setIndex != (int)b2SetType.b2_staticSet)
                        {
                            Debug.Assert(stackCount < bodyCount);
                            stack[stackCount++] = otherBodyId;
                            otherBody.isMarked = true;
                        }

                        // Add contact to island
                        contact.islandId = islandId;
                        if (island.tailContact != B2_NULL_INDEX)
                        {
                            b2Contact tailContact = b2Array_Get(world.contacts, island.tailContact);
                            tailContact.islandNext = contactId;
                        }

                        contact.islandPrev = island.tailContact;
                        contact.islandNext = B2_NULL_INDEX;
                        island.tailContact = contactId;

                        if (island.headContact == B2_NULL_INDEX)
                        {
                            island.headContact = contactId;
                        }

                        island.contactCount += 1;
                    }

                    // Search all joints connect to this body.
                    int jointKey = body.headJointKey;
                    while (jointKey != B2_NULL_INDEX)
                    {
                        int jointId = jointKey >> 1;
                        int edgeIndex = jointKey & 1;

                        b2Joint joint = b2Array_Get(world.joints, jointId);
                        Debug.Assert(joint.jointId == jointId);

                        // Next key
                        jointKey = joint.edges[edgeIndex].nextKey;

                        // Has this joint already been added to this island?
                        if (joint.isMarked)
                        {
                            continue;
                        }

                        joint.isMarked = true;

                        int otherEdgeIndex = edgeIndex ^ 1;
                        int otherBodyId = joint.edges[otherEdgeIndex].bodyId;
                        b2Body otherBody = bodies[otherBodyId];

                        // Don't simulate joints connected to disabled bodies.
                        if (otherBody.setIndex == (int)b2SetType.b2_disabledSet)
                        {
                            continue;
                        }

                        // Maybe add other body to stack
                        if (otherBody.isMarked == false && otherBody.setIndex == (int)b2SetType.b2_awakeSet)
                        {
                            Debug.Assert(stackCount < bodyCount);
                            stack[stackCount++] = otherBodyId;
                            otherBody.isMarked = true;
                        }

                        // Add joint to island
                        joint.islandId = islandId;
                        if (island.tailJoint != B2_NULL_INDEX)
                        {
                            b2Joint tailJoint = b2Array_Get(world.joints, island.tailJoint);
                            tailJoint.islandNext = jointId;
                        }

                        joint.islandPrev = island.tailJoint;
                        joint.islandNext = B2_NULL_INDEX;
                        island.tailJoint = jointId;

                        if (island.headJoint == B2_NULL_INDEX)
                        {
                            island.headJoint = jointId;
                        }

                        island.jointCount += 1;
                    }
                }

                b2ValidateIsland(world, islandId);
            }

            b2FreeArenaItem(alloc, bodyIds);
            b2FreeArenaItem(alloc, stack);
        }

// Split an island because some contacts and/or joints have been removed.
// This is called during the constraint solve while islands are not being touched. This uses DFS and touches a lot of memory,
// so it can be quite slow.
// Note: contacts/joints connected to static bodies must belong to an island but don't affect island connectivity
// Note: static bodies are never in an island
// Note: this task interacts with some allocators without locks under the assumption that no other tasks
// are interacting with these data structures.
        public static void b2SplitIslandTask(int startIndex, int endIndex, uint threadIndex, object context)
        {
            b2TracyCZoneNC(b2TracyCZone.split, "Split Island", b2HexColor.b2_colorOlive, true);

            B2_UNUSED(startIndex, endIndex, threadIndex);

            ulong ticks = b2GetTicks();
            b2World world = context as b2World;

            Debug.Assert(world.splitIslandId != B2_NULL_INDEX);

            b2SplitIsland(world, world.splitIslandId);

            world.profile.splitIslands += b2GetMilliseconds(ticks);
            b2TracyCZoneEnd(b2TracyCZone.split);
        }

#if B2_VALIDATE
void b2ValidateIsland( b2World* world, int islandId )
{
	b2Island* island = Array_Get( &world.islands, islandId );
	Debug.Assert( island.islandId == islandId );
	Debug.Assert( island.setIndex != B2_NULL_INDEX );
	Debug.Assert( island.headBody != B2_NULL_INDEX );

	{
		Debug.Assert( island.tailBody != B2_NULL_INDEX );
		Debug.Assert( island.bodyCount > 0 );
		if ( island.bodyCount > 1 )
		{
			Debug.Assert( island.tailBody != island.headBody );
		}
		Debug.Assert( island.bodyCount <= b2GetIdCount( &world.bodyIdPool ) );

		int count = 0;
		int bodyId = island.headBody;
		while ( bodyId != B2_NULL_INDEX )
		{
			b2Body* body = Array_Get(&world.bodies, bodyId);
			Debug.Assert( body.islandId == islandId );
			Debug.Assert( body.setIndex == island.setIndex );
			count += 1;

			if ( count == island.bodyCount )
			{
				Debug.Assert( bodyId == island.tailBody );
			}

			bodyId = body.islandNext;
		}
		Debug.Assert( count == island.bodyCount );
	}

	if ( island.headContact != B2_NULL_INDEX )
	{
		Debug.Assert( island.tailContact != B2_NULL_INDEX );
		Debug.Assert( island.contactCount > 0 );
		if ( island.contactCount > 1 )
		{
			Debug.Assert( island.tailContact != island.headContact );
		}
		Debug.Assert( island.contactCount <= b2GetIdCount( &world.contactIdPool ) );

		int count = 0;
		int contactId = island.headContact;
		while ( contactId != B2_NULL_INDEX )
		{
			b2Contact* contact = Array_Get( &world.contacts, contactId );
			Debug.Assert( contact.setIndex == island.setIndex );
			Debug.Assert( contact.islandId == islandId );
			count += 1;

			if ( count == island.contactCount )
			{
				Debug.Assert( contactId == island.tailContact );
			}

			contactId = contact.islandNext;
		}
		Debug.Assert( count == island.contactCount );
	}
	else
	{
		Debug.Assert( island.tailContact == B2_NULL_INDEX );
		Debug.Assert( island.contactCount == 0 );
	}

	if ( island.headJoint != B2_NULL_INDEX )
	{
		Debug.Assert( island.tailJoint != B2_NULL_INDEX );
		Debug.Assert( island.jointCount > 0 );
		if ( island.jointCount > 1 )
		{
			Debug.Assert( island.tailJoint != island.headJoint );
		}
		Debug.Assert( island.jointCount <= b2GetIdCount( &world.jointIdPool ) );

		int count = 0;
		int jointId = island.headJoint;
		while ( jointId != B2_NULL_INDEX )
		{
			b2Joint* joint = Array_Get( &world.joints, jointId );
			Debug.Assert( joint.setIndex == island.setIndex );
			count += 1;

			if ( count == island.jointCount )
			{
				Debug.Assert( jointId == island.tailJoint );
			}

			jointId = joint.islandNext;
		}
		Debug.Assert( count == island.jointCount );
	}
	else
	{
		Debug.Assert( island.tailJoint == B2_NULL_INDEX );
		Debug.Assert( island.jointCount == 0 );
	}
}

#else

        public static void b2ValidateIsland(b2World world, int islandId)
        {
            B2_UNUSED(world);
            B2_UNUSED(islandId);
        }
#endif
    }
}