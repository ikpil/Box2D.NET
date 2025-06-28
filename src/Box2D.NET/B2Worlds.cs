// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.IO;
using static Box2D.NET.B2Tables;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2DynamicTrees;
using static Box2D.NET.B2Cores;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Buffers;
using static Box2D.NET.B2Profiling;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Contacts;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Solvers;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2IdPools;
using static Box2D.NET.B2ArenaAllocators;
using static Box2D.NET.B2BoardPhases;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2ConstraintGraphs;
using static Box2D.NET.B2BitSets;
using static Box2D.NET.B2SolverSets;
using static Box2D.NET.B2AABBs;
using static Box2D.NET.B2CTZs;
using static Box2D.NET.B2Islands;
using static Box2D.NET.B2Timers;
using static Box2D.NET.B2Sensors;

namespace Box2D.NET
{
    public static class B2Worlds
    {
        private static readonly B2World[] b2_worlds = b2AllocWorlds(B2_MAX_WORLDS);

        private static B2World[] b2AllocWorlds(int maxWorld)
        {
            B2_ASSERT(B2_MAX_WORLDS > 0, "must be 1 or more");
            B2_ASSERT(B2_MAX_WORLDS < ushort.MaxValue, "B2_MAX_WORLDS limit exceeded");
            var worlds = new B2World[maxWorld];
            for (int i = 0; i < maxWorld; ++i)
            {
                worlds[i] = new B2World();
            }

            return worlds;
        }


        public static B2World b2GetWorldFromId(B2WorldId id)
        {
            B2_ASSERT(1 <= id.index1 && id.index1 <= B2_MAX_WORLDS);
            B2World world = b2_worlds[id.index1 - 1];
            B2_ASSERT(id.index1 == world.worldId + 1);
            B2_ASSERT(id.generation == world.generation);
            return world;
        }

        public static B2World b2GetWorld(int index)
        {
            B2_ASSERT(0 <= index && index < B2_MAX_WORLDS);
            B2World world = b2_worlds[index];
            B2_ASSERT(world.worldId == index);
            return world;
        }

        public static B2World b2GetWorldLocked(int index)
        {
            B2_ASSERT(0 <= index && index < B2_MAX_WORLDS);
            B2World world = b2_worlds[index];
            B2_ASSERT(world.worldId == index);
            if (world.locked)
            {
                B2_ASSERT(false);
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

        public static B2WorldId b2CreateWorld(ref B2WorldDef def)
        {
            // check
            B2RuntimeValidator.Shared.ThrowIfSafeRuntimePlatform();

            B2_ASSERT(B2_MAX_WORLDS < ushort.MaxValue, "B2_MAX_WORLDS limit exceeded");
            B2_CHECK_DEF(ref def);

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
                return new B2WorldId(0, 0);
            }

            b2InitializeContactRegisters();

            B2World world = b2_worlds[worldId];
            ushort generation = world.generation;

            //*world = ( b2World ){ 0 };
            world.Clear();

            world.worldId = (ushort)worldId;
            world.generation = generation;
            world.inUse = true;

            world.arena = b2CreateArenaAllocator(256);
            b2CreateBroadPhase(ref world.broadPhase);
            b2CreateGraph(ref world.constraintGraph, 16);

            // pools
            world.bodyIdPool = b2CreateIdPool();
            world.bodies = b2Array_Create<B2Body>(16);
            world.solverSets = b2Array_Create<B2SolverSet>(8);

            // add empty static, active, and disabled body sets
            world.solverSetIdPool = b2CreateIdPool();
            B2SolverSet set = null;

            // static set
            set = b2CreateSolverSet(world);
            set.setIndex = b2AllocId(world.solverSetIdPool);
            b2Array_Push(ref world.solverSets, set);
            B2_ASSERT(world.solverSets.data[(int)B2SetType.b2_staticSet].setIndex == (int)B2SetType.b2_staticSet);

            // disabled set
            set = b2CreateSolverSet(world);
            set.setIndex = b2AllocId(world.solverSetIdPool);
            b2Array_Push(ref world.solverSets, set);
            B2_ASSERT(world.solverSets.data[(int)B2SetType.b2_disabledSet].setIndex == (int)B2SetType.b2_disabledSet);

            // awake set
            set = b2CreateSolverSet(world);
            set.setIndex = b2AllocId(world.solverSetIdPool);
            b2Array_Push(ref world.solverSets, set);
            B2_ASSERT(world.solverSets.data[(int)B2SetType.b2_awakeSet].setIndex == (int)B2SetType.b2_awakeSet);

            world.shapeIdPool = b2CreateIdPool();
            world.shapes = b2Array_Create<B2Shape>(16);

            world.chainIdPool = b2CreateIdPool();
            world.chainShapes = b2Array_Create<B2ChainShape>(4);

            world.contactIdPool = b2CreateIdPool();
            world.contacts = b2Array_Create<B2Contact>(16);

            world.jointIdPool = b2CreateIdPool();
            world.joints = b2Array_Create<B2Joint>(16);

            world.islandIdPool = b2CreateIdPool();
            world.islands = b2Array_Create<B2Island>(8);

            world.sensors = b2Array_Create<B2Sensor>(4);

            world.bodyMoveEvents = b2Array_Create<B2BodyMoveEvent>(4);
            world.sensorBeginEvents = b2Array_Create<B2SensorBeginTouchEvent>(4);
            world.sensorEndEvents[0] = b2Array_Create<B2SensorEndTouchEvent>(4);
            world.sensorEndEvents[1] = b2Array_Create<B2SensorEndTouchEvent>(4);
            world.contactBeginEvents = b2Array_Create<B2ContactBeginTouchEvent>(4);
            world.contactEndEvents[0] = b2Array_Create<B2ContactEndTouchEvent>(4);
            world.contactEndEvents[1] = b2Array_Create<B2ContactEndTouchEvent>(4);
            world.contactHitEvents = b2Array_Create<B2ContactHitEvent>(4);
            world.jointEvents = b2Array_Create<B2JointEvent>(4);
            world.endEventArrayIndex = 0;

            world.stepIndex = 0;
            world.splitIslandId = B2_NULL_INDEX;
            world.activeTaskCount = 0;
            world.taskCount = 0;
            world.gravity = def.gravity;
            world.hitEventThreshold = def.hitEventThreshold;
            world.restitutionThreshold = def.restitutionThreshold;
            world.maxLinearSpeed = def.maximumLinearSpeed;
            world.contactSpeed = def.contactSpeed;
            world.contactHertz = def.contactHertz;
            world.contactDampingRatio = def.contactDampingRatio;

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
            world.profile = new B2Profile();

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

            world.taskContexts = b2Array_Create<B2TaskContext>(world.workerCount);
            b2Array_Resize(ref world.taskContexts, world.workerCount);

            world.sensorTaskContexts = b2Array_Create<B2SensorTaskContext>(world.workerCount);
            b2Array_Resize(ref world.sensorTaskContexts, world.workerCount);

            for (int i = 0; i < world.workerCount; ++i)
            {
                world.taskContexts.data[i].sensorHits = b2Array_Create<B2SensorHit>(8);
                world.taskContexts.data[i].contactStateBitSet = b2CreateBitSet(1024);
                world.taskContexts.data[i].jointStateBitSet = b2CreateBitSet(1024);
                world.taskContexts.data[i].enlargedSimBitSet = b2CreateBitSet(256);
                world.taskContexts.data[i].awakeIslandBitSet = b2CreateBitSet(256);

                world.sensorTaskContexts.data[i].eventBits = b2CreateBitSet(128);
            }

            world.debugBodySet = b2CreateBitSet(256);
            world.debugJointSet = b2CreateBitSet(256);
            world.debugContactSet = b2CreateBitSet(256);
            world.debugIslandSet = b2CreateBitSet(256);

            // add one to worldId so that 0 represents a null b2WorldId
            return new B2WorldId((ushort)(worldId + 1), world.generation);
        }

        public static void b2DestroyWorld(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);

            b2DestroyBitSet(ref world.debugBodySet);
            b2DestroyBitSet(ref world.debugJointSet);
            b2DestroyBitSet(ref world.debugContactSet);
            b2DestroyBitSet(ref world.debugIslandSet);

            for (int i = 0; i < world.workerCount; ++i)
            {
                b2Array_Destroy(ref world.taskContexts.data[i].sensorHits);
                b2DestroyBitSet(ref world.taskContexts.data[i].contactStateBitSet);
                b2DestroyBitSet(ref world.taskContexts.data[i].jointStateBitSet);
                b2DestroyBitSet(ref world.taskContexts.data[i].enlargedSimBitSet);
                b2DestroyBitSet(ref world.taskContexts.data[i].awakeIslandBitSet);

                b2DestroyBitSet(ref world.sensorTaskContexts.data[i].eventBits);
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
            b2Array_Destroy(ref world.jointEvents);

            int chainCapacity = world.chainShapes.count;
            for (int i = 0; i < chainCapacity; ++i)
            {
                B2ChainShape chain = world.chainShapes.data[i];
                if (chain.id != B2_NULL_INDEX)
                {
                    b2FreeChainData(chain);
                }
                else
                {
                    B2_ASSERT(chain.shapeIndices == null);
                    B2_ASSERT(chain.materials == null);
                }
            }

            int sensorCount = world.sensors.count;
            for (int i = 0; i < sensorCount; ++i)
            {
                b2Array_Destroy(ref world.sensors.data[i].hits);
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
                B2SolverSet set = world.solverSets.data[i];
                if (set.setIndex != B2_NULL_INDEX)
                {
                    b2DestroySolverSet(world, i);
                }
            }

            b2Array_Destroy(ref world.solverSets);

            b2DestroyGraph(ref world.constraintGraph);
            b2DestroyBroadPhase(world.broadPhase);

            b2DestroyIdPool(ref world.bodyIdPool);
            b2DestroyIdPool(ref world.shapeIdPool);
            b2DestroyIdPool(ref world.chainIdPool);
            b2DestroyIdPool(ref world.contactIdPool);
            b2DestroyIdPool(ref world.jointIdPool);
            b2DestroyIdPool(ref world.islandIdPool);
            b2DestroyIdPool(ref world.solverSetIdPool);

            b2DestroyArenaAllocator(world.arena);

            // Wipe world but preserve generation
            ushort generation = world.generation;
            world.Clear();
            world.worldId = 0;
            world.generation = (ushort)(generation + 1);
        }

        public static void b2CollideTask(int startIndex, int endIndex, uint threadIndex, object context)
        {
            b2TracyCZoneNC(B2TracyCZone.collide_task, "Collide", B2HexColor.b2_colorDodgerBlue, true);

            B2StepContext stepContext = context as B2StepContext;
            B2World world = stepContext.world;
            B2_ASSERT((int)threadIndex < world.workerCount);
            B2TaskContext taskContext = world.taskContexts.data[threadIndex];
            ArraySegment<B2ContactSim> contactSims = stepContext.contacts;
            B2Shape[] shapes = world.shapes.data;
            B2Body[] bodies = world.bodies.data;

            B2_ASSERT(startIndex < endIndex);

            for (int contactIndex = startIndex; contactIndex < endIndex; ++contactIndex)
            {
                B2ContactSim contactSim = contactSims[contactIndex];

                int contactId = contactSim.contactId;

                B2Shape shapeA = shapes[contactSim.shapeIdA];
                B2Shape shapeB = shapes[contactSim.shapeIdB];

                // Do proxies still overlap?
                bool overlap = b2AABB_Overlaps(shapeA.fatAABB, shapeB.fatAABB);
                if (overlap == false)
                {
                    contactSim.simFlags |= (uint)B2ContactSimFlags.b2_simDisjoint;
                    contactSim.simFlags &= ~(uint)B2ContactSimFlags.b2_simTouchingFlag;
                    b2SetBit(ref taskContext.contactStateBitSet, contactId);
                }
                else
                {
                    bool wasTouching = 0 != (contactSim.simFlags & (uint)B2ContactSimFlags.b2_simTouchingFlag);

                    // Update contact respecting shape/body order (A,B)
                    B2Body bodyA = bodies[shapeA.bodyId];
                    B2Body bodyB = bodies[shapeB.bodyId];
                    B2BodySim bodySimA = b2GetBodySim(world, bodyA);
                    B2BodySim bodySimB = b2GetBodySim(world, bodyB);

                    // avoid cache misses in b2PrepareContactsTask
                    contactSim.bodySimIndexA = bodyA.setIndex == (int)B2SetType.b2_awakeSet ? bodyA.localIndex : B2_NULL_INDEX;
                    contactSim.invMassA = bodySimA.invMass;
                    contactSim.invIA = bodySimA.invInertia;

                    contactSim.bodySimIndexB = bodyB.setIndex == (int)B2SetType.b2_awakeSet ? bodyB.localIndex : B2_NULL_INDEX;
                    contactSim.invMassB = bodySimB.invMass;
                    contactSim.invIB = bodySimB.invInertia;

                    B2Transform transformA = bodySimA.transform;
                    B2Transform transformB = bodySimB.transform;

                    B2Vec2 centerOffsetA = b2RotateVector(transformA.q, bodySimA.localCenter);
                    B2Vec2 centerOffsetB = b2RotateVector(transformB.q, bodySimB.localCenter);

                    // This updates solid contacts
                    bool touching =
                        b2UpdateContact(world, contactSim, shapeA, transformA, centerOffsetA, shapeB, transformB, centerOffsetB);

                    // State changes that affect island connectivity. Also affects contact events.
                    if (touching == true && wasTouching == false)
                    {
                        contactSim.simFlags |= (uint)B2ContactSimFlags.b2_simStartedTouching;
                        b2SetBit(ref taskContext.contactStateBitSet, contactId);
                    }
                    else if (touching == false && wasTouching == true)
                    {
                        contactSim.simFlags |= (uint)B2ContactSimFlags.b2_simStoppedTouching;
                        b2SetBit(ref taskContext.contactStateBitSet, contactId);
                    }

                    // To make this work, the time of impact code needs to adjust the target
                    // distance based on the number of TOI events for a body.
                    // if (touching && bodySimB->isFast)
                    //{
                    //	b2Manifold* manifold = &contactSim->manifold;
                    //	int pointCount = manifold->pointCount;
                    //	for (int i = 0; i < pointCount; ++i)
                    //	{
                    //		// trick the solver into pushing the fast shapes apart
                    //		manifold->points[i].separation -= 0.25f * B2_SPECULATIVE_DISTANCE;
                    //	}
                    //}
                }
            }

            b2TracyCZoneEnd(B2TracyCZone.collide_task);
        }

        public static void b2UpdateTreesTask(int startIndex, int endIndex, uint threadIndex, object context)
        {
            B2_UNUSED(startIndex);
            B2_UNUSED(endIndex);
            B2_UNUSED(threadIndex);

            b2TracyCZoneNC(B2TracyCZone.tree_task, "Rebuild BVH", B2HexColor.b2_colorFireBrick, true);

            B2World world = context as B2World;
            b2BroadPhase_RebuildTrees(world.broadPhase);

            b2TracyCZoneEnd(B2TracyCZone.tree_task);
        }

        public static void b2AddNonTouchingContact(B2World world, B2Contact contact, B2ContactSim contactSim)
        {
            B2_ASSERT(contact.setIndex == (int)B2SetType.b2_awakeSet);
            B2SolverSet set = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_awakeSet);
            contact.colorIndex = B2_NULL_INDEX;
            contact.localIndex = set.contactSims.count;

            ref B2ContactSim newContactSim = ref b2Array_Add(ref set.contactSims);
            //memcpy( newContactSim, contactSim, sizeof( b2ContactSim ) );
            newContactSim.CopyFrom(contactSim);
        }

        public static void b2RemoveNonTouchingContact(B2World world, int setIndex, int localIndex)
        {
            B2SolverSet set = b2Array_Get(ref world.solverSets, setIndex);
            int movedIndex = b2Array_RemoveSwap(ref set.contactSims, localIndex);
            if (movedIndex != B2_NULL_INDEX)
            {
                B2ContactSim movedContactSim = set.contactSims.data[localIndex];
                B2Contact movedContact = b2Array_Get(ref world.contacts, movedContactSim.contactId);
                B2_ASSERT(movedContact.setIndex == setIndex);
                B2_ASSERT(movedContact.localIndex == movedIndex);
                B2_ASSERT(movedContact.colorIndex == B2_NULL_INDEX);
                movedContact.localIndex = localIndex;
            }
        }

        // Narrow-phase collision
        public static void b2Collide(B2StepContext context)
        {
            B2World world = context.world;

            B2_ASSERT(world.workerCount > 0);

            b2TracyCZoneNC(B2TracyCZone.collide, "Narrow Phase", B2HexColor.b2_colorDodgerBlue, true);

            // Task that can be done in parallel with the narrow-phase
            // - rebuild the collision tree for dynamic and kinematic bodies to keep their query performance good
            // todo_erin move this to start when contacts are being created
            world.userTreeTask = world.enqueueTaskFcn(b2UpdateTreesTask, 1, 1, world, world.userTaskContext);
            world.taskCount += 1;
            world.activeTaskCount += world.userTreeTask == null ? 0 : 1;

            // gather contacts into a single array for easier parallel-for
            int contactCount = 0;
            B2GraphColor[] graphColors = world.constraintGraph.colors;
            for (int i = 0; i < B2_GRAPH_COLOR_COUNT; ++i)
            {
                contactCount += graphColors[i].contactSims.count;
            }

            int nonTouchingCount = world.solverSets.data[(int)B2SetType.b2_awakeSet].contactSims.count;
            contactCount += nonTouchingCount;

            if (contactCount == 0)
            {
                b2TracyCZoneEnd(B2TracyCZone.collide);
                return;
            }

            ArraySegment<B2ContactSim> contactSims = b2AllocateArenaItem<B2ContactSim>(world.arena, contactCount, "contacts");

            int contactIndex = 0;
            for (int i = 0; i < B2_GRAPH_COLOR_COUNT; ++i)
            {
                ref B2GraphColor color = ref graphColors[i];
                int count = color.contactSims.count;
                B2ContactSim[] @base = color.contactSims.data;
                for (int j = 0; j < count; ++j)
                {
                    contactSims[contactIndex] = @base[j];
                    contactIndex += 1;
                }
            }

            {
                B2ContactSim[] @base = world.solverSets.data[(int)B2SetType.b2_awakeSet].contactSims.data;
                for (int i = 0; i < nonTouchingCount; ++i)
                {
                    contactSims[contactIndex] = @base[i];
                    contactIndex += 1;
                }
            }

            B2_ASSERT(contactIndex == contactCount);

            context.contacts = contactSims;

            // Contact bit set on ids because contact pointers are unstable as they move between touching and not touching.
            int contactIdCapacity = b2GetIdCapacity(world.contactIdPool);
            for (int i = 0; i < world.workerCount; ++i)
            {
                b2SetBitCountAndClear(ref world.taskContexts.data[i].contactStateBitSet, contactIdCapacity);
            }

            // Task should take at least 40us on a 4GHz CPU (10K cycles)
            int minRange = 64;
            object userCollideTask = world.enqueueTaskFcn(b2CollideTask, contactCount, minRange, context, world.userTaskContext);
            world.taskCount += 1;
            if (userCollideTask != null)
            {
                world.finishTaskFcn(userCollideTask, world.userTaskContext);
            }

            b2FreeArenaItem(world.arena, contactSims);
            context.contacts = null;
            contactSims = null;

            // Serially update contact state
            // todo_erin bring this zone together with island merge
            b2TracyCZoneNC(B2TracyCZone.contact_state, "Contact State", B2HexColor.b2_colorLightSlateGray, true);

            // Bitwise OR all contact bits
            ref B2BitSet bitSet = ref world.taskContexts.data[0].contactStateBitSet;
            for (int i = 1; i < world.workerCount; ++i)
            {
                b2InPlaceUnion(ref bitSet, ref world.taskContexts.data[i].contactStateBitSet);
            }

            B2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_awakeSet);

            int endEventArrayIndex = world.endEventArrayIndex;

            B2Shape[] shapes = world.shapes.data;
            ushort worldId = world.worldId;

            // Process contact state changes. Iterate over set bits
            for (uint k = 0; k < bitSet.blockCount; ++k)
            {
                ulong bits = bitSet.bits[k];
                while (bits != 0)
                {
                    uint ctz = b2CTZ64(bits);
                    int contactId = (int)(64 * k + ctz);

                    B2Contact contact = b2Array_Get(ref world.contacts, contactId);
                    B2_ASSERT(contact.setIndex == (int)B2SetType.b2_awakeSet);

                    int colorIndex = contact.colorIndex;
                    int localIndex = contact.localIndex;

                    B2ContactSim contactSim = null;
                    if (colorIndex != B2_NULL_INDEX)
                    {
                        // contact lives in constraint graph
                        B2_ASSERT(0 <= colorIndex && colorIndex < B2_GRAPH_COLOR_COUNT);
                        ref B2GraphColor color = ref graphColors[colorIndex];
                        contactSim = b2Array_Get(ref color.contactSims, localIndex);
                    }
                    else
                    {
                        contactSim = b2Array_Get(ref awakeSet.contactSims, localIndex);
                    }

                    B2Shape shapeA = shapes[contact.shapeIdA];
                    B2Shape shapeB = shapes[contact.shapeIdB];
                    B2ShapeId shapeIdA = new B2ShapeId(shapeA.id + 1, worldId, shapeA.generation);
                    B2ShapeId shapeIdB = new B2ShapeId(shapeB.id + 1, worldId, shapeB.generation);
                    B2ContactId contactFullId = new B2ContactId(
                        index1: contactId + 1,
                        world0: worldId,
                        padding: 0,
                        generation: contact.generation
                    );

                    uint flags = contact.flags;
                    uint simFlags = contactSim.simFlags;

                    if (0 != (simFlags & (uint)B2ContactSimFlags.b2_simDisjoint))
                    {
                        // Bounding boxes no longer overlap
                        b2DestroyContact(world, contact, false);
                        contact = null;
                        contactSim = null;
                    }
                    else if (0 != (simFlags & (uint)B2ContactSimFlags.b2_simStartedTouching))
                    {
                        B2_ASSERT(contact.islandId == B2_NULL_INDEX);

                        if (0 != (flags & (uint)B2ContactFlags.b2_contactEnableContactEvents))
                        {
                            B2ContactBeginTouchEvent @event = new B2ContactBeginTouchEvent(shapeIdA, shapeIdB, contactFullId);
                            b2Array_Push(ref world.contactBeginEvents, @event);
                        }

                        B2_ASSERT(contactSim.manifold.pointCount > 0);
                        B2_ASSERT(contact.setIndex == (int)B2SetType.b2_awakeSet);

                        // Link first because this wakes colliding bodies and ensures the body sims
                        // are in the correct place.
                        contact.flags |= (uint)B2ContactFlags.b2_contactTouchingFlag;
                        b2LinkContact(world, contact);

                        // Make sure these didn't change
                        B2_ASSERT(contact.colorIndex == B2_NULL_INDEX);
                        B2_ASSERT(contact.localIndex == localIndex);

                        // Contact sim pointer may have become orphaned due to awake set growth,
                        // so I just need to refresh it.
                        contactSim = b2Array_Get(ref awakeSet.contactSims, localIndex);

                        contactSim.simFlags &= ~(uint)B2ContactSimFlags.b2_simStartedTouching;

                        b2AddContactToGraph(world, contactSim, contact);
                        b2RemoveNonTouchingContact(world, (int)B2SetType.b2_awakeSet, localIndex);
                        contactSim = null;
                    }
                    else if (0 != (simFlags & (uint)B2ContactSimFlags.b2_simStoppedTouching))
                    {
                        contactSim.simFlags &= ~(uint)B2ContactSimFlags.b2_simStoppedTouching;
                        contact.flags &= ~(uint)B2ContactFlags.b2_contactTouchingFlag;

                        if (0 != (contact.flags & (uint)B2ContactFlags.b2_contactEnableContactEvents))
                        {
                            B2ContactEndTouchEvent @event = new B2ContactEndTouchEvent(shapeIdA, shapeIdB, contactFullId);
                            b2Array_Push(ref world.contactEndEvents[endEventArrayIndex], @event);
                        }

                        B2_ASSERT(contactSim.manifold.pointCount == 0);

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

            b2TracyCZoneEnd(B2TracyCZone.contact_state);
            b2TracyCZoneEnd(B2TracyCZone.collide);
        }

        public static void b2World_Step(B2WorldId worldId, float timeStep, int subStepCount)
        {
            B2_ASSERT(b2IsValidFloat(timeStep));
            B2_ASSERT(0 < subStepCount);

            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
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
            b2Array_Clear(ref world.jointEvents);

            // world.profile = ( b2Profile ){ 0 };
            world.profile = new B2Profile();

            if (timeStep == 0.0f)
            {
                // Swap end event array buffers
                world.endEventArrayIndex = 1 - world.endEventArrayIndex;
                b2Array_Clear(ref world.sensorEndEvents[world.endEventArrayIndex]);
                b2Array_Clear(ref world.contactEndEvents[world.endEventArrayIndex]);

                // todo_erin would be useful to still process collision while paused
                return;
            }

            b2TracyCZoneNC(B2TracyCZone.world_step, "Step", B2HexColor.b2_colorBox2DGreen, true);

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

            B2StepContext context = new B2StepContext();
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
            float contactHertz = b2MinFloat(world.contactHertz, 0.125f * context.inv_h);
            context.contactSoftness = b2MakeSoft(contactHertz, world.contactDampingRatio, context.h);
            context.staticSoftness = b2MakeSoft(2.0f * contactHertz, world.contactDampingRatio, context.h);

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

            B2_ASSERT(b2GetArenaAllocation(world.arena) == 0);

            // Ensure stack is large enough
            b2GrowArena(world.arena);

            // Make sure all tasks that were started were also finished
            B2_ASSERT(world.activeTaskCount == 0);

            b2TracyCZoneEnd(B2TracyCZone.world_step);

            // Swap end event array buffers
            world.endEventArrayIndex = 1 - world.endEventArrayIndex;
            b2Array_Clear(ref world.sensorEndEvents[world.endEventArrayIndex]);
            b2Array_Clear(ref world.contactEndEvents[world.endEventArrayIndex]);
            world.locked = false;
        }

        public static void b2DrawShape(B2DebugDraw draw, B2Shape shape, B2Transform xf, B2HexColor color)
        {
            switch (shape.type)
            {
                case B2ShapeType.b2_capsuleShape:
                {
                    ref readonly B2Capsule capsule = ref shape.us.capsule;
                    B2Vec2 p1 = b2TransformPoint(ref xf, capsule.center1);
                    B2Vec2 p2 = b2TransformPoint(ref xf, capsule.center2);
                    draw.DrawSolidCapsuleFcn(p1, p2, capsule.radius, color, draw.context);
                }
                    break;

                case B2ShapeType.b2_circleShape:
                {
                    ref readonly B2Circle circle = ref shape.us.circle;
                    xf.p = b2TransformPoint(ref xf, circle.center);
                    draw.DrawSolidCircleFcn(ref xf, circle.radius, color, draw.context);
                }
                    break;

                case B2ShapeType.b2_polygonShape:
                {
                    ref readonly B2Polygon poly = ref shape.us.polygon;
                    draw.DrawSolidPolygonFcn(ref xf, poly.vertices.AsSpan(), poly.count, poly.radius, color, draw.context);
                }
                    break;

                case B2ShapeType.b2_segmentShape:
                {
                    ref readonly B2Segment segment = ref shape.us.segment;
                    B2Vec2 p1 = b2TransformPoint(ref xf, segment.point1);
                    B2Vec2 p2 = b2TransformPoint(ref xf, segment.point2);
                    draw.DrawSegmentFcn(p1, p2, color, draw.context);
                }
                    break;

                case B2ShapeType.b2_chainSegmentShape:
                {
                    ref readonly B2Segment segment = ref shape.us.chainSegment.segment;
                    B2Vec2 p1 = b2TransformPoint(ref xf, segment.point1);
                    B2Vec2 p2 = b2TransformPoint(ref xf, segment.point2);
                    draw.DrawSegmentFcn(p1, p2, color, draw.context);
                    draw.DrawPointFcn(p2, 4.0f, color, draw.context);
                    draw.DrawSegmentFcn(p1, b2Lerp(p1, p2, 0.1f), B2HexColor.b2_colorPaleGreen, draw.context);
                }
                    break;

                default:
                    break;
            }
        }

        public static bool DrawQueryCallback(int proxyId, ulong userData, ref B2DrawContext context)
        {
            B2_UNUSED(proxyId);

            int shapeId = (int)userData;

            ref B2DrawContext drawContext = ref context;
            B2World world = drawContext.world;
            B2DebugDraw draw = drawContext.draw;

            B2Shape shape = b2Array_Get(ref world.shapes, shapeId);
            B2_ASSERT(shape.id == shapeId);

            b2SetBit(ref world.debugBodySet, shape.bodyId);

            if (draw.drawShapes)
            {
                B2Body body = b2Array_Get(ref world.bodies, shape.bodyId);
                B2BodySim bodySim = b2GetBodySim(world, body);

                B2HexColor color;

                if (shape.customColor != 0)
                {
                    color = (B2HexColor)shape.customColor;
                }
                else if (body.type == B2BodyType.b2_dynamicBody && body.mass == 0.0f)
                {
                    // Bad body
                    color = B2HexColor.b2_colorRed;
                }
                else if (body.setIndex == (int)B2SetType.b2_disabledSet)
                {
                    color = B2HexColor.b2_colorSlateGray;
                }
                else if (shape.sensorIndex != B2_NULL_INDEX)
                {
                    color = B2HexColor.b2_colorWheat;
                }
                else if (0 != (bodySim.flags & (uint)B2BodyFlags.b2_isBullet) && body.setIndex == (int)B2SetType.b2_awakeSet)
                {
                    color = B2HexColor.b2_colorTurquoise;
                }
                else if (body.isSpeedCapped)
                {
                    color = B2HexColor.b2_colorYellow;
                }
                else if (0 != (bodySim.flags & (uint)B2BodyFlags.b2_isFast))
                {
                    color = B2HexColor.b2_colorSalmon;
                }
                else if (body.type == B2BodyType.b2_staticBody)
                {
                    color = B2HexColor.b2_colorPaleGreen;
                }
                else if (body.type == B2BodyType.b2_kinematicBody)
                {
                    color = B2HexColor.b2_colorRoyalBlue;
                }
                else if (body.setIndex == (int)B2SetType.b2_awakeSet)
                {
                    color = B2HexColor.b2_colorPink;
                }
                else
                {
                    color = B2HexColor.b2_colorGray;
                }

                b2DrawShape(draw, shape, bodySim.transform, color);
            }

            if (draw.drawBounds)
            {
                B2AABB aabb = shape.fatAABB;

                var array4 = new B2FixedArray4<B2Vec2>();
                Span<B2Vec2> vs = array4.AsSpan();
                vs[0] = new B2Vec2(aabb.lowerBound.X, aabb.lowerBound.Y);
                vs[1] = new B2Vec2(aabb.upperBound.X, aabb.lowerBound.Y);
                vs[2] = new B2Vec2(aabb.upperBound.X, aabb.upperBound.Y);
                vs[3] = new B2Vec2(aabb.lowerBound.X, aabb.upperBound.Y);

                draw.DrawPolygonFcn(vs, 4, B2HexColor.b2_colorGold, draw.context);
            }

            return true;
        }

        // todo this has varying order for moving shapes, causing flicker when overlapping shapes are moving
        // solution: display order by shape id modulus 3, keep 3 buckets in GLSolid* and flush in 3 passes.
        public static void b2DrawWithBounds(B2World world, B2DebugDraw draw)
        {
            B2_ASSERT(b2IsValidAABB(draw.drawingBounds));

            const float k_impulseScale = 1.0f;
            const float k_axisScale = 0.3f;
            B2HexColor speculativeColor = B2HexColor.b2_colorGainsboro;
            B2HexColor addColor = B2HexColor.b2_colorGreen;
            B2HexColor persistColor = B2HexColor.b2_colorBlue;
            B2HexColor normalColor = B2HexColor.b2_colorDimGray;
            B2HexColor impulseColor = B2HexColor.b2_colorMagenta;
            B2HexColor frictionColor = B2HexColor.b2_colorYellow;

            Span<B2HexColor> graphColors = stackalloc B2HexColor[B2_GRAPH_COLOR_COUNT]
            {
                B2HexColor.b2_colorRed, B2HexColor.b2_colorOrange, B2HexColor.b2_colorYellow, B2HexColor.b2_colorGreen,
                B2HexColor.b2_colorCyan, B2HexColor.b2_colorBlue, B2HexColor.b2_colorViolet, B2HexColor.b2_colorPink,
                B2HexColor.b2_colorChocolate, B2HexColor.b2_colorGoldenRod, B2HexColor.b2_colorCoral, B2HexColor.b2_colorBlack
            };

            int bodyCapacity = b2GetIdCapacity(world.bodyIdPool);
            b2SetBitCountAndClear(ref world.debugBodySet, bodyCapacity);

            int jointCapacity = b2GetIdCapacity(world.jointIdPool);
            b2SetBitCountAndClear(ref world.debugJointSet, jointCapacity);

            int contactCapacity = b2GetIdCapacity(world.contactIdPool);
            b2SetBitCountAndClear(ref world.debugContactSet, contactCapacity);

            B2DrawContext drawContext = new B2DrawContext();
            drawContext.world = world;
            drawContext.draw = draw;

            for (int i = 0; i < (int)B2BodyType.b2_bodyTypeCount; ++i)
            {
                b2DynamicTree_Query(world.broadPhase.trees[i], draw.drawingBounds, B2_DEFAULT_MASK_BITS, DrawQueryCallback, ref drawContext);
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

                    B2Body body = b2Array_Get(ref world.bodies, (int)bodyId);

                    if (draw.drawBodyNames && !string.IsNullOrEmpty(body.name))
                    {
                        B2Vec2 offset = new B2Vec2(0.1f, 0.1f);
                        B2BodySim bodySim = b2GetBodySim(world, body);

                        B2Transform transform = new B2Transform(bodySim.center, bodySim.transform.q);
                        B2Vec2 p = b2TransformPoint(ref transform, offset);
                        draw.DrawStringFcn(p, body.name, B2HexColor.b2_colorBlueViolet, draw.context);
                    }

                    if (draw.drawMass && body.type == B2BodyType.b2_dynamicBody)
                    {
                        B2Vec2 offset = new B2Vec2(0.1f, 0.1f);
                        B2BodySim bodySim = b2GetBodySim(world, body);

                        B2Transform transform = new B2Transform(bodySim.center, bodySim.transform.q);
                        draw.DrawTransformFcn(transform, draw.context);

                        B2Vec2 p = b2TransformPoint(ref transform, offset);

                        string buffer = string.Format("{0:F2}", body.mass);
                        draw.DrawStringFcn(p, buffer, B2HexColor.b2_colorWhite, draw.context);
                    }

                    if (draw.drawJoints)
                    {
                        int jointKey = body.headJointKey;
                        while (jointKey != B2_NULL_INDEX)
                        {
                            int jointId = jointKey >> 1;
                            int edgeIndex = jointKey & 1;
                            B2Joint joint = b2Array_Get(ref world.joints, jointId);

                            // avoid double draw
                            if (b2GetBit(ref world.debugJointSet, jointId) == false)
                            {
                                b2DrawJoint(draw, world, joint);
                                b2SetBit(ref world.debugJointSet, jointId);
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
                    if (draw.drawContacts && body.type == B2BodyType.b2_dynamicBody && body.setIndex == (int)B2SetType.b2_awakeSet)
                    {
                        int contactKey = body.headContactKey;
                        while (contactKey != B2_NULL_INDEX)
                        {
                            int contactId = contactKey >> 1;
                            int edgeIndex = contactKey & 1;
                            B2Contact contact = b2Array_Get(ref world.contacts, contactId);
                            contactKey = contact.edges[edgeIndex].nextKey;

                            if (contact.setIndex != (int)B2SetType.b2_awakeSet || contact.colorIndex == B2_NULL_INDEX)
                            {
                                continue;
                            }

                            // avoid double draw
                            if (b2GetBit(ref world.debugContactSet, contactId) == false)
                            {
                                B2_ASSERT(0 <= contact.colorIndex && contact.colorIndex < B2_GRAPH_COLOR_COUNT);

                                ref B2GraphColor gc = ref world.constraintGraph.colors[contact.colorIndex];
                                B2ContactSim contactSim = b2Array_Get(ref gc.contactSims, contact.localIndex);
                                int pointCount = contactSim.manifold.pointCount;
                                B2Vec2 normal = contactSim.manifold.normal;
                                string buffer;

                                for (int j = 0; j < pointCount; ++j)
                                {
                                    ref B2ManifoldPoint point = ref contactSim.manifold.points[j];

                                    if (draw.drawGraphColors)
                                    {
                                        // graph color
                                        float pointSize = contact.colorIndex == B2_OVERFLOW_INDEX ? 7.5f : 5.0f;
                                        draw.DrawPointFcn(point.point, pointSize, graphColors[contact.colorIndex], draw.context);
                                        // B2.g_draw.DrawString(point.position, "%d", point.color);
                                    }
                                    else if (point.separation > linearSlop)
                                    {
                                        // Speculative
                                        draw.DrawPointFcn(point.point, 5.0f, speculativeColor, draw.context);
                                    }
                                    else if (point.persisted == false)
                                    {
                                        // Add
                                        draw.DrawPointFcn(point.point, 10.0f, addColor, draw.context);
                                    }
                                    else if (point.persisted == true)
                                    {
                                        // Persist
                                        draw.DrawPointFcn(point.point, 5.0f, persistColor, draw.context);
                                    }

                                    if (draw.drawContactNormals)
                                    {
                                        B2Vec2 p1 = point.point;
                                        B2Vec2 p2 = b2MulAdd(p1, k_axisScale, normal);
                                        draw.DrawSegmentFcn(p1, p2, normalColor, draw.context);
                                    }
                                    else if (draw.drawContactImpulses)
                                    {
                                        B2Vec2 p1 = point.point;
                                        B2Vec2 p2 = b2MulAdd(p1, k_impulseScale * point.normalImpulse, normal);
                                        draw.DrawSegmentFcn(p1, p2, impulseColor, draw.context);
                                        buffer = $"{1000.0f * point.normalImpulse:F1}";
                                        draw.DrawStringFcn(p1, buffer, B2HexColor.b2_colorWhite, draw.context);
                                    }

                                    if (draw.drawContactFeatures)
                                    {
                                        buffer = "" + point.id;
                                        draw.DrawStringFcn(point.point, buffer, B2HexColor.b2_colorOrange, draw.context);
                                    }

                                    if (draw.drawFrictionImpulses)
                                    {
                                        B2Vec2 tangent = b2RightPerp(normal);
                                        B2Vec2 p1 = point.point;
                                        B2Vec2 p2 = b2MulAdd(p1, k_impulseScale * point.tangentImpulse, tangent);
                                        draw.DrawSegmentFcn(p1, p2, frictionColor, draw.context);
                                        buffer = $"{1000.0f * point.tangentImpulse:F1}";
                                        draw.DrawStringFcn(p1, buffer, B2HexColor.b2_colorWhite, draw.context);
                                    }
                                }

                                b2SetBit(ref world.debugContactSet, contactId);
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

        public static void b2World_Draw(B2WorldId worldId, B2DebugDraw draw)
        {
            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
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
                    B2SolverSet set = b2Array_Get(ref world.solverSets, setIndex);
                    int bodyCount = set.bodySims.count;
                    for (int bodyIndex = 0; bodyIndex < bodyCount; ++bodyIndex)
                    {
                        B2BodySim bodySim = set.bodySims.data[bodyIndex];
                        B2Body body = b2Array_Get(ref world.bodies, bodySim.bodyId);
                        B2_ASSERT(body.setIndex == setIndex);

                        B2Transform xf = bodySim.transform;
                        int shapeId = body.headShapeId;
                        while (shapeId != B2_NULL_INDEX)
                        {
                            B2Shape shape = world.shapes.data[shapeId];
                            B2HexColor color;

                            if (shape.customColor != 0)
                            {
                                color = (B2HexColor)shape.customColor;
                            }
                            else if (body.type == B2BodyType.b2_dynamicBody && body.mass == 0.0f)
                            {
                                // Bad body
                                color = B2HexColor.b2_colorRed;
                            }
                            else if (body.setIndex == (int)B2SetType.b2_disabledSet)
                            {
                                color = B2HexColor.b2_colorSlateGray;
                            }
                            else if (shape.sensorIndex != B2_NULL_INDEX)
                            {
                                color = B2HexColor.b2_colorWheat;
                            }
                            else if (0 != (bodySim.flags & (uint)B2BodyFlags.b2_isBullet) && body.setIndex == (int)B2SetType.b2_awakeSet)
                            {
                                color = B2HexColor.b2_colorTurquoise;
                            }
                            else if (body.isSpeedCapped)
                            {
                                color = B2HexColor.b2_colorYellow;
                            }
                            else if (0 != (bodySim.flags & (uint)B2BodyFlags.b2_isFast))
                            {
                                color = B2HexColor.b2_colorSalmon;
                            }
                            else if (body.type == B2BodyType.b2_staticBody)
                            {
                                color = B2HexColor.b2_colorPaleGreen;
                            }
                            else if (body.type == B2BodyType.b2_kinematicBody)
                            {
                                color = B2HexColor.b2_colorRoyalBlue;
                            }
                            else if (body.setIndex == (int)B2SetType.b2_awakeSet)
                            {
                                color = B2HexColor.b2_colorPink;
                            }
                            else
                            {
                                color = B2HexColor.b2_colorGray;
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
                    B2Joint joint = world.joints.data[i];
                    if (joint.setIndex == B2_NULL_INDEX)
                    {
                        continue;
                    }

                    b2DrawJoint(draw, world, joint);
                }
            }

            if (draw.drawBounds)
            {
                B2HexColor color = B2HexColor.b2_colorGold;

                int setCount = world.solverSets.count;
                var array4 = new B2FixedArray4<B2Vec2>();
                Span<B2Vec2> vs = array4.AsSpan();
                for (int setIndex = 0; setIndex < setCount; ++setIndex)
                {
                    B2SolverSet set = b2Array_Get(ref world.solverSets, setIndex);
                    int bodyCount = set.bodySims.count;
                    for (int bodyIndex = 0; bodyIndex < bodyCount; ++bodyIndex)
                    {
                        B2BodySim bodySim = set.bodySims.data[bodyIndex];

                        string buffer = "" + bodySim.bodyId;
                        draw.DrawStringFcn(bodySim.center, buffer, B2HexColor.b2_colorWhite, draw.context);

                        B2Body body = b2Array_Get(ref world.bodies, bodySim.bodyId);
                        B2_ASSERT(body.setIndex == setIndex);

                        int shapeId = body.headShapeId;
                        while (shapeId != B2_NULL_INDEX)
                        {
                            B2Shape shape = world.shapes.data[shapeId];
                            B2AABB aabb = shape.fatAABB;

                            vs[0] = new B2Vec2(aabb.lowerBound.X, aabb.lowerBound.Y);
                            vs[1] = new B2Vec2(aabb.upperBound.X, aabb.lowerBound.Y);
                            vs[2] = new B2Vec2(aabb.upperBound.X, aabb.upperBound.Y);
                            vs[3] = new B2Vec2(aabb.lowerBound.X, aabb.upperBound.Y);

                            draw.DrawPolygonFcn(vs, 4, color, draw.context);

                            shapeId = shape.nextShapeId;
                        }
                    }
                }
            }

            if (draw.drawBodyNames)
            {
                B2Vec2 offset = new B2Vec2(0.05f, 0.05f);
                int count = world.bodies.count;
                for (int i = 0; i < count; ++i)
                {
                    B2Body body = world.bodies.data[i];
                    if (body.setIndex == B2_NULL_INDEX)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(body.name))
                    {
                        continue;
                    }

                    B2BodySim bodySim = b2GetBodySim(world, body);

                    B2Transform transform = new B2Transform(bodySim.center, bodySim.transform.q);
                    B2Vec2 p = b2TransformPoint(ref transform, offset);
                    draw.DrawStringFcn(p, body.name, B2HexColor.b2_colorBlueViolet, draw.context);
                }
            }

            if (draw.drawMass)
            {
                B2Vec2 offset = new B2Vec2(0.1f, 0.1f);
                int setCount = world.solverSets.count;
                for (int setIndex = 0; setIndex < setCount; ++setIndex)
                {
                    B2SolverSet set = b2Array_Get(ref world.solverSets, setIndex);
                    int bodyCount = set.bodySims.count;
                    for (int bodyIndex = 0; bodyIndex < bodyCount; ++bodyIndex)
                    {
                        B2BodySim bodySim = set.bodySims.data[bodyIndex];

                        B2Transform transform = new B2Transform(bodySim.center, bodySim.transform.q);
                        draw.DrawTransformFcn(transform, draw.context);

                        B2Vec2 p = b2TransformPoint(ref transform, offset);

                        float mass = bodySim.invMass > 0.0f ? 1.0f / bodySim.invMass : 0.0f;
                        string buffer = $"{mass:F2}";
                        draw.DrawStringFcn(p, buffer, B2HexColor.b2_colorWhite, draw.context);
                    }
                }
            }

            if (draw.drawContacts)
            {
                const float k_impulseScale = 1.0f;
                const float k_axisScale = 0.3f;
                float linearSlop = B2_LINEAR_SLOP;

                B2HexColor speculativeColor = B2HexColor.b2_colorLightGray;
                B2HexColor addColor = B2HexColor.b2_colorGreen;
                B2HexColor persistColor = B2HexColor.b2_colorBlue;
                B2HexColor normalColor = B2HexColor.b2_colorDimGray;
                B2HexColor impulseColor = B2HexColor.b2_colorMagenta;
                B2HexColor frictionColor = B2HexColor.b2_colorYellow;

                Span<B2HexColor> colors = stackalloc B2HexColor[B2_GRAPH_COLOR_COUNT]
                {
                    B2HexColor.b2_colorRed, B2HexColor.b2_colorOrange, B2HexColor.b2_colorYellow, B2HexColor.b2_colorGreen,
                    B2HexColor.b2_colorCyan, B2HexColor.b2_colorBlue, B2HexColor.b2_colorViolet, B2HexColor.b2_colorPink,
                    B2HexColor.b2_colorChocolate, B2HexColor.b2_colorGoldenRod, B2HexColor.b2_colorCoral, B2HexColor.b2_colorBlack
                };

                for (int colorIndex = 0; colorIndex < B2_GRAPH_COLOR_COUNT; ++colorIndex)
                {
                    ref B2GraphColor graphColor = ref world.constraintGraph.colors[colorIndex];

                    int contactCount = graphColor.contactSims.count;
                    for (int contactIndex = 0; contactIndex < contactCount; ++contactIndex)
                    {
                        B2ContactSim contact = graphColor.contactSims.data[contactIndex];
                        int pointCount = contact.manifold.pointCount;
                        B2Vec2 normal = contact.manifold.normal;

                        for (int j = 0; j < pointCount; ++j)
                        {
                            ref B2ManifoldPoint point = ref contact.manifold.points[j];

                            if (draw.drawGraphColors && 0 <= colorIndex && colorIndex <= B2_GRAPH_COLOR_COUNT)
                            {
                                // graph color
                                float pointSize = colorIndex == B2_OVERFLOW_INDEX ? 7.5f : 5.0f;
                                draw.DrawPointFcn(point.point, pointSize, colors[colorIndex], draw.context);
                                // B2.g_draw.DrawString(point.position, "%d", point.color);
                            }
                            else if (point.separation > linearSlop)
                            {
                                // Speculative
                                draw.DrawPointFcn(point.point, 5.0f, speculativeColor, draw.context);
                            }
                            else if (point.persisted == false)
                            {
                                // Add
                                draw.DrawPointFcn(point.point, 10.0f, addColor, draw.context);
                            }
                            else if (point.persisted == true)
                            {
                                // Persist
                                draw.DrawPointFcn(point.point, 5.0f, persistColor, draw.context);
                            }

                            if (draw.drawContactNormals)
                            {
                                B2Vec2 p1 = point.point;
                                B2Vec2 p2 = b2MulAdd(p1, k_axisScale, normal);
                                draw.DrawSegmentFcn(p1, p2, normalColor, draw.context);
                            }
                            else if (draw.drawContactImpulses)
                            {
                                B2Vec2 p1 = point.point;
                                B2Vec2 p2 = b2MulAdd(p1, k_impulseScale * point.totalNormalImpulse, normal);
                                draw.DrawSegmentFcn(p1, p2, impulseColor, draw.context);
                                var buffer = $"{1000.0f * point.totalNormalImpulse:F2}";
                                draw.DrawStringFcn(p1, buffer, B2HexColor.b2_colorWhite, draw.context);
                            }

                            if (draw.drawContactFeatures)
                            {
                                string buffer = "" + point.id;
                                draw.DrawStringFcn(point.point, buffer, B2HexColor.b2_colorOrange, draw.context);
                            }

                            if (draw.drawFrictionImpulses)
                            {
                                B2Vec2 tangent = b2RightPerp(normal);
                                B2Vec2 p1 = point.point;
                                B2Vec2 p2 = b2MulAdd(p1, k_impulseScale * point.tangentImpulse, tangent);
                                draw.DrawSegmentFcn(p1, p2, frictionColor, draw.context);
                                var buffer = $"{1000.0f * point.tangentImpulse:F2}";
                                draw.DrawStringFcn(p1, buffer, B2HexColor.b2_colorWhite, draw.context);
                            }
                        }
                    }
                }
            }

            if (draw.drawIslands)
            {
                int count = world.islands.count;
                for (int i = 0; i < count; ++i)
                {
                    B2Island island = world.islands.data[i];
                    if (island.setIndex == B2_NULL_INDEX)
                    {
                        continue;
                    }

                    int shapeCount = 0;
                    B2AABB aabb = new B2AABB(
                        new B2Vec2(float.MaxValue, float.MaxValue),
                        new B2Vec2(-float.MaxValue, -float.MaxValue)
                    );

                    int bodyId = island.headBody;
                    while (bodyId != B2_NULL_INDEX)
                    {
                        B2Body body = b2Array_Get(ref world.bodies, bodyId);
                        int shapeId = body.headShapeId;
                        while (shapeId != B2_NULL_INDEX)
                        {
                            B2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                            aabb = b2AABB_Union(aabb, shape.fatAABB);
                            shapeCount += 1;
                            shapeId = shape.nextShapeId;
                        }

                        bodyId = body.islandNext;
                    }

                    if (shapeCount > 0)
                    {
                        B2FixedArray4<B2Vec2> vsBuffer = new B2FixedArray4<B2Vec2>();
                        Span<B2Vec2> vs = vsBuffer.AsSpan();
                        vs[0] = new B2Vec2(aabb.lowerBound.X, aabb.lowerBound.Y);
                        vs[1] = new B2Vec2(aabb.upperBound.X, aabb.lowerBound.Y);
                        vs[2] = new B2Vec2(aabb.upperBound.X, aabb.upperBound.Y);
                        vs[3] = new B2Vec2(aabb.lowerBound.X, aabb.upperBound.Y);

                        draw.DrawPolygonFcn(vs, 4, B2HexColor.b2_colorOrangeRed, draw.context);
                    }
                }
            }
        }

        /// Get the body events for the current time step. The event data is transient. Do not store a reference to this data.
        public static B2BodyEvents b2World_GetBodyEvents(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return new B2BodyEvents();
            }

            int count = world.bodyMoveEvents.count;
            B2BodyEvents events = new B2BodyEvents(world.bodyMoveEvents.data, count);
            return events;
        }

        /// Get sensor events for the current time step. The event data is transient. Do not store a reference to this data.
        public static B2SensorEvents b2World_GetSensorEvents(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return new B2SensorEvents();
            }

            // Careful to use previous buffer
            int endEventArrayIndex = 1 - world.endEventArrayIndex;

            int beginCount = world.sensorBeginEvents.count;
            int endCount = world.sensorEndEvents[endEventArrayIndex].count;

            B2SensorEvents events = new B2SensorEvents()
            {
                beginEvents = world.sensorBeginEvents.data,
                endEvents = world.sensorEndEvents[endEventArrayIndex].data,
                beginCount = beginCount,
                endCount = endCount,
            };
            return events;
        }

        /// Get contact events for this current time step. The event data is transient. Do not store a reference to this data.
        public static B2ContactEvents b2World_GetContactEvents(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return new B2ContactEvents();
            }

            // Careful to use previous buffer
            int endEventArrayIndex = 1 - world.endEventArrayIndex;

            int beginCount = world.contactBeginEvents.count;
            int endCount = world.contactEndEvents[endEventArrayIndex].count;
            int hitCount = world.contactHitEvents.count;

            B2ContactEvents events = new B2ContactEvents()
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

        /// Get the joint events for the current time step. The event data is transient. Do not store a reference to this data.
        public static B2JointEvents b2World_GetJointEvents(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return new B2JointEvents();
            }

            int count = world.jointEvents.count;
            B2JointEvents events = new B2JointEvents(world.jointEvents.data, count);
            return events;
        }

        public static bool b2World_IsValid(B2WorldId id)
        {
            if (id.index1 < 1 || B2_MAX_WORLDS < id.index1)
            {
                return false;
            }

            B2World world = b2_worlds[(id.index1 - 1)];

            if (world.worldId != id.index1 - 1)
            {
                // world is not allocated
                return false;
            }

            return id.generation == world.generation;
        }

        public static bool b2Body_IsValid(B2BodyId id)
        {
            if (B2_MAX_WORLDS <= id.world0)
            {
                // invalid world
                return false;
            }

            B2World world = b2_worlds[id.world0];
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

            B2Body body = world.bodies.data[(id.index1 - 1)];
            if (body.setIndex == B2_NULL_INDEX)
            {
                // this was freed
                return false;
            }

            B2_ASSERT(body.localIndex != B2_NULL_INDEX);

            if (body.generation != id.generation)
            {
                // this id is orphaned
                return false;
            }

            return true;
        }

        public static bool b2Shape_IsValid(B2ShapeId id)
        {
            if (B2_MAX_WORLDS <= id.world0)
            {
                return false;
            }

            B2World world = b2_worlds[id.world0];
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

            B2Shape shape = world.shapes.data[shapeId];
            if (shape.id == B2_NULL_INDEX)
            {
                // shape is free
                return false;
            }

            B2_ASSERT(shape.id == shapeId);

            return id.generation == shape.generation;
        }

        public static bool b2Chain_IsValid(B2ChainId id)
        {
            if (B2_MAX_WORLDS <= id.world0)
            {
                return false;
            }

            B2World world = b2_worlds[id.world0];
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

            B2ChainShape chain = world.chainShapes.data[chainId];
            if (chain.id == B2_NULL_INDEX)
            {
                // chain is free
                return false;
            }

            B2_ASSERT(chain.id == chainId);

            return id.generation == chain.generation;
        }

        public static bool b2Joint_IsValid(B2JointId id)
        {
            if (B2_MAX_WORLDS <= id.world0)
            {
                return false;
            }

            B2World world = b2_worlds[id.world0];
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

            B2Joint joint = world.joints.data[jointId];
            if (joint.jointId == B2_NULL_INDEX)
            {
                // joint is free
                return false;
            }

            B2_ASSERT(joint.jointId == jointId);

            return id.generation == joint.generation;
        }

        public static void b2World_EnableSleeping(B2WorldId worldId, bool flag)
        {
            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
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
                for (int i = (int)B2SetType.b2_firstSleepingSet; i < setCount; ++i)
                {
                    B2SolverSet set = b2Array_Get(ref world.solverSets, i);
                    if (set.bodySims.count > 0)
                    {
                        b2WakeSolverSet(world, i);
                    }
                }
            }
        }

        public static bool b2World_IsSleepingEnabled(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            return world.enableSleep;
        }

        /// Enable/disable constraint warm starting. Advanced feature for testing. Disabling
        /// warm starting greatly reduces stability and provides no performance gain.
        public static void b2World_EnableWarmStarting(B2WorldId worldId, bool flag)
        {
            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return;
            }

            world.enableWarmStarting = flag;
        }

        public static bool b2World_IsWarmStartingEnabled(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            return world.enableWarmStarting;
        }

        public static int b2World_GetAwakeBodyCount(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            B2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_awakeSet);
            return awakeSet.bodySims.count;
        }

        public static void b2World_EnableContinuous(B2WorldId worldId, bool flag)
        {
            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return;
            }

            world.enableContinuous = flag;
        }

        public static bool b2World_IsContinuousEnabled(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            return world.enableContinuous;
        }

        public static void b2World_SetRestitutionThreshold(B2WorldId worldId, float value)
        {
            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return;
            }

            world.restitutionThreshold = b2ClampFloat(value, 0.0f, float.MaxValue);
        }

        public static float b2World_GetRestitutionThreshold(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            return world.restitutionThreshold;
        }

        public static void b2World_SetHitEventThreshold(B2WorldId worldId, float value)
        {
            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return;
            }

            world.hitEventThreshold = b2ClampFloat(value, 0.0f, float.MaxValue);
        }

        public static float b2World_GetHitEventThreshold(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            return world.hitEventThreshold;
        }

        public static void b2World_SetContactTuning(B2WorldId worldId, float hertz, float dampingRatio, float pushSpeed)
        {
            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return;
            }

            world.contactHertz = b2ClampFloat(hertz, 0.0f, float.MaxValue);
            world.contactDampingRatio = b2ClampFloat(dampingRatio, 0.0f, float.MaxValue);
            world.contactSpeed = b2ClampFloat(pushSpeed, 0.0f, float.MaxValue);
        }

        public static void b2World_SetMaximumLinearSpeed(B2WorldId worldId, float maximumLinearSpeed)
        {
            B2_ASSERT(b2IsValidFloat(maximumLinearSpeed) && maximumLinearSpeed > 0.0f);

            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return;
            }

            world.maxLinearSpeed = maximumLinearSpeed;
        }

        public static float b2World_GetMaximumLinearSpeed(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            return world.maxLinearSpeed;
        }

        public static B2Profile b2World_GetProfile(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            return world.profile;
        }

        public static B2Counters b2World_GetCounters(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            B2Counters s = new B2Counters();
            s.bodyCount = b2GetIdCount(world.bodyIdPool);
            s.shapeCount = b2GetIdCount(world.shapeIdPool);
            s.contactCount = b2GetIdCount(world.contactIdPool);
            s.jointCount = b2GetIdCount(world.jointIdPool);
            s.islandCount = b2GetIdCount(world.islandIdPool);

            B2DynamicTree staticTree = world.broadPhase.trees[(int)B2BodyType.b2_staticBody];
            s.staticTreeHeight = b2DynamicTree_GetHeight(staticTree);

            B2DynamicTree dynamicTree = world.broadPhase.trees[(int)B2BodyType.b2_dynamicBody];
            B2DynamicTree kinematicTree = world.broadPhase.trees[(int)B2BodyType.b2_kinematicBody];
            s.treeHeight = b2MaxInt(b2DynamicTree_GetHeight(dynamicTree), b2DynamicTree_GetHeight(kinematicTree));

            s.stackUsed = b2GetMaxArenaAllocation(world.arena);
            s.byteCount = b2GetByteCount();
            s.taskCount = world.taskCount;

            for (int i = 0; i < B2_GRAPH_COLOR_COUNT; ++i)
            {
                s.colorCounts[i] = world.constraintGraph.colors[i].contactSims.count + world.constraintGraph.colors[i].jointSims.count;
            }

            return s;
        }

        public static void b2World_SetUserData(B2WorldId worldId, object userData)
        {
            B2World world = b2GetWorldFromId(worldId);
            world.userData = userData;
        }

        public static object b2World_GetUserData(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            return world.userData;
        }

        public static void b2World_SetFrictionCallback(B2WorldId worldId, b2FrictionCallback callback)
        {
            B2World world = b2GetWorldFromId(worldId);
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

        public static void b2World_SetRestitutionCallback(B2WorldId worldId, b2RestitutionCallback callback)
        {
            B2World world = b2GetWorldFromId(worldId);
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

        public static void b2World_DumpMemoryStats(B2WorldId worldId)
        {
            using StreamWriter writer = new StreamWriter("box2d_memory.txt");

            B2World world = b2GetWorldFromId(worldId);

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
            writer.Write("static tree: {0}\n", b2DynamicTree_GetByteCount(world.broadPhase.trees[(int)B2BodyType.b2_staticBody]));
            writer.Write("kinematic tree: {0}\n", b2DynamicTree_GetByteCount(world.broadPhase.trees[(int)B2BodyType.b2_kinematicBody]));
            writer.Write("dynamic tree: {0}\n", b2DynamicTree_GetByteCount(world.broadPhase.trees[(int)B2BodyType.b2_dynamicBody]));
            ref B2HashSet moveSet = ref world.broadPhase.moveSet;
            writer.Write("moveSet: {0} ({1}, {2})\n", b2GetHashSetBytes(ref moveSet), moveSet.count, moveSet.capacity);
            writer.Write("moveArray: {0}\n", b2Array_ByteCount(ref world.broadPhase.moveArray));
            ref B2HashSet pairSet = ref world.broadPhase.pairSet;
            writer.Write("pairSet: {0} ({1}, {2})\n", b2GetHashSetBytes(ref pairSet), pairSet.count, pairSet.capacity);
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
                B2SolverSet set = world.solverSets.data[i];
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
                ref B2GraphColor c = ref world.constraintGraph.colors[i];
                bodyBitSetBytes += b2GetBitSetBytes(ref c.bodySet);
                contactSimCapacity += c.contactSims.capacity;
                jointSimCapacity += c.jointSims.capacity;
            }

            writer.Write("constraint graph\n");
            writer.Write("body bit sets: {0}\n", bodyBitSetBytes);
            writer.Write("joint sim: {0}n", jointSimCapacity);
            writer.Write("contact sim: {0}n", contactSimCapacity);
            writer.Write("\n");

            // stack allocator
            writer.Write("stack allocator: {0}\n\n", b2GetArenaCapacity(world.arena));

            // chain shapes
            // todo
        }


        static bool TreeQueryCallback(int proxyId, ulong userData, ref B2WorldQueryContext context)
        {
            B2_UNUSED(proxyId);

            int shapeId = (int)userData;

            ref B2WorldQueryContext worldContext = ref context;
            B2World world = worldContext.world;

            B2Shape shape = b2Array_Get(ref world.shapes, shapeId);

            if (b2ShouldQueryCollide(shape.filter, worldContext.filter) == false)
            {
                return true;
            }

            B2ShapeId id = new B2ShapeId(shapeId + 1, world.worldId, shape.generation);
            bool result = worldContext.fcn(id, worldContext.userContext);
            return result;
        }

        public static B2TreeStats b2World_OverlapAABB(B2WorldId worldId, B2AABB aabb, B2QueryFilter filter, b2OverlapResultFcn fcn, object context)
        {
            B2TreeStats treeStats = new B2TreeStats();

            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return treeStats;
            }

            B2_ASSERT(b2IsValidAABB(aabb));

            B2WorldQueryContext worldContext = new B2WorldQueryContext(world, fcn, filter, context);

            for (int i = 0; i < (int)B2BodyType.b2_bodyTypeCount; ++i)
            {
                B2TreeStats treeResult =
                    b2DynamicTree_Query(world.broadPhase.trees[i], aabb, filter.maskBits, TreeQueryCallback, ref worldContext);

                treeStats.nodeVisits += treeResult.nodeVisits;
                treeStats.leafVisits += treeResult.leafVisits;
            }

            return treeStats;
        }


        public static bool TreeOverlapCallback(int proxyId, ulong userData, ref B2WorldOverlapContext context)
        {
            B2_UNUSED(proxyId);

            int shapeId = (int)userData;

            ref B2WorldOverlapContext worldContext = ref context;
            B2World world = worldContext.world;

            B2Shape shape = b2Array_Get(ref world.shapes, shapeId);

            if (b2ShouldQueryCollide(shape.filter, worldContext.filter) == false)
            {
                return true;
            }

            B2Body body = b2Array_Get(ref world.bodies, shape.bodyId);
            B2Transform transform = b2GetBodyTransformQuick(world, body);

            B2DistanceInput input = new B2DistanceInput();
            input.proxyA = worldContext.proxy;
            input.proxyB = b2MakeShapeDistanceProxy(shape);
            input.transformA = b2Transform_identity;
            input.transformB = transform;
            input.useRadii = true;

            B2SimplexCache cache = new B2SimplexCache();
            B2DistanceOutput output = b2ShapeDistance(ref input, ref cache, null, 0);

            float tolerance = 0.1f * B2_LINEAR_SLOP;
            if (output.distance > tolerance)
            {
                return true;
            }

            B2ShapeId id = new B2ShapeId(shape.id + 1, world.worldId, shape.generation);
            bool result = worldContext.fcn(id, worldContext.userContext);
            return result;
        }

        /// Overlap test for all shapes that overlap the provided shape proxy.
        public static B2TreeStats b2World_OverlapShape(B2WorldId worldId, ref B2ShapeProxy proxy, B2QueryFilter filter, b2OverlapResultFcn fcn, object context)
        {
            B2TreeStats treeStats = new B2TreeStats();

            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return treeStats;
            }

            B2AABB aabb = b2MakeAABB(proxy.points.AsSpan(), proxy.count, proxy.radius);
            B2WorldOverlapContext worldContext = new B2WorldOverlapContext(
                world, fcn, filter, proxy, context
            );

            for (int i = 0; i < (int)B2BodyType.b2_bodyTypeCount; ++i)
            {
                B2TreeStats treeResult =
                    b2DynamicTree_Query(world.broadPhase.trees[i], aabb, filter.maskBits, TreeOverlapCallback, ref worldContext);

                treeStats.nodeVisits += treeResult.nodeVisits;
                treeStats.leafVisits += treeResult.leafVisits;
            }

            return treeStats;
        }

        public static float RayCastCallback(ref B2RayCastInput input, int proxyId, ulong userData, ref B2WorldRayCastContext context)
        {
            B2_UNUSED(proxyId);

            int shapeId = (int)userData;

            ref B2WorldRayCastContext worldContext = ref context;
            B2World world = worldContext.world;

            B2Shape shape = b2Array_Get(ref world.shapes, shapeId);

            if (b2ShouldQueryCollide(shape.filter, worldContext.filter) == false)
            {
                return input.maxFraction;
            }

            B2Body body = b2Array_Get(ref world.bodies, shape.bodyId);
            B2Transform transform = b2GetBodyTransformQuick(world, body);
            B2CastOutput output = b2RayCastShape(ref input, shape, transform);

            if (output.hit)
            {
                B2ShapeId id = new B2ShapeId(shapeId + 1, world.worldId, shape.generation);
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

        /// Cast a ray into the world to collect shapes in the path of the ray.
        /// Your callback function controls whether you get the closest point, any point, or n-points.
        /// @note The callback function may receive shapes in any order
        /// @param worldId The world to cast the ray against
        /// @param origin The start point of the ray
        /// @param translation The translation of the ray from the start point to the end point
        /// @param filter Contains bit flags to filter unwanted shapes from the results
        /// @param fcn A user implemented callback function
        /// @param context A user context that is passed along to the callback function
        ///	@return traversal performance counters
        public static B2TreeStats b2World_CastRay(B2WorldId worldId, B2Vec2 origin, B2Vec2 translation, B2QueryFilter filter, b2CastResultFcn fcn, object context)
        {
            B2TreeStats treeStats = new B2TreeStats();

            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return treeStats;
            }

            B2_ASSERT(b2IsValidVec2(origin));
            B2_ASSERT(b2IsValidVec2(translation));

            B2RayCastInput input = new B2RayCastInput(origin, translation, 1.0f);

            B2WorldRayCastContext worldContext = new B2WorldRayCastContext(world, fcn, filter, 1.0f, context);

            for (int i = 0; i < (int)B2BodyType.b2_bodyTypeCount; ++i)
            {
                B2TreeStats treeResult =
                    b2DynamicTree_RayCast(world.broadPhase.trees[i], ref input, filter.maskBits, RayCastCallback, ref worldContext);
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
        public static float b2RayCastClosestFcn(B2ShapeId shapeId, B2Vec2 point, B2Vec2 normal, float fraction, object context)
        {
            // Ignore initial overlap
            if (fraction == 0.0f)
            {
                return -1.0f;
            }

            B2RayResult rayResult = context as B2RayResult;
            rayResult.shapeId = shapeId;
            rayResult.point = point;
            rayResult.normal = normal;
            rayResult.fraction = fraction;
            rayResult.hit = true;
            return fraction;
        }

        /// Cast a ray into the world to collect the closest hit. This is a convenience function. Ignores initial overlap.
        /// This is less general than b2World_CastRay() and does not allow for custom filtering.
        public static B2RayResult b2World_CastRayClosest(B2WorldId worldId, B2Vec2 origin, B2Vec2 translation, B2QueryFilter filter)
        {
            B2RayResult result = new B2RayResult();

            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return result;
            }

            B2_ASSERT(b2IsValidVec2(origin));
            B2_ASSERT(b2IsValidVec2(translation));

            B2RayCastInput input = new B2RayCastInput(origin, translation, 1.0f);
            B2WorldRayCastContext worldContext = new B2WorldRayCastContext(world, b2RayCastClosestFcn, filter, 1.0f, result);

            for (int i = 0; i < (int)B2BodyType.b2_bodyTypeCount; ++i)
            {
                B2TreeStats treeResult =
                    b2DynamicTree_RayCast(world.broadPhase.trees[i], ref input, filter.maskBits, RayCastCallback, ref worldContext);
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

        public static float ShapeCastCallback(ref B2ShapeCastInput input, int proxyId, ulong userData, ref B2WorldRayCastContext context)
        {
            B2_UNUSED(proxyId);

            int shapeId = (int)userData;

            ref B2WorldRayCastContext worldContext = ref context;
            B2World world = worldContext.world;

            B2Shape shape = b2Array_Get(ref world.shapes, shapeId);

            if (b2ShouldQueryCollide(shape.filter, worldContext.filter) == false)
            {
                return input.maxFraction;
            }

            B2Body body = b2Array_Get(ref world.bodies, shape.bodyId);
            B2Transform transform = b2GetBodyTransformQuick(world, body);

            B2CastOutput output = b2ShapeCastShape(ref input, shape, transform);

            if (output.hit)
            {
                B2ShapeId id = new B2ShapeId(shapeId + 1, world.worldId, shape.generation);
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

        /// Cast a shape through the world. Similar to a cast ray except that a shape is cast instead of a point.
        ///	@see b2World_CastRay
        public static B2TreeStats b2World_CastShape(B2WorldId worldId, ref B2ShapeProxy proxy, B2Vec2 translation, B2QueryFilter filter,
            b2CastResultFcn fcn, object context)
        {
            B2TreeStats treeStats = new B2TreeStats();

            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return treeStats;
            }

            B2_ASSERT(b2IsValidVec2(translation));

            B2ShapeCastInput input = new B2ShapeCastInput();
            input.proxy = proxy;
            input.translation = translation;
            input.maxFraction = 1.0f;

            B2WorldRayCastContext worldContext = new B2WorldRayCastContext(world, fcn, filter, 1.0f, context);

            for (int i = 0; i < (int)B2BodyType.b2_bodyTypeCount; ++i)
            {
                B2TreeStats treeResult =
                    b2DynamicTree_ShapeCast(world.broadPhase.trees[i], ref input, filter.maskBits, ShapeCastCallback, ref worldContext);
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

        public static float MoverCastCallback(ref B2ShapeCastInput input, int proxyId, ulong userData, ref B2WorldMoverCastContext context)
        {
            B2_UNUSED(proxyId);

            int shapeId = (int)userData;
            ref B2WorldMoverCastContext worldContext = ref context;
            B2World world = worldContext.world;

            B2Shape shape = b2Array_Get(ref world.shapes, shapeId);

            if (b2ShouldQueryCollide(shape.filter, worldContext.filter) == false)
            {
                return worldContext.fraction;
            }

            B2Body body = b2Array_Get(ref world.bodies, shape.bodyId);
            B2Transform transform = b2GetBodyTransformQuick(world, body);

            B2CastOutput output = b2ShapeCastShape(ref input, shape, transform);
            if (output.fraction == 0.0f)
            {
                // Ignore overlapping shapes
                return worldContext.fraction;
            }

            worldContext.fraction = output.fraction;
            return output.fraction;
        }

        /// Cast a capsule mover through the world. This is a special shape cast that handles sliding along other shapes while reducing
        /// clipping.
        public static float b2World_CastMover(B2WorldId worldId, ref B2Capsule mover, B2Vec2 translation, B2QueryFilter filter)
        {
            B2_ASSERT(b2IsValidVec2(translation));
            B2_ASSERT(mover.radius > 2.0f * B2_LINEAR_SLOP);

            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return 1.0f;
            }

            B2ShapeCastInput input = new B2ShapeCastInput();
            input.proxy.points[0] = mover.center1;
            input.proxy.points[1] = mover.center2;
            input.proxy.count = 2;
            input.proxy.radius = mover.radius;
            input.translation = translation;
            input.maxFraction = 1.0f;
            input.canEncroach = true;

            B2WorldMoverCastContext worldContext = new B2WorldMoverCastContext(world, filter, 1.0f);

            for (int i = 0; i < (int)B2BodyType.b2_bodyTypeCount; ++i)
            {
                b2DynamicTree_ShapeCast(world.broadPhase.trees[i], ref input, filter.maskBits, MoverCastCallback, ref worldContext);

                if (worldContext.fraction == 0.0f)
                {
                    return 0.0f;
                }

                input.maxFraction = worldContext.fraction;
            }

            return worldContext.fraction;
        }

        public static bool TreeCollideCallback(int proxyId, ulong userData, ref B2WorldMoverContext context)
        {
            B2_UNUSED(proxyId);

            int shapeId = (int)userData;
            ref B2WorldMoverContext worldContext = ref context;
            B2World world = worldContext.world;

            B2Shape shape = b2Array_Get(ref world.shapes, shapeId);

            if (b2ShouldQueryCollide(shape.filter, worldContext.filter) == false)
            {
                return true;
            }


            B2Body body = b2Array_Get(ref world.bodies, shape.bodyId);
            B2Transform transform = b2GetBodyTransformQuick(world, body);

            B2PlaneResult result = b2CollideMover(ref worldContext.mover, shape, transform);

            // todo handle deep overlap
            if (result.hit && b2IsNormalized(result.plane.normal))
            {
                B2ShapeId id = new B2ShapeId(shape.id + 1, world.worldId, shape.generation);
                return worldContext.fcn(id, ref result, worldContext.userContext);
            }

            return true;
        }


        /// Collide a capsule mover with the world, gathering collision planes that can be fed to b2SolvePlanes. Useful for
        /// kinematic character movement
        // It is tempting to use a shape proxy for the mover, but this makes handling deep overlap difficult and the generality may
        // not be worth it.
        public static void b2World_CollideMover(B2WorldId worldId, ref B2Capsule mover, B2QueryFilter filter, b2PlaneResultFcn fcn, object context)
        {
            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return;
            }

            B2Vec2 r = new B2Vec2(mover.radius, mover.radius);

            B2AABB aabb;
            aabb.lowerBound = b2Sub(b2Min(mover.center1, mover.center2), r);
            aabb.upperBound = b2Add(b2Max(mover.center1, mover.center2), r);

            B2WorldMoverContext worldContext = new B2WorldMoverContext();
            worldContext.world = world;
            worldContext.fcn = fcn;
            worldContext.filter = filter;
            worldContext.mover = mover;
            worldContext.userContext = context;

            for (int i = 0; i < (int)B2BodyType.b2_bodyTypeCount; ++i)
            {
                b2DynamicTree_Query(world.broadPhase.trees[i], aabb, filter.maskBits, TreeCollideCallback, ref worldContext);
            }
        }

#if FALSE
void b2World_Dump()
{
	if (m_locked)
	{
		return;
	}

	b2OpenDump("box2d_dump.inl");

	b2Dump("B2Vec2 g(%.9g, %.9g);\n", m_gravity.x, m_gravity.y);
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

        public static void b2World_SetCustomFilterCallback(B2WorldId worldId, b2CustomFilterFcn fcn, object context)
        {
            B2World world = b2GetWorldFromId(worldId);
            world.customFilterFcn = fcn;
            world.customFilterContext = context;
        }

        public static void b2World_SetPreSolveCallback(B2WorldId worldId, b2PreSolveFcn fcn, object context)
        {
            B2World world = b2GetWorldFromId(worldId);
            world.preSolveFcn = fcn;
            world.preSolveContext = context;
        }

        public static void b2World_SetGravity(B2WorldId worldId, B2Vec2 gravity)
        {
            B2World world = b2GetWorldFromId(worldId);
            world.gravity = gravity;
        }

        public static B2Vec2 b2World_GetGravity(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            return world.gravity;
        }

        public static bool ExplosionCallback(int proxyId, ulong userData, ref B2ExplosionContext context)
        {
            B2_UNUSED(proxyId);

            int shapeId = (int)userData;

            ref B2ExplosionContext explosionContext = ref context;
            B2World world = explosionContext.world;

            B2Shape shape = b2Array_Get(ref world.shapes, shapeId);

            B2Body body = b2Array_Get(ref world.bodies, shape.bodyId);
            B2_ASSERT(body.type == B2BodyType.b2_dynamicBody);

            B2Transform transform = b2GetBodyTransformQuick(world, body);

            B2DistanceInput input = new B2DistanceInput();
            input.proxyA = b2MakeShapeDistanceProxy(shape);
            input.proxyB = b2MakeProxy(explosionContext.position, 1, 0.0f);
            input.transformA = transform;
            input.transformB = b2Transform_identity;
            input.useRadii = true;

            B2SimplexCache cache = new B2SimplexCache();
            B2DistanceOutput output = b2ShapeDistance(ref input, ref cache, null, 0);

            float radius = explosionContext.radius;
            float falloff = explosionContext.falloff;
            if (output.distance > radius + falloff)
            {
                return true;
            }

            b2WakeBody(world, body);

            if (body.setIndex != (int)B2SetType.b2_awakeSet)
            {
                return true;
            }

            B2Vec2 closestPoint = output.pointA;
            if (output.distance == 0.0f)
            {
                B2Vec2 localCentroid = b2GetShapeCentroid(shape);
                closestPoint = b2TransformPoint(ref transform, localCentroid);
            }

            B2Vec2 direction = b2Sub(closestPoint, explosionContext.position);
            if (b2LengthSquared(direction) > 100.0f * FLT_EPSILON * FLT_EPSILON)
            {
                direction = b2Normalize(direction);
            }
            else
            {
                direction = new B2Vec2(1.0f, 0.0f);
            }

            B2Vec2 localLine = b2InvRotateVector(transform.q, b2LeftPerp(direction));
            float perimeter = b2GetShapeProjectedPerimeter(shape, localLine);
            float scale = 1.0f;
            if (output.distance > radius && falloff > 0.0f)
            {
                scale = b2ClampFloat((radius + falloff - output.distance) / falloff, 0.0f, 1.0f);
            }

            float magnitude = explosionContext.impulsePerLength * perimeter * scale;
            B2Vec2 impulse = b2MulSV(magnitude, direction);

            int localIndex = body.localIndex;
            B2SolverSet set = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_awakeSet);
            B2BodyState state = b2Array_Get(ref set.bodyStates, localIndex);
            B2BodySim bodySim = b2Array_Get(ref set.bodySims, localIndex);
            state.linearVelocity = b2MulAdd(state.linearVelocity, bodySim.invMass, impulse);
            state.angularVelocity += bodySim.invInertia * b2Cross(b2Sub(closestPoint, bodySim.center), impulse);

            return true;
        }

        public static void b2World_Explode(B2WorldId worldId, ref B2ExplosionDef explosionDef)
        {
            ulong maskBits = explosionDef.maskBits;
            B2Vec2 position = explosionDef.position;
            float radius = explosionDef.radius;
            float falloff = explosionDef.falloff;
            float impulsePerLength = explosionDef.impulsePerLength;

            B2_ASSERT(b2IsValidVec2(position));
            B2_ASSERT(b2IsValidFloat(radius) && radius >= 0.0f);
            B2_ASSERT(b2IsValidFloat(falloff) && falloff >= 0.0f);
            B2_ASSERT(b2IsValidFloat(impulsePerLength));

            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return;
            }

            B2ExplosionContext explosionContext = new B2ExplosionContext(world, position, radius, falloff, impulsePerLength);

            B2AABB aabb;
            aabb.lowerBound.X = position.X - (radius + falloff);
            aabb.lowerBound.Y = position.Y - (radius + falloff);
            aabb.upperBound.X = position.X + (radius + falloff);
            aabb.upperBound.Y = position.Y + (radius + falloff);

            b2DynamicTree_Query(world.broadPhase.trees[(int)B2BodyType.b2_dynamicBody], aabb, maskBits, ExplosionCallback, ref explosionContext);
        }

        public static void b2World_RebuildStaticTree(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return;
            }

            B2DynamicTree staticTree = world.broadPhase.trees[(int)B2BodyType.b2_staticBody];
            b2DynamicTree_Rebuild(staticTree, true);
        }

        public static void b2World_EnableSpeculative(B2WorldId worldId, bool flag)
        {
            B2World world = b2GetWorldFromId(worldId);
            world.enableSpeculative = flag;
        }

#if DEBUG
        // When validating islands ids I have to compare the root island
        // ids because islands are not merged until the next time step.
        public static int b2GetRootIslandId(B2World world, int islandId)
        {
            if (islandId == B2_NULL_INDEX)
            {
                return B2_NULL_INDEX;
            }

            B2Island island = b2Array_Get(ref world.islands, islandId);

            int rootId = islandId;
            B2Island rootIsland = island;
            while (rootIsland.parentIsland != B2_NULL_INDEX)
            {
                B2Island parent = b2Array_Get(ref world.islands, rootIsland.parentIsland);
                rootId = rootIsland.parentIsland;
                rootIsland = parent;
            }

            return rootId;
        }

        // This validates island graph connectivity for each body
        public static void b2ValidateConnectivity(B2World world)
        {
            B2Body[] bodies = world.bodies.data;
            int bodyCapacity = world.bodies.count;

            for (int bodyIndex = 0; bodyIndex < bodyCapacity; ++bodyIndex)
            {
                B2Body body = bodies[bodyIndex];
                if (body.id == B2_NULL_INDEX)
                {
                    b2ValidateFreeId(world.bodyIdPool, bodyIndex);
                    continue;
                }

                b2ValidateUsedId(world.bodyIdPool, bodyIndex);

                B2_ASSERT(bodyIndex == body.id);

                // Need to get the root island because islands are not merged until the next time step
                int bodyIslandId = b2GetRootIslandId(world, body.islandId);
                int bodySetIndex = body.setIndex;

                int contactKey = body.headContactKey;
                while (contactKey != B2_NULL_INDEX)
                {
                    int contactId = contactKey >> 1;
                    int edgeIndex = contactKey & 1;

                    B2Contact contact = b2Array_Get(ref world.contacts, contactId);

                    bool touching = (contact.flags & (uint)B2ContactFlags.b2_contactTouchingFlag) != 0;
                    if (touching)
                    {
                        if (bodySetIndex != (int)B2SetType.b2_staticSet)
                        {
                            int contactIslandId = b2GetRootIslandId(world, contact.islandId);
                            B2_ASSERT(contactIslandId == bodyIslandId);
                        }
                    }
                    else
                    {
                        B2_ASSERT(contact.islandId == B2_NULL_INDEX);
                    }

                    contactKey = contact.edges[edgeIndex].nextKey;
                }

                int jointKey = body.headJointKey;
                while (jointKey != B2_NULL_INDEX)
                {
                    int jointId = jointKey >> 1;
                    int edgeIndex = jointKey & 1;

                    B2Joint joint = b2Array_Get(ref world.joints, jointId);

                    int otherEdgeIndex = edgeIndex ^ 1;

                    B2Body otherBody = b2Array_Get(ref world.bodies, joint.edges[otherEdgeIndex].bodyId);

                    if (bodySetIndex == (int)B2SetType.b2_disabledSet || otherBody.setIndex == (int)B2SetType.b2_disabledSet)
                    {
                        B2_ASSERT(joint.islandId == B2_NULL_INDEX);
                    }
                    else if (bodySetIndex == (int)B2SetType.b2_staticSet)
                    {
                        if (otherBody.setIndex == (int)B2SetType.b2_staticSet)
                        {
                            B2_ASSERT(joint.islandId == B2_NULL_INDEX);
                        }
                    }
                    else
                    {
                        int jointIslandId = b2GetRootIslandId(world, joint.islandId);
                        B2_ASSERT(jointIslandId == bodyIslandId);
                    }

                    jointKey = joint.edges[edgeIndex].nextKey;
                }
            }
        }

        // Validates solver sets, but not island connectivity
        public static void b2ValidateSolverSets(B2World world)
        {
            B2_ASSERT(b2GetIdCapacity(world.bodyIdPool) == world.bodies.count);
            B2_ASSERT(b2GetIdCapacity(world.contactIdPool) == world.contacts.count);
            B2_ASSERT(b2GetIdCapacity(world.jointIdPool) == world.joints.count);
            B2_ASSERT(b2GetIdCapacity(world.islandIdPool) == world.islands.count);
            B2_ASSERT(b2GetIdCapacity(world.solverSetIdPool) == world.solverSets.count);

            int activeSetCount = 0;
            int totalBodyCount = 0;
            int totalJointCount = 0;
            int totalContactCount = 0;
            int totalIslandCount = 0;

            // Validate all solver sets
            int setCount = world.solverSets.count;
            for (int setIndex = 0; setIndex < setCount; ++setIndex)
            {
                B2SolverSet set = world.solverSets.data[setIndex];
                if (set.setIndex != B2_NULL_INDEX)
                {
                    activeSetCount += 1;

                    if (setIndex == (int)B2SetType.b2_staticSet)
                    {
                        B2_ASSERT(set.contactSims.count == 0);
                        B2_ASSERT(set.islandSims.count == 0);
                        B2_ASSERT(set.bodyStates.count == 0);
                    }
                    else if (setIndex == (int)B2SetType.b2_awakeSet)
                    {
                        B2_ASSERT(set.bodySims.count == set.bodyStates.count);
                        B2_ASSERT(set.jointSims.count == 0);
                    }
                    else if (setIndex == (int)B2SetType.b2_disabledSet)
                    {
                        B2_ASSERT(set.islandSims.count == 0);
                        B2_ASSERT(set.bodyStates.count == 0);
                    }
                    else
                    {
                        B2_ASSERT(set.bodyStates.count == 0);
                    }

                    // Validate bodies
                    {
                        B2Body[] bodies = world.bodies.data;
                        B2_ASSERT(set.bodySims.count >= 0);
                        totalBodyCount += set.bodySims.count;
                        for (int i = 0; i < set.bodySims.count; ++i)
                        {
                            B2BodySim bodySim = set.bodySims.data[i];

                            int bodyId = bodySim.bodyId;
                            B2_ASSERT(0 <= bodyId && bodyId < world.bodies.count);
                            B2Body body = bodies[bodyId];
                            B2_ASSERT(body.setIndex == setIndex);
                            B2_ASSERT(body.localIndex == i);
                            B2_ASSERT(body.generation == body.generation);

                            if (setIndex == (int)B2SetType.b2_disabledSet)
                            {
                                B2_ASSERT(body.headContactKey == B2_NULL_INDEX);
                            }

                            // Validate body shapes
                            int prevShapeId = B2_NULL_INDEX;
                            int shapeId = body.headShapeId;
                            while (shapeId != B2_NULL_INDEX)
                            {
                                B2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                                B2_ASSERT(shape.id == shapeId);
                                B2_ASSERT(shape.prevShapeId == prevShapeId);

                                if (setIndex == (int)B2SetType.b2_disabledSet)
                                {
                                    B2_ASSERT(shape.proxyKey == B2_NULL_INDEX);
                                }
                                else if (setIndex == (int)B2SetType.b2_staticSet)
                                {
                                    B2_ASSERT(B2_PROXY_TYPE(shape.proxyKey) == B2BodyType.b2_staticBody);
                                }
                                else
                                {
                                    B2BodyType proxyType = B2_PROXY_TYPE(shape.proxyKey);
                                    B2_ASSERT(proxyType == B2BodyType.b2_kinematicBody || proxyType == B2BodyType.b2_dynamicBody);
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

                                B2Contact contact = b2Array_Get(ref world.contacts, contactId);
                                B2_ASSERT(contact.setIndex != (int)B2SetType.b2_staticSet);
                                B2_ASSERT(contact.edges[0].bodyId == bodyId || contact.edges[1].bodyId == bodyId);
                                contactKey = contact.edges[edgeIndex].nextKey;
                            }

                            // Validate body joints
                            int jointKey = body.headJointKey;
                            while (jointKey != B2_NULL_INDEX)
                            {
                                int jointId = jointKey >> 1;
                                int edgeIndex = jointKey & 1;

                                B2Joint joint = b2Array_Get(ref world.joints, jointId);

                                int otherEdgeIndex = edgeIndex ^ 1;

                                B2Body otherBody = b2Array_Get(ref world.bodies, joint.edges[otherEdgeIndex].bodyId);

                                if (setIndex == (int)B2SetType.b2_disabledSet || otherBody.setIndex == (int)B2SetType.b2_disabledSet)
                                {
                                    B2_ASSERT(joint.setIndex == (int)B2SetType.b2_disabledSet);
                                }
                                else if (setIndex == (int)B2SetType.b2_staticSet && otherBody.setIndex == (int)B2SetType.b2_staticSet)
                                {
                                    B2_ASSERT(joint.setIndex == (int)B2SetType.b2_staticSet);
                                }
                                else if (setIndex == (int)B2SetType.b2_awakeSet)
                                {
                                    B2_ASSERT(joint.setIndex == (int)B2SetType.b2_awakeSet);
                                }
                                else if (setIndex >= (int)B2SetType.b2_firstSleepingSet)
                                {
                                    B2_ASSERT(joint.setIndex == setIndex);
                                }

                                B2JointSim jointSim = b2GetJointSim(world, joint);
                                B2_ASSERT(jointSim.jointId == jointId);
                                B2_ASSERT(jointSim.bodyIdA == joint.edges[0].bodyId);
                                B2_ASSERT(jointSim.bodyIdB == joint.edges[1].bodyId);

                                jointKey = joint.edges[edgeIndex].nextKey;
                            }
                        }
                    }

                    // Validate contacts
                    {
                        B2_ASSERT(set.contactSims.count >= 0);
                        totalContactCount += set.contactSims.count;
                        for (int i = 0; i < set.contactSims.count; ++i)
                        {
                            B2ContactSim contactSim = set.contactSims.data[i];
                            B2Contact contact = b2Array_Get(ref world.contacts, contactSim.contactId);
                            if (setIndex == (int)B2SetType.b2_awakeSet)
                            {
                                // contact should be non-touching if awake
                                // or it could be this contact hasn't been transferred yet
                                B2_ASSERT(contactSim.manifold.pointCount == 0 ||
                                          (contactSim.simFlags & (uint)B2ContactSimFlags.b2_simStartedTouching) != 0);
                            }

                            B2_ASSERT(contact.setIndex == setIndex);
                            B2_ASSERT(contact.colorIndex == B2_NULL_INDEX);
                            B2_ASSERT(contact.localIndex == i);
                        }
                    }

                    // Validate joints
                    {
                        B2_ASSERT(set.jointSims.count >= 0);
                        totalJointCount += set.jointSims.count;
                        for (int i = 0; i < set.jointSims.count; ++i)
                        {
                            B2JointSim jointSim = set.jointSims.data[i];
                            B2Joint joint = b2Array_Get(ref world.joints, jointSim.jointId);
                            B2_ASSERT(joint.setIndex == setIndex);
                            B2_ASSERT(joint.colorIndex == B2_NULL_INDEX);
                            B2_ASSERT(joint.localIndex == i);
                        }
                    }

                    // Validate islands
                    {
                        B2_ASSERT(set.islandSims.count >= 0);
                        totalIslandCount += set.islandSims.count;
                        for (int i = 0; i < set.islandSims.count; ++i)
                        {
                            B2IslandSim islandSim = set.islandSims.data[i];
                            B2Island island = b2Array_Get(ref world.islands, islandSim.islandId);
                            B2_ASSERT(island.setIndex == setIndex);
                            B2_ASSERT(island.localIndex == i);
                        }
                    }
                }
                else
                {
                    B2_ASSERT(set.bodySims.count == 0);
                    B2_ASSERT(set.contactSims.count == 0);
                    B2_ASSERT(set.jointSims.count == 0);
                    B2_ASSERT(set.islandSims.count == 0);
                    B2_ASSERT(set.bodyStates.count == 0);
                }
            }

            int setIdCount = b2GetIdCount(world.solverSetIdPool);
            B2_ASSERT(activeSetCount == setIdCount);

            int bodyIdCount = b2GetIdCount(world.bodyIdPool);
            B2_ASSERT(totalBodyCount == bodyIdCount);

            int islandIdCount = b2GetIdCount(world.islandIdPool);
            B2_ASSERT(totalIslandCount == islandIdCount);

            // Validate constraint graph
            for (int colorIndex = 0; colorIndex < B2_GRAPH_COLOR_COUNT; ++colorIndex)
            {
                ref B2GraphColor color = ref world.constraintGraph.colors[colorIndex];
                {
                    B2_ASSERT(color.contactSims.count >= 0);
                    totalContactCount += color.contactSims.count;
                    for (int i = 0; i < color.contactSims.count; ++i)
                    {
                        B2ContactSim contactSim = color.contactSims.data[i];
                        B2Contact contact = b2Array_Get(ref world.contacts, contactSim.contactId);
                        // contact should be touching in the constraint graph or awaiting transfer to non-touching
                        B2_ASSERT(contactSim.manifold.pointCount > 0 ||
                                  (contactSim.simFlags & ((uint)B2ContactSimFlags.b2_simStoppedTouching | (uint)B2ContactSimFlags.b2_simDisjoint)) != 0);
                        B2_ASSERT(contact.setIndex == (int)B2SetType.b2_awakeSet);
                        B2_ASSERT(contact.colorIndex == colorIndex);
                        B2_ASSERT(contact.localIndex == i);

                        int bodyIdA = contact.edges[0].bodyId;
                        int bodyIdB = contact.edges[1].bodyId;

                        if (colorIndex < B2_OVERFLOW_INDEX)
                        {
                            B2Body bodyA = b2Array_Get(ref world.bodies, bodyIdA);
                            B2Body bodyB = b2Array_Get(ref world.bodies, bodyIdB);
                            B2_ASSERT(b2GetBit(ref color.bodySet, bodyIdA) == (bodyA.type != B2BodyType.b2_staticBody));
                            B2_ASSERT(b2GetBit(ref color.bodySet, bodyIdB) == (bodyB.type != B2BodyType.b2_staticBody));
                        }
                    }
                }

                {
                    B2_ASSERT(color.jointSims.count >= 0);
                    totalJointCount += color.jointSims.count;
                    for (int i = 0; i < color.jointSims.count; ++i)
                    {
                        B2JointSim jointSim = color.jointSims.data[i];
                        B2Joint joint = b2Array_Get(ref world.joints, jointSim.jointId);
                        B2_ASSERT(joint.setIndex == (int)B2SetType.b2_awakeSet);
                        B2_ASSERT(joint.colorIndex == colorIndex);
                        B2_ASSERT(joint.localIndex == i);

                        int bodyIdA = joint.edges[0].bodyId;
                        int bodyIdB = joint.edges[1].bodyId;

                        if (colorIndex < B2_OVERFLOW_INDEX)
                        {
                            B2Body bodyA = b2Array_Get(ref world.bodies, bodyIdA);
                            B2Body bodyB = b2Array_Get(ref world.bodies, bodyIdB);
                            B2_ASSERT(b2GetBit(ref color.bodySet, bodyIdA) == (bodyA.type != B2BodyType.b2_staticBody));
                            B2_ASSERT(b2GetBit(ref color.bodySet, bodyIdB) == (bodyB.type != B2BodyType.b2_staticBody));
                        }
                    }
                }
            }

            int contactIdCount = b2GetIdCount(world.contactIdPool);
            B2_ASSERT(totalContactCount == contactIdCount);
            B2_ASSERT(totalContactCount == (int)world.broadPhase.pairSet.count);

            int jointIdCount = b2GetIdCount(world.jointIdPool);
            B2_ASSERT(totalJointCount == jointIdCount);

// Validate shapes
// This is very slow on compounds
#if FALSE
	int shapeCapacity = b2Array(world.shapeArray).count;
	for (int shapeIndex = 0; shapeIndex < shapeCapacity; shapeIndex += 1)
	{
		b2Shape* shape = world.shapeArray + shapeIndex;
		if (shape.id != shapeIndex)
		{
			continue;
		}

		B2_ASSERT(0 <= shape.bodyId && shape.bodyId < b2Array(world.bodyArray).count);

		b2Body* body = world.bodyArray + shape.bodyId;
		B2_ASSERT(0 <= body.setIndex && body.setIndex < b2Array(world.solverSetArray).count);

		b2SolverSet* set = world.solverSetArray + body.setIndex;
		B2_ASSERT(0 <= body.localIndex && body.localIndex < set.sims.count);

		b2BodySim* bodySim = set.sims.data + body.localIndex;
		B2_ASSERT(bodySim.bodyId == shape.bodyId);

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

		B2_ASSERT(found);
		B2_ASSERT(shapeCount == body.shapeCount);
	}
#endif
        }

        // Validate contact touching status.
        public static void b2ValidateContacts(B2World world)
        {
            int contactCount = world.contacts.count;
            B2_ASSERT(contactCount == b2GetIdCapacity(world.contactIdPool));
            int allocatedContactCount = 0;

            for (int contactIndex = 0; contactIndex < contactCount; ++contactIndex)
            {
                B2Contact contact = b2Array_Get(ref world.contacts, contactIndex);
                if (contact.contactId == B2_NULL_INDEX)
                {
                    continue;
                }

                B2_ASSERT(contact.contactId == contactIndex);

                allocatedContactCount += 1;

                bool touching = (contact.flags & (uint)B2ContactFlags.b2_contactTouchingFlag) != 0;

                int setId = contact.setIndex;

                if (setId == (int)B2SetType.b2_awakeSet)
                {
                    if (touching)
                    {
                        B2_ASSERT(0 <= contact.colorIndex && contact.colorIndex < B2_GRAPH_COLOR_COUNT);
                    }
                    else
                    {
                        B2_ASSERT(contact.colorIndex == B2_NULL_INDEX);
                    }
                }
                else if (setId >= (int)B2SetType.b2_firstSleepingSet)
                {
                    // Only touching contacts allowed in a sleeping set
                    B2_ASSERT(touching == true);
                }
                else
                {
                    // Sleeping and non-touching contacts belong in the disabled set
                    B2_ASSERT(touching == false && setId == (int)B2SetType.b2_disabledSet);
                }

                B2ContactSim contactSim = b2GetContactSim(world, contact);
                B2_ASSERT(contactSim.contactId == contactIndex);
                B2_ASSERT(contactSim.bodyIdA == contact.edges[0].bodyId);
                B2_ASSERT(contactSim.bodyIdB == contact.edges[1].bodyId);

                bool simTouching = (contactSim.simFlags & (uint)B2ContactSimFlags.b2_simTouchingFlag) != 0;
                B2_ASSERT(touching == simTouching);

                B2_ASSERT(0 <= contactSim.manifold.pointCount && contactSim.manifold.pointCount <= 2);
            }

            int contactIdCount = b2GetIdCount(world.contactIdPool);
            B2_ASSERT(allocatedContactCount == contactIdCount);
        }

#else
        public static void b2ValidateConnectivity(B2World world)
        {
            B2_UNUSED(world);
        }

        public static void b2ValidateSolverSets(B2World world)
        {
            B2_UNUSED(world);
        }

        public static void b2ValidateContacts(B2World world)
        {
            B2_UNUSED(world);
        }

#endif
        /**
 * @defgroup contact Contact
 * Access to contacts
 * @{
 */
        /// Contact identifier validation. Provides validation for up to 2^32 allocations.
        public static bool b2Contact_IsValid(B2ContactId id)
        {
            if (B2_MAX_WORLDS <= id.world0)
            {
                return false;
            }

            B2World world = b2_worlds[id.world0];
            if (world.worldId != id.world0)
            {
                // world is free
                return false;
            }

            int contactId = id.index1 - 1;
            if (contactId < 0 || world.contacts.count <= contactId)
            {
                return false;
            }

            B2Contact contact = world.contacts.data[contactId];
            if (contact.contactId == B2_NULL_INDEX)
            {
                // contact is free
                return false;
            }

            B2_ASSERT(contact.contactId == contactId);

            return id.generation == contact.generation;
        }
    }
}