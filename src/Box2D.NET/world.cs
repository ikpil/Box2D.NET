// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using System.IO;
using Box2D.NET.Primitives;
using static Box2D.NET.table;
using static Box2D.NET.array;
using static Box2D.NET.dynamic_tree;
using static Box2D.NET.core;
using static Box2D.NET.constants;
using static Box2D.NET.contact;
using static Box2D.NET.math_function;
using static Box2D.NET.shape;
using static Box2D.NET.solver;
using static Box2D.NET.body;
using static Box2D.NET.joint;
using static Box2D.NET.id_pool;
using static Box2D.NET.arena_allocator;
using static Box2D.NET.board_phase;
using static Box2D.NET.geometry;
using static Box2D.NET.distance;
using static Box2D.NET.constraint_graph;
using static Box2D.NET.bitset;
using static Box2D.NET.solver_set;
using static Box2D.NET.aabb;
using static Box2D.NET.ctz;
using static Box2D.NET.island;
using static Box2D.NET.timer;
using static Box2D.NET.sensor;

namespace Box2D.NET
{
    public static class world
    {
        private static readonly b2World[] b2_worlds = b2AllocWorlds(B2_MAX_WORLDS);

        private static b2World[] b2AllocWorlds(int maxWorld)
        {
            Debug.Assert(B2_MAX_WORLDS > 0, "must be 1 or more");
            var worlds = new b2World[maxWorld];
            for (int i = 0; i < maxWorld; ++i)
            {
                worlds[i] = new b2World();
            }

            return worlds;
        }


        public static b2World b2GetWorldFromId(b2WorldId id)
        {
            Debug.Assert(1 <= id.index1 && id.index1 <= B2_MAX_WORLDS);
            b2World world = b2_worlds[id.index1 - 1];
            Debug.Assert(id.index1 == world.worldId + 1);
            Debug.Assert(id.generation == world.generation);
            return world;
        }

        public static b2World b2GetWorld(int index)
        {
            Debug.Assert(0 <= index && index < B2_MAX_WORLDS);
            b2World world = b2_worlds[index];
            Debug.Assert(world.worldId == index);
            return world;
        }

        public static b2World b2GetWorldLocked(int index)
        {
            Debug.Assert(0 <= index && index < B2_MAX_WORLDS);
            b2World world = b2_worlds[index];
            Debug.Assert(world.worldId == index);
            if (world.locked)
            {
                Debug.Assert(false);
                return null;
            }

            return world;
        }

        public static object b2DefaultAddTaskFcn(b2TaskCallback task, int count, int minRange, object taskContext, object userContext)
        {
            B2_UNUSED(minRange, userContext);
            task(0, count, 0, taskContext);
            return null;
        }

        public static void b2DefaultFinishTaskFcn(object userTask, object userContext)
        {
            B2_UNUSED(userTask, userContext);
        }

        public static float b2DefaultFrictionCallback(float frictionA, int materialA, float frictionB, int materialB)
        {
            B2_UNUSED(materialA, materialB);
            return MathF.Sqrt(frictionA * frictionB);
        }

        public static float b2DefaultRestitutionCallback(float restitutionA, int materialA, float restitutionB, int materialB)
        {
            B2_UNUSED(materialA, materialB);
            return b2MaxFloat(restitutionA, restitutionB);
        }

        public static b2WorldId b2CreateWorld(b2WorldDef def)
        {
            Debug.Assert(B2_MAX_WORLDS < ushort.MaxValue, "B2_MAX_WORLDS limit exceeded");
            B2_CHECK_DEF(def);

            int worldId = B2_NULL_INDEX;
            for (int i = 0; i < B2_MAX_WORLDS; ++i)
            {
                if (b2_worlds[i].inUse == false)
                {
                    worldId = i;
                    break;
                }
            }

            if (worldId == B2_NULL_INDEX)
            {
                return new b2WorldId(0, 0);
            }

            b2InitializeContactRegisters();

            b2World world = b2_worlds[worldId];
            ushort generation = world.generation;

            //*world = ( b2World ){ 0 };
            world.Clear();

            world.worldId = (ushort)worldId;
            world.generation = generation;
            world.inUse = true;

            world.stackAllocator = b2CreateArenaAllocator(2048);
            b2CreateBroadPhase(ref world.broadPhase);
            b2CreateGraph(ref world.constraintGraph, 16);

            // pools
            world.bodyIdPool = b2CreateIdPool();
            world.bodies = b2Array_Create<b2Body>(16);
            world.solverSets = b2Array_Create<b2SolverSet>(8);

            // add empty static, active, and disabled body sets
            world.solverSetIdPool = b2CreateIdPool();
            b2SolverSet set = null;

            // static set
            set = b2CreateSolverSet(world);
            set.setIndex = b2AllocId(world.solverSetIdPool);
            b2Array_Push(ref world.solverSets, set);
            Debug.Assert(world.solverSets.data[(int)b2SetType.b2_staticSet].setIndex == (int)b2SetType.b2_staticSet);

            // disabled set
            set = b2CreateSolverSet(world);
            set.setIndex = b2AllocId(world.solverSetIdPool);
            b2Array_Push(ref world.solverSets, set);
            Debug.Assert(world.solverSets.data[(int)b2SetType.b2_disabledSet].setIndex == (int)b2SetType.b2_disabledSet);

            // awake set
            set = b2CreateSolverSet(world);
            set.setIndex = b2AllocId(world.solverSetIdPool);
            b2Array_Push(ref world.solverSets, set);
            Debug.Assert(world.solverSets.data[(int)b2SetType.b2_awakeSet].setIndex == (int)b2SetType.b2_awakeSet);

            world.shapeIdPool = b2CreateIdPool();
            world.shapes = b2Array_Create<b2Shape>(16);

            world.chainIdPool = b2CreateIdPool();
            world.chainShapes = b2Array_Create<b2ChainShape>(4);

            world.contactIdPool = b2CreateIdPool();
            world.contacts = b2Array_Create<b2Contact>(16);

            world.jointIdPool = b2CreateIdPool();
            world.joints = b2Array_Create<b2Joint>(16);

            world.islandIdPool = b2CreateIdPool();
            world.islands = b2Array_Create<b2Island>(8);

            world.sensors = b2Array_Create<b2Sensor>(4);

            world.bodyMoveEvents = b2Array_Create<b2BodyMoveEvent>(4);
            world.sensorBeginEvents = b2Array_Create<b2SensorBeginTouchEvent>(4);
            world.sensorEndEvents[0] = b2Array_Create<b2SensorEndTouchEvent>(4);
            world.sensorEndEvents[1] = b2Array_Create<b2SensorEndTouchEvent>(4);
            world.contactBeginEvents = b2Array_Create<b2ContactBeginTouchEvent>(4);
            world.contactEndEvents[0] = b2Array_Create<b2ContactEndTouchEvent>(4);
            world.contactEndEvents[1] = b2Array_Create<b2ContactEndTouchEvent>(4);
            world.contactHitEvents = b2Array_Create<b2ContactHitEvent>(4);
            world.endEventArrayIndex = 0;

            world.stepIndex = 0;
            world.splitIslandId = B2_NULL_INDEX;
            world.activeTaskCount = 0;
            world.taskCount = 0;
            world.gravity = def.gravity;
            world.hitEventThreshold = def.hitEventThreshold;
            world.restitutionThreshold = def.restitutionThreshold;
            world.maxLinearSpeed = def.maximumLinearSpeed;
            world.contactMaxPushSpeed = def.contactPushMaxSpeed;
            world.contactHertz = def.contactHertz;
            world.contactDampingRatio = def.contactDampingRatio;
            world.jointHertz = def.jointHertz;
            world.jointDampingRatio = def.jointDampingRatio;

            if (def.frictionCallback == null)
            {
                world.frictionCallback = b2DefaultFrictionCallback;
            }
            else
            {
                world.frictionCallback = def.frictionCallback;
            }

            if (def.restitutionCallback == null)
            {
                world.restitutionCallback = b2DefaultRestitutionCallback;
            }
            else
            {
                world.restitutionCallback = def.restitutionCallback;
            }

            // @ikpil, new profile
            world.profile = new b2Profile();

            world.enableSleep = def.enableSleep;
            world.locked = false;
            world.enableWarmStarting = true;
            world.enableContinuous = def.enableContinuous;
            world.enableSpeculative = true;
            world.userTreeTask = null;
            world.userData = def.userData;

            if (def.workerCount > 0 && def.enqueueTask != null && def.finishTask != null)
            {
                world.workerCount = b2MinInt(def.workerCount, B2_MAX_WORKERS);
                world.enqueueTaskFcn = def.enqueueTask;
                world.finishTaskFcn = def.finishTask;
                world.userTaskContext = def.userTaskContext;
            }
            else
            {
                world.workerCount = 1;
                world.enqueueTaskFcn = b2DefaultAddTaskFcn;
                world.finishTaskFcn = b2DefaultFinishTaskFcn;
                world.userTaskContext = null;
            }

            world.taskContexts = b2Array_Create<b2TaskContext>(world.workerCount);
            b2Array_Resize(ref world.taskContexts, world.workerCount);

            world.sensorTaskContexts = b2Array_Create<b2SensorTaskContext>(world.workerCount);
            b2Array_Resize(ref world.sensorTaskContexts, world.workerCount);

            for (int i = 0; i < world.workerCount; ++i)
            {
                world.taskContexts.data[i].contactStateBitSet = b2CreateBitSet(1024);
                world.taskContexts.data[i].enlargedSimBitSet = b2CreateBitSet(256);
                world.taskContexts.data[i].awakeIslandBitSet = b2CreateBitSet(256);

                world.sensorTaskContexts.data[i].eventBits = b2CreateBitSet(128);
            }

            world.debugBodySet = b2CreateBitSet(256);
            world.debugJointSet = b2CreateBitSet(256);
            world.debugContactSet = b2CreateBitSet(256);

            // add one to worldId so that 0 represents a null b2WorldId
            return new b2WorldId((ushort)(worldId + 1), world.generation);
        }

        public static void b2DestroyWorld(b2WorldId worldId)
        {
            b2World world = b2GetWorldFromId(worldId);

            b2DestroyBitSet(world.debugBodySet);
            b2DestroyBitSet(world.debugJointSet);
            b2DestroyBitSet(world.debugContactSet);

            for (int i = 0; i < world.workerCount; ++i)
            {
                b2DestroyBitSet(world.taskContexts.data[i].contactStateBitSet);
                b2DestroyBitSet(world.taskContexts.data[i].enlargedSimBitSet);
                b2DestroyBitSet(world.taskContexts.data[i].awakeIslandBitSet);

                b2DestroyBitSet(world.sensorTaskContexts.data[i].eventBits);
            }

            b2Array_Destroy(ref world.taskContexts);
            b2Array_Destroy(ref world.sensorTaskContexts);

            b2Array_Destroy(ref world.bodyMoveEvents);
            b2Array_Destroy(ref world.sensorBeginEvents);
            b2Array_Destroy(ref world.sensorEndEvents[0]);
            b2Array_Destroy(ref world.sensorEndEvents[1]);
            b2Array_Destroy(ref world.contactBeginEvents);
            b2Array_Destroy(ref world.contactEndEvents[0]);
            b2Array_Destroy(ref world.contactEndEvents[1]);
            b2Array_Destroy(ref world.contactHitEvents);

            int chainCapacity = world.chainShapes.count;
            for (int i = 0; i < chainCapacity; ++i)
            {
                b2ChainShape chain = world.chainShapes.data[i];
                if (chain.id != B2_NULL_INDEX)
                {
                    b2FreeChainData(chain);
                }
                else
                {
                    Debug.Assert(chain.shapeIndices == null);
                    Debug.Assert(chain.materials == null);
                }
            }

            int sensorCount = world.sensors.count;
            for (int i = 0; i < sensorCount; ++i)
            {
                b2Array_Destroy(ref world.sensors.data[i].overlaps1);
                b2Array_Destroy(ref world.sensors.data[i].overlaps2);
            }

            b2Array_Destroy(ref world.sensors);

            b2Array_Destroy(ref world.bodies);
            b2Array_Destroy(ref world.shapes);
            b2Array_Destroy(ref world.chainShapes);
            b2Array_Destroy(ref world.contacts);
            b2Array_Destroy(ref world.joints);
            b2Array_Destroy(ref world.islands);

            // Destroy solver sets
            int setCapacity = world.solverSets.count;
            for (int i = 0; i < setCapacity; ++i)
            {
                b2SolverSet set = world.solverSets.data[i];
                if (set.setIndex != B2_NULL_INDEX)
                {
                    b2DestroySolverSet(world, i);
                }
            }

            b2Array_Destroy(ref world.solverSets);

            b2DestroyGraph(world.constraintGraph);
            b2DestroyBroadPhase(world.broadPhase);

            b2DestroyIdPool(ref world.bodyIdPool);
            b2DestroyIdPool(ref world.shapeIdPool);
            b2DestroyIdPool(ref world.chainIdPool);
            b2DestroyIdPool(ref world.contactIdPool);
            b2DestroyIdPool(ref world.jointIdPool);
            b2DestroyIdPool(ref world.islandIdPool);
            b2DestroyIdPool(ref world.solverSetIdPool);

            b2DestroyArenaAllocator(world.stackAllocator);

            // Wipe world but preserve generation
            ushort generation = world.generation;
            world.Clear();
            world.worldId = 0;
            world.generation = (ushort)(generation + 1);
        }

        public static void b2CollideTask(int startIndex, int endIndex, uint threadIndex, object context)
        {
            b2TracyCZoneNC(b2TracyCZone.collide_task, "Collide", b2HexColor.b2_colorDodgerBlue, true);

            b2StepContext stepContext = context as b2StepContext;
            b2World world = stepContext.world;
            Debug.Assert((int)threadIndex < world.workerCount);
            b2TaskContext taskContext = world.taskContexts.data[threadIndex];
            ArraySegment<b2ContactSim> contactSims = stepContext.contacts;
            b2Shape[] shapes = world.shapes.data;
            b2Body[] bodies = world.bodies.data;

            Debug.Assert(startIndex < endIndex);

            for (int i = startIndex; i < endIndex; ++i)
            {
                b2ContactSim contactSim = contactSims[i];

                int contactId = contactSim.contactId;

                b2Shape shapeA = shapes[contactSim.shapeIdA];
                b2Shape shapeB = shapes[contactSim.shapeIdB];

                // Do proxies still overlap?
                bool overlap = b2AABB_Overlaps(shapeA.fatAABB, shapeB.fatAABB);
                if (overlap == false)
                {
                    contactSim.simFlags |= (uint)b2ContactSimFlags.b2_simDisjoint;
                    contactSim.simFlags &= ~(uint)b2ContactSimFlags.b2_simTouchingFlag;
                    b2SetBit(taskContext.contactStateBitSet, contactId);
                }
                else
                {
                    bool wasTouching = 0 != (contactSim.simFlags & (uint)b2ContactSimFlags.b2_simTouchingFlag);

                    // Update contact respecting shape/body order (A,B)
                    b2Body bodyA = bodies[shapeA.bodyId];
                    b2Body bodyB = bodies[shapeB.bodyId];
                    b2BodySim bodySimA = b2GetBodySim(world, bodyA);
                    b2BodySim bodySimB = b2GetBodySim(world, bodyB);

                    // avoid cache misses in b2PrepareContactsTask
                    contactSim.bodySimIndexA = bodyA.setIndex == (int)b2SetType.b2_awakeSet ? bodyA.localIndex : B2_NULL_INDEX;
                    contactSim.invMassA = bodySimA.invMass;
                    contactSim.invIA = bodySimA.invInertia;

                    contactSim.bodySimIndexB = bodyB.setIndex == (int)b2SetType.b2_awakeSet ? bodyB.localIndex : B2_NULL_INDEX;
                    contactSim.invMassB = bodySimB.invMass;
                    contactSim.invIB = bodySimB.invInertia;

                    b2Transform transformA = bodySimA.transform;
                    b2Transform transformB = bodySimB.transform;

                    b2Vec2 centerOffsetA = b2RotateVector(transformA.q, bodySimA.localCenter);
                    b2Vec2 centerOffsetB = b2RotateVector(transformB.q, bodySimB.localCenter);

                    // This updates solid contacts and sensors
                    bool touching =
                        b2UpdateContact(world, contactSim, shapeA, transformA, centerOffsetA, shapeB, transformB, centerOffsetB);

                    // State changes that affect island connectivity. Also affects contact and sensor events.
                    if (touching == true && wasTouching == false)
                    {
                        contactSim.simFlags |= (uint)b2ContactSimFlags.b2_simStartedTouching;
                        b2SetBit(taskContext.contactStateBitSet, contactId);
                    }
                    else if (touching == false && wasTouching == true)
                    {
                        contactSim.simFlags |= (uint)b2ContactSimFlags.b2_simStoppedTouching;
                        b2SetBit(taskContext.contactStateBitSet, contactId);
                    }
                }
            }

            b2TracyCZoneEnd(b2TracyCZone.collide_task);
        }

        public static void b2UpdateTreesTask(int startIndex, int endIndex, uint threadIndex, object context)
        {
            B2_UNUSED(startIndex);
            B2_UNUSED(endIndex);
            B2_UNUSED(threadIndex);

            b2TracyCZoneNC(b2TracyCZone.tree_task, "Rebuild BVH", b2HexColor.b2_colorFireBrick, true);

            b2World world = context as b2World;
            b2BroadPhase_RebuildTrees(world.broadPhase);

            b2TracyCZoneEnd(b2TracyCZone.tree_task);
        }

        public static void b2AddNonTouchingContact(b2World world, b2Contact contact, b2ContactSim contactSim)
        {
            Debug.Assert(contact.setIndex == (int)b2SetType.b2_awakeSet);
            b2SolverSet set = b2Array_Get(ref world.solverSets, (int)b2SetType.b2_awakeSet);
            contact.colorIndex = B2_NULL_INDEX;
            contact.localIndex = set.contactSims.count;

            ref b2ContactSim newContactSim = ref b2Array_Add(ref set.contactSims);
            //memcpy( newContactSim, contactSim, sizeof( b2ContactSim ) );
            newContactSim.CopyFrom(contactSim);
        }

        public static void b2RemoveNonTouchingContact(b2World world, int setIndex, int localIndex)
        {
            b2SolverSet set = b2Array_Get(ref world.solverSets, setIndex);
            int movedIndex = b2Array_RemoveSwap(ref set.contactSims, localIndex);
            if (movedIndex != B2_NULL_INDEX)
            {
                b2ContactSim movedContactSim = set.contactSims.data[localIndex];
                b2Contact movedContact = b2Array_Get(ref world.contacts, movedContactSim.contactId);
                Debug.Assert(movedContact.setIndex == setIndex);
                Debug.Assert(movedContact.localIndex == movedIndex);
                Debug.Assert(movedContact.colorIndex == B2_NULL_INDEX);
                movedContact.localIndex = localIndex;
            }
        }

// Narrow-phase collision
        public static void b2Collide(b2StepContext context)
        {
            b2World world = context.world;

            Debug.Assert(world.workerCount > 0);

            b2TracyCZoneNC(b2TracyCZone.collide, "Narrow Phase", b2HexColor.b2_colorDodgerBlue, true);

            // Task that can be done in parallel with the narrow-phase
            // - rebuild the collision tree for dynamic and kinematic bodies to keep their query performance good
            // todo_erin move this to start when contacts are being created
            world.userTreeTask = world.enqueueTaskFcn(b2UpdateTreesTask, 1, 1, world, world.userTaskContext);
            world.taskCount += 1;
            world.activeTaskCount += world.userTreeTask == null ? 0 : 1;

            // gather contacts into a single array for easier parallel-for
            int contactCount = 0;
            b2GraphColor[] graphColors = world.constraintGraph.colors;
            for (int i = 0; i < B2_GRAPH_COLOR_COUNT; ++i)
            {
                contactCount += graphColors[i].contactSims.count;
            }

            int nonTouchingCount = world.solverSets.data[(int)b2SetType.b2_awakeSet].contactSims.count;
            contactCount += nonTouchingCount;

            if (contactCount == 0)
            {
                b2TracyCZoneEnd(b2TracyCZone.collide);
                return;
            }

            ArraySegment<b2ContactSim> contactSims =
                b2AllocateArenaItem<b2ContactSim>(world.stackAllocator, contactCount, "contacts");

            int contactIndex = 0;
            for (int i = 0; i < B2_GRAPH_COLOR_COUNT; ++i)
            {
                b2GraphColor color = graphColors[i];
                int count = color.contactSims.count;
                b2ContactSim[] @base = color.contactSims.data;
                for (int j = 0; j < count; ++j)
                {
                    contactSims[contactIndex] = @base[j];
                    contactIndex += 1;
                }
            }

            {
                b2ContactSim[] @base = world.solverSets.data[(int)b2SetType.b2_awakeSet].contactSims.data;
                for (int i = 0; i < nonTouchingCount; ++i)
                {
                    contactSims[contactIndex] = @base[i];
                    contactIndex += 1;
                }
            }

            Debug.Assert(contactIndex == contactCount);

            context.contacts = contactSims;

            // Contact bit set on ids because contact pointers are unstable as they move between touching and not touching.
            int contactIdCapacity = b2GetIdCapacity(world.contactIdPool);
            for (int i = 0; i < world.workerCount; ++i)
            {
                b2SetBitCountAndClear(world.taskContexts.data[i].contactStateBitSet, contactIdCapacity);
            }

            // Task should take at least 40us on a 4GHz CPU (10K cycles)
            int minRange = 64;
            object userCollideTask = world.enqueueTaskFcn(b2CollideTask, contactCount, minRange, context, world.userTaskContext);
            world.taskCount += 1;
            if (userCollideTask != null)
            {
                world.finishTaskFcn(userCollideTask, world.userTaskContext);
            }

            b2FreeArenaItem(world.stackAllocator, contactSims);
            context.contacts = null;
            contactSims = null;

            // Serially update contact state
            // todo_erin bring this zone together with island merge
            b2TracyCZoneNC(b2TracyCZone.contact_state, "Contact State", b2HexColor.b2_colorLightSlateGray, true);

            // Bitwise OR all contact bits
            b2BitSet bitSet = world.taskContexts.data[0].contactStateBitSet;
            for (int i = 1; i < world.workerCount; ++i)
            {
                b2InPlaceUnion(bitSet, world.taskContexts.data[i].contactStateBitSet);
            }

            b2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)b2SetType.b2_awakeSet);

            int endEventArrayIndex = world.endEventArrayIndex;

            b2Shape[] shapes = world.shapes.data;
            ushort worldId = world.worldId;

            // Process contact state changes. Iterate over set bits
            for (uint k = 0; k < bitSet.blockCount; ++k)
            {
                ulong bits = bitSet.bits[k];
                while (bits != 0)
                {
                    uint ctz = b2CTZ64(bits);
                    int contactId = (int)(64 * k + ctz);

                    b2Contact contact = b2Array_Get(ref world.contacts, contactId);
                    Debug.Assert(contact.setIndex == (int)b2SetType.b2_awakeSet);

                    int colorIndex = contact.colorIndex;
                    int localIndex = contact.localIndex;

                    b2ContactSim contactSim = null;
                    if (colorIndex != B2_NULL_INDEX)
                    {
                        // contact lives in constraint graph
                        Debug.Assert(0 <= colorIndex && colorIndex < B2_GRAPH_COLOR_COUNT);
                        b2GraphColor color = graphColors[colorIndex];
                        contactSim = b2Array_Get(ref color.contactSims, localIndex);
                    }
                    else
                    {
                        contactSim = b2Array_Get(ref awakeSet.contactSims, localIndex);
                    }

                    b2Shape shapeA = shapes[contact.shapeIdA];
                    b2Shape shapeB = shapes[contact.shapeIdB];
                    b2ShapeId shapeIdA = new b2ShapeId(shapeA.id + 1, worldId, shapeA.generation);
                    b2ShapeId shapeIdB = new b2ShapeId(shapeB.id + 1, worldId, shapeB.generation);
                    uint flags = contact.flags;
                    uint simFlags = contactSim.simFlags;

                    if (0 != (simFlags & (uint)b2ContactSimFlags.b2_simDisjoint))
                    {
                        // Bounding boxes no longer overlap
                        b2DestroyContact(world, contact, false);
                        contact = null;
                        contactSim = null;
                    }
                    else if (0 != (simFlags & (uint)b2ContactSimFlags.b2_simStartedTouching))
                    {
                        Debug.Assert(contact.islandId == B2_NULL_INDEX);
                        // Contact is solid
                        if (0 != (flags & (uint)b2ContactFlags.b2_contactEnableContactEvents))
                        {
                            b2ContactBeginTouchEvent @event = new b2ContactBeginTouchEvent(shapeIdA, shapeIdB, ref contactSim.manifold);
                            b2Array_Push(ref world.contactBeginEvents, @event);
                        }

                        Debug.Assert(contactSim.manifold.pointCount > 0);
                        Debug.Assert(contact.setIndex == (int)b2SetType.b2_awakeSet);

                        // Link first because this wakes colliding bodies and ensures the body sims
                        // are in the correct place.
                        contact.flags |= (uint)b2ContactFlags.b2_contactTouchingFlag;
                        b2LinkContact(world, contact);

                        // Make sure these didn't change
                        Debug.Assert(contact.colorIndex == B2_NULL_INDEX);
                        Debug.Assert(contact.localIndex == localIndex);

                        // Contact sim pointer may have become orphaned due to awake set growth,
                        // so I just need to refresh it.
                        contactSim = b2Array_Get(ref awakeSet.contactSims, localIndex);

                        contactSim.simFlags &= ~(uint)b2ContactSimFlags.b2_simStartedTouching;

                        b2AddContactToGraph(world, contactSim, contact);
                        b2RemoveNonTouchingContact(world, (int)b2SetType.b2_awakeSet, localIndex);
                        contactSim = null;
                    }
                    else if (0 != (simFlags & (uint)b2ContactSimFlags.b2_simStoppedTouching))
                    {
                        contactSim.simFlags &= ~(uint)b2ContactSimFlags.b2_simStoppedTouching;

                        // Contact is solid
                        contact.flags &= ~(uint)b2ContactFlags.b2_contactTouchingFlag;

                        if (0 != (contact.flags & (uint)b2ContactFlags.b2_contactEnableContactEvents))
                        {
                            b2ContactEndTouchEvent @event = new b2ContactEndTouchEvent(shapeIdA, shapeIdB);
                            b2Array_Push(ref world.contactEndEvents[endEventArrayIndex], @event);
                        }

                        Debug.Assert(contactSim.manifold.pointCount == 0);

                        b2UnlinkContact(world, contact);
                        int bodyIdA = contact.edges[0].bodyId;
                        int bodyIdB = contact.edges[1].bodyId;

                        b2AddNonTouchingContact(world, contact, contactSim);
                        b2RemoveContactFromGraph(world, bodyIdA, bodyIdB, colorIndex, localIndex);
                        contact = null;
                        contactSim = null;
                    }

                    // Clear the smallest set bit
                    bits = bits & (bits - 1);
                }
            }

            b2ValidateSolverSets(world);
            b2ValidateContacts(world);

            b2TracyCZoneEnd(b2TracyCZone.contact_state);
            b2TracyCZoneEnd(b2TracyCZone.collide);
        }

        public static void b2World_Step(b2WorldId worldId, float timeStep, int subStepCount)
        {
            b2World world = b2GetWorldFromId(worldId);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return;
            }

            // Prepare to capture events
            // Ensure user does not access stale data if there is an early return
            b2Array_Clear(ref world.bodyMoveEvents);
            b2Array_Clear(ref world.sensorBeginEvents);
            b2Array_Clear(ref world.contactBeginEvents);
            b2Array_Clear(ref world.contactHitEvents);

            // world.profile = ( b2Profile ){ 0 };
            world.profile.Clear();

            if (timeStep == 0.0f)
            {
                // Swap end event array buffers
                world.endEventArrayIndex = 1 - world.endEventArrayIndex;
                b2Array_Clear(ref world.sensorEndEvents[world.endEventArrayIndex]);
                b2Array_Clear(ref world.contactEndEvents[world.endEventArrayIndex]);

                // todo_erin would be useful to still process collision while paused
                return;
            }

            b2TracyCZoneNC(b2TracyCZone.world_step, "Step", b2HexColor.b2_colorBox2DGreen, true);

            world.locked = true;
            world.activeTaskCount = 0;
            world.taskCount = 0;

            ulong stepTicks = b2GetTicks();

            // Update collision pairs and create contacts
            {
                ulong pairTicks = b2GetTicks();
                b2UpdateBroadPhasePairs(world);
                world.profile.pairs = b2GetMilliseconds(pairTicks);
            }

            b2StepContext context = new b2StepContext();
            context.world = world;
            context.dt = timeStep;
            context.subStepCount = b2MaxInt(1, subStepCount);

            if (timeStep > 0.0f)
            {
                context.inv_dt = 1.0f / timeStep;
                context.h = timeStep / context.subStepCount;
                context.inv_h = context.subStepCount * context.inv_dt;
            }
            else
            {
                context.inv_dt = 0.0f;
                context.h = 0.0f;
                context.inv_h = 0.0f;
            }

            world.inv_h = context.inv_h;

            // Hertz values get reduced for large time steps
            float contactHertz = b2MinFloat(world.contactHertz, 0.25f * context.inv_h);
            float jointHertz = b2MinFloat(world.jointHertz, 0.125f * context.inv_h);

            context.contactSoftness = b2MakeSoft(contactHertz, world.contactDampingRatio, context.h);
            context.staticSoftness = b2MakeSoft(2.0f * contactHertz, world.contactDampingRatio, context.h);
            context.jointSoftness = b2MakeSoft(jointHertz, world.jointDampingRatio, context.h);

            context.restitutionThreshold = world.restitutionThreshold;
            context.maxLinearVelocity = world.maxLinearSpeed;
            context.enableWarmStarting = world.enableWarmStarting;

            // Update contacts
            {
                ulong collideTicks = b2GetTicks();
                b2Collide(context);
                world.profile.collide = b2GetMilliseconds(collideTicks);
            }

            // Integrate velocities, solve velocity constraints, and integrate positions.
            if (context.dt > 0.0f)
            {
                ulong solveTicks = b2GetTicks();
                b2Solve(world, context);
                world.profile.solve = b2GetMilliseconds(solveTicks);
            }

            // Update sensors
            {
                ulong sensorTicks = b2GetTicks();
                b2OverlapSensors(world);
                world.profile.sensors = b2GetMilliseconds(sensorTicks);
            }

            world.profile.step = b2GetMilliseconds(stepTicks);

            Debug.Assert(b2GetArenaAllocation(world.stackAllocator) == 0);

            // Ensure stack is large enough
            b2GrowArena(world.stackAllocator);

            // Make sure all tasks that were started were also finished
            Debug.Assert(world.activeTaskCount == 0);

            b2TracyCZoneEnd(b2TracyCZone.world_step);

            // Swap end event array buffers
            world.endEventArrayIndex = 1 - world.endEventArrayIndex;
            b2Array_Clear(ref world.sensorEndEvents[world.endEventArrayIndex]);
            b2Array_Clear(ref world.contactEndEvents[world.endEventArrayIndex]);
            world.locked = false;
        }

        public static void b2DrawShape(b2DebugDraw draw, b2Shape shape, b2Transform xf, b2HexColor color)
        {
            switch (shape.type)
            {
                case b2ShapeType.b2_capsuleShape:
                {
                    b2Capsule capsule = shape.capsule;
                    b2Vec2 p1 = b2TransformPoint(xf, capsule.center1);
                    b2Vec2 p2 = b2TransformPoint(xf, capsule.center2);
                    draw.DrawSolidCapsule(p1, p2, capsule.radius, color, draw.context);
                }
                    break;

                case b2ShapeType.b2_circleShape:
                {
                    b2Circle circle = shape.circle;
                    xf.p = b2TransformPoint(xf, circle.center);
                    draw.DrawSolidCircle(xf, circle.radius, color, draw.context);
                }
                    break;

                case b2ShapeType.b2_polygonShape:
                {
                    b2Polygon poly = shape.polygon;
                    draw.DrawSolidPolygon(xf, poly.vertices, poly.count, poly.radius, color, draw.context);
                }
                    break;

                case b2ShapeType.b2_segmentShape:
                {
                    b2Segment segment = shape.segment;
                    b2Vec2 p1 = b2TransformPoint(xf, segment.point1);
                    b2Vec2 p2 = b2TransformPoint(xf, segment.point2);
                    draw.DrawSegment(p1, p2, color, draw.context);
                }
                    break;

                case b2ShapeType.b2_chainSegmentShape:
                {
                    b2Segment segment = shape.chainSegment.segment;
                    b2Vec2 p1 = b2TransformPoint(xf, segment.point1);
                    b2Vec2 p2 = b2TransformPoint(xf, segment.point2);
                    draw.DrawSegment(p1, p2, color, draw.context);
                    draw.DrawPoint(p2, 4.0f, color, draw.context);
                    draw.DrawSegment(p1, b2Lerp(p1, p2, 0.1f), b2HexColor.b2_colorPaleGreen, draw.context);
                }
                    break;

                default:
                    break;
            }
        }


        public static bool DrawQueryCallback(int proxyId, int shapeId, object context)
        {
            B2_UNUSED(proxyId);

            DrawContext drawContext = context as DrawContext;
            b2World world = drawContext.world;
            b2DebugDraw draw = drawContext.draw;

            b2Shape shape = b2Array_Get(ref world.shapes, shapeId);
            Debug.Assert(shape.id == shapeId);

            b2SetBit(world.debugBodySet, shape.bodyId);

            if (draw.drawShapes)
            {
                b2Body body = b2Array_Get(ref world.bodies, shape.bodyId);
                b2BodySim bodySim = b2GetBodySim(world, body);

                b2HexColor color;

                if (shape.customColor != 0)
                {
                    color = (b2HexColor)shape.customColor;
                }
                else if (body.type == b2BodyType.b2_dynamicBody && body.mass == 0.0f)
                {
                    // Bad body
                    color = b2HexColor.b2_colorRed;
                }
                else if (body.setIndex == (int)b2SetType.b2_disabledSet)
                {
                    color = b2HexColor.b2_colorSlateGray;
                }
                else if (shape.sensorIndex != B2_NULL_INDEX)
                {
                    color = b2HexColor.b2_colorWheat;
                }
                else if (bodySim.isBullet && body.setIndex == (int)b2SetType.b2_awakeSet)
                {
                    color = b2HexColor.b2_colorTurquoise;
                }
                else if (body.isSpeedCapped)
                {
                    color = b2HexColor.b2_colorYellow;
                }
                else if (bodySim.isFast)
                {
                    color = b2HexColor.b2_colorSalmon;
                }
                else if (body.type == b2BodyType.b2_staticBody)
                {
                    color = b2HexColor.b2_colorPaleGreen;
                }
                else if (body.type == b2BodyType.b2_kinematicBody)
                {
                    color = b2HexColor.b2_colorRoyalBlue;
                }
                else if (body.setIndex == (int)b2SetType.b2_awakeSet)
                {
                    color = b2HexColor.b2_colorPink;
                }
                else
                {
                    color = b2HexColor.b2_colorGray;
                }

                b2DrawShape(draw, shape, bodySim.transform, color);
            }

            if (draw.drawAABBs)
            {
                b2AABB aabb = shape.fatAABB;

                Span<b2Vec2> vs = stackalloc b2Vec2[4]
                {
                    new b2Vec2(aabb.lowerBound.x, aabb.lowerBound.y),
                    new b2Vec2(aabb.upperBound.x, aabb.lowerBound.y),
                    new b2Vec2(aabb.upperBound.x, aabb.upperBound.y),
                    new b2Vec2(aabb.lowerBound.x, aabb.upperBound.y),
                };


                draw.DrawPolygon(vs, 4, b2HexColor.b2_colorGold, draw.context);
            }

            return true;
        }

// todo this has varying order for moving shapes, causing flicker when overlapping shapes are moving
// solution: display order by shape id modulus 3, keep 3 buckets in GLSolid* and flush in 3 passes.
        public static void b2DrawWithBounds(b2World world, b2DebugDraw draw)
        {
            Debug.Assert(b2IsValidAABB(draw.drawingBounds));

            const float k_impulseScale = 1.0f;
            const float k_axisScale = 0.3f;
            b2HexColor speculativeColor = b2HexColor.b2_colorGainsboro;
            b2HexColor addColor = b2HexColor.b2_colorGreen;
            b2HexColor persistColor = b2HexColor.b2_colorBlue;
            b2HexColor normalColor = b2HexColor.b2_colorDimGray;
            b2HexColor impulseColor = b2HexColor.b2_colorMagenta;
            b2HexColor frictionColor = b2HexColor.b2_colorYellow;

            Span<b2HexColor> graphColors = stackalloc b2HexColor[B2_GRAPH_COLOR_COUNT]
            {
                b2HexColor.b2_colorRed, b2HexColor.b2_colorOrange, b2HexColor.b2_colorYellow, b2HexColor.b2_colorGreen,
                b2HexColor.b2_colorCyan, b2HexColor.b2_colorBlue, b2HexColor.b2_colorViolet, b2HexColor.b2_colorPink,
                b2HexColor.b2_colorChocolate, b2HexColor.b2_colorGoldenRod, b2HexColor.b2_colorCoral, b2HexColor.b2_colorBlack
            };

            int bodyCapacity = b2GetIdCapacity(world.bodyIdPool);
            b2SetBitCountAndClear(world.debugBodySet, bodyCapacity);

            int jointCapacity = b2GetIdCapacity(world.jointIdPool);
            b2SetBitCountAndClear(world.debugJointSet, jointCapacity);

            int contactCapacity = b2GetIdCapacity(world.contactIdPool);
            b2SetBitCountAndClear(world.debugContactSet, contactCapacity);

            DrawContext drawContext = new DrawContext();
            drawContext.world = world;
            drawContext.draw = draw;

            for (int i = 0; i < (int)b2BodyType.b2_bodyTypeCount; ++i)
            {
                b2DynamicTree_Query(world.broadPhase.trees[i], draw.drawingBounds, B2_DEFAULT_MASK_BITS, DrawQueryCallback,
                    drawContext);
            }

            uint wordCount = (uint)world.debugBodySet.blockCount;
            ulong[] bits = world.debugBodySet.bits;
            for (uint k = 0; k < wordCount; ++k)
            {
                ulong word = bits[k];
                while (word != 0)
                {
                    uint ctz = b2CTZ64(word);
                    uint bodyId = 64 * k + ctz;

                    b2Body body = b2Array_Get(ref world.bodies, (int)bodyId);

                    if (draw.drawBodyNames && body.name[0] != 0)
                    {
                        b2Vec2 offset = new b2Vec2(0.1f, 0.1f);
                        b2BodySim bodySim = b2GetBodySim(world, body);

                        b2Transform transform = new b2Transform(bodySim.center, bodySim.transform.q);
                        draw.DrawTransform(transform, draw.context);

                        b2Vec2 p = b2TransformPoint(transform, offset);

                        draw.DrawString(p, body.name, b2HexColor.b2_colorBlueViolet, draw.context);
                    }

                    if (draw.drawMass && body.type == b2BodyType.b2_dynamicBody)
                    {
                        b2Vec2 offset = new b2Vec2(0.1f, 0.1f);
                        b2BodySim bodySim = b2GetBodySim(world, body);

                        b2Transform transform = new b2Transform(bodySim.center, bodySim.transform.q);
                        draw.DrawTransform(transform, draw.context);

                        b2Vec2 p = b2TransformPoint(transform, offset);

                        string buffer = string.Format("{0:F2}", body.mass);
                        draw.DrawString(p, buffer, b2HexColor.b2_colorWhite, draw.context);
                    }

                    if (draw.drawJoints)
                    {
                        int jointKey = body.headJointKey;
                        while (jointKey != B2_NULL_INDEX)
                        {
                            int jointId = jointKey >> 1;
                            int edgeIndex = jointKey & 1;
                            b2Joint joint = b2Array_Get(ref world.joints, jointId);

                            // avoid double draw
                            if (b2GetBit(world.debugJointSet, jointId) == false)
                            {
                                b2DrawJoint(draw, world, joint);
                                b2SetBit(world.debugJointSet, jointId);
                            }
                            else
                            {
                                // todo testing
                                edgeIndex += 0;
                            }

                            jointKey = joint.edges[edgeIndex].nextKey;
                        }
                    }

                    float linearSlop = B2_LINEAR_SLOP;
                    if (draw.drawContacts && body.type == b2BodyType.b2_dynamicBody && body.setIndex == (int)b2SetType.b2_awakeSet)
                    {
                        int contactKey = body.headContactKey;
                        while (contactKey != B2_NULL_INDEX)
                        {
                            int contactId = contactKey >> 1;
                            int edgeIndex = contactKey & 1;
                            b2Contact contact = b2Array_Get(ref world.contacts, contactId);
                            contactKey = contact.edges[edgeIndex].nextKey;

                            if (contact.setIndex != (int)b2SetType.b2_awakeSet || contact.colorIndex == B2_NULL_INDEX)
                            {
                                continue;
                            }

                            // avoid double draw
                            if (b2GetBit(world.debugContactSet, contactId) == false)
                            {
                                Debug.Assert(0 <= contact.colorIndex && contact.colorIndex < B2_GRAPH_COLOR_COUNT);

                                b2GraphColor gc = world.constraintGraph.colors[contact.colorIndex];
                                b2ContactSim contactSim = b2Array_Get(ref gc.contactSims, contact.localIndex);
                                int pointCount = contactSim.manifold.pointCount;
                                b2Vec2 normal = contactSim.manifold.normal;
                                string buffer;

                                for (int j = 0; j < pointCount; ++j)
                                {
                                    ref b2ManifoldPoint point = ref contactSim.manifold.points[j];

                                    if (draw.drawGraphColors)
                                    {
                                        // graph color
                                        float pointSize = contact.colorIndex == B2_OVERFLOW_INDEX ? 7.5f : 5.0f;
                                        draw.DrawPoint(point.point, pointSize, graphColors[contact.colorIndex], draw.context);
                                        // Draw.g_draw.DrawString(point.position, "%d", point.color);
                                    }
                                    else if (point.separation > linearSlop)
                                    {
                                        // Speculative
                                        draw.DrawPoint(point.point, 5.0f, speculativeColor, draw.context);
                                    }
                                    else if (point.persisted == false)
                                    {
                                        // Add
                                        draw.DrawPoint(point.point, 10.0f, addColor, draw.context);
                                    }
                                    else if (point.persisted == true)
                                    {
                                        // Persist
                                        draw.DrawPoint(point.point, 5.0f, persistColor, draw.context);
                                    }

                                    if (draw.drawContactNormals)
                                    {
                                        b2Vec2 p1 = point.point;
                                        b2Vec2 p2 = b2MulAdd(p1, k_axisScale, normal);
                                        draw.DrawSegment(p1, p2, normalColor, draw.context);
                                    }
                                    else if (draw.drawContactImpulses)
                                    {
                                        b2Vec2 p1 = point.point;
                                        b2Vec2 p2 = b2MulAdd(p1, k_impulseScale * point.normalImpulse, normal);
                                        draw.DrawSegment(p1, p2, impulseColor, draw.context);
                                        buffer = $"{1000.0f * point.normalImpulse:F1}";
                                        draw.DrawString(p1, buffer, b2HexColor.b2_colorWhite, draw.context);
                                    }

                                    if (draw.drawFrictionImpulses)
                                    {
                                        b2Vec2 tangent = b2RightPerp(normal);
                                        b2Vec2 p1 = point.point;
                                        b2Vec2 p2 = b2MulAdd(p1, k_impulseScale * point.tangentImpulse, tangent);
                                        draw.DrawSegment(p1, p2, frictionColor, draw.context);
                                        buffer = $"{1000.0f * point.tangentImpulse:F1}";
                                        draw.DrawString(p1, buffer, b2HexColor.b2_colorWhite, draw.context);
                                    }
                                }

                                b2SetBit(world.debugContactSet, contactId);
                            }
                            else
                            {
                                // todo testing
                                edgeIndex += 0;
                            }

                            contactKey = contact.edges[edgeIndex].nextKey;
                        }
                    }

                    // Clear the smallest set bit
                    word = word & (word - 1);
                }
            }
        }

        public static void b2World_Draw(b2WorldId worldId, b2DebugDraw draw)
        {
            b2World world = b2GetWorldFromId(worldId);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return;
            }

            // todo it seems bounds drawing is fast enough for regular usage
            if (draw.useDrawingBounds)
            {
                b2DrawWithBounds(world, draw);
                return;
            }

            if (draw.drawShapes)
            {
                int setCount = world.solverSets.count;
                for (int setIndex = 0; setIndex < setCount; ++setIndex)
                {
                    b2SolverSet set = b2Array_Get(ref world.solverSets, setIndex);
                    int bodyCount = set.bodySims.count;
                    for (int bodyIndex = 0; bodyIndex < bodyCount; ++bodyIndex)
                    {
                        b2BodySim bodySim = set.bodySims.data[bodyIndex];
                        b2Body body = b2Array_Get(ref world.bodies, bodySim.bodyId);
                        Debug.Assert(body.setIndex == setIndex);

                        b2Transform xf = bodySim.transform;
                        int shapeId = body.headShapeId;
                        while (shapeId != B2_NULL_INDEX)
                        {
                            b2Shape shape = world.shapes.data[shapeId];
                            b2HexColor color;

                            if (shape.customColor != 0)
                            {
                                color = (b2HexColor)shape.customColor;
                            }
                            else if (body.type == b2BodyType.b2_dynamicBody && body.mass == 0.0f)
                            {
                                // Bad body
                                color = b2HexColor.b2_colorRed;
                            }
                            else if (body.setIndex == (int)b2SetType.b2_disabledSet)
                            {
                                color = b2HexColor.b2_colorSlateGray;
                            }
                            else if (shape.sensorIndex != B2_NULL_INDEX)
                            {
                                color = b2HexColor.b2_colorWheat;
                            }
                            else if (bodySim.isBullet && body.setIndex == (int)b2SetType.b2_awakeSet)
                            {
                                color = b2HexColor.b2_colorTurquoise;
                            }
                            else if (body.isSpeedCapped)
                            {
                                color = b2HexColor.b2_colorYellow;
                            }
                            else if (bodySim.isFast)
                            {
                                color = b2HexColor.b2_colorSalmon;
                            }
                            else if (body.type == b2BodyType.b2_staticBody)
                            {
                                color = b2HexColor.b2_colorPaleGreen;
                            }
                            else if (body.type == b2BodyType.b2_kinematicBody)
                            {
                                color = b2HexColor.b2_colorRoyalBlue;
                            }
                            else if (body.setIndex == (int)b2SetType.b2_awakeSet)
                            {
                                color = b2HexColor.b2_colorPink;
                            }
                            else
                            {
                                color = b2HexColor.b2_colorGray;
                            }

                            b2DrawShape(draw, shape, xf, color);
                            shapeId = shape.nextShapeId;
                        }
                    }
                }
            }

            if (draw.drawJoints)
            {
                int count = world.joints.count;
                for (int i = 0; i < count; ++i)
                {
                    b2Joint joint = world.joints.data[i];
                    if (joint.setIndex == B2_NULL_INDEX)
                    {
                        continue;
                    }

                    b2DrawJoint(draw, world, joint);
                }
            }

            if (draw.drawAABBs)
            {
                b2HexColor color = b2HexColor.b2_colorGold;

                int setCount = world.solverSets.count;
                Span<b2Vec2> vs = stackalloc b2Vec2[4];
                for (int setIndex = 0; setIndex < setCount; ++setIndex)
                {
                    b2SolverSet set = b2Array_Get(ref world.solverSets, setIndex);
                    int bodyCount = set.bodySims.count;
                    for (int bodyIndex = 0; bodyIndex < bodyCount; ++bodyIndex)
                    {
                        b2BodySim bodySim = set.bodySims.data[bodyIndex];

                        string buffer = "" + bodySim.bodyId;
                        draw.DrawString(bodySim.center, buffer, b2HexColor.b2_colorWhite, draw.context);

                        b2Body body = b2Array_Get(ref world.bodies, bodySim.bodyId);
                        Debug.Assert(body.setIndex == setIndex);

                        int shapeId = body.headShapeId;
                        while (shapeId != B2_NULL_INDEX)
                        {
                            b2Shape shape = world.shapes.data[shapeId];
                            b2AABB aabb = shape.fatAABB;

                            vs[0] = new b2Vec2(aabb.lowerBound.x, aabb.lowerBound.y);
                            vs[1] = new b2Vec2(aabb.upperBound.x, aabb.lowerBound.y);
                            vs[2] = new b2Vec2(aabb.upperBound.x, aabb.upperBound.y);
                            vs[3] = new b2Vec2(aabb.lowerBound.x, aabb.upperBound.y);

                            draw.DrawPolygon(vs, 4, color, draw.context);

                            shapeId = shape.nextShapeId;
                        }
                    }
                }
            }

            if (draw.drawBodyNames)
            {
                b2Vec2 offset = new b2Vec2(0.1f, 0.2f);
                int count = world.bodies.count;
                for (int i = 0; i < count; ++i)
                {
                    b2Body body = world.bodies.data[i];
                    if (body.setIndex == B2_NULL_INDEX)
                    {
                        continue;
                    }

                    if (body.name[0] == 0)
                    {
                        continue;
                    }

                    b2Transform transform = b2GetBodyTransformQuick(world, body);
                    b2Vec2 p = b2TransformPoint(transform, offset);

                    draw.DrawString(p, body.name, b2HexColor.b2_colorBlueViolet, draw.context);
                }
            }

            if (draw.drawMass)
            {
                b2Vec2 offset = new b2Vec2(0.1f, 0.1f);
                int setCount = world.solverSets.count;
                for (int setIndex = 0; setIndex < setCount; ++setIndex)
                {
                    b2SolverSet set = b2Array_Get(ref world.solverSets, setIndex);
                    int bodyCount = set.bodySims.count;
                    for (int bodyIndex = 0; bodyIndex < bodyCount; ++bodyIndex)
                    {
                        b2BodySim bodySim = set.bodySims.data[bodyIndex];

                        b2Transform transform = new b2Transform(bodySim.center, bodySim.transform.q);
                        draw.DrawTransform(transform, draw.context);

                        b2Vec2 p = b2TransformPoint(transform, offset);

                        float mass = bodySim.invMass > 0.0f ? 1.0f / bodySim.invMass : 0.0f;
                        string buffer = $"{mass:F2}";
                        draw.DrawString(p, buffer, b2HexColor.b2_colorWhite, draw.context);
                    }
                }
            }

            if (draw.drawContacts)
            {
                const float k_impulseScale = 1.0f;
                const float k_axisScale = 0.3f;
                float linearSlop = B2_LINEAR_SLOP;

                b2HexColor speculativeColor = b2HexColor.b2_colorLightGray;
                b2HexColor addColor = b2HexColor.b2_colorGreen;
                b2HexColor persistColor = b2HexColor.b2_colorBlue;
                b2HexColor normalColor = b2HexColor.b2_colorDimGray;
                b2HexColor impulseColor = b2HexColor.b2_colorMagenta;
                b2HexColor frictionColor = b2HexColor.b2_colorYellow;

                Span<b2HexColor> colors = stackalloc b2HexColor[B2_GRAPH_COLOR_COUNT]
                {
                    b2HexColor.b2_colorRed, b2HexColor.b2_colorOrange, b2HexColor.b2_colorYellow, b2HexColor.b2_colorGreen,
                    b2HexColor.b2_colorCyan, b2HexColor.b2_colorBlue, b2HexColor.b2_colorViolet, b2HexColor.b2_colorPink,
                    b2HexColor.b2_colorChocolate, b2HexColor.b2_colorGoldenRod, b2HexColor.b2_colorCoral, b2HexColor.b2_colorBlack
                };

                for (int colorIndex = 0; colorIndex < B2_GRAPH_COLOR_COUNT; ++colorIndex)
                {
                    b2GraphColor graphColor = world.constraintGraph.colors[colorIndex];

                    int contactCount = graphColor.contactSims.count;
                    for (int contactIndex = 0; contactIndex < contactCount; ++contactIndex)
                    {
                        b2ContactSim contact = graphColor.contactSims.data[contactIndex];
                        int pointCount = contact.manifold.pointCount;
                        b2Vec2 normal = contact.manifold.normal;

                        for (int j = 0; j < pointCount; ++j)
                        {
                            ref b2ManifoldPoint point = ref contact.manifold.points[j];

                            if (draw.drawGraphColors && 0 <= colorIndex && colorIndex <= B2_GRAPH_COLOR_COUNT)
                            {
                                // graph color
                                float pointSize = colorIndex == B2_OVERFLOW_INDEX ? 7.5f : 5.0f;
                                draw.DrawPoint(point.point, pointSize, colors[colorIndex], draw.context);
                                // Draw.g_draw.DrawString(point.position, "%d", point.color);
                            }
                            else if (point.separation > linearSlop)
                            {
                                // Speculative
                                draw.DrawPoint(point.point, 5.0f, speculativeColor, draw.context);
                            }
                            else if (point.persisted == false)
                            {
                                // Add
                                draw.DrawPoint(point.point, 10.0f, addColor, draw.context);
                            }
                            else if (point.persisted == true)
                            {
                                // Persist
                                draw.DrawPoint(point.point, 5.0f, persistColor, draw.context);
                            }

                            if (draw.drawContactNormals)
                            {
                                b2Vec2 p1 = point.point;
                                b2Vec2 p2 = b2MulAdd(p1, k_axisScale, normal);
                                draw.DrawSegment(p1, p2, normalColor, draw.context);
                            }
                            else if (draw.drawContactImpulses)
                            {
                                b2Vec2 p1 = point.point;
                                b2Vec2 p2 = b2MulAdd(p1, k_impulseScale * point.normalImpulse, normal);
                                draw.DrawSegment(p1, p2, impulseColor, draw.context);
                                var buffer = $"{1000.0f * point.normalImpulse:F2}";
                                draw.DrawString(p1, buffer, b2HexColor.b2_colorWhite, draw.context);
                            }

                            if (draw.drawFrictionImpulses)
                            {
                                b2Vec2 tangent = b2RightPerp(normal);
                                b2Vec2 p1 = point.point;
                                b2Vec2 p2 = b2MulAdd(p1, k_impulseScale * point.tangentImpulse, tangent);
                                draw.DrawSegment(p1, p2, frictionColor, draw.context);
                                var buffer = $"{1000.0f * point.tangentImpulse:F2}";
                                draw.DrawString(p1, buffer, b2HexColor.b2_colorWhite, draw.context);
                            }
                        }
                    }
                }
            }
        }

        public static b2BodyEvents b2World_GetBodyEvents(b2WorldId worldId)
        {
            b2World world = b2GetWorldFromId(worldId);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return new b2BodyEvents();
            }

            int count = world.bodyMoveEvents.count;
            b2BodyEvents events = new b2BodyEvents(world.bodyMoveEvents.data, count);
            return events;
        }

        public static b2SensorEvents b2World_GetSensorEvents(b2WorldId worldId)
        {
            b2World world = b2GetWorldFromId(worldId);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return new b2SensorEvents();
            }

            // Careful to use previous buffer
            int endEventArrayIndex = 1 - world.endEventArrayIndex;

            int beginCount = world.sensorBeginEvents.count;
            int endCount = world.sensorEndEvents[endEventArrayIndex].count;

            b2SensorEvents events = new b2SensorEvents()
            {
                beginEvents = world.sensorBeginEvents.data,
                endEvents = world.sensorEndEvents[endEventArrayIndex].data,
                beginCount = beginCount,
                endCount = endCount,
            };
            return events;
        }

        public static b2ContactEvents b2World_GetContactEvents(b2WorldId worldId)
        {
            b2World world = b2GetWorldFromId(worldId);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return new b2ContactEvents();
            }

            // Careful to use previous buffer
            int endEventArrayIndex = 1 - world.endEventArrayIndex;

            int beginCount = world.contactBeginEvents.count;
            int endCount = world.contactEndEvents[endEventArrayIndex].count;
            int hitCount = world.contactHitEvents.count;

            b2ContactEvents events = new b2ContactEvents()
            {
                beginEvents = world.contactBeginEvents.data,
                endEvents = world.contactEndEvents[endEventArrayIndex].data,
                hitEvents = world.contactHitEvents.data,
                beginCount = beginCount,
                endCount = endCount,
                hitCount = hitCount,
            };

            return events;
        }

        public static bool b2World_IsValid(b2WorldId id)
        {
            if (id.index1 < 1 || B2_MAX_WORLDS < id.index1)
            {
                return false;
            }

            b2World world = b2_worlds[(id.index1 - 1)];

            if (world.worldId != id.index1 - 1)
            {
                // world is not allocated
                return false;
            }

            return id.generation == world.generation;
        }

        public static bool b2Body_IsValid(b2BodyId id)
        {
            if (id.world0 < 0 || B2_MAX_WORLDS <= id.world0)
            {
                // invalid world
                return false;
            }

            b2World world = b2_worlds[id.world0];
            if (world.worldId != id.world0)
            {
                // world is free
                return false;
            }

            if (id.index1 < 1 || world.bodies.count < id.index1)
            {
                // invalid index
                return false;
            }

            b2Body body = world.bodies.data[(id.index1 - 1)];
            if (body.setIndex == B2_NULL_INDEX)
            {
                // this was freed
                return false;
            }

            Debug.Assert(body.localIndex != B2_NULL_INDEX);

            if (body.generation != id.generation)
            {
                // this id is orphaned
                return false;
            }

            return true;
        }

        public static bool b2Shape_IsValid(b2ShapeId id)
        {
            if (B2_MAX_WORLDS <= id.world0)
            {
                return false;
            }

            b2World world = b2_worlds[id.world0];
            if (world.worldId != id.world0)
            {
                // world is free
                return false;
            }

            int shapeId = id.index1 - 1;
            if (shapeId < 0 || world.shapes.count <= shapeId)
            {
                return false;
            }

            b2Shape shape = world.shapes.data[shapeId];
            if (shape.id == B2_NULL_INDEX)
            {
                // shape is free
                return false;
            }

            Debug.Assert(shape.id == shapeId);

            return id.generation == shape.generation;
        }

        public static bool b2Chain_IsValid(b2ChainId id)
        {
            if (id.world0 < 0 || B2_MAX_WORLDS <= id.world0)
            {
                return false;
            }

            b2World world = b2_worlds[id.world0];
            if (world.worldId != id.world0)
            {
                // world is free
                return false;
            }

            int chainId = id.index1 - 1;
            if (chainId < 0 || world.chainShapes.count <= chainId)
            {
                return false;
            }

            b2ChainShape chain = world.chainShapes.data[chainId];
            if (chain.id == B2_NULL_INDEX)
            {
                // chain is free
                return false;
            }

            Debug.Assert(chain.id == chainId);

            return id.generation == chain.generation;
        }

        public static bool b2Joint_IsValid(b2JointId id)
        {
            if (id.world0 < 0 || B2_MAX_WORLDS <= id.world0)
            {
                return false;
            }

            b2World world = b2_worlds[id.world0];
            if (world.worldId != id.world0)
            {
                // world is free
                return false;
            }

            int jointId = id.index1 - 1;
            if (jointId < 0 || world.joints.count <= jointId)
            {
                return false;
            }

            b2Joint joint = world.joints.data[jointId];
            if (joint.jointId == B2_NULL_INDEX)
            {
                // joint is free
                return false;
            }

            Debug.Assert(joint.jointId == jointId);

            return id.generation == joint.generation;
        }

        public static void b2World_EnableSleeping(b2WorldId worldId, bool flag)
        {
            b2World world = b2GetWorldFromId(worldId);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return;
            }

            if (flag == world.enableSleep)
            {
                return;
            }

            world.enableSleep = flag;

            if (flag == false)
            {
                int setCount = world.solverSets.count;
                for (int i = (int)b2SetType.b2_firstSleepingSet; i < setCount; ++i)
                {
                    b2SolverSet set = b2Array_Get(ref world.solverSets, i);
                    if (set.bodySims.count > 0)
                    {
                        b2WakeSolverSet(world, i);
                    }
                }
            }
        }

        public static bool b2World_IsSleepingEnabled(b2WorldId worldId)
        {
            b2World world = b2GetWorldFromId(worldId);
            return world.enableSleep;
        }

        public static void b2World_EnableWarmStarting(b2WorldId worldId, bool flag)
        {
            b2World world = b2GetWorldFromId(worldId);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return;
            }

            world.enableWarmStarting = flag;
        }

        public static bool b2World_IsWarmStartingEnabled(b2WorldId worldId)
        {
            b2World world = b2GetWorldFromId(worldId);
            return world.enableWarmStarting;
        }

        public static int b2World_GetAwakeBodyCount(b2WorldId worldId)
        {
            b2World world = b2GetWorldFromId(worldId);
            b2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)b2SetType.b2_awakeSet);
            return awakeSet.bodySims.count;
        }

        public static void b2World_EnableContinuous(b2WorldId worldId, bool flag)
        {
            b2World world = b2GetWorldFromId(worldId);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return;
            }

            world.enableContinuous = flag;
        }

        public static bool b2World_IsContinuousEnabled(b2WorldId worldId)
        {
            b2World world = b2GetWorldFromId(worldId);
            return world.enableContinuous;
        }

        public static void b2World_SetRestitutionThreshold(b2WorldId worldId, float value)
        {
            b2World world = b2GetWorldFromId(worldId);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return;
            }

            world.restitutionThreshold = b2ClampFloat(value, 0.0f, float.MaxValue);
        }

        public static float b2World_GetRestitutionThreshold(b2WorldId worldId)
        {
            b2World world = b2GetWorldFromId(worldId);
            return world.restitutionThreshold;
        }

        public static void b2World_SetHitEventThreshold(b2WorldId worldId, float value)
        {
            b2World world = b2GetWorldFromId(worldId);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return;
            }

            world.hitEventThreshold = b2ClampFloat(value, 0.0f, float.MaxValue);
        }

        public static float b2World_GetHitEventThreshold(b2WorldId worldId)
        {
            b2World world = b2GetWorldFromId(worldId);
            return world.hitEventThreshold;
        }

        public static void b2World_SetContactTuning(b2WorldId worldId, float hertz, float dampingRatio, float pushSpeed)
        {
            b2World world = b2GetWorldFromId(worldId);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return;
            }

            world.contactHertz = b2ClampFloat(hertz, 0.0f, float.MaxValue);
            world.contactDampingRatio = b2ClampFloat(dampingRatio, 0.0f, float.MaxValue);
            world.contactMaxPushSpeed = b2ClampFloat(pushSpeed, 0.0f, float.MaxValue);
        }

        public static void b2World_SetJointTuning(b2WorldId worldId, float hertz, float dampingRatio)
        {
            b2World world = b2GetWorldFromId(worldId);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return;
            }

            world.jointHertz = b2ClampFloat(hertz, 0.0f, float.MaxValue);
            world.jointDampingRatio = b2ClampFloat(dampingRatio, 0.0f, float.MaxValue);
        }

        public static void b2World_SetMaximumLinearSpeed(b2WorldId worldId, float maximumLinearSpeed)
        {
            Debug.Assert(b2IsValidFloat(maximumLinearSpeed) && maximumLinearSpeed > 0.0f);

            b2World world = b2GetWorldFromId(worldId);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return;
            }

            world.maxLinearSpeed = maximumLinearSpeed;
        }

        public static float b2World_GetMaximumLinearSpeed(b2WorldId worldId)
        {
            b2World world = b2GetWorldFromId(worldId);
            return world.maxLinearSpeed;
        }

        public static b2Profile b2World_GetProfile(b2WorldId worldId)
        {
            b2World world = b2GetWorldFromId(worldId);
            return world.profile;
        }

        public static b2Counters b2World_GetCounters(b2WorldId worldId)
        {
            b2World world = b2GetWorldFromId(worldId);
            b2Counters s = new b2Counters();
            s.bodyCount = b2GetIdCount(world.bodyIdPool);
            s.shapeCount = b2GetIdCount(world.shapeIdPool);
            s.contactCount = b2GetIdCount(world.contactIdPool);
            s.jointCount = b2GetIdCount(world.jointIdPool);
            s.islandCount = b2GetIdCount(world.islandIdPool);

            b2DynamicTree staticTree = world.broadPhase.trees[(int)b2BodyType.b2_staticBody];
            s.staticTreeHeight = b2DynamicTree_GetHeight(staticTree);

            b2DynamicTree dynamicTree = world.broadPhase.trees[(int)b2BodyType.b2_dynamicBody];
            b2DynamicTree kinematicTree = world.broadPhase.trees[(int)b2BodyType.b2_kinematicBody];
            s.treeHeight = b2MaxInt(b2DynamicTree_GetHeight(dynamicTree), b2DynamicTree_GetHeight(kinematicTree));

            s.stackUsed = b2GetMaxArenaAllocation(world.stackAllocator);
            s.byteCount = b2GetByteCount();
            s.taskCount = world.taskCount;

            for (int i = 0; i < B2_GRAPH_COLOR_COUNT; ++i)
            {
                s.colorCounts[i] = world.constraintGraph.colors[i].contactSims.count + world.constraintGraph.colors[i].jointSims.count;
            }

            return s;
        }

        public static void b2World_SetUserData(b2WorldId worldId, object userData)
        {
            b2World world = b2GetWorldFromId(worldId);
            world.userData = userData;
        }

        public static object b2World_GetUserData(b2WorldId worldId)
        {
            b2World world = b2GetWorldFromId(worldId);
            return world.userData;
        }

        public static void b2World_SetFrictionCallback(b2WorldId worldId, b2FrictionCallback callback)
        {
            b2World world = b2GetWorldFromId(worldId);
            if (world.locked)
            {
                return;
            }

            if (callback != null)
            {
                world.frictionCallback = callback;
            }
            else
            {
                world.frictionCallback = b2DefaultFrictionCallback;
            }
        }

        public static void b2World_SetRestitutionCallback(b2WorldId worldId, b2RestitutionCallback callback)
        {
            b2World world = b2GetWorldFromId(worldId);
            if (world.locked)
            {
                return;
            }

            if (callback != null)
            {
                world.restitutionCallback = callback;
            }
            else
            {
                world.restitutionCallback = b2DefaultRestitutionCallback;
            }
        }

        public static void b2World_DumpMemoryStats(b2WorldId worldId)
        {
            using StreamWriter writer = new StreamWriter("box2d_memory.txt");

            b2World world = b2GetWorldFromId(worldId);

            // id pools
            writer.Write("id pools\n");
            writer.Write("body ids: {0}\n", b2GetIdBytes(world.bodyIdPool));
            writer.Write("solver set ids: {0}\n", b2GetIdBytes(world.solverSetIdPool));
            writer.Write("joint ids: {0}\n", b2GetIdBytes(world.jointIdPool));
            writer.Write("contact ids: {0}\n", b2GetIdBytes(world.contactIdPool));
            writer.Write("island ids: {0}\n", b2GetIdBytes(world.islandIdPool));
            writer.Write("shape ids: {0}\n", b2GetIdBytes(world.shapeIdPool));
            writer.Write("chain ids: {0}\n", b2GetIdBytes(world.chainIdPool));
            writer.Write("\n");

            // world arrays
            writer.Write("world arrays\n");
            writer.Write("bodies: {0}\n", b2Array_ByteCount(ref world.bodies));
            writer.Write("solver sets: {0}\n", b2Array_ByteCount(ref world.solverSets));
            writer.Write("joints: {0}\n", b2Array_ByteCount(ref world.joints));
            writer.Write("contacts: {0}\n", b2Array_ByteCount(ref world.contacts));
            writer.Write("islands: {0}\n", b2Array_ByteCount(ref world.islands));
            writer.Write("shapes: {0}\n", b2Array_ByteCount(ref world.shapes));
            writer.Write("chains: {0}\n", b2Array_ByteCount(ref world.chainShapes));
            writer.Write("\n");

            // broad-phase
            writer.Write("broad-phase\n");
            writer.Write("static tree: {0}\n", b2DynamicTree_GetByteCount(world.broadPhase.trees[(int)b2BodyType.b2_staticBody]));
            writer.Write("kinematic tree: {0}\n", b2DynamicTree_GetByteCount(world.broadPhase.trees[(int)b2BodyType.b2_kinematicBody]));
            writer.Write("dynamic tree: {0}\n", b2DynamicTree_GetByteCount(world.broadPhase.trees[(int)b2BodyType.b2_dynamicBody]));
            b2HashSet moveSet = world.broadPhase.moveSet;
            writer.Write("moveSet: {0} ({1}, {2})\n", b2GetHashSetBytes(moveSet), moveSet.count, moveSet.capacity);
            writer.Write("moveArray: {0}\n", b2Array_ByteCount(ref world.broadPhase.moveArray));
            b2HashSet pairSet = world.broadPhase.pairSet;
            writer.Write("pairSet: {0} ({1}, {2})\n", b2GetHashSetBytes(pairSet), pairSet.count, pairSet.capacity);
            writer.Write("\n");

            // solver sets
            int bodySimCapacity = 0;
            int bodyStateCapacity = 0;
            int jointSimCapacity = 0;
            int contactSimCapacity = 0;
            int islandSimCapacity = 0;
            int solverSetCapacity = world.solverSets.count;
            for (int i = 0; i < solverSetCapacity; ++i)
            {
                b2SolverSet set = world.solverSets.data[i];
                if (set.setIndex == B2_NULL_INDEX)
                {
                    continue;
                }

                bodySimCapacity += set.bodySims.capacity;
                bodyStateCapacity += set.bodyStates.capacity;
                jointSimCapacity += set.jointSims.capacity;
                contactSimCapacity += set.contactSims.capacity;
                islandSimCapacity += set.islandSims.capacity;
            }

            writer.Write("solver sets\n");
            writer.Write("body sim: {0}\n", bodySimCapacity);
            writer.Write("body state: {0}\n", bodyStateCapacity);
            writer.Write("joint sim: {0}\n", jointSimCapacity);
            writer.Write("contact sim: {0}\n", contactSimCapacity);
            writer.Write("island sim: {0}\n", islandSimCapacity);
            writer.Write("\n");

            // constraint graph
            int bodyBitSetBytes = 0;
            contactSimCapacity = 0;
            jointSimCapacity = 0;
            for (int i = 0; i < B2_GRAPH_COLOR_COUNT; ++i)
            {
                b2GraphColor c = world.constraintGraph.colors[i];
                bodyBitSetBytes += b2GetBitSetBytes(c.bodySet);
                contactSimCapacity += c.contactSims.capacity;
                jointSimCapacity += c.jointSims.capacity;
            }

            writer.Write("constraint graph\n");
            writer.Write("body bit sets: {0}\n", bodyBitSetBytes);
            writer.Write("joint sim: {0}n", jointSimCapacity);
            writer.Write("contact sim: {0}n", contactSimCapacity);
            writer.Write("\n");

            // stack allocator
            writer.Write("stack allocator: {0}\n\n", b2GetArenaCapacity(world.stackAllocator));

            // chain shapes
            // todo
        }


        static bool TreeQueryCallback(int proxyId, int shapeId, object context)
        {
            B2_UNUSED(proxyId);

            WorldQueryContext worldContext = context as WorldQueryContext;
            b2World world = worldContext.world;

            b2Shape shape = b2Array_Get(ref world.shapes, shapeId);

            b2Filter shapeFilter = shape.filter;
            b2QueryFilter queryFilter = worldContext.filter;

            if ((shapeFilter.categoryBits & queryFilter.maskBits) == 0 || (shapeFilter.maskBits & queryFilter.categoryBits) == 0)
            {
                return true;
            }

            b2ShapeId id = new b2ShapeId(shapeId + 1, world.worldId, shape.generation);
            bool result = worldContext.fcn(id, worldContext.userContext);
            return result;
        }

        public static b2TreeStats b2World_OverlapAABB(b2WorldId worldId, b2AABB aabb, b2QueryFilter filter, b2OverlapResultFcn fcn, object context)
        {
            b2TreeStats treeStats = new b2TreeStats();

            b2World world = b2GetWorldFromId(worldId);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return treeStats;
            }

            Debug.Assert(b2IsValidAABB(aabb));

            WorldQueryContext worldContext = new WorldQueryContext(world, fcn, filter, context);

            for (int i = 0; i < (int)b2BodyType.b2_bodyTypeCount; ++i)
            {
                b2TreeStats treeResult =
                    b2DynamicTree_Query(world.broadPhase.trees[i], aabb, filter.maskBits, TreeQueryCallback, worldContext);

                treeStats.nodeVisits += treeResult.nodeVisits;
                treeStats.leafVisits += treeResult.leafVisits;
            }

            return treeStats;
        }


        public static bool TreeOverlapCallback(int proxyId, int shapeId, object context)
        {
            B2_UNUSED(proxyId);

            WorldOverlapContext worldContext = context as WorldOverlapContext;
            b2World world = worldContext.world;

            b2Shape shape = b2Array_Get(ref world.shapes, shapeId);

            b2Filter shapeFilter = shape.filter;
            b2QueryFilter queryFilter = worldContext.filter;

            if ((shapeFilter.categoryBits & queryFilter.maskBits) == 0 || (shapeFilter.maskBits & queryFilter.categoryBits) == 0)
            {
                return true;
            }

            b2Body body = b2Array_Get(ref world.bodies, shape.bodyId);
            b2Transform transform = b2GetBodyTransformQuick(world, body);

            b2DistanceInput input = new b2DistanceInput();
            input.proxyA = worldContext.proxy;
            input.proxyB = b2MakeShapeDistanceProxy(shape);
            input.transformA = worldContext.transform;
            input.transformB = transform;
            input.useRadii = true;

            b2SimplexCache cache = new b2SimplexCache();
            b2DistanceOutput output = b2ShapeDistance(ref cache, input, null, 0);

            if (output.distance > 0.0f)
            {
                return true;
            }

            b2ShapeId id = new b2ShapeId(shape.id + 1, world.worldId, shape.generation);
            bool result = worldContext.fcn(id, worldContext.userContext);
            return result;
        }

        public static b2TreeStats b2World_OverlapPoint(b2WorldId worldId, b2Vec2 point, b2Transform transform, b2QueryFilter filter,
            b2OverlapResultFcn fcn, object context)
        {
            b2Circle circle = new b2Circle(point, 0.0f);
            return b2World_OverlapCircle(worldId, circle, transform, filter, fcn, context);
        }

        public static b2TreeStats b2World_OverlapCircle(b2WorldId worldId, b2Circle circle, b2Transform transform, b2QueryFilter filter,
            b2OverlapResultFcn fcn, object context)
        {
            b2TreeStats treeStats = new b2TreeStats();

            b2World world = b2GetWorldFromId(worldId);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return treeStats;
            }

            Debug.Assert(b2IsValidVec2(transform.p));
            Debug.Assert(b2IsValidRotation(transform.q));

            b2AABB aabb = b2ComputeCircleAABB(circle, transform);
            WorldOverlapContext worldContext = new WorldOverlapContext(
                world, fcn, filter, b2MakeProxy(circle.center, 1, circle.radius), transform, context
            );

            for (int i = 0; i < (int)b2BodyType.b2_bodyTypeCount; ++i)
            {
                b2TreeStats treeResult =
                    b2DynamicTree_Query(world.broadPhase.trees[i], aabb, filter.maskBits, TreeOverlapCallback, worldContext);

                treeStats.nodeVisits += treeResult.nodeVisits;
                treeStats.leafVisits += treeResult.leafVisits;
            }

            return treeStats;
        }

        public static b2TreeStats b2World_OverlapCapsule(b2WorldId worldId, b2Capsule capsule, b2Transform transform, b2QueryFilter filter,
            b2OverlapResultFcn fcn, object context)
        {
            b2TreeStats treeStats = new b2TreeStats();

            b2World world = b2GetWorldFromId(worldId);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return treeStats;
            }

            Debug.Assert(b2IsValidVec2(transform.p));
            Debug.Assert(b2IsValidRotation(transform.q));

            b2AABB aabb = b2ComputeCapsuleAABB(capsule, transform);
            WorldOverlapContext worldContext = new WorldOverlapContext(
                world, fcn, filter, b2MakeProxy(capsule.center1, capsule.center2, 2, capsule.radius), transform, context
            );

            for (int i = 0; i < (int)b2BodyType.b2_bodyTypeCount; ++i)
            {
                b2TreeStats treeResult =
                    b2DynamicTree_Query(world.broadPhase.trees[i], aabb, filter.maskBits, TreeOverlapCallback, worldContext);

                treeStats.nodeVisits += treeResult.nodeVisits;
                treeStats.leafVisits += treeResult.leafVisits;
            }

            return treeStats;
        }

        public static b2TreeStats b2World_OverlapPolygon(b2WorldId worldId, b2Polygon polygon, b2Transform transform, b2QueryFilter filter,
            b2OverlapResultFcn fcn, object context)
        {
            b2TreeStats treeStats = new b2TreeStats();

            b2World world = b2GetWorldFromId(worldId);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return treeStats;
            }

            Debug.Assert(b2IsValidVec2(transform.p));
            Debug.Assert(b2IsValidRotation(transform.q));

            b2AABB aabb = b2ComputePolygonAABB(polygon, transform);
            WorldOverlapContext worldContext = new WorldOverlapContext(
                world, fcn, filter, b2MakeProxy(polygon.vertices, polygon.count, polygon.radius), transform, context
            );

            for (int i = 0; i < (int)b2BodyType.b2_bodyTypeCount; ++i)
            {
                b2TreeStats treeResult =
                    b2DynamicTree_Query(world.broadPhase.trees[i], aabb, filter.maskBits, TreeOverlapCallback, worldContext);

                treeStats.nodeVisits += treeResult.nodeVisits;
                treeStats.leafVisits += treeResult.leafVisits;
            }

            return treeStats;
        }

        public class WorldRayCastContext
        {
            public b2World world;
            public b2CastResultFcn fcn;
            public b2QueryFilter filter;
            public float fraction;
            public object userContext;

            public WorldRayCastContext(b2World world, b2CastResultFcn fcn, b2QueryFilter filter, float fraction, object userContext)
            {
                this.world = world;
                this.fcn = fcn;
                this.filter = filter;
                this.fraction = fraction;
                this.userContext = userContext;
            }
        }

        public static float RayCastCallback(b2RayCastInput input, int proxyId, int shapeId, object context)
        {
            B2_UNUSED(proxyId);

            WorldRayCastContext worldContext = context as WorldRayCastContext;
            b2World world = worldContext.world;

            b2Shape shape = b2Array_Get(ref world.shapes, shapeId);
            b2Filter shapeFilter = shape.filter;
            b2QueryFilter queryFilter = worldContext.filter;

            if ((shapeFilter.categoryBits & queryFilter.maskBits) == 0 || (shapeFilter.maskBits & queryFilter.categoryBits) == 0)
            {
                return input.maxFraction;
            }

            b2Body body = b2Array_Get(ref world.bodies, shape.bodyId);
            b2Transform transform = b2GetBodyTransformQuick(world, body);
            b2CastOutput output = b2RayCastShape(input, shape, transform);

            if (output.hit)
            {
                b2ShapeId id = new b2ShapeId(shapeId + 1, world.worldId, shape.generation);
                float fraction = worldContext.fcn(id, output.point, output.normal, output.fraction, worldContext.userContext);

                // The user may return -1 to skip this shape
                if (0.0f <= fraction && fraction <= 1.0f)
                {
                    worldContext.fraction = fraction;
                }

                return fraction;
            }

            return input.maxFraction;
        }

        public static b2TreeStats b2World_CastRay(b2WorldId worldId, b2Vec2 origin, b2Vec2 translation, b2QueryFilter filter, b2CastResultFcn fcn,
            object context)
        {
            b2TreeStats treeStats = new b2TreeStats();

            b2World world = b2GetWorldFromId(worldId);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return treeStats;
            }

            Debug.Assert(b2IsValidVec2(origin));
            Debug.Assert(b2IsValidVec2(translation));

            b2RayCastInput input = new b2RayCastInput(origin, translation, 1.0f);

            WorldRayCastContext worldContext = new WorldRayCastContext(world, fcn, filter, 1.0f, context);

            for (int i = 0; i < (int)b2BodyType.b2_bodyTypeCount; ++i)
            {
                b2TreeStats treeResult =
                    b2DynamicTree_RayCast(world.broadPhase.trees[i], input, filter.maskBits, RayCastCallback, worldContext);
                treeStats.nodeVisits += treeResult.nodeVisits;
                treeStats.leafVisits += treeResult.leafVisits;

                if (worldContext.fraction == 0.0f)
                {
                    return treeStats;
                }

                input.maxFraction = worldContext.fraction;
            }

            return treeStats;
        }

// This callback finds the closest hit. This is the most common callback used in games.
        public static float b2RayCastClosestFcn(b2ShapeId shapeId, b2Vec2 point, b2Vec2 normal, float fraction, object context)
        {
            b2RayResult rayResult = context as b2RayResult;
            rayResult.shapeId = shapeId;
            rayResult.point = point;
            rayResult.normal = normal;
            rayResult.fraction = fraction;
            rayResult.hit = true;
            return fraction;
        }

        public static b2RayResult b2World_CastRayClosest(b2WorldId worldId, b2Vec2 origin, b2Vec2 translation, b2QueryFilter filter)
        {
            b2RayResult result = new b2RayResult();

            b2World world = b2GetWorldFromId(worldId);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return result;
            }

            Debug.Assert(b2IsValidVec2(origin));
            Debug.Assert(b2IsValidVec2(translation));

            b2RayCastInput input = new b2RayCastInput(origin, translation, 1.0f);
            WorldRayCastContext worldContext = new WorldRayCastContext(world, b2RayCastClosestFcn, filter, 1.0f, result);

            for (int i = 0; i < (int)b2BodyType.b2_bodyTypeCount; ++i)
            {
                b2TreeStats treeResult =
                    b2DynamicTree_RayCast(world.broadPhase.trees[i], input, filter.maskBits, RayCastCallback, worldContext);
                result.nodeVisits += treeResult.nodeVisits;
                result.leafVisits += treeResult.leafVisits;

                if (worldContext.fraction == 0.0f)
                {
                    return result;
                }

                input.maxFraction = worldContext.fraction;
            }

            return result;
        }

        public static float ShapeCastCallback(b2ShapeCastInput input, int proxyId, int shapeId, object context)
        {
            B2_UNUSED(proxyId);

            WorldRayCastContext worldContext = context as WorldRayCastContext;
            b2World world = worldContext.world;

            b2Shape shape = b2Array_Get(ref world.shapes, shapeId);
            b2Filter shapeFilter = shape.filter;
            b2QueryFilter queryFilter = worldContext.filter;

            if ((shapeFilter.categoryBits & queryFilter.maskBits) == 0 || (shapeFilter.maskBits & queryFilter.categoryBits) == 0)
            {
                return input.maxFraction;
            }

            b2Body body = b2Array_Get(ref world.bodies, shape.bodyId);
            b2Transform transform = b2GetBodyTransformQuick(world, body);

            b2CastOutput output = b2ShapeCastShape(input, shape, transform);

            if (output.hit)
            {
                b2ShapeId id = new b2ShapeId(shapeId + 1, world.worldId, shape.generation);
                float fraction = worldContext.fcn(id, output.point, output.normal, output.fraction, worldContext.userContext);
                worldContext.fraction = fraction;
                return fraction;
            }

            return input.maxFraction;
        }

        public static b2TreeStats b2World_CastCircle(b2WorldId worldId, b2Circle circle, b2Transform originTransform, b2Vec2 translation,
            b2QueryFilter filter, b2CastResultFcn fcn, object context)
        {
            b2TreeStats treeStats = new b2TreeStats();

            b2World world = b2GetWorldFromId(worldId);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return treeStats;
            }

            Debug.Assert(b2IsValidVec2(originTransform.p));
            Debug.Assert(b2IsValidRotation(originTransform.q));
            Debug.Assert(b2IsValidVec2(translation));

            b2ShapeCastInput input = new b2ShapeCastInput();
            input.points[0] = b2TransformPoint(originTransform, circle.center);
            input.count = 1;
            input.radius = circle.radius;
            input.translation = translation;
            input.maxFraction = 1.0f;

            WorldRayCastContext worldContext = new WorldRayCastContext(world, fcn, filter, 1.0f, context);

            for (int i = 0; i < (int)b2BodyType.b2_bodyTypeCount; ++i)
            {
                b2TreeStats treeResult =
                    b2DynamicTree_ShapeCast(world.broadPhase.trees[i], input, filter.maskBits, ShapeCastCallback, worldContext);
                treeStats.nodeVisits += treeResult.nodeVisits;
                treeStats.leafVisits += treeResult.leafVisits;

                if (worldContext.fraction == 0.0f)
                {
                    return treeStats;
                }

                input.maxFraction = worldContext.fraction;
            }

            return treeStats;
        }

        public static b2TreeStats b2World_CastCapsule(b2WorldId worldId, b2Capsule capsule, b2Transform originTransform, b2Vec2 translation,
            b2QueryFilter filter, b2CastResultFcn fcn, object context)
        {
            b2TreeStats treeStats = new b2TreeStats();

            b2World world = b2GetWorldFromId(worldId);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return treeStats;
            }

            Debug.Assert(b2IsValidVec2(originTransform.p));
            Debug.Assert(b2IsValidRotation(originTransform.q));
            Debug.Assert(b2IsValidVec2(translation));

            b2ShapeCastInput input = new b2ShapeCastInput();
            input.points[0] = b2TransformPoint(originTransform, capsule.center1);
            input.points[1] = b2TransformPoint(originTransform, capsule.center2);
            input.count = 2;
            input.radius = capsule.radius;
            input.translation = translation;
            input.maxFraction = 1.0f;

            WorldRayCastContext worldContext = new WorldRayCastContext(world, fcn, filter, 1.0f, context);

            for (int i = 0; i < (int)b2BodyType.b2_bodyTypeCount; ++i)
            {
                b2TreeStats treeResult =
                    b2DynamicTree_ShapeCast(world.broadPhase.trees[i], input, filter.maskBits, ShapeCastCallback, worldContext);
                treeStats.nodeVisits += treeResult.nodeVisits;
                treeStats.leafVisits += treeResult.leafVisits;

                if (worldContext.fraction == 0.0f)
                {
                    return treeStats;
                }

                input.maxFraction = worldContext.fraction;
            }

            return treeStats;
        }

        public static b2TreeStats b2World_CastPolygon(b2WorldId worldId, b2Polygon polygon, b2Transform originTransform, b2Vec2 translation,
            b2QueryFilter filter, b2CastResultFcn fcn, object context)
        {
            b2TreeStats treeStats = new b2TreeStats();

            b2World world = b2GetWorldFromId(worldId);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return treeStats;
            }

            Debug.Assert(b2IsValidVec2(originTransform.p));
            Debug.Assert(b2IsValidRotation(originTransform.q));
            Debug.Assert(b2IsValidVec2(translation));

            b2ShapeCastInput input = new b2ShapeCastInput();
            for (int i = 0; i < polygon.count; ++i)
            {
                input.points[i] = b2TransformPoint(originTransform, polygon.vertices[i]);
            }

            input.count = polygon.count;
            input.radius = polygon.radius;
            input.translation = translation;
            input.maxFraction = 1.0f;

            WorldRayCastContext worldContext = new WorldRayCastContext(world, fcn, filter, 1.0f, context);

            for (int i = 0; i < (int)b2BodyType.b2_bodyTypeCount; ++i)
            {
                b2TreeStats treeResult =
                    b2DynamicTree_ShapeCast(world.broadPhase.trees[i], input, filter.maskBits, ShapeCastCallback, worldContext);
                treeStats.nodeVisits += treeResult.nodeVisits;
                treeStats.leafVisits += treeResult.leafVisits;

                if (worldContext.fraction == 0.0f)
                {
                    return treeStats;
                }

                input.maxFraction = worldContext.fraction;
            }

            return treeStats;
        }

#if ZERO_DEFINE
void b2World_ShiftOrigin(b2WorldId worldId, b2Vec2 newOrigin)
{
	Debug.Assert(m_locked == false);
	if (m_locked)
	{
		return;
	}

	for (b2Body* b = m_bodyList; b; b = b.m_next)
	{
		b.m_xf.p -= newOrigin;
		b.m_sweep.c0 -= newOrigin;
		b.m_sweep.c -= newOrigin;
	}

	for (b2Joint* j = m_jointList; j; j = j.m_next)
	{
		j.ShiftOrigin(newOrigin);
	}

	m_contactManager.m_broadPhase.ShiftOrigin(newOrigin);
}

void b2World_Dump()
{
	if (m_locked)
	{
		return;
	}

	b2OpenDump("box2d_dump.inl");

	b2Dump("b2Vec2 g(%.9g, %.9g);\n", m_gravity.x, m_gravity.y);
	b2Dump("m_world.SetGravity(g);\n");

	b2Dump("b2Body** sims = (b2Body**)b2Alloc(%d * sizeof(b2Body*));\n", m_bodyCount);
	b2Dump("b2Joint** joints = (b2Joint**)b2Alloc(%d * sizeof(b2Joint*));\n", m_jointCount);

	int32 i = 0;
	for (b2Body* b = m_bodyList; b; b = b.m_next)
	{
		b.m_islandIndex = i;
		b.Dump();
		++i;
	}

	i = 0;
	for (b2Joint* j = m_jointList; j; j = j.m_next)
	{
		j.m_index = i;
		++i;
	}

	// First pass on joints, skip gear joints.
	for (b2Joint* j = m_jointList; j; j = j.m_next)
	{
		if (j.m_type == e_gearJoint)
		{
			continue;
		}

		b2Dump("{\n");
		j.Dump();
		b2Dump("}\n");
	}

	// Second pass on joints, only gear joints.
	for (b2Joint* j = m_jointList; j; j = j.m_next)
	{
		if (j.m_type != e_gearJoint)
		{
			continue;
		}

		b2Dump("{\n");
		j.Dump();
		b2Dump("}\n");
	}

	b2Dump("b2Free(joints);\n");
	b2Dump("b2Free(sims);\n");
	b2Dump("joints = nullptr;\n");
	b2Dump("sims = nullptr;\n");

	b2CloseDump();
}
#endif

        public static void b2World_SetCustomFilterCallback(b2WorldId worldId, b2CustomFilterFcn fcn, object context)
        {
            b2World world = b2GetWorldFromId(worldId);
            world.customFilterFcn = fcn;
            world.customFilterContext = context;
        }

        public static void b2World_SetPreSolveCallback(b2WorldId worldId, b2PreSolveFcn fcn, object context)
        {
            b2World world = b2GetWorldFromId(worldId);
            world.preSolveFcn = fcn;
            world.preSolveContext = context;
        }

        public static void b2World_SetGravity(b2WorldId worldId, b2Vec2 gravity)
        {
            b2World world = b2GetWorldFromId(worldId);
            world.gravity = gravity;
        }

        public static b2Vec2 b2World_GetGravity(b2WorldId worldId)
        {
            b2World world = b2GetWorldFromId(worldId);
            return world.gravity;
        }

        public class ExplosionContext
        {
            public b2World world;
            public b2Vec2 position;
            public float radius;
            public float falloff;
            public float impulsePerLength;

            public ExplosionContext(b2World world, b2Vec2 position, float radius, float falloff, float impulsePerLength)
            {
                this.world = world;
                this.position = position;
                this.radius = radius;
                this.falloff = falloff;
                this.impulsePerLength = impulsePerLength;
            }
        };

        public static bool ExplosionCallback(int proxyId, int shapeId, object context)
        {
            B2_UNUSED(proxyId);

            ExplosionContext explosionContext = context as ExplosionContext;
            b2World world = explosionContext.world;

            b2Shape shape = b2Array_Get(ref world.shapes, shapeId);

            b2Body body = b2Array_Get(ref world.bodies, shape.bodyId);
            Debug.Assert(body.type == b2BodyType.b2_dynamicBody);

            b2Transform transform = b2GetBodyTransformQuick(world, body);

            b2DistanceInput input = new b2DistanceInput();
            input.proxyA = b2MakeShapeDistanceProxy(shape);
            input.proxyB = b2MakeProxy(explosionContext.position, 1, 0.0f);
            input.transformA = transform;
            input.transformB = b2Transform_identity;
            input.useRadii = true;

            b2SimplexCache cache = new b2SimplexCache();
            b2DistanceOutput output = b2ShapeDistance(ref cache, input, null, 0);

            float radius = explosionContext.radius;
            float falloff = explosionContext.falloff;
            if (output.distance > radius + falloff)
            {
                return true;
            }

            b2WakeBody(world, body);

            if (body.setIndex != (int)b2SetType.b2_awakeSet)
            {
                return true;
            }

            b2Vec2 closestPoint = output.pointA;
            if (output.distance == 0.0f)
            {
                b2Vec2 localCentroid = b2GetShapeCentroid(shape);
                closestPoint = b2TransformPoint(transform, localCentroid);
            }

            b2Vec2 direction = b2Sub(closestPoint, explosionContext.position);
            if (b2LengthSquared(direction) > 100.0f * FLT_EPSILON * FLT_EPSILON)
            {
                direction = b2Normalize(direction);
            }
            else
            {
                direction = new b2Vec2(1.0f, 0.0f);
            }

            b2Vec2 localLine = b2InvRotateVector(transform.q, b2LeftPerp(direction));
            float perimeter = b2GetShapeProjectedPerimeter(shape, localLine);
            float scale = 1.0f;
            if (output.distance > radius && falloff > 0.0f)
            {
                scale = b2ClampFloat((radius + falloff - output.distance) / falloff, 0.0f, 1.0f);
            }

            float magnitude = explosionContext.impulsePerLength * perimeter * scale;
            b2Vec2 impulse = b2MulSV(magnitude, direction);

            int localIndex = body.localIndex;
            b2SolverSet set = b2Array_Get(ref world.solverSets, (int)b2SetType.b2_awakeSet);
            b2BodyState state = b2Array_Get(ref set.bodyStates, localIndex);
            b2BodySim bodySim = b2Array_Get(ref set.bodySims, localIndex);
            state.linearVelocity = b2MulAdd(state.linearVelocity, bodySim.invMass, impulse);
            state.angularVelocity += bodySim.invInertia * b2Cross(b2Sub(closestPoint, bodySim.center), impulse);

            return true;
        }

        public static void b2World_Explode(b2WorldId worldId, b2ExplosionDef explosionDef)
        {
            ulong maskBits = explosionDef.maskBits;
            b2Vec2 position = explosionDef.position;
            float radius = explosionDef.radius;
            float falloff = explosionDef.falloff;
            float impulsePerLength = explosionDef.impulsePerLength;

            Debug.Assert(b2IsValidVec2(position));
            Debug.Assert(b2IsValidFloat(radius) && radius >= 0.0f);
            Debug.Assert(b2IsValidFloat(falloff) && falloff >= 0.0f);
            Debug.Assert(b2IsValidFloat(impulsePerLength));

            b2World world = b2GetWorldFromId(worldId);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return;
            }

            ExplosionContext explosionContext = new ExplosionContext(world, position, radius, falloff, impulsePerLength);

            b2AABB aabb;
            aabb.lowerBound.x = position.x - (radius + falloff);
            aabb.lowerBound.y = position.y - (radius + falloff);
            aabb.upperBound.x = position.x + (radius + falloff);
            aabb.upperBound.y = position.y + (radius + falloff);

            b2DynamicTree_Query(world.broadPhase.trees[(int)b2BodyType.b2_dynamicBody], aabb, maskBits, ExplosionCallback, explosionContext);
        }

        public static void b2World_RebuildStaticTree(b2WorldId worldId)
        {
            b2World world = b2GetWorldFromId(worldId);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return;
            }

            b2DynamicTree staticTree = world.broadPhase.trees[(int)b2BodyType.b2_staticBody];
            b2DynamicTree_Rebuild(staticTree, true);
        }

        public static void b2World_EnableSpeculative(b2WorldId worldId, bool flag)
        {
            b2World world = b2GetWorldFromId(worldId);
            world.enableSpeculative = flag;
        }

#if B2_VALIDATE
        // When validating islands ids I have to compare the root island
        // ids because islands are not merged until the next time step.
        public static int b2GetRootIslandId(b2World world, int islandId)
        {
            if (islandId == B2_NULL_INDEX)
            {
                return B2_NULL_INDEX;
            }

            b2Island island = b2Array_Get(ref world.islands, islandId);

            int rootId = islandId;
            b2Island rootIsland = island;
            while (rootIsland.parentIsland != B2_NULL_INDEX)
            {
                b2Island parent = b2Array_Get(ref world.islands, rootIsland.parentIsland);
                rootId = rootIsland.parentIsland;
                rootIsland = parent;
            }

            return rootId;
        }

        // This validates island graph connectivity for each body
        public static void b2ValidateConnectivity(b2World world)
        {
            b2Body[] bodies = world.bodies.data;
            int bodyCapacity = world.bodies.count;

            for (int bodyIndex = 0; bodyIndex < bodyCapacity; ++bodyIndex)
            {
                b2Body body = bodies[bodyIndex];
                if (body.id == B2_NULL_INDEX)
                {
                    b2ValidateFreeId(world.bodyIdPool, bodyIndex);
                    continue;
                }

                Debug.Assert(bodyIndex == body.id);

                // Need to get the root island because islands are not merged until the next time step
                int bodyIslandId = b2GetRootIslandId(world, body.islandId);
                int bodySetIndex = body.setIndex;

                int contactKey = body.headContactKey;
                while (contactKey != B2_NULL_INDEX)
                {
                    int contactId = contactKey >> 1;
                    int edgeIndex = contactKey & 1;

                    b2Contact contact = b2Array_Get(ref world.contacts, contactId);

                    bool touching = (contact.flags & (uint)b2ContactFlags.b2_contactTouchingFlag) != 0;
                    if (touching)
                    {
                        if (bodySetIndex != (int)b2SetType.b2_staticSet)
                        {
                            int contactIslandId = b2GetRootIslandId(world, contact.islandId);
                            Debug.Assert(contactIslandId == bodyIslandId);
                        }
                    }
                    else
                    {
                        Debug.Assert(contact.islandId == B2_NULL_INDEX);
                    }

                    contactKey = contact.edges[edgeIndex].nextKey;
                }

                int jointKey = body.headJointKey;
                while (jointKey != B2_NULL_INDEX)
                {
                    int jointId = jointKey >> 1;
                    int edgeIndex = jointKey & 1;

                    b2Joint joint = b2Array_Get(ref world.joints, jointId);

                    int otherEdgeIndex = edgeIndex ^ 1;

                    b2Body otherBody = b2Array_Get(ref world.bodies, joint.edges[otherEdgeIndex].bodyId);

                    if (bodySetIndex == (int)b2SetType.b2_disabledSet || otherBody.setIndex == (int)b2SetType.b2_disabledSet)
                    {
                        Debug.Assert(joint.islandId == B2_NULL_INDEX);
                    }
                    else if (bodySetIndex == (int)b2SetType.b2_staticSet)
                    {
                        if (otherBody.setIndex == (int)b2SetType.b2_staticSet)
                        {
                            Debug.Assert(joint.islandId == B2_NULL_INDEX);
                        }
                    }
                    else
                    {
                        int jointIslandId = b2GetRootIslandId(world, joint.islandId);
                        Debug.Assert(jointIslandId == bodyIslandId);
                    }

                    jointKey = joint.edges[edgeIndex].nextKey;
                }
            }
        }

        // Validates solver sets, but not island connectivity
        public static void b2ValidateSolverSets(b2World world)
        {
            Debug.Assert(b2GetIdCapacity(world.bodyIdPool) == world.bodies.count);
            Debug.Assert(b2GetIdCapacity(world.contactIdPool) == world.contacts.count);
            Debug.Assert(b2GetIdCapacity(world.jointIdPool) == world.joints.count);
            Debug.Assert(b2GetIdCapacity(world.islandIdPool) == world.islands.count);
            Debug.Assert(b2GetIdCapacity(world.solverSetIdPool) == world.solverSets.count);

            int activeSetCount = 0;
            int totalBodyCount = 0;
            int totalJointCount = 0;
            int totalContactCount = 0;
            int totalIslandCount = 0;

            // Validate all solver sets
            int setCount = world.solverSets.count;
            for (int setIndex = 0; setIndex < setCount; ++setIndex)
            {
                b2SolverSet set = world.solverSets.data[setIndex];
                if (set.setIndex != B2_NULL_INDEX)
                {
                    activeSetCount += 1;

                    if (setIndex == (int)b2SetType.b2_staticSet)
                    {
                        Debug.Assert(set.contactSims.count == 0);
                        Debug.Assert(set.islandSims.count == 0);
                        Debug.Assert(set.bodyStates.count == 0);
                    }
                    else if (setIndex == (int)b2SetType.b2_awakeSet)
                    {
                        Debug.Assert(set.bodySims.count == set.bodyStates.count);
                        Debug.Assert(set.jointSims.count == 0);
                    }
                    else if (setIndex == (int)b2SetType.b2_disabledSet)
                    {
                        Debug.Assert(set.islandSims.count == 0);
                        Debug.Assert(set.bodyStates.count == 0);
                    }
                    else
                    {
                        Debug.Assert(set.bodyStates.count == 0);
                    }

                    // Validate bodies
                    {
                        b2Body[] bodies = world.bodies.data;
                        Debug.Assert(set.bodySims.count >= 0);
                        totalBodyCount += set.bodySims.count;
                        for (int i = 0; i < set.bodySims.count; ++i)
                        {
                            b2BodySim bodySim = set.bodySims.data[i];

                            int bodyId = bodySim.bodyId;
                            Debug.Assert(0 <= bodyId && bodyId < world.bodies.count);
                            b2Body body = bodies[bodyId];
                            Debug.Assert(body.setIndex == setIndex);
                            Debug.Assert(body.localIndex == i);
                            Debug.Assert(body.generation == body.generation);

                            if (setIndex == (int)b2SetType.b2_disabledSet)
                            {
                                Debug.Assert(body.headContactKey == B2_NULL_INDEX);
                            }

                            // Validate body shapes
                            int prevShapeId = B2_NULL_INDEX;
                            int shapeId = body.headShapeId;
                            while (shapeId != B2_NULL_INDEX)
                            {
                                b2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                                Debug.Assert(shape.id == shapeId);
                                Debug.Assert(shape.prevShapeId == prevShapeId);

                                if (setIndex == (int)b2SetType.b2_disabledSet)
                                {
                                    Debug.Assert(shape.proxyKey == B2_NULL_INDEX);
                                }
                                else if (setIndex == (int)b2SetType.b2_staticSet)
                                {
                                    Debug.Assert(B2_PROXY_TYPE(shape.proxyKey) == b2BodyType.b2_staticBody);
                                }
                                else
                                {
                                    b2BodyType proxyType = B2_PROXY_TYPE(shape.proxyKey);
                                    Debug.Assert(proxyType == b2BodyType.b2_kinematicBody || proxyType == b2BodyType.b2_dynamicBody);
                                }

                                prevShapeId = shapeId;
                                shapeId = shape.nextShapeId;
                            }

                            // Validate body contacts
                            int contactKey = body.headContactKey;
                            while (contactKey != B2_NULL_INDEX)
                            {
                                int contactId = contactKey >> 1;
                                int edgeIndex = contactKey & 1;

                                b2Contact contact = b2Array_Get(ref world.contacts, contactId);
                                Debug.Assert(contact.setIndex != (int)b2SetType.b2_staticSet);
                                Debug.Assert(contact.edges[0].bodyId == bodyId || contact.edges[1].bodyId == bodyId);
                                contactKey = contact.edges[edgeIndex].nextKey;
                            }

                            // Validate body joints
                            int jointKey = body.headJointKey;
                            while (jointKey != B2_NULL_INDEX)
                            {
                                int jointId = jointKey >> 1;
                                int edgeIndex = jointKey & 1;

                                b2Joint joint = b2Array_Get(ref world.joints, jointId);

                                int otherEdgeIndex = edgeIndex ^ 1;

                                b2Body otherBody = b2Array_Get(ref world.bodies, joint.edges[otherEdgeIndex].bodyId);

                                if (setIndex == (int)b2SetType.b2_disabledSet || otherBody.setIndex == (int)b2SetType.b2_disabledSet)
                                {
                                    Debug.Assert(joint.setIndex == (int)b2SetType.b2_disabledSet);
                                }
                                else if (setIndex == (int)b2SetType.b2_staticSet && otherBody.setIndex == (int)b2SetType.b2_staticSet)
                                {
                                    Debug.Assert(joint.setIndex == (int)b2SetType.b2_staticSet);
                                }
                                else if (setIndex == (int)b2SetType.b2_awakeSet)
                                {
                                    Debug.Assert(joint.setIndex == (int)b2SetType.b2_awakeSet);
                                }
                                else if (setIndex >= (int)b2SetType.b2_firstSleepingSet)
                                {
                                    Debug.Assert(joint.setIndex == setIndex);
                                }

                                b2JointSim jointSim = b2GetJointSim(world, joint);
                                Debug.Assert(jointSim.jointId == jointId);
                                Debug.Assert(jointSim.bodyIdA == joint.edges[0].bodyId);
                                Debug.Assert(jointSim.bodyIdB == joint.edges[1].bodyId);

                                jointKey = joint.edges[edgeIndex].nextKey;
                            }
                        }
                    }

                    // Validate contacts
                    {
                        Debug.Assert(set.contactSims.count >= 0);
                        totalContactCount += set.contactSims.count;
                        for (int i = 0; i < set.contactSims.count; ++i)
                        {
                            b2ContactSim contactSim = set.contactSims.data[i];
                            b2Contact contact = b2Array_Get(ref world.contacts, contactSim.contactId);
                            if (setIndex == (int)b2SetType.b2_awakeSet)
                            {
                                // contact should be non-touching if awake
                                // or it could be this contact hasn't been transferred yet
                                Debug.Assert(contactSim.manifold.pointCount == 0 ||
                                             (contactSim.simFlags & (uint)b2ContactSimFlags.b2_simStartedTouching) != 0);
                            }

                            Debug.Assert(contact.setIndex == setIndex);
                            Debug.Assert(contact.colorIndex == B2_NULL_INDEX);
                            Debug.Assert(contact.localIndex == i);
                        }
                    }

                    // Validate joints
                    {
                        Debug.Assert(set.jointSims.count >= 0);
                        totalJointCount += set.jointSims.count;
                        for (int i = 0; i < set.jointSims.count; ++i)
                        {
                            b2JointSim jointSim = set.jointSims.data[i];
                            b2Joint joint = b2Array_Get(ref world.joints, jointSim.jointId);
                            Debug.Assert(joint.setIndex == setIndex);
                            Debug.Assert(joint.colorIndex == B2_NULL_INDEX);
                            Debug.Assert(joint.localIndex == i);
                        }
                    }

                    // Validate islands
                    {
                        Debug.Assert(set.islandSims.count >= 0);
                        totalIslandCount += set.islandSims.count;
                        for (int i = 0; i < set.islandSims.count; ++i)
                        {
                            b2IslandSim islandSim = set.islandSims.data[i];
                            b2Island island = b2Array_Get(ref world.islands, islandSim.islandId);
                            Debug.Assert(island.setIndex == setIndex);
                            Debug.Assert(island.localIndex == i);
                        }
                    }
                }
                else
                {
                    Debug.Assert(set.bodySims.count == 0);
                    Debug.Assert(set.contactSims.count == 0);
                    Debug.Assert(set.jointSims.count == 0);
                    Debug.Assert(set.islandSims.count == 0);
                    Debug.Assert(set.bodyStates.count == 0);
                }
            }

            int setIdCount = b2GetIdCount(world.solverSetIdPool);
            Debug.Assert(activeSetCount == setIdCount);

            int bodyIdCount = b2GetIdCount(world.bodyIdPool);
            Debug.Assert(totalBodyCount == bodyIdCount);

            int islandIdCount = b2GetIdCount(world.islandIdPool);
            Debug.Assert(totalIslandCount == islandIdCount);

            // Validate constraint graph
            for (int colorIndex = 0; colorIndex < B2_GRAPH_COLOR_COUNT; ++colorIndex)
            {
                b2GraphColor color = world.constraintGraph.colors[colorIndex];
                {
                    Debug.Assert(color.contactSims.count >= 0);
                    totalContactCount += color.contactSims.count;
                    for (int i = 0; i < color.contactSims.count; ++i)
                    {
                        b2ContactSim contactSim = color.contactSims.data[i];
                        b2Contact contact = b2Array_Get(ref world.contacts, contactSim.contactId);
                        // contact should be touching in the constraint graph or awaiting transfer to non-touching
                        Debug.Assert(contactSim.manifold.pointCount > 0 ||
                                     (contactSim.simFlags & ((uint)b2ContactSimFlags.b2_simStoppedTouching | (uint)b2ContactSimFlags.b2_simDisjoint)) != 0);
                        Debug.Assert(contact.setIndex == (int)b2SetType.b2_awakeSet);
                        Debug.Assert(contact.colorIndex == colorIndex);
                        Debug.Assert(contact.localIndex == i);

                        int bodyIdA = contact.edges[0].bodyId;
                        int bodyIdB = contact.edges[1].bodyId;

                        if (colorIndex < B2_OVERFLOW_INDEX)
                        {
                            b2Body bodyA = b2Array_Get(ref world.bodies, bodyIdA);
                            b2Body bodyB = b2Array_Get(ref world.bodies, bodyIdB);
                            Debug.Assert(b2GetBit(color.bodySet, bodyIdA) == (bodyA.type != b2BodyType.b2_staticBody));
                            Debug.Assert(b2GetBit(color.bodySet, bodyIdB) == (bodyB.type != b2BodyType.b2_staticBody));
                        }
                    }
                }

                {
                    Debug.Assert(color.jointSims.count >= 0);
                    totalJointCount += color.jointSims.count;
                    for (int i = 0; i < color.jointSims.count; ++i)
                    {
                        b2JointSim jointSim = color.jointSims.data[i];
                        b2Joint joint = b2Array_Get(ref world.joints, jointSim.jointId);
                        Debug.Assert(joint.setIndex == (int)b2SetType.b2_awakeSet);
                        Debug.Assert(joint.colorIndex == colorIndex);
                        Debug.Assert(joint.localIndex == i);

                        int bodyIdA = joint.edges[0].bodyId;
                        int bodyIdB = joint.edges[1].bodyId;

                        if (colorIndex < B2_OVERFLOW_INDEX)
                        {
                            b2Body bodyA = b2Array_Get(ref world.bodies, bodyIdA);
                            b2Body bodyB = b2Array_Get(ref world.bodies, bodyIdB);
                            Debug.Assert(b2GetBit(color.bodySet, bodyIdA) == (bodyA.type != b2BodyType.b2_staticBody));
                            Debug.Assert(b2GetBit(color.bodySet, bodyIdB) == (bodyB.type != b2BodyType.b2_staticBody));
                        }
                    }
                }
            }

            int contactIdCount = b2GetIdCount(world.contactIdPool);
            Debug.Assert(totalContactCount == contactIdCount);
            Debug.Assert(totalContactCount == (int)world.broadPhase.pairSet.count);

            int jointIdCount = b2GetIdCount(world.jointIdPool);
            Debug.Assert(totalJointCount == jointIdCount);

// Validate shapes
// This is very slow on compounds
#if ZERO_DEFINE
	int shapeCapacity = b2Array(world.shapeArray).count;
	for (int shapeIndex = 0; shapeIndex < shapeCapacity; shapeIndex += 1)
	{
		b2Shape* shape = world.shapeArray + shapeIndex;
		if (shape.id != shapeIndex)
		{
			continue;
		}

		Debug.Assert(0 <= shape.bodyId && shape.bodyId < b2Array(world.bodyArray).count);

		b2Body* body = world.bodyArray + shape.bodyId;
		Debug.Assert(0 <= body.setIndex && body.setIndex < b2Array(world.solverSetArray).count);

		b2SolverSet* set = world.solverSetArray + body.setIndex;
		Debug.Assert(0 <= body.localIndex && body.localIndex < set.sims.count);

		b2BodySim* bodySim = set.sims.data + body.localIndex;
		Debug.Assert(bodySim.bodyId == shape.bodyId);

		bool found = false;
		int shapeCount = 0;
		int index = body.headShapeId;
		while (index != B2_NULL_INDEX)
		{
			b2CheckId(world.shapeArray, index);
			b2Shape* s = world.shapeArray + index;
			if (index == shapeIndex)
			{
				found = true;
			}

			index = s.nextShapeId;
			shapeCount += 1;
		}

		Debug.Assert(found);
		Debug.Assert(shapeCount == body.shapeCount);
	}
#endif
        }

        // Validate contact touching status.
        public static void b2ValidateContacts(b2World world)
        {
            int contactCount = world.contacts.count;
            Debug.Assert(contactCount == b2GetIdCapacity(world.contactIdPool));
            int allocatedContactCount = 0;

            for (int contactIndex = 0; contactIndex < contactCount; ++contactIndex)
            {
                b2Contact contact = b2Array_Get(ref world.contacts, contactIndex);
                if (contact.contactId == B2_NULL_INDEX)
                {
                    continue;
                }

                Debug.Assert(contact.contactId == contactIndex);

                allocatedContactCount += 1;

                bool touching = (contact.flags & (uint)b2ContactFlags.b2_contactTouchingFlag) != 0;

                int setId = contact.setIndex;

                if (setId == (int)b2SetType.b2_awakeSet)
                {
                    // If touching and not a sensor
                    if (touching)
                    {
                        Debug.Assert(0 <= contact.colorIndex && contact.colorIndex < B2_GRAPH_COLOR_COUNT);
                    }
                    else
                    {
                        Debug.Assert(contact.colorIndex == B2_NULL_INDEX);
                    }
                }
                else if (setId >= (int)b2SetType.b2_firstSleepingSet)
                {
                    // Only touching contacts allowed in a sleeping set
                    Debug.Assert(touching == true);
                }
                else
                {
                    // Sleeping and non-touching contacts or sensor contacts belong in the disabled set
                    Debug.Assert(touching == false && setId == (int)b2SetType.b2_disabledSet);
                }

                b2ContactSim contactSim = b2GetContactSim(world, contact);
                Debug.Assert(contactSim.contactId == contactIndex);
                Debug.Assert(contactSim.bodyIdA == contact.edges[0].bodyId);
                Debug.Assert(contactSim.bodyIdB == contact.edges[1].bodyId);

                // Sim touching is true for solid and sensor contacts
                bool simTouching = (contactSim.simFlags & (uint)b2ContactSimFlags.b2_simTouchingFlag) != 0;
                Debug.Assert(touching == simTouching);

                Debug.Assert(0 <= contactSim.manifold.pointCount && contactSim.manifold.pointCount <= 2);
            }

            int contactIdCount = b2GetIdCount(world.contactIdPool);
            Debug.Assert(allocatedContactCount == contactIdCount);
        }

#else
        public static void b2ValidateConnectivity(b2World world)
        {
            B2_UNUSED(world);
        }

        public static void b2ValidateSolverSets(b2World world)
        {
            B2_UNUSED(world);
        }

        public static void b2ValidateContacts(b2World world)
        {
            B2_UNUSED(world);
        }

#endif
    }
}