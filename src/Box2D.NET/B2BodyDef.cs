// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// A body definition holds all the data needed to construct a rigid body.
    /// You can safely re-use body definitions. Shapes are added to a body after construction.
    /// Body definitions are temporary objects used to bundle creation parameters.
    /// Must be initialized using b2DefaultBodyDef().
    /// @ingroup body
    public struct B2BodyDef
    {
        /// The body type: static, kinematic, or dynamic.
        public B2BodyType type;

        /// The initial world position of the body. Bodies should be created with the desired position.
        /// @note Creating bodies at the origin and then moving them nearly doubles the cost of body creation, especially
        /// if the body is moved after shapes have been added.
        public B2Vec2 position;

        /// The initial world rotation of the body. Use b2MakeRot() if you have an angle.
        public B2Rot rotation;

        /// The initial linear velocity of the body's origin. Usually in meters per second.
        public B2Vec2 linearVelocity;

        /// The initial angular velocity of the body. Radians per second.
        public float angularVelocity;

        /// Linear damping is used to reduce the linear velocity. The damping parameter
        /// can be larger than 1 but the damping effect becomes sensitive to the
        /// time step when the damping parameter is large.
        /// Generally linear damping is undesirable because it makes objects move slowly
        /// as if they are floating.
        public float linearDamping;

        /// Angular damping is used to reduce the angular velocity. The damping parameter
        /// can be larger than 1.0f but the damping effect becomes sensitive to the
        /// time step when the damping parameter is large.
        /// Angular damping can be use slow down rotating bodies.
        public float angularDamping;

        /// Scale the gravity applied to this body. Non-dimensional.
        public float gravityScale;

        /// Sleep speed threshold, default is 0.05 meters per second
        public float sleepThreshold;

        /// Optional body name for debugging. Up to 31 characters (excluding null termination)
        public string name;

        /// Use this to store application specific body data.
        public object userData;
        
        /// Motions locks to restrict linear and angular movement.
        /// Caution: may lead to softer constraints along the locked direction
        public B2MotionLocks motionLocks;

        /// Set this flag to false if this body should never fall asleep.
        public bool enableSleep;

        /// Is this body initially awake or sleeping?
        public bool isAwake;

        /// Treat this body as a high speed object that performs continuous collision detection
        /// against dynamic and kinematic bodies, but not other bullet bodies.
        /// @warning Bullets should be used sparingly. They are not a solution for general dynamic-versus-dynamic
        /// continuous collision. They do not guarantee accurate collision if both bodies are fast moving because
        /// the bullet does a continuous check after all non-bullet bodies have moved. You could get unlucky and have
        /// the bullet body end a time step very close to a non-bullet body and the non-bullet body then moves over
        /// the bullet body. In continuous collision, initial overlap is ignored to avoid freezing bodies in place.
        /// I do not recommend using them for game projectiles if precise collision timing is needed. Instead consider
        /// using a ray or shape cast. You can use a marching ray or shape cast for projectile that moves over time.
        /// If you want a fast moving projectile to collide with a fast moving target, you need to consider the relative
        /// movement in your ray or shape cast. This is out of the scope of Box2D.
        /// So what are good use cases for bullets? Pinball games or games with dynamic containers that hold other objects.
        /// It should be a use case where it doesn't break the game if there is a collision missed, but the having them
        /// captured improves the quality of the game.
        public bool isBullet;
        
        /// Used to disable a body. A disabled body does not move or collide.
        public bool isEnabled;

        /// This allows this body to bypass rotational speed limits. Should only be used
        /// for circular objects, like wheels.
        public bool allowFastRotation;

        /// Used internally to detect a valid definition. DO NOT SET.
        public int internalValue;
    }
}
