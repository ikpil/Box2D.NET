// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Buffers;
using static Box2D.NET.B2Constants;

namespace Box2D.NET
{
    // The world struct manages all physics entities, dynamic simulation,  and asynchronous queries.
    // The world also contains efficient memory management facilities.
    public class B2World
    {
        public B2ArenaAllocator arena;
        public B2BroadPhase broadPhase;
        public B2ConstraintGraph constraintGraph;

        // The body id pool is used to allocate and recycle body ids. Body ids
        // provide a stable identifier for users, but incur caches misses when used
        // to access body data. Aligns with b2Body.
        public B2IdPool bodyIdPool;

        // This is a sparse array that maps body ids to the body data
        // stored in solver sets. As sims move within a set or across set.
        // Indices come from id pool.
        public B2Array<B2Body> bodies;

        // Provides free list for solver sets.
        public B2IdPool solverSetIdPool;

        // Solvers sets allow sims to be stored in contiguous arrays. The first
        // set is all static sims. The second set is active sims. The third set is disabled
        // sims. The remaining sets are sleeping islands.
        public B2Array<B2SolverSet> solverSets;

        // Used to create stable ids for joints
        public B2IdPool jointIdPool;

        // This is a sparse array that maps joint ids to the joint data stored in the constraint graph
        // or in the solver sets.
        public B2Array<B2Joint> joints;

        // Used to create stable ids for contacts
        public B2IdPool contactIdPool;

        // This is a sparse array that maps contact ids to the contact data stored in the constraint graph
        // or in the solver sets.
        public B2Array<B2Contact> contacts;

        // Used to create stable ids for islands
        public B2IdPool islandIdPool;

        // This is a sparse array that maps island ids to the island data stored in the solver sets.
        public B2Array<B2Island> islands;

        public B2IdPool shapeIdPool;
        public B2IdPool chainIdPool;

        // These are sparse arrays that point into the pools above
        public B2Array<B2Shape> shapes;
        public B2Array<B2ChainShape> chainShapes;

        // This is a dense array of sensor data.
        public B2Array<B2Sensor> sensors;

        // Per thread storage
        public B2Array<B2TaskContext> taskContexts;
        public B2Array<B2SensorTaskContext> sensorTaskContexts;

        public B2Array<B2BodyMoveEvent> bodyMoveEvents;
        public B2Array<B2SensorBeginTouchEvent> sensorBeginEvents;
        public B2Array<B2ContactBeginTouchEvent> contactBeginEvents;

        // End events are double buffered so that the user doesn't need to flush events
        public B2Array<B2SensorEndTouchEvent>[] sensorEndEvents = new B2Array<B2SensorEndTouchEvent>[2];
        public B2Array<B2ContactEndTouchEvent>[] contactEndEvents = new B2Array<B2ContactEndTouchEvent>[2];
        public int endEventArrayIndex;

        public B2Array<B2ContactHitEvent> contactHitEvents;
        public B2Array<B2JointEvent> jointEvents;

        // Used to track debug draw
        public B2BitSet debugBodySet;
        public B2BitSet debugJointSet;
        public B2BitSet debugContactSet;
        public B2BitSet debugIslandSet;

        // Id that is incremented every time step
        public ulong stepIndex;

        // Identify islands for splitting as follows:
        // - I want to split islands so smaller islands can sleep
        // - when a body comes to rest and its sleep timer trips, I can look at the island and flag it for splitting
        //   if it has removed constraints
        // - islands that have removed constraints must be put split first because I don't want to wake bodies incorrectly
        // - otherwise I can use the awake islands that have bodies wanting to sleep as the splitting candidates
        // - if no bodies want to sleep then there is no reason to perform island splitting
        public int splitIslandId;

        public B2Vec2 gravity;
        public float hitEventThreshold;
        public float restitutionThreshold;
        public float maxLinearSpeed;
        public float contactSpeed;
        public float contactHertz;
        public float contactDampingRatio;

        public b2FrictionCallback frictionCallback;
        public b2RestitutionCallback restitutionCallback;

        public ushort generation;

        public B2Profile profile;

        public b2PreSolveFcn preSolveFcn;
        public object preSolveContext;

        public b2CustomFilterFcn customFilterFcn;
        public object customFilterContext;

        public int workerCount;
        public b2EnqueueTaskCallback enqueueTaskFcn;
        public b2FinishTaskCallback finishTaskFcn;
        public object userTaskContext;
        public object userTreeTask;

        public object userData;

        // Remember type step used for reporting forces and torques
        public float inv_h;

        public int activeTaskCount;
        public int taskCount;

        public ushort worldId;

        public bool enableSleep;
        public bool locked;
        public bool enableWarmStarting;
        public bool enableContinuous;
        public bool enableSpeculative;
        public bool inUse;

        // TODO: @ikpil for b2Solve 
        public readonly B2WorkerContext[] tempWorkerContext = b2Alloc<B2WorkerContext>(B2_MAX_WORKERS);

        public void Clear()
        {
            arena = null;
            broadPhase = null;

            bodyIdPool = null;

            bodies = new B2Array<B2Body>();

            solverSetIdPool = null;

            solverSets = new B2Array<B2SolverSet>();

            jointIdPool = null;

            joints = new B2Array<B2Joint>();

            contactIdPool = null;

            contacts = new B2Array<B2Contact>();

            islandIdPool = null;

            islands = new B2Array<B2Island>();

            shapeIdPool = null;
            chainIdPool = null;

            shapes = new B2Array<B2Shape>();
            chainShapes = new B2Array<B2ChainShape>();

            sensors = new B2Array<B2Sensor>();

            taskContexts = new B2Array<B2TaskContext>();
            sensorTaskContexts = new B2Array<B2SensorTaskContext>();

            bodyMoveEvents = new B2Array<B2BodyMoveEvent>();
            sensorBeginEvents = new B2Array<B2SensorBeginTouchEvent>();
            contactBeginEvents = new B2Array<B2ContactBeginTouchEvent>();

            sensorEndEvents[0] = new B2Array<B2SensorEndTouchEvent>();
            sensorEndEvents[1] = new B2Array<B2SensorEndTouchEvent>();
            contactEndEvents[0] = new B2Array<B2ContactEndTouchEvent>();
            contactEndEvents[1] = new B2Array<B2ContactEndTouchEvent>();
            endEventArrayIndex = 0;

            contactHitEvents = new B2Array<B2ContactHitEvent>();
            jointEvents = new B2Array<B2JointEvent>();

            // debugBodySet = null;
            // debugJointSet = null;
            // debugContactSet = null;

            stepIndex = 0;

            splitIslandId = 0;

            gravity = new B2Vec2();
            hitEventThreshold = 0.0f;
            restitutionThreshold = 0.0f;
            maxLinearSpeed = 0.0f;
            contactSpeed = 0.0f;
            contactHertz = 0.0f;
            contactDampingRatio = 0.0f;

            frictionCallback = null;
            restitutionCallback = null;

            generation = 0;

            profile = new B2Profile();

            preSolveFcn = null;
            preSolveContext = null;

            customFilterFcn = null;
            customFilterContext = null;

            workerCount = 0;
            enqueueTaskFcn = null;
            finishTaskFcn = null;
            userTaskContext = null;
            userTreeTask = null;

            userData = null;

            inv_h = 0.0f;

            activeTaskCount = 0;
            taskCount = 0;

            worldId = 0;

            enableSleep = false;
            locked = false;
            enableWarmStarting = false;
            enableContinuous = false;
            enableSpeculative = false;
            inUse = false;

            foreach (var workerContext in tempWorkerContext)
            {
                workerContext.Clear();
            }
        }
    }
}
