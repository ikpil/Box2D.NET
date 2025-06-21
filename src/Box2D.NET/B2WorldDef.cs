// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// World definition used to create a simulation world.
    /// Must be initialized using b2DefaultWorldDef().
    /// @ingroup world
    public struct B2WorldDef
    {
        /// Gravity vector. Box2D has no up-vector defined.
        public B2Vec2 gravity;

        /// Restitution speed threshold, usually in m/s. Collisions above this
        /// speed have restitution applied (will bounce).
        public float restitutionThreshold;

        /// Threshold speed for hit events. Usually meters per second.
        public float hitEventThreshold;

        /// Contact stiffness. Cycles per second. Increasing this increases the speed of overlap recovery, but can introduce jitter.
        public float contactHertz;

        /// Contact bounciness. Non-dimensional. You can speed up overlap recovery by decreasing this with
        /// the trade-off that overlap resolution becomes more energetic.
        public float contactDampingRatio;

        /// This parameter controls how fast overlap is resolved and usually has units of meters per second. This only
        /// puts a cap on the resolution speed. The resolution speed is increased by increasing the hertz and/or
        /// decreasing the damping ratio.
        public float contactSpeed;

        /// Maximum linear speed. Usually meters per second.
        public float maximumLinearSpeed;

        /// Optional mixing callback for friction. The default uses sqrt(frictionA * frictionB).
        public b2FrictionCallback frictionCallback;

        /// Optional mixing callback for restitution. The default uses max(restitutionA, restitutionB).
        public b2RestitutionCallback restitutionCallback;

        /// Can bodies go to sleep to improve performance
        public bool enableSleep;

        /// Enable continuous collision
        public bool enableContinuous;

        /// Number of workers to use with the provided task system. Box2D performs best when using only
        /// performance cores and accessing a single L2 cache. Efficiency cores and hyper-threading provide
        /// little benefit and may even harm performance.
        /// @note Box2D does not create threads. This is the number of threads your applications has created
        /// that you are allocating to b2World_Step.
        /// @warning Do not modify the default value unless you are also providing a task system and providing
        /// task callbacks (enqueueTask and finishTask).
        public int workerCount;

        /// Function to spawn tasks
        public b2EnqueueTaskCallback enqueueTask;

        /// Function to finish a task
        public b2FinishTaskCallback finishTask;

        /// User context that is provided to enqueueTask and finishTask
        public object userTaskContext;

        /// User data
        public object userData;

        /// Used internally to detect a valid definition. DO NOT SET.
        public int internalValue;
    }
}