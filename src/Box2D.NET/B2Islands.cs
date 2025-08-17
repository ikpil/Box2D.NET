// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Profiling;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2IdPools;
using static Box2D.NET.B2SolverSets;
using static Box2D.NET.B2ArenaAllocators;
using static Box2D.NET.B2Timers;

namespace Box2D.NET
{
    // Deterministic solver
    //
    // Collide all awake contacts
    // Use bit array to emit start/stop touching events in defined order, per thread. Try using contact index, assuming contacts are
    // created in a deterministic order. bit-wise OR together bit arrays and issue changes:
    // - start touching: merge islands - temporary linked list - mark root island dirty - wake all - largest island is root
    // - stop touching: increment constraintRemoveCount
    public static class B2Islands
    {
        public const int B2_CONTACT_REMOVE_THRESHOLD = 1;

        public static B2Island b2CreateIsland(B2World world, int setIndex)
        {
            B2_ASSERT(setIndex == (int)B2SetType.b2_awakeSet || setIndex >= (int)B2SetType.b2_firstSleepingSet);

            int islandId = b2AllocId(world.islandIdPool);

            if (islandId == world.islands.count)
            {
                B2Island emptyIsland = new B2Island();
                b2Array_Push(ref world.islands, emptyIsland);
            }
            else
            {
                B2_ASSERT(world.islands.data[islandId].setIndex == B2_NULL_INDEX);
            }

            B2SolverSet set = b2Array_Get(ref world.solverSets, setIndex);

            B2Island island = b2Array_Get(ref world.islands, islandId);
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
            island.constraintRemoveCount = 0;

            ref B2IslandSim islandSim = ref b2Array_Add(ref set.islandSims);
            islandSim.islandId = islandId;

            return island;
        }

        public static void b2DestroyIsland(B2World world, int islandId)
        {
            if (world.splitIslandId == islandId)
            {
                world.splitIslandId = B2_NULL_INDEX;
            }

            // assume island is empty
            B2Island island = b2Array_Get(ref world.islands, islandId);
            B2SolverSet set = b2Array_Get(ref world.solverSets, island.setIndex);
            int movedIndex = b2Array_RemoveSwap(ref set.islandSims, island.localIndex);
            if (movedIndex != B2_NULL_INDEX)
            {
                // Fix index on moved element
                B2IslandSim movedElement = set.islandSims.data[island.localIndex];
                int movedId = movedElement.islandId;
                B2Island movedIsland = b2Array_Get(ref world.islands, movedId);
                B2_ASSERT(movedIsland.localIndex == movedIndex);
                movedIsland.localIndex = island.localIndex;
            }

            // Free island and id (preserve island revision)
            island.islandId = B2_NULL_INDEX;
            island.setIndex = B2_NULL_INDEX;
            island.localIndex = B2_NULL_INDEX;
            b2FreeId(world.islandIdPool, islandId);
        }


        public static int b2MergeIslands(B2World world, int islandIdA, int islandIdB)
        {
            if (islandIdA == islandIdB)
            {
                return islandIdA;
            }

            if (islandIdA == B2_NULL_INDEX)
            {
                B2_ASSERT(islandIdB != B2_NULL_INDEX);
                return islandIdB;
            }

            if (islandIdB == B2_NULL_INDEX)
            {
                B2_ASSERT(islandIdA != B2_NULL_INDEX);
                return islandIdA;
            }

            B2Island islandA = b2Array_Get(ref world.islands, islandIdA);
            B2Island islandB = b2Array_Get(ref world.islands, islandIdB);

            // Keep the biggest island to reduce cache misses
            B2Island big;
            B2Island small;
            if (islandA.bodyCount >= islandB.bodyCount)
            {
                big = islandA;
                small = islandB;
            }
            else
            {
                big = islandB;
                small = islandA;
            }

            int bigId = big.islandId;

            // remap island indices (cache misses)
            int bodyId = small.headBody;
            while (bodyId != B2_NULL_INDEX)
            {
                B2Body body = b2Array_Get(ref world.bodies, bodyId);
                body.islandId = bigId;
                bodyId = body.islandNext;
            }

            int contactId = small.headContact;
            while (contactId != B2_NULL_INDEX)
            {
                B2Contact contact = b2Array_Get(ref world.contacts, contactId);
                contact.islandId = bigId;
                contactId = contact.islandNext;
            }

            int jointId = small.headJoint;
            while (jointId != B2_NULL_INDEX)
            {
                B2Joint joint = b2Array_Get(ref world.joints, jointId);
                joint.islandId = bigId;
                jointId = joint.islandNext;
            }

            // connect body lists
            B2_ASSERT(big.tailBody != B2_NULL_INDEX);
            B2Body tailBody = b2Array_Get(ref world.bodies, big.tailBody);
            B2_ASSERT(tailBody.islandNext == B2_NULL_INDEX);
            tailBody.islandNext = small.headBody;

            B2_ASSERT(small.headBody != B2_NULL_INDEX);
            B2Body headBody = b2Array_Get(ref world.bodies, small.headBody);
            B2_ASSERT(headBody.islandPrev == B2_NULL_INDEX);
            headBody.islandPrev = big.tailBody;

            big.tailBody = small.tailBody;
            big.bodyCount += small.bodyCount;

            // connect contact lists
            if (big.headContact == B2_NULL_INDEX)
            {
                // Big island has no contacts
                B2_ASSERT(big.tailContact == B2_NULL_INDEX && big.contactCount == 0);
                big.headContact = small.headContact;
                big.tailContact = small.tailContact;
                big.contactCount = small.contactCount;
            }
            else if (small.headContact != B2_NULL_INDEX)
            {
                // Both islands have contacts
                B2_ASSERT(small.tailContact != B2_NULL_INDEX && small.contactCount > 0);
                B2_ASSERT(big.tailContact != B2_NULL_INDEX && big.contactCount > 0);

                B2Contact tailContact = b2Array_Get(ref world.contacts, big.tailContact);
                B2_ASSERT(tailContact.islandNext == B2_NULL_INDEX);
                tailContact.islandNext = small.headContact;

                B2Contact headContact = b2Array_Get(ref world.contacts, small.headContact);
                B2_ASSERT(headContact.islandPrev == B2_NULL_INDEX);
                headContact.islandPrev = big.tailContact;

                big.tailContact = small.tailContact;
                big.contactCount += small.contactCount;
            }

            if (big.headJoint == B2_NULL_INDEX)
            {
                // Root island has no joints
                B2_ASSERT(big.tailJoint == B2_NULL_INDEX && big.jointCount == 0);
                big.headJoint = small.headJoint;
                big.tailJoint = small.tailJoint;
                big.jointCount = small.jointCount;
            }
            else if (small.headJoint != B2_NULL_INDEX)
            {
                // Both islands have joints
                B2_ASSERT(small.tailJoint != B2_NULL_INDEX && small.jointCount > 0);
                B2_ASSERT(big.tailJoint != B2_NULL_INDEX && big.jointCount > 0);

                B2Joint tailJoint = b2Array_Get(ref world.joints, big.tailJoint);
                B2_ASSERT(tailJoint.islandNext == B2_NULL_INDEX);
                tailJoint.islandNext = small.headJoint;

                B2Joint headJoint = b2Array_Get(ref world.joints, small.headJoint);
                B2_ASSERT(headJoint.islandPrev == B2_NULL_INDEX);
                headJoint.islandPrev = big.tailJoint;

                big.tailJoint = small.tailJoint;
                big.jointCount += small.jointCount;
            }

            // Track removed constraints
            big.constraintRemoveCount += small.constraintRemoveCount;

            small.bodyCount = 0;
            small.contactCount = 0;
            small.jointCount = 0;
            small.headBody = B2_NULL_INDEX;
            small.headContact = B2_NULL_INDEX;
            small.headJoint = B2_NULL_INDEX;
            small.tailBody = B2_NULL_INDEX;
            small.tailContact = B2_NULL_INDEX;
            small.tailJoint = B2_NULL_INDEX;
            small.constraintRemoveCount = 0;

            b2DestroyIsland(world, small.islandId);

            b2ValidateIsland(world, bigId);

            return bigId;
        }

        public static void b2AddContactToIsland(B2World world, int islandId, B2Contact contact)
        {
            B2_ASSERT(contact.islandId == B2_NULL_INDEX);
            B2_ASSERT(contact.islandPrev == B2_NULL_INDEX);
            B2_ASSERT(contact.islandNext == B2_NULL_INDEX);

            B2Island island = b2Array_Get(ref world.islands, islandId);

            if (island.headContact != B2_NULL_INDEX)
            {
                contact.islandNext = island.headContact;
                B2Contact headContact = b2Array_Get(ref world.contacts, island.headContact);
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

        // Link a contact into an island.
        public static void b2LinkContact(B2World world, B2Contact contact)
        {
            B2_ASSERT((contact.flags & (uint)B2ContactFlags.b2_contactTouchingFlag) != 0);

            int bodyIdA = contact.edges[0].bodyId;
            int bodyIdB = contact.edges[1].bodyId;

            B2Body bodyA = b2Array_Get(ref world.bodies, bodyIdA);
            B2Body bodyB = b2Array_Get(ref world.bodies, bodyIdB);

            B2_ASSERT(bodyA.setIndex != (int)B2SetType.b2_disabledSet && bodyB.setIndex != (int)B2SetType.b2_disabledSet);
            B2_ASSERT(bodyA.setIndex != (int)B2SetType.b2_staticSet || bodyB.setIndex != (int)B2SetType.b2_staticSet);

            // Wake bodyB if bodyA is awake and bodyB is sleeping
            if (bodyA.setIndex == (int)B2SetType.b2_awakeSet && bodyB.setIndex >= (int)B2SetType.b2_firstSleepingSet)
            {
                b2WakeSolverSet(world, bodyB.setIndex);
            }

            // Wake bodyA if bodyB is awake and bodyA is sleeping
            if (bodyB.setIndex == (int)B2SetType.b2_awakeSet && bodyA.setIndex >= (int)B2SetType.b2_firstSleepingSet)
            {
                b2WakeSolverSet(world, bodyA.setIndex);
            }

            int islandIdA = bodyA.islandId;
            int islandIdB = bodyB.islandId;

            // Static bodies have null island indices.
            B2_ASSERT(bodyA.setIndex != (int)B2SetType.b2_staticSet || islandIdA == B2_NULL_INDEX);
            B2_ASSERT(bodyB.setIndex != (int)B2SetType.b2_staticSet || islandIdB == B2_NULL_INDEX);
            B2_ASSERT(islandIdA != B2_NULL_INDEX || islandIdB != B2_NULL_INDEX);

            // Merge islands. This will destroy one of the islands.
            int finalIslandId = b2MergeIslands(world, islandIdA, islandIdB);

            // Add contact to the island that survived
            b2AddContactToIsland(world, finalIslandId, contact);
        }

        // Unlink contact from the island graph when it stops having contact points
        // This is called when a contact no longer has contact points or when a contact is destroyed.
        public static void b2UnlinkContact(B2World world, B2Contact contact)
        {
            B2_ASSERT(contact.islandId != B2_NULL_INDEX);

            // remove from island
            int islandId = contact.islandId;
            B2Island island = b2Array_Get(ref world.islands, islandId);

            if (contact.islandPrev != B2_NULL_INDEX)
            {
                B2Contact prevContact = b2Array_Get(ref world.contacts, contact.islandPrev);
                B2_ASSERT(prevContact.islandNext == contact.contactId);
                prevContact.islandNext = contact.islandNext;
            }

            if (contact.islandNext != B2_NULL_INDEX)
            {
                B2Contact nextContact = b2Array_Get(ref world.contacts, contact.islandNext);
                B2_ASSERT(nextContact.islandPrev == contact.contactId);
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

            B2_ASSERT(island.contactCount > 0);
            island.contactCount -= 1;
            island.constraintRemoveCount += 1;

            contact.islandId = B2_NULL_INDEX;
            contact.islandPrev = B2_NULL_INDEX;
            contact.islandNext = B2_NULL_INDEX;

            b2ValidateIsland(world, islandId);
        }

        public static void b2AddJointToIsland(B2World world, int islandId, B2Joint joint)
        {
            B2_ASSERT(joint.islandId == B2_NULL_INDEX);
            B2_ASSERT(joint.islandPrev == B2_NULL_INDEX);
            B2_ASSERT(joint.islandNext == B2_NULL_INDEX);

            B2Island island = b2Array_Get(ref world.islands, islandId);

            if (island.headJoint != B2_NULL_INDEX)
            {
                joint.islandNext = island.headJoint;
                B2Joint headJoint = b2Array_Get(ref world.joints, island.headJoint);
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
        public static void b2LinkJoint(B2World world, B2Joint joint)
        {
            B2Body bodyA = b2Array_Get(ref world.bodies, joint.edges[0].bodyId);
            B2Body bodyB = b2Array_Get(ref world.bodies, joint.edges[1].bodyId);

            B2_ASSERT(bodyA.type == B2BodyType.b2_dynamicBody || bodyB.type == B2BodyType.b2_dynamicBody);

            if (bodyA.setIndex == (int)B2SetType.b2_awakeSet && bodyB.setIndex >= (int)B2SetType.b2_firstSleepingSet)
            {
                b2WakeSolverSet(world, bodyB.setIndex);
            }
            else if (bodyB.setIndex == (int)B2SetType.b2_awakeSet && bodyA.setIndex >= (int)B2SetType.b2_firstSleepingSet)
            {
                b2WakeSolverSet(world, bodyA.setIndex);
            }

            int islandIdA = bodyA.islandId;
            int islandIdB = bodyB.islandId;

            B2_ASSERT(islandIdA != B2_NULL_INDEX || islandIdB != B2_NULL_INDEX);

            // Merge islands. This will destroy one of the islands.
            int finalIslandId = b2MergeIslands(world, islandIdA, islandIdB);

            // Add joint the island that survived
            b2AddJointToIsland(world, finalIslandId, joint);
        }

        // Unlink a joint from the island graph when it is destroyed
        public static void b2UnlinkJoint(B2World world, B2Joint joint)
        {
            if (joint.islandId == B2_NULL_INDEX)
            {
                return;
            }

            // remove from island
            int islandId = joint.islandId;
            B2Island island = b2Array_Get(ref world.islands, islandId);

            if (joint.islandPrev != B2_NULL_INDEX)
            {
                B2Joint prevJoint = b2Array_Get(ref world.joints, joint.islandPrev);
                B2_ASSERT(prevJoint.islandNext == joint.jointId);
                prevJoint.islandNext = joint.islandNext;
            }

            if (joint.islandNext != B2_NULL_INDEX)
            {
                B2Joint nextJoint = b2Array_Get(ref world.joints, joint.islandNext);
                B2_ASSERT(nextJoint.islandPrev == joint.jointId);
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

            B2_ASSERT(island.jointCount > 0);
            island.jointCount -= 1;
            island.constraintRemoveCount += 1;

            joint.islandId = B2_NULL_INDEX;
            joint.islandPrev = B2_NULL_INDEX;
            joint.islandNext = B2_NULL_INDEX;

            b2ValidateIsland(world, islandId);
        }

        // Possible optimizations:
        // 1. use the body island id as the mark
        // 2. start from the sleepy bodies and stop processing if a sleep body is connected to a non-sleepy body
        // 3. use a sleepy flag on bodies to avoid velocity access
        public static void b2SplitIsland(B2World world, int baseId)
        {
            B2Island baseIsland = b2Array_Get(ref world.islands, baseId);
            int setIndex = baseIsland.setIndex;

            if (setIndex != (int)B2SetType.b2_awakeSet)
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

            B2Body[] bodies = world.bodies.data;
            B2ArenaAllocator alloc = world.arena;

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
                B2Body body = bodies[nextBody];

                nextBody = body.islandNext;
            }

            B2_ASSERT(index == bodyCount);

            // Each island is found as a depth first search starting from a seed body
            for (int i = 0; i < bodyCount; ++i)
            {
                int seedIndex = bodyIds[i];
                B2Body seed = bodies[seedIndex];
                B2_ASSERT(seed.setIndex == setIndex);

                if (seed.islandId != baseId)
                {
                    // The body has already been visited
                    continue;
                }

                int stackCount = 0;
                stack[stackCount++] = seedIndex;

                // Create new island
                // No lock needed because only a single island can split per time step. No islands are being used during the constraint
                // solve. However, islands are touched during body finalization.
                B2Island island = b2CreateIsland(world, setIndex);

                int islandId = island.islandId;
                seed.islandId = islandId;

                // Perform a depth first search (DFS) on the constraint graph.
                while (stackCount > 0)
                {
                    // Grab the next body off the stack and add it to the island.
                    int bodyId = stack[--stackCount];
                    B2Body body = bodies[bodyId];
                    B2_ASSERT(body.setIndex == (int)B2SetType.b2_awakeSet);
                    B2_ASSERT(body.islandId == islandId);

                    // Add body to island
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

                        B2Contact contact = b2Array_Get(ref world.contacts, contactId);
                        B2_ASSERT(contact.contactId == contactId);

                        // Next key
                        contactKey = contact.edges[edgeIndex].nextKey;

                        // Has this contact already been added to this island?
                        if (contact.islandId == islandId)
                        {
                            continue;
                        }

                        // Is this contact enabled and touching?
                        if ((contact.flags & (uint)B2ContactFlags.b2_contactTouchingFlag) == 0)
                        {
                            continue;
                        }

                        int otherEdgeIndex = edgeIndex ^ 1;
                        int otherBodyId = contact.edges[otherEdgeIndex].bodyId;
                        B2Body otherBody = bodies[otherBodyId];

                        // Maybe add other body to stack
                        if (otherBody.islandId != islandId && otherBody.setIndex != (int)B2SetType.b2_staticSet)
                        {
                            B2_ASSERT(stackCount < bodyCount);
                            stack[stackCount++] = otherBodyId;

                            // Need to update the body's island id immediately so it is not traversed again
                            otherBody.islandId = islandId;
                        }

                        // Add contact to island
                        contact.islandId = islandId;
                        if (island.tailContact != B2_NULL_INDEX)
                        {
                            B2Contact tailContact = b2Array_Get(ref world.contacts, island.tailContact);
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

                        B2Joint joint = b2Array_Get(ref world.joints, jointId);
                        B2_ASSERT(joint.jointId == jointId);

                        // Next key
                        jointKey = joint.edges[edgeIndex].nextKey;

                        // Has this joint already been added to this island?
                        if (joint.islandId == islandId)
                        {
                            continue;
                        }

                        if (joint.setIndex == (int)B2SetType.b2_disabledSet)
                        {
                            continue;
                        }

                        int otherEdgeIndex = edgeIndex ^ 1;
                        int otherBodyId = joint.edges[otherEdgeIndex].bodyId;
                        B2Body otherBody = bodies[otherBodyId];

                        // Don't simulate joints connected to disabled bodies.
                        if (otherBody.setIndex == (int)B2SetType.b2_disabledSet)
                        {
                            continue;
                        }

                        // At least one body must be dynamic
                        if (body.type != B2BodyType.b2_dynamicBody && otherBody.type != B2BodyType.b2_dynamicBody)
                        {
                            continue;
                        }

                        // Maybe add other body to stack
                        if (otherBody.islandId != islandId && otherBody.setIndex == (int)B2SetType.b2_awakeSet)
                        {
                            B2_ASSERT(stackCount < bodyCount);
                            stack[stackCount++] = otherBodyId;

                            // Need to update the body's island id immediately so it is not traversed again
                            otherBody.islandId = islandId;
                        }

                        // Add joint to island
                        joint.islandId = islandId;
                        if (island.tailJoint != B2_NULL_INDEX)
                        {
                            B2Joint tailJoint = b2Array_Get(ref world.joints, island.tailJoint);
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

            // Done with the base split island. This is delayed because the baseId is used as a marker and it
            // should not be recycled in while splitting.
            b2DestroyIsland(world, baseId);

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
            b2TracyCZoneNC(B2TracyCZone.split, "Split Island", B2HexColor.b2_colorOlive, true);

            B2_UNUSED(startIndex, endIndex, threadIndex);

            ulong ticks = b2GetTicks();
            B2World world = context as B2World;

            B2_ASSERT(world.splitIslandId != B2_NULL_INDEX);

            b2SplitIsland(world, world.splitIslandId);

            world.profile.splitIslands += b2GetMilliseconds(ticks);
            b2TracyCZoneEnd(B2TracyCZone.split);
        }

#if DEBUG
        public static void b2ValidateIsland(B2World world, int islandId)
        {
            if (islandId == B2_NULL_INDEX)
            {
                return;
            }

            B2Island island = b2Array_Get(ref world.islands, islandId);
            B2_ASSERT(island.islandId == islandId);
            B2_ASSERT(island.setIndex != B2_NULL_INDEX);
            B2_ASSERT(island.headBody != B2_NULL_INDEX);

            {
                B2_ASSERT(island.tailBody != B2_NULL_INDEX);
                B2_ASSERT(island.bodyCount > 0);
                if (island.bodyCount > 1)
                {
                    B2_ASSERT(island.tailBody != island.headBody);
                }

                B2_ASSERT(island.bodyCount <= b2GetIdCount(world.bodyIdPool));

                int count = 0;
                int bodyId = island.headBody;
                while (bodyId != B2_NULL_INDEX)
                {
                    B2Body body = b2Array_Get(ref world.bodies, bodyId);
                    B2_ASSERT(body.islandId == islandId);
                    B2_ASSERT(body.setIndex == island.setIndex);
                    count += 1;

                    if (count == island.bodyCount)
                    {
                        B2_ASSERT(bodyId == island.tailBody);
                    }

                    bodyId = body.islandNext;
                }

                B2_ASSERT(count == island.bodyCount);
            }

            if (island.headContact != B2_NULL_INDEX)
            {
                B2_ASSERT(island.tailContact != B2_NULL_INDEX);
                B2_ASSERT(island.contactCount > 0);
                if (island.contactCount > 1)
                {
                    B2_ASSERT(island.tailContact != island.headContact);
                }

                B2_ASSERT(island.contactCount <= b2GetIdCount(world.contactIdPool));

                int count = 0;
                int contactId = island.headContact;
                while (contactId != B2_NULL_INDEX)
                {
                    B2Contact contact = b2Array_Get(ref world.contacts, contactId);
                    B2_ASSERT(contact.setIndex == island.setIndex);
                    B2_ASSERT(contact.islandId == islandId);
                    count += 1;

                    if (count == island.contactCount)
                    {
                        B2_ASSERT(contactId == island.tailContact);
                    }

                    contactId = contact.islandNext;
                }

                B2_ASSERT(count == island.contactCount);
            }
            else
            {
                B2_ASSERT(island.tailContact == B2_NULL_INDEX);
                B2_ASSERT(island.contactCount == 0);
            }

            if (island.headJoint != B2_NULL_INDEX)
            {
                B2_ASSERT(island.tailJoint != B2_NULL_INDEX);
                B2_ASSERT(island.jointCount > 0);
                if (island.jointCount > 1)
                {
                    B2_ASSERT(island.tailJoint != island.headJoint);
                }

                B2_ASSERT(island.jointCount <= b2GetIdCount(world.jointIdPool));

                int count = 0;
                int jointId = island.headJoint;
                while (jointId != B2_NULL_INDEX)
                {
                    B2Joint joint = b2Array_Get(ref world.joints, jointId);
                    B2_ASSERT(joint.setIndex == island.setIndex);
                    count += 1;

                    if (count == island.jointCount)
                    {
                        B2_ASSERT(jointId == island.tailJoint);
                    }

                    jointId = joint.islandNext;
                }

                B2_ASSERT(count == island.jointCount);
            }
            else
            {
                B2_ASSERT(island.tailJoint == B2_NULL_INDEX);
                B2_ASSERT(island.jointCount == 0);
            }
        }

#else
        public static void b2ValidateIsland(B2World world, int islandId)
        {
            B2_UNUSED(world);
            B2_UNUSED(islandId);
        }
#endif
    }
}