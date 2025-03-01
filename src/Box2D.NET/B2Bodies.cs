﻿// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Cores;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Contacts;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2IdPools;
using static Box2D.NET.B2Islands;
using static Box2D.NET.B2Sensors;
using static Box2D.NET.B2SolverSets;
using static Box2D.NET.B2BoardPhases;

namespace Box2D.NET
{
    public static class B2Bodies
    {
        // Identity body state, notice the deltaRotation is {1, 0}
        public static readonly B2BodyState b2_identityBodyState = new B2BodyState()
        {
            linearVelocity = new B2Vec2(0.0f, 0.0f),
            angularVelocity = 0.0f,
            flags = 0,
            deltaPosition = new B2Vec2(0.0f, 0.0f),
            deltaRotation = new B2Rot(1.0f, 0.0f),
        };

        public static B2Sweep b2MakeSweep(B2BodySim bodySim)
        {
            B2Sweep s = new B2Sweep();
            s.c1 = bodySim.center0;
            s.c2 = bodySim.center;
            s.q1 = bodySim.rotation0;
            s.q2 = bodySim.transform.q;
            s.localCenter = bodySim.localCenter;
            return s;
        }

        // Get a validated body from a world using an id.
        public static B2Body b2GetBodyFullId(B2World world, B2BodyId bodyId)
        {
            Debug.Assert(b2Body_IsValid(bodyId));

            // id index starts at one so that zero can represent null
            return b2Array_Get(ref world.bodies, bodyId.index1 - 1);
        }

        public static B2Transform b2GetBodyTransformQuick(B2World world, B2Body body)
        {
            B2SolverSet set = b2Array_Get(ref world.solverSets, body.setIndex);
            B2BodySim bodySim = b2Array_Get(ref set.bodySims, body.localIndex);
            return bodySim.transform;
        }

        public static B2Transform b2GetBodyTransform(B2World world, int bodyId)
        {
            B2Body body = b2Array_Get(ref world.bodies, bodyId);
            return b2GetBodyTransformQuick(world, body);
        }

        // Create a b2BodyId from a raw id.
        public static B2BodyId b2MakeBodyId(B2World world, int bodyId)
        {
            B2Body body = b2Array_Get(ref world.bodies, bodyId);
            return new B2BodyId(bodyId + 1, world.worldId, body.generation);
        }

        public static B2BodySim b2GetBodySim(B2World world, B2Body body)
        {
            B2SolverSet set = b2Array_Get(ref world.solverSets, body.setIndex);
            B2BodySim bodySim = b2Array_Get(ref set.bodySims, body.localIndex);
            return bodySim;
        }

        public static B2BodyState b2GetBodyState(B2World world, B2Body body)
        {
            if (body.setIndex == (int)B2SetType.b2_awakeSet)
            {
                B2SolverSet set = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_awakeSet);
                return b2Array_Get(ref set.bodyStates, body.localIndex);
            }

            return null;
        }

        public static void b2CreateIslandForBody(B2World world, int setIndex, B2Body body)
        {
            Debug.Assert(body.islandId == B2_NULL_INDEX);
            Debug.Assert(body.islandPrev == B2_NULL_INDEX);
            Debug.Assert(body.islandNext == B2_NULL_INDEX);
            Debug.Assert(setIndex != (int)B2SetType.b2_disabledSet);

            B2Island island = b2CreateIsland(world, setIndex);

            body.islandId = island.islandId;
            island.headBody = body.id;
            island.tailBody = body.id;
            island.bodyCount = 1;
        }

        public static void b2RemoveBodyFromIsland(B2World world, B2Body body)
        {
            if (body.islandId == B2_NULL_INDEX)
            {
                Debug.Assert(body.islandPrev == B2_NULL_INDEX);
                Debug.Assert(body.islandNext == B2_NULL_INDEX);
                return;
            }

            int islandId = body.islandId;
            B2Island island = b2Array_Get(ref world.islands, islandId);

            // Fix the island's linked list of sims
            if (body.islandPrev != B2_NULL_INDEX)
            {
                B2Body prevBody = b2Array_Get(ref world.bodies, body.islandPrev);
                prevBody.islandNext = body.islandNext;
            }

            if (body.islandNext != B2_NULL_INDEX)
            {
                B2Body nextBody = b2Array_Get(ref world.bodies, body.islandNext);
                nextBody.islandPrev = body.islandPrev;
            }

            Debug.Assert(island.bodyCount > 0);
            island.bodyCount -= 1;
            bool islandDestroyed = false;

            if (island.headBody == body.id)
            {
                island.headBody = body.islandNext;

                if (island.headBody == B2_NULL_INDEX)
                {
                    // Destroy empty island
                    Debug.Assert(island.tailBody == body.id);
                    Debug.Assert(island.bodyCount == 0);
                    Debug.Assert(island.contactCount == 0);
                    Debug.Assert(island.jointCount == 0);

                    // Free the island
                    b2DestroyIsland(world, island.islandId);
                    islandDestroyed = true;
                }
            }
            else if (island.tailBody == body.id)
            {
                island.tailBody = body.islandPrev;
            }

            if (islandDestroyed == false)
            {
                b2ValidateIsland(world, islandId);
            }

            body.islandId = B2_NULL_INDEX;
            body.islandPrev = B2_NULL_INDEX;
            body.islandNext = B2_NULL_INDEX;
        }

        public static void b2DestroyBodyContacts(B2World world, B2Body body, bool wakeBodies)
        {
            // Destroy the attached contacts
            int edgeKey = body.headContactKey;
            while (edgeKey != B2_NULL_INDEX)
            {
                int contactId = edgeKey >> 1;
                int edgeIndex = edgeKey & 1;

                B2Contact contact = b2Array_Get(ref world.contacts, contactId);
                edgeKey = contact.edges[edgeIndex].nextKey;
                b2DestroyContact(world, contact, wakeBodies);
            }

            b2ValidateSolverSets(world);
        }

        public static B2BodyId b2CreateBody(B2WorldId worldId, B2BodyDef def)
        {
            B2_CHECK_DEF(ref def);
            Debug.Assert(b2IsValidVec2(def.position));
            Debug.Assert(b2IsValidRotation(def.rotation));
            Debug.Assert(b2IsValidVec2(def.linearVelocity));
            Debug.Assert(b2IsValidFloat(def.angularVelocity));
            Debug.Assert(b2IsValidFloat(def.linearDamping) && def.linearDamping >= 0.0f);
            Debug.Assert(b2IsValidFloat(def.angularDamping) && def.angularDamping >= 0.0f);
            Debug.Assert(b2IsValidFloat(def.sleepThreshold) && def.sleepThreshold >= 0.0f);
            Debug.Assert(b2IsValidFloat(def.gravityScale));

            B2World world = b2GetWorldFromId(worldId);
            Debug.Assert(world.locked == false);

            if (world.locked)
            {
                return b2_nullBodyId;
            }

            bool isAwake = (def.isAwake || def.enableSleep == false) && def.isEnabled;

            // determine the solver set
            int setId;
            if (def.isEnabled == false)
            {
                // any body type can be disabled
                setId = (int)B2SetType.b2_disabledSet;
            }
            else if (def.type == B2BodyType.b2_staticBody)
            {
                setId = (int)B2SetType.b2_staticSet;
            }
            else if (isAwake == true)
            {
                setId = (int)B2SetType.b2_awakeSet;
            }
            else
            {
                // new set for a sleeping body in its own island
                setId = b2AllocId(world.solverSetIdPool);
                if (setId == world.solverSets.count)
                {
                    // Create a zero initialized solver set. All sub-arrays are also zero initialized.
                    b2Array_Push(ref world.solverSets, new B2SolverSet());
                }
                else
                {
                    Debug.Assert(world.solverSets.data[setId].setIndex == B2_NULL_INDEX);
                }

                world.solverSets.data[setId].setIndex = setId;
            }

            Debug.Assert(0 <= setId && setId < world.solverSets.count);

            int bodyId = b2AllocId(world.bodyIdPool);

            B2SolverSet set = b2Array_Get(ref world.solverSets, setId);
            ref B2BodySim bodySim = ref b2Array_Add(ref set.bodySims);
            //*bodySim = ( b2BodySim ){ 0 };
            bodySim.Clear();
            bodySim.transform.p = def.position;
            bodySim.transform.q = def.rotation;
            bodySim.center = def.position;
            bodySim.rotation0 = bodySim.transform.q;
            bodySim.center0 = bodySim.center;
            bodySim.localCenter = b2Vec2_zero;
            bodySim.force = b2Vec2_zero;
            bodySim.torque = 0.0f;
            bodySim.invMass = 0.0f;
            bodySim.invInertia = 0.0f;
            bodySim.minExtent = B2_HUGE;
            bodySim.maxExtent = 0.0f;
            bodySim.linearDamping = def.linearDamping;
            bodySim.angularDamping = def.angularDamping;
            bodySim.gravityScale = def.gravityScale;
            bodySim.bodyId = bodyId;
            bodySim.isBullet = def.isBullet;
            bodySim.allowFastRotation = def.allowFastRotation;
            bodySim.enlargeAABB = false;
            bodySim.isFast = false;
            bodySim.isSpeedCapped = false;

            if (setId == (int)B2SetType.b2_awakeSet)
            {
                ref B2BodyState bodyState = ref b2Array_Add(ref set.bodyStates);
                //Debug.Assert( ( (uintptr_t)bodyState & 0x1F ) == 0 );
                //*bodyState = ( b2BodyState ){ 0 }; 
                bodyState.Clear();
                bodyState.linearVelocity = def.linearVelocity;
                bodyState.angularVelocity = def.angularVelocity;
                bodyState.deltaRotation = b2Rot_identity;
            }

            if (bodyId == world.bodies.count)
            {
                b2Array_Push(ref world.bodies, new B2Body());
            }
            else
            {
                Debug.Assert(world.bodies.data[bodyId].id == B2_NULL_INDEX);
            }

            B2Body body = b2Array_Get(ref world.bodies, bodyId);

            if (!string.IsNullOrEmpty(def.name))
            {
                body.name = def.name;
            }
            else
            {
                body.name = "";
            }

            body.userData = def.userData;
            body.setIndex = setId;
            body.localIndex = set.bodySims.count - 1;
            body.generation += 1;
            body.headShapeId = B2_NULL_INDEX;
            body.shapeCount = 0;
            body.headChainId = B2_NULL_INDEX;
            body.headContactKey = B2_NULL_INDEX;
            body.contactCount = 0;
            body.headJointKey = B2_NULL_INDEX;
            body.jointCount = 0;
            body.islandId = B2_NULL_INDEX;
            body.islandPrev = B2_NULL_INDEX;
            body.islandNext = B2_NULL_INDEX;
            body.bodyMoveIndex = B2_NULL_INDEX;
            body.id = bodyId;
            body.mass = 0.0f;
            body.inertia = 0.0f;
            body.sleepThreshold = def.sleepThreshold;
            body.sleepTime = 0.0f;
            body.type = def.type;
            body.enableSleep = def.enableSleep;
            body.fixedRotation = def.fixedRotation;
            body.isSpeedCapped = false;
            body.isMarked = false;

            // dynamic and kinematic bodies that are enabled need a island
            if (setId >= (int)B2SetType.b2_awakeSet)
            {
                b2CreateIslandForBody(world, setId, body);
            }

            b2ValidateSolverSets(world);

            B2BodyId id = new B2BodyId(bodyId + 1, world.worldId, body.generation);
            return id;
        }

        public static bool b2IsBodyAwake(B2World world, B2Body body)
        {
            B2_UNUSED(world);
            return body.setIndex == (int)B2SetType.b2_awakeSet;
        }

        // careful calling this because it can invalidate body, state, joint, and contact pointers
        public static bool b2WakeBody(B2World world, B2Body body)
        {
            if (body.setIndex >= (int)B2SetType.b2_firstSleepingSet)
            {
                b2WakeSolverSet(world, body.setIndex);
                return true;
            }

            return false;
        }

        public static void b2DestroyBody(B2BodyId bodyId)
        {
            B2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return;
            }

            B2Body body = b2GetBodyFullId(world, bodyId);

            // Wake bodies attached to this body, even if this body is static.
            bool wakeBodies = true;

            // Destroy the attached joints
            int edgeKey = body.headJointKey;
            while (edgeKey != B2_NULL_INDEX)
            {
                int jointId = edgeKey >> 1;
                int edgeIndex = edgeKey & 1;

                B2Joint joint = b2Array_Get(ref world.joints, jointId);
                edgeKey = joint.edges[edgeIndex].nextKey;

                // Careful because this modifies the list being traversed
                b2DestroyJointInternal(world, joint, wakeBodies);
            }

            // Destroy all contacts attached to this body.
            b2DestroyBodyContacts(world, body, wakeBodies);

            // Destroy the attached shapes and their broad-phase proxies.
            int shapeId = body.headShapeId;
            while (shapeId != B2_NULL_INDEX)
            {
                B2Shape shape = b2Array_Get(ref world.shapes, shapeId);

                if (shape.sensorIndex != B2_NULL_INDEX)
                {
                    b2DestroySensor(world, shape);
                }

                b2DestroyShapeProxy(shape, world.broadPhase);

                // Return shape to free list.
                b2FreeId(world.shapeIdPool, shapeId);
                shape.id = B2_NULL_INDEX;

                shapeId = shape.nextShapeId;
            }

            // Destroy the attached chains. The associated shapes have already been destroyed above.
            int chainId = body.headChainId;
            while (chainId != B2_NULL_INDEX)
            {
                B2ChainShape chain = b2Array_Get(ref world.chainShapes, chainId);

                b2FreeChainData(chain);

                // Return chain to free list.
                b2FreeId(world.chainIdPool, chainId);
                chain.id = B2_NULL_INDEX;

                chainId = chain.nextChainId;
            }

            b2RemoveBodyFromIsland(world, body);

            // Remove body sim from solver set that owns it
            B2SolverSet set = b2Array_Get(ref world.solverSets, body.setIndex);
            int movedIndex = b2Array_RemoveSwap(ref set.bodySims, body.localIndex);
            if (movedIndex != B2_NULL_INDEX)
            {
                // Fix moved body index
                B2BodySim movedSim = set.bodySims.data[body.localIndex];
                int movedId = movedSim.bodyId;
                B2Body movedBody = b2Array_Get(ref world.bodies, movedId);
                Debug.Assert(movedBody.localIndex == movedIndex);
                movedBody.localIndex = body.localIndex;
            }

            // Remove body state from awake set
            if (body.setIndex == (int)B2SetType.b2_awakeSet)
            {
                int result = b2Array_RemoveSwap(ref set.bodyStates, body.localIndex);
                B2_UNUSED(result);
                Debug.Assert(result == movedIndex);
            }
            else if (set.setIndex >= (int)B2SetType.b2_firstSleepingSet && set.bodySims.count == 0)
            {
                // Remove solver set if it's now an orphan.
                b2DestroySolverSet(world, set.setIndex);
            }

            // Free body and id (preserve body generation)
            b2FreeId(world.bodyIdPool, body.id);

            body.setIndex = B2_NULL_INDEX;
            body.localIndex = B2_NULL_INDEX;
            body.id = B2_NULL_INDEX;

            b2ValidateSolverSets(world);
        }

        public static int b2Body_GetContactCapacity(B2BodyId bodyId)
        {
            B2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return 0;
            }

            B2Body body = b2GetBodyFullId(world, bodyId);

            // Conservative and fast
            return body.contactCount;
        }

        // todo what about sensors?
        // todo sample needed
        public static int b2Body_GetContactData(B2BodyId bodyId, Span<B2ContactData> contactData, int capacity)
        {
            B2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return 0;
            }

            B2Body body = b2GetBodyFullId(world, bodyId);

            int contactKey = body.headContactKey;
            int index = 0;
            while (contactKey != B2_NULL_INDEX && index < capacity)
            {
                int contactId = contactKey >> 1;
                int edgeIndex = contactKey & 1;

                B2Contact contact = b2Array_Get(ref world.contacts, contactId);

                // Is contact touching?
                if (0 != (contact.flags & (uint)B2ContactFlags.b2_contactTouchingFlag))
                {
                    B2Shape shapeA = b2Array_Get(ref world.shapes, contact.shapeIdA);
                    B2Shape shapeB = b2Array_Get(ref world.shapes, contact.shapeIdB);

                    contactData[index].shapeIdA = new B2ShapeId(shapeA.id + 1, bodyId.world0, shapeA.generation);
                    contactData[index].shapeIdB = new B2ShapeId(shapeB.id + 1, bodyId.world0, shapeB.generation);

                    B2ContactSim contactSim = b2GetContactSim(world, contact);
                    contactData[index].manifold = contactSim.manifold;

                    index += 1;
                }

                contactKey = contact.edges[edgeIndex].nextKey;
            }

            Debug.Assert(index <= capacity);

            return index;
        }

        public static B2AABB b2Body_ComputeAABB(B2BodyId bodyId)
        {
            B2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return new B2AABB();
            }

            B2Body body = b2GetBodyFullId(world, bodyId);
            if (body.headShapeId == B2_NULL_INDEX)
            {
                B2Transform transform = b2GetBodyTransform(world, body.id);
                return new B2AABB(transform.p, transform.p);
            }

            B2Shape shape = b2Array_Get(ref world.shapes, body.headShapeId);
            B2AABB aabb = shape.aabb;
            while (shape.nextShapeId != B2_NULL_INDEX)
            {
                shape = b2Array_Get(ref world.shapes, shape.nextShapeId);
                aabb = b2AABB_Union(aabb, shape.aabb);
            }

            return aabb;
        }

        public static void b2UpdateBodyMassData(B2World world, B2Body body)
        {
            B2BodySim bodySim = b2GetBodySim(world, body);

            // Compute mass data from shapes. Each shape has its own density.
            body.mass = 0.0f;
            body.inertia = 0.0f;

            bodySim.invMass = 0.0f;
            bodySim.invInertia = 0.0f;
            bodySim.localCenter = b2Vec2_zero;
            bodySim.minExtent = B2_HUGE;
            bodySim.maxExtent = 0.0f;

            // Static and kinematic sims have zero mass.
            if (body.type != B2BodyType.b2_dynamicBody)
            {
                bodySim.center = bodySim.transform.p;

                // Need extents for kinematic bodies for sleeping to work correctly.
                if (body.type == B2BodyType.b2_kinematicBody)
                {
                    int nextShapeId = body.headShapeId;
                    while (nextShapeId != B2_NULL_INDEX)
                    {
                        B2Shape s = b2Array_Get(ref world.shapes, nextShapeId);

                        B2ShapeExtent extent = b2ComputeShapeExtent(s, b2Vec2_zero);
                        bodySim.minExtent = b2MinFloat(bodySim.minExtent, extent.minExtent);
                        bodySim.maxExtent = b2MaxFloat(bodySim.maxExtent, extent.maxExtent);

                        nextShapeId = s.nextShapeId;
                    }
                }

                return;
            }

            // Accumulate mass over all shapes.
            B2Vec2 localCenter = b2Vec2_zero;
            int shapeId = body.headShapeId;
            while (shapeId != B2_NULL_INDEX)
            {
                B2Shape s = b2Array_Get(ref world.shapes, shapeId);
                shapeId = s.nextShapeId;

                if (s.density == 0.0f)
                {
                    continue;
                }

                B2MassData massData = b2ComputeShapeMass(s);
                body.mass += massData.mass;
                localCenter = b2MulAdd(localCenter, massData.mass, massData.center);
                body.inertia += massData.rotationalInertia;
            }

            // Compute center of mass.
            if (body.mass > 0.0f)
            {
                bodySim.invMass = 1.0f / body.mass;
                localCenter = b2MulSV(bodySim.invMass, localCenter);
            }

            if (body.inertia > 0.0f && body.fixedRotation == false)
            {
                // Center the inertia about the center of mass.
                body.inertia -= body.mass * b2Dot(localCenter, localCenter);
                Debug.Assert(body.inertia > 0.0f);
                bodySim.invInertia = 1.0f / body.inertia;
            }
            else
            {
                body.inertia = 0.0f;
                bodySim.invInertia = 0.0f;
            }

            // Move center of mass.
            B2Vec2 oldCenter = bodySim.center;
            bodySim.localCenter = localCenter;
            bodySim.center = b2TransformPoint(ref bodySim.transform, bodySim.localCenter);

            // Update center of mass velocity
            B2BodyState state = b2GetBodyState(world, body);
            if (state != null)
            {
                B2Vec2 deltaLinear = b2CrossSV(state.angularVelocity, b2Sub(bodySim.center, oldCenter));
                state.linearVelocity = b2Add(state.linearVelocity, deltaLinear);
            }

            // Compute body extents relative to center of mass
            shapeId = body.headShapeId;
            while (shapeId != B2_NULL_INDEX)
            {
                B2Shape s = b2Array_Get(ref world.shapes, shapeId);

                B2ShapeExtent extent = b2ComputeShapeExtent(s, localCenter);
                bodySim.minExtent = b2MinFloat(bodySim.minExtent, extent.minExtent);
                bodySim.maxExtent = b2MaxFloat(bodySim.maxExtent, extent.maxExtent);

                shapeId = s.nextShapeId;
            }
        }

        public static B2Vec2 b2Body_GetPosition(B2BodyId bodyId)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            B2Transform transform = b2GetBodyTransformQuick(world, body);
            return transform.p;
        }

        public static B2Rot b2Body_GetRotation(B2BodyId bodyId)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            B2Transform transform = b2GetBodyTransformQuick(world, body);
            return transform.q;
        }

        public static B2Transform b2Body_GetTransform(B2BodyId bodyId)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            return b2GetBodyTransformQuick(world, body);
        }

        public static B2Vec2 b2Body_GetLocalPoint(B2BodyId bodyId, B2Vec2 worldPoint)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            B2Transform transform = b2GetBodyTransformQuick(world, body);
            return b2InvTransformPoint(transform, worldPoint);
        }

        public static B2Vec2 b2Body_GetWorldPoint(B2BodyId bodyId, B2Vec2 localPoint)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            B2Transform transform = b2GetBodyTransformQuick(world, body);
            return b2TransformPoint(ref transform, localPoint);
        }

        public static B2Vec2 b2Body_GetLocalVector(B2BodyId bodyId, B2Vec2 worldVector)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            B2Transform transform = b2GetBodyTransformQuick(world, body);
            return b2InvRotateVector(transform.q, worldVector);
        }

        public static B2Vec2 b2Body_GetWorldVector(B2BodyId bodyId, B2Vec2 localVector)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            B2Transform transform = b2GetBodyTransformQuick(world, body);
            return b2RotateVector(transform.q, localVector);
        }

        public static void b2Body_SetTransform(B2BodyId bodyId, B2Vec2 position, B2Rot rotation)
        {
            Debug.Assert(b2IsValidVec2(position));
            Debug.Assert(b2IsValidRotation(rotation));
            Debug.Assert(b2Body_IsValid(bodyId));
            B2World world = b2GetWorld(bodyId.world0);
            Debug.Assert(world.locked == false);

            B2Body body = b2GetBodyFullId(world, bodyId);
            B2BodySim bodySim = b2GetBodySim(world, body);

            bodySim.transform.p = position;
            bodySim.transform.q = rotation;
            bodySim.center = b2TransformPoint(ref bodySim.transform, bodySim.localCenter);

            bodySim.rotation0 = bodySim.transform.q;
            bodySim.center0 = bodySim.center;

            B2BroadPhase broadPhase = world.broadPhase;

            B2Transform transform = bodySim.transform;
            float margin = B2_AABB_MARGIN;
            float speculativeDistance = B2_SPECULATIVE_DISTANCE;

            int shapeId = body.headShapeId;
            while (shapeId != B2_NULL_INDEX)
            {
                B2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                B2AABB aabb = b2ComputeShapeAABB(shape, transform);
                aabb.lowerBound.x -= speculativeDistance;
                aabb.lowerBound.y -= speculativeDistance;
                aabb.upperBound.x += speculativeDistance;
                aabb.upperBound.y += speculativeDistance;
                shape.aabb = aabb;

                if (b2AABB_Contains(shape.fatAABB, aabb) == false)
                {
                    B2AABB fatAABB;
                    fatAABB.lowerBound.x = aabb.lowerBound.x - margin;
                    fatAABB.lowerBound.y = aabb.lowerBound.y - margin;
                    fatAABB.upperBound.x = aabb.upperBound.x + margin;
                    fatAABB.upperBound.y = aabb.upperBound.y + margin;
                    shape.fatAABB = fatAABB;

                    // They body could be disabled
                    if (shape.proxyKey != B2_NULL_INDEX)
                    {
                        b2BroadPhase_MoveProxy(broadPhase, shape.proxyKey, fatAABB);
                    }
                }

                shapeId = shape.nextShapeId;
            }
        }

        public static B2Vec2 b2Body_GetLinearVelocity(B2BodyId bodyId)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            B2BodyState state = b2GetBodyState(world, body);
            if (state != null)
            {
                return state.linearVelocity;
            }

            return b2Vec2_zero;
        }

        public static float b2Body_GetAngularVelocity(B2BodyId bodyId)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            B2BodyState state = b2GetBodyState(world, body);
            if (state != null)
            {
                return state.angularVelocity;
            }

            return 0.0f;
        }

        public static void b2Body_SetLinearVelocity(B2BodyId bodyId, B2Vec2 linearVelocity)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);

            if (body.type == B2BodyType.b2_staticBody)
            {
                return;
            }

            if (b2LengthSquared(linearVelocity) > 0.0f)
            {
                b2WakeBody(world, body);
            }

            B2BodyState state = b2GetBodyState(world, body);
            if (state == null)
            {
                return;
            }

            state.linearVelocity = linearVelocity;
        }

        public static void b2Body_SetAngularVelocity(B2BodyId bodyId, float angularVelocity)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);

            if (body.type == B2BodyType.b2_staticBody || body.fixedRotation)
            {
                return;
            }

            if (angularVelocity != 0.0f)
            {
                b2WakeBody(world, body);
            }

            B2BodyState state = b2GetBodyState(world, body);
            if (state == null)
            {
                return;
            }

            state.angularVelocity = angularVelocity;
        }

        public static B2Vec2 b2Body_GetLocalPointVelocity(B2BodyId bodyId, B2Vec2 localPoint)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            B2BodyState state = b2GetBodyState(world, body);
            if (state == null)
            {
                return b2Vec2_zero;
            }

            B2SolverSet set = b2Array_Get(ref world.solverSets, body.setIndex);
            B2BodySim bodySim = b2Array_Get(ref set.bodySims, body.localIndex);

            B2Vec2 r = b2RotateVector(bodySim.transform.q, b2Sub(localPoint, bodySim.localCenter));
            B2Vec2 v = b2Add(state.linearVelocity, b2CrossSV(state.angularVelocity, r));
            return v;
        }

        public static B2Vec2 b2Body_GetWorldPointVelocity(B2BodyId bodyId, B2Vec2 worldPoint)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            B2BodyState state = b2GetBodyState(world, body);
            if (state == null)
            {
                return b2Vec2_zero;
            }

            B2SolverSet set = b2Array_Get(ref world.solverSets, body.setIndex);
            B2BodySim bodySim = b2Array_Get(ref set.bodySims, body.localIndex);

            B2Vec2 r = b2Sub(worldPoint, bodySim.center);
            B2Vec2 v = b2Add(state.linearVelocity, b2CrossSV(state.angularVelocity, r));
            return v;
        }

        public static void b2Body_ApplyForce(B2BodyId bodyId, B2Vec2 force, B2Vec2 point, bool wake)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);

            if (wake && body.setIndex >= (int)B2SetType.b2_firstSleepingSet)
            {
                b2WakeBody(world, body);
            }

            if (body.setIndex == (int)B2SetType.b2_awakeSet)
            {
                B2BodySim bodySim = b2GetBodySim(world, body);
                bodySim.force = b2Add(bodySim.force, force);
                bodySim.torque += b2Cross(b2Sub(point, bodySim.center), force);
            }
        }

        public static void b2Body_ApplyForceToCenter(B2BodyId bodyId, B2Vec2 force, bool wake)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);

            if (wake && body.setIndex >= (int)B2SetType.b2_firstSleepingSet)
            {
                b2WakeBody(world, body);
            }

            if (body.setIndex == (int)B2SetType.b2_awakeSet)
            {
                B2BodySim bodySim = b2GetBodySim(world, body);
                bodySim.force = b2Add(bodySim.force, force);
            }
        }

        public static void b2Body_ApplyTorque(B2BodyId bodyId, float torque, bool wake)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);

            if (wake && body.setIndex >= (int)B2SetType.b2_firstSleepingSet)
            {
                b2WakeBody(world, body);
            }

            if (body.setIndex == (int)B2SetType.b2_awakeSet)
            {
                B2BodySim bodySim = b2GetBodySim(world, body);
                bodySim.torque += torque;
            }
        }

        public static void b2Body_ApplyLinearImpulse(B2BodyId bodyId, B2Vec2 impulse, B2Vec2 point, bool wake)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);

            if (wake && body.setIndex >= (int)B2SetType.b2_firstSleepingSet)
            {
                b2WakeBody(world, body);
            }

            if (body.setIndex == (int)B2SetType.b2_awakeSet)
            {
                int localIndex = body.localIndex;
                B2SolverSet set = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_awakeSet);
                B2BodyState state = b2Array_Get(ref set.bodyStates, localIndex);
                B2BodySim bodySim = b2Array_Get(ref set.bodySims, localIndex);
                state.linearVelocity = b2MulAdd(state.linearVelocity, bodySim.invMass, impulse);
                state.angularVelocity += bodySim.invInertia * b2Cross(b2Sub(point, bodySim.center), impulse);
            }
        }

        public static void b2Body_ApplyLinearImpulseToCenter(B2BodyId bodyId, B2Vec2 impulse, bool wake)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);

            if (wake && body.setIndex >= (int)B2SetType.b2_firstSleepingSet)
            {
                b2WakeBody(world, body);
            }

            if (body.setIndex == (int)B2SetType.b2_awakeSet)
            {
                int localIndex = body.localIndex;
                B2SolverSet set = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_awakeSet);
                B2BodyState state = b2Array_Get(ref set.bodyStates, localIndex);
                B2BodySim bodySim = b2Array_Get(ref set.bodySims, localIndex);
                state.linearVelocity = b2MulAdd(state.linearVelocity, bodySim.invMass, impulse);
            }
        }

        public static void b2Body_ApplyAngularImpulse(B2BodyId bodyId, float impulse, bool wake)
        {
            Debug.Assert(b2Body_IsValid(bodyId));
            B2World world = b2GetWorld(bodyId.world0);

            int id = bodyId.index1 - 1;
            B2Body body = b2Array_Get(ref world.bodies, id);
            Debug.Assert(body.generation == bodyId.generation);

            if (wake && body.setIndex >= (int)B2SetType.b2_firstSleepingSet)
            {
                // this will not invalidate body pointer
                b2WakeBody(world, body);
            }

            if (body.setIndex == (int)B2SetType.b2_awakeSet)
            {
                int localIndex = body.localIndex;
                B2SolverSet set = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_awakeSet);
                B2BodyState state = b2Array_Get(ref set.bodyStates, localIndex);
                B2BodySim bodySim = b2Array_Get(ref set.bodySims, localIndex);
                state.angularVelocity += bodySim.invInertia * impulse;
            }
        }

        public static B2BodyType b2Body_GetType(B2BodyId bodyId)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            return body.type;
        }

        // Changing the body type is quite complex mainly due to joints.
        // Considerations:
        // - body and joints must be moved to the correct set
        // - islands must be updated
        // - graph coloring must be correct
        // - any body connected to a joint may be disabled
        // - joints between static bodies must go into the static set
        public static void b2Body_SetType(B2BodyId bodyId, B2BodyType type)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);

            B2BodyType originalType = body.type;
            if (originalType == type)
            {
                return;
            }

            if (body.setIndex == (int)B2SetType.b2_disabledSet)
            {
                // Disabled bodies don't change solver sets or islands when they change type.
                body.type = type;

                // Body type affects the mass
                b2UpdateBodyMassData(world, body);
                return;
            }

            // Destroy all contacts but don't wake bodies.
            bool wakeBodies = false;
            b2DestroyBodyContacts(world, body, wakeBodies);

            // Wake this body because we assume below that it is awake or static.
            b2WakeBody(world, body);

            // Unlink all joints and wake attached bodies.
            {
                int jointKey = body.headJointKey;
                while (jointKey != B2_NULL_INDEX)
                {
                    int jointId = jointKey >> 1;
                    int edgeIndex = jointKey & 1;

                    B2Joint joint = b2Array_Get(ref world.joints, jointId);
                    if (joint.islandId != B2_NULL_INDEX)
                    {
                        b2UnlinkJoint(world, joint);
                    }

                    // A body going from static to dynamic or kinematic goes to the awake set
                    // and other attached bodies must be awake as well. For consistency, this is
                    // done for all cases.
                    B2Body bodyA = b2Array_Get(ref world.bodies, joint.edges[0].bodyId);
                    B2Body bodyB = b2Array_Get(ref world.bodies, joint.edges[1].bodyId);
                    b2WakeBody(world, bodyA);
                    b2WakeBody(world, bodyB);

                    jointKey = joint.edges[edgeIndex].nextKey;
                }
            }

            body.type = type;

            if (originalType == B2BodyType.b2_staticBody)
            {
                // Body is going from static to dynamic or kinematic. It only makes sense to move it to the awake set.
                Debug.Assert(body.setIndex == (int)B2SetType.b2_staticSet);

                B2SolverSet staticSet = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_staticSet);
                B2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_awakeSet);

                // Transfer body to awake set
                b2TransferBody(world, awakeSet, staticSet, body);

                // Create island for body
                b2CreateIslandForBody(world, (int)B2SetType.b2_awakeSet, body);

                // Transfer static joints to awake set
                int jointKey = body.headJointKey;
                while (jointKey != B2_NULL_INDEX)
                {
                    int jointId = jointKey >> 1;
                    int edgeIndex = jointKey & 1;

                    B2Joint joint = b2Array_Get(ref world.joints, jointId);

                    // Transfer the joint if it is in the static set
                    if (joint.setIndex == (int)B2SetType.b2_staticSet)
                    {
                        b2TransferJoint(world, awakeSet, staticSet, joint);
                    }
                    else if (joint.setIndex == (int)B2SetType.b2_awakeSet)
                    {
                        // In this case the joint must be re-inserted into the constraint graph to ensure the correct
                        // graph color.

                        // First transfer to the static set.
                        b2TransferJoint(world, staticSet, awakeSet, joint);

                        // Now transfer it back to the awake set and into the graph coloring.
                        b2TransferJoint(world, awakeSet, staticSet, joint);
                    }
                    else
                    {
                        // Otherwise the joint must be disabled.
                        Debug.Assert(joint.setIndex == (int)B2SetType.b2_disabledSet);
                    }

                    jointKey = joint.edges[edgeIndex].nextKey;
                }

                // Recreate shape proxies in movable tree.
                B2Transform transform = b2GetBodyTransformQuick(world, body);
                int shapeId = body.headShapeId;
                while (shapeId != B2_NULL_INDEX)
                {
                    B2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                    shapeId = shape.nextShapeId;
                    b2DestroyShapeProxy(shape, world.broadPhase);
                    bool forcePairCreation = true;
                    B2BodyType proxyType = type;
                    b2CreateShapeProxy(shape, world.broadPhase, proxyType, transform, forcePairCreation);
                }
            }
            else if (type == B2BodyType.b2_staticBody)
            {
                // The body is going from dynamic/kinematic to static. It should be awake.
                Debug.Assert(body.setIndex == (int)B2SetType.b2_awakeSet);

                B2SolverSet staticSet = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_staticSet);
                B2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_awakeSet);

                // Transfer body to static set
                b2TransferBody(world, staticSet, awakeSet, body);

                // Remove body from island.
                b2RemoveBodyFromIsland(world, body);

                B2BodySim bodySim = b2Array_Get(ref staticSet.bodySims, body.localIndex);
                bodySim.isFast = false;

                // Maybe transfer joints to static set.
                int jointKey = body.headJointKey;
                while (jointKey != B2_NULL_INDEX)
                {
                    int jointId = jointKey >> 1;
                    int edgeIndex = jointKey & 1;

                    B2Joint joint = b2Array_Get(ref world.joints, jointId);
                    jointKey = joint.edges[edgeIndex].nextKey;

                    int otherEdgeIndex = edgeIndex ^ 1;
                    B2Body otherBody = b2Array_Get(ref world.bodies, joint.edges[otherEdgeIndex].bodyId);

                    // Skip disabled joint
                    if (joint.setIndex == (int)B2SetType.b2_disabledSet)
                    {
                        // Joint is disable, should be connected to a disabled body
                        Debug.Assert(otherBody.setIndex == (int)B2SetType.b2_disabledSet);
                        continue;
                    }

                    // Since the body was not static, the joint must be awake.
                    Debug.Assert(joint.setIndex == (int)B2SetType.b2_awakeSet);

                    // Only transfer joint to static set if both bodies are static.
                    if (otherBody.setIndex == (int)B2SetType.b2_staticSet)
                    {
                        b2TransferJoint(world, staticSet, awakeSet, joint);
                    }
                    else
                    {
                        // The other body must be awake.
                        Debug.Assert(otherBody.setIndex == (int)B2SetType.b2_awakeSet);

                        // The joint must live in a graph color.
                        Debug.Assert(0 <= joint.colorIndex && joint.colorIndex < B2_GRAPH_COLOR_COUNT);

                        // In this case the joint must be re-inserted into the constraint graph to ensure the correct
                        // graph color.

                        // First transfer to the static set.
                        b2TransferJoint(world, staticSet, awakeSet, joint);

                        // Now transfer it back to the awake set and into the graph coloring.
                        b2TransferJoint(world, awakeSet, staticSet, joint);
                    }
                }

                // Recreate shape proxies in static tree.
                B2Transform transform = b2GetBodyTransformQuick(world, body);
                int shapeId = body.headShapeId;
                while (shapeId != B2_NULL_INDEX)
                {
                    B2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                    shapeId = shape.nextShapeId;
                    b2DestroyShapeProxy(shape, world.broadPhase);
                    bool forcePairCreation = true;
                    b2CreateShapeProxy(shape, world.broadPhase, B2BodyType.b2_staticBody, transform, forcePairCreation);
                }
            }
            else
            {
                Debug.Assert(originalType == B2BodyType.b2_dynamicBody || originalType == B2BodyType.b2_kinematicBody);
                Debug.Assert(type == B2BodyType.b2_dynamicBody || type == B2BodyType.b2_kinematicBody);

                // Recreate shape proxies in static tree.
                B2Transform transform = b2GetBodyTransformQuick(world, body);
                int shapeId = body.headShapeId;
                while (shapeId != B2_NULL_INDEX)
                {
                    B2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                    shapeId = shape.nextShapeId;
                    b2DestroyShapeProxy(shape, world.broadPhase);
                    B2BodyType proxyType = type;
                    bool forcePairCreation = true;
                    b2CreateShapeProxy(shape, world.broadPhase, proxyType, transform, forcePairCreation);
                }
            }

            // Relink all joints
            {
                int jointKey = body.headJointKey;
                while (jointKey != B2_NULL_INDEX)
                {
                    int jointId = jointKey >> 1;
                    int edgeIndex = jointKey & 1;

                    B2Joint joint = b2Array_Get(ref world.joints, jointId);
                    jointKey = joint.edges[edgeIndex].nextKey;

                    int otherEdgeIndex = edgeIndex ^ 1;
                    int otherBodyId = joint.edges[otherEdgeIndex].bodyId;
                    B2Body otherBody = b2Array_Get(ref world.bodies, otherBodyId);

                    if (otherBody.setIndex == (int)B2SetType.b2_disabledSet)
                    {
                        continue;
                    }

                    if (body.type == B2BodyType.b2_staticBody && otherBody.type == B2BodyType.b2_staticBody)
                    {
                        continue;
                    }

                    b2LinkJoint(world, joint, false);
                }

                b2MergeAwakeIslands(world);
            }

            // Body type affects the mass
            b2UpdateBodyMassData(world, body);

            b2ValidateSolverSets(world);
        }

        public static void b2Body_SetName(B2BodyId bodyId, string name)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);

            if (!string.IsNullOrEmpty(name))
            {
                body.name = name;
            }
            else
            {
                body.name = "";
            }
        }

        public static string b2Body_GetName(B2BodyId bodyId)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            return body.name;
        }

        public static void b2Body_SetUserData(B2BodyId bodyId, object userData)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            body.userData = userData;
        }

        public static object b2Body_GetUserData(B2BodyId bodyId)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            return body.userData;
        }

        public static float b2Body_GetMass(B2BodyId bodyId)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            return body.mass;
        }

        public static float b2Body_GetRotationalInertia(B2BodyId bodyId)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            return body.inertia;
        }

        public static B2Vec2 b2Body_GetLocalCenterOfMass(B2BodyId bodyId)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            B2BodySim bodySim = b2GetBodySim(world, body);
            return bodySim.localCenter;
        }

        public static B2Vec2 b2Body_GetWorldCenterOfMass(B2BodyId bodyId)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            B2BodySim bodySim = b2GetBodySim(world, body);
            return bodySim.center;
        }

        public static void b2Body_SetMassData(B2BodyId bodyId, B2MassData massData)
        {
            Debug.Assert(b2IsValidFloat(massData.mass) && massData.mass >= 0.0f);
            Debug.Assert(b2IsValidFloat(massData.rotationalInertia) && massData.rotationalInertia >= 0.0f);
            Debug.Assert(b2IsValidVec2(massData.center));

            B2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return;
            }

            B2Body body = b2GetBodyFullId(world, bodyId);
            B2BodySim bodySim = b2GetBodySim(world, body);

            body.mass = massData.mass;
            body.inertia = massData.rotationalInertia;
            bodySim.localCenter = massData.center;

            B2Vec2 center = b2TransformPoint(ref bodySim.transform, massData.center);
            bodySim.center = center;
            bodySim.center0 = center;

            bodySim.invMass = body.mass > 0.0f ? 1.0f / body.mass : 0.0f;
            bodySim.invInertia = body.inertia > 0.0f ? 1.0f / body.inertia : 0.0f;
        }

        public static B2MassData b2Body_GetMassData(B2BodyId bodyId)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            B2BodySim bodySim = b2GetBodySim(world, body);
            B2MassData massData = new B2MassData(body.mass, bodySim.localCenter, body.inertia);
            return massData;
        }

        public static void b2Body_ApplyMassFromShapes(B2BodyId bodyId)
        {
            B2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return;
            }

            B2Body body = b2GetBodyFullId(world, bodyId);
            b2UpdateBodyMassData(world, body);
        }

        public static void b2Body_SetLinearDamping(B2BodyId bodyId, float linearDamping)
        {
            Debug.Assert(b2IsValidFloat(linearDamping) && linearDamping >= 0.0f);

            B2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return;
            }

            B2Body body = b2GetBodyFullId(world, bodyId);
            B2BodySim bodySim = b2GetBodySim(world, body);
            bodySim.linearDamping = linearDamping;
        }

        public static float b2Body_GetLinearDamping(B2BodyId bodyId)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            B2BodySim bodySim = b2GetBodySim(world, body);
            return bodySim.linearDamping;
        }

        public static void b2Body_SetAngularDamping(B2BodyId bodyId, float angularDamping)
        {
            Debug.Assert(b2IsValidFloat(angularDamping) && angularDamping >= 0.0f);

            B2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return;
            }

            B2Body body = b2GetBodyFullId(world, bodyId);
            B2BodySim bodySim = b2GetBodySim(world, body);
            bodySim.angularDamping = angularDamping;
        }

        public static float b2Body_GetAngularDamping(B2BodyId bodyId)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            B2BodySim bodySim = b2GetBodySim(world, body);
            return bodySim.angularDamping;
        }

        public static void b2Body_SetGravityScale(B2BodyId bodyId, float gravityScale)
        {
            Debug.Assert(b2Body_IsValid(bodyId));
            Debug.Assert(b2IsValidFloat(gravityScale));

            B2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return;
            }

            B2Body body = b2GetBodyFullId(world, bodyId);
            B2BodySim bodySim = b2GetBodySim(world, body);
            bodySim.gravityScale = gravityScale;
        }

        public static float b2Body_GetGravityScale(B2BodyId bodyId)
        {
            Debug.Assert(b2Body_IsValid(bodyId));
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            B2BodySim bodySim = b2GetBodySim(world, body);
            return bodySim.gravityScale;
        }

        public static bool b2Body_IsAwake(B2BodyId bodyId)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            return body.setIndex == (int)B2SetType.b2_awakeSet;
        }

        public static void b2Body_SetAwake(B2BodyId bodyId, bool awake)
        {
            B2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return;
            }

            B2Body body = b2GetBodyFullId(world, bodyId);

            if (awake && body.setIndex >= (int)B2SetType.b2_firstSleepingSet)
            {
                b2WakeBody(world, body);
            }
            else if (awake == false && body.setIndex == (int)B2SetType.b2_awakeSet)
            {
                B2Island island = b2Array_Get(ref world.islands, body.islandId);
                if (island.constraintRemoveCount > 0)
                {
                    // Must split the island before sleeping. This is expensive.
                    b2SplitIsland(world, body.islandId);
                }

                b2TrySleepIsland(world, body.islandId);
            }
        }

        public static bool b2Body_IsEnabled(B2BodyId bodyId)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            return body.setIndex != (int)B2SetType.b2_disabledSet;
        }

        public static bool b2Body_IsSleepEnabled(B2BodyId bodyId)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            return body.enableSleep;
        }

        public static void b2Body_SetSleepThreshold(B2BodyId bodyId, float sleepThreshold)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            body.sleepThreshold = sleepThreshold;
        }

        public static float b2Body_GetSleepThreshold(B2BodyId bodyId)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            return body.sleepThreshold;
        }

        public static void b2Body_EnableSleep(B2BodyId bodyId, bool enableSleep)
        {
            B2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return;
            }

            B2Body body = b2GetBodyFullId(world, bodyId);
            body.enableSleep = enableSleep;

            if (enableSleep == false)
            {
                b2WakeBody(world, body);
            }
        }

        // Disabling a body requires a lot of detailed bookkeeping, but it is a valuable feature.
        // The most challenging aspect that joints may connect to bodies that are not disabled.
        public static void b2Body_Disable(B2BodyId bodyId)
        {
            B2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return;
            }

            B2Body body = b2GetBodyFullId(world, bodyId);
            if (body.setIndex == (int)B2SetType.b2_disabledSet)
            {
                return;
            }

            // Destroy contacts and wake bodies touching this body. This avoid floating bodies.
            // This is necessary even for static bodies.
            bool wakeBodies = true;
            b2DestroyBodyContacts(world, body, wakeBodies);

            // Disabled bodies are not in an island.
            b2RemoveBodyFromIsland(world, body);

            // Remove shapes from broad-phase
            int shapeId = body.headShapeId;
            while (shapeId != B2_NULL_INDEX)
            {
                B2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                shapeId = shape.nextShapeId;
                b2DestroyShapeProxy(shape, world.broadPhase);
            }

            // Transfer simulation data to disabled set
            B2SolverSet set = b2Array_Get(ref world.solverSets, body.setIndex);
            B2SolverSet disabledSet = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_disabledSet);

            // Transfer body sim
            b2TransferBody(world, disabledSet, set, body);

            // Unlink joints and transfer
            int jointKey = body.headJointKey;
            while (jointKey != B2_NULL_INDEX)
            {
                int jointId = jointKey >> 1;
                int edgeIndex = jointKey & 1;

                B2Joint joint = b2Array_Get(ref world.joints, jointId);
                jointKey = joint.edges[edgeIndex].nextKey;

                // joint may already be disabled by other body
                if (joint.setIndex == (int)B2SetType.b2_disabledSet)
                {
                    continue;
                }

                Debug.Assert(joint.setIndex == set.setIndex || set.setIndex == (int)B2SetType.b2_staticSet);

                // Remove joint from island
                if (joint.islandId != B2_NULL_INDEX)
                {
                    b2UnlinkJoint(world, joint);
                }

                // Transfer joint to disabled set
                B2SolverSet jointSet = b2Array_Get(ref world.solverSets, joint.setIndex);
                b2TransferJoint(world, disabledSet, jointSet, joint);
            }

            b2ValidateConnectivity(world);
            b2ValidateSolverSets(world);
        }

        public static void b2Body_Enable(B2BodyId bodyId)
        {
            B2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return;
            }

            B2Body body = b2GetBodyFullId(world, bodyId);
            if (body.setIndex != (int)B2SetType.b2_disabledSet)
            {
                return;
            }

            B2SolverSet disabledSet = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_disabledSet);
            int setId = body.type == B2BodyType.b2_staticBody ? (int)B2SetType.b2_staticSet : (int)B2SetType.b2_awakeSet;
            B2SolverSet targetSet = b2Array_Get(ref world.solverSets, setId);

            b2TransferBody(world, targetSet, disabledSet, body);

            B2Transform transform = b2GetBodyTransformQuick(world, body);

            // Add shapes to broad-phase
            B2BodyType proxyType = body.type;
            bool forcePairCreation = true;
            int shapeId = body.headShapeId;
            while (shapeId != B2_NULL_INDEX)
            {
                B2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                shapeId = shape.nextShapeId;

                b2CreateShapeProxy(shape, world.broadPhase, proxyType, transform, forcePairCreation);
            }

            if (setId != (int)B2SetType.b2_staticSet)
            {
                b2CreateIslandForBody(world, setId, body);
            }

            // Transfer joints. If the other body is disabled, don't transfer.
            // If the other body is sleeping, wake it.
            bool mergeIslands = false;
            int jointKey = body.headJointKey;
            while (jointKey != B2_NULL_INDEX)
            {
                int jointId = jointKey >> 1;
                int edgeIndex = jointKey & 1;

                B2Joint joint = b2Array_Get(ref world.joints, jointId);
                Debug.Assert(joint.setIndex == (int)B2SetType.b2_disabledSet);
                Debug.Assert(joint.islandId == B2_NULL_INDEX);

                jointKey = joint.edges[edgeIndex].nextKey;

                B2Body bodyA = b2Array_Get(ref world.bodies, joint.edges[0].bodyId);
                B2Body bodyB = b2Array_Get(ref world.bodies, joint.edges[1].bodyId);

                if (bodyA.setIndex == (int)B2SetType.b2_disabledSet || bodyB.setIndex == (int)B2SetType.b2_disabledSet)
                {
                    // one body is still disabled
                    continue;
                }

                // Transfer joint first
                int jointSetId;
                if (bodyA.setIndex == (int)B2SetType.b2_staticSet && bodyB.setIndex == (int)B2SetType.b2_staticSet)
                {
                    jointSetId = (int)B2SetType.b2_staticSet;
                }
                else if (bodyA.setIndex == (int)B2SetType.b2_staticSet)
                {
                    jointSetId = bodyB.setIndex;
                }
                else
                {
                    jointSetId = bodyA.setIndex;
                }

                B2SolverSet jointSet = b2Array_Get(ref world.solverSets, jointSetId);
                b2TransferJoint(world, jointSet, disabledSet, joint);

                // Now that the joint is in the correct set, I can link the joint in the island.
                if (jointSetId != (int)B2SetType.b2_staticSet)
                {
                    b2LinkJoint(world, joint, mergeIslands);
                }
            }

            // Now merge islands
            b2MergeAwakeIslands(world);

            b2ValidateSolverSets(world);
        }

        public static void b2Body_SetFixedRotation(B2BodyId bodyId, bool flag)
        {
            B2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return;
            }

            B2Body body = b2GetBodyFullId(world, bodyId);
            if (body.fixedRotation != flag)
            {
                body.fixedRotation = flag;

                B2BodyState state = b2GetBodyState(world, body);
                if (state != null)
                {
                    state.angularVelocity = 0.0f;
                }

                b2UpdateBodyMassData(world, body);
            }
        }

        public static bool b2Body_IsFixedRotation(B2BodyId bodyId)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            return body.fixedRotation;
        }

        public static void b2Body_SetBullet(B2BodyId bodyId, bool flag)
        {
            B2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return;
            }

            B2Body body = b2GetBodyFullId(world, bodyId);
            B2BodySim bodySim = b2GetBodySim(world, body);
            bodySim.isBullet = flag;
        }

        public static bool b2Body_IsBullet(B2BodyId bodyId)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            B2BodySim bodySim = b2GetBodySim(world, body);
            return bodySim.isBullet;
        }

        public static void b2Body_EnableContactEvents(B2BodyId bodyId, bool flag)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            int shapeId = body.headShapeId;
            while (shapeId != B2_NULL_INDEX)
            {
                B2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                shape.enableContactEvents = flag;
                shapeId = shape.nextShapeId;
            }
        }

        public static void b2Body_EnableHitEvents(B2BodyId bodyId, bool flag)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            int shapeId = body.headShapeId;
            while (shapeId != B2_NULL_INDEX)
            {
                B2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                shape.enableHitEvents = flag;
                shapeId = shape.nextShapeId;
            }
        }

        public static B2WorldId b2Body_GetWorld(B2BodyId bodyId)
        {
            B2World world = b2GetWorld(bodyId.world0);
            return new B2WorldId((ushort)(bodyId.world0 + 1), world.generation);
        }

        public static int b2Body_GetShapeCount(B2BodyId bodyId)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            return body.shapeCount;
        }

        public static int b2Body_GetShapes(B2BodyId bodyId, Span<B2ShapeId> shapeArray, int capacity)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            int shapeId = body.headShapeId;
            int shapeCount = 0;
            while (shapeId != B2_NULL_INDEX && shapeCount < capacity)
            {
                B2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                B2ShapeId id = new B2ShapeId(shape.id + 1, bodyId.world0, shape.generation);
                shapeArray[shapeCount] = id;
                shapeCount += 1;

                shapeId = shape.nextShapeId;
            }

            return shapeCount;
        }

        public static int b2Body_GetJointCount(B2BodyId bodyId)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            return body.jointCount;
        }

        public static int b2Body_GetJoints(B2BodyId bodyId, Span<B2JointId> jointArray, int capacity)
        {
            B2World world = b2GetWorld(bodyId.world0);
            B2Body body = b2GetBodyFullId(world, bodyId);
            int jointKey = body.headJointKey;

            int jointCount = 0;
            while (jointKey != B2_NULL_INDEX && jointCount < capacity)
            {
                int jointId = jointKey >> 1;
                int edgeIndex = jointKey & 1;

                B2Joint joint = b2Array_Get(ref world.joints, jointId);

                B2JointId id = new B2JointId(jointId + 1, bodyId.world0, joint.generation);
                jointArray[jointCount] = id;
                jointCount += 1;

                jointKey = joint.edges[edgeIndex].nextKey;
            }

            return jointCount;
        }

        public static bool b2ShouldBodiesCollide(B2World world, B2Body bodyA, B2Body bodyB)
        {
            if (bodyA.type != B2BodyType.b2_dynamicBody && bodyB.type != B2BodyType.b2_dynamicBody)
            {
                return false;
            }

            int jointKey;
            int otherBodyId;
            if (bodyA.jointCount < bodyB.jointCount)
            {
                jointKey = bodyA.headJointKey;
                otherBodyId = bodyB.id;
            }
            else
            {
                jointKey = bodyB.headJointKey;
                otherBodyId = bodyA.id;
            }

            while (jointKey != B2_NULL_INDEX)
            {
                int jointId = jointKey >> 1;
                int edgeIndex = jointKey & 1;
                int otherEdgeIndex = edgeIndex ^ 1;

                B2Joint joint = b2Array_Get(ref world.joints, jointId);
                if (joint.collideConnected == false && joint.edges[otherEdgeIndex].bodyId == otherBodyId)
                {
                    return false;
                }

                jointKey = joint.edges[edgeIndex].nextKey;
            }

            return true;
        }
    }
}
