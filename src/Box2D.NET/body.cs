// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using Box2D.NET.Primitives;
using static Box2D.NET.array;
using static Box2D.NET.core;
using static Box2D.NET.constants;
using static Box2D.NET.contact;
using static Box2D.NET.math_function;
using static Box2D.NET.id;
using static Box2D.NET.shape;
using static Box2D.NET.world;
using static Box2D.NET.joint;
using static Box2D.NET.id_pool;
using static Box2D.NET.island;
using static Box2D.NET.sensor;
using static Box2D.NET.solver_set;
using static Box2D.NET.board_phase;

namespace Box2D.NET
{
    public static class body
    {
        // Identity body state, notice the deltaRotation is {1, 0}
        public static readonly b2BodyState b2_identityBodyState = new b2BodyState()
        {
            linearVelocity = new b2Vec2(0.0f, 0.0f),
            angularVelocity = 0.0f,
            flags = 0,
            deltaPosition = new b2Vec2(0.0f, 0.0f),
            deltaRotation = new b2Rot(1.0f, 0.0f),
        };

        public static b2Sweep b2MakeSweep(b2BodySim bodySim)
        {
            b2Sweep s = new b2Sweep();
            s.c1 = bodySim.center0;
            s.c2 = bodySim.center;
            s.q1 = bodySim.rotation0;
            s.q2 = bodySim.transform.q;
            s.localCenter = bodySim.localCenter;
            return s;
        }

        // Get a validated body from a world using an id.
        public static b2Body b2GetBodyFullId(b2World world, b2BodyId bodyId)
        {
            Debug.Assert(b2Body_IsValid(bodyId));

            // id index starts at one so that zero can represent null
            return b2Array_Get(ref world.bodies, bodyId.index1 - 1);
        }

        public static b2Transform b2GetBodyTransformQuick(b2World world, b2Body body)
        {
            b2SolverSet set = b2Array_Get(ref world.solverSets, body.setIndex);
            b2BodySim bodySim = b2Array_Get(ref set.bodySims, body.localIndex);
            return bodySim.transform;
        }

        public static b2Transform b2GetBodyTransform(b2World world, int bodyId)
        {
            b2Body body = b2Array_Get(ref world.bodies, bodyId);
            return b2GetBodyTransformQuick(world, body);
        }

        // Create a b2BodyId from a raw id.
        public static b2BodyId b2MakeBodyId(b2World world, int bodyId)
        {
            b2Body body = b2Array_Get(ref world.bodies, bodyId);
            return new b2BodyId(bodyId + 1, world.worldId, body.generation);
        }

        public static b2BodySim b2GetBodySim(b2World world, b2Body body)
        {
            b2SolverSet set = b2Array_Get(ref world.solverSets, body.setIndex);
            b2BodySim bodySim = b2Array_Get(ref set.bodySims, body.localIndex);
            return bodySim;
        }

        public static b2BodyState b2GetBodyState(b2World world, b2Body body)
        {
            if (body.setIndex == (int)b2SetType.b2_awakeSet)
            {
                b2SolverSet set = b2Array_Get(ref world.solverSets, (int)b2SetType.b2_awakeSet);
                return b2Array_Get(ref set.bodyStates, body.localIndex);
            }

            return null;
        }

        public static void b2CreateIslandForBody(b2World world, int setIndex, b2Body body)
        {
            Debug.Assert(body.islandId == B2_NULL_INDEX);
            Debug.Assert(body.islandPrev == B2_NULL_INDEX);
            Debug.Assert(body.islandNext == B2_NULL_INDEX);
            Debug.Assert(setIndex != (int)b2SetType.b2_disabledSet);

            b2Island island = b2CreateIsland(world, setIndex);

            body.islandId = island.islandId;
            island.headBody = body.id;
            island.tailBody = body.id;
            island.bodyCount = 1;
        }

        public static void b2RemoveBodyFromIsland(b2World world, b2Body body)
        {
            if (body.islandId == B2_NULL_INDEX)
            {
                Debug.Assert(body.islandPrev == B2_NULL_INDEX);
                Debug.Assert(body.islandNext == B2_NULL_INDEX);
                return;
            }

            int islandId = body.islandId;
            b2Island island = b2Array_Get(ref world.islands, islandId);

            // Fix the island's linked list of sims
            if (body.islandPrev != B2_NULL_INDEX)
            {
                b2Body prevBody = b2Array_Get(ref world.bodies, body.islandPrev);
                prevBody.islandNext = body.islandNext;
            }

            if (body.islandNext != B2_NULL_INDEX)
            {
                b2Body nextBody = b2Array_Get(ref world.bodies, body.islandNext);
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

        public static void b2DestroyBodyContacts(b2World world, b2Body body, bool wakeBodies)
        {
            // Destroy the attached contacts
            int edgeKey = body.headContactKey;
            while (edgeKey != B2_NULL_INDEX)
            {
                int contactId = edgeKey >> 1;
                int edgeIndex = edgeKey & 1;

                b2Contact contact = b2Array_Get(ref world.contacts, contactId);
                edgeKey = contact.edges[edgeIndex].nextKey;
                b2DestroyContact(world, contact, wakeBodies);
            }

            b2ValidateSolverSets(world);
        }

        public static b2BodyId b2CreateBody(b2WorldId worldId, b2BodyDef def)
        {
            B2_CHECK_DEF(def);
            Debug.Assert(b2IsValidVec2(def.position));
            Debug.Assert(b2IsValidRotation(def.rotation));
            Debug.Assert(b2IsValidVec2(def.linearVelocity));
            Debug.Assert(b2IsValidFloat(def.angularVelocity));
            Debug.Assert(b2IsValidFloat(def.linearDamping) && def.linearDamping >= 0.0f);
            Debug.Assert(b2IsValidFloat(def.angularDamping) && def.angularDamping >= 0.0f);
            Debug.Assert(b2IsValidFloat(def.sleepThreshold) && def.sleepThreshold >= 0.0f);
            Debug.Assert(b2IsValidFloat(def.gravityScale));

            b2World world = b2GetWorldFromId(worldId);
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
                setId = (int)b2SetType.b2_disabledSet;
            }
            else if (def.type == b2BodyType.b2_staticBody)
            {
                setId = (int)b2SetType.b2_staticSet;
            }
            else if (isAwake == true)
            {
                setId = (int)b2SetType.b2_awakeSet;
            }
            else
            {
                // new set for a sleeping body in its own island
                setId = b2AllocId(world.solverSetIdPool);
                if (setId == world.solverSets.count)
                {
                    // Create a zero initialized solver set. All sub-arrays are also zero initialized.
                    b2Array_Push(ref world.solverSets, new b2SolverSet());
                }
                else
                {
                    Debug.Assert(world.solverSets.data[setId].setIndex == B2_NULL_INDEX);
                }

                world.solverSets.data[setId].setIndex = setId;
            }

            Debug.Assert(0 <= setId && setId < world.solverSets.count);

            int bodyId = b2AllocId(world.bodyIdPool);

            b2SolverSet set = b2Array_Get(ref world.solverSets, setId);
            ref b2BodySim bodySim = ref b2Array_Add(ref set.bodySims);
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

            if (setId == (int)b2SetType.b2_awakeSet)
            {
                ref b2BodyState bodyState = ref b2Array_Add(ref set.bodyStates);
                //Debug.Assert( ( (uintptr_t)bodyState & 0x1F ) == 0 );
                //*bodyState = ( b2BodyState ){ 0 }; 
                bodyState.Clear();
                bodyState.linearVelocity = def.linearVelocity;
                bodyState.angularVelocity = def.angularVelocity;
                bodyState.deltaRotation = b2Rot_identity;
            }

            if (bodyId == world.bodies.count)
            {
                b2Array_Push(ref world.bodies, new b2Body());
            }
            else
            {
                Debug.Assert(world.bodies.data[bodyId].id == B2_NULL_INDEX);
            }

            b2Body body = b2Array_Get(ref world.bodies, bodyId);

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
            if (setId >= (int)b2SetType.b2_awakeSet)
            {
                b2CreateIslandForBody(world, setId, body);
            }

            b2ValidateSolverSets(world);

            b2BodyId id = new b2BodyId(bodyId + 1, world.worldId, body.generation);
            return id;
        }

        public static bool b2IsBodyAwake(b2World world, b2Body body)
        {
            B2_UNUSED(world);
            return body.setIndex == (int)b2SetType.b2_awakeSet;
        }

        // careful calling this because it can invalidate body, state, joint, and contact pointers
        public static bool b2WakeBody(b2World world, b2Body body)
        {
            if (body.setIndex >= (int)b2SetType.b2_firstSleepingSet)
            {
                b2WakeSolverSet(world, body.setIndex);
                return true;
            }

            return false;
        }

        public static void b2DestroyBody(b2BodyId bodyId)
        {
            b2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return;
            }

            b2Body body = b2GetBodyFullId(world, bodyId);

            // Wake bodies attached to this body, even if this body is static.
            bool wakeBodies = true;

            // Destroy the attached joints
            int edgeKey = body.headJointKey;
            while (edgeKey != B2_NULL_INDEX)
            {
                int jointId = edgeKey >> 1;
                int edgeIndex = edgeKey & 1;

                b2Joint joint = b2Array_Get(ref world.joints, jointId);
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
                b2Shape shape = b2Array_Get(ref world.shapes, shapeId);

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
                b2ChainShape chain = b2Array_Get(ref world.chainShapes, chainId);

                b2FreeChainData(chain);

                // Return chain to free list.
                b2FreeId(world.chainIdPool, chainId);
                chain.id = B2_NULL_INDEX;

                chainId = chain.nextChainId;
            }

            b2RemoveBodyFromIsland(world, body);

            // Remove body sim from solver set that owns it
            b2SolverSet set = b2Array_Get(ref world.solverSets, body.setIndex);
            int movedIndex = b2Array_RemoveSwap(ref set.bodySims, body.localIndex);
            if (movedIndex != B2_NULL_INDEX)
            {
                // Fix moved body index
                b2BodySim movedSim = set.bodySims.data[body.localIndex];
                int movedId = movedSim.bodyId;
                b2Body movedBody = b2Array_Get(ref world.bodies, movedId);
                Debug.Assert(movedBody.localIndex == movedIndex);
                movedBody.localIndex = body.localIndex;
            }

            // Remove body state from awake set
            if (body.setIndex == (int)b2SetType.b2_awakeSet)
            {
                int result = b2Array_RemoveSwap(ref set.bodyStates, body.localIndex);
                B2_UNUSED(result);
                Debug.Assert(result == movedIndex);
            }
            else if (set.setIndex >= (int)b2SetType.b2_firstSleepingSet && set.bodySims.count == 0)
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

        public static int b2Body_GetContactCapacity(b2BodyId bodyId)
        {
            b2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return 0;
            }

            b2Body body = b2GetBodyFullId(world, bodyId);

            // Conservative and fast
            return body.contactCount;
        }

        // todo what about sensors?
        // todo sample needed
        public static int b2Body_GetContactData(b2BodyId bodyId, b2ContactData[] contactData, int capacity)
        {
            b2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return 0;
            }

            b2Body body = b2GetBodyFullId(world, bodyId);

            int contactKey = body.headContactKey;
            int index = 0;
            while (contactKey != B2_NULL_INDEX && index < capacity)
            {
                int contactId = contactKey >> 1;
                int edgeIndex = contactKey & 1;

                b2Contact contact = b2Array_Get(ref world.contacts, contactId);

                // Is contact touching?
                if (0 != (contact.flags & (uint)b2ContactFlags.b2_contactTouchingFlag))
                {
                    b2Shape shapeA = b2Array_Get(ref world.shapes, contact.shapeIdA);
                    b2Shape shapeB = b2Array_Get(ref world.shapes, contact.shapeIdB);

                    contactData[index].shapeIdA = new b2ShapeId(shapeA.id + 1, bodyId.world0, shapeA.generation);
                    contactData[index].shapeIdB = new b2ShapeId(shapeB.id + 1, bodyId.world0, shapeB.generation);

                    b2ContactSim contactSim = b2GetContactSim(world, contact);
                    contactData[index].manifold = contactSim.manifold;

                    index += 1;
                }

                contactKey = contact.edges[edgeIndex].nextKey;
            }

            Debug.Assert(index <= capacity);

            return index;
        }

        public static b2AABB b2Body_ComputeAABB(b2BodyId bodyId)
        {
            b2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return new b2AABB();
            }

            b2Body body = b2GetBodyFullId(world, bodyId);
            if (body.headShapeId == B2_NULL_INDEX)
            {
                b2Transform transform = b2GetBodyTransform(world, body.id);
                return new b2AABB(transform.p, transform.p);
            }

            b2Shape shape = b2Array_Get(ref world.shapes, body.headShapeId);
            b2AABB aabb = shape.aabb;
            while (shape.nextShapeId != B2_NULL_INDEX)
            {
                shape = b2Array_Get(ref world.shapes, shape.nextShapeId);
                aabb = b2AABB_Union(aabb, shape.aabb);
            }

            return aabb;
        }

        public static void b2UpdateBodyMassData(b2World world, b2Body body)
        {
            b2BodySim bodySim = b2GetBodySim(world, body);

            // Compute mass data from shapes. Each shape has its own density.
            body.mass = 0.0f;
            body.inertia = 0.0f;

            bodySim.invMass = 0.0f;
            bodySim.invInertia = 0.0f;
            bodySim.localCenter = b2Vec2_zero;
            bodySim.minExtent = B2_HUGE;
            bodySim.maxExtent = 0.0f;

            // Static and kinematic sims have zero mass.
            if (body.type != b2BodyType.b2_dynamicBody)
            {
                bodySim.center = bodySim.transform.p;

                // Need extents for kinematic bodies for sleeping to work correctly.
                if (body.type == b2BodyType.b2_kinematicBody)
                {
                    int nextShapeId = body.headShapeId;
                    while (nextShapeId != B2_NULL_INDEX)
                    {
                        b2Shape s = b2Array_Get(ref world.shapes, nextShapeId);

                        b2ShapeExtent extent = b2ComputeShapeExtent(s, b2Vec2_zero);
                        bodySim.minExtent = b2MinFloat(bodySim.minExtent, extent.minExtent);
                        bodySim.maxExtent = b2MaxFloat(bodySim.maxExtent, extent.maxExtent);

                        nextShapeId = s.nextShapeId;
                    }
                }

                return;
            }

            // Accumulate mass over all shapes.
            b2Vec2 localCenter = b2Vec2_zero;
            int shapeId = body.headShapeId;
            while (shapeId != B2_NULL_INDEX)
            {
                b2Shape s = b2Array_Get(ref world.shapes, shapeId);
                shapeId = s.nextShapeId;

                if (s.density == 0.0f)
                {
                    continue;
                }

                b2MassData massData = b2ComputeShapeMass(s);
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
            b2Vec2 oldCenter = bodySim.center;
            bodySim.localCenter = localCenter;
            bodySim.center = b2TransformPoint(ref bodySim.transform, bodySim.localCenter);

            // Update center of mass velocity
            b2BodyState state = b2GetBodyState(world, body);
            if (state != null)
            {
                b2Vec2 deltaLinear = b2CrossSV(state.angularVelocity, b2Sub(bodySim.center, oldCenter));
                state.linearVelocity = b2Add(state.linearVelocity, deltaLinear);
            }

            // Compute body extents relative to center of mass
            shapeId = body.headShapeId;
            while (shapeId != B2_NULL_INDEX)
            {
                b2Shape s = b2Array_Get(ref world.shapes, shapeId);

                b2ShapeExtent extent = b2ComputeShapeExtent(s, localCenter);
                bodySim.minExtent = b2MinFloat(bodySim.minExtent, extent.minExtent);
                bodySim.maxExtent = b2MaxFloat(bodySim.maxExtent, extent.maxExtent);

                shapeId = s.nextShapeId;
            }
        }

        public static b2Vec2 b2Body_GetPosition(b2BodyId bodyId)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            b2Transform transform = b2GetBodyTransformQuick(world, body);
            return transform.p;
        }

        public static b2Rot b2Body_GetRotation(b2BodyId bodyId)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            b2Transform transform = b2GetBodyTransformQuick(world, body);
            return transform.q;
        }

        public static b2Transform b2Body_GetTransform(b2BodyId bodyId)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            return b2GetBodyTransformQuick(world, body);
        }

        public static b2Vec2 b2Body_GetLocalPoint(b2BodyId bodyId, b2Vec2 worldPoint)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            b2Transform transform = b2GetBodyTransformQuick(world, body);
            return b2InvTransformPoint(transform, worldPoint);
        }

        public static b2Vec2 b2Body_GetWorldPoint(b2BodyId bodyId, b2Vec2 localPoint)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            b2Transform transform = b2GetBodyTransformQuick(world, body);
            return b2TransformPoint(ref transform, localPoint);
        }

        public static b2Vec2 b2Body_GetLocalVector(b2BodyId bodyId, b2Vec2 worldVector)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            b2Transform transform = b2GetBodyTransformQuick(world, body);
            return b2InvRotateVector(transform.q, worldVector);
        }

        public static b2Vec2 b2Body_GetWorldVector(b2BodyId bodyId, b2Vec2 localVector)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            b2Transform transform = b2GetBodyTransformQuick(world, body);
            return b2RotateVector(transform.q, localVector);
        }

        public static void b2Body_SetTransform(b2BodyId bodyId, b2Vec2 position, b2Rot rotation)
        {
            Debug.Assert(b2IsValidVec2(position));
            Debug.Assert(b2IsValidRotation(rotation));
            Debug.Assert(b2Body_IsValid(bodyId));
            b2World world = b2GetWorld(bodyId.world0);
            Debug.Assert(world.locked == false);

            b2Body body = b2GetBodyFullId(world, bodyId);
            b2BodySim bodySim = b2GetBodySim(world, body);

            bodySim.transform.p = position;
            bodySim.transform.q = rotation;
            bodySim.center = b2TransformPoint(ref bodySim.transform, bodySim.localCenter);

            bodySim.rotation0 = bodySim.transform.q;
            bodySim.center0 = bodySim.center;

            b2BroadPhase broadPhase = world.broadPhase;

            b2Transform transform = bodySim.transform;
            float margin = B2_AABB_MARGIN;
            float speculativeDistance = B2_SPECULATIVE_DISTANCE;

            int shapeId = body.headShapeId;
            while (shapeId != B2_NULL_INDEX)
            {
                b2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                b2AABB aabb = b2ComputeShapeAABB(shape, transform);
                aabb.lowerBound.x -= speculativeDistance;
                aabb.lowerBound.y -= speculativeDistance;
                aabb.upperBound.x += speculativeDistance;
                aabb.upperBound.y += speculativeDistance;
                shape.aabb = aabb;

                if (b2AABB_Contains(shape.fatAABB, aabb) == false)
                {
                    b2AABB fatAABB;
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

        public static b2Vec2 b2Body_GetLinearVelocity(b2BodyId bodyId)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            b2BodyState state = b2GetBodyState(world, body);
            if (state != null)
            {
                return state.linearVelocity;
            }

            return b2Vec2_zero;
        }

        public static float b2Body_GetAngularVelocity(b2BodyId bodyId)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            b2BodyState state = b2GetBodyState(world, body);
            if (state != null)
            {
                return state.angularVelocity;
            }

            return 0.0f;
        }

        public static void b2Body_SetLinearVelocity(b2BodyId bodyId, b2Vec2 linearVelocity)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);

            if (body.type == b2BodyType.b2_staticBody)
            {
                return;
            }

            if (b2LengthSquared(linearVelocity) > 0.0f)
            {
                b2WakeBody(world, body);
            }

            b2BodyState state = b2GetBodyState(world, body);
            if (state == null)
            {
                return;
            }

            state.linearVelocity = linearVelocity;
        }

        public static void b2Body_SetAngularVelocity(b2BodyId bodyId, float angularVelocity)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);

            if (body.type == b2BodyType.b2_staticBody || body.fixedRotation)
            {
                return;
            }

            if (angularVelocity != 0.0f)
            {
                b2WakeBody(world, body);
            }

            b2BodyState state = b2GetBodyState(world, body);
            if (state == null)
            {
                return;
            }

            state.angularVelocity = angularVelocity;
        }

        public static b2Vec2 b2Body_GetLocalPointVelocity(b2BodyId bodyId, b2Vec2 localPoint)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            b2BodyState state = b2GetBodyState(world, body);
            if (state == null)
            {
                return b2Vec2_zero;
            }

            b2SolverSet set = b2Array_Get(ref world.solverSets, body.setIndex);
            b2BodySim bodySim = b2Array_Get(ref set.bodySims, body.localIndex);

            b2Vec2 r = b2RotateVector(bodySim.transform.q, b2Sub(localPoint, bodySim.localCenter));
            b2Vec2 v = b2Add(state.linearVelocity, b2CrossSV(state.angularVelocity, r));
            return v;
        }

        public static b2Vec2 b2Body_GetWorldPointVelocity(b2BodyId bodyId, b2Vec2 worldPoint)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            b2BodyState state = b2GetBodyState(world, body);
            if (state == null)
            {
                return b2Vec2_zero;
            }

            b2SolverSet set = b2Array_Get(ref world.solverSets, body.setIndex);
            b2BodySim bodySim = b2Array_Get(ref set.bodySims, body.localIndex);

            b2Vec2 r = b2Sub(worldPoint, bodySim.center);
            b2Vec2 v = b2Add(state.linearVelocity, b2CrossSV(state.angularVelocity, r));
            return v;
        }

        public static void b2Body_ApplyForce(b2BodyId bodyId, b2Vec2 force, b2Vec2 point, bool wake)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);

            if (wake && body.setIndex >= (int)b2SetType.b2_firstSleepingSet)
            {
                b2WakeBody(world, body);
            }

            if (body.setIndex == (int)b2SetType.b2_awakeSet)
            {
                b2BodySim bodySim = b2GetBodySim(world, body);
                bodySim.force = b2Add(bodySim.force, force);
                bodySim.torque += b2Cross(b2Sub(point, bodySim.center), force);
            }
        }

        public static void b2Body_ApplyForceToCenter(b2BodyId bodyId, b2Vec2 force, bool wake)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);

            if (wake && body.setIndex >= (int)b2SetType.b2_firstSleepingSet)
            {
                b2WakeBody(world, body);
            }

            if (body.setIndex == (int)b2SetType.b2_awakeSet)
            {
                b2BodySim bodySim = b2GetBodySim(world, body);
                bodySim.force = b2Add(bodySim.force, force);
            }
        }

        public static void b2Body_ApplyTorque(b2BodyId bodyId, float torque, bool wake)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);

            if (wake && body.setIndex >= (int)b2SetType.b2_firstSleepingSet)
            {
                b2WakeBody(world, body);
            }

            if (body.setIndex == (int)b2SetType.b2_awakeSet)
            {
                b2BodySim bodySim = b2GetBodySim(world, body);
                bodySim.torque += torque;
            }
        }

        public static void b2Body_ApplyLinearImpulse(b2BodyId bodyId, b2Vec2 impulse, b2Vec2 point, bool wake)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);

            if (wake && body.setIndex >= (int)b2SetType.b2_firstSleepingSet)
            {
                b2WakeBody(world, body);
            }

            if (body.setIndex == (int)b2SetType.b2_awakeSet)
            {
                int localIndex = body.localIndex;
                b2SolverSet set = b2Array_Get(ref world.solverSets, (int)b2SetType.b2_awakeSet);
                b2BodyState state = b2Array_Get(ref set.bodyStates, localIndex);
                b2BodySim bodySim = b2Array_Get(ref set.bodySims, localIndex);
                state.linearVelocity = b2MulAdd(state.linearVelocity, bodySim.invMass, impulse);
                state.angularVelocity += bodySim.invInertia * b2Cross(b2Sub(point, bodySim.center), impulse);
            }
        }

        public static void b2Body_ApplyLinearImpulseToCenter(b2BodyId bodyId, b2Vec2 impulse, bool wake)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);

            if (wake && body.setIndex >= (int)b2SetType.b2_firstSleepingSet)
            {
                b2WakeBody(world, body);
            }

            if (body.setIndex == (int)b2SetType.b2_awakeSet)
            {
                int localIndex = body.localIndex;
                b2SolverSet set = b2Array_Get(ref world.solverSets, (int)b2SetType.b2_awakeSet);
                b2BodyState state = b2Array_Get(ref set.bodyStates, localIndex);
                b2BodySim bodySim = b2Array_Get(ref set.bodySims, localIndex);
                state.linearVelocity = b2MulAdd(state.linearVelocity, bodySim.invMass, impulse);
            }
        }

        public static void b2Body_ApplyAngularImpulse(b2BodyId bodyId, float impulse, bool wake)
        {
            Debug.Assert(b2Body_IsValid(bodyId));
            b2World world = b2GetWorld(bodyId.world0);

            int id = bodyId.index1 - 1;
            b2Body body = b2Array_Get(ref world.bodies, id);
            Debug.Assert(body.generation == bodyId.generation);

            if (wake && body.setIndex >= (int)b2SetType.b2_firstSleepingSet)
            {
                // this will not invalidate body pointer
                b2WakeBody(world, body);
            }

            if (body.setIndex == (int)b2SetType.b2_awakeSet)
            {
                int localIndex = body.localIndex;
                b2SolverSet set = b2Array_Get(ref world.solverSets, (int)b2SetType.b2_awakeSet);
                b2BodyState state = b2Array_Get(ref set.bodyStates, localIndex);
                b2BodySim bodySim = b2Array_Get(ref set.bodySims, localIndex);
                state.angularVelocity += bodySim.invInertia * impulse;
            }
        }

        public static b2BodyType b2Body_GetType(b2BodyId bodyId)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            return body.type;
        }

        // Changing the body type is quite complex mainly due to joints.
        // Considerations:
        // - body and joints must be moved to the correct set
        // - islands must be updated
        // - graph coloring must be correct
        // - any body connected to a joint may be disabled
        // - joints between static bodies must go into the static set
        public static void b2Body_SetType(b2BodyId bodyId, b2BodyType type)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);

            b2BodyType originalType = body.type;
            if (originalType == type)
            {
                return;
            }

            if (body.setIndex == (int)b2SetType.b2_disabledSet)
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

                    b2Joint joint = b2Array_Get(ref world.joints, jointId);
                    if (joint.islandId != B2_NULL_INDEX)
                    {
                        b2UnlinkJoint(world, joint);
                    }

                    // A body going from static to dynamic or kinematic goes to the awake set
                    // and other attached bodies must be awake as well. For consistency, this is
                    // done for all cases.
                    b2Body bodyA = b2Array_Get(ref world.bodies, joint.edges[0].bodyId);
                    b2Body bodyB = b2Array_Get(ref world.bodies, joint.edges[1].bodyId);
                    b2WakeBody(world, bodyA);
                    b2WakeBody(world, bodyB);

                    jointKey = joint.edges[edgeIndex].nextKey;
                }
            }

            body.type = type;

            if (originalType == b2BodyType.b2_staticBody)
            {
                // Body is going from static to dynamic or kinematic. It only makes sense to move it to the awake set.
                Debug.Assert(body.setIndex == (int)b2SetType.b2_staticSet);

                b2SolverSet staticSet = b2Array_Get(ref world.solverSets, (int)b2SetType.b2_staticSet);
                b2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)b2SetType.b2_awakeSet);

                // Transfer body to awake set
                b2TransferBody(world, awakeSet, staticSet, body);

                // Create island for body
                b2CreateIslandForBody(world, (int)b2SetType.b2_awakeSet, body);

                // Transfer static joints to awake set
                int jointKey = body.headJointKey;
                while (jointKey != B2_NULL_INDEX)
                {
                    int jointId = jointKey >> 1;
                    int edgeIndex = jointKey & 1;

                    b2Joint joint = b2Array_Get(ref world.joints, jointId);

                    // Transfer the joint if it is in the static set
                    if (joint.setIndex == (int)b2SetType.b2_staticSet)
                    {
                        b2TransferJoint(world, awakeSet, staticSet, joint);
                    }
                    else if (joint.setIndex == (int)b2SetType.b2_awakeSet)
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
                        Debug.Assert(joint.setIndex == (int)b2SetType.b2_disabledSet);
                    }

                    jointKey = joint.edges[edgeIndex].nextKey;
                }

                // Recreate shape proxies in movable tree.
                b2Transform transform = b2GetBodyTransformQuick(world, body);
                int shapeId = body.headShapeId;
                while (shapeId != B2_NULL_INDEX)
                {
                    b2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                    shapeId = shape.nextShapeId;
                    b2DestroyShapeProxy(shape, world.broadPhase);
                    bool forcePairCreation = true;
                    b2BodyType proxyType = type;
                    b2CreateShapeProxy(shape, world.broadPhase, proxyType, transform, forcePairCreation);
                }
            }
            else if (type == b2BodyType.b2_staticBody)
            {
                // The body is going from dynamic/kinematic to static. It should be awake.
                Debug.Assert(body.setIndex == (int)b2SetType.b2_awakeSet);

                b2SolverSet staticSet = b2Array_Get(ref world.solverSets, (int)b2SetType.b2_staticSet);
                b2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)b2SetType.b2_awakeSet);

                // Transfer body to static set
                b2TransferBody(world, staticSet, awakeSet, body);

                // Remove body from island.
                b2RemoveBodyFromIsland(world, body);

                b2BodySim bodySim = b2Array_Get(ref staticSet.bodySims, body.localIndex);
                bodySim.isFast = false;

                // Maybe transfer joints to static set.
                int jointKey = body.headJointKey;
                while (jointKey != B2_NULL_INDEX)
                {
                    int jointId = jointKey >> 1;
                    int edgeIndex = jointKey & 1;

                    b2Joint joint = b2Array_Get(ref world.joints, jointId);
                    jointKey = joint.edges[edgeIndex].nextKey;

                    int otherEdgeIndex = edgeIndex ^ 1;
                    b2Body otherBody = b2Array_Get(ref world.bodies, joint.edges[otherEdgeIndex].bodyId);

                    // Skip disabled joint
                    if (joint.setIndex == (int)b2SetType.b2_disabledSet)
                    {
                        // Joint is disable, should be connected to a disabled body
                        Debug.Assert(otherBody.setIndex == (int)b2SetType.b2_disabledSet);
                        continue;
                    }

                    // Since the body was not static, the joint must be awake.
                    Debug.Assert(joint.setIndex == (int)b2SetType.b2_awakeSet);

                    // Only transfer joint to static set if both bodies are static.
                    if (otherBody.setIndex == (int)b2SetType.b2_staticSet)
                    {
                        b2TransferJoint(world, staticSet, awakeSet, joint);
                    }
                    else
                    {
                        // The other body must be awake.
                        Debug.Assert(otherBody.setIndex == (int)b2SetType.b2_awakeSet);

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
                b2Transform transform = b2GetBodyTransformQuick(world, body);
                int shapeId = body.headShapeId;
                while (shapeId != B2_NULL_INDEX)
                {
                    b2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                    shapeId = shape.nextShapeId;
                    b2DestroyShapeProxy(shape, world.broadPhase);
                    bool forcePairCreation = true;
                    b2CreateShapeProxy(shape, world.broadPhase, b2BodyType.b2_staticBody, transform, forcePairCreation);
                }
            }
            else
            {
                Debug.Assert(originalType == b2BodyType.b2_dynamicBody || originalType == b2BodyType.b2_kinematicBody);
                Debug.Assert(type == b2BodyType.b2_dynamicBody || type == b2BodyType.b2_kinematicBody);

                // Recreate shape proxies in static tree.
                b2Transform transform = b2GetBodyTransformQuick(world, body);
                int shapeId = body.headShapeId;
                while (shapeId != B2_NULL_INDEX)
                {
                    b2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                    shapeId = shape.nextShapeId;
                    b2DestroyShapeProxy(shape, world.broadPhase);
                    b2BodyType proxyType = type;
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

                    b2Joint joint = b2Array_Get(ref world.joints, jointId);
                    jointKey = joint.edges[edgeIndex].nextKey;

                    int otherEdgeIndex = edgeIndex ^ 1;
                    int otherBodyId = joint.edges[otherEdgeIndex].bodyId;
                    b2Body otherBody = b2Array_Get(ref world.bodies, otherBodyId);

                    if (otherBody.setIndex == (int)b2SetType.b2_disabledSet)
                    {
                        continue;
                    }

                    if (body.type == b2BodyType.b2_staticBody && otherBody.type == b2BodyType.b2_staticBody)
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

        public static void b2Body_SetName(b2BodyId bodyId, string name)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);

            if (!string.IsNullOrEmpty(name))
            {
                body.name = name;
            }
            else
            {
                body.name = "";
            }
        }

        public static string b2Body_GetName(b2BodyId bodyId)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            return body.name;
        }

        public static void b2Body_SetUserData(b2BodyId bodyId, object userData)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            body.userData = userData;
        }

        public static object b2Body_GetUserData(b2BodyId bodyId)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            return body.userData;
        }

        public static float b2Body_GetMass(b2BodyId bodyId)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            return body.mass;
        }

        public static float b2Body_GetRotationalInertia(b2BodyId bodyId)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            return body.inertia;
        }

        public static b2Vec2 b2Body_GetLocalCenterOfMass(b2BodyId bodyId)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            b2BodySim bodySim = b2GetBodySim(world, body);
            return bodySim.localCenter;
        }

        public static b2Vec2 b2Body_GetWorldCenterOfMass(b2BodyId bodyId)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            b2BodySim bodySim = b2GetBodySim(world, body);
            return bodySim.center;
        }

        public static void b2Body_SetMassData(b2BodyId bodyId, b2MassData massData)
        {
            Debug.Assert(b2IsValidFloat(massData.mass) && massData.mass >= 0.0f);
            Debug.Assert(b2IsValidFloat(massData.rotationalInertia) && massData.rotationalInertia >= 0.0f);
            Debug.Assert(b2IsValidVec2(massData.center));

            b2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return;
            }

            b2Body body = b2GetBodyFullId(world, bodyId);
            b2BodySim bodySim = b2GetBodySim(world, body);

            body.mass = massData.mass;
            body.inertia = massData.rotationalInertia;
            bodySim.localCenter = massData.center;

            b2Vec2 center = b2TransformPoint(ref bodySim.transform, massData.center);
            bodySim.center = center;
            bodySim.center0 = center;

            bodySim.invMass = body.mass > 0.0f ? 1.0f / body.mass : 0.0f;
            bodySim.invInertia = body.inertia > 0.0f ? 1.0f / body.inertia : 0.0f;
        }

        public static b2MassData b2Body_GetMassData(b2BodyId bodyId)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            b2BodySim bodySim = b2GetBodySim(world, body);
            b2MassData massData = new b2MassData(body.mass, bodySim.localCenter, body.inertia);
            return massData;
        }

        public static void b2Body_ApplyMassFromShapes(b2BodyId bodyId)
        {
            b2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return;
            }

            b2Body body = b2GetBodyFullId(world, bodyId);
            b2UpdateBodyMassData(world, body);
        }

        public static void b2Body_SetLinearDamping(b2BodyId bodyId, float linearDamping)
        {
            Debug.Assert(b2IsValidFloat(linearDamping) && linearDamping >= 0.0f);

            b2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return;
            }

            b2Body body = b2GetBodyFullId(world, bodyId);
            b2BodySim bodySim = b2GetBodySim(world, body);
            bodySim.linearDamping = linearDamping;
        }

        public static float b2Body_GetLinearDamping(b2BodyId bodyId)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            b2BodySim bodySim = b2GetBodySim(world, body);
            return bodySim.linearDamping;
        }

        public static void b2Body_SetAngularDamping(b2BodyId bodyId, float angularDamping)
        {
            Debug.Assert(b2IsValidFloat(angularDamping) && angularDamping >= 0.0f);

            b2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return;
            }

            b2Body body = b2GetBodyFullId(world, bodyId);
            b2BodySim bodySim = b2GetBodySim(world, body);
            bodySim.angularDamping = angularDamping;
        }

        public static float b2Body_GetAngularDamping(b2BodyId bodyId)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            b2BodySim bodySim = b2GetBodySim(world, body);
            return bodySim.angularDamping;
        }

        public static void b2Body_SetGravityScale(b2BodyId bodyId, float gravityScale)
        {
            Debug.Assert(b2Body_IsValid(bodyId));
            Debug.Assert(b2IsValidFloat(gravityScale));

            b2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return;
            }

            b2Body body = b2GetBodyFullId(world, bodyId);
            b2BodySim bodySim = b2GetBodySim(world, body);
            bodySim.gravityScale = gravityScale;
        }

        public static float b2Body_GetGravityScale(b2BodyId bodyId)
        {
            Debug.Assert(b2Body_IsValid(bodyId));
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            b2BodySim bodySim = b2GetBodySim(world, body);
            return bodySim.gravityScale;
        }

        public static bool b2Body_IsAwake(b2BodyId bodyId)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            return body.setIndex == (int)b2SetType.b2_awakeSet;
        }

        public static void b2Body_SetAwake(b2BodyId bodyId, bool awake)
        {
            b2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return;
            }

            b2Body body = b2GetBodyFullId(world, bodyId);

            if (awake && body.setIndex >= (int)b2SetType.b2_firstSleepingSet)
            {
                b2WakeBody(world, body);
            }
            else if (awake == false && body.setIndex == (int)b2SetType.b2_awakeSet)
            {
                b2Island island = b2Array_Get(ref world.islands, body.islandId);
                if (island.constraintRemoveCount > 0)
                {
                    // Must split the island before sleeping. This is expensive.
                    b2SplitIsland(world, body.islandId);
                }

                b2TrySleepIsland(world, body.islandId);
            }
        }

        public static bool b2Body_IsEnabled(b2BodyId bodyId)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            return body.setIndex != (int)b2SetType.b2_disabledSet;
        }

        public static bool b2Body_IsSleepEnabled(b2BodyId bodyId)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            return body.enableSleep;
        }

        public static void b2Body_SetSleepThreshold(b2BodyId bodyId, float sleepThreshold)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            body.sleepThreshold = sleepThreshold;
        }

        public static float b2Body_GetSleepThreshold(b2BodyId bodyId)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            return body.sleepThreshold;
        }

        public static void b2Body_EnableSleep(b2BodyId bodyId, bool enableSleep)
        {
            b2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return;
            }

            b2Body body = b2GetBodyFullId(world, bodyId);
            body.enableSleep = enableSleep;

            if (enableSleep == false)
            {
                b2WakeBody(world, body);
            }
        }

        // Disabling a body requires a lot of detailed bookkeeping, but it is a valuable feature.
        // The most challenging aspect that joints may connect to bodies that are not disabled.
        public static void b2Body_Disable(b2BodyId bodyId)
        {
            b2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return;
            }

            b2Body body = b2GetBodyFullId(world, bodyId);
            if (body.setIndex == (int)b2SetType.b2_disabledSet)
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
                b2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                shapeId = shape.nextShapeId;
                b2DestroyShapeProxy(shape, world.broadPhase);
            }

            // Transfer simulation data to disabled set
            b2SolverSet set = b2Array_Get(ref world.solverSets, body.setIndex);
            b2SolverSet disabledSet = b2Array_Get(ref world.solverSets, (int)b2SetType.b2_disabledSet);

            // Transfer body sim
            b2TransferBody(world, disabledSet, set, body);

            // Unlink joints and transfer
            int jointKey = body.headJointKey;
            while (jointKey != B2_NULL_INDEX)
            {
                int jointId = jointKey >> 1;
                int edgeIndex = jointKey & 1;

                b2Joint joint = b2Array_Get(ref world.joints, jointId);
                jointKey = joint.edges[edgeIndex].nextKey;

                // joint may already be disabled by other body
                if (joint.setIndex == (int)b2SetType.b2_disabledSet)
                {
                    continue;
                }

                Debug.Assert(joint.setIndex == set.setIndex || set.setIndex == (int)b2SetType.b2_staticSet);

                // Remove joint from island
                if (joint.islandId != B2_NULL_INDEX)
                {
                    b2UnlinkJoint(world, joint);
                }

                // Transfer joint to disabled set
                b2SolverSet jointSet = b2Array_Get(ref world.solverSets, joint.setIndex);
                b2TransferJoint(world, disabledSet, jointSet, joint);
            }

            b2ValidateConnectivity(world);
            b2ValidateSolverSets(world);
        }

        public static void b2Body_Enable(b2BodyId bodyId)
        {
            b2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return;
            }

            b2Body body = b2GetBodyFullId(world, bodyId);
            if (body.setIndex != (int)b2SetType.b2_disabledSet)
            {
                return;
            }

            b2SolverSet disabledSet = b2Array_Get(ref world.solverSets, (int)b2SetType.b2_disabledSet);
            int setId = body.type == b2BodyType.b2_staticBody ? (int)b2SetType.b2_staticSet : (int)b2SetType.b2_awakeSet;
            b2SolverSet targetSet = b2Array_Get(ref world.solverSets, setId);

            b2TransferBody(world, targetSet, disabledSet, body);

            b2Transform transform = b2GetBodyTransformQuick(world, body);

            // Add shapes to broad-phase
            b2BodyType proxyType = body.type;
            bool forcePairCreation = true;
            int shapeId = body.headShapeId;
            while (shapeId != B2_NULL_INDEX)
            {
                b2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                shapeId = shape.nextShapeId;

                b2CreateShapeProxy(shape, world.broadPhase, proxyType, transform, forcePairCreation);
            }

            if (setId != (int)b2SetType.b2_staticSet)
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

                b2Joint joint = b2Array_Get(ref world.joints, jointId);
                Debug.Assert(joint.setIndex == (int)b2SetType.b2_disabledSet);
                Debug.Assert(joint.islandId == B2_NULL_INDEX);

                jointKey = joint.edges[edgeIndex].nextKey;

                b2Body bodyA = b2Array_Get(ref world.bodies, joint.edges[0].bodyId);
                b2Body bodyB = b2Array_Get(ref world.bodies, joint.edges[1].bodyId);

                if (bodyA.setIndex == (int)b2SetType.b2_disabledSet || bodyB.setIndex == (int)b2SetType.b2_disabledSet)
                {
                    // one body is still disabled
                    continue;
                }

                // Transfer joint first
                int jointSetId;
                if (bodyA.setIndex == (int)b2SetType.b2_staticSet && bodyB.setIndex == (int)b2SetType.b2_staticSet)
                {
                    jointSetId = (int)b2SetType.b2_staticSet;
                }
                else if (bodyA.setIndex == (int)b2SetType.b2_staticSet)
                {
                    jointSetId = bodyB.setIndex;
                }
                else
                {
                    jointSetId = bodyA.setIndex;
                }

                b2SolverSet jointSet = b2Array_Get(ref world.solverSets, jointSetId);
                b2TransferJoint(world, jointSet, disabledSet, joint);

                // Now that the joint is in the correct set, I can link the joint in the island.
                if (jointSetId != (int)b2SetType.b2_staticSet)
                {
                    b2LinkJoint(world, joint, mergeIslands);
                }
            }

            // Now merge islands
            b2MergeAwakeIslands(world);

            b2ValidateSolverSets(world);
        }

        public static void b2Body_SetFixedRotation(b2BodyId bodyId, bool flag)
        {
            b2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return;
            }

            b2Body body = b2GetBodyFullId(world, bodyId);
            if (body.fixedRotation != flag)
            {
                body.fixedRotation = flag;

                b2BodyState state = b2GetBodyState(world, body);
                if (state != null)
                {
                    state.angularVelocity = 0.0f;
                }

                b2UpdateBodyMassData(world, body);
            }
        }

        public static bool b2Body_IsFixedRotation(b2BodyId bodyId)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            return body.fixedRotation;
        }

        public static void b2Body_SetBullet(b2BodyId bodyId, bool flag)
        {
            b2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return;
            }

            b2Body body = b2GetBodyFullId(world, bodyId);
            b2BodySim bodySim = b2GetBodySim(world, body);
            bodySim.isBullet = flag;
        }

        public static bool b2Body_IsBullet(b2BodyId bodyId)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            b2BodySim bodySim = b2GetBodySim(world, body);
            return bodySim.isBullet;
        }

        public static void b2Body_EnableContactEvents(b2BodyId bodyId, bool flag)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            int shapeId = body.headShapeId;
            while (shapeId != B2_NULL_INDEX)
            {
                b2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                shape.enableContactEvents = flag;
                shapeId = shape.nextShapeId;
            }
        }

        public static void b2Body_EnableHitEvents(b2BodyId bodyId, bool flag)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            int shapeId = body.headShapeId;
            while (shapeId != B2_NULL_INDEX)
            {
                b2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                shape.enableHitEvents = flag;
                shapeId = shape.nextShapeId;
            }
        }

        public static b2WorldId b2Body_GetWorld(b2BodyId bodyId)
        {
            b2World world = b2GetWorld(bodyId.world0);
            return new b2WorldId((ushort)(bodyId.world0 + 1), world.generation);
        }

        public static int b2Body_GetShapeCount(b2BodyId bodyId)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            return body.shapeCount;
        }

        public static int b2Body_GetShapes(b2BodyId bodyId, b2ShapeId[] shapeArray, int capacity)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            int shapeId = body.headShapeId;
            int shapeCount = 0;
            while (shapeId != B2_NULL_INDEX && shapeCount < capacity)
            {
                b2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                b2ShapeId id = new b2ShapeId(shape.id + 1, bodyId.world0, shape.generation);
                shapeArray[shapeCount] = id;
                shapeCount += 1;

                shapeId = shape.nextShapeId;
            }

            return shapeCount;
        }

        public static int b2Body_GetJointCount(b2BodyId bodyId)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            return body.jointCount;
        }

        public static int b2Body_GetJoints(b2BodyId bodyId, b2JointId[] jointArray, int capacity)
        {
            b2World world = b2GetWorld(bodyId.world0);
            b2Body body = b2GetBodyFullId(world, bodyId);
            int jointKey = body.headJointKey;

            int jointCount = 0;
            while (jointKey != B2_NULL_INDEX && jointCount < capacity)
            {
                int jointId = jointKey >> 1;
                int edgeIndex = jointKey & 1;

                b2Joint joint = b2Array_Get(ref world.joints, jointId);

                b2JointId id = new b2JointId(jointId + 1, bodyId.world0, joint.generation);
                jointArray[jointCount] = id;
                jointCount += 1;

                jointKey = joint.edges[edgeIndex].nextKey;
            }

            return jointCount;
        }

        public static bool b2ShouldBodiesCollide(b2World world, b2Body bodyA, b2Body bodyB)
        {
            if (bodyA.type != b2BodyType.b2_dynamicBody && bodyB.type != b2BodyType.b2_dynamicBody)
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

                b2Joint joint = b2Array_Get(ref world.joints, jointId);
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
