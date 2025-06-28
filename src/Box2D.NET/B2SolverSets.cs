// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2IdPools;
using static Box2D.NET.B2ConstraintGraphs;
using static Box2D.NET.B2BitSets;
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET
{
    public static class B2SolverSets
    {
        public static B2SolverSet b2CreateSolverSet(B2World world)
        {
            var set = new B2SolverSet();
            set.bodySims = b2Array_Create<B2BodySim>();
            set.bodyStates = b2Array_Create<B2BodyState>();
            set.jointSims = b2Array_Create<B2JointSim>();
            set.contactSims = b2Array_Create<B2ContactSim>();
            set.islandSims = b2Array_Create<B2IslandSim>();

            return set;
        }

        public static void b2DestroySolverSet(B2World world, int setIndex)
        {
            B2SolverSet set = b2Array_Get(ref world.solverSets, setIndex);
            b2Array_Destroy(ref set.bodySims);
            b2Array_Destroy(ref set.bodyStates);
            b2Array_Destroy(ref set.contactSims);
            b2Array_Destroy(ref set.jointSims);
            b2Array_Destroy(ref set.islandSims);
            b2FreeId(world.solverSetIdPool, setIndex);
            //*set = ( b2SolverSet ){ 0 };
            set.Clear();
            set.setIndex = B2_NULL_INDEX;
        }

        // Wake a solver set. Does not merge islands.
        // Contacts can be in several places:
        // 1. non-touching contacts in the disabled set
        // 2. non-touching contacts already in the awake set
        // 3. touching contacts in the sleeping set
        // This handles contact types 1 and 3. Type 2 doesn't need any action.
        public static void b2WakeSolverSet(B2World world, int setIndex)
        {
            B2_ASSERT(setIndex >= (int)B2SetType.b2_firstSleepingSet);
            B2SolverSet set = b2Array_Get(ref world.solverSets, setIndex);
            B2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_awakeSet);
            B2SolverSet disabledSet = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_disabledSet);

            B2Body[] bodies = world.bodies.data;

            int bodyCount = set.bodySims.count;
            for (int i = 0; i < bodyCount; ++i)
            {
                B2BodySim simSrc = set.bodySims.data[i];

                B2Body body = bodies[simSrc.bodyId];
                B2_ASSERT(body.setIndex == setIndex);
                body.setIndex = (int)B2SetType.b2_awakeSet;
                body.localIndex = awakeSet.bodySims.count;

                // Reset sleep timer
                body.sleepTime = 0.0f;

                ref B2BodySim simDst = ref b2Array_Add(ref awakeSet.bodySims);
                //memcpy( simDst, simSrc, sizeof( b2BodySim ) );
                simDst.CopyFrom(simSrc);

                ref B2BodyState state = ref b2Array_Add(ref awakeSet.bodyStates);
                //*state = b2_identityBodyState;
                state.CopyFrom(b2_identityBodyState);
                state.flags = body.flags;

                // move non-touching contacts from disabled set to awake set
                int contactKey = body.headContactKey;
                while (contactKey != B2_NULL_INDEX)
                {
                    int edgeIndex = contactKey & 1;
                    int contactId = contactKey >> 1;

                    B2Contact contact = b2Array_Get(ref world.contacts, contactId);

                    contactKey = contact.edges[edgeIndex].nextKey;

                    if (contact.setIndex != (int)B2SetType.b2_disabledSet)
                    {
                        B2_ASSERT(contact.setIndex == (int)B2SetType.b2_awakeSet || contact.setIndex == setIndex);
                        continue;
                    }

                    int localIndex = contact.localIndex;
                    B2ContactSim contactSim = b2Array_Get(ref disabledSet.contactSims, localIndex);

                    B2_ASSERT((contact.flags & (int)B2ContactFlags.b2_contactTouchingFlag) == 0 && contactSim.manifold.pointCount == 0);

                    contact.setIndex = (int)B2SetType.b2_awakeSet;
                    contact.localIndex = awakeSet.contactSims.count;
                    ref B2ContactSim awakeContactSim = ref b2Array_Add(ref awakeSet.contactSims);
                    //memcpy( awakeContactSim, contactSim, sizeof( b2ContactSim ) );
                    awakeContactSim.CopyFrom(contactSim);

                    int movedLocalIndex = b2Array_RemoveSwap(ref disabledSet.contactSims, localIndex);
                    if (movedLocalIndex != B2_NULL_INDEX)
                    {
                        // fix moved element
                        B2ContactSim movedContactSim = disabledSet.contactSims.data[localIndex];
                        B2Contact movedContact = b2Array_Get(ref world.contacts, movedContactSim.contactId);
                        B2_ASSERT(movedContact.localIndex == movedLocalIndex);
                        movedContact.localIndex = localIndex;
                    }
                }
            }

            // transfer touching contacts from sleeping set to contact graph
            {
                int contactCount = set.contactSims.count;
                for (int i = 0; i < contactCount; ++i)
                {
                    B2ContactSim contactSim = set.contactSims.data[i];
                    B2Contact contact = b2Array_Get(ref world.contacts, contactSim.contactId);
                    B2_ASSERT(0 != (contact.flags & (int)B2ContactFlags.b2_contactTouchingFlag));
                    B2_ASSERT(0 != (contactSim.simFlags & (int)B2ContactSimFlags.b2_simTouchingFlag));
                    B2_ASSERT(contactSim.manifold.pointCount > 0);
                    B2_ASSERT(contact.setIndex == setIndex);
                    b2AddContactToGraph(world, contactSim, contact);
                    contact.setIndex = (int)B2SetType.b2_awakeSet;
                }
            }

            // transfer joints from sleeping set to awake set
            {
                int jointCount = set.jointSims.count;
                for (int i = 0; i < jointCount; ++i)
                {
                    B2JointSim jointSim = set.jointSims.data[i];
                    B2Joint joint = b2Array_Get(ref world.joints, jointSim.jointId);
                    B2_ASSERT(joint.setIndex == setIndex);
                    b2AddJointToGraph(world, jointSim, joint);
                    joint.setIndex = (int)B2SetType.b2_awakeSet;
                }
            }

            // transfer island from sleeping set to awake set
            // Usually a sleeping set has only one island, but it is possible
            // that joints are created between sleeping islands and they
            // are moved to the same sleeping set.
            {
                int islandCount = set.islandSims.count;
                for (int i = 0; i < islandCount; ++i)
                {
                    B2IslandSim islandSrc = set.islandSims.data[i];
                    B2Island island = b2Array_Get(ref world.islands, islandSrc.islandId);
                    island.setIndex = (int)B2SetType.b2_awakeSet;
                    island.localIndex = awakeSet.islandSims.count;
                    ref B2IslandSim islandDst = ref b2Array_Add(ref awakeSet.islandSims);
                    //memcpy( islandDst, islandSrc, sizeof( b2IslandSim ) );
                    islandDst.CopyFrom(islandSrc);
                }
            }

            // destroy the sleeping set
            b2DestroySolverSet(world, setIndex);
        }

        public static void b2TrySleepIsland(B2World world, int islandId)
        {
            B2Island island = b2Array_Get(ref world.islands, islandId);
            B2_ASSERT(island.setIndex == (int)B2SetType.b2_awakeSet);

            // cannot put an island to sleep while it has a pending split
            if (island.constraintRemoveCount > 0)
            {
                return;
            }

            // island is sleeping
            // - create new sleeping solver set
            // - move island to sleeping solver set
            // - identify non-touching contacts that should move to sleeping solver set or disabled set
            // - remove old island
            // - fix island
            int sleepSetId = b2AllocId(world.solverSetIdPool);
            if (sleepSetId == world.solverSets.count)
            {
                B2SolverSet set = new B2SolverSet();
                set.setIndex = B2_NULL_INDEX;
                b2Array_Push(ref world.solverSets, set);
            }

            B2SolverSet sleepSet = b2Array_Get(ref world.solverSets, sleepSetId);
            //*sleepSet = ( b2SolverSet ){ 0 };
            sleepSet.Clear();

            // grab awake set after creating the sleep set because the solver set array may have been resized
            B2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_awakeSet);
            B2_ASSERT(0 <= island.localIndex && island.localIndex < awakeSet.islandSims.count);

            sleepSet.setIndex = sleepSetId;
            sleepSet.bodySims = b2Array_Create<B2BodySim>(island.bodyCount);
            sleepSet.contactSims = b2Array_Create<B2ContactSim>(island.contactCount);
            sleepSet.jointSims = b2Array_Create<B2JointSim>(island.jointCount);

            // move awake bodies to sleeping set
            // this shuffles around bodies in the awake set
            {
                B2SolverSet disabledSet = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_disabledSet);
                int bodyId = island.headBody;
                while (bodyId != B2_NULL_INDEX)
                {
                    B2Body body = b2Array_Get(ref world.bodies, bodyId);
                    B2_ASSERT(body.setIndex == (int)B2SetType.b2_awakeSet);
                    B2_ASSERT(body.islandId == islandId);

                    // Update the body move event to indicate this body fell asleep
                    // It could happen the body is forced asleep before it ever moves.
                    if (body.bodyMoveIndex != B2_NULL_INDEX)
                    {
                        ref B2BodyMoveEvent moveEvent = ref b2Array_Get(ref world.bodyMoveEvents, body.bodyMoveIndex);
                        B2_ASSERT(moveEvent.bodyId.index1 - 1 == bodyId);
                        B2_ASSERT(moveEvent.bodyId.generation == body.generation);
                        moveEvent.fellAsleep = true;
                        body.bodyMoveIndex = B2_NULL_INDEX;
                    }

                    int awakeBodyIndex = body.localIndex;
                    B2BodySim awakeSim = b2Array_Get(ref awakeSet.bodySims, awakeBodyIndex);

                    // move body sim to sleep set
                    int sleepBodyIndex = sleepSet.bodySims.count;
                    ref B2BodySim sleepBodySim = ref b2Array_Add(ref sleepSet.bodySims);
                    //memcpy( sleepBodySim, awakeSim, sizeof( b2BodySim ) );
                    sleepBodySim.CopyFrom(awakeSim);

                    int movedIndex = b2Array_RemoveSwap(ref awakeSet.bodySims, awakeBodyIndex);
                    if (movedIndex != B2_NULL_INDEX)
                    {
                        // fix local index on moved element
                        B2BodySim movedSim = awakeSet.bodySims.data[awakeBodyIndex];
                        int movedId = movedSim.bodyId;
                        B2Body movedBody = b2Array_Get(ref world.bodies, movedId);
                        B2_ASSERT(movedBody.localIndex == movedIndex);
                        movedBody.localIndex = awakeBodyIndex;
                    }

                    // destroy state, no need to clone
                    b2Array_RemoveSwap(ref awakeSet.bodyStates, awakeBodyIndex);

                    body.setIndex = sleepSetId;
                    body.localIndex = sleepBodyIndex;

                    // Move non-touching contacts to the disabled set.
                    // Non-touching contacts may exist between sleeping islands and there is no clear ownership.
                    int contactKey = body.headContactKey;
                    while (contactKey != B2_NULL_INDEX)
                    {
                        int contactId = contactKey >> 1;
                        int edgeIndex = contactKey & 1;

                        B2Contact contact = b2Array_Get(ref world.contacts, contactId);

                        B2_ASSERT(contact.setIndex == (int)B2SetType.b2_awakeSet || contact.setIndex == (int)B2SetType.b2_disabledSet);
                        contactKey = contact.edges[edgeIndex].nextKey;

                        if (contact.setIndex == (int)B2SetType.b2_disabledSet)
                        {
                            // already moved to disabled set by another body in the island
                            continue;
                        }

                        if (contact.colorIndex != B2_NULL_INDEX)
                        {
                            // contact is touching and will be moved separately
                            B2_ASSERT((contact.flags & (int)B2ContactFlags.b2_contactTouchingFlag) != 0);
                            continue;
                        }

                        // the other body may still be awake, it still may go to sleep and then it will be responsible
                        // for moving this contact to the disabled set.
                        int otherEdgeIndex = edgeIndex ^ 1;
                        int otherBodyId = contact.edges[otherEdgeIndex].bodyId;
                        B2Body otherBody = b2Array_Get(ref world.bodies, otherBodyId);
                        if (otherBody.setIndex == (int)B2SetType.b2_awakeSet)
                        {
                            continue;
                        }

                        int localIndex = contact.localIndex;
                        B2ContactSim contactSim = b2Array_Get(ref awakeSet.contactSims, localIndex);

                        B2_ASSERT(contactSim.manifold.pointCount == 0);
                        B2_ASSERT((contact.flags & (int)B2ContactFlags.b2_contactTouchingFlag) == 0);

                        // move the non-touching contact to the disabled set
                        contact.setIndex = (int)B2SetType.b2_disabledSet;
                        contact.localIndex = disabledSet.contactSims.count;
                        ref B2ContactSim disabledContactSim = ref b2Array_Add(ref disabledSet.contactSims);
                        //memcpy( disabledContactSim, contactSim, sizeof( b2ContactSim ) );
                        disabledContactSim.CopyFrom(contactSim);

                        int movedLocalIndex = b2Array_RemoveSwap(ref awakeSet.contactSims, localIndex);
                        if (movedLocalIndex != B2_NULL_INDEX)
                        {
                            // fix moved element
                            B2ContactSim movedContactSim = awakeSet.contactSims.data[localIndex];
                            B2Contact movedContact = b2Array_Get(ref world.contacts, movedContactSim.contactId);
                            B2_ASSERT(movedContact.localIndex == movedLocalIndex);
                            movedContact.localIndex = localIndex;
                        }
                    }

                    bodyId = body.islandNext;
                }
            }

            // move touching contacts
            // this shuffles contacts in the awake set
            {
                int contactId = island.headContact;
                while (contactId != B2_NULL_INDEX)
                {
                    B2Contact contact = b2Array_Get(ref world.contacts, contactId);
                    B2_ASSERT(contact.setIndex == (int)B2SetType.b2_awakeSet);
                    B2_ASSERT(contact.islandId == islandId);
                    int colorIndex = contact.colorIndex;
                    B2_ASSERT(0 <= colorIndex && colorIndex < B2_GRAPH_COLOR_COUNT);

                    ref B2GraphColor color = ref world.constraintGraph.colors[colorIndex];

                    // Remove bodies from graph coloring associated with this constraint
                    if (colorIndex != B2_OVERFLOW_INDEX)
                    {
                        // might clear a bit for a static body, but this has no effect
                        b2ClearBit(ref color.bodySet, (uint)contact.edges[0].bodyId);
                        b2ClearBit(ref color.bodySet, (uint)contact.edges[1].bodyId);
                    }

                    int localIndex = contact.localIndex;
                    B2ContactSim awakeContactSim = b2Array_Get(ref color.contactSims, localIndex);

                    int sleepContactIndex = sleepSet.contactSims.count;
                    ref B2ContactSim sleepContactSim = ref b2Array_Add(ref sleepSet.contactSims);
                    //memcpy( sleepContactSim, awakeContactSim, sizeof( b2ContactSim ) );
                    sleepContactSim.CopyFrom(awakeContactSim);

                    int movedLocalIndex = b2Array_RemoveSwap(ref color.contactSims, localIndex);
                    if (movedLocalIndex != B2_NULL_INDEX)
                    {
                        // fix moved element
                        B2ContactSim movedContactSim = color.contactSims.data[localIndex];
                        B2Contact movedContact = b2Array_Get(ref world.contacts, movedContactSim.contactId);
                        B2_ASSERT(movedContact.localIndex == movedLocalIndex);
                        movedContact.localIndex = localIndex;
                    }

                    contact.setIndex = sleepSetId;
                    contact.colorIndex = B2_NULL_INDEX;
                    contact.localIndex = sleepContactIndex;

                    contactId = contact.islandNext;
                }
            }

            // move joints
            // this shuffles joints in the awake set
            {
                int jointId = island.headJoint;
                while (jointId != B2_NULL_INDEX)
                {
                    B2Joint joint = b2Array_Get(ref world.joints, jointId);
                    B2_ASSERT(joint.setIndex == (int)B2SetType.b2_awakeSet);
                    B2_ASSERT(joint.islandId == islandId);
                    int colorIndex = joint.colorIndex;
                    int localIndex = joint.localIndex;

                    B2_ASSERT(0 <= colorIndex && colorIndex < B2_GRAPH_COLOR_COUNT);

                    ref B2GraphColor color = ref world.constraintGraph.colors[colorIndex];

                    B2JointSim awakeJointSim = b2Array_Get(ref color.jointSims, localIndex);

                    if (colorIndex != B2_OVERFLOW_INDEX)
                    {
                        // might clear a bit for a static body, but this has no effect
                        b2ClearBit(ref color.bodySet, (uint)joint.edges[0].bodyId);
                        b2ClearBit(ref color.bodySet, (uint)joint.edges[1].bodyId);
                    }

                    int sleepJointIndex = sleepSet.jointSims.count;
                    ref B2JointSim sleepJointSim = ref b2Array_Add(ref sleepSet.jointSims);
                    //memcpy( sleepJointSim, awakeJointSim, sizeof( b2JointSim ) );
                    sleepJointSim.CopyFrom(awakeJointSim);

                    int movedIndex = b2Array_RemoveSwap(ref color.jointSims, localIndex);
                    if (movedIndex != B2_NULL_INDEX)
                    {
                        // fix moved element
                        B2JointSim movedJointSim = color.jointSims.data[localIndex];
                        int movedId = movedJointSim.jointId;
                        B2Joint movedJoint = b2Array_Get(ref world.joints, movedId);
                        B2_ASSERT(movedJoint.localIndex == movedIndex);
                        movedJoint.localIndex = localIndex;
                    }

                    joint.setIndex = sleepSetId;
                    joint.colorIndex = B2_NULL_INDEX;
                    joint.localIndex = sleepJointIndex;

                    jointId = joint.islandNext;
                }
            }

            // move island struct
            {
                B2_ASSERT(island.setIndex == (int)B2SetType.b2_awakeSet);

                int islandIndex = island.localIndex;
                ref B2IslandSim sleepIsland = ref b2Array_Add(ref sleepSet.islandSims);
                sleepIsland.islandId = islandId;

                int movedIslandIndex = b2Array_RemoveSwap(ref awakeSet.islandSims, islandIndex);
                if (movedIslandIndex != B2_NULL_INDEX)
                {
                    // fix index on moved element
                    B2IslandSim movedIslandSim = awakeSet.islandSims.data[islandIndex];
                    int movedIslandId = movedIslandSim.islandId;
                    B2Island movedIsland = b2Array_Get(ref world.islands, movedIslandId);
                    B2_ASSERT(movedIsland.localIndex == movedIslandIndex);
                    movedIsland.localIndex = islandIndex;
                }

                island.setIndex = sleepSetId;
                island.localIndex = 0;
            }

            b2ValidateSolverSets(world);
        }

        // Merge set 2 into set 1 then destroy set 2.
        // Warning: any pointers into these sets will be orphaned.
        // This is called when joints are created between sets. I want to allow the sets
        // to continue sleeping if both are asleep. Otherwise one set is waked.
        // Islands will get merge when the set is waked.
        public static void b2MergeSolverSets(B2World world, int setId1, int setId2)
        {
            B2_ASSERT(setId1 >= (int)B2SetType.b2_firstSleepingSet);
            B2_ASSERT(setId2 >= (int)B2SetType.b2_firstSleepingSet);
            B2SolverSet set1 = b2Array_Get(ref world.solverSets, setId1);
            B2SolverSet set2 = b2Array_Get(ref world.solverSets, setId2);

            // Move the fewest number of bodies
            if (set1.bodySims.count < set2.bodySims.count)
            {
                B2SolverSet tempSet = set1;
                set1 = set2;
                set2 = tempSet;

                int tempId = setId1;
                setId1 = setId2;
                setId2 = tempId;
            }

            // transfer bodies
            {
                B2Body[] bodies = world.bodies.data;
                int bodyCount = set2.bodySims.count;
                for (int i = 0; i < bodyCount; ++i)
                {
                    B2BodySim simSrc = set2.bodySims.data[i];

                    B2Body body = bodies[simSrc.bodyId];
                    B2_ASSERT(body.setIndex == setId2);
                    body.setIndex = setId1;
                    body.localIndex = set1.bodySims.count;

                    ref B2BodySim simDst = ref b2Array_Add(ref set1.bodySims);
                    //memcpy( simDst, simSrc, sizeof( b2BodySim ) );
                    simDst.CopyFrom(simSrc);
                }
            }

            // transfer contacts
            {
                int contactCount = set2.contactSims.count;
                for (int i = 0; i < contactCount; ++i)
                {
                    B2ContactSim contactSrc = set2.contactSims.data[i];

                    B2Contact contact = b2Array_Get(ref world.contacts, contactSrc.contactId);
                    B2_ASSERT(contact.setIndex == setId2);
                    contact.setIndex = setId1;
                    contact.localIndex = set1.contactSims.count;

                    ref B2ContactSim contactDst = ref b2Array_Add(ref set1.contactSims);
                    //memcpy( contactDst, contactSrc, sizeof( b2ContactSim ) );
                    contactDst.CopyFrom(contactSrc);
                }
            }

            // transfer joints
            {
                int jointCount = set2.jointSims.count;
                for (int i = 0; i < jointCount; ++i)
                {
                    B2JointSim jointSrc = set2.jointSims.data[i];

                    B2Joint joint = b2Array_Get(ref world.joints, jointSrc.jointId);
                    B2_ASSERT(joint.setIndex == setId2);
                    joint.setIndex = setId1;
                    joint.localIndex = set1.jointSims.count;

                    ref B2JointSim jointDst = ref b2Array_Add(ref set1.jointSims);
                    //memcpy( jointDst, jointSrc, sizeof( b2JointSim ) );
                    jointDst.CopyFrom(jointSrc);
                }
            }

            // transfer islands
            {
                int islandCount = set2.islandSims.count;
                for (int i = 0; i < islandCount; ++i)
                {
                    B2IslandSim islandSrc = set2.islandSims.data[i];
                    int islandId = islandSrc.islandId;

                    B2Island island = b2Array_Get(ref world.islands, islandId);
                    island.setIndex = setId1;
                    island.localIndex = set1.islandSims.count;

                    ref B2IslandSim islandDst = ref b2Array_Add(ref set1.islandSims);
                    //memcpy( islandDst, islandSrc, sizeof( b2IslandSim ) );
                    islandDst.CopyFrom(islandSrc);
                }
            }

            // destroy the merged set
            b2DestroySolverSet(world, setId2);

            b2ValidateSolverSets(world);
        }

        public static void b2TransferBody(B2World world, B2SolverSet targetSet, B2SolverSet sourceSet, B2Body body)
        {
            B2_ASSERT(targetSet != sourceSet);

            int sourceIndex = body.localIndex;
            B2BodySim sourceSim = b2Array_Get(ref sourceSet.bodySims, sourceIndex);

            int targetIndex = targetSet.bodySims.count;
            ref B2BodySim targetSim = ref b2Array_Add(ref targetSet.bodySims);
            //memcpy( targetSim, sourceSim, sizeof( b2BodySim ) );
            targetSim.CopyFrom(sourceSim);

            // Remove body sim from solver set that owns it
            int movedIndex = b2Array_RemoveSwap(ref sourceSet.bodySims, sourceIndex);
            if (movedIndex != B2_NULL_INDEX)
            {
                // Fix moved body index
                B2BodySim movedSim = sourceSet.bodySims.data[sourceIndex];
                int movedId = movedSim.bodyId;
                B2Body movedBody = b2Array_Get(ref world.bodies, movedId);
                B2_ASSERT(movedBody.localIndex == movedIndex);
                movedBody.localIndex = sourceIndex;
            }

            if (sourceSet.setIndex == (int)B2SetType.b2_awakeSet)
            {
                b2Array_RemoveSwap(ref sourceSet.bodyStates, sourceIndex);
            }
            else if (targetSet.setIndex == (int)B2SetType.b2_awakeSet)
            {
                ref B2BodyState state = ref b2Array_Add(ref targetSet.bodyStates);
                //*state = b2_identityBodyState;
                state.CopyFrom(b2_identityBodyState);
                state.flags = body.flags;
            }

            body.setIndex = targetSet.setIndex;
            body.localIndex = targetIndex;
        }

        public static void b2TransferJoint(B2World world, B2SolverSet targetSet, B2SolverSet sourceSet, B2Joint joint)
        {
            B2_ASSERT(targetSet != sourceSet);

            int localIndex = joint.localIndex;
            int colorIndex = joint.colorIndex;

            // Retrieve source.
            B2JointSim sourceSim = null;
            if (sourceSet.setIndex == (int)B2SetType.b2_awakeSet)
            {
                B2_ASSERT(0 <= colorIndex && colorIndex < B2_GRAPH_COLOR_COUNT);
                ref B2GraphColor color = ref world.constraintGraph.colors[colorIndex];

                sourceSim = b2Array_Get(ref color.jointSims, localIndex);
            }
            else
            {
                B2_ASSERT(colorIndex == B2_NULL_INDEX);
                sourceSim = b2Array_Get(ref sourceSet.jointSims, localIndex);
            }

            // Create target and copy. Fix joint.
            if (targetSet.setIndex == (int)B2SetType.b2_awakeSet)
            {
                b2AddJointToGraph(world, sourceSim, joint);
                joint.setIndex = (int)B2SetType.b2_awakeSet;
            }
            else
            {
                joint.setIndex = targetSet.setIndex;
                joint.localIndex = targetSet.jointSims.count;
                joint.colorIndex = B2_NULL_INDEX;

                ref B2JointSim targetSim = ref b2Array_Add(ref targetSet.jointSims);
                //memcpy( targetSim, sourceSim, sizeof( b2JointSim ) );
                targetSim.CopyFrom(sourceSim);
            }

            // Destroy source.
            if (sourceSet.setIndex == (int)B2SetType.b2_awakeSet)
            {
                b2RemoveJointFromGraph(world, joint.edges[0].bodyId, joint.edges[1].bodyId, colorIndex, localIndex);
            }
            else
            {
                int movedIndex = b2Array_RemoveSwap(ref sourceSet.jointSims, localIndex);
                if (movedIndex != B2_NULL_INDEX)
                {
                    // fix swapped element
                    B2JointSim movedJointSim = sourceSet.jointSims.data[localIndex];
                    int movedId = movedJointSim.jointId;
                    B2Joint movedJoint = b2Array_Get(ref world.joints, movedId);
                    movedJoint.localIndex = localIndex;
                }
            }
        }
    }
}
