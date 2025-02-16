// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using static Box2D.NET.table;
using static Box2D.NET.array;
using static Box2D.NET.core;
using static Box2D.NET.constants;
using static Box2D.NET.math_function;
using static Box2D.NET.shape;
using static Box2D.NET.body;
using static Box2D.NET.id_pool;
using static Box2D.NET.manifold;
using static Box2D.NET.collision;
using static Box2D.NET.island;
using static Box2D.NET.constraint_graph;


namespace Box2D.NET;

// A contact edge is used to connect bodies and contacts together
// in a contact graph where each body is a node and each contact
// is an edge. A contact edge belongs to a doubly linked list
// maintained in each attached body. Each contact has two contact
// edges, one for each attached body.

public enum b2ContactFlags
{
    // Set when the solid shapes are touching.
    b2_contactTouchingFlag = 0x00000001,

    // Contact has a hit event
    b2_contactHitEventFlag = 0x00000002,

    // This contact wants contact events
    b2_contactEnableContactEvents = 0x00000004,
}

public class b2ContactEdge
{
    public int bodyId;
    public int prevKey;
    public int nextKey;
}

// Cold contact data. Used as a persistent handle and for persistent island
// connectivity.
public class b2Contact
{
    // index of simulation set stored in b2World
    // B2_NULL_INDEX when slot is free
    public int setIndex;

    // index into the constraint graph color array
    // B2_NULL_INDEX for non-touching or sleeping contacts
    // B2_NULL_INDEX when slot is free
    public int colorIndex;

    // contact index within set or graph color
    // B2_NULL_INDEX when slot is free
    public int localIndex;

    public b2ContactEdge[] edges = new b2ContactEdge[2];
    public int shapeIdA;
    public int shapeIdB;

    // A contact only belongs to an island if touching, otherwise B2_NULL_INDEX.
    public int islandPrev;
    public int islandNext;
    public int islandId;

    public int contactId;

    // b2ContactFlags
    public uint flags;

    public bool isMarked;
};

// Shifted to be distinct from b2ContactFlags
public enum b2ContactSimFlags
{
    // Set when the shapes are touching, including sensors
    b2_simTouchingFlag = 0x00010000,

    // This contact no longer has overlapping AABBs
    b2_simDisjoint = 0x00020000,

    // This contact started touching
    b2_simStartedTouching = 0x00040000,

    // This contact stopped touching
    b2_simStoppedTouching = 0x00080000,

    // This contact has a hit event
    b2_simEnableHitEvent = 0x00100000,

    // This contact wants pre-solve events
    b2_simEnablePreSolveEvents = 0x00200000,
}

/// The class manages contact between two shapes. A contact exists for each overlapping
/// AABB in the broad-phase (except if filtered). Therefore a contact object may exist
/// that has no contact points.
public class b2ContactSim
{
    public int contactId;

#if B2_VALIDATE
	public int bodyIdA;
	public int bodyIdB;
#endif

    public int bodySimIndexA;
    public int bodySimIndexB;

    public int shapeIdA;
    public int shapeIdB;

    public float invMassA;
    public float invIA;

    public float invMassB;
    public float invIB;

    public b2Manifold manifold;

    // Mixed friction and restitution
    public float friction;
    public float restitution;
    public float rollingResistance;
    public float tangentSpeed;

    // b2ContactSimFlags
    public uint simFlags;

    public b2SimplexCache cache;

    public void CopyFrom(b2ContactSim other)
    {
        Debug.Assert(false);
        // TODO: @ikpil class type copy!?
    }
}

public struct b2ContactRegister
{
    public contact.b2ManifoldFcn fcn;
    public bool primary;
};

public static class contact
{
    public static bool b2ShouldShapesCollide(b2Filter filterA, b2Filter filterB)
    {
        if (filterA.groupIndex == filterB.groupIndex && filterA.groupIndex != 0)
        {
            return filterA.groupIndex > 0;
        }

        bool collide = (filterA.maskBits & filterB.categoryBits) != 0 && (filterA.categoryBits & filterB.maskBits) != 0;
        return collide;
    }


// Contacts and determinism
// A deterministic simulation requires contacts to exist in the same order in b2Island no matter the thread count.
// The order must reproduce from run to run. This is necessary because the Gauss-Seidel constraint solver is order dependent.
//
// Creation:
// - Contacts are created using results from b2UpdateBroadPhasePairs
// - These results are ordered according to the order of the broad-phase move array
// - The move array is ordered according to the shape creation order using a bitset.
// - The island/shape/body order is determined by creation order
// - Logically contacts are only created for awake bodies, so they are immediately added to the awake contact array (serially)
//
// Island linking:
// - The awake contact array is built from the body-contact graph for all awake bodies in awake islands.
// - Awake contacts are solved in parallel and they generate contact state changes.
// - These state changes may link islands together using union find.
// - The state changes are ordered using a bit array that encompasses all contacts
// - As long as contacts are created in deterministic order, island link order is deterministic.
// - This keeps the order of contacts in islands deterministic

// Manifold functions should compute important results in local space to improve precision. However, this
// interface function takes two world transforms instead of a relative transform for these reasons:
//
// First:
// The anchors need to be computed relative to the shape origin in world space. This is necessary so the
// solver does not need to access static body transforms. Not even in constraint preparation. This approach
// has world space vectors yet retains precision.
//
// Second:
// b3ManifoldPoint::point is very useful for debugging and it is in world space.
//
// Third:
// The user may call the manifold functions directly and they should be easy to use and have easy to use
// results.
    public delegate b2Manifold b2ManifoldFcn(b2Shape shapeA, b2Transform xfA, b2Shape shapeB, b2Transform xfB, b2SimplexCache cache);


    public static b2ContactRegister[,] s_registers = new b2ContactRegister[(int)b2ShapeType.b2_shapeTypeCount, (int)b2ShapeType.b2_shapeTypeCount];
    public static bool s_initialized = false;

    public static b2Manifold b2CircleManifold(b2Shape shapeA, b2Transform xfA, b2Shape shapeB, b2Transform xfB, b2SimplexCache cache)
    {
        B2_UNUSED(cache);
        return b2CollideCircles(shapeA.circle, xfA, shapeB.circle, xfB);
    }

    public static b2Manifold b2CapsuleAndCircleManifold(b2Shape shapeA, b2Transform xfA, b2Shape shapeB, b2Transform xfB, b2SimplexCache cache)
    {
        B2_UNUSED(cache);
        return b2CollideCapsuleAndCircle(shapeA.capsule, xfA, shapeB.circle, xfB);
    }

    public static b2Manifold b2CapsuleManifold(b2Shape shapeA, b2Transform xfA, b2Shape shapeB, b2Transform xfB, b2SimplexCache cache)
    {
        B2_UNUSED(cache);
        return b2CollideCapsules(shapeA.capsule, xfA, shapeB.capsule, xfB);
    }

    public static b2Manifold b2PolygonAndCircleManifold(b2Shape shapeA, b2Transform xfA, b2Shape shapeB, b2Transform xfB, b2SimplexCache cache)
    {
        B2_UNUSED(cache);
        return b2CollidePolygonAndCircle(shapeA.polygon, xfA, shapeB.circle, xfB);
    }

    public static b2Manifold b2PolygonAndCapsuleManifold(b2Shape shapeA, b2Transform xfA, b2Shape shapeB, b2Transform xfB, b2SimplexCache cache)
    {
        B2_UNUSED(cache);
        return b2CollidePolygonAndCapsule(shapeA.polygon, xfA, shapeB.capsule, xfB);
    }

    public static b2Manifold b2PolygonManifold(b2Shape shapeA, b2Transform xfA, b2Shape shapeB, b2Transform xfB, b2SimplexCache cache)
    {
        B2_UNUSED(cache);
        return b2CollidePolygons(shapeA.polygon, xfA, shapeB.polygon, xfB);
    }

    public static b2Manifold b2SegmentAndCircleManifold(b2Shape shapeA, b2Transform xfA, b2Shape shapeB, b2Transform xfB, b2SimplexCache cache)
    {
        B2_UNUSED(cache);
        return b2CollideSegmentAndCircle(shapeA.segment, xfA, shapeB.circle, xfB);
    }

    public static b2Manifold b2SegmentAndCapsuleManifold(b2Shape shapeA, b2Transform xfA, b2Shape shapeB, b2Transform xfB, b2SimplexCache cache)
    {
        B2_UNUSED(cache);
        return b2CollideSegmentAndCapsule(shapeA.segment, xfA, shapeB.capsule, xfB);
    }

    public static b2Manifold b2SegmentAndPolygonManifold(b2Shape shapeA, b2Transform xfA, b2Shape shapeB, b2Transform xfB, b2SimplexCache cache)
    {
        B2_UNUSED(cache);
        return b2CollideSegmentAndPolygon(shapeA.segment, xfA, shapeB.polygon, xfB);
    }

    public static b2Manifold b2ChainSegmentAndCircleManifold(b2Shape shapeA, b2Transform xfA, b2Shape shapeB, b2Transform xfB, b2SimplexCache cache)
    {
        B2_UNUSED(cache);
        return b2CollideChainSegmentAndCircle(shapeA.chainSegment, xfA, shapeB.circle, xfB);
    }

    public static b2Manifold b2ChainSegmentAndCapsuleManifold(b2Shape shapeA, b2Transform xfA, b2Shape shapeB, b2Transform xfB, b2SimplexCache cache)
    {
        return b2CollideChainSegmentAndCapsule(shapeA.chainSegment, xfA, shapeB.capsule, xfB, cache);
    }

    public static b2Manifold b2ChainSegmentAndPolygonManifold(b2Shape shapeA, b2Transform xfA, b2Shape shapeB, b2Transform xfB, b2SimplexCache cache)
    {
        return b2CollideChainSegmentAndPolygon(shapeA.chainSegment, xfA, shapeB.polygon, xfB, cache);
    }

    public static void b2AddType(b2ManifoldFcn fcn, b2ShapeType type1, b2ShapeType type2)
    {
        Debug.Assert(0 <= type1 && type1 < b2ShapeType.b2_shapeTypeCount);
        Debug.Assert(0 <= type2 && type2 < b2ShapeType.b2_shapeTypeCount);

        s_registers[(int)type1, (int)type2].fcn = fcn;
        s_registers[(int)type1, (int)type2].primary = true;

        if (type1 != type2)
        {
            s_registers[(int)type2, (int)type1].fcn = fcn;
            s_registers[(int)type2, (int)type1].primary = false;
        }
    }

    public static void b2InitializeContactRegisters()
    {
        if (s_initialized == false)
        {
            b2AddType(b2CircleManifold, b2ShapeType.b2_circleShape, b2ShapeType.b2_circleShape);
            b2AddType(b2CapsuleAndCircleManifold, b2ShapeType.b2_capsuleShape, b2ShapeType.b2_circleShape);
            b2AddType(b2CapsuleManifold, b2ShapeType.b2_capsuleShape, b2ShapeType.b2_capsuleShape);
            b2AddType(b2PolygonAndCircleManifold, b2ShapeType.b2_polygonShape, b2ShapeType.b2_circleShape);
            b2AddType(b2PolygonAndCapsuleManifold, b2ShapeType.b2_polygonShape, b2ShapeType.b2_capsuleShape);
            b2AddType(b2PolygonManifold, b2ShapeType.b2_polygonShape, b2ShapeType.b2_polygonShape);
            b2AddType(b2SegmentAndCircleManifold, b2ShapeType.b2_segmentShape, b2ShapeType.b2_circleShape);
            b2AddType(b2SegmentAndCapsuleManifold, b2ShapeType.b2_segmentShape, b2ShapeType.b2_capsuleShape);
            b2AddType(b2SegmentAndPolygonManifold, b2ShapeType.b2_segmentShape, b2ShapeType.b2_polygonShape);
            b2AddType(b2ChainSegmentAndCircleManifold, b2ShapeType.b2_chainSegmentShape, b2ShapeType.b2_circleShape);
            b2AddType(b2ChainSegmentAndCapsuleManifold, b2ShapeType.b2_chainSegmentShape, b2ShapeType.b2_capsuleShape);
            b2AddType(b2ChainSegmentAndPolygonManifold, b2ShapeType.b2_chainSegmentShape, b2ShapeType.b2_polygonShape);
            s_initialized = true;
        }
    }

    public static void b2CreateContact(b2World world, b2Shape shapeA, b2Shape shapeB)
    {
        b2ShapeType type1 = shapeA.type;
        b2ShapeType type2 = shapeB.type;

        Debug.Assert(0 <= type1 && type1 < b2ShapeType.b2_shapeTypeCount);
        Debug.Assert(0 <= type2 && type2 < b2ShapeType.b2_shapeTypeCount);

        if (s_registers[(int)type1, (int)type2].fcn == null)
        {
            // For example, no segment vs segment collision
            return;
        }

        if (s_registers[(int)type1, (int)type2].primary == false)
        {
            // flip order
            b2CreateContact(world, shapeB, shapeA);
            return;
        }

        b2Body bodyA = Array_Get(world.bodies, shapeA.bodyId);
        b2Body bodyB = Array_Get(world.bodies, shapeB.bodyId);

        Debug.Assert(bodyA.setIndex != (int)b2SetType.b2_disabledSet && bodyB.setIndex != (int)b2SetType.b2_disabledSet);
        Debug.Assert(bodyA.setIndex != (int)b2SetType.b2_staticSet || bodyB.setIndex != (int)b2SetType.b2_staticSet);

        int setIndex;
        if (bodyA.setIndex == (int)b2SetType.b2_awakeSet || bodyB.setIndex == (int)b2SetType.b2_awakeSet)
        {
            setIndex = (int)b2SetType.b2_awakeSet;
        }
        else
        {
            // sleeping and non-touching contacts live in the disabled set
            // later if this set is found to be touching then the sleeping
            // islands will be linked and the contact moved to the merged island
            setIndex = (int)b2SetType.b2_disabledSet;
        }

        b2SolverSet set = Array_Get(world.solverSets, setIndex);

        // Create contact key and contact
        int contactId = b2AllocId(world.contactIdPool);
        if (contactId == world.contacts.count)
        {
            Array_Push(world.contacts, new b2Contact());
        }

        int shapeIdA = shapeA.id;
        int shapeIdB = shapeB.id;

        b2Contact contact = Array_Get(world.contacts, contactId);
        contact.contactId = contactId;
        contact.setIndex = setIndex;
        contact.colorIndex = B2_NULL_INDEX;
        contact.localIndex = set.contactSims.count;
        contact.islandId = B2_NULL_INDEX;
        contact.islandPrev = B2_NULL_INDEX;
        contact.islandNext = B2_NULL_INDEX;
        contact.shapeIdA = shapeIdA;
        contact.shapeIdB = shapeIdB;
        contact.isMarked = false;
        contact.flags = 0;

        Debug.Assert(shapeA.sensorIndex == B2_NULL_INDEX && shapeB.sensorIndex == B2_NULL_INDEX);

        if (shapeA.enableContactEvents || shapeB.enableContactEvents)
        {
            contact.flags |= (uint)b2ContactFlags.b2_contactEnableContactEvents;
        }

        // Connect to body A
        {
            contact.edges[0].bodyId = shapeA.bodyId;
            contact.edges[0].prevKey = B2_NULL_INDEX;
            contact.edges[0].nextKey = bodyA.headContactKey;

            int keyA = (contactId << 1) | 0;
            int headContactKey = bodyA.headContactKey;
            if (headContactKey != B2_NULL_INDEX)
            {
                b2Contact headContact = Array_Get(world.contacts, headContactKey >> 1);
                headContact.edges[headContactKey & 1].prevKey = keyA;
            }

            bodyA.headContactKey = keyA;
            bodyA.contactCount += 1;
        }

        // Connect to body B
        {
            contact.edges[1].bodyId = shapeB.bodyId;
            contact.edges[1].prevKey = B2_NULL_INDEX;
            contact.edges[1].nextKey = bodyB.headContactKey;

            int keyB = (contactId << 1) | 1;
            int headContactKey = bodyB.headContactKey;
            if (bodyB.headContactKey != B2_NULL_INDEX)
            {
                b2Contact headContact = Array_Get(world.contacts, headContactKey >> 1);
                headContact.edges[headContactKey & 1].prevKey = keyB;
            }

            bodyB.headContactKey = keyB;
            bodyB.contactCount += 1;
        }

        // Add to pair set for fast lookup
        ulong pairKey = B2_SHAPE_PAIR_KEY(shapeIdA, shapeIdB);
        b2AddKey(world.broadPhase.pairSet, pairKey);

        // Contacts are created as non-touching. Later if they are found to be touching
        // they will link islands and be moved into the constraint graph.
        b2ContactSim contactSim = Array_Add(set.contactSims);
        contactSim.contactId = contactId;

#if B2_VALIDATE
	contactSim.bodyIdA = shapeA.bodyId;
	contactSim.bodyIdB = shapeB.bodyId;
#endif

        contactSim.bodySimIndexA = B2_NULL_INDEX;
        contactSim.bodySimIndexB = B2_NULL_INDEX;
        contactSim.invMassA = 0.0f;
        contactSim.invIA = 0.0f;
        contactSim.invMassB = 0.0f;
        contactSim.invIB = 0.0f;
        contactSim.shapeIdA = shapeIdA;
        contactSim.shapeIdB = shapeIdB;
        contactSim.cache = b2_emptySimplexCache;
        contactSim.manifold = new b2Manifold();

        // These also get updated in the narrow phase
        contactSim.friction = world.frictionCallback(shapeA.friction, shapeA.material, shapeB.friction, shapeB.material);
        contactSim.restitution = world.restitutionCallback(shapeA.restitution, shapeA.material, shapeB.restitution, shapeB.material);

        contactSim.tangentSpeed = 0.0f;
        contactSim.simFlags = 0;

        if (shapeA.enablePreSolveEvents || shapeB.enablePreSolveEvents)
        {
            contactSim.simFlags |= (uint)b2ContactSimFlags.b2_simEnablePreSolveEvents;
        }
    }

// A contact is destroyed when:
// - broad-phase proxies stop overlapping
// - a body is destroyed
// - a body is disabled
// - a body changes type from dynamic to kinematic or static
// - a shape is destroyed
// - contact filtering is modified
// - a shape becomes a sensor (check this!!!)
    public static void b2DestroyContact(b2World world, b2Contact contact, bool wakeBodies)
    {
        // Remove pair from set
        ulong pairKey = B2_SHAPE_PAIR_KEY(contact.shapeIdA, contact.shapeIdB);
        b2RemoveKey(world.broadPhase.pairSet, pairKey);

        b2ContactEdge edgeA = contact.edges[0];
        b2ContactEdge edgeB = contact.edges[1];

        int bodyIdA = edgeA.bodyId;
        int bodyIdB = edgeB.bodyId;
        b2Body bodyA = Array_Get(world.bodies, bodyIdA);
        b2Body bodyB = Array_Get(world.bodies, bodyIdB);

        uint flags = contact.flags;

        // End touch event
        if ((flags & (uint)b2ContactFlags.b2_contactTouchingFlag) != 0 && (flags & (uint)b2ContactFlags.b2_contactEnableContactEvents) != 0)
        {
            ushort worldId = world.worldId;
            b2Shape shapeA = Array_Get(world.shapes, contact.shapeIdA);
            b2Shape shapeB = Array_Get(world.shapes, contact.shapeIdB);
            b2ShapeId shapeIdA = new b2ShapeId(shapeA.id + 1, worldId, shapeA.generation);
            b2ShapeId shapeIdB = new b2ShapeId(shapeB.id + 1, worldId, shapeB.generation);

            b2ContactEndTouchEvent @event = new b2ContactEndTouchEvent(shapeIdA, shapeIdB);
            Array_Push(world.contactEndEvents[world.endEventArrayIndex], @event);
        }

        // Remove from body A
        if (edgeA.prevKey != B2_NULL_INDEX)
        {
            b2Contact prevContact = Array_Get(world.contacts, edgeA.prevKey >> 1);
            b2ContactEdge prevEdge = prevContact.edges[(edgeA.prevKey & 1)];
            prevEdge.nextKey = edgeA.nextKey;
        }

        if (edgeA.nextKey != B2_NULL_INDEX)
        {
            b2Contact nextContact = Array_Get(world.contacts, edgeA.nextKey >> 1);
            b2ContactEdge nextEdge = nextContact.edges[(edgeA.nextKey & 1)];
            nextEdge.prevKey = edgeA.prevKey;
        }

        int contactId = contact.contactId;

        int edgeKeyA = (contactId << 1) | 0;
        if (bodyA.headContactKey == edgeKeyA)
        {
            bodyA.headContactKey = edgeA.nextKey;
        }

        bodyA.contactCount -= 1;

        // Remove from body B
        if (edgeB.prevKey != B2_NULL_INDEX)
        {
            b2Contact prevContact = Array_Get(world.contacts, edgeB.prevKey >> 1);
            b2ContactEdge prevEdge = prevContact.edges[(edgeB.prevKey & 1)];
            prevEdge.nextKey = edgeB.nextKey;
        }

        if (edgeB.nextKey != B2_NULL_INDEX)
        {
            b2Contact nextContact = Array_Get(world.contacts, edgeB.nextKey >> 1);
            b2ContactEdge nextEdge = nextContact.edges[(edgeB.nextKey & 1)];
            nextEdge.prevKey = edgeB.prevKey;
        }

        int edgeKeyB = (contactId << 1) | 1;
        if (bodyB.headContactKey == edgeKeyB)
        {
            bodyB.headContactKey = edgeB.nextKey;
        }

        bodyB.contactCount -= 1;

        // Remove contact from the array that owns it
        if (contact.islandId != B2_NULL_INDEX)
        {
            b2UnlinkContact(world, contact);
        }

        if (contact.colorIndex != B2_NULL_INDEX)
        {
            // contact is an active constraint
            Debug.Assert(contact.setIndex == (int)b2SetType.b2_awakeSet);
            b2RemoveContactFromGraph(world, bodyIdA, bodyIdB, contact.colorIndex, contact.localIndex);
        }
        else
        {
            // contact is non-touching or is sleeping or is a sensor
            Debug.Assert(contact.setIndex != (int)b2SetType.b2_awakeSet || (contact.flags & (uint)b2ContactFlags.b2_contactTouchingFlag) == 0);
            b2SolverSet set = Array_Get(world.solverSets, contact.setIndex);
            int movedIndex = Array_RemoveSwap(set.contactSims, contact.localIndex);
            if (movedIndex != B2_NULL_INDEX)
            {
                b2ContactSim movedContactSim = set.contactSims.data[contact.localIndex];
                b2Contact movedContact = Array_Get(world.contacts, movedContactSim.contactId);
                movedContact.localIndex = contact.localIndex;
            }
        }

        contact.contactId = B2_NULL_INDEX;
        contact.setIndex = B2_NULL_INDEX;
        contact.colorIndex = B2_NULL_INDEX;
        contact.localIndex = B2_NULL_INDEX;

        b2FreeId(world.contactIdPool, contactId);

        if (wakeBodies)
        {
            b2WakeBody(world, bodyA);
            b2WakeBody(world, bodyB);
        }
    }

    public static b2ContactSim b2GetContactSim(b2World world, b2Contact contact)
    {
        if (contact.setIndex == (int)b2SetType.b2_awakeSet && contact.colorIndex != B2_NULL_INDEX)
        {
            // contact lives in constraint graph
            Debug.Assert(0 <= contact.colorIndex && contact.colorIndex < B2_GRAPH_COLOR_COUNT);
            b2GraphColor color = world.constraintGraph.colors[contact.colorIndex];
            return Array_Get(color.contactSims, contact.localIndex);
        }

        b2SolverSet set = Array_Get(world.solverSets, contact.setIndex);
        return Array_Get(set.contactSims, contact.localIndex);
    }


// Update the contact manifold and touching status. Also updates sensor overlap.
// Note: do not assume the shape AABBs are overlapping or are valid.
    public static bool b2UpdateContact(b2World world, b2ContactSim contactSim, b2Shape shapeA, b2Transform transformA, b2Vec2 centerOffsetA,
        b2Shape shapeB, b2Transform transformB, b2Vec2 centerOffsetB)
    {
        // Save old manifold
        b2Manifold oldManifold = contactSim.manifold;

        // Compute new manifold
        b2ManifoldFcn fcn = s_registers[(int)shapeA.type, (int)shapeB.type].fcn;
        contactSim.manifold = fcn(shapeA, transformA, shapeB, transformB, contactSim.cache);

        // Keep these updated in case the values on the shapes are modified
        contactSim.friction = world.frictionCallback(shapeA.friction, shapeA.material, shapeB.friction, shapeB.material);
        contactSim.restitution = world.restitutionCallback(shapeA.restitution, shapeA.material, shapeB.restitution, shapeB.material);

        // todo branch improves perf?
        if (shapeA.rollingResistance > 0.0f || shapeB.rollingResistance > 0.0f)
        {
            float radiusA = b2GetShapeRadius(shapeA);
            float radiusB = b2GetShapeRadius(shapeB);
            float maxRadius = b2MaxFloat(radiusA, radiusB);
            contactSim.rollingResistance = b2MaxFloat(shapeA.rollingResistance, shapeB.rollingResistance) * maxRadius;
        }
        else
        {
            contactSim.rollingResistance = 0.0f;
        }

        contactSim.tangentSpeed = shapeA.tangentSpeed + shapeB.tangentSpeed;

        int pointCount = contactSim.manifold.pointCount;
        bool touching = pointCount > 0;

        if (touching && null != world.preSolveFcn && (contactSim.simFlags & (uint)b2ContactSimFlags.b2_simEnablePreSolveEvents) != 0)
        {
            b2ShapeId shapeIdA = new b2ShapeId(shapeA.id + 1, world.worldId, shapeA.generation);
            b2ShapeId shapeIdB = new b2ShapeId(shapeB.id + 1, world.worldId, shapeB.generation);

            // this call assumes thread safety
            touching = world.preSolveFcn(shapeIdA, shapeIdB, contactSim.manifold, world.preSolveContext);
            if (touching == false)
            {
                // disable contact
                pointCount = 0;
                contactSim.manifold.pointCount = 0;
            }
        }

        // This flag is for testing
        if (world.enableSpeculative == false && pointCount == 2)
        {
            if (contactSim.manifold.points[0].separation > 1.5f * B2_LINEAR_SLOP)
            {
                contactSim.manifold.points[0] = contactSim.manifold.points[1];
                contactSim.manifold.pointCount = 1;
            }
            else if (contactSim.manifold.points[0].separation > 1.5f * B2_LINEAR_SLOP)
            {
                contactSim.manifold.pointCount = 1;
            }

            pointCount = contactSim.manifold.pointCount;
        }

        if (touching && (shapeA.enableHitEvents || shapeB.enableHitEvents))
        {
            contactSim.simFlags |= (uint)b2ContactSimFlags.b2_simEnableHitEvent;
        }
        else
        {
            contactSim.simFlags &= ~(uint)b2ContactSimFlags.b2_simEnableHitEvent;
        }

        if (pointCount > 0)
        {
            contactSim.manifold.rollingImpulse = oldManifold.rollingImpulse;
        }

        // Match old contact ids to new contact ids and copy the
        // stored impulses to warm start the solver.
        int unmatchedCount = 0;
        for (int i = 0; i < pointCount; ++i)
        {
            b2ManifoldPoint mp2 = contactSim.manifold.points[i];

            // shift anchors to be center of mass relative
            mp2.anchorA = b2Sub(mp2.anchorA, centerOffsetA);
            mp2.anchorB = b2Sub(mp2.anchorB, centerOffsetB);

            mp2.normalImpulse = 0.0f;
            mp2.tangentImpulse = 0.0f;
            mp2.maxNormalImpulse = 0.0f;
            mp2.normalVelocity = 0.0f;
            mp2.persisted = false;

            ushort id2 = mp2.id;

            for (int j = 0; j < oldManifold.pointCount; ++j)
            {
                b2ManifoldPoint mp1 = oldManifold.points[j];

                if (mp1.id == id2)
                {
                    mp2.normalImpulse = mp1.normalImpulse;
                    mp2.tangentImpulse = mp1.tangentImpulse;
                    mp2.persisted = true;

                    // clear old impulse
                    mp1.normalImpulse = 0.0f;
                    mp1.tangentImpulse = 0.0f;
                    break;
                }
            }

            unmatchedCount += mp2.persisted ? 0 : 1;
        }

        B2_UNUSED(unmatchedCount);

#if ZERO_DEFINE
		// todo I haven't found an improvement from this yet
		// If there are unmatched new contact points, apply any left over old impulse.
		if (unmatchedCount > 0)
		{
			float unmatchedNormalImpulse = 0.0f;
			float unmatchedTangentImpulse = 0.0f;
			for (int i = 0; i < oldManifold.pointCount; ++i)
			{
				b2ManifoldPoint* mp = oldManifold.points + i;
				unmatchedNormalImpulse += mp.normalImpulse;
				unmatchedTangentImpulse += mp.tangentImpulse;
			}

			float inverse = 1.0f / unmatchedCount;
			unmatchedNormalImpulse *= inverse;
			unmatchedTangentImpulse *= inverse;

			for ( int i = 0; i < pointCount; ++i )
			{
				b2ManifoldPoint* mp2 = contactSim.manifold.points + i;

				if (mp2.persisted)
				{
					continue;
				}

				mp2.normalImpulse = unmatchedNormalImpulse;
				mp2.tangentImpulse = unmatchedTangentImpulse;
			}
		}
#endif

        if (touching)
        {
            contactSim.simFlags |= (uint)b2ContactSimFlags.b2_simTouchingFlag;
        }
        else
        {
            contactSim.simFlags &= ~(uint)b2ContactSimFlags.b2_simTouchingFlag;
        }

        return touching;
    }

    public static b2Manifold b2ComputeManifold(b2Shape shapeA, b2Transform transformA, b2Shape shapeB, b2Transform transformB)
    {
        b2ManifoldFcn fcn = s_registers[(int)shapeA.type, (int)shapeB.type].fcn;
        b2SimplexCache cache = new b2SimplexCache();
        return fcn(shapeA, transformA, shapeB, transformB, cache);
    }
}