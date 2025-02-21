using static Box2D.NET.core;
using static Box2D.NET.constants;

namespace Box2D.NET.Primitives
{
    // The world struct manages all physics entities, dynamic simulation,  and asynchronous queries.
    // The world also contains efficient memory management facilities.
    public class b2World
    {
        public b2ArenaAllocator stackAllocator;
        public b2BroadPhase broadPhase;
        public b2ConstraintGraph constraintGraph;

        // The body id pool is used to allocate and recycle body ids. Body ids
        // provide a stable identifier for users, but incur caches misses when used
        // to access body data. Aligns with b2Body.
        public b2IdPool bodyIdPool;

        // This is a sparse array that maps body ids to the body data
        // stored in solver sets. As sims move within a set or across set.
        // Indices come from id pool.
        public b2Array<b2Body> bodies;

        // Provides free list for solver sets.
        public b2IdPool solverSetIdPool;

        // Solvers sets allow sims to be stored in contiguous arrays. The first
        // set is all static sims. The second set is active sims. The third set is disabled
        // sims. The remaining sets are sleeping islands.
        public b2Array<b2SolverSet> solverSets;

        // Used to create stable ids for joints
        public b2IdPool jointIdPool;

        // This is a sparse array that maps joint ids to the joint data stored in the constraint graph
        // or in the solver sets.
        public b2Array<b2Joint> joints;

        // Used to create stable ids for contacts
        public b2IdPool contactIdPool;

        // This is a sparse array that maps contact ids to the contact data stored in the constraint graph
        // or in the solver sets.
        public b2Array<b2Contact> contacts;

        // Used to create stable ids for islands
        public b2IdPool islandIdPool;

        // This is a sparse array that maps island ids to the island data stored in the solver sets.
        public b2Array<b2Island> islands;

        public b2IdPool shapeIdPool;
        public b2IdPool chainIdPool;

        // These are sparse arrays that point into the pools above
        public b2Array<b2Shape> shapes;
        public b2Array<b2ChainShape> chainShapes;

        // This is a dense array of sensor data.
        public b2Array<b2Sensor> sensors;

        // Per thread storage
        public b2Array<b2TaskContext> taskContexts;
        public b2Array<b2SensorTaskContext> sensorTaskContexts;

        public b2Array<b2BodyMoveEvent> bodyMoveEvents;
        public b2Array<b2SensorBeginTouchEvent> sensorBeginEvents;
        public b2Array<b2ContactBeginTouchEvent> contactBeginEvents;

        // End events are double buffered so that the user doesn't need to flush events
        public b2Array<b2SensorEndTouchEvent>[] sensorEndEvents = new b2Array<b2SensorEndTouchEvent>[2];
        public b2Array<b2ContactEndTouchEvent>[] contactEndEvents = new b2Array<b2ContactEndTouchEvent>[2];
        public int endEventArrayIndex;

        public b2Array<b2ContactHitEvent> contactHitEvents;

        // Used to track debug draw
        public b2BitSet debugBodySet;
        public b2BitSet debugJointSet;
        public b2BitSet debugContactSet;

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

        public b2Vec2 gravity;
        public float hitEventThreshold;
        public float restitutionThreshold;
        public float maxLinearSpeed;
        public float contactMaxPushSpeed;
        public float contactHertz;
        public float contactDampingRatio;
        public float jointHertz;
        public float jointDampingRatio;

        public b2FrictionCallback frictionCallback;
        public b2RestitutionCallback restitutionCallback;

        public ushort generation;

        public b2Profile profile;

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
        public readonly b2WorkerContext[] workerContext = b2Alloc<b2WorkerContext>(B2_MAX_WORKERS);

        public void Reset()
        {
            // ..?
        }
    }
}