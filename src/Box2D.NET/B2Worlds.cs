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
using static Box2D.NET.B2BroadPhases;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2ConstraintGraphs;
using static Box2D.NET.B2BitSets;
using static Box2D.NET.B2SolverSets;
using static Box2D.NET.B2AABBs;
using static Box2D.NET.B2CTZs;
using static Box2D.NET.B2Islands;
using static Box2D.NET.B2Timers;
using static Box2D.NET.B2Sensors;
using static Box2D.NET.B2ParallelFors;
using static Box2D.NET.B2Schedulers;

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

        public static B2World b2GetUnlockedWorldFromId(B2WorldId id)
        {
            B2_ASSERT(1 <= id.index1 && id.index1 <= B2_MAX_WORLDS);
            B2World world = b2_worlds[(id.index1 - 1)];
            B2_ASSERT(id.index1 == world.worldId + 1);
            B2_ASSERT(id.generation == world.generation);

            // A world accessed from an id should not be locked
            if (world.locked)
            {
                B2_ASSERT(false);
                return null;
            }

            return world;
        }

        public static B2World b2GetWorldFromId(B2WorldId id)
        {
            B2_ASSERT(1 <= id.index1 && id.index1 <= B2_MAX_WORLDS);
            B2World world = b2_worlds[id.index1 - 1];
            B2_ASSERT(id.index1 == world.worldId + 1);
            B2_ASSERT(id.generation == world.generation);
            return world;
        }

        internal static B2World b2GetWorld(int index)
        {
            B2_ASSERT(0 <= index && index < B2_MAX_WORLDS);
            B2World world = b2_worlds[index];
            B2_ASSERT(world.worldId == index);
            return world;
        }

        internal static B2World b2GetWorldLocked(int index)
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

        internal static object b2DefaultAddTaskFcn(b2TaskCallback task, object taskContext, object userContext)
        {
            B2_UNUSED(userContext);
            task(taskContext);
            return null;
        }

        internal static void b2DefaultFinishTaskFcn(object userTask, object userContext)
        {
            B2_UNUSED(userTask, userContext);
        }

        private static void b2CreateWorkerContexts(B2World world)
        {
            world.taskContexts = b2Array_Create<B2TaskContext>(world.workerCount);
            b2Array_ResizeAndSetZero(ref world.taskContexts, world.workerCount);

            world.sensorTaskContexts = b2Array_Create<B2SensorTaskContext>(world.workerCount);
            b2Array_ResizeAndSetZero(ref world.sensorTaskContexts, world.workerCount);

            for (int i = 0; i < world.workerCount; ++i)
            {
                world.taskContexts.data[i].sensorHits = b2Array_Create<B2SensorHit>(8);
                world.taskContexts.data[i].pairKeys = b2Array_Create<ulong>();
                world.taskContexts.data[i].contactStateBitSet = b2CreateBitSet(1024);
                world.taskContexts.data[i].hitEventBitSet = b2CreateBitSet(1024);
                world.taskContexts.data[i].hasHitEvents = false;
                world.taskContexts.data[i].jointStateBitSet = b2CreateBitSet(1024);
                world.taskContexts.data[i].enlargedSimBitSet = b2CreateBitSet(256);
                world.taskContexts.data[i].awakeIslandBitSet = b2CreateBitSet(256);
                world.taskContexts.data[i].splitIslandId = B2_NULL_INDEX;

                world.sensorTaskContexts.data[i].eventBits = b2CreateBitSet(128);
            }
        }

        private static void b2DestroyWorkerContexts(B2World world)
        {
            for (int i = 0; i < world.workerCount; ++i)
            {
                b2Array_Destroy(ref world.taskContexts.data[i].sensorHits);
                b2Array_Destroy(ref world.taskContexts.data[i].pairKeys);
                b2DestroyBitSet(ref world.taskContexts.data[i].contactStateBitSet);
                b2DestroyBitSet(ref world.taskContexts.data[i].hitEventBitSet);
                b2DestroyBitSet(ref world.taskContexts.data[i].jointStateBitSet);
                b2DestroyBitSet(ref world.taskContexts.data[i].enlargedSimBitSet);
                b2DestroyBitSet(ref world.taskContexts.data[i].awakeIslandBitSet);

                b2DestroyBitSet(ref world.sensorTaskContexts.data[i].eventBits);
            }

            b2Array_Destroy(ref world.taskContexts);
            b2Array_Destroy(ref world.sensorTaskContexts);
        }

        private static void b2UseSerialTaskSystem(B2World world)
        {
            if (world.scheduler != null)
            {
                b2DestroyScheduler(world.scheduler);
            }

            world.workerCount = 1;
            world.enqueueTaskFcn = b2DefaultAddTaskFcn;
            world.finishTaskFcn = b2DefaultFinishTaskFcn;
            world.userTaskContext = null;
            world.scheduler = null;
        }

        private static void b2UseBuiltInScheduler(B2World world, int workerCount)
        {
            if (world.scheduler != null)
            {
                b2DestroyScheduler(world.scheduler);
            }

            world.workerCount = b2MinInt(workerCount, B2_MAX_WORKERS);
            world.scheduler = b2CreateScheduler(world.workerCount);
            world.enqueueTaskFcn = b2SchedulerEnqueueTask;
            world.finishTaskFcn = b2SchedulerFinishTask;
            world.userTaskContext = world.scheduler;
        }
        /// Create a world for rigid body simulation. A world contains bodies, shapes, and constraints. You make create
        /// up to 128 worlds. Each world is completely independent and may be simulated in parallel.
        /// @return the world id.
        public static B2WorldId b2CreateWorld(in B2WorldDef def)
        {
            // check
            B2RuntimeValidator.Shared.ThrowIfSafeRuntimePlatform();

            B2_ASSERT(B2_MAX_WORLDS < ushort.MaxValue, "B2_MAX_WORLDS limit exceeded");
            B2_CHECK_DEF(def);
            if (B2_CHECK_INPUT(b2IsValidVec2(def.gravity)) == false) return new B2WorldId(0, 0);
            if (B2_CHECK_INPUT(b2IsValidFloat(def.restitutionThreshold)) == false) return new B2WorldId(0, 0);
            if (B2_CHECK_INPUT(def.restitutionIterations >= 0) == false) return new B2WorldId(0, 0);
            if (B2_CHECK_INPUT(b2IsValidFloat(def.hitEventThreshold)) == false) return new B2WorldId(0, 0);
            if (B2_CHECK_INPUT(b2IsValidFloat(def.contactHertz)) == false) return new B2WorldId(0, 0);
            if (B2_CHECK_INPUT(b2IsValidFloat(def.contactDampingRatio)) == false) return new B2WorldId(0, 0);
            if (B2_CHECK_INPUT(b2IsValidFloat(def.contactSpeed)) == false) return new B2WorldId(0, 0);
            if (B2_CHECK_INPUT(b2IsValidFloat(def.maximumLinearSpeed) && def.maximumLinearSpeed > 0.0f) == false) return new B2WorldId(0, 0);

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

            B2World world = b2_worlds[worldId];
            ushort generation = world.generation;

            //*world = ( b2World ){ 0 };
            world.Clear();

            world.worldId = (ushort)worldId;
            world.generation = generation;
            world.inUse = true;

            world.stack = b2CreateStackAllocator(2048);
            b2CreateBroadPhase(ref world.broadPhase, def.capacity);
            b2CreateGraph(ref world.constraintGraph, def.capacity);

            // pools
            world.bodyIdPool = b2CreateIdPool();
            int bodyCapacity = b2MaxInt(16, def.capacity.staticBodyCount + def.capacity.dynamicBodyCount);
            world.bodies = b2Array_Create<B2Body>(bodyCapacity);
            world.solverSets = b2Array_Create<B2SolverSet>(8);

            // add empty static, active, and disabled body sets
            world.solverSetIdPool = b2CreateIdPool();
            B2SolverSet set = null;

            // static set
            set = b2CreateSolverSet(world);
            set.setIndex = b2AllocId(world.solverSetIdPool);
            b2Array_Push(ref world.solverSets, set);
            b2Array_Reserve(ref world.solverSets.data[(int)B2SolverSetType.b2_staticSet].bodySims, b2MaxInt(16, def.capacity.staticBodyCount));
            B2_ASSERT(world.solverSets.data[(int)B2SolverSetType.b2_staticSet].setIndex == (int)B2SolverSetType.b2_staticSet);

            // disabled set
            set = b2CreateSolverSet(world);
            set.setIndex = b2AllocId(world.solverSetIdPool);
            b2Array_Push(ref world.solverSets, set);
            B2_ASSERT(world.solverSets.data[(int)B2SolverSetType.b2_disabledSet].setIndex == (int)B2SolverSetType.b2_disabledSet);

            // awake set
            set = b2CreateSolverSet(world);
            set.setIndex = b2AllocId(world.solverSetIdPool);
            b2Array_Push(ref world.solverSets, set);
            b2Array_Reserve(ref world.solverSets.data[(int)B2SolverSetType.b2_awakeSet].bodySims, b2MaxInt(16, def.capacity.dynamicBodyCount));
            b2Array_Reserve(ref world.solverSets.data[(int)B2SolverSetType.b2_awakeSet].bodyStates, b2MaxInt(16, def.capacity.dynamicBodyCount));
            b2Array_Reserve(ref world.solverSets.data[(int)B2SolverSetType.b2_awakeSet].contactSims, b2MaxInt(16, def.capacity.contactCount));
            B2_ASSERT(world.solverSets.data[(int)B2SolverSetType.b2_awakeSet].setIndex == (int)B2SolverSetType.b2_awakeSet);

            world.shapeIdPool = b2CreateIdPool();
            int shapeCapacity = b2MaxInt(16, def.capacity.staticShapeCount + def.capacity.dynamicShapeCount);
            world.shapes = b2Array_Create<B2Shape>(shapeCapacity);
            world.fatAABBs = b2Array_Create<B2AABB>(shapeCapacity);

            world.chainIdPool = b2CreateIdPool();
            world.chainShapes = b2Array_Create<B2ChainShape>(4);

            world.contactIdPool = b2CreateIdPool();
            world.contacts = b2Array_Create<B2Contact>(b2MaxInt(16, def.capacity.contactCount));

            world.jointIdPool = b2CreateIdPool();
            world.joints = b2Array_Create<B2Joint>(16);

            world.islandIdPool = b2CreateIdPool();
            world.islands = b2Array_Create<B2Island>(b2MaxInt(16, def.capacity.dynamicBodyCount));

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
            world.hitEventThreshold = b2ClampFloat(def.hitEventThreshold, 0.0f, float.MaxValue);
            world.restitutionThreshold = b2ClampFloat(def.restitutionThreshold, 0.0f, float.MaxValue);
            // Clamp this to avoid overflowing the solver sync flags.
            world.restitutionIterations = b2ClampInt(def.restitutionIterations, 0, B2_MAX_RESTITUTION_ITERATIONS);
            world.enableRestitutionPropagation = def.enableRestitutionPropagation;
            world.maxLinearSpeed = def.maximumLinearSpeed;
            world.contactSpeed = b2ClampFloat(def.contactSpeed, 0.0f, float.MaxValue);
            world.contactHertz = b2ClampFloat(def.contactHertz, 0.0f, float.MaxValue);
            world.contactDampingRatio = b2ClampFloat(def.contactDampingRatio, 0.0f, float.MaxValue);
            world.contactRecycleDistance = B2_CONTACT_RECYCLE_DISTANCE;


            if (def.frictionCallback != null)
            {
                world.frictionCallback = def.frictionCallback;
            }

            if (def.restitutionCallback != null)
            {
                world.restitutionCallback = def.restitutionCallback;
            }

            // @ikpil, new profile
            world.profile = new B2Profile();

            world.enableSleep = def.enableSleep;
            world.locked = false;
            world.enableWarmStarting = true;
            world.enableContinuous = def.enableContinuous;
            world.userTreeTask = null;
            world.userData = def.userData;

            if (def.workerCount > 0 && def.enqueueTask != null && def.finishTask != null)
            {
                // External task system
                world.workerCount = b2MinInt(def.workerCount, B2_MAX_WORKERS);
                world.enqueueTaskFcn = def.enqueueTask;
                world.finishTaskFcn = def.finishTask;
                world.userTaskContext = def.userTaskContext;
                world.scheduler = null;
            }
            else if (def.workerCount > 1)
            {
                // Built-in scheduler
                world.workerCount = b2MinInt(def.workerCount, B2_MAX_WORKERS);
                world.scheduler = b2CreateScheduler(world.workerCount);
                world.enqueueTaskFcn = b2SchedulerEnqueueTask;
                world.finishTaskFcn = b2SchedulerFinishTask;
                world.userTaskContext = world.scheduler;
            }
            else
            {
                // Serial fallback
                world.workerCount = 1;
                world.enqueueTaskFcn = b2DefaultAddTaskFcn;
                world.finishTaskFcn = b2DefaultFinishTaskFcn;
                world.userTaskContext = null;
                world.scheduler = null;
            }

            b2CreateWorkerContexts(world);

            world.debugBodySet = b2CreateBitSet(256);
            world.debugJointSet = b2CreateBitSet(256);
            world.debugContactSet = b2CreateBitSet(256);
            world.debugIslandSet = b2CreateBitSet(256);

            // add one to worldId so that 0 represents a null b2WorldId
            return new B2WorldId((ushort)(worldId + 1), world.generation);
        }
        /// Destroy a world
        public static void b2DestroyWorld(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);

            if (world.scheduler != null)
            {
                b2DestroyScheduler(world.scheduler);
                world.scheduler = null;
            }

            b2DestroyBitSet(ref world.debugBodySet);
            b2DestroyBitSet(ref world.debugJointSet);
            b2DestroyBitSet(ref world.debugContactSet);
            b2DestroyBitSet(ref world.debugIslandSet);

            b2DestroyWorkerContexts(world);

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
            b2Array_Destroy(ref world.fatAABBs);
            b2Array_Destroy(ref world.chainShapes);
            b2Array_Destroy(ref world.contacts);
            b2Array_Destroy(ref world.joints);

            for (int i = 0; i < world.islands.count; ++i)
            {
                b2Array_Destroy(ref world.islands.data[i].bodies);
                b2Array_Destroy(ref world.islands.data[i].contacts);
                b2Array_Destroy(ref world.islands.data[i].joints);
            }

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

            b2DestroyStackAllocator(world.stack);

            // Wipe world but preserve generation
            ushort generation = world.generation;
            world.Clear();
            world.worldId = 0;
            world.generation = (ushort)(generation + 1);
        }

        private static float b2RelativeCos(B2Rot a, B2Rot b)
        {
            return a.c * b.c + a.s * b.s;
        }

        private static B2BodySim b2ResolveContactBodySim(B2World world, B2BodySim[] awakeSims, B2BodySim[] staticSims, int encodedIndex,
            int bodyId)
        {
            if (encodedIndex >= 0)
            {
                return awakeSims[encodedIndex];
            }

            if (b2IsStaticSimIndex(encodedIndex))
            {
                return staticSims[-encodedIndex - 2];
            }

            return b2GetBodySim(world, b2Array_Get(ref world.bodies, bodyId));
        }

        internal static void b2CollideTask(int startIndex, int endIndex, int workerIndex, object context)
        {
            b2TracyCZoneNC(B2TracyCZone.collide_task, "Collide", B2HexColor.b2_colorDodgerBlue, true);

            B2StepContext stepContext = context as B2StepContext;
            B2World world = stepContext.world;
            B2TaskContext taskContext = world.taskContexts.data[workerIndex];
            B2ContactCollideSpan[] spans = stepContext.collideSpans;
            B2Shape[] shapes = world.shapes.data;
            B2AABB[] fatAABBs = world.fatAABBs.data;
            B2SolverSet[] solverSets = world.solverSets.data;
            B2BodySim[] awakeSims = solverSets[(int)B2SolverSetType.b2_awakeSet].bodySims.data;
            B2BodySim[] staticSims = solverSets[(int)B2SolverSetType.b2_staticSet].bodySims.data;

            B2_ASSERT(startIndex < endIndex);

            int spanIndex = 0;
            while (spans[spanIndex + 1].start <= startIndex)
            {
                spanIndex += 1;
            }

            int spanStart = spans[spanIndex].start;
            int spanEnd = spans[spanIndex + 1].start;
            B2ContactSim[] spanBase = spans[spanIndex].contacts;

            float recycleDistance = world.contactRecycleDistance;
            float speculativeDistance = B2_SPECULATIVE_DISTANCE;
            float recycleDistanceNonTouching = b2MinFloat(recycleDistance, speculativeDistance);

            for (int contactIndex = startIndex; contactIndex < endIndex; ++contactIndex)
            {
                if (contactIndex == spanEnd)
                {
                    spanIndex += 1;
                    spanStart = spans[spanIndex].start;
                    spanEnd = spans[spanIndex + 1].start;
                    spanBase = spans[spanIndex].contacts;
                }

                B2ContactSim contactSim = spanBase[contactIndex - spanStart];

                int contactId = contactSim.contactId;
                int shapeIdA = contactSim.shapeIdA;
                int shapeIdB = contactSim.shapeIdB;

                // Do proxies still overlap?
                bool overlap = b2AABB_Overlaps(fatAABBs[shapeIdA], fatAABBs[shapeIdB]);
                if (overlap == false)
                {
                    contactSim.simFlags |= (uint)B2ContactFlags.b2_simDisjoint;
                    contactSim.simFlags &= ~(uint)B2ContactFlags.b2_simTouchingFlag;
                    b2SetBit(ref taskContext.contactStateBitSet, contactId);
                }
                else
                {
                    B2Shape shapeA = shapes[shapeIdA];
                    B2Shape shapeB = shapes[shapeIdB];

                    bool wasTouching = 0 != (contactSim.simFlags & (uint)B2ContactFlags.b2_simTouchingFlag);

                    // Update contact respecting shape/body order (A,B)
                    int encodedA = contactSim.encodedBodySimA;
                    int encodedB = contactSim.encodedBodySimB;
                    B2BodySim bodySimA = b2ResolveContactBodySim(world, awakeSims, staticSims, encodedA, shapeA.bodyId);
                    B2BodySim bodySimB = b2ResolveContactBodySim(world, awakeSims, staticSims, encodedB, shapeB.bodyId);
                    B2WorldTransform transformA = bodySimA.transform;
                    B2WorldTransform transformB = bodySimB.transform;

                    // These may not be skipped by relative transform check below
                    contactSim.invMassA = bodySimA.invMass;
                    contactSim.invIA = bodySimA.invInertia;
                    contactSim.invMassB = bodySimB.invMass;
                    contactSim.invIB = bodySimB.invInertia;

                    bool isFast = ((bodySimA.flags | bodySimB.flags) & (uint)B2BodyFlags.b2_isFast) != 0;

                    // Contact recycling optimization. Please cite this code if you use this optimization.
                    // This is inspired by persistent contact manifolds used in some physics engines, such as PhysX.
                    // However, this allows larger relative motion and has fewer tuning parameters (just one).
                    if (isFast == false && recycleDistance > 0.0f && 0 != (contactSim.simFlags & (uint)B2ContactFlags.b2_simRelativeTransformValid) &&
                        0 != (contactSim.simFlags & (uint)B2ContactFlags.b2_contactRecycleFlag))
                    {
                        B2Rot cachedQA = contactSim.cachedRotationA;
                        B2Rot cachedQB = contactSim.cachedRotationB;
                        B2Transform xfc = contactSim.cachedRelativePose;
                        B2Transform xf = b2InvMulWorldTransforms(transformA, transformB);

                        float cosA = b2RelativeCos(transformA.q, cachedQA);
                        float cosB = b2RelativeCos(transformB.q, cachedQB);
                        float minCos = b2MinFloat(cosA, cosB);

                        float maxExtentA = b2IsStaticSimIndex(encodedA) ? 0.0f : bodySimA.maxExtent;
                        float maxExtentB = b2IsStaticSimIndex(encodedB) ? 0.0f : bodySimB.maxExtent;
                        float maxExtent = b2MaxFloat(maxExtentA, maxExtentB);
                        float distance = b2Distance(xf.p, xfc.p);
                        B2Rot qr = b2InvMulRot(xf.q, xfc.q);

                        // This metric is used for fast bodies and sleeping. It comes from conservative advancement.
                        // Note that qr.s == sin(theta) ~= theta for small angles.
                        // Need a tighter tolerance for non-touching shapes so that contacts are not missed.
                        float tolerance = wasTouching ? recycleDistance : recycleDistanceNonTouching;

                        if (minCos > B2_CONTACT_RECYCLE_COS_ANGLE && distance + maxExtent * b2AbsFloat(qr.s) < tolerance)
                        {
                            B2Rot dqA = b2MulRot(transformA.q, b2InvertRot(cachedQA));
                            B2Rot dqB = b2MulRot(transformB.q, b2InvertRot(cachedQB));
                            B2Vec2 normal = contactSim.manifold.normal;

                            // Minimize round-off
                            B2Vec2 dc = b2SubPos(bodySimB.center, bodySimA.center);

                            for (int i = 0; i < contactSim.manifold.pointCount; ++i)
                            {
                                // Keep anchors but update separation, same as sub-stepping. This eliminates jitter.
                                ref B2ManifoldPoint mp = ref contactSim.manifold.points[i];
                                B2Vec2 rA = b2RotateVector(dqA, mp.anchorA);
                                B2Vec2 rB = b2RotateVector(dqB, mp.anchorB);
                                B2Vec2 dp = b2Add(dc, b2Sub(rB, rA));
                                mp.separation = mp.baseSeparation + b2Dot(dp, normal);

                                mp.normalVelocity = 0.0f;
                                mp.persisted = true;
                            }

                            taskContext.recycledContactCount += 1;

                            // Contact is recycled. This also skips updating other aspects of the contact
                            // such as material parameters.
                            continue;
                        }
                    }

                    // Caching for contact recycling.
                    contactSim.cachedRotationA = transformA.q;
                    contactSim.cachedRotationB = transformB.q;
                    contactSim.cachedRelativePose = b2InvMulWorldTransforms(transformA, transformB);
                    contactSim.simFlags |= (uint)B2ContactFlags.b2_simRelativeTransformValid;

                    B2Vec2 centerOffsetA = b2RotateVector(transformA.q, bodySimA.localCenter);
                    B2Vec2 centerOffsetB = b2RotateVector(transformB.q, bodySimB.localCenter);

                    // This updates solid contacts
                    bool touching =
                        b2UpdateContact(world, contactSim, shapeA, transformA, centerOffsetA, shapeB, transformB, centerOffsetB);

                    // State changes that affect island connectivity. Also affects contact events.
                    if (touching == true && wasTouching == false)
                    {
                        contactSim.simFlags |= (uint)B2ContactFlags.b2_simStartedTouching;
                        b2SetBit(ref taskContext.contactStateBitSet, contactId);
                    }
                    else if (touching == false && wasTouching == true)
                    {
                        contactSim.simFlags |= (uint)B2ContactFlags.b2_simStoppedTouching;
                        b2SetBit(ref taskContext.contactStateBitSet, contactId);
                    }

                    if (touching)
                    {
                        for (int i = 0; i < contactSim.manifold.pointCount; ++i)
                        {
                            // Cache separation
                            ref B2ManifoldPoint mp = ref contactSim.manifold.points[i];
                            mp.baseSeparation = mp.separation;
                        }
                    }

                    // To make this work, the time of impact code needs to adjust the target
                    // distance based on the number of TOI events for a body.
                    // if (touching && bodySimB.isFast)
                    //{
                    //	b2Manifold* manifold = &contactSim.manifold;
                    //	int pointCount = manifold.pointCount;
                    //	for (int i = 0; i < pointCount; ++i)
                    //	{
                    //		// trick the solver into pushing the fast shapes apart
                    //		manifold.points[i].separation -= 0.25f * B2_SPECULATIVE_DISTANCE;
                    //	}
                    //}
                }
            }

            b2TracyCZoneEnd(B2TracyCZone.collide_task);
        }

        internal static void b2AddNonTouchingContact(B2World world, B2Contact contact, B2ContactSim contactSim)
        {
            B2_ASSERT(contact.setIndex == (int)B2SolverSetType.b2_awakeSet);
            B2SolverSet set = b2Array_Get(ref world.solverSets, (int)B2SolverSetType.b2_awakeSet);
            contact.colorIndex = B2_NULL_INDEX;
            contact.localIndex = set.contactSims.count;

            ref B2ContactSim newContactSim = ref b2Array_Emplace(ref set.contactSims);
            //memcpy( newContactSim, contactSim, sizeof( b2ContactSim ) );
            newContactSim.CopyFrom(contactSim);
        }

        internal static void b2RemoveNonTouchingContact(B2World world, int setIndex, int localIndex)
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
        internal static void b2Collide(B2StepContext context)
        {
            B2World world = context.world;

            B2_ASSERT(world.workerCount > 0);

            b2TracyCZoneNC(B2TracyCZone.collide, "Narrow Phase", B2HexColor.b2_colorDodgerBlue, true);

            // gather contacts into a single array for easier parallel-for
            int contactCount = 0;
            B2GraphColor[] graphColors = world.constraintGraph.colors;
            for (int i = 0; i < B2_GRAPH_COLOR_COUNT; ++i)
            {
                contactCount += graphColors[i].contactSims.count;
            }

            int nonTouchingCount = world.solverSets.data[(int)B2SolverSetType.b2_awakeSet].contactSims.count;
            contactCount += nonTouchingCount;

            if (contactCount == 0)
            {
                b2TracyCZoneEnd(B2TracyCZone.collide);
                return;
            }

            B2ContactCollideSpan[] collideSpans = new B2ContactCollideSpan[B2_GRAPH_COLOR_COUNT + 2];
            int spanCount = 0;
            int contactIndex = 0;
            for (int i = 0; i < B2_GRAPH_COLOR_COUNT; ++i)
            {
                ref B2GraphColor color = ref graphColors[i];
                int count = color.contactSims.count;
                if (count > 0)
                {
                    collideSpans[spanCount].start = contactIndex;
                    collideSpans[spanCount].contacts = color.contactSims.data;
                    spanCount += 1;
                    contactIndex += count;
                }
            }

            if (nonTouchingCount > 0)
            {
                collideSpans[spanCount].start = contactIndex;
                collideSpans[spanCount].contacts = world.solverSets.data[(int)B2SolverSetType.b2_awakeSet].contactSims.data;
                spanCount += 1;
                contactIndex += nonTouchingCount;
            }

            B2_ASSERT(contactIndex == contactCount);
            B2_ASSERT(spanCount <= B2_GRAPH_COLOR_COUNT + 1);

            collideSpans[spanCount].start = contactCount;
            collideSpans[spanCount].contacts = null;

            context.collideSpans = collideSpans;

            // Contact bit set on ids because contact pointers are unstable as they move between touching and not touching.
            int contactIdCapacity = b2GetIdCapacity(world.contactIdPool);
            for (int i = 0; i < world.workerCount; ++i)
            {
                b2SetBitCountAndClear(ref world.taskContexts.data[i].contactStateBitSet, contactIdCapacity);
                world.taskContexts.data[i].recycledContactCount = 0;
            }

            // Task should take at least 40us on a 4GHz CPU (10K cycles)
            int minRange = 64;
            b2ParallelFor(world, b2CollideTask, contactCount, minRange, context);

            context.collideSpans = null;

            // Serially update contact state
            b2TracyCZoneNC(B2TracyCZone.contact_state, "Contact State", B2HexColor.b2_colorLightSlateGray, true);

            // Bitwise OR all contact bits
            ref B2BitSet bitSet = ref world.taskContexts.data[0].contactStateBitSet;
            for (int i = 1; i < world.workerCount; ++i)
            {
                b2InPlaceUnion(ref bitSet, ref world.taskContexts.data[i].contactStateBitSet);
            }

            B2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)B2SolverSetType.b2_awakeSet);

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
                    B2_ASSERT(contact.setIndex == (int)B2SolverSetType.b2_awakeSet);

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

                    uint flags = contact.flags;
                    uint simFlags = contactSim.simFlags;

                    if (0 != (simFlags & (uint)B2ContactFlags.b2_simDisjoint))
                    {
                        // Bounding boxes no longer overlap
                        b2DestroyContact(world, contact);
                        contact = null;
                        contactSim = null;
                    }
                    else if (0 != (simFlags & (uint)B2ContactFlags.b2_simStartedTouching))
                    {
                        B2_ASSERT(contact.islandId == B2_NULL_INDEX);

                        if (0 != (flags & (uint)B2ContactFlags.b2_contactEnableContactEvents))
                        {
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

                            B2ContactBeginTouchEvent @event = new B2ContactBeginTouchEvent(shapeIdA, shapeIdB, contactFullId);
                            b2Array_Push(ref world.contactBeginEvents, @event);
                        }

                        B2_ASSERT(contactSim.manifold.pointCount > 0);
                        B2_ASSERT(contact.setIndex == (int)B2SolverSetType.b2_awakeSet);

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

                        contactSim.simFlags &= ~(uint)B2ContactFlags.b2_simStartedTouching;

                        // Add first for memcpy
                        b2AddContactToGraph(world, contactSim, contact);

                        // This destroys the contact sim
                        b2RemoveNonTouchingContact(world, (int)B2SolverSetType.b2_awakeSet, localIndex);
                        contactSim = null;
                    }
                    else if (0 != (simFlags & (uint)B2ContactFlags.b2_simStoppedTouching))
                    {
                        contactSim.simFlags &= ~(uint)B2ContactFlags.b2_simStoppedTouching;
                        contact.flags &= ~(uint)B2ContactFlags.b2_contactTouchingFlag;

                        if (0 != (contact.flags & (uint)B2ContactFlags.b2_contactEnableContactEvents))
                        {
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

                            B2ContactEndTouchEvent @event = new B2ContactEndTouchEvent(shapeIdA, shapeIdB, contactFullId);
                            b2Array_Push(ref world.contactEndEvents[endEventArrayIndex], @event);
                        }

                        B2_ASSERT(contactSim.manifold.pointCount == 0);

                        b2UnlinkContact(world, contact);
                        int bodyIdA = contact.edges[0].bodyId;
                        int bodyIdB = contact.edges[1].bodyId;

                        // Add first for memcpy
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
        /// Simulate a world for one time step. This performs collision detection, integration, and constraint solution.
        /// @param worldId The world to simulate
        /// @param timeStep The amount of time to simulate, this should be a fixed number. Usually 1/60.
        /// @param subStepCount The number of sub-steps, increasing the sub-step count can increase accuracy. Usually 4.
        public static void b2World_Step(B2WorldId worldId, float timeStep, int subStepCount)
        {
            if (B2_CHECK_INPUT(b2IsValidFloat(timeStep)) == false) return;
            B2_ASSERT(0 < subStepCount);

            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                //b2TracyCFrame
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

            b2TracyCZoneNC(B2TracyCZone.world_step, "Step", B2HexColor.b2_colorBox2DGreen, true);

            world.locked = true;
            world.activeTaskCount = 0;
            world.taskCount = 0;

            if (world.scheduler != null)
            {
                b2ResetScheduler(world.scheduler);
            }

            ulong stepTicks = b2GetTicks();

            {
                ref B2Capacity c = ref world.maxCapacity;
                c.staticShapeCount = b2MaxInt(c.staticShapeCount, world.broadPhase.trees[(int)B2BodyType.b2_staticBody].proxyCount);
                c.dynamicShapeCount = b2MaxInt(c.dynamicShapeCount, world.broadPhase.trees[(int)B2BodyType.b2_dynamicBody].proxyCount);

                int staticBodyCount = world.solverSets.data[(int)B2SolverSetType.b2_staticSet].bodySims.count;
                c.staticBodyCount = b2MaxInt(c.staticBodyCount, staticBodyCount);

                // this includes kinematic bodies
                int totalBodyCount = b2GetIdCount(world.bodyIdPool);
                c.dynamicBodyCount = b2MaxInt(c.dynamicBodyCount, totalBodyCount - staticBodyCount);

                int totalContactCount = b2GetIdCount(world.contactIdPool);
                c.contactCount = b2MaxInt(c.contactCount, totalContactCount);
            }

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
            world.inv_dt = context.inv_dt;

            // Hertz values get reduced for large time steps
            float contactHertz = b2MinFloat(world.contactHertz, 0.125f * context.inv_h);
            context.contactSoftness = b2MakeSoft(contactHertz, world.contactDampingRatio, context.h);
            context.staticSoftness = b2MakeSoft(2.0f * contactHertz, world.contactDampingRatio, context.h);

            context.restitutionThreshold = world.restitutionThreshold;
            context.maxLinearVelocity = world.maxLinearSpeed;
            context.enableWarmStarting = world.enableWarmStarting;

            // Narrow phase : update contacts
            {
                ulong collideTicks = b2GetTicks();
                b2Collide(context);
                world.profile.collide = b2GetMilliseconds(collideTicks);
            }

            // Integrate velocities, solve velocity constraints, and integrate positions.
            if (timeStep > 0.0f)
            {
                ulong solveTicks = b2GetTicks();
                b2Solve(world, context);
                world.profile.solve = b2GetMilliseconds(solveTicks);
            }

            // Finish the tree task in case b2Solve didn't finish it
            if (world.userTreeTask != null)
            {
                world.finishTaskFcn(world.userTreeTask, world.userTaskContext);
                world.userTreeTask = null;
                world.activeTaskCount -= 1;
                b2ValidateNoEnlarged(world.broadPhase);
            }

            // Update sensors
            {
                ulong sensorTicks = b2GetTicks();
                b2OverlapSensors(world);
                world.profile.sensors = b2GetMilliseconds(sensorTicks);
            }

            world.profile.step = b2GetMilliseconds(stepTicks);

            B2_ASSERT(b2GetStackAllocation(world.stack) == 0);

            // Ensure stack is large enough
            b2GrowStack(world.stack);

            // Make sure all tasks that were started were also finished
            B2_ASSERT(world.activeTaskCount == 0);

            b2TracyCZoneEnd(B2TracyCZone.world_step);

            // Swap end event array buffers
            world.endEventArrayIndex = 1 - world.endEventArrayIndex;
            b2Array_Clear(ref world.sensorEndEvents[world.endEventArrayIndex]);
            b2Array_Clear(ref world.contactEndEvents[world.endEventArrayIndex]);
            world.locked = false;
            //b2TracyCFrame
        }

        internal static void b2DrawShape(B2DebugDraw draw, B2Shape shape, in B2WorldTransform transform, B2HexColor color, bool drawChainNormals)
        {
            switch (shape.type)
            {
                case B2ShapeType.b2_capsuleShape:
                {
                    ref readonly B2Capsule capsule = ref shape.us.capsule;
                    B2Pos p1 = b2TransformWorldPoint(transform, capsule.center1);
                    B2Pos p2 = b2TransformWorldPoint(transform, capsule.center2);
                    draw.DrawSolidCapsuleFcn(p1, p2, capsule.radius, color, draw.context);
                }
                    break;

                case B2ShapeType.b2_circleShape:
                {
                    ref readonly B2Circle circle = ref shape.us.circle;
                    draw.DrawSolidCircleFcn(transform, circle.center, circle.radius, color, draw.context);
                }
                    break;

                case B2ShapeType.b2_polygonShape:
                {
                    ref B2Polygon poly = ref shape.us.polygon;
                    draw.DrawSolidPolygonFcn(transform, poly.vertices.AsSpan(), poly.count, poly.radius, color, draw.context);
                }
                    break;

                case B2ShapeType.b2_segmentShape:
                {
                    ref readonly B2Segment segment = ref shape.us.segment;
                    B2Pos p1 = b2TransformWorldPoint(transform, segment.point1);
                    B2Pos p2 = b2TransformWorldPoint(transform, segment.point2);
                    draw.DrawLineFcn(p1, p2, color, draw.context);
                }
                    break;

                case B2ShapeType.b2_chainSegmentShape:
                {
                    ref readonly B2Segment segment = ref shape.us.chainSegment.segment;
                    B2Pos p1 = b2TransformWorldPoint(transform, segment.point1);
                    B2Pos p2 = b2TransformWorldPoint(transform, segment.point2);
                    draw.DrawLineFcn(p1, p2, color, draw.context);
                    draw.DrawPointFcn(p2, 4.0f, color, draw.context);

                    if (drawChainNormals)
                    {
                        B2Pos c = b2LerpPosition(p1, p2, 0.5f);
                        B2Vec2 e = b2Normalize(b2SubPos(p2, p1));
                        B2Vec2 n = b2RightPerp(e);
                        float L = 0.2f * b2GetLengthUnitsPerMeter();
                        draw.DrawLineFcn(c, b2OffsetPos(c, b2MulSV(L, n)), B2HexColor.b2_colorPaleGreen, draw.context);
                    }
                }
                    break;

                default:
                    break;
            }
        }

        internal static bool DrawQueryCallback(int proxyId, ulong userData, ref B2DrawContext context)
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

                if (shape.material.customColor != 0)
                {
                    color = (B2HexColor)shape.material.customColor;
                }
                else if (body.type == B2BodyType.b2_dynamicBody && body.mass == 0.0f)
                {
                    // Bad body
                    color = B2HexColor.b2_colorRed;
                }
                else if (body.setIndex == (int)B2SolverSetType.b2_disabledSet)
                {
                    color = B2HexColor.b2_colorSlateGray;
                }
                else if (shape.sensorIndex != B2_NULL_INDEX)
                {
                    color = B2HexColor.b2_colorWheat;
                }
                else if (0 != (body.flags & (uint)B2BodyFlags.b2_hadTimeOfImpact))
                {
                    color = B2HexColor.b2_colorLime;
                }
                else if (0 != (bodySim.flags & (uint)B2BodyFlags.b2_isBullet) && body.setIndex == (int)B2SolverSetType.b2_awakeSet)
                {
                    color = B2HexColor.b2_colorTurquoise;
                }
                else if (0 != (body.flags & (uint)B2BodyFlags.b2_isSpeedCapped))
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
                else if (body.setIndex == (int)B2SolverSetType.b2_awakeSet)
                {
                    color = B2HexColor.b2_colorPink;
                }
                else
                {
                    color = B2HexColor.b2_colorGray;
                }

                b2DrawShape(draw, shape, bodySim.transform, color, draw.drawChainNormals);
            }

            if (draw.drawBounds)
            {
                draw.DrawBoundsFcn(world.fatAABBs.data[shapeId], B2HexColor.b2_colorGold, draw.context);
            }

            return true;
        }

        // todo this has varying order for moving shapes, causing flicker when overlapping shapes are moving
        // solution: display order by shape id modulus 3, keep 3 buckets in GLSolid* and flush in 3 passes.
        /// Call this to draw shapes and other debug draw data
        public static void b2World_Draw(B2WorldId worldId, in B2DebugDraw draw)
        {
            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return;
            }


            B2_ASSERT(b2IsValidAABB(draw.drawingBounds));

            const float k_axisScale = 0.3f;
            B2HexColor speculativeColor = B2HexColor.b2_colorGainsboro;
            B2HexColor addColor = B2HexColor.b2_colorGreen;
            B2HexColor persistColor = B2HexColor.b2_colorBlue;
            B2HexColor normalColor = B2HexColor.b2_colorDimGray;
            B2HexColor impulseColor = B2HexColor.b2_colorMagenta;
            B2HexColor frictionColor = B2HexColor.b2_colorYellow;

            int bodyCapacity = b2GetIdCapacity(world.bodyIdPool);
            b2SetBitCountAndClear(ref world.debugBodySet, bodyCapacity);

            int jointCapacity = b2GetIdCapacity(world.jointIdPool);
            b2SetBitCountAndClear(ref world.debugJointSet, jointCapacity);

            int contactCapacity = b2GetIdCapacity(world.contactIdPool);
            b2SetBitCountAndClear(ref world.debugContactSet, contactCapacity);

            int islandCapacity = b2GetIdCapacity(world.islandIdPool);
            b2SetBitCountAndClear(ref world.debugIslandSet, islandCapacity);

            B2DrawContext drawContext = new B2DrawContext();
            drawContext.world = world;
            drawContext.draw = draw;

            for (int i = 0; i < (int)B2BodyType.b2_bodyTypeCount; ++i)
            {
                b2DynamicTree_QueryAll(world.broadPhase.trees[i], draw.drawingBounds, DrawQueryCallback, ref drawContext);
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

                        B2WorldTransform transform = new B2WorldTransform(bodySim.center, bodySim.transform.q);
                        B2Pos p = b2TransformWorldPoint(transform, offset);
                        draw.DrawStringFcn(p, body.name, B2HexColor.b2_colorWhiteSmoke, draw.context);
                    }

                    if (draw.drawMass && body.type == B2BodyType.b2_dynamicBody)
                    {
                        B2Vec2 offset = new B2Vec2(0.1f, 0.1f);
                        B2BodySim bodySim = b2GetBodySim(world, body);

                        B2WorldTransform transform = new B2WorldTransform(bodySim.center, bodySim.transform.q);
                        draw.DrawLineFcn(bodySim.center0, bodySim.center, B2HexColor.b2_colorWhiteSmoke, draw.context);
                        draw.DrawTransformFcn(transform, draw.context);

                        B2Pos p = b2TransformWorldPoint(transform, offset);

                        string buffer = $"{body.mass:F2}";
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

                            jointKey = joint.edges[edgeIndex].nextKey;
                        }
                    }

                    float linearSlop = B2_LINEAR_SLOP;
                    if (draw.drawContacts && body.type == B2BodyType.b2_dynamicBody)
                    {
                        int contactKey = body.headContactKey;
                        while (contactKey != B2_NULL_INDEX)
                        {
                            int contactId = contactKey >> 1;
                            int edgeIndex = contactKey & 1;
                            B2Contact contact = b2Array_Get(ref world.contacts, contactId);
                            contactKey = contact.edges[edgeIndex].nextKey;

                            // avoid double draw
                            if (b2GetBit(ref world.debugContactSet, contactId) == false)
                            {
                                B2ContactSim contactSim = b2GetContactSim(world, contact);
                                B2Body bodyA = b2Array_Get(ref world.bodies, contact.edges[0].bodyId);
                                B2BodySim bodySimA = b2GetBodySim(world, bodyA);
                                B2Body bodyB = b2Array_Get(ref world.bodies, contact.edges[1].bodyId);
                                B2BodySim bodySimB = b2GetBodySim(world, bodyB);

                                int pointCount = contactSim.manifold.pointCount;
                                B2Vec2 normal = contactSim.manifold.normal;
                                string buffer;

                                for (int j = 0; j < pointCount; ++j)
                                {
                                    ref B2ManifoldPoint mp = ref contactSim.manifold.points[j];

                                    B2Pos p;
                                    if (draw.drawAnchorA)
                                    {
                                        p = b2OffsetPos(bodySimA.center, mp.anchorA);
                                    }
                                    else
                                    {
                                        p = b2OffsetPos(bodySimB.center, mp.anchorB);
                                    }

                                    if (draw.drawGraphColors && contact.colorIndex != B2_NULL_INDEX)
                                    {
                                        // graph color
                                        float pointSize = contact.colorIndex == B2_OVERFLOW_INDEX ? 7.5f : 5.0f;
                                        draw.DrawPointFcn(p, pointSize, b2GetGraphColor(contact.colorIndex), draw.context);
                                        // B2.g_draw.DrawString(point.position, "%d", point.color);
                                    }
                                    else if (mp.separation > linearSlop)
                                    {
                                        // Speculative
                                        draw.DrawPointFcn(p, 5.0f, speculativeColor, draw.context);
                                    }
                                    else if (mp.persisted == false)
                                    {
                                        // Add
                                        draw.DrawPointFcn(p, 10.0f, addColor, draw.context);
                                    }
                                    else if (mp.persisted == true)
                                    {
                                        // Persist
                                        draw.DrawPointFcn(p, 5.0f, persistColor, draw.context);
                                    }

                                    if (draw.drawContactNormals)
                                    {
                                        B2Pos p1 = p;
                                        B2Pos p2 = b2OffsetPos(p1, b2MulSV(k_axisScale, normal));
                                        draw.DrawLineFcn(p1, p2, normalColor, draw.context);

                                        buffer = $" {mp.separation:F2}";
                                        draw.DrawStringFcn(p1, buffer, B2HexColor.b2_colorWhite, draw.context);
                                    }
                                    else if (draw.drawContactForces)
                                    {
                                        // todo validate
                                        // multiply by one-half due to relax iteration
                                        float force = 0.5f * mp.totalNormalImpulse * world.inv_dt;
                                        B2Pos p1 = p;
                                        B2Pos p2 = b2OffsetPos(p1, b2MulSV(draw.forceScale * force, normal));
                                        draw.DrawLineFcn(p1, p2, impulseColor, draw.context);
                                        buffer = $"{force:F1}";
                                        draw.DrawStringFcn(p1, buffer, B2HexColor.b2_colorWhite, draw.context);
                                    }

                                    if (draw.drawContactFeatures)
                                    {
                                        buffer = "" + mp.id;
                                        draw.DrawStringFcn(p, buffer, B2HexColor.b2_colorOrange, draw.context);
                                    }

                                    if (draw.drawFrictionForces)
                                    {
                                        float force = 0.5f * mp.tangentImpulse * world.inv_h;
                                        B2Vec2 tangent = b2RightPerp(normal);
                                        B2Pos p1 = p;
                                        B2Pos p2 = b2OffsetPos(p1, b2MulSV(draw.forceScale * force, tangent));
                                        draw.DrawLineFcn(p1, p2, frictionColor, draw.context);
                                        buffer = $"{force:F1}";
                                        draw.DrawStringFcn(p1, buffer, B2HexColor.b2_colorWhite, draw.context);
                                    }
                                }

                                b2SetBit(ref world.debugContactSet, contactId);
                            }

                            contactKey = contact.edges[edgeIndex].nextKey;
                        }
                    }

                    if (draw.drawIslands)
                    {
                        int islandId = body.islandId;
                        if (islandId != B2_NULL_INDEX && b2GetBit(ref world.debugIslandSet, islandId) == false)
                        {
                            B2Island island = world.islands.data[islandId];
                            if (island.setIndex == B2_NULL_INDEX)
                            {
                                continue;
                            }

                            int shapeCount = 0;
                            B2AABB aabb = new B2AABB(
                                lowerBound: new B2Vec2(float.MaxValue, float.MaxValue),
                                upperBound: new B2Vec2(-float.MaxValue, -float.MaxValue)
                            );

                            for (int bodyIndex = 0; bodyIndex < island.bodies.count; ++bodyIndex)
                            {
                                int islandBodyId = island.bodies.data[bodyIndex];
                                B2Body islandBody = b2Array_Get(ref world.bodies, islandBodyId);
                                int shapeId = islandBody.headShapeId;
                                while (shapeId != B2_NULL_INDEX)
                                {
                                    B2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                                    aabb = b2AABB_Union(aabb, world.fatAABBs.data[shapeId]);
                                    shapeCount += 1;
                                    shapeId = shape.nextShapeId;
                                }
                            }

                            if (shapeCount > 0)
                            {
                                draw.DrawBoundsFcn(aabb, B2HexColor.b2_colorOrangeRed, draw.context);
                            }

                            b2SetBit(ref world.debugIslandSet, islandId);
                        }
                    }

                    // Clear the smallest set bit
                    word = word & (word - 1);
                }
            }
        }

        // Union of the broad-phase root bounds across all body types. Returns false when no tree holds a
        // proxy, so callers don't fold an empty world's origin into a running bounds.
        internal static bool b2ComputeWorldBounds(B2World world, out B2AABB bounds)
        {
            B2AABB worldBounds = new B2AABB();
            bool haveBounds = false;

            for (int i = 0; i < (int)B2BodyType.b2_bodyTypeCount; ++i)
            {
                B2DynamicTree tree = world.broadPhase.trees[i];
                if (b2DynamicTree_GetProxyCount(tree) == 0)
                {
                    continue;
                }

                B2AABB treeBounds = b2DynamicTree_GetRootBounds(tree);
                worldBounds = haveBounds ? b2AABB_Union(worldBounds, treeBounds) : treeBounds;
                haveBounds = true;
            }

            bounds = worldBounds;
            return haveBounds;
        }

        /// Call this to get the world bounds based on the union of all shape bounds.
        public static B2AABB b2World_GetBounds(B2WorldId worldId)
        {
            B2World world = b2GetUnlockedWorldFromId(worldId);
            if (world == null)
            {
                return new B2AABB();
            }

            b2ComputeWorldBounds(world, out B2AABB bounds);
            return bounds;
        }

        private const ulong B2_SNAP_FNV_INIT = 14695981039346656037UL;
        private const ulong B2_SNAP_FNV_PRIME = 1099511628211UL;

        private static ulong b2FnvMixFloat(ulong hash, float f)
        {
            uint bits = (uint)BitConverter.SingleToInt32Bits(f);
            return (hash ^ bits) * B2_SNAP_FNV_PRIME;
        }

        private static ulong b2FnvMixInt(ulong hash, int v)
        {
            return (hash ^ (uint)v) * B2_SNAP_FNV_PRIME;
        }

        // Equivalent to hashing the bytes of a b2Vec2 one at a time (little-endian)
        private static ulong b2FnvMixVec2Bytes(ulong hash, B2Vec2 v)
        {
            uint bx = (uint)BitConverter.SingleToInt32Bits(v.X);
            uint by = (uint)BitConverter.SingleToInt32Bits(v.Y);
            for (int i = 0; i < 4; ++i)
            {
                hash = (hash ^ ((bx >> (8 * i)) & 0xFF)) * B2_SNAP_FNV_PRIME;
            }

            for (int i = 0; i < 4; ++i)
            {
                hash = (hash ^ ((by >> (8 * i)) & 0xFF)) * B2_SNAP_FNV_PRIME;
            }

            return hash;
        }

        private static ulong b2FnvMixPosition(ulong hash, B2Pos p)
        {
            ulong bx = (uint)BitConverter.SingleToInt32Bits(p.X);
            ulong by = (uint)BitConverter.SingleToInt32Bits(p.Y);
            hash = (hash ^ bx) * B2_SNAP_FNV_PRIME;
            hash = (hash ^ by) * B2_SNAP_FNV_PRIME;
            return hash;
        }

        internal static ulong b2HashWorldStateDeep(B2World world)
        {
            ulong hash = B2_SNAP_FNV_INIT;

            // Bodies: sparse array, skip free slots
            int bodyCount = world.bodies.count;
            for (int i = 0; i < bodyCount; ++i)
            {
                B2Body body = world.bodies.data[i];
                if (body.id != i)
                {
                    continue;
                }

                B2BodySim sim = b2GetBodySim(world, body);

                hash = b2FnvMixPosition(hash, sim.transform.p);
                hash = b2FnvMixFloat(hash, sim.transform.q.c);
                hash = b2FnvMixFloat(hash, sim.transform.q.s);

                B2BodyState state = b2GetBodyState(world, body);
                if (state != null)
                {
                    hash = b2FnvMixFloat(hash, state.linearVelocity.X);
                    hash = b2FnvMixFloat(hash, state.linearVelocity.Y);
                    hash = b2FnvMixFloat(hash, state.angularVelocity);
                }

                // Index bookkeeping
                hash = b2FnvMixInt(hash, body.setIndex);
                hash = b2FnvMixInt(hash, body.localIndex);
            }

            // Contacts: sparse array, skip free slots
            int contactCount = world.contacts.count;
            for (int i = 0; i < contactCount; ++i)
            {
                B2Contact contact = world.contacts.data[i];
                if (contact.contactId != i)
                {
                    continue;
                }

                hash = b2FnvMixInt(hash, contact.setIndex);
                hash = b2FnvMixInt(hash, contact.colorIndex);
                hash = b2FnvMixInt(hash, contact.localIndex);

                // Manifold points + impulses from the contact sim
                B2ContactSim sim = b2GetContactSim(world, contact);
                if (sim != null)
                {
                    ref B2Manifold m = ref sim.manifold;
                    hash = b2FnvMixInt(hash, m.pointCount);
                    for (int p = 0; p < m.pointCount; ++p)
                    {
                        hash = b2FnvMixFloat(hash, m.points[p].normalImpulse);
                        hash = b2FnvMixFloat(hash, m.points[p].tangentImpulse);
                        hash = b2FnvMixFloat(hash, m.points[p].totalNormalImpulse);
                    }
                }
            }

            // Joints: sparse array, skip free slots
            int jointCount = world.joints.count;
            for (int i = 0; i < jointCount; ++i)
            {
                B2Joint joint = world.joints.data[i];
                if (joint.jointId != i)
                {
                    continue;
                }

                hash = b2FnvMixInt(hash, joint.setIndex);
                hash = b2FnvMixInt(hash, joint.colorIndex);
                hash = b2FnvMixInt(hash, joint.localIndex);

                B2JointSim sim = b2GetJointSim(world, joint);
                if (sim != null)
                {
                    // Hash accumulated impulses per joint type
                    switch (sim.type)
                    {
                        case B2JointType.b2_distanceJoint:
                            hash = b2FnvMixFloat(hash, sim.uj.distanceJoint.impulse);
                            hash = b2FnvMixFloat(hash, sim.uj.distanceJoint.lowerImpulse);
                            hash = b2FnvMixFloat(hash, sim.uj.distanceJoint.upperImpulse);
                            hash = b2FnvMixFloat(hash, sim.uj.distanceJoint.motorImpulse);
                            break;
                        case B2JointType.b2_moverJoint:
                            hash = b2FnvMixFloat(hash, sim.uj.moverJoint.linearVelocityImpulse.X);
                            hash = b2FnvMixFloat(hash, sim.uj.moverJoint.linearVelocityImpulse.Y);
                            break;
                        case B2JointType.b2_motorJoint:
                            hash = b2FnvMixFloat(hash, sim.uj.motorJoint.linearVelocityImpulse.X);
                            hash = b2FnvMixFloat(hash, sim.uj.motorJoint.linearVelocityImpulse.Y);
                            hash = b2FnvMixFloat(hash, sim.uj.motorJoint.angularVelocityImpulse);
                            hash = b2FnvMixFloat(hash, sim.uj.motorJoint.linearSpringImpulse.X);
                            hash = b2FnvMixFloat(hash, sim.uj.motorJoint.linearSpringImpulse.Y);
                            hash = b2FnvMixFloat(hash, sim.uj.motorJoint.angularSpringImpulse);
                            break;
                        case B2JointType.b2_pogoJoint:
                            hash = b2FnvMixFloat(hash, sim.uj.pogoJoint.impulse);
                            hash = b2FnvMixFloat(hash, sim.uj.pogoJoint.velocity);
                            break;
                        case B2JointType.b2_prismaticJoint:
                            hash = b2FnvMixVec2Bytes(hash, sim.uj.prismaticJoint.impulse);
                            hash = b2FnvMixFloat(hash, sim.uj.prismaticJoint.springImpulse);
                            hash = b2FnvMixFloat(hash, sim.uj.prismaticJoint.motorImpulse);
                            hash = b2FnvMixFloat(hash, sim.uj.prismaticJoint.lowerImpulse);
                            hash = b2FnvMixFloat(hash, sim.uj.prismaticJoint.upperImpulse);
                            break;
                        case B2JointType.b2_revoluteJoint:
                            hash = b2FnvMixVec2Bytes(hash, sim.uj.revoluteJoint.linearImpulse);
                            hash = b2FnvMixFloat(hash, sim.uj.revoluteJoint.springImpulse);
                            hash = b2FnvMixFloat(hash, sim.uj.revoluteJoint.motorImpulse);
                            hash = b2FnvMixFloat(hash, sim.uj.revoluteJoint.lowerImpulse);
                            hash = b2FnvMixFloat(hash, sim.uj.revoluteJoint.upperImpulse);
                            break;
                        case B2JointType.b2_weldJoint:
                            hash = b2FnvMixVec2Bytes(hash, sim.uj.weldJoint.linearImpulse);
                            hash = b2FnvMixFloat(hash, sim.uj.weldJoint.angularImpulse);
                            break;
                        case B2JointType.b2_wheelJoint:
                            hash = b2FnvMixFloat(hash, sim.uj.wheelJoint.perpImpulse);
                            hash = b2FnvMixFloat(hash, sim.uj.wheelJoint.motorImpulse);
                            hash = b2FnvMixFloat(hash, sim.uj.wheelJoint.springImpulse);
                            hash = b2FnvMixFloat(hash, sim.uj.wheelJoint.lowerImpulse);
                            hash = b2FnvMixFloat(hash, sim.uj.wheelJoint.upperImpulse);
                            break;
                        default:
                            break;
                    }
                }
            }

            // 7 id pools: nextIndex + count
            hash = b2FnvMixInt(hash, world.bodyIdPool.nextIndex);
            hash = b2FnvMixInt(hash, b2GetIdCount(world.bodyIdPool));
            hash = b2FnvMixInt(hash, world.shapeIdPool.nextIndex);
            hash = b2FnvMixInt(hash, b2GetIdCount(world.shapeIdPool));
            hash = b2FnvMixInt(hash, world.chainIdPool.nextIndex);
            hash = b2FnvMixInt(hash, b2GetIdCount(world.chainIdPool));
            hash = b2FnvMixInt(hash, world.contactIdPool.nextIndex);
            hash = b2FnvMixInt(hash, b2GetIdCount(world.contactIdPool));
            hash = b2FnvMixInt(hash, world.jointIdPool.nextIndex);
            hash = b2FnvMixInt(hash, b2GetIdCount(world.jointIdPool));
            hash = b2FnvMixInt(hash, world.islandIdPool.nextIndex);
            hash = b2FnvMixInt(hash, b2GetIdCount(world.islandIdPool));
            hash = b2FnvMixInt(hash, world.solverSetIdPool.nextIndex);
            hash = b2FnvMixInt(hash, b2GetIdCount(world.solverSetIdPool));

            // Solver set count
            hash = b2FnvMixInt(hash, world.solverSets.count);

            return hash;
        }

        /// Compute a deterministic hash of the simulation state: body transforms and velocities, contact and
        /// joint impulses, and the index bookkeeping that drives the solve. Reproduces exactly across worker
        /// counts and ignores struct padding and free slots, so two worlds that simulate identically always
        /// agree. Use this to detect desyncs (for example rollback resimulation).
        public static ulong b2World_GetStateHash(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            return b2HashWorldStateDeep(world);
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
        /// World id validation. Provides validation for up to 64K allocations.
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

        /// Body identifier validation. A valid body exists in a world and is non-null.
        /// This can be used to detect orphaned ids. Provides validation for up to 64K allocations.
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
        /// Shape identifier validation. Provides validation for up to 64K allocations.
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
        /// Chain identifier validation. Provides validation for up to 64K allocations.
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
        /// Joint identifier validation. Provides validation for up to 64K allocations.
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
        /// Enable/disable sleep. If your application does not need sleeping, you can gain some performance
        /// by disabling sleep completely at the world level.
        /// @see b2WorldDef
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
                for (int i = (int)B2SolverSetType.b2_firstSleepingSet; i < setCount; ++i)
                {
                    B2SolverSet set = b2Array_Get(ref world.solverSets, i);
                    if (set.bodySims.count > 0)
                    {
                        b2WakeSolverSet(world, i);
                    }
                }
            }
        }
        /// Is body sleeping enabled?
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
        /// Is constraint warm starting enabled?
        public static bool b2World_IsWarmStartingEnabled(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            return world.enableWarmStarting;
        }
        /// Get the number of awake bodies.
        public static int b2World_GetAwakeBodyCount(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            B2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)B2SolverSetType.b2_awakeSet);
            return awakeSet.bodySims.count;
        }
        /// Enable/disable continuous collision between dynamic and static bodies. Generally you should keep continuous
        /// collision enabled to prevent fast moving objects from going through static objects. The performance gain from
        /// disabling continuous collision is minor.
        /// @see b2WorldDef
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
        /// Is continuous collision enabled?
        public static bool b2World_IsContinuousEnabled(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            return world.enableContinuous;
        }
        /// Adjust the restitution threshold. It is recommended not to make this value very small
        /// because it will prevent bodies from sleeping. Usually in meters per second.
        /// @see b2WorldDef
        public static void b2World_SetRestitutionThreshold(B2WorldId worldId, float value)
        {
            if (B2_CHECK_INPUT(b2IsValidFloat(value)) == false) return;

            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return;
            }

            world.restitutionThreshold = b2ClampFloat(value, 0.0f, float.MaxValue);
        }
        /// Get the the restitution speed threshold. Usually in meters per second.
        public static float b2World_GetRestitutionThreshold(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            return world.restitutionThreshold;
        }

        /// Set the number of restitution iterations. More iterations can lead to less box spinning.
        /// Clamped to B2_MAX_RESTITUTION_ITERATIONS.
        public static void b2World_SetRestitutionIterations(B2WorldId worldId, int iterations)
        {
            if (B2_CHECK_INPUT(iterations >= 0) == false) return;

            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return;
            }

            world.restitutionIterations = b2ClampInt(iterations, 0, B2_MAX_RESTITUTION_ITERATIONS);
        }

        /// Get the number of restitution iterations.
        public static int b2World_GetRestitutionIterations(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            return world.restitutionIterations;
        }

        /// Enable full contact propagation in the restitution solver. Expensive.
        public static void b2World_EnableRestitutionPropagation(B2WorldId worldId, bool flag)
        {
            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return;
            }

            world.enableRestitutionPropagation = flag;
        }

        /// Is restitution propagation enabled?
        public static bool b2World_IsRestitutionPropagationEnabled(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            return world.enableRestitutionPropagation;
        }
        /// Adjust the hit event threshold. This controls the collision speed needed to generate a b2ContactHitEvent.
        /// Usually in meters per second.
        /// @see b2WorldDef::hitEventThreshold
        public static void b2World_SetHitEventThreshold(B2WorldId worldId, float value)
        {
            if (B2_CHECK_INPUT(b2IsValidFloat(value)) == false) return;

            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return;
            }

            world.hitEventThreshold = b2ClampFloat(value, 0.0f, float.MaxValue);
        }
        /// Get the the hit event speed threshold. Usually in meters per second.
        public static float b2World_GetHitEventThreshold(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            return world.hitEventThreshold;
        }
        /// Adjust contact tuning parameters
        /// @param worldId The world id
        /// @param hertz The contact stiffness (cycles per second)
        /// @param dampingRatio The contact bounciness with 1 being critical damping (non-dimensional)
        /// @param pushSpeed The maximum contact constraint push out speed (meters per second)
        /// @note Advanced feature
        public static void b2World_SetContactTuning(B2WorldId worldId, float hertz, float dampingRatio, float pushSpeed)
        {
            if (B2_CHECK_INPUT(b2IsValidFloat(hertz)) == false) return;
            if (B2_CHECK_INPUT(b2IsValidFloat(dampingRatio)) == false) return;
            if (B2_CHECK_INPUT(b2IsValidFloat(pushSpeed)) == false) return;

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

        /// Set the contact point recycling distance. Setting this to zero disables contact point recycling.
        /// Usually in meters.
        public static void b2World_SetContactRecycleDistance(B2WorldId worldId, float recycleDistance)
        {
            if (B2_CHECK_INPUT(b2IsValidFloat(recycleDistance)) == false) return;

            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return;
            }

            world.contactRecycleDistance = b2ClampFloat(recycleDistance, 0.0f, float.MaxValue);
        }

        /// Get the contact point recycling distance. Usually in meters.
        public static float b2World_GetContactRecycleDistance(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            return world.contactRecycleDistance;
        }
        /// Set the maximum linear speed. Usually in m/s.
        public static void b2World_SetMaximumLinearSpeed(B2WorldId worldId, float maximumLinearSpeed)
        {
            if (B2_CHECK_INPUT(b2IsValidFloat(maximumLinearSpeed) && maximumLinearSpeed > 0.0f) == false) return;

            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return;
            }

            world.maxLinearSpeed = maximumLinearSpeed;
        }
        /// Get the maximum linear speed. Usually in m/s.
        public static float b2World_GetMaximumLinearSpeed(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            return world.maxLinearSpeed;
        }
        /// Get the current world performance profile
        public static B2Profile b2World_GetProfile(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            return world.profile;
        }

        /// Set the worker count. Must be between in the range [1, B2_MAX_WORKERS]
        public static void b2World_SetWorkerCount(B2WorldId worldId, int count)
        {
            B2World world = b2GetUnlockedWorldFromId(worldId);
            if (world == null)
            {
                return;
            }

            if (count == world.workerCount)
            {
                return;
            }

            b2DestroyWorkerContexts(world);
            world.workerCount = b2ClampInt(count, 1, B2_MAX_WORKERS);
            b2CreateWorkerContexts(world);
        }

        /// Get the worker count.
        public static int b2World_GetWorkerCount(B2WorldId worldId)
        {
            B2World world = b2GetUnlockedWorldFromId(worldId);
            if (world == null)
            {
                return 0;
            }

            return world.workerCount;
        }
        /// Get world counters and sizes
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

            s.stackUsed = b2GetMaxStackAllocation(world.stack);
            s.byteCount = b2GetByteCount();
            s.taskCount = world.taskCount;

            s.recycledContactCount = 0;
            for (int i = 0; i < world.workerCount; ++i)
            {
                s.recycledContactCount += world.taskContexts.data[i].recycledContactCount;
            }

            s.awakeContactCount = 0;
            for (int i = 0; i < B2_GRAPH_COLOR_COUNT; ++i)
            {
                ref B2GraphColor color = ref world.constraintGraph.colors[i];
                s.colorCounts[i] = color.contactSims.count + color.jointSims.count;
                s.awakeContactCount += color.contactSims.count;
            }

            s.awakeContactCount += world.solverSets.data[(int)B2SolverSetType.b2_awakeSet].contactSims.count;

            return s;
        }

        /// Get max capacity. This can be used with b2WorldDef to avoid run-time allocations and copies
        public static B2Capacity b2World_GetMaxCapacity(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            return world.maxCapacity;
        }

        /// Set the user data pointer.
        public static void b2World_SetUserData(B2WorldId worldId, B2UserData userData)
        {
            B2World world = b2GetWorldFromId(worldId);
            world.userData = userData;
        }
        /// Get the user data pointer.
        public static B2UserData b2World_GetUserData(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            return world.userData;
        }
        /// Set the friction callback. Passing NULL resets to default.
        public static void b2World_SetFrictionCallback(B2WorldId worldId, b2FrictionCallback callback)
        {
            B2World world = b2GetWorldFromId(worldId);
            if (world.locked)
            {
                return;
            }

            world.frictionCallback = callback;
        }
        /// Set the restitution callback. Passing NULL resets to default.
        public static void b2World_SetRestitutionCallback(B2WorldId worldId, b2RestitutionCallback callback)
        {
            B2World world = b2GetWorldFromId(worldId);
            if (world.locked)
            {
                return;
            }

            world.restitutionCallback = callback;
        }
        /// Dump memory stats to box2d_memory.txt
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
            // todo account for body/contact/joint arrays in island
            writer.Write("islands: {0}\n", b2Array_ByteCount(ref world.islands));
            writer.Write("shapes: {0}\n", b2Array_ByteCount(ref world.shapes));
            writer.Write("chains: {0}\n", b2Array_ByteCount(ref world.chainShapes));
            writer.Write("\n");

            // broad-phase
            writer.Write("broad-phase\n");
            writer.Write("static tree: {0}\n", b2DynamicTree_GetByteCount(world.broadPhase.trees[(int)B2BodyType.b2_staticBody]));
            writer.Write("kinematic tree: {0}\n", b2DynamicTree_GetByteCount(world.broadPhase.trees[(int)B2BodyType.b2_kinematicBody]));
            writer.Write("dynamic tree: {0}\n", b2DynamicTree_GetByteCount(world.broadPhase.trees[(int)B2BodyType.b2_dynamicBody]));
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
            writer.Write("stack allocator: {0}\n\n", b2GetStackCapacity(world.stack));

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
        /// Overlap test for all shapes that *potentially* overlap the provided AABB.
        /// The AABB is relative to the origin, which keeps the test precise far from the world origin
        /// in large world mode. Near the origin pass b2Pos_zero and a world AABB.
        public static B2TreeStats b2World_OverlapAABB(B2WorldId worldId, B2Pos origin, in B2AABB aabb, in B2QueryFilter filter, b2OverlapResultFcn fcn, object context)
        {
            B2TreeStats treeStats = new B2TreeStats();

            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return treeStats;
            }

            B2_ASSERT(b2IsValidPosition(origin));
            B2_ASSERT(b2IsValidAABB(aabb));

            // Lift to a world float box with outward rounding so the conservative tree test never misses
            B2AABB worldBox = b2OffsetAABB(aabb, origin);

            B2WorldQueryContext worldContext = new B2WorldQueryContext(world, fcn, filter, context);

            for (int i = 0; i < (int)B2BodyType.b2_bodyTypeCount; ++i)
            {
                B2TreeStats treeResult =
                    b2DynamicTree_Query(world.broadPhase.trees[i], worldBox, filter.maskBits, TreeQueryCallback, ref worldContext);

                treeStats.nodeVisits += treeResult.nodeVisits;
                treeStats.leafVisits += treeResult.leafVisits;
            }

            return treeStats;
        }


        internal static bool TreeOverlapCallback(int proxyId, ulong userData, ref B2WorldOverlapContext context)
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

            // Re-center on the query origin so the distance test stays in float precision far from the world origin
            B2Body body = b2Array_Get(ref world.bodies, shape.bodyId);
            B2Transform transform = b2ToRelativeTransform(b2GetBodyTransformQuick(world, body), worldContext.origin);

            B2DistanceInput input = new B2DistanceInput();
            input.proxyA = worldContext.proxy;
            input.proxyB = b2MakeShapeDistanceProxy(shape);
            input.transform = transform;
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
        /// The proxy points are relative to the origin. Near the origin pass b2Pos_zero and world points.
        public static B2TreeStats b2World_OverlapShape(B2WorldId worldId, B2Pos origin, ref B2ShapeProxy proxy, in B2QueryFilter filter, b2OverlapResultFcn fcn, object context)
        {
            B2TreeStats treeStats = new B2TreeStats();

            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return treeStats;
            }

            B2_ASSERT(b2IsValidPosition(origin));

            // Relative box lifted to world float with outward rounding, conservative for the tree
            B2AABB aabb = b2OffsetAABB(b2MakeAABB(proxy.points.AsSpan(), proxy.count, proxy.radius), origin);
            B2WorldOverlapContext worldContext = new B2WorldOverlapContext(
                world, fcn, filter, proxy, origin, context
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

        internal static float RayCastCallback<T>(in B2RayCastInput input, int proxyId, ulong userData, ref B2WorldRayCastContext<T> context) where T : class
        {
            B2_UNUSED(proxyId);

            int shapeId = (int)userData;

            ref B2WorldRayCastContext<T> worldContext = ref context;
            B2World world = worldContext.world;

            B2Shape shape = b2Array_Get(ref world.shapes, shapeId);

            if (b2ShouldQueryCollide(shape.filter, worldContext.filter) == false)
            {
                return input.maxFraction;
            }

            B2Body body = b2Array_Get(ref world.bodies, shape.bodyId);
            B2WorldTransform bodyTransform = b2GetBodyTransformQuick(world, body);
            B2Transform transform = b2ToRelativeTransform(bodyTransform, worldContext.origin);

            B2RayCastInput localInput = input;
            localInput.origin = b2Vec2_zero;
            B2CastOutput output = b2RayCastShape(localInput, shape, transform);

            if (output.hit)
            {
                B2ShapeId id = new B2ShapeId(shapeId + 1, world.worldId, shape.generation);
                B2Pos point = b2OffsetPos(worldContext.origin, output.point);
                float fraction = worldContext.fcn(id, point, output.normal, output.fraction, ref worldContext.userContext);

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
        /// @return traversal performance counters
        public static B2TreeStats b2World_CastRay<T>(B2WorldId worldId, B2Pos origin, B2Vec2 translation, in B2QueryFilter filter, b2CastResultFcn<T> fcn, T context) where T : class
        {
            B2TreeStats treeStats = new B2TreeStats();

            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return treeStats;
            }

            B2_ASSERT(b2IsValidPosition(origin));
            B2_ASSERT(b2IsValidVec2(translation));

            // Tree traversal sees the origin truncated to float, displacing the ray by up to one
            // coordinate ULP, a graze sized miss tolerance at extreme range. Per-shape casts
            // re-difference against the full precision origin carried on the context.
            B2RayCastInput input = new B2RayCastInput(b2ToVec2(origin), translation, 1.0f);

            var worldContext = B2WorldRayCastContext.Create(world, fcn, filter, 1.0f, origin, context);

            for (int i = 0; i < (int)B2BodyType.b2_bodyTypeCount; ++i)
            {
                B2TreeStats treeResult =
                    b2DynamicTree_CastRay(world.broadPhase.trees[i], input, filter.maskBits, RayCastCallback, ref worldContext);
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
        internal static float b2RayCastClosestFcn(B2ShapeId shapeId, B2Pos point, B2Vec2 normal, float fraction, ref B2RayResult context)
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
        public static B2RayResult b2World_CastRayClosest(B2WorldId worldId, B2Pos origin, B2Vec2 translation, in B2QueryFilter filter)
        {
            B2RayResult result = new B2RayResult();

            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return result;
            }

            B2_ASSERT(b2IsValidPosition(origin));
            B2_ASSERT(b2IsValidVec2(translation));

            B2RayCastInput input = new B2RayCastInput(b2ToVec2(origin), translation, 1.0f);
            var worldContext = B2WorldRayCastContext.Create(world, b2RayCastClosestFcn, filter, 1.0f, origin, result);

            for (int i = 0; i < (int)B2BodyType.b2_bodyTypeCount; ++i)
            {
                B2TreeStats treeResult =
                    b2DynamicTree_CastRay(world.broadPhase.trees[i], input, filter.maskBits, RayCastCallback, ref worldContext);
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

        internal static float ShapeCastCallback<T>(in B2BoxCastInput input, int proxyId, ulong userData, ref B2WorldShapeCastContext<T> context) where T : class
        {
            B2_UNUSED(proxyId);

            int shapeId = (int)userData;

            ref B2WorldShapeCastContext<T> worldContext = ref context;
            B2World world = worldContext.world;

            B2Shape shape = b2Array_Get(ref world.shapes, shapeId);

            if (b2ShouldQueryCollide(shape.filter, worldContext.filter) == false)
            {
                return input.maxFraction;
            }

            // Rebuild from the origin relative input, taking only the advancing fraction from the tree.
            // The tree input is world float and would lose the cast far from the origin.
            B2ShapeCastInput localInput = worldContext.input;
            localInput.maxFraction = input.maxFraction;

            B2Body body = b2Array_Get(ref world.bodies, shape.bodyId);
            B2WorldTransform transform = b2GetBodyTransformQuick(world, body);
            B2Transform localTransform = b2ToRelativeTransform(transform, worldContext.origin);

            B2CastOutput output = b2ShapeCastShape(localInput, shape, localTransform);

            if (output.hit)
            {
                B2ShapeId id = new B2ShapeId(shapeId + 1, world.worldId, shape.generation);

                B2Pos point = b2OffsetPos(worldContext.origin, output.point);
                float fraction = worldContext.fcn(id, point, output.normal, output.fraction, ref worldContext.userContext);

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
        /// The proxy points are relative to the origin. Callback points are world positions.
        /// @see b2World_CastRay
        public static B2TreeStats b2World_CastShape<T>(B2WorldId worldId, B2Pos origin, ref B2ShapeProxy proxy, B2Vec2 translation, in B2QueryFilter filter, b2CastResultFcn<T> fcn, T context) where T : class
        {
            B2TreeStats treeStats = new B2TreeStats();

            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return treeStats;
            }

            B2_ASSERT(b2IsValidPosition(origin));
            B2_ASSERT(b2IsValidVec2(translation));

            B2WorldShapeCastContext<T> worldContext = new B2WorldShapeCastContext<T>();
            worldContext.world = world;
            worldContext.fcn = fcn;
            worldContext.filter = filter;
            worldContext.fraction = 1.0f;
            worldContext.origin = origin;
            worldContext.input.proxy = proxy;
            worldContext.input.translation = translation;
            worldContext.input.maxFraction = 1.0f;
            worldContext.userContext = context;

            // Create a conservative world space box from the proxy and origin.
            B2AABB localBox = b2MakeAABB(proxy.points.AsSpan(), proxy.count, proxy.radius);
            B2AABB box = b2OffsetAABB(localBox, origin);
            B2BoxCastInput treeInput = new B2BoxCastInput();
            treeInput.box = box;
            treeInput.translation = translation;
            treeInput.maxFraction = 1.0f;

            for (int i = 0; i < (int)B2BodyType.b2_bodyTypeCount; ++i)
            {
                B2TreeStats treeResult =
                    b2DynamicTree_CastBox(world.broadPhase.trees[i], treeInput, filter.maskBits, ShapeCastCallback, ref worldContext);
                treeStats.nodeVisits += treeResult.nodeVisits;
                treeStats.leafVisits += treeResult.leafVisits;

                if (worldContext.fraction == 0.0f)
                {
                    return treeStats;
                }

                treeInput.maxFraction = worldContext.fraction;
            }

            return treeStats;
        }

        internal static float MoverCastCallback(in B2BoxCastInput input, int proxyId, ulong userData, ref B2WorldMoverCastContext context)
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

            // Rebuild from the origin relative input, taking only the advancing fraction from the tree
            B2ShapeCastInput localInput = worldContext.input;
            localInput.maxFraction = input.maxFraction;

            B2Body body = b2Array_Get(ref world.bodies, shape.bodyId);
            B2Transform transform = b2ToRelativeTransform(b2GetBodyTransformQuick(world, body), worldContext.origin);

            B2CastOutput output = b2ShapeCastShape(localInput, shape, transform);
            if (output.fraction == 0.0f)
            {
                // Ignore overlapping shapes
                return worldContext.fraction;
            }

            worldContext.fraction = output.fraction;
            return output.fraction;
        }

        /// Cast a capsule mover through the world. This is a special shape cast that handles sliding along other shapes while reducing
        /// clipping. The mover capsule is relative to the origin. Near the origin pass b2Pos_zero and a world capsule.
        public static float b2World_CastMover(B2WorldId worldId, B2Pos origin, in B2Capsule mover, B2Vec2 translation, in B2QueryFilter filter)
        {
            B2_ASSERT(b2IsValidPosition(origin));
            B2_ASSERT(b2IsValidVec2(translation));
            B2_ASSERT(mover.radius > 2.0f * B2_LINEAR_SLOP);

            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return 1.0f;
            }

            B2WorldMoverCastContext worldContext = new B2WorldMoverCastContext();
            worldContext.world = world;
            worldContext.filter = filter;
            worldContext.fraction = 1.0f;
            worldContext.origin = origin;
            worldContext.input.proxy.points[0] = mover.center1;
            worldContext.input.proxy.points[1] = mover.center2;
            worldContext.input.proxy.count = 2;
            worldContext.input.proxy.radius = mover.radius;
            worldContext.input.translation = translation;
            worldContext.input.maxFraction = 1.0f;
            worldContext.input.canEncroach = true;

            // Bound the capsule in origin relative space then lift to a conservative world float box
            Span<B2Vec2> centers = stackalloc B2Vec2[2] { mover.center1, mover.center2 };
            B2AABB box = b2OffsetAABB(b2MakeAABB(centers, 2, mover.radius), origin);
            B2BoxCastInput treeInput = new B2BoxCastInput();
            treeInput.box = box;
            treeInput.translation = translation;
            treeInput.maxFraction = 1.0f;

            for (int i = 0; i < (int)B2BodyType.b2_bodyTypeCount; ++i)
            {
                b2DynamicTree_CastBox(world.broadPhase.trees[i], treeInput, filter.maskBits, MoverCastCallback, ref worldContext);

                if (worldContext.fraction == 0.0f)
                {
                    return 0.0f;
                }

                treeInput.maxFraction = worldContext.fraction;
            }

            return worldContext.fraction;
        }

        internal static bool TreeCollideCallback(int proxyId, ulong userData, ref B2WorldMoverContext context)
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


            // Re-center on the query origin, the mover and the resulting planes are origin relative
            B2Body body = b2Array_Get(ref world.bodies, shape.bodyId);
            B2Transform transform = b2ToRelativeTransform(b2GetBodyTransformQuick(world, body), worldContext.origin);

            B2PlaneResult result = b2CollideMover(worldContext.mover, shape, transform);

            // todo handle deep overlap
            if (result.hit && b2IsNormalized(result.plane.normal))
            {
                B2ShapeId id = new B2ShapeId(shape.id + 1, world.worldId, shape.generation);
                return worldContext.fcn(id, ref result, worldContext.userContext);
            }

            return true;
        }


        /// Collide a capsule mover with the world, gathering collision planes that can be fed to b2SolvePlanes. Useful for
        /// kinematic character movement. The mover capsule and the resulting planes are relative to the origin.
        public static void b2World_CollideMover(B2WorldId worldId, B2Pos origin, in B2Capsule mover, in B2QueryFilter filter, b2PlaneResultFcn fcn, object context)
        {
            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return;
            }

            B2_ASSERT(b2IsValidPosition(origin));

            B2Vec2 r = new B2Vec2(mover.radius, mover.radius);

            // Relative box lifted to world float with outward rounding, conservative for the tree
            B2AABB relBox;
            relBox.lowerBound = b2Sub(b2Min(mover.center1, mover.center2), r);
            relBox.upperBound = b2Add(b2Max(mover.center1, mover.center2), r);
            B2AABB aabb = b2OffsetAABB(relBox, origin);

            B2WorldMoverContext worldContext = new B2WorldMoverContext();
            worldContext.world = world;
            worldContext.fcn = fcn;
            worldContext.filter = filter;
            worldContext.mover = mover;
            worldContext.origin = origin;
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
        /// Register the custom filter callback. This is optional.
        public static void b2World_SetCustomFilterCallback(B2WorldId worldId, b2CustomFilterFcn fcn, object context)
        {
            B2World world = b2GetWorldFromId(worldId);
            world.customFilterFcn = fcn;
            world.customFilterContext = context;
        }
        /// Register the pre-solve and pre-continuous callback. This is optional.
        public static void b2World_SetPreSolveCallback(B2WorldId worldId, b2PreSolveFcn preSolveFcn, b2PreContinuousFcn preContinuousFcn, object context)
        {
            B2World world = b2GetWorldFromId(worldId);

            world.preSolveFcn = preSolveFcn;
            world.preContinuousFcn = preContinuousFcn;
            world.preSolveContext = context;
        }
        /// Set the gravity vector for the entire world. Box2D has no concept of an up direction and this
        /// is left as a decision for the application. Usually in m/s^2.
        /// @see b2WorldDef
        public static void b2World_SetGravity(B2WorldId worldId, B2Vec2 gravity)
        {
            if (B2_CHECK_INPUT(b2IsValidVec2(gravity)) == false) return;

            B2World world = b2GetWorldFromId(worldId);
            world.gravity = gravity;
        }
        /// Get the gravity vector
        public static B2Vec2 b2World_GetGravity(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            return world.gravity;
        }

        internal static bool ExplosionCallback(int proxyId, ulong userData, ref B2ExplosionContext context)
        {
            B2_UNUSED(proxyId);

            int shapeId = (int)userData;

            ref B2ExplosionContext explosionContext = ref context;
            B2World world = explosionContext.world;

            B2Shape shape = b2Array_Get(ref world.shapes, shapeId);

            B2Body body = b2Array_Get(ref world.bodies, shape.bodyId);
            B2_ASSERT(body.type == B2BodyType.b2_dynamicBody);

            B2WorldTransform xf = b2GetBodyTransformQuick(world, body);

            // Re-center the explosion into the shape local frame so distance and direction stay precise
            // far from the origin. Everything below runs in that near-origin frame.
            B2Vec2 localPosition = b2InvTransformWorldPoint(xf, explosionContext.position);

            B2DistanceInput input = new B2DistanceInput();
            input.proxyA = b2MakeShapeDistanceProxy(shape);
            input.proxyB = b2MakeProxy(localPosition, 1, 0.0f);
            input.transform = b2Transform_identity;
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

            if (body.setIndex != (int)B2SolverSetType.b2_awakeSet)
            {
                return true;
            }

            B2Vec2 closestPoint = output.pointA;
            if (output.distance == 0.0f)
            {
                closestPoint = b2GetShapeCentroid(shape);
            }

            B2Vec2 direction = b2Sub(closestPoint, localPosition);
            if (b2LengthSquared(direction) > 100.0f * FLT_EPSILON * FLT_EPSILON)
            {
                direction = b2Normalize(direction);
            }
            else
            {
                direction = new B2Vec2(1.0f, 0.0f);
            }

            B2Vec2 localLine = b2LeftPerp(direction);
            float perimeter = b2GetShapeProjectedPerimeter(shape, localLine);
            float scale = 1.0f;
            if (output.distance > radius && falloff > 0.0f)
            {
                scale = b2ClampFloat((radius + falloff - output.distance) / falloff, 0.0f, 1.0f);
            }

            float magnitude = explosionContext.impulsePerLength * perimeter * scale;
            B2Vec2 impulse = b2MulSV(magnitude, b2RotateVector(xf.q, direction));

            int localIndex = body.localIndex;
            B2SolverSet set = b2Array_Get(ref world.solverSets, (int)B2SolverSetType.b2_awakeSet);
            B2BodyState state = b2Array_Get(ref set.bodyStates, localIndex);
            B2BodySim bodySim = b2Array_Get(ref set.bodySims, localIndex);
            state.linearVelocity = b2MulAdd(state.linearVelocity, bodySim.invMass, impulse);

            // Lever arm from the center of mass to the closest point, rotated to world
            B2Vec2 r = b2RotateVector(xf.q, b2Sub(closestPoint, bodySim.localCenter));
            state.angularVelocity += bodySim.invInertia * b2Cross(r, impulse);

            return true;
        }
        /// Apply a radial explosion
        /// @param worldId The world id
        /// @param explosionDef The explosion definition
        public static void b2World_Explode(B2WorldId worldId, in B2ExplosionDef explosionDef)
        {
            ulong maskBits = explosionDef.maskBits;
            B2Pos position = explosionDef.position;
            float radius = explosionDef.radius;
            float falloff = explosionDef.falloff;
            float impulsePerLength = explosionDef.impulsePerLength;

            if (B2_CHECK_INPUT(b2IsValidPosition(position)) == false) return;
            if (B2_CHECK_INPUT(b2IsValidFloat(radius) && radius >= 0.0f) == false) return;
            if (B2_CHECK_INPUT(b2IsValidFloat(falloff) && falloff >= 0.0f) == false) return;
            if (B2_CHECK_INPUT(b2IsValidFloat(impulsePerLength)) == false) return;

            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return;
            }

            B2ExplosionContext explosionContext = new B2ExplosionContext(world, position, radius, falloff, impulsePerLength);

            // The broad-phase tree is float, so translate a local query box out to world with outward rounding
            float extent = radius + falloff;
            B2AABB localBox = new B2AABB(new B2Vec2(-extent, -extent), new B2Vec2(extent, extent));
            B2AABB aabb = b2OffsetAABB(localBox, position);

            b2DynamicTree_Query(world.broadPhase.trees[(int)B2BodyType.b2_dynamicBody], aabb, maskBits, ExplosionCallback, ref explosionContext);
        }
        /// This is for internal testing
        // This is for internal testing. Not optimized.
        public static void b2World_RebuildStaticTree(B2WorldId worldId)
        {
            B2World world = b2GetWorldFromId(worldId);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return;
            }

            B2DynamicTree staticTree = world.broadPhase.trees[(int)B2BodyType.b2_staticBody];

            // Moved proxies must retain the flag in the rebuild.
            int movedCount = 0;
            int[] movedProxies = null;
            int proxyCount = b2DynamicTree_GetProxyCount(staticTree);
            if (b2HasTreeMoved(staticTree))
            {
                movedProxies = b2Alloc<int>(proxyCount);
                movedCount = b2DynamicTree_GatherMovedProxies(staticTree, movedProxies);
            }

            b2DynamicTree_Rebuild(staticTree, true);

            for (int i = 0; i < movedCount; ++i)
            {
                b2DynamicTree_MarkProxyMovedSerial(staticTree, movedProxies[i]);
            }

            if (movedProxies != null)
            {
                b2Free(movedProxies, proxyCount);
            }
        }

#if DEBUG
        // This validates island graph connectivity for each body
        internal static void b2ValidateConnectivity(B2World world)
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
                int bodyIslandId = body.islandId;
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
                        if (bodySetIndex != (int)B2SolverSetType.b2_staticSet)
                        {
                            int contactIslandId = contact.islandId;
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

                    if (bodySetIndex == (int)B2SolverSetType.b2_disabledSet || otherBody.setIndex == (int)B2SolverSetType.b2_disabledSet)
                    {
                        B2_ASSERT(joint.islandId == B2_NULL_INDEX);
                    }
                    else if (bodySetIndex == (int)B2SolverSetType.b2_staticSet)
                    {
                        // Intentional nesting
                        if (otherBody.setIndex == (int)B2SolverSetType.b2_staticSet)
                        {
                            B2_ASSERT(joint.islandId == B2_NULL_INDEX);
                        }
                    }
                    else if (body.type != B2BodyType.b2_dynamicBody && otherBody.type != B2BodyType.b2_dynamicBody)
                    {
                        B2_ASSERT(joint.islandId == B2_NULL_INDEX);
                    }
                    else
                    {
                        int jointIslandId = joint.islandId;
                        B2_ASSERT(jointIslandId == bodyIslandId);
                    }

                    jointKey = joint.edges[edgeIndex].nextKey;
                }
            }
        }

        // Validates solver sets, but not island connectivity
        internal static void b2ValidateSolverSets(B2World world)
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

                    if (setIndex == (int)B2SolverSetType.b2_staticSet)
                    {
                        B2_ASSERT(set.contactSims.count == 0);
                        B2_ASSERT(set.islandSims.count == 0);
                        B2_ASSERT(set.bodyStates.count == 0);
                    }
                    else if (setIndex == (int)B2SolverSetType.b2_disabledSet)
                    {
                        B2_ASSERT(set.islandSims.count == 0);
                        B2_ASSERT(set.bodyStates.count == 0);
                    }
                    else if (setIndex == (int)B2SolverSetType.b2_awakeSet)
                    {
                        B2_ASSERT(set.bodySims.count == set.bodyStates.count);
                        B2_ASSERT(set.jointSims.count == 0);
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

                            uint syncedFlags = body.flags & ~((uint)B2BodyFlags.b2_isFast | (uint)B2BodyFlags.b2_bodyTransientFlags);
                            B2_ASSERT((bodySim.flags & syncedFlags) == syncedFlags);

                            B2BodyState bodyState = b2GetBodyState(world, body);
                            if (bodyState != null)
                            {
                                B2_ASSERT((bodyState.flags & syncedFlags) == syncedFlags);
                            }

                            if (body.type == B2BodyType.b2_dynamicBody)
                            {
                                B2_ASSERT(0 != (body.flags & (uint)B2BodyFlags.b2_dynamicFlag));
                            }

                            if (setIndex == (int)B2SolverSetType.b2_disabledSet)
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

                                if (setIndex == (int)B2SolverSetType.b2_disabledSet)
                                {
                                    B2_ASSERT(shape.proxyKey == B2_NULL_INDEX);
                                }
                                else if (setIndex == (int)B2SolverSetType.b2_staticSet)
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
                                B2_ASSERT(contact.setIndex != (int)B2SolverSetType.b2_staticSet);
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

                                if (setIndex == (int)B2SolverSetType.b2_disabledSet || otherBody.setIndex == (int)B2SolverSetType.b2_disabledSet)
                                {
                                    B2_ASSERT(joint.setIndex == (int)B2SolverSetType.b2_disabledSet);
                                }
                                else if (setIndex == (int)B2SolverSetType.b2_staticSet && otherBody.setIndex == (int)B2SolverSetType.b2_staticSet)
                                {
                                    B2_ASSERT(joint.setIndex == (int)B2SolverSetType.b2_staticSet);
                                }
                                else if (body.type != B2BodyType.b2_dynamicBody && otherBody.type != B2BodyType.b2_dynamicBody)
                                {
                                    B2_ASSERT(joint.setIndex == (int)B2SolverSetType.b2_staticSet);
                                }
                                else if (setIndex == (int)B2SolverSetType.b2_awakeSet)
                                {
                                    B2_ASSERT(joint.setIndex == (int)B2SolverSetType.b2_awakeSet);
                                }
                                else if (setIndex >= (int)B2SolverSetType.b2_firstSleepingSet)
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
                            if (setIndex == (int)B2SolverSetType.b2_awakeSet)
                            {
                                // contact should be non-touching if awake
                                // or it could be this contact hasn't been transferred yet
                                B2_ASSERT(contactSim.manifold.pointCount == 0 ||
                                          (contactSim.simFlags & (uint)B2ContactFlags.b2_simStartedTouching) != 0);
                            }

                            B2_ASSERT(contact.setIndex == setIndex);
                            B2_ASSERT(contact.colorIndex == B2_NULL_INDEX);
                            B2_ASSERT(contact.localIndex == i);

                            B2Body edgeBodyA = b2Array_Get(ref world.bodies, contact.edges[0].bodyId);
                            B2Body edgeBodyB = b2Array_Get(ref world.bodies, contact.edges[1].bodyId);
                            B2_ASSERT(contactSim.encodedBodySimA == b2EncodeBodySimIndex(edgeBodyA));
                            B2_ASSERT(contactSim.encodedBodySimB == b2EncodeBodySimIndex(edgeBodyB));
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
                int bitCount = 0;

                B2_ASSERT(color.contactSims.count >= 0);
                totalContactCount += color.contactSims.count;
                for (int i = 0; i < color.contactSims.count; ++i)
                {
                    B2ContactSim contactSim = color.contactSims.data[i];
                    B2Contact contact = b2Array_Get(ref world.contacts, contactSim.contactId);
                    // contact should be touching in the constraint graph or awaiting transfer to non-touching
                    B2_ASSERT(contactSim.manifold.pointCount > 0 ||
                              (contactSim.simFlags & ((uint)B2ContactFlags.b2_simStoppedTouching | (uint)B2ContactFlags.b2_simDisjoint)) != 0);
                    B2_ASSERT(contact.setIndex == (int)B2SolverSetType.b2_awakeSet);
                    B2_ASSERT(contact.colorIndex == colorIndex);
                    B2_ASSERT(contact.localIndex == i);

                    int bodyIdA = contact.edges[0].bodyId;
                    int bodyIdB = contact.edges[1].bodyId;

                    B2Body edgeBodyA = b2Array_Get(ref world.bodies, bodyIdA);
                    B2Body edgeBodyB = b2Array_Get(ref world.bodies, bodyIdB);
                    B2_ASSERT(contactSim.encodedBodySimA == b2EncodeBodySimIndex(edgeBodyA));
                    B2_ASSERT(contactSim.encodedBodySimB == b2EncodeBodySimIndex(edgeBodyB));

                    if (colorIndex < B2_OVERFLOW_INDEX)
                    {
                        B2_ASSERT(b2GetBit(ref color.bodySet, bodyIdA) == (edgeBodyA.type == B2BodyType.b2_dynamicBody));
                        B2_ASSERT(b2GetBit(ref color.bodySet, bodyIdB) == (edgeBodyB.type == B2BodyType.b2_dynamicBody));

                        bitCount += edgeBodyA.type == B2BodyType.b2_dynamicBody ? 1 : 0;
                        bitCount += edgeBodyB.type == B2BodyType.b2_dynamicBody ? 1 : 0;
                    }
                }


                B2_ASSERT(color.jointSims.count >= 0);
                totalJointCount += color.jointSims.count;
                for (int i = 0; i < color.jointSims.count; ++i)
                {
                    B2JointSim jointSim = color.jointSims.data[i];
                    B2Joint joint = b2Array_Get(ref world.joints, jointSim.jointId);
                    B2_ASSERT(joint.setIndex == (int)B2SolverSetType.b2_awakeSet);
                    B2_ASSERT(joint.colorIndex == colorIndex);
                    B2_ASSERT(joint.localIndex == i);

                    int bodyIdA = joint.edges[0].bodyId;
                    int bodyIdB = joint.edges[1].bodyId;

                    if (colorIndex < B2_OVERFLOW_INDEX)
                    {
                        B2Body bodyA = b2Array_Get(ref world.bodies, bodyIdA);
                        B2Body bodyB = b2Array_Get(ref world.bodies, bodyIdB);

                        B2_ASSERT(b2GetBit(ref color.bodySet, bodyIdA) == (bodyA.type == B2BodyType.b2_dynamicBody));
                        B2_ASSERT(b2GetBit(ref color.bodySet, bodyIdB) == (bodyB.type == B2BodyType.b2_dynamicBody));

                        bitCount += bodyA.type == B2BodyType.b2_dynamicBody ? 1 : 0;
                        bitCount += bodyB.type == B2BodyType.b2_dynamicBody ? 1 : 0;
                    }
                }

                // Validate the bit population for this graph color
                B2_ASSERT(bitCount == b2CountSetBits(ref color.bodySet));
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
        internal static void b2ValidateContacts(B2World world)
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

                if (setId == (int)B2SolverSetType.b2_awakeSet)
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
                else if (setId >= (int)B2SolverSetType.b2_firstSleepingSet)
                {
                    // Only touching contacts allowed in a sleeping set
                    B2_ASSERT(touching == true);
                }
                else
                {
                    // Sleeping and non-touching contacts belong in the disabled set
                    B2_ASSERT(touching == false && setId == (int)B2SolverSetType.b2_disabledSet);
                }

                B2ContactSim contactSim = b2GetContactSim(world, contact);
                B2_ASSERT(contactSim.contactId == contactIndex);
                B2_ASSERT(contactSim.bodyIdA == contact.edges[0].bodyId);
                B2_ASSERT(contactSim.bodyIdB == contact.edges[1].bodyId);

                bool simTouching = (contactSim.simFlags & (uint)B2ContactFlags.b2_simTouchingFlag) != 0;
                B2_ASSERT(touching == simTouching);

                B2_ASSERT(0 <= contactSim.manifold.pointCount && contactSim.manifold.pointCount <= 2);
            }

            int contactIdCount = b2GetIdCount(world.contactIdPool);
            B2_ASSERT(allocatedContactCount == contactIdCount);
        }

#else
        // This validates island graph connectivity for each body
        internal static void b2ValidateConnectivity(B2World world)
        {
            B2_UNUSED(world);
        }
        // Validates solver sets, but not island connectivity
        internal static void b2ValidateSolverSets(B2World world)
        {
            B2_UNUSED(world);
        }
        // Validate contact touching status.
        internal static void b2ValidateContacts(B2World world)
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
