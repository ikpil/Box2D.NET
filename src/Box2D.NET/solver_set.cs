// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using static Box2D.NET.array;
using static Box2D.NET.constants;
using static Box2D.NET.body;
using static Box2D.NET.world;
using static Box2D.NET.id_pool;
using static Box2D.NET.constraint_graph;
using static Box2D.NET.bitset;


namespace Box2D.NET;

// This holds solver set data. The following sets are used:
// - static set for all static bodies (no contacts or joints)
// - active set for all active bodies with body states (no contacts or joints)
// - disabled set for disabled bodies and their joints
// - all further sets are sleeping island sets along with their contacts and joints
// The purpose of solver sets is to achieve high memory locality.
// https://www.youtube.com/watch?v=nZNd5FjSquk
public class b2SolverSet
{
    // Body array. Empty for unused set.
    public b2Array<b2BodySim> bodySims;

    // Body state only exists for active set
    public b2Array<b2BodyState> bodyStates;

    // This holds sleeping/disabled joints. Empty for static/active set.
    public b2Array<b2JointSim> jointSims;

    // This holds all contacts for sleeping sets.
    // This holds non-touching contacts for the awake set.
    public b2Array<b2ContactSim> contactSims;

    // The awake set has an array of islands. Sleeping sets normally have a single islands. However, joints
    // created between sleeping sets causes the sets to merge, leaving them with multiple islands. These sleeping
    // islands will be naturally merged with the set is woken.
    // The static and disabled sets have no islands.
    // Islands live in the solver sets to limit the number of islands that need to be considered for sleeping.
    public b2Array<b2IslandSim> islandSims;

    // Aligns with b2World::solverSetIdPool. Used to create a stable id for body/contact/joint/islands.
    public int setIndex;

    public void Clear()
    {
        bodySims = null;
        bodyStates = null;
        jointSims = null;
        contactSims = null;
        islandSims = null;
        setIndex = 0;
    }
}

public class solver_set
{
    public static b2SolverSet b2CreateSolverSet(b2World world)
    {
        var set = new b2SolverSet();
         set.bodySims = b2Array_Create<b2BodySim>();
         set.bodyStates = b2Array_Create<b2BodyState>();
         set.jointSims = b2Array_Create<b2JointSim>();
         set.contactSims = b2Array_Create<b2ContactSim>();
         set.islandSims = b2Array_Create<b2IslandSim>();
        
        return set;
    }
    
    public static void b2DestroySolverSet(b2World world, int setIndex)
    {
        b2SolverSet set = b2Array_Get(world.solverSets, setIndex);
        b2Array_Destroy(set.bodySims);
        b2Array_Destroy(set.bodyStates);
        b2Array_Destroy(set.contactSims);
        b2Array_Destroy(set.jointSims);
        b2Array_Destroy(set.islandSims);
        b2FreeId(world.solverSetIdPool, setIndex);
        set.Clear();
        set.setIndex = B2_NULL_INDEX;
    }

    // Wake a solver set. Does not merge islands.
    // Contacts can be in several places:
    // 1. non-touching contacts in the disabled set
    // 2. non-touching contacts already in the awake set
    // 3. touching contacts in the sleeping set
    // This handles contact types 1 and 3. Type 2 doesn't need any action.
    public static void b2WakeSolverSet(b2World world, int setIndex)
    {
        Debug.Assert(setIndex >= (int)b2SetType.b2_firstSleepingSet);
        b2SolverSet set = b2Array_Get(world.solverSets, setIndex);
        b2SolverSet awakeSet = b2Array_Get(world.solverSets, (int)b2SetType.b2_awakeSet);
        b2SolverSet disabledSet = b2Array_Get(world.solverSets, (int)b2SetType.b2_disabledSet);

        b2Body[] bodies = world.bodies.data;

        int bodyCount = set.bodySims.count;
        for (int i = 0; i < bodyCount; ++i)
        {
            b2BodySim simSrc = set.bodySims.data[i];

            b2Body body = bodies[simSrc.bodyId];
            Debug.Assert(body.setIndex == setIndex);
            body.setIndex = (int)b2SetType.b2_awakeSet;
            body.localIndex = awakeSet.bodySims.count;

            // Reset sleep timer
            body.sleepTime = 0.0f;

            b2BodySim simDst = b2Array_Add(awakeSet.bodySims);
            //memcpy( simDst, simSrc, sizeof( b2BodySim ) );
            simDst.CopyFrom(simSrc);

            b2BodyState state = b2Array_Add(awakeSet.bodyStates);
            state.CopyFrom(b2_identityBodyState);

            // move non-touching contacts from disabled set to awake set
            int contactKey = body.headContactKey;
            while (contactKey != B2_NULL_INDEX)
            {
                int edgeIndex = contactKey & 1;
                int contactId = contactKey >> 1;

                b2Contact contact = b2Array_Get(world.contacts, contactId);

                contactKey = contact.edges[edgeIndex].nextKey;

                if (contact.setIndex != (int)b2SetType.b2_disabledSet)
                {
                    Debug.Assert(contact.setIndex == (int)b2SetType.b2_awakeSet || contact.setIndex == setIndex);
                    continue;
                }

                int localIndex = contact.localIndex;
                b2ContactSim contactSim = b2Array_Get(disabledSet.contactSims, localIndex);

                Debug.Assert((contact.flags & (int)b2ContactFlags.b2_contactTouchingFlag) == 0 && contactSim.manifold.pointCount == 0);

                contact.setIndex = (int)b2SetType.b2_awakeSet;
                contact.localIndex = awakeSet.contactSims.count;
                b2ContactSim awakeContactSim = b2Array_Add(awakeSet.contactSims);
                //memcpy( awakeContactSim, contactSim, sizeof( b2ContactSim ) );
                awakeContactSim.CopyFrom(contactSim);

                int movedLocalIndex = b2Array_RemoveSwap(disabledSet.contactSims, localIndex);
                if (movedLocalIndex != B2_NULL_INDEX)
                {
                    // fix moved element
                    b2ContactSim movedContactSim = disabledSet.contactSims.data[localIndex];
                    b2Contact movedContact = b2Array_Get(world.contacts, movedContactSim.contactId);
                    Debug.Assert(movedContact.localIndex == movedLocalIndex);
                    movedContact.localIndex = localIndex;
                }
            }
        }

        // transfer touching contacts from sleeping set to contact graph
        {
            int contactCount = set.contactSims.count;
            for (int i = 0; i < contactCount; ++i)
            {
                b2ContactSim contactSim = set.contactSims.data[i];
                b2Contact contact = b2Array_Get(world.contacts, contactSim.contactId);
                Debug.Assert(0 != (contact.flags & (int)b2ContactFlags.b2_contactTouchingFlag));
                Debug.Assert(0 != (contactSim.simFlags & (int)b2ContactSimFlags.b2_simTouchingFlag));
                Debug.Assert(contactSim.manifold.pointCount > 0);
                Debug.Assert(contact.setIndex == setIndex);
                b2AddContactToGraph(world, contactSim, contact);
                contact.setIndex = (int)b2SetType.b2_awakeSet;
            }
        }

        // transfer joints from sleeping set to awake set
        {
            int jointCount = set.jointSims.count;
            for (int i = 0; i < jointCount; ++i)
            {
                b2JointSim jointSim = set.jointSims.data[i];
                b2Joint joint = b2Array_Get(world.joints, jointSim.jointId);
                Debug.Assert(joint.setIndex == setIndex);
                b2AddJointToGraph(world, jointSim, joint);
                joint.setIndex = (int)b2SetType.b2_awakeSet;
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
                b2IslandSim islandSrc = set.islandSims.data[i];
                b2Island island = b2Array_Get(world.islands, islandSrc.islandId);
                island.setIndex = (int)b2SetType.b2_awakeSet;
                island.localIndex = awakeSet.islandSims.count;
                b2IslandSim islandDst = b2Array_Add(awakeSet.islandSims);
                //memcpy( islandDst, islandSrc, sizeof( b2IslandSim ) );
                islandDst.CopyFrom(islandSrc);
            }
        }

        // destroy the sleeping set
        b2DestroySolverSet(world, setIndex);

        b2ValidateSolverSets(world);
    }

    public static void b2TrySleepIsland(b2World world, int islandId)
    {
        b2Island island = b2Array_Get(world.islands, islandId);
        Debug.Assert(island.setIndex == (int)b2SetType.b2_awakeSet);

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
            b2SolverSet set = new b2SolverSet();
            set.setIndex = B2_NULL_INDEX;
            b2Array_Push(world.solverSets, set);
        }

        b2SolverSet sleepSet = b2Array_Get(world.solverSets, sleepSetId);
        //*sleepSet = ( b2SolverSet ){ 0 };
        sleepSet.Clear();

        // grab awake set after creating the sleep set because the solver set array may have been resized
        b2SolverSet awakeSet = b2Array_Get(world.solverSets, (int)b2SetType.b2_awakeSet);
        Debug.Assert(0 <= island.localIndex && island.localIndex < awakeSet.islandSims.count);

        sleepSet.setIndex = sleepSetId;
        sleepSet.bodySims = b2Array_Create<b2BodySim>(island.bodyCount);
        sleepSet.contactSims = b2Array_Create<b2ContactSim>(island.contactCount);
        sleepSet.jointSims = b2Array_Create<b2JointSim>(island.jointCount);

        // move awake bodies to sleeping set
        // this shuffles around bodies in the awake set
        {
            b2SolverSet disabledSet = b2Array_Get(world.solverSets, (int)b2SetType.b2_disabledSet);
            int bodyId = island.headBody;
            while (bodyId != B2_NULL_INDEX)
            {
                b2Body body = b2Array_Get(world.bodies, bodyId);
                Debug.Assert(body.setIndex == (int)b2SetType.b2_awakeSet);
                Debug.Assert(body.islandId == islandId);

                // Update the body move event to indicate this body fell asleep
                // It could happen the body is forced asleep before it ever moves.
                if (body.bodyMoveIndex != B2_NULL_INDEX)
                {
                    b2BodyMoveEvent moveEvent = b2Array_Get(world.bodyMoveEvents, body.bodyMoveIndex);
                    Debug.Assert(moveEvent.bodyId.index1 - 1 == bodyId);
                    Debug.Assert(moveEvent.bodyId.generation == body.generation);
                    moveEvent.fellAsleep = true;
                    body.bodyMoveIndex = B2_NULL_INDEX;
                }

                int awakeBodyIndex = body.localIndex;
                b2BodySim awakeSim = b2Array_Get(awakeSet.bodySims, awakeBodyIndex);

                // move body sim to sleep set
                int sleepBodyIndex = sleepSet.bodySims.count;
                b2BodySim sleepBodySim = b2Array_Add(sleepSet.bodySims);
                //memcpy( sleepBodySim, awakeSim, sizeof( b2BodySim ) );
                sleepBodySim.CopyFrom(awakeSim);

                int movedIndex = b2Array_RemoveSwap(awakeSet.bodySims, awakeBodyIndex);
                if (movedIndex != B2_NULL_INDEX)
                {
                    // fix local index on moved element
                    b2BodySim movedSim = awakeSet.bodySims.data[awakeBodyIndex];
                    int movedId = movedSim.bodyId;
                    b2Body movedBody = b2Array_Get(world.bodies, movedId);
                    Debug.Assert(movedBody.localIndex == movedIndex);
                    movedBody.localIndex = awakeBodyIndex;
                }

                // destroy state, no need to clone
                b2Array_RemoveSwap(awakeSet.bodyStates, awakeBodyIndex);

                body.setIndex = sleepSetId;
                body.localIndex = sleepBodyIndex;

                // Move non-touching contacts to the disabled set.
                // Non-touching contacts may exist between sleeping islands and there is no clear ownership.
                int contactKey = body.headContactKey;
                while (contactKey != B2_NULL_INDEX)
                {
                    int contactId = contactKey >> 1;
                    int edgeIndex = contactKey & 1;

                    b2Contact contact = b2Array_Get(world.contacts, contactId);

                    Debug.Assert(contact.setIndex == (int)b2SetType.b2_awakeSet || contact.setIndex == (int)b2SetType.b2_disabledSet);
                    contactKey = contact.edges[edgeIndex].nextKey;

                    if (contact.setIndex == (int)b2SetType.b2_disabledSet)
                    {
                        // already moved to disabled set by another body in the island
                        continue;
                    }

                    if (contact.colorIndex != B2_NULL_INDEX)
                    {
                        // contact is touching and will be moved separately
                        Debug.Assert((contact.flags & (int)b2ContactFlags.b2_contactTouchingFlag) != 0);
                        continue;
                    }

                    // the other body may still be awake, it still may go to sleep and then it will be responsible
                    // for moving this contact to the disabled set.
                    int otherEdgeIndex = edgeIndex ^ 1;
                    int otherBodyId = contact.edges[otherEdgeIndex].bodyId;
                    b2Body otherBody = b2Array_Get(world.bodies, otherBodyId);
                    if (otherBody.setIndex == (int)b2SetType.b2_awakeSet)
                    {
                        continue;
                    }

                    int localIndex = contact.localIndex;
                    b2ContactSim contactSim = b2Array_Get(awakeSet.contactSims, localIndex);

                    Debug.Assert(contactSim.manifold.pointCount == 0);
                    Debug.Assert((contact.flags & (int)b2ContactFlags.b2_contactTouchingFlag) == 0);

                    // move the non-touching contact to the disabled set
                    contact.setIndex = (int)b2SetType.b2_disabledSet;
                    contact.localIndex = disabledSet.contactSims.count;
                    b2ContactSim disabledContactSim = b2Array_Add(disabledSet.contactSims);
                    //memcpy( disabledContactSim, contactSim, sizeof( b2ContactSim ) );
                    disabledContactSim.CopyFrom(contactSim);

                    int movedLocalIndex = b2Array_RemoveSwap(awakeSet.contactSims, localIndex);
                    if (movedLocalIndex != B2_NULL_INDEX)
                    {
                        // fix moved element
                        b2ContactSim movedContactSim = awakeSet.contactSims.data[localIndex];
                        b2Contact movedContact = b2Array_Get(world.contacts, movedContactSim.contactId);
                        Debug.Assert(movedContact.localIndex == movedLocalIndex);
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
                b2Contact contact = b2Array_Get(world.contacts, contactId);
                Debug.Assert(contact.setIndex == (int)b2SetType.b2_awakeSet);
                Debug.Assert(contact.islandId == islandId);
                int colorIndex = contact.colorIndex;
                Debug.Assert(0 <= colorIndex && colorIndex < B2_GRAPH_COLOR_COUNT);

                b2GraphColor color = world.constraintGraph.colors[colorIndex];

                // Remove bodies from graph coloring associated with this constraint
                if (colorIndex != B2_OVERFLOW_INDEX)
                {
                    // might clear a bit for a static body, but this has no effect
                    b2ClearBit(color.bodySet, (uint)contact.edges[0].bodyId);
                    b2ClearBit(color.bodySet, (uint)contact.edges[1].bodyId);
                }

                int localIndex = contact.localIndex;
                b2ContactSim awakeContactSim = b2Array_Get(color.contactSims, localIndex);

                int sleepContactIndex = sleepSet.contactSims.count;
                b2ContactSim sleepContactSim = b2Array_Add(sleepSet.contactSims);
                //memcpy( sleepContactSim, awakeContactSim, sizeof( b2ContactSim ) );
                sleepContactSim.CopyFrom(awakeContactSim);

                int movedLocalIndex = b2Array_RemoveSwap(color.contactSims, localIndex);
                if (movedLocalIndex != B2_NULL_INDEX)
                {
                    // fix moved element
                    b2ContactSim movedContactSim = color.contactSims.data[localIndex];
                    b2Contact movedContact = b2Array_Get(world.contacts, movedContactSim.contactId);
                    Debug.Assert(movedContact.localIndex == movedLocalIndex);
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
                b2Joint joint = b2Array_Get(world.joints, jointId);
                Debug.Assert(joint.setIndex == (int)b2SetType.b2_awakeSet);
                Debug.Assert(joint.islandId == islandId);
                int colorIndex = joint.colorIndex;
                int localIndex = joint.localIndex;

                Debug.Assert(0 <= colorIndex && colorIndex < B2_GRAPH_COLOR_COUNT);

                b2GraphColor color = world.constraintGraph.colors[colorIndex];

                b2JointSim awakeJointSim = b2Array_Get(color.jointSims, localIndex);

                if (colorIndex != B2_OVERFLOW_INDEX)
                {
                    // might clear a bit for a static body, but this has no effect
                    b2ClearBit(color.bodySet, (uint)joint.edges[0].bodyId);
                    b2ClearBit(color.bodySet, (uint)joint.edges[1].bodyId);
                }

                int sleepJointIndex = sleepSet.jointSims.count;
                b2JointSim sleepJointSim = b2Array_Add(sleepSet.jointSims);
                //memcpy( sleepJointSim, awakeJointSim, sizeof( b2JointSim ) );
                sleepJointSim.CopyFrom(awakeJointSim);

                int movedIndex = b2Array_RemoveSwap(color.jointSims, localIndex);
                if (movedIndex != B2_NULL_INDEX)
                {
                    // fix moved element
                    b2JointSim movedJointSim = color.jointSims.data[localIndex];
                    int movedId = movedJointSim.jointId;
                    b2Joint movedJoint = b2Array_Get(world.joints, movedId);
                    Debug.Assert(movedJoint.localIndex == movedIndex);
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
            Debug.Assert(island.setIndex == (int)b2SetType.b2_awakeSet);

            int islandIndex = island.localIndex;
            b2IslandSim sleepIsland = b2Array_Add(sleepSet.islandSims);
            sleepIsland.islandId = islandId;

            int movedIslandIndex = b2Array_RemoveSwap(awakeSet.islandSims, islandIndex);
            if (movedIslandIndex != B2_NULL_INDEX)
            {
                // fix index on moved element
                b2IslandSim movedIslandSim = awakeSet.islandSims.data[islandIndex];
                int movedIslandId = movedIslandSim.islandId;
                b2Island movedIsland = b2Array_Get(world.islands, movedIslandId);
                Debug.Assert(movedIsland.localIndex == movedIslandIndex);
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
    public static void b2MergeSolverSets(b2World world, int setId1, int setId2)
    {
        Debug.Assert(setId1 >= (int)b2SetType.b2_firstSleepingSet);
        Debug.Assert(setId2 >= (int)b2SetType.b2_firstSleepingSet);
        b2SolverSet set1 = b2Array_Get(world.solverSets, setId1);
        b2SolverSet set2 = b2Array_Get(world.solverSets, setId2);

        // Move the fewest number of bodies
        if (set1.bodySims.count < set2.bodySims.count)
        {
            b2SolverSet tempSet = set1;
            set1 = set2;
            set2 = tempSet;

            int tempId = setId1;
            setId1 = setId2;
            setId2 = tempId;
        }

        // transfer bodies
        {
            b2Body[] bodies = world.bodies.data;
            int bodyCount = set2.bodySims.count;
            for (int i = 0; i < bodyCount; ++i)
            {
                b2BodySim simSrc = set2.bodySims.data[i];

                b2Body body = bodies[simSrc.bodyId];
                Debug.Assert(body.setIndex == setId2);
                body.setIndex = setId1;
                body.localIndex = set1.bodySims.count;

                b2BodySim simDst = b2Array_Add(set1.bodySims);
                //memcpy( simDst, simSrc, sizeof( b2BodySim ) );
                simDst.CopyFrom(simSrc);
            }
        }

        // transfer contacts
        {
            int contactCount = set2.contactSims.count;
            for (int i = 0; i < contactCount; ++i)
            {
                b2ContactSim contactSrc = set2.contactSims.data[i];

                b2Contact contact = b2Array_Get(world.contacts, contactSrc.contactId);
                Debug.Assert(contact.setIndex == setId2);
                contact.setIndex = setId1;
                contact.localIndex = set1.contactSims.count;

                b2ContactSim contactDst = b2Array_Add(set1.contactSims);
                //memcpy( contactDst, contactSrc, sizeof( b2ContactSim ) );
                contactDst.CopyFrom(contactSrc);
            }
        }

        // transfer joints
        {
            int jointCount = set2.jointSims.count;
            for (int i = 0; i < jointCount; ++i)
            {
                b2JointSim jointSrc = set2.jointSims.data[i];

                b2Joint joint = b2Array_Get(world.joints, jointSrc.jointId);
                Debug.Assert(joint.setIndex == setId2);
                joint.setIndex = setId1;
                joint.localIndex = set1.jointSims.count;

                b2JointSim jointDst = b2Array_Add(set1.jointSims);
                //memcpy( jointDst, jointSrc, sizeof( b2JointSim ) );
                jointDst.CopyFrom(jointSrc);
            }
        }

        // transfer islands
        {
            int islandCount = set2.islandSims.count;
            for (int i = 0; i < islandCount; ++i)
            {
                b2IslandSim islandSrc = set2.islandSims.data[i];
                int islandId = islandSrc.islandId;

                b2Island island = b2Array_Get(world.islands, islandId);
                island.setIndex = setId1;
                island.localIndex = set1.islandSims.count;

                b2IslandSim islandDst = b2Array_Add(set1.islandSims);
                //memcpy( islandDst, islandSrc, sizeof( b2IslandSim ) );
                islandDst.CopyFrom(islandSrc);
            }
        }

        // destroy the merged set
        b2DestroySolverSet(world, setId2);

        b2ValidateSolverSets(world);
    }

    public static void b2TransferBody(b2World world, b2SolverSet targetSet, b2SolverSet sourceSet, b2Body body)
    {
        Debug.Assert(targetSet != sourceSet);

        int sourceIndex = body.localIndex;
        b2BodySim sourceSim = b2Array_Get(sourceSet.bodySims, sourceIndex);

        int targetIndex = targetSet.bodySims.count;
        b2BodySim targetSim = b2Array_Add(targetSet.bodySims);
        //memcpy( targetSim, sourceSim, sizeof( b2BodySim ) );
        targetSim.CopyFrom(sourceSim);

        // Remove body sim from solver set that owns it
        int movedIndex = b2Array_RemoveSwap(sourceSet.bodySims, sourceIndex);
        if (movedIndex != B2_NULL_INDEX)
        {
            // Fix moved body index
            b2BodySim movedSim = sourceSet.bodySims.data[sourceIndex];
            int movedId = movedSim.bodyId;
            b2Body movedBody = b2Array_Get(world.bodies, movedId);
            Debug.Assert(movedBody.localIndex == movedIndex);
            movedBody.localIndex = sourceIndex;
        }

        if (sourceSet.setIndex == (int)b2SetType.b2_awakeSet)
        {
            b2Array_RemoveSwap(sourceSet.bodyStates, sourceIndex);
        }
        else if (targetSet.setIndex == (int)b2SetType.b2_awakeSet)
        {
            b2BodyState state = b2Array_Add(targetSet.bodyStates);
            //*state = b2_identityBodyState;
            state.CopyFrom(b2_identityBodyState);
        }

        body.setIndex = targetSet.setIndex;
        body.localIndex = targetIndex;
    }

    public static void b2TransferJoint(b2World world, b2SolverSet targetSet, b2SolverSet sourceSet, b2Joint joint)
    {
        Debug.Assert(targetSet != sourceSet);

        int localIndex = joint.localIndex;
        int colorIndex = joint.colorIndex;

        // Retrieve source.
        b2JointSim sourceSim = null;
        if (sourceSet.setIndex == (int)b2SetType.b2_awakeSet)
        {
            Debug.Assert(0 <= colorIndex && colorIndex < B2_GRAPH_COLOR_COUNT);
            b2GraphColor color = world.constraintGraph.colors[colorIndex];

            sourceSim = b2Array_Get(color.jointSims, localIndex);
        }
        else
        {
            Debug.Assert(colorIndex == B2_NULL_INDEX);
            sourceSim = b2Array_Get(sourceSet.jointSims, localIndex);
        }

        // Create target and copy. Fix joint.
        if (targetSet.setIndex == (int)b2SetType.b2_awakeSet)
        {
            b2AddJointToGraph(world, sourceSim, joint);
            joint.setIndex = (int)b2SetType.b2_awakeSet;
        }
        else
        {
            joint.setIndex = targetSet.setIndex;
            joint.localIndex = targetSet.jointSims.count;
            joint.colorIndex = B2_NULL_INDEX;

            b2JointSim targetSim = b2Array_Add(targetSet.jointSims);
            //memcpy( targetSim, sourceSim, sizeof( b2JointSim ) );
            targetSim.CopyFrom(sourceSim);
        }

        // Destroy source.
        if (sourceSet.setIndex == (int)b2SetType.b2_awakeSet)
        {
            b2RemoveJointFromGraph(world, joint.edges[0].bodyId, joint.edges[1].bodyId, colorIndex, localIndex);
        }
        else
        {
            int movedIndex = b2Array_RemoveSwap(sourceSet.jointSims, localIndex);
            if (movedIndex != B2_NULL_INDEX)
            {
                // fix swapped element
                b2JointSim movedJointSim = sourceSet.jointSims.data[localIndex];
                int movedId = movedJointSim.jointId;
                b2Joint movedJoint = b2Array_Get(world.joints, movedId);
                movedJoint.localIndex = localIndex;
            }
        }
    }
}