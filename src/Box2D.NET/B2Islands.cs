// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Buffers;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2IdPools;
using static Box2D.NET.B2Profiling;
using static Box2D.NET.B2SolverSets;
using static Box2D.NET.B2Timers;
using static Box2D.NET.B2ArenaAllocators;

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
            B2_ASSERT(setIndex == (int)B2SolverSetType.b2_awakeSet || setIndex >= (int)B2SolverSetType.b2_firstSleepingSet);

            int islandId = b2AllocId(world.islandIdPool);

            if (islandId == world.islands.count)
            {
                b2Array_Push(ref world.islands, new B2Island());
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
            island.bodies = b2Array_Create<int>();
            island.contacts = b2Array_Create<B2ContactLink>();
            island.joints = b2Array_Create<B2JointLink>();
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
            {
                int localIndex = island.localIndex;
                int lastIndex = set.islandSims.count - 1;
                B2_ASSERT(0 <= localIndex && localIndex <= lastIndex);
                int moveIslandId = set.islandSims.data[lastIndex].islandId;
                set.islandSims.data[localIndex].CopyFrom(set.islandSims.data[lastIndex]);
                world.islands.data[moveIslandId].localIndex = localIndex;
                if (localIndex != lastIndex)
                {
                    set.islandSims.data[lastIndex] = new B2IslandSim();
                }

                set.islandSims.count -= 1;
            }

            // Free island and id (preserve island revision)
            b2Array_Destroy(ref island.bodies);
            b2Array_Destroy(ref island.contacts);
            b2Array_Destroy(ref island.joints);
            island.constraintRemoveCount = 0;
            island.localIndex = B2_NULL_INDEX;
            island.islandId = B2_NULL_INDEX;
            island.setIndex = B2_NULL_INDEX;

            B2_VALIDATE(island.localIndex == B2_NULL_INDEX);

            b2FreeId(world.islandIdPool, islandId);
        }

        private static int b2MergeIslands(B2World world, int islandIdA, int islandIdB)
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

            B2Island smallIsland;
            B2Island bigIsland;
            {
                B2Island islandA = b2Array_Get(ref world.islands, islandIdA);
                B2Island islandB = b2Array_Get(ref world.islands, islandIdB);

                // Keep the biggest island to reduce cache misses
                if (islandA.bodies.count >= islandB.bodies.count)
                {
                    bigIsland = islandA;
                    smallIsland = islandB;
                }
                else
                {
                    bigIsland = islandB;
                    smallIsland = islandA;
                }
            }

            int bigIslandId = bigIsland.islandId;
            b2Array_Reserve(ref bigIsland.bodies, bigIsland.bodies.count + smallIsland.bodies.count);

            // Move bodies from smaller island to larger island
            for (int i = 0; i < smallIsland.bodies.count; ++i)
            {
                int bodyId = smallIsland.bodies.data[i];
                B2Body body = b2Array_Get(ref world.bodies, bodyId);
                B2_VALIDATE(body.islandId == smallIsland.islandId);
                body.islandId = bigIslandId;
                body.islandIndex = bigIsland.bodies.count;
                b2Array_Push(ref bigIsland.bodies, bodyId);
            }

            // Migrate contacts from smaller island to larger island
            if (smallIsland.contacts.count > 0)
            {
                b2Array_Reserve(ref bigIsland.contacts, bigIsland.contacts.count + smallIsland.contacts.count);

                for (int i = 0; i < smallIsland.contacts.count; ++i)
                {
                    B2ContactLink link = smallIsland.contacts.data[i];
                    B2Contact contact = b2Array_Get(ref world.contacts, link.contactId);
                    contact.islandId = bigIslandId;
                    contact.islandIndex = bigIsland.contacts.count;
                    b2Array_Push(ref bigIsland.contacts, link.ToCopy());
                }
            }

            // Migrate joints from smaller island to larger island
            if (smallIsland.joints.count > 0)
            {
                b2Array_Reserve(ref bigIsland.joints, bigIsland.joints.count + smallIsland.joints.count);

                for (int i = 0; i < smallIsland.joints.count; ++i)
                {
                    B2JointLink link = smallIsland.joints.data[i];
                    B2Joint joint = b2Array_Get(ref world.joints, link.jointId);
                    joint.islandId = bigIslandId;
                    joint.islandIndex = bigIsland.joints.count;
                    b2Array_Push(ref bigIsland.joints, link.ToCopy());
                }
            }

            // Track removed constraints
            bigIsland.constraintRemoveCount += smallIsland.constraintRemoveCount;

            b2DestroyIsland(world, smallIsland.islandId);

            b2ValidateIsland(world, bigIslandId);

            return bigIslandId;
        }

        private static void b2AddContactToIsland(B2World world, int islandId, B2Contact contact)
        {
            B2_ASSERT(contact.islandId == B2_NULL_INDEX);
            B2_ASSERT(contact.islandIndex == B2_NULL_INDEX);

            B2Island island = b2Array_Get(ref world.islands, islandId);

            contact.islandId = islandId;
            contact.islandIndex = island.contacts.count;

            B2ContactLink link = new B2ContactLink
            {
                contactId = contact.contactId,
                bodyIdA = contact.edges[0].bodyId,
                bodyIdB = contact.edges[1].bodyId,
            };
            b2Array_Push(ref island.contacts, link);

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

            B2_ASSERT(bodyA.setIndex != (int)B2SolverSetType.b2_disabledSet && bodyB.setIndex != (int)B2SolverSetType.b2_disabledSet);
            B2_ASSERT(bodyA.setIndex != (int)B2SolverSetType.b2_staticSet || bodyB.setIndex != (int)B2SolverSetType.b2_staticSet);

            // Wake bodyB if bodyA is awake and bodyB is sleeping
            if (bodyA.setIndex == (int)B2SolverSetType.b2_awakeSet && bodyB.setIndex >= (int)B2SolverSetType.b2_firstSleepingSet)
            {
                b2WakeSolverSet(world, bodyB.setIndex);
            }

            // Wake bodyA if bodyB is awake and bodyA is sleeping
            if (bodyB.setIndex == (int)B2SolverSetType.b2_awakeSet && bodyA.setIndex >= (int)B2SolverSetType.b2_firstSleepingSet)
            {
                b2WakeSolverSet(world, bodyA.setIndex);
            }

            int islandIdA = bodyA.islandId;
            int islandIdB = bodyB.islandId;

            // Static bodies have null island indices.
            B2_ASSERT(bodyA.setIndex != (int)B2SolverSetType.b2_staticSet || islandIdA == B2_NULL_INDEX);
            B2_ASSERT(bodyB.setIndex != (int)B2SolverSetType.b2_staticSet || islandIdB == B2_NULL_INDEX);
            B2_ASSERT(islandIdA != B2_NULL_INDEX || islandIdB != B2_NULL_INDEX);

            // Merge islands. This will destroy one of the islands.
            int finalIslandId = b2MergeIslands(world, islandIdA, islandIdB);

            // Add contact to the island that survived
            b2AddContactToIsland(world, finalIslandId, contact);
        }

        // This is called when a contact no longer has contact points or when a contact is destroyed.
        public static void b2UnlinkContact(B2World world, B2Contact contact)
        {
            B2_ASSERT(contact.islandId != B2_NULL_INDEX);

            // remove from island
            int islandId = contact.islandId;
            B2Island island = b2Array_Get(ref world.islands, islandId);

            int removeIndex = contact.islandIndex;
            B2_ASSERT(0 <= removeIndex && removeIndex < island.contacts.count);
            B2_ASSERT(island.contacts.data[removeIndex].contactId == contact.contactId);

            int movedIndex = b2Array_RemoveSwap(ref island.contacts, removeIndex);
            if (movedIndex != B2_NULL_INDEX)
            {
                // Fix islandIndex on the contact that was swapped into removeIndex
                B2ContactLink movedLink = island.contacts.data[removeIndex];
                B2Contact movedContact = b2Array_Get(ref world.contacts, movedLink.contactId);
                B2_ASSERT(movedContact.islandIndex == movedIndex);
                movedContact.islandIndex = removeIndex;
            }

            contact.islandId = B2_NULL_INDEX;
            contact.islandIndex = B2_NULL_INDEX;
            island.constraintRemoveCount += 1;

            b2ValidateIsland(world, islandId);
        }

        private static void b2AddJointToIsland(B2World world, int islandId, B2Joint joint)
        {
            B2_ASSERT(joint.islandId == B2_NULL_INDEX);
            B2_ASSERT(joint.islandIndex == B2_NULL_INDEX);

            B2Island island = b2Array_Get(ref world.islands, islandId);

            joint.islandId = islandId;
            joint.islandIndex = island.joints.count;

            B2JointLink link = new B2JointLink
            {
                jointId = joint.jointId,
                bodyIdA = joint.edges[0].bodyId,
                bodyIdB = joint.edges[1].bodyId,
            };
            b2Array_Push(ref island.joints, link);

            b2ValidateIsland(world, islandId);
        }

        public static void b2LinkJoint(B2World world, B2Joint joint)
        {
            B2Body bodyA = b2Array_Get(ref world.bodies, joint.edges[0].bodyId);
            B2Body bodyB = b2Array_Get(ref world.bodies, joint.edges[1].bodyId);

            B2_ASSERT(bodyA.type == B2BodyType.b2_dynamicBody || bodyB.type == B2BodyType.b2_dynamicBody);

            if (bodyA.setIndex == (int)B2SolverSetType.b2_awakeSet && bodyB.setIndex >= (int)B2SolverSetType.b2_firstSleepingSet)
            {
                b2WakeSolverSet(world, bodyB.setIndex);
            }
            else if (bodyB.setIndex == (int)B2SolverSetType.b2_awakeSet && bodyA.setIndex >= (int)B2SolverSetType.b2_firstSleepingSet)
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

        public static void b2UnlinkJoint(B2World world, B2Joint joint)
        {
            if (joint.islandId == B2_NULL_INDEX)
            {
                return;
            }

            // remove from island
            int islandId = joint.islandId;
            B2Island island = b2Array_Get(ref world.islands, islandId);

            int removeIndex = joint.islandIndex;
            B2_ASSERT(0 <= removeIndex && removeIndex < island.joints.count);
            B2_ASSERT(island.joints.data[removeIndex].jointId == joint.jointId);

            int movedIndex = b2Array_RemoveSwap(ref island.joints, removeIndex);
            if (movedIndex != B2_NULL_INDEX)
            {
                // Fix islandIndex on the joint that was swapped into removeIndex
                B2JointLink movedLink = island.joints.data[removeIndex];
                B2Joint movedJoint = b2Array_Get(ref world.joints, movedLink.jointId);
                B2_ASSERT(movedJoint.islandIndex == movedIndex);
                movedJoint.islandIndex = removeIndex;
            }

            joint.islandId = B2_NULL_INDEX;
            joint.islandIndex = B2_NULL_INDEX;
            island.constraintRemoveCount += 1;

            b2ValidateIsland(world, islandId);
        }

        // Find parent of a node. Use path halving to speed up further queries.
        private static int b2IslandFindParent(Span<int> parents, int node)
        {
            // Walk the chain of parents to find the node that is its own parent (the root)
            while (parents[node] != node)
            {
                int grandParent = parents[parents[node]];
                parents[node] = grandParent;
                node = grandParent;
            }

            return node;
        }

        // Connect the components containing node1 and node2.
        // Uses rank to keep tree balanced. Tracks per-component contact and joint counts.
        private static void b2IslandUnion(Span<int> parents, Span<int> ranks, int node1, int node2, Span<int> contactCounts, Span<int> jointCounts)
        {
            int root1 = b2IslandFindParent(parents, node1);
            int root2 = b2IslandFindParent(parents, node2);
            if (root1 != root2)
            {
                if (ranks[root1] < ranks[root2])
                {
                    parents[root1] = root2;
                    contactCounts[root2] += contactCounts[root1];
                    jointCounts[root2] += jointCounts[root1];
                }
                else if (ranks[root1] > ranks[root2])
                {
                    parents[root2] = root1;
                    contactCounts[root1] += contactCounts[root2];
                    jointCounts[root1] += jointCounts[root2];
                }
                else
                {
                    parents[root2] = root1;
                    ranks[root1] += 1;
                    contactCounts[root1] += contactCounts[root2];
                    jointCounts[root1] += jointCounts[root2];
                }
            }
        }

        // This uses union-find.
        // https://en.wikipedia.org/wiki/Disjoint-set_data_structure
        public static void b2SplitIsland(B2World world, int baseId)
        {
            B2Island baseIsland = b2Array_Get(ref world.islands, baseId);
            B2_ASSERT(baseIsland.constraintRemoveCount > 0);
            B2_ASSERT(baseIsland.setIndex == (int)B2SolverSetType.b2_awakeSet);

            b2ValidateIsland(world, baseId);

            // Cache base island fields before b2CreateIsland, which may reallocate
            // world.islands and invalidate the baseIsland pointer.
            int baseBodyCount = baseIsland.bodies.count;
            int[] baseBodyIds = baseIsland.bodies.data;
            int baseBodyCapacity = baseIsland.bodies.capacity;

            int baseContactCount = baseIsland.contacts.count;
            B2ContactLink[] baseContacts = baseIsland.contacts.data;
            int baseContactCapacity = baseIsland.contacts.capacity;

            int baseJointCount = baseIsland.joints.count;
            B2JointLink[] baseJoints = baseIsland.joints.data;
            int baseJointCapacity = baseIsland.joints.capacity;

            B2ArenaAllocator alloc = world.arena;

            // No lock is needed because I ensure the allocator is not used while this task is active.
            // Allocate contactCounts and jointCounts before ranks so ranks can be freed first (LIFO arena).
            ArraySegment<int> parents = b2AllocateArenaItem<int>(alloc, baseBodyCount, "parents");
            ArraySegment<int> contactCounts = b2AllocateArenaItem<int>(alloc, baseBodyCount, "contact counts");
            ArraySegment<int> jointCounts = b2AllocateArenaItem<int>(alloc, baseBodyCount, "joint counts");
            ArraySegment<int> ranks = b2AllocateArenaItem<int>(alloc, baseBodyCount, "ranks");
            for (int i = 0; i < baseBodyCount; ++i)
            {
                parents[i] = i;
                ranks[i] = 0;
                contactCounts[i] = 0;
                jointCounts[i] = 0;
            }

            Span<B2Body> bodies = world.bodies.data;

            // Union over contacts, tracking per-component contact counts
            for (int i = 0; i < baseContactCount; ++i)
            {
                int bodyIdA = baseContacts[i].bodyIdA;
                int bodyIdB = baseContacts[i].bodyIdB;
                B2_VALIDATE(0 <= bodyIdA && bodyIdA < world.bodies.count);
                B2_VALIDATE(0 <= bodyIdB && bodyIdB < world.bodies.count);
                B2Body bodyA = bodies[bodyIdA];
                B2Body bodyB = bodies[bodyIdB];
                int islandIndexA = bodyA.islandIndex;
                int islandIndexB = bodyB.islandIndex;

                // Only connect non-static bodies
                if (islandIndexA != B2_NULL_INDEX && islandIndexB != B2_NULL_INDEX)
                {
                    B2_VALIDATE(0 <= islandIndexA && islandIndexA < baseBodyCount);
                    B2_VALIDATE(0 <= islandIndexB && islandIndexB < baseBodyCount);
                    b2IslandUnion(parents, ranks, islandIndexA, islandIndexB, contactCounts, jointCounts);
                    int root = b2IslandFindParent(parents, islandIndexA);
                    contactCounts[root] += 1;
                }
                else
                {
                    int islandIndex = islandIndexA != B2_NULL_INDEX ? islandIndexA : islandIndexB;
                    int root = b2IslandFindParent(parents, islandIndex);
                    contactCounts[root] += 1;
                }
            }

            // Union over joints, tracking per-component joint counts
            for (int i = 0; i < baseJointCount; ++i)
            {
                int bodyIdA = baseJoints[i].bodyIdA;
                int bodyIdB = baseJoints[i].bodyIdB;
                B2_VALIDATE(0 <= bodyIdA && bodyIdA < world.bodies.count);
                B2_VALIDATE(0 <= bodyIdB && bodyIdB < world.bodies.count);
                B2Body bodyA = bodies[bodyIdA];
                B2Body bodyB = bodies[bodyIdB];
                int islandIndexA = bodyA.islandIndex;
                int islandIndexB = bodyB.islandIndex;

                // Only connect non-static bodies
                if (islandIndexA != B2_NULL_INDEX && islandIndexB != B2_NULL_INDEX)
                {
                    B2_VALIDATE(0 <= islandIndexA && islandIndexA < baseBodyCount);
                    B2_VALIDATE(0 <= islandIndexB && islandIndexB < baseBodyCount);
                    b2IslandUnion(parents, ranks, islandIndexA, islandIndexB, contactCounts, jointCounts);
                    int root = b2IslandFindParent(parents, islandIndexA);
                    jointCounts[root] += 1;
                }
                else
                {
                    int islandIndex = islandIndexA != B2_NULL_INDEX ? islandIndexA : islandIndexB;
                    int root = b2IslandFindParent(parents, islandIndex);
                    jointCounts[root] += 1;
                }
            }

            // Done with ranks
            b2FreeArenaItem(alloc, ranks);
            ranks = null;

            // Flatten all parent indices and count connected components.
            int componentCount = 0;
            for (int i = 0; i < baseBodyCount; ++i)
            {
                parents[i] = b2IslandFindParent(parents, i);
                if (parents[i] == i)
                {
                    componentCount += 1;
                }
            }

            // Early return — island is still fully connected, no split needed.
            if (componentCount == 1)
            {
                baseIsland.constraintRemoveCount = 0;
                b2FreeArenaItem(alloc, jointCounts);
                b2FreeArenaItem(alloc, contactCounts);
                b2FreeArenaItem(alloc, parents);
                return;
            }

            // Detach body/contact/joint arrays from base island so b2DestroyIsland won't free them
            baseIsland.bodies.data = null;
            baseIsland.bodies.count = 0;
            baseIsland.bodies.capacity = 0;

            baseIsland.contacts.data = null;
            baseIsland.contacts.count = 0;
            baseIsland.contacts.capacity = 0;

            baseIsland.joints.data = null;
            baseIsland.joints.count = 0;
            baseIsland.joints.capacity = 0;

            // Null so code below doesn't accidentally use this.
            baseIsland = null;

            // Map from body index to new island index. Only set for root bodies.
            ArraySegment<int> rootMap = b2AllocateArenaItem<int>(alloc, baseBodyCount, "root map");
            for (int i = 0; i < baseBodyCount; ++i)
            {
                rootMap[i] = B2_NULL_INDEX;
            }

            ArraySegment<int> componentBodyCounts = b2AllocateArenaItem<int>(alloc, componentCount, "component body counts");
            ArraySegment<int> componentContactCounts = b2AllocateArenaItem<int>(alloc, componentCount, "component contact counts");
            ArraySegment<int> componentJointCounts = b2AllocateArenaItem<int>(alloc, componentCount, "component joint counts");
            int islandCount = 0;

            // Find the root body for each body and create islands as needed.
            // Extract per-component counts from the root nodes' accumulated counts.
            for (int i = 0; i < baseBodyCount; ++i)
            {
                int rootIndex = parents[i];
                if (rootMap[rootIndex] == B2_NULL_INDEX)
                {
                    rootMap[rootIndex] = islandCount;
                    componentBodyCounts[islandCount] = 0;
                    componentContactCounts[islandCount] = contactCounts[rootIndex];
                    componentJointCounts[islandCount] = jointCounts[rootIndex];
                    islandCount += 1;
                }

                componentBodyCounts[rootMap[rootIndex]] += 1;
            }

            B2_ASSERT(islandCount == componentCount);

            // Map from new island index to island id
            ArraySegment<int> islandIds = b2AllocateArenaItem<int>(alloc, islandCount, "island ids");

            // Create new islands and reserve body/contact/joint arrays
            for (int i = 0; i < islandCount; ++i)
            {
                // WARNING: this invalidates baseIsland pointer
                B2Island newIsland = b2CreateIsland(world, (int)B2SolverSetType.b2_awakeSet);
                islandIds[i] = newIsland.islandId;

                // Reserve arrays to avoid wasteful growth and memcpy.
                b2Array_Reserve(ref newIsland.bodies, componentBodyCounts[i]);
                b2Array_Reserve(ref newIsland.contacts, componentContactCounts[i]);
                b2Array_Reserve(ref newIsland.joints, componentJointCounts[i]);
            }

            // Assign bodies to new islands
            for (int i = 0; i < baseBodyCount; ++i)
            {
                int bodyId = baseBodyIds[i];
                int root = b2IslandFindParent(parents, i);
                int newIslandId = islandIds[rootMap[root]];

                B2Body body = b2Array_Get(ref world.bodies, bodyId);
                B2Island newIsland = b2Array_Get(ref world.islands, newIslandId);

                body.islandId = newIslandId;
                body.islandIndex = newIsland.bodies.count;

                // Ensure the array has the correct capacity
                B2_VALIDATE(newIsland.bodies.count < newIsland.bodies.capacity);
                b2Array_Push(ref newIsland.bodies, bodyId);
            }

            // Assign contacts to the island of their bodies
            for (int i = 0; i < baseContactCount; ++i)
            {
                B2ContactLink link = baseContacts[i];
                B2Contact contact = b2Array_Get(ref world.contacts, link.contactId);

                // Static bodies don't have an island id.
                B2Body bodyA = b2Array_Get(ref world.bodies, link.bodyIdA);
                B2Body bodyB = b2Array_Get(ref world.bodies, link.bodyIdB);
                int targetIslandId = bodyA.islandId != B2_NULL_INDEX ? bodyA.islandId : bodyB.islandId;

                B2Island targetIsland = b2Array_Get(ref world.islands, targetIslandId);
                contact.islandId = targetIslandId;
                contact.islandIndex = targetIsland.contacts.count;

                // Ensure the array has the correct capacity
                B2_VALIDATE(targetIsland.contacts.count < targetIsland.contacts.capacity);
                b2Array_Push(ref targetIsland.contacts, link.ToCopy());
            }

            // Assign joints to the island of their bodies
            for (int i = 0; i < baseJointCount; ++i)
            {
                B2JointLink link = baseJoints[i];
                B2Joint joint = b2Array_Get(ref world.joints, link.jointId);

                // Static bodies don't have an island id.
                B2Body bodyA = b2Array_Get(ref world.bodies, link.bodyIdA);
                B2Body bodyB = b2Array_Get(ref world.bodies, link.bodyIdB);
                int targetIslandId = bodyA.islandId != B2_NULL_INDEX ? bodyA.islandId : bodyB.islandId;

                B2Island targetIsland = b2Array_Get(ref world.islands, targetIslandId);
                joint.islandId = targetIslandId;
                joint.islandIndex = targetIsland.joints.count;

                // Ensure the array has the correct capacity
                B2_VALIDATE(targetIsland.joints.count < targetIsland.joints.capacity);
                b2Array_Push(ref targetIsland.joints, link.ToCopy());
            }

            // Destroy the base island
            b2DestroyIsland(world, baseId);

            // Free the detached arrays manually
            b2Free(baseBodyIds, baseBodyCapacity);
            b2Free(baseContacts, baseContactCapacity);
            b2Free(baseJoints, baseJointCapacity);

            // Free arena items in LIFO order
            b2FreeArenaItem(alloc, islandIds);
            b2FreeArenaItem(alloc, componentJointCounts);
            b2FreeArenaItem(alloc, componentContactCounts);
            b2FreeArenaItem(alloc, componentBodyCounts);
            b2FreeArenaItem(alloc, rootMap);
            b2FreeArenaItem(alloc, jointCounts);
            b2FreeArenaItem(alloc, contactCounts);
            b2FreeArenaItem(alloc, parents);
        }

        // Split an island because some contacts and/or joints have been removed.
        // This is called during the constraint solve while islands are not being touched. This uses union find and
        // touches a lot of memory, so it can be slow.
        // Note: contacts/joints connected to static bodies must belong to an island but don't affect island connectivity
        // Note: static bodies are never in an island
        // Note: this task interacts with some allocators without locks under the assumption that no other tasks
        // are interacting with these data structures.
        public static void b2SplitIslandTask(int startIndex, int endIndex, uint threadIndex, object context)
        {
            b2TracyCZoneNC(B2TracyCZone.split, "Split Island", B2HexColor.b2_colorOlive, true);

            B2_UNUSED(startIndex, endIndex, threadIndex);

            ulong ticks = b2GetTicks();
            B2World world = (B2World)context;

            B2_ASSERT(world.splitIslandId != B2_NULL_INDEX);

            b2SplitIsland(world, world.splitIslandId);

            world.splitIslandId = B2_NULL_INDEX;
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

            {
                B2_ASSERT(island.bodies.count > 0);
                B2_ASSERT(island.bodies.count <= b2GetIdCount(world.bodyIdPool));

                for (int i = 0; i < island.bodies.count; ++i)
                {
                    B2Body body = b2Array_Get(ref world.bodies, island.bodies.data[i]);
                    B2_ASSERT(body.islandId == islandId);
                    B2_ASSERT(body.islandIndex == i);
                    B2_ASSERT(body.setIndex == island.setIndex);
                }
            }

            if (island.contacts.count > 0)
            {
                B2_ASSERT(island.contacts.count <= b2GetIdCount(world.contactIdPool));

                for (int i = 0; i < island.contacts.count; ++i)
                {
                    B2ContactLink link = island.contacts.data[i];
                    B2Contact contact = b2Array_Get(ref world.contacts, link.contactId);
                    B2_ASSERT(contact.setIndex == island.setIndex);
                    B2_ASSERT(contact.islandId == islandId);
                    B2_ASSERT(contact.islandIndex == i);
                }
            }

            if (island.joints.count > 0)
            {
                B2_ASSERT(island.joints.count <= b2GetIdCount(world.jointIdPool));

                for (int i = 0; i < island.joints.count; ++i)
                {
                    B2JointLink link = island.joints.data[i];
                    B2Joint joint = b2Array_Get(ref world.joints, link.jointId);
                    B2_ASSERT(joint.setIndex == island.setIndex);
                    B2_ASSERT(joint.islandId == islandId);
                    B2_ASSERT(joint.islandIndex == i);
                }
            }
        }
#else
        public static void b2ValidateIsland(B2World world, int islandId)
        {
            B2_UNUSED(world, islandId);
        }
#endif
    }
}
