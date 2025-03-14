// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using static Box2D.NET.B2Tables;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Cores;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2IdPools;
using static Box2D.NET.B2Manifolds;
using static Box2D.NET.B2Collisions;
using static Box2D.NET.B2Islands;
using static Box2D.NET.B2ConstraintGraphs;


namespace Box2D.NET
{
    public static class B2Contacts
    {
        public static bool b2ShouldShapesCollide(B2Filter filterA, B2Filter filterB)
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
        public delegate B2Manifold b2ManifoldFcn(B2Shape shapeA, B2Transform xfA, B2Shape shapeB, B2Transform xfB, ref B2SimplexCache cache);


        public static B2ContactRegister[,] s_registers = new B2ContactRegister[(int)B2ShapeType.b2_shapeTypeCount, (int)B2ShapeType.b2_shapeTypeCount];
        public static bool s_initialized = false;

        public static B2Manifold b2CircleManifold(B2Shape shapeA, B2Transform xfA, B2Shape shapeB, B2Transform xfB, ref B2SimplexCache cache)
        {
            B2_UNUSED(cache);
            return b2CollideCircles(shapeA.circle, xfA, shapeB.circle, xfB);
        }

        public static B2Manifold b2CapsuleAndCircleManifold(B2Shape shapeA, B2Transform xfA, B2Shape shapeB, B2Transform xfB, ref B2SimplexCache cache)
        {
            B2_UNUSED(cache);
            return b2CollideCapsuleAndCircle(shapeA.capsule, xfA, shapeB.circle, xfB);
        }

        public static B2Manifold b2CapsuleManifold(B2Shape shapeA, B2Transform xfA, B2Shape shapeB, B2Transform xfB, ref B2SimplexCache cache)
        {
            B2_UNUSED(cache);
            return b2CollideCapsules(shapeA.capsule, xfA, shapeB.capsule, xfB);
        }

        public static B2Manifold b2PolygonAndCircleManifold(B2Shape shapeA, B2Transform xfA, B2Shape shapeB, B2Transform xfB, ref B2SimplexCache cache)
        {
            B2_UNUSED(cache);
            return b2CollidePolygonAndCircle(shapeA.polygon, xfA, shapeB.circle, xfB);
        }

        public static B2Manifold b2PolygonAndCapsuleManifold(B2Shape shapeA, B2Transform xfA, B2Shape shapeB, B2Transform xfB, ref B2SimplexCache cache)
        {
            B2_UNUSED(cache);
            return b2CollidePolygonAndCapsule(shapeA.polygon, xfA, shapeB.capsule, xfB);
        }

        public static B2Manifold b2PolygonManifold(B2Shape shapeA, B2Transform xfA, B2Shape shapeB, B2Transform xfB, ref B2SimplexCache cache)
        {
            B2_UNUSED(cache);
            return b2CollidePolygons(shapeA.polygon, xfA, shapeB.polygon, xfB);
        }

        public static B2Manifold b2SegmentAndCircleManifold(B2Shape shapeA, B2Transform xfA, B2Shape shapeB, B2Transform xfB, ref B2SimplexCache cache)
        {
            B2_UNUSED(cache);
            return b2CollideSegmentAndCircle(shapeA.segment, xfA, shapeB.circle, xfB);
        }

        public static B2Manifold b2SegmentAndCapsuleManifold(B2Shape shapeA, B2Transform xfA, B2Shape shapeB, B2Transform xfB, ref B2SimplexCache cache)
        {
            B2_UNUSED(cache);
            return b2CollideSegmentAndCapsule(shapeA.segment, xfA, shapeB.capsule, xfB);
        }

        public static B2Manifold b2SegmentAndPolygonManifold(B2Shape shapeA, B2Transform xfA, B2Shape shapeB, B2Transform xfB, ref B2SimplexCache cache)
        {
            B2_UNUSED(cache);
            return b2CollideSegmentAndPolygon(shapeA.segment, xfA, shapeB.polygon, xfB);
        }

        public static B2Manifold b2ChainSegmentAndCircleManifold(B2Shape shapeA, B2Transform xfA, B2Shape shapeB, B2Transform xfB, ref B2SimplexCache cache)
        {
            B2_UNUSED(cache);
            return b2CollideChainSegmentAndCircle(shapeA.chainSegment, xfA, shapeB.circle, xfB);
        }

        public static B2Manifold b2ChainSegmentAndCapsuleManifold(B2Shape shapeA, B2Transform xfA, B2Shape shapeB, B2Transform xfB, ref B2SimplexCache cache)
        {
            return b2CollideChainSegmentAndCapsule(shapeA.chainSegment, xfA, shapeB.capsule, xfB, ref cache);
        }

        public static B2Manifold b2ChainSegmentAndPolygonManifold(B2Shape shapeA, B2Transform xfA, B2Shape shapeB, B2Transform xfB, ref B2SimplexCache cache)
        {
            return b2CollideChainSegmentAndPolygon(shapeA.chainSegment, xfA, shapeB.polygon, xfB, ref cache);
        }

        public static void b2AddType(b2ManifoldFcn fcn, B2ShapeType type1, B2ShapeType type2)
        {
            Debug.Assert(0 <= type1 && type1 < B2ShapeType.b2_shapeTypeCount);
            Debug.Assert(0 <= type2 && type2 < B2ShapeType.b2_shapeTypeCount);

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
                b2AddType(b2CircleManifold, B2ShapeType.b2_circleShape, B2ShapeType.b2_circleShape);
                b2AddType(b2CapsuleAndCircleManifold, B2ShapeType.b2_capsuleShape, B2ShapeType.b2_circleShape);
                b2AddType(b2CapsuleManifold, B2ShapeType.b2_capsuleShape, B2ShapeType.b2_capsuleShape);
                b2AddType(b2PolygonAndCircleManifold, B2ShapeType.b2_polygonShape, B2ShapeType.b2_circleShape);
                b2AddType(b2PolygonAndCapsuleManifold, B2ShapeType.b2_polygonShape, B2ShapeType.b2_capsuleShape);
                b2AddType(b2PolygonManifold, B2ShapeType.b2_polygonShape, B2ShapeType.b2_polygonShape);
                b2AddType(b2SegmentAndCircleManifold, B2ShapeType.b2_segmentShape, B2ShapeType.b2_circleShape);
                b2AddType(b2SegmentAndCapsuleManifold, B2ShapeType.b2_segmentShape, B2ShapeType.b2_capsuleShape);
                b2AddType(b2SegmentAndPolygonManifold, B2ShapeType.b2_segmentShape, B2ShapeType.b2_polygonShape);
                b2AddType(b2ChainSegmentAndCircleManifold, B2ShapeType.b2_chainSegmentShape, B2ShapeType.b2_circleShape);
                b2AddType(b2ChainSegmentAndCapsuleManifold, B2ShapeType.b2_chainSegmentShape, B2ShapeType.b2_capsuleShape);
                b2AddType(b2ChainSegmentAndPolygonManifold, B2ShapeType.b2_chainSegmentShape, B2ShapeType.b2_polygonShape);
                s_initialized = true;
            }
        }

        public static void b2CreateContact(B2World world, B2Shape shapeA, B2Shape shapeB)
        {
            B2ShapeType type1 = shapeA.type;
            B2ShapeType type2 = shapeB.type;

            Debug.Assert(0 <= type1 && type1 < B2ShapeType.b2_shapeTypeCount);
            Debug.Assert(0 <= type2 && type2 < B2ShapeType.b2_shapeTypeCount);

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

            B2Body bodyA = b2Array_Get(ref world.bodies, shapeA.bodyId);
            B2Body bodyB = b2Array_Get(ref world.bodies, shapeB.bodyId);

            Debug.Assert(bodyA.setIndex != (int)B2SetType.b2_disabledSet && bodyB.setIndex != (int)B2SetType.b2_disabledSet);
            Debug.Assert(bodyA.setIndex != (int)B2SetType.b2_staticSet || bodyB.setIndex != (int)B2SetType.b2_staticSet);

            int setIndex;
            if (bodyA.setIndex == (int)B2SetType.b2_awakeSet || bodyB.setIndex == (int)B2SetType.b2_awakeSet)
            {
                setIndex = (int)B2SetType.b2_awakeSet;
            }
            else
            {
                // sleeping and non-touching contacts live in the disabled set
                // later if this set is found to be touching then the sleeping
                // islands will be linked and the contact moved to the merged island
                setIndex = (int)B2SetType.b2_disabledSet;
            }

            B2SolverSet set = b2Array_Get(ref world.solverSets, setIndex);

            // Create contact key and contact
            int contactId = b2AllocId(world.contactIdPool);
            if (contactId == world.contacts.count)
            {
                b2Array_Push(ref world.contacts, new B2Contact());
            }

            int shapeIdA = shapeA.id;
            int shapeIdB = shapeB.id;

            B2Contact contact = b2Array_Get(ref world.contacts, contactId);
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
                contact.flags |= (uint)B2ContactFlags.b2_contactEnableContactEvents;
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
                    B2Contact headContact = b2Array_Get(ref world.contacts, headContactKey >> 1);
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
                    B2Contact headContact = b2Array_Get(ref world.contacts, headContactKey >> 1);
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
            ref B2ContactSim contactSim = ref b2Array_Add(ref set.contactSims);
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
            contactSim.manifold = new B2Manifold();

            // These also get updated in the narrow phase
            contactSim.friction = world.frictionCallback(shapeA.friction, shapeA.material, shapeB.friction, shapeB.material);
            contactSim.restitution = world.restitutionCallback(shapeA.restitution, shapeA.material, shapeB.restitution, shapeB.material);

            contactSim.tangentSpeed = 0.0f;
            contactSim.simFlags = 0;

            if (shapeA.enablePreSolveEvents || shapeB.enablePreSolveEvents)
            {
                contactSim.simFlags |= (uint)B2ContactSimFlags.b2_simEnablePreSolveEvents;
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
        public static void b2DestroyContact(B2World world, B2Contact contact, bool wakeBodies)
        {
            // Remove pair from set
            ulong pairKey = B2_SHAPE_PAIR_KEY(contact.shapeIdA, contact.shapeIdB);
            b2RemoveKey(world.broadPhase.pairSet, pairKey);

            ref B2ContactEdge edgeA = ref contact.edges[0];
            ref B2ContactEdge edgeB = ref contact.edges[1];

            int bodyIdA = edgeA.bodyId;
            int bodyIdB = edgeB.bodyId;
            B2Body bodyA = b2Array_Get(ref world.bodies, bodyIdA);
            B2Body bodyB = b2Array_Get(ref world.bodies, bodyIdB);

            uint flags = contact.flags;

            // End touch event
            if ((flags & (uint)B2ContactFlags.b2_contactTouchingFlag) != 0 && (flags & (uint)B2ContactFlags.b2_contactEnableContactEvents) != 0)
            {
                ushort worldId = world.worldId;
                B2Shape shapeA = b2Array_Get(ref world.shapes, contact.shapeIdA);
                B2Shape shapeB = b2Array_Get(ref world.shapes, contact.shapeIdB);
                B2ShapeId shapeIdA = new B2ShapeId(shapeA.id + 1, worldId, shapeA.generation);
                B2ShapeId shapeIdB = new B2ShapeId(shapeB.id + 1, worldId, shapeB.generation);

                B2ContactEndTouchEvent @event = new B2ContactEndTouchEvent(shapeIdA, shapeIdB);
                b2Array_Push(ref world.contactEndEvents[world.endEventArrayIndex], @event);
            }

            // Remove from body A
            if (edgeA.prevKey != B2_NULL_INDEX)
            {
                B2Contact prevContact = b2Array_Get(ref world.contacts, edgeA.prevKey >> 1);
                ref B2ContactEdge prevEdge = ref prevContact.edges[(edgeA.prevKey & 1)];
                prevEdge.nextKey = edgeA.nextKey;
            }

            if (edgeA.nextKey != B2_NULL_INDEX)
            {
                B2Contact nextContact = b2Array_Get(ref world.contacts, edgeA.nextKey >> 1);
                ref B2ContactEdge nextEdge = ref nextContact.edges[(edgeA.nextKey & 1)];
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
                B2Contact prevContact = b2Array_Get(ref world.contacts, edgeB.prevKey >> 1);
                ref B2ContactEdge prevEdge = ref prevContact.edges[(edgeB.prevKey & 1)];
                prevEdge.nextKey = edgeB.nextKey;
            }

            if (edgeB.nextKey != B2_NULL_INDEX)
            {
                B2Contact nextContact = b2Array_Get(ref world.contacts, edgeB.nextKey >> 1);
                ref B2ContactEdge nextEdge = ref nextContact.edges[(edgeB.nextKey & 1)];
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
                Debug.Assert(contact.setIndex == (int)B2SetType.b2_awakeSet);
                b2RemoveContactFromGraph(world, bodyIdA, bodyIdB, contact.colorIndex, contact.localIndex);
            }
            else
            {
                // contact is non-touching or is sleeping or is a sensor
                Debug.Assert(contact.setIndex != (int)B2SetType.b2_awakeSet || (contact.flags & (uint)B2ContactFlags.b2_contactTouchingFlag) == 0);
                B2SolverSet set = b2Array_Get(ref world.solverSets, contact.setIndex);
                int movedIndex = b2Array_RemoveSwap(ref set.contactSims, contact.localIndex);
                if (movedIndex != B2_NULL_INDEX)
                {
                    B2ContactSim movedContactSim = set.contactSims.data[contact.localIndex];
                    B2Contact movedContact = b2Array_Get(ref world.contacts, movedContactSim.contactId);
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

        public static B2ContactSim b2GetContactSim(B2World world, B2Contact contact)
        {
            if (contact.setIndex == (int)B2SetType.b2_awakeSet && contact.colorIndex != B2_NULL_INDEX)
            {
                // contact lives in constraint graph
                Debug.Assert(0 <= contact.colorIndex && contact.colorIndex < B2_GRAPH_COLOR_COUNT);
                ref B2GraphColor color = ref world.constraintGraph.colors[contact.colorIndex];
                return b2Array_Get(ref color.contactSims, contact.localIndex);
            }

            B2SolverSet set = b2Array_Get(ref world.solverSets, contact.setIndex);
            return b2Array_Get(ref set.contactSims, contact.localIndex);
        }


        // Update the contact manifold and touching status. Also updates sensor overlap.
        // Note: do not assume the shape AABBs are overlapping or are valid.
        public static bool b2UpdateContact(B2World world, B2ContactSim contactSim, B2Shape shapeA, B2Transform transformA, B2Vec2 centerOffsetA,
            B2Shape shapeB, B2Transform transformB, B2Vec2 centerOffsetB)
        {
            // Save old manifold
            B2Manifold oldManifold = contactSim.manifold;

            // Compute new manifold
            b2ManifoldFcn fcn = s_registers[(int)shapeA.type, (int)shapeB.type].fcn;
            contactSim.manifold = fcn(shapeA, transformA, shapeB, transformB, ref contactSim.cache);

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

            if (touching && null != world.preSolveFcn && (contactSim.simFlags & (uint)B2ContactSimFlags.b2_simEnablePreSolveEvents) != 0)
            {
                B2ShapeId shapeIdA = new B2ShapeId(shapeA.id + 1, world.worldId, shapeA.generation);
                B2ShapeId shapeIdB = new B2ShapeId(shapeB.id + 1, world.worldId, shapeB.generation);

                // this call assumes thread safety
                touching = world.preSolveFcn(shapeIdA, shapeIdB, ref contactSim.manifold, world.preSolveContext);
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
                contactSim.simFlags |= (uint)B2ContactSimFlags.b2_simEnableHitEvent;
            }
            else
            {
                contactSim.simFlags &= ~(uint)B2ContactSimFlags.b2_simEnableHitEvent;
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
                ref B2ManifoldPoint mp2 = ref contactSim.manifold.points[i];

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
                    ref B2ManifoldPoint mp1 = ref oldManifold.points[j];

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

#if FALSE
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
                contactSim.simFlags |= (uint)B2ContactSimFlags.b2_simTouchingFlag;
            }
            else
            {
                contactSim.simFlags &= ~(uint)B2ContactSimFlags.b2_simTouchingFlag;
            }

            return touching;
        }

        public static B2Manifold b2ComputeManifold(B2Shape shapeA, B2Transform transformA, B2Shape shapeB, B2Transform transformB)
        {
            b2ManifoldFcn fcn = s_registers[(int)shapeA.type, (int)shapeB.type].fcn;
            B2SimplexCache cache = new B2SimplexCache();
            return fcn(shapeA, transformA, shapeB, transformB, ref cache);
        }
    }
}
