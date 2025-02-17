// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using static Box2D.NET.constants;
using static Box2D.NET.core;
using static Box2D.NET.math_function;

namespace Box2D.NET;

/// Task interface
/// This is prototype for a Box2D task. Your task system is expected to invoke the Box2D task with these arguments.
/// The task spans a range of the parallel-for: [startIndex, endIndex)
/// The worker index must correctly identify each worker in the user thread pool, expected in [0, workerCount).
/// A worker must only exist on only one thread at a time and is analogous to the thread index.
/// The task context is the context pointer sent from Box2D when it is enqueued.
/// The startIndex and endIndex are expected in the range [0, itemCount) where itemCount is the argument to b2EnqueueTaskCallback
/// below. Box2D expects startIndex < endIndex and will execute a loop like this:
///
/// @code{.c}
/// for (int i = startIndex; i < endIndex; ++i)
/// {
/// 	DoWork();
/// }
/// @endcode
/// @ingroup world
public delegate void b2TaskCallback(int startIndex, int endIndex, uint workerIndex, object taskContext);

/// These functions can be provided to Box2D to invoke a task system. These are designed to work well with enkiTS.
/// Returns a pointer to the user's task object. May be nullptr. A nullptr indicates to Box2D that the work was executed
/// serially within the callback and there is no need to call b2FinishTaskCallback.
/// The itemCount is the number of Box2D work items that are to be partitioned among workers by the user's task system.
/// This is essentially a parallel-for. The minRange parameter is a suggestion of the minimum number of items to assign
/// per worker to reduce overhead. For example, suppose the task is small and that itemCount is 16. A minRange of 8 suggests
/// that your task system should split the work items among just two workers, even if you have more available.
/// In general the range [startIndex, endIndex) send to b2TaskCallback should obey:
/// endIndex - startIndex >= minRange
/// The exception of course is when itemCount < minRange.
/// @ingroup world
public delegate object b2EnqueueTaskCallback(b2TaskCallback task, int itemCount, int minRange, object taskContext, object userContext);

/// Finishes a user task object that wraps a Box2D task.
/// @ingroup world
public delegate void b2FinishTaskCallback(object userTask, object userContext);

/// Optional friction mixing callback. This intentionally provides no context objects because this is called
/// from a worker thread.
/// @warning This function should not attempt to modify Box2D state or user application state.
public delegate float b2FrictionCallback(float frictionA, int materialA, float frictionB, int materialB);

/// Optional restitution mixing callback. This intentionally provides no context objects because this is called
/// from a worker thread.
/// @warning This function should not attempt to modify Box2D state or user application state.
public delegate float b2RestitutionCallback(float restitutionA, int materialA, float restitutionB, int materialB);

/// Prototype for a contact filter callback.
/// This is called when a contact pair is considered for collision. This allows you to
/// perform custom logic to prevent collision between shapes. This is only called if
/// one of the two shapes has custom filtering enabled.
/// Notes:
/// - this function must be thread-safe
/// - this is only called if one of the two shapes has enabled custom filtering
/// - this is called only for awake dynamic bodies
/// Return false if you want to disable the collision
/// @see b2ShapeDef
/// @warning Do not attempt to modify the world inside this callback
/// @ingroup world
public delegate bool b2CustomFilterFcn(b2ShapeId shapeIdA, b2ShapeId shapeIdB, object context);

/// Prototype for a pre-solve callback.
/// This is called after a contact is updated. This allows you to inspect a
/// contact before it goes to the solver. If you are careful, you can modify the
/// contact manifold (e.g. modify the normal).
/// Notes:
/// - this function must be thread-safe
/// - this is only called if the shape has enabled pre-solve events
/// - this is called only for awake dynamic bodies
/// - this is not called for sensors
/// - the supplied manifold has impulse values from the previous step
/// Return false if you want to disable the contact this step
/// @warning Do not attempt to modify the world inside this callback
/// @ingroup world
public delegate bool b2PreSolveFcn(b2ShapeId shapeIdA, b2ShapeId shapeIdB, ref b2Manifold manifold, object context);

/// Prototype callback for overlap queries.
/// Called for each shape found in the query.
/// @see b2World_OverlapABB
/// @return false to terminate the query.
/// @ingroup world
public delegate bool b2OverlapResultFcn(b2ShapeId shapeId, object context);

/// Prototype callback for ray casts.
/// Called for each shape found in the query. You control how the ray cast
/// proceeds by returning a float:
/// return -1: ignore this shape and continue
/// return 0: terminate the ray cast
/// return fraction: clip the ray to this point
/// return 1: don't clip the ray and continue
/// @param shapeId the shape hit by the ray
/// @param point the point of initial intersection
/// @param normal the normal vector at the point of intersection
/// @param fraction the fraction along the ray at the point of intersection
/// @param context the user context
/// @return -1 to filter, 0 to terminate, fraction to clip the ray for closest hit, 1 to continue
/// @see b2World_CastRay
/// @ingroup world
public delegate float b2CastResultFcn(b2ShapeId shapeId, b2Vec2 point, b2Vec2 normal, float fraction, object context);

/// Result from b2World_RayCastClosest
/// @ingroup world
public class b2RayResult
{
    public b2ShapeId shapeId;
    public b2Vec2 point;
    public b2Vec2 normal;
    public float fraction;
    public int nodeVisits;
    public int leafVisits;
    public bool hit;
}

/// World definition used to create a simulation world.
/// Must be initialized using b2DefaultWorldDef().
/// @ingroup world
public class b2WorldDef
{
    /// Gravity vector. Box2D has no up-vector defined.
    public b2Vec2 gravity;

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
    public float contactPushMaxSpeed;

    /// Joint stiffness. Cycles per second.
    public float jointHertz;

    /// Joint bounciness. Non-dimensional.
    public float jointDampingRatio;

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

/// The body simulation type.
/// Each body is one of these three types. The type determines how the body behaves in the simulation.
/// @ingroup body
public enum b2BodyType
{
    /// zero mass, zero velocity, may be manually moved
    b2_staticBody = 0,

    /// zero mass, velocity set by user, moved by solver
    b2_kinematicBody = 1,

    /// positive mass, velocity determined by forces, moved by solver
    b2_dynamicBody = 2,

    /// number of body types
    b2_bodyTypeCount,
};

/// A body definition holds all the data needed to construct a rigid body.
/// You can safely re-use body definitions. Shapes are added to a body after construction.
/// Body definitions are temporary objects used to bundle creation parameters.
/// Must be initialized using b2DefaultBodyDef().
/// @ingroup body
public class b2BodyDef
{
    /// The body type: static, kinematic, or dynamic.
    public b2BodyType type;

    /// The initial world position of the body. Bodies should be created with the desired position.
    /// @note Creating bodies at the origin and then moving them nearly doubles the cost of body creation, especially
    /// if the body is moved after shapes have been added.
    public b2Vec2 position;

    /// The initial world rotation of the body. Use b2MakeRot() if you have an angle.
    public b2Rot rotation;

    /// The initial linear velocity of the body's origin. Usually in meters per second.
    public b2Vec2 linearVelocity;

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

    /// Set this flag to false if this body should never fall asleep.
    public bool enableSleep;

    /// Is this body initially awake or sleeping?
    public bool isAwake;

    /// Should this body be prevented from rotating? Useful for characters.
    public bool fixedRotation;

    /// Treat this body as high speed object that performs continuous collision detection
    /// against dynamic and kinematic bodies, but not other bullet bodies.
    /// @warning Bullets should be used sparingly. They are not a solution for general dynamic-versus-dynamic
    /// continuous collision. They may interfere with joint constraints.
    public bool isBullet;

    /// Used to disable a body. A disabled body does not move or collide.
    public bool isEnabled;

    /// This allows this body to bypass rotational speed limits. Should only be used
    /// for circular objects, like wheels.
    public bool allowFastRotation;

    /// Used internally to detect a valid definition. DO NOT SET.
    public int internalValue;
}

/// This is used to filter collision on shapes. It affects shape-vs-shape collision
/// and shape-versus-query collision (such as b2World_CastRay).
/// @ingroup shape
public struct b2Filter
{
    /// The collision category bits. Normally you would just set one bit. The category bits should
    /// represent your application object types. For example:
    /// @code{.cpp}
    /// enum MyCategories
    /// {
    ///    Static  = 0x00000001,
    ///    Dynamic = 0x00000002,
    ///    Debris  = 0x00000004,
    ///    Player  = 0x00000008,
    ///    // etc
    /// };
    /// @endcode
    public ulong categoryBits;

    /// The collision mask bits. This states the categories that this
    /// shape would accept for collision.
    /// For example, you may want your player to only collide with static objects
    /// and other players.
    /// @code{.c}
    /// maskBits = Static | Player;
    /// @endcode
    public ulong maskBits;

    /// Collision groups allow a certain group of objects to never collide (negative)
    /// or always collide (positive). A group index of zero has no effect. Non-zero group filtering
    /// always wins against the mask bits.
    /// For example, you may want ragdolls to collide with other ragdolls but you don't want
    /// ragdoll self-collision. In this case you would give each ragdoll a unique negative group index
    /// and apply that group index to all shapes on the ragdoll.
    public int groupIndex;

    public b2Filter(ulong categoryBits, ulong maskBits, int groupIndex)
    {
        this.categoryBits = categoryBits;
        this.maskBits = maskBits;
        this.groupIndex = groupIndex;
    }
}

/// The query filter is used to filter collisions between queries and shapes. For example,
/// you may want a ray-cast representing a projectile to hit players and the static environment
/// but not debris.
/// @ingroup shape
public struct b2QueryFilter
{
    /// The collision category bits of this query. Normally you would just set one bit.
    public ulong categoryBits;

    /// The collision mask bits. This states the shape categories that this
    /// query would accept for collision.
    public ulong maskBits;

    public b2QueryFilter(ulong categoryBits, ulong maskBits)
    {
        this.categoryBits = categoryBits;
        this.maskBits = maskBits;
    }
}

/// Shape type
/// @ingroup shape
public enum b2ShapeType
{
    /// A circle with an offset
    b2_circleShape,

    /// A capsule is an extruded circle
    b2_capsuleShape,

    /// A line segment
    b2_segmentShape,

    /// A convex polygon
    b2_polygonShape,

    /// A line segment owned by a chain shape
    b2_chainSegmentShape,

    /// The number of shape types
    b2_shapeTypeCount
}

/// Used to create a shape.
/// This is a temporary object used to bundle shape creation parameters. You may use
/// the same shape definition to create multiple shapes.
/// Must be initialized using b2DefaultShapeDef().
/// @ingroup shape
public class b2ShapeDef
{
    /// Use this to store application specific shape data.
    public object userData;

    /// The Coulomb (dry) friction coefficient, usually in the range [0,1].
    public float friction;

    /// The coefficient of restitution (bounce) usually in the range [0,1].
    /// https://en.wikipedia.org/wiki/Coefficient_of_restitution
    public float restitution;

    /// The rolling resistance usually in the range [0,1].
    public float rollingResistance;

    /// The tangent speed for conveyor belts
    public float tangentSpeed;

    /// User material identifier. This is passed with query results and to friction and restitution
    /// combining functions. It is not used internally.
    public int material;

    /// The density, usually in kg/m^2.
    public float density;

    /// Collision filtering data.
    public b2Filter filter;

    /// Custom debug draw color.
    public uint customColor;

    /// A sensor shape generates overlap events but never generates a collision response.
    /// Sensors do not collide with other sensors and do not have continuous collision.
    /// Instead, use a ray or shape cast for those scenarios.
    public bool isSensor;

    /// Enable contact events for this shape. Only applies to kinematic and dynamic bodies. Ignored for sensors.
    public bool enableContactEvents;

    /// Enable hit events for this shape. Only applies to kinematic and dynamic bodies. Ignored for sensors.
    public bool enableHitEvents;

    /// Enable pre-solve contact events for this shape. Only applies to dynamic bodies. These are expensive
    /// and must be carefully handled due to threading. Ignored for sensors.
    public bool enablePreSolveEvents;

    /// Normally shapes on static bodies don't invoke contact creation when they are added to the world. This overrides
    /// that behavior and causes contact creation. This significantly slows down static body creation which can be important
    /// when there are many static shapes.
    /// This is implicitly always true for sensors, dynamic bodies, and kinematic bodies.
    public bool invokeContactCreation;

    /// Should the body update the mass properties when this shape is created. Default is true.
    public bool updateBodyMass;

    /// Used internally to detect a valid definition. DO NOT SET.
    public int internalValue;
}

/// Surface materials allow chain shapes to have per segment surface properties.
/// @ingroup shape
public class b2SurfaceMaterial
{
    /// The Coulomb (dry) friction coefficient, usually in the range [0,1].
    public float friction;

    /// The coefficient of restitution (bounce) usually in the range [0,1].
    /// https://en.wikipedia.org/wiki/Coefficient_of_restitution
    public float restitution;

    /// The rolling resistance usually in the range [0,1].
    public float rollingResistance;

    /// The tangent speed for conveyor belts
    public float tangentSpeed;

    /// User material identifier. This is passed with query results and to friction and restitution
    /// combining functions. It is not used internally.
    public int material;

    /// Custom debug draw color.
    public uint customColor;

    public b2SurfaceMaterial Clone()
    {
        Debug.Assert(false);
        return null;
    }
}

/// Used to create a chain of line segments. This is designed to eliminate ghost collisions with some limitations.
/// - chains are one-sided
/// - chains have no mass and should be used on static bodies
/// - chains have a counter-clockwise winding order
/// - chains are either a loop or open
/// - a chain must have at least 4 points
/// - the distance between any two points must be greater than B2_LINEAR_SLOP
/// - a chain shape should not self intersect (this is not validated)
/// - an open chain shape has NO COLLISION on the first and final edge
/// - you may overlap two open chains on their first three and/or last three points to get smooth collision
/// - a chain shape creates multiple line segment shapes on the body
/// https://en.wikipedia.org/wiki/Polygonal_chain
/// Must be initialized using b2DefaultChainDef().
/// @warning Do not use chain shapes unless you understand the limitations. This is an advanced feature.
/// @ingroup shape
public class b2ChainDef
{
    /// Use this to store application specific shape data.
    public object userData;

    /// An array of at least 4 points. These are cloned and may be temporary.
    public b2Vec2[] points;

    /// The point count, must be 4 or more.
    public int count;

    /// Surface materials for each segment. These are cloned.
    public b2SurfaceMaterial[] materials;

    /// The material count. Must be 1 or count. This allows you to provide one
    /// material for all segments or a unique material per segment.
    public int materialCount;

    /// Contact filtering data.
    public b2Filter filter;

    /// Indicates a closed chain formed by connecting the first and last points
    public bool isLoop;

    /// Used internally to detect a valid definition. DO NOT SET.
    public int internalValue;
}

//! @cond
/// Profiling data. Times are in milliseconds.
public class b2Profile
{
    public float step;
    public float pairs;
    public float collide;
    public float solve;
    public float mergeIslands;
    public float prepareStages;
    public float solveConstraints;
    public float prepareConstraints;
    public float integrateVelocities;
    public float warmStart;
    public float solveImpulses;
    public float integratePositions;
    public float relaxImpulses;
    public float applyRestitution;
    public float storeImpulses;
    public float splitIslands;
    public float transforms;
    public float hitEvents;
    public float refit;
    public float bullets;
    public float sleepIslands;
    public float sensors;

    public void Clear()
    {
        step  = 0;
        pairs  = 0;
        collide  = 0;
        solve  = 0;
        mergeIslands  = 0;
        prepareStages  = 0;
        solveConstraints  = 0;
        prepareConstraints  = 0;
        integrateVelocities  = 0;
        warmStart  = 0;
        solveImpulses  = 0;
        integratePositions  = 0;
        relaxImpulses  = 0;
        applyRestitution  = 0;
        storeImpulses  = 0;
        splitIslands  = 0;
        transforms  = 0;
        hitEvents  = 0;
        refit  = 0;
        bullets  = 0;
        sleepIslands  = 0;
        sensors  = 0;
    }
}

/// Counters that give details of the simulation size.
public class b2Counters
{
    public int bodyCount;
    public int shapeCount;
    public int contactCount;
    public int jointCount;
    public int islandCount;
    public int stackUsed;
    public int staticTreeHeight;
    public int treeHeight;
    public int byteCount;
    public int taskCount;
    public readonly int[] colorCounts = new int[12];
}

/// Joint type enumeration
///
/// This is useful because all joint types use b2JointId and sometimes you
/// want to get the type of a joint.
/// @ingroup joint
public enum b2JointType
{
    b2_distanceJoint,
    b2_motorJoint,
    b2_mouseJoint,
    b2_nullJoint,
    b2_prismaticJoint,
    b2_revoluteJoint,
    b2_weldJoint,
    b2_wheelJoint,
}

/// Distance joint definition
///
/// This requires defining an anchor point on both
/// bodies and the non-zero distance of the distance joint. The definition uses
/// local anchor points so that the initial configuration can violate the
/// constraint slightly. This helps when saving and loading a game.
/// @ingroup distance_joint
public class b2DistanceJointDef
{
    /// The first attached body
    public b2BodyId bodyIdA;

    /// The second attached body
    public b2BodyId bodyIdB;

    /// The local anchor point relative to bodyA's origin
    public b2Vec2 localAnchorA;

    /// The local anchor point relative to bodyB's origin
    public b2Vec2 localAnchorB;

    /// The rest length of this joint. Clamped to a stable minimum value.
    public float length;

    /// Enable the distance constraint to behave like a spring. If false
    /// then the distance joint will be rigid, overriding the limit and motor.
    public bool enableSpring;

    /// The spring linear stiffness Hertz, cycles per second
    public float hertz;

    /// The spring linear damping ratio, non-dimensional
    public float dampingRatio;

    /// Enable/disable the joint limit
    public bool enableLimit;

    /// Minimum length. Clamped to a stable minimum value.
    public float minLength;

    /// Maximum length. Must be greater than or equal to the minimum length.
    public float maxLength;

    /// Enable/disable the joint motor
    public bool enableMotor;

    /// The maximum motor force, usually in newtons
    public float maxMotorForce;

    /// The desired motor speed, usually in meters per second
    public float motorSpeed;

    /// Set this flag to true if the attached bodies should collide
    public bool collideConnected;

    /// User data pointer
    public object userData;

    /// Used internally to detect a valid definition. DO NOT SET.
    public int internalValue;
}

/// A motor joint is used to control the relative motion between two bodies
///
/// A typical usage is to control the movement of a dynamic body with respect to the ground.
/// @ingroup motor_joint
public class b2MotorJointDef
{
    /// The first attached body
    public b2BodyId bodyIdA;

    /// The second attached body
    public b2BodyId bodyIdB;

    /// Position of bodyB minus the position of bodyA, in bodyA's frame
    public b2Vec2 linearOffset;

    /// The bodyB angle minus bodyA angle in radians
    public float angularOffset;

    /// The maximum motor force in newtons
    public float maxForce;

    /// The maximum motor torque in newton-meters
    public float maxTorque;

    /// Position correction factor in the range [0,1]
    public float correctionFactor;

    /// Set this flag to true if the attached bodies should collide
    public bool collideConnected;

    /// User data pointer
    public object userData;

    /// Used internally to detect a valid definition. DO NOT SET.
    public int internalValue;
}

/// A mouse joint is used to make a point on a body track a specified world point.
///
/// This a soft constraint and allows the constraint to stretch without
/// applying huge forces. This also applies rotation constraint heuristic to improve control.
/// @ingroup mouse_joint
public class b2MouseJointDef
{
    /// The first attached body. This is assumed to be static.
    public b2BodyId bodyIdA;

    /// The second attached body.
    public b2BodyId bodyIdB;

    /// The initial target point in world space
    public b2Vec2 target;

    /// Stiffness in hertz
    public float hertz;

    /// Damping ratio, non-dimensional
    public float dampingRatio;

    /// Maximum force, typically in newtons
    public float maxForce;

    /// Set this flag to true if the attached bodies should collide.
    public bool collideConnected;

    /// User data pointer
    public object userData;

    /// Used internally to detect a valid definition. DO NOT SET.
    public int internalValue;
}

/// A null joint is used to disable collision between two specific bodies.
///
/// @ingroup null_joint
public class b2NullJointDef
{
    /// The first attached body.
    public b2BodyId bodyIdA;

    /// The second attached body.
    public b2BodyId bodyIdB;

    /// User data pointer
    public object userData;

    /// Used internally to detect a valid definition. DO NOT SET.
    public int internalValue;
}

/// Prismatic joint definition
///
/// This requires defining a line of motion using an axis and an anchor point.
/// The definition uses local anchor points and a local axis so that the initial
/// configuration can violate the constraint slightly. The joint translation is zero
/// when the local anchor points coincide in world space.
/// @ingroup prismatic_joint
public class b2PrismaticJointDef
{
    /// The first attached body
    public b2BodyId bodyIdA;

    /// The second attached body
    public b2BodyId bodyIdB;

    /// The local anchor point relative to bodyA's origin
    public b2Vec2 localAnchorA;

    /// The local anchor point relative to bodyB's origin
    public b2Vec2 localAnchorB;

    /// The local translation unit axis in bodyA
    public b2Vec2 localAxisA;

    /// The constrained angle between the bodies: bodyB_angle - bodyA_angle
    public float referenceAngle;

    /// Enable a linear spring along the prismatic joint axis
    public bool enableSpring;

    /// The spring stiffness Hertz, cycles per second
    public float hertz;

    /// The spring damping ratio, non-dimensional
    public float dampingRatio;

    /// Enable/disable the joint limit
    public bool enableLimit;

    /// The lower translation limit
    public float lowerTranslation;

    /// The upper translation limit
    public float upperTranslation;

    /// Enable/disable the joint motor
    public bool enableMotor;

    /// The maximum motor force, typically in newtons
    public float maxMotorForce;

    /// The desired motor speed, typically in meters per second
    public float motorSpeed;

    /// Set this flag to true if the attached bodies should collide
    public bool collideConnected;

    /// User data pointer
    public object userData;

    /// Used internally to detect a valid definition. DO NOT SET.
    public int internalValue;
}

/// Revolute joint definition
///
/// This requires defining an anchor point where the bodies are joined.
/// The definition uses local anchor points so that the
/// initial configuration can violate the constraint slightly. You also need to
/// specify the initial relative angle for joint limits. This helps when saving
/// and loading a game.
/// The local anchor points are measured from the body's origin
/// rather than the center of mass because:
/// 1. you might not know where the center of mass will be
/// 2. if you add/remove shapes from a body and recompute the mass, the joints will be broken
/// @ingroup revolute_joint
public class b2RevoluteJointDef
{
    /// The first attached body
    public b2BodyId bodyIdA;

    /// The second attached body
    public b2BodyId bodyIdB;

    /// The local anchor point relative to bodyA's origin
    public b2Vec2 localAnchorA;

    /// The local anchor point relative to bodyB's origin
    public b2Vec2 localAnchorB;

    /// The bodyB angle minus bodyA angle in the reference state (radians).
    /// This defines the zero angle for the joint limit.
    public float referenceAngle;

    /// Enable a rotational spring on the revolute hinge axis
    public bool enableSpring;

    /// The spring stiffness Hertz, cycles per second
    public float hertz;

    /// The spring damping ratio, non-dimensional
    public float dampingRatio;

    /// A flag to enable joint limits
    public bool enableLimit;

    /// The lower angle for the joint limit in radians
    public float lowerAngle;

    /// The upper angle for the joint limit in radians
    public float upperAngle;

    /// A flag to enable the joint motor
    public bool enableMotor;

    /// The maximum motor torque, typically in newton-meters
    public float maxMotorTorque;

    /// The desired motor speed in radians per second
    public float motorSpeed;

    /// Scale the debug draw
    public float drawSize;

    /// Set this flag to true if the attached bodies should collide
    public bool collideConnected;

    /// User data pointer
    public object userData;

    /// Used internally to detect a valid definition. DO NOT SET.
    public int internalValue;
}

/// Weld joint definition
///
/// A weld joint connect to bodies together rigidly. This constraint provides springs to mimic
/// soft-body simulation.
/// @note The approximate solver in Box2D cannot hold many bodies together rigidly
/// @ingroup weld_joint
public class b2WeldJointDef
{
    /// The first attached body
    public b2BodyId bodyIdA;

    /// The second attached body
    public b2BodyId bodyIdB;

    /// The local anchor point relative to bodyA's origin
    public b2Vec2 localAnchorA;

    /// The local anchor point relative to bodyB's origin
    public b2Vec2 localAnchorB;

    /// The bodyB angle minus bodyA angle in the reference state (radians)
    public float referenceAngle;

    /// Linear stiffness expressed as Hertz (cycles per second). Use zero for maximum stiffness.
    public float linearHertz;

    /// Angular stiffness as Hertz (cycles per second). Use zero for maximum stiffness.
    public float angularHertz;

    /// Linear damping ratio, non-dimensional. Use 1 for critical damping.
    public float linearDampingRatio;

    /// Linear damping ratio, non-dimensional. Use 1 for critical damping.
    public float angularDampingRatio;

    /// Set this flag to true if the attached bodies should collide
    public bool collideConnected;

    /// User data pointer
    public object userData;

    /// Used internally to detect a valid definition. DO NOT SET.
    public int internalValue;
}

/// Wheel joint definition
///
/// This requires defining a line of motion using an axis and an anchor point.
/// The definition uses local  anchor points and a local axis so that the initial
/// configuration can violate the constraint slightly. The joint translation is zero
/// when the local anchor points coincide in world space.
/// @ingroup wheel_joint
public class b2WheelJointDef
{
    /// The first attached body
    public b2BodyId bodyIdA;

    /// The second attached body
    public b2BodyId bodyIdB;

    /// The local anchor point relative to bodyA's origin
    public b2Vec2 localAnchorA;

    /// The local anchor point relative to bodyB's origin
    public b2Vec2 localAnchorB;

    /// The local translation unit axis in bodyA
    public b2Vec2 localAxisA;

    /// Enable a linear spring along the local axis
    public bool enableSpring;

    /// Spring stiffness in Hertz
    public float hertz;

    /// Spring damping ratio, non-dimensional
    public float dampingRatio;

    /// Enable/disable the joint linear limit
    public bool enableLimit;

    /// The lower translation limit
    public float lowerTranslation;

    /// The upper translation limit
    public float upperTranslation;

    /// Enable/disable the joint rotational motor
    public bool enableMotor;

    /// The maximum motor torque, typically in newton-meters
    public float maxMotorTorque;

    /// The desired motor speed in radians per second
    public float motorSpeed;

    /// Set this flag to true if the attached bodies should collide
    public bool collideConnected;

    /// User data pointer
    public object userData;

    /// Used internally to detect a valid definition. DO NOT SET.
    public int internalValue;
}

/// The explosion definition is used to configure options for explosions. Explosions
/// consider shape geometry when computing the impulse.
/// @ingroup world
public class b2ExplosionDef
{
    /// Mask bits to filter shapes
    public ulong maskBits;

    /// The center of the explosion in world space
    public b2Vec2 position;

    /// The radius of the explosion
    public float radius;

    /// The falloff distance beyond the radius. Impulse is reduced to zero at this distance.
    public float falloff;

    /// Impulse per unit length. This applies an impulse according to the shape perimeter that
    /// is facing the explosion. Explosions only apply to circles, capsules, and polygons. This
    /// may be negative for implosions.
    public float impulsePerLength;
}

/**
 * @defgroup events Events
 * World event types.
 *
 * Events are used to collect events that occur during the world time step. These events
 * are then available to query after the time step is complete. This is preferable to callbacks
 * because Box2D uses multithreaded simulation.
 *
 * Also when events occur in the simulation step it may be problematic to modify the world, which is
 * often what applications want to do when events occur.
 *
 * With event arrays, you can scan the events in a loop and modify the world. However, you need to be careful
 * that some event data may become invalid. There are several samples that show how to do this safely.
 *
 * @{
 */
/// A begin touch event is generated when a shape starts to overlap a sensor shape.
public class b2SensorBeginTouchEvent
{
    /// The id of the sensor shape
    public b2ShapeId sensorShapeId;

    /// The id of the dynamic shape that began touching the sensor shape
    public b2ShapeId visitorShapeId;
    
    public b2SensorBeginTouchEvent()
    {
        
    }

    public b2SensorBeginTouchEvent(b2ShapeId sensorShapeId, b2ShapeId visitorShapeId)
    {
        this.sensorShapeId = sensorShapeId;
        this.visitorShapeId = visitorShapeId;
    }

}

/// An end touch event is generated when a shape stops overlapping a sensor shape.
///	These include things like setting the transform, destroying a body or shape, or changing
///	a filter. You will also get an end event if the sensor or visitor are destroyed.
///	Therefore you should always confirm the shape id is valid using b2Shape_IsValid.
public class b2SensorEndTouchEvent
{
    /// The id of the sensor shape
    ///	@warning this shape may have been destroyed
    ///	@see b2Shape_IsValid
    public b2ShapeId sensorShapeId;

    /// The id of the dynamic shape that stopped touching the sensor shape
    ///	@warning this shape may have been destroyed
    ///	@see b2Shape_IsValid
    public b2ShapeId visitorShapeId;

    public b2SensorEndTouchEvent()
    {
        
    }

    public b2SensorEndTouchEvent(b2ShapeId sensorShapeId, b2ShapeId visitorShapeId)
    {
        this.sensorShapeId = sensorShapeId;
        this.visitorShapeId = visitorShapeId;
    }
}

/// Sensor events are buffered in the Box2D world and are available
/// as begin/end overlap event arrays after the time step is complete.
/// Note: these may become invalid if bodies and/or shapes are destroyed
public struct b2SensorEvents
{
    /// Array of sensor begin touch events
    public b2SensorBeginTouchEvent[] beginEvents;

    /// Array of sensor end touch events
    public b2SensorEndTouchEvent[] endEvents;

    /// The number of begin touch events
    public int beginCount;

    /// The number of end touch events
    public int endCount;
}

/// A begin touch event is generated when two shapes begin touching.
public class b2ContactBeginTouchEvent
{
    /// Id of the first shape
    public b2ShapeId shapeIdA;

    /// Id of the second shape
    public b2ShapeId shapeIdB;

    /// The initial contact manifold. This is recorded before the solver is called,
    /// so all the impulses will be zero.
    public b2Manifold manifold;

    public b2ContactBeginTouchEvent()
    {
        
    }

    public b2ContactBeginTouchEvent(b2ShapeId shapeIdA, b2ShapeId shapeIdB, ref b2Manifold manifold)
    {
        this.shapeIdA = shapeIdA;
        this.shapeIdB = shapeIdB;
        this.manifold = manifold;
    }
}

/// An end touch event is generated when two shapes stop touching.
///	You will get an end event if you do anything that destroys contacts previous to the last
///	world step. These include things like setting the transform, destroying a body
///	or shape, or changing a filter or body type.
public class b2ContactEndTouchEvent
{
    /// Id of the first shape
    ///	@warning this shape may have been destroyed
    ///	@see b2Shape_IsValid
    public b2ShapeId shapeIdA;

    /// Id of the second shape
    ///	@warning this shape may have been destroyed
    ///	@see b2Shape_IsValid
    public b2ShapeId shapeIdB;

    public b2ContactEndTouchEvent()
    {
        
    }

    public b2ContactEndTouchEvent(b2ShapeId shapeIdA, b2ShapeId shapeIdB)
    {
        this.shapeIdA = shapeIdA;
        this.shapeIdB = shapeIdB;
    }
}

/// A hit touch event is generated when two shapes collide with a speed faster than the hit speed threshold.
public class b2ContactHitEvent
{
    /// Id of the first shape
    public b2ShapeId shapeIdA;

    /// Id of the second shape
    public b2ShapeId shapeIdB;

    /// Point where the shapes hit
    public b2Vec2 point;

    /// Normal vector pointing from shape A to shape B
    public b2Vec2 normal;

    /// The speed the shapes are approaching. Always positive. Typically in meters per second.
    public float approachSpeed;
}

/// Contact events are buffered in the Box2D world and are available
/// as event arrays after the time step is complete.
/// Note: these may become invalid if bodies and/or shapes are destroyed
public struct b2ContactEvents
{
    /// Array of begin touch events
    public b2ContactBeginTouchEvent[] beginEvents;

    /// Array of end touch events
    public b2ContactEndTouchEvent[] endEvents;

    /// Array of hit events
    public b2ContactHitEvent[] hitEvents;

    /// Number of begin touch events
    public int beginCount;

    /// Number of end touch events
    public int endCount;

    /// Number of hit events
    public int hitCount;
}

/// Body move events triggered when a body moves.
/// Triggered when a body moves due to simulation. Not reported for bodies moved by the user.
/// This also has a flag to indicate that the body went to sleep so the application can also
/// sleep that actor/entity/object associated with the body.
/// On the other hand if the flag does not indicate the body went to sleep then the application
/// can treat the actor/entity/object associated with the body as awake.
/// This is an efficient way for an application to update game object transforms rather than
/// calling functions such as b2Body_GetTransform() because this data is delivered as a contiguous array
/// and it is only populated with bodies that have moved.
/// @note If sleeping is disabled all dynamic and kinematic bodies will trigger move events.
public class b2BodyMoveEvent
{
    public b2Transform transform;
    public b2BodyId bodyId;
    public object userData;
    public bool fellAsleep;
}

/// Body events are buffered in the Box2D world and are available
/// as event arrays after the time step is complete.
/// Note: this data becomes invalid if bodies are destroyed
public struct b2BodyEvents
{
    /// Array of move events
    public b2BodyMoveEvent[] moveEvents;

    /// Number of move events
    public int moveCount;

    public b2BodyEvents(b2BodyMoveEvent[] moveEvents, int moveCount)
    {
        this.moveEvents = moveEvents;
        this.moveCount = moveCount;
    }
}

/// The contact data for two shapes. By convention the manifold normal points
/// from shape A to shape B.
/// @see b2Shape_GetContactData() and b2Body_GetContactData()
public class b2ContactData
{
    public b2ShapeId shapeIdA;
    public b2ShapeId shapeIdB;
    public b2Manifold manifold;
}

/**@}*/
/// These colors are used for debug draw and mostly match the named SVG colors.
/// See https://www.rapidtables.com/web/color/index.html
/// https://johndecember.com/html/spec/colorsvg.html
/// https://upload.wikimedia.org/wikipedia/commons/2/2b/SVG_Recognized_color_keyword_names.svg
public enum b2HexColor
{
    b2_colorAliceBlue = 0xF0F8FF,
    b2_colorAntiqueWhite = 0xFAEBD7,
    b2_colorAqua = 0x00FFFF,
    b2_colorAquamarine = 0x7FFFD4,
    b2_colorAzure = 0xF0FFFF,
    b2_colorBeige = 0xF5F5DC,
    b2_colorBisque = 0xFFE4C4,
    b2_colorBlack = 0x000000,
    b2_colorBlanchedAlmond = 0xFFEBCD,
    b2_colorBlue = 0x0000FF,
    b2_colorBlueViolet = 0x8A2BE2,
    b2_colorBrown = 0xA52A2A,
    b2_colorBurlywood = 0xDEB887,
    b2_colorCadetBlue = 0x5F9EA0,
    b2_colorChartreuse = 0x7FFF00,
    b2_colorChocolate = 0xD2691E,
    b2_colorCoral = 0xFF7F50,
    b2_colorCornflowerBlue = 0x6495ED,
    b2_colorCornsilk = 0xFFF8DC,
    b2_colorCrimson = 0xDC143C,
    b2_colorCyan = 0x00FFFF,
    b2_colorDarkBlue = 0x00008B,
    b2_colorDarkCyan = 0x008B8B,
    b2_colorDarkGoldenRod = 0xB8860B,
    b2_colorDarkGray = 0xA9A9A9,
    b2_colorDarkGreen = 0x006400,
    b2_colorDarkKhaki = 0xBDB76B,
    b2_colorDarkMagenta = 0x8B008B,
    b2_colorDarkOliveGreen = 0x556B2F,
    b2_colorDarkOrange = 0xFF8C00,
    b2_colorDarkOrchid = 0x9932CC,
    b2_colorDarkRed = 0x8B0000,
    b2_colorDarkSalmon = 0xE9967A,
    b2_colorDarkSeaGreen = 0x8FBC8F,
    b2_colorDarkSlateBlue = 0x483D8B,
    b2_colorDarkSlateGray = 0x2F4F4F,
    b2_colorDarkTurquoise = 0x00CED1,
    b2_colorDarkViolet = 0x9400D3,
    b2_colorDeepPink = 0xFF1493,
    b2_colorDeepSkyBlue = 0x00BFFF,
    b2_colorDimGray = 0x696969,
    b2_colorDodgerBlue = 0x1E90FF,
    b2_colorFireBrick = 0xB22222,
    b2_colorFloralWhite = 0xFFFAF0,
    b2_colorForestGreen = 0x228B22,
    b2_colorFuchsia = 0xFF00FF,
    b2_colorGainsboro = 0xDCDCDC,
    b2_colorGhostWhite = 0xF8F8FF,
    b2_colorGold = 0xFFD700,
    b2_colorGoldenRod = 0xDAA520,
    b2_colorGray = 0x808080,
    b2_colorGreen = 0x008000,
    b2_colorGreenYellow = 0xADFF2F,
    b2_colorHoneyDew = 0xF0FFF0,
    b2_colorHotPink = 0xFF69B4,
    b2_colorIndianRed = 0xCD5C5C,
    b2_colorIndigo = 0x4B0082,
    b2_colorIvory = 0xFFFFF0,
    b2_colorKhaki = 0xF0E68C,
    b2_colorLavender = 0xE6E6FA,
    b2_colorLavenderBlush = 0xFFF0F5,
    b2_colorLawnGreen = 0x7CFC00,
    b2_colorLemonChiffon = 0xFFFACD,
    b2_colorLightBlue = 0xADD8E6,
    b2_colorLightCoral = 0xF08080,
    b2_colorLightCyan = 0xE0FFFF,
    b2_colorLightGoldenRodYellow = 0xFAFAD2,
    b2_colorLightGray = 0xD3D3D3,
    b2_colorLightGreen = 0x90EE90,
    b2_colorLightPink = 0xFFB6C1,
    b2_colorLightSalmon = 0xFFA07A,
    b2_colorLightSeaGreen = 0x20B2AA,
    b2_colorLightSkyBlue = 0x87CEFA,
    b2_colorLightSlateGray = 0x778899,
    b2_colorLightSteelBlue = 0xB0C4DE,
    b2_colorLightYellow = 0xFFFFE0,
    b2_colorLime = 0x00FF00,
    b2_colorLimeGreen = 0x32CD32,
    b2_colorLinen = 0xFAF0E6,
    b2_colorMagenta = 0xFF00FF,
    b2_colorMaroon = 0x800000,
    b2_colorMediumAquaMarine = 0x66CDAA,
    b2_colorMediumBlue = 0x0000CD,
    b2_colorMediumOrchid = 0xBA55D3,
    b2_colorMediumPurple = 0x9370DB,
    b2_colorMediumSeaGreen = 0x3CB371,
    b2_colorMediumSlateBlue = 0x7B68EE,
    b2_colorMediumSpringGreen = 0x00FA9A,
    b2_colorMediumTurquoise = 0x48D1CC,
    b2_colorMediumVioletRed = 0xC71585,
    b2_colorMidnightBlue = 0x191970,
    b2_colorMintCream = 0xF5FFFA,
    b2_colorMistyRose = 0xFFE4E1,
    b2_colorMoccasin = 0xFFE4B5,
    b2_colorNavajoWhite = 0xFFDEAD,
    b2_colorNavy = 0x000080,
    b2_colorOldLace = 0xFDF5E6,
    b2_colorOlive = 0x808000,
    b2_colorOliveDrab = 0x6B8E23,
    b2_colorOrange = 0xFFA500,
    b2_colorOrangeRed = 0xFF4500,
    b2_colorOrchid = 0xDA70D6,
    b2_colorPaleGoldenRod = 0xEEE8AA,
    b2_colorPaleGreen = 0x98FB98,
    b2_colorPaleTurquoise = 0xAFEEEE,
    b2_colorPaleVioletRed = 0xDB7093,
    b2_colorPapayaWhip = 0xFFEFD5,
    b2_colorPeachPuff = 0xFFDAB9,
    b2_colorPeru = 0xCD853F,
    b2_colorPink = 0xFFC0CB,
    b2_colorPlum = 0xDDA0DD,
    b2_colorPowderBlue = 0xB0E0E6,
    b2_colorPurple = 0x800080,
    b2_colorRebeccaPurple = 0x663399,
    b2_colorRed = 0xFF0000,
    b2_colorRosyBrown = 0xBC8F8F,
    b2_colorRoyalBlue = 0x4169E1,
    b2_colorSaddleBrown = 0x8B4513,
    b2_colorSalmon = 0xFA8072,
    b2_colorSandyBrown = 0xF4A460,
    b2_colorSeaGreen = 0x2E8B57,
    b2_colorSeaShell = 0xFFF5EE,
    b2_colorSienna = 0xA0522D,
    b2_colorSilver = 0xC0C0C0,
    b2_colorSkyBlue = 0x87CEEB,
    b2_colorSlateBlue = 0x6A5ACD,
    b2_colorSlateGray = 0x708090,
    b2_colorSnow = 0xFFFAFA,
    b2_colorSpringGreen = 0x00FF7F,
    b2_colorSteelBlue = 0x4682B4,
    b2_colorTan = 0xD2B48C,
    b2_colorTeal = 0x008080,
    b2_colorThistle = 0xD8BFD8,
    b2_colorTomato = 0xFF6347,
    b2_colorTurquoise = 0x40E0D0,
    b2_colorViolet = 0xEE82EE,
    b2_colorWheat = 0xF5DEB3,
    b2_colorWhite = 0xFFFFFF,
    b2_colorWhiteSmoke = 0xF5F5F5,
    b2_colorYellow = 0xFFFF00,
    b2_colorYellowGreen = 0x9ACD32,

    b2_colorBox2DRed = 0xDC3132,
    b2_colorBox2DBlue = 0x30AEBF,
    b2_colorBox2DGreen = 0x8CC924,
    b2_colorBox2DYellow = 0xFFEE8C
}

/// This struct holds callbacks you can implement to draw a Box2D world.
/// This structure should be zero initialized.
/// @ingroup world
public class b2DebugDraw
{
    /// Draw a closed polygon provided in CCW order.
    //void ( *DrawPolygon )( const b2Vec2* vertices, int vertexCount, b2HexColor color, object context );
    public delegate void DrawPolygonDelegate(ReadOnlySpan<b2Vec2> vertices, int vertexCount, b2HexColor color, object context);

    public DrawPolygonDelegate DrawPolygon;

    /// Draw a solid closed polygon provided in CCW order.
    public delegate void DrawSolidPolygonDelegate(b2Transform transform, ReadOnlySpan<b2Vec2> vertices, int vertexCount, float radius, b2HexColor color, object context);

    public DrawSolidPolygonDelegate DrawSolidPolygon;

    /// Draw a circle.
    public delegate void DrawCircleDelegate(b2Vec2 center, float radius, b2HexColor color, object context);

    public DrawCircleDelegate DrawCircle;

    /// Draw a solid circle.
    public delegate void DrawSolidCircleDelegate(b2Transform transform, float radius, b2HexColor color, object context);

    public DrawSolidCircleDelegate DrawSolidCircle;

    /// Draw a solid capsule.
    public delegate void DrawSolidCapsuleDelegate(b2Vec2 p1, b2Vec2 p2, float radius, b2HexColor color, object context);

    public DrawSolidCapsuleDelegate DrawSolidCapsule;

    /// Draw a line segment.
    public delegate void DrawSegmentDelegate(b2Vec2 p1, b2Vec2 p2, b2HexColor color, object context);

    public DrawSegmentDelegate DrawSegment;

    /// Draw a transform. Choose your own length scale.
    public delegate void DrawTransformDelegate(b2Transform transform, object context);

    public DrawTransformDelegate DrawTransform;

    /// Draw a point.
    public delegate void DrawPointDelegate(b2Vec2 p, float size, b2HexColor color, object context);

    public DrawPointDelegate DrawPoint;

    /// Draw a string in world space
    public delegate void DrawStringDelegate(b2Vec2 p, string s, b2HexColor color, object context);

    public DrawStringDelegate DrawString;

    /// Bounds to use if restricting drawing to a rectangular region
    public b2AABB drawingBounds;

    /// Option to restrict drawing to a rectangular region. May suffer from unstable depth sorting.
    public bool useDrawingBounds;

    /// Option to draw shapes
    public bool drawShapes;

    /// Option to draw joints
    public bool drawJoints;

    /// Option to draw additional information for joints
    public bool drawJointExtras;

    /// Option to draw the bounding boxes for shapes
    public bool drawAABBs;

    /// Option to draw the mass and center of mass of dynamic bodies
    public bool drawMass;

    /// Option to draw body names
    public bool drawBodyNames;

    /// Option to draw contact points
    public bool drawContacts;

    /// Option to visualize the graph coloring used for contacts and joints
    public bool drawGraphColors;

    /// Option to draw contact normals
    public bool drawContactNormals;

    /// Option to draw contact normal impulses
    public bool drawContactImpulses;

    /// Option to draw contact friction impulses
    public bool drawFrictionImpulses;

    /// User context that is passed as an argument to drawing callback functions
    public object context;
}

public static class types
{
    /// Use this to initialize your world definition
    /// @ingroup world
    public static b2WorldDef b2DefaultWorldDef()
    {
        b2WorldDef def = new b2WorldDef();
        def.gravity.x = 0.0f;
        def.gravity.y = -10.0f;
        def.hitEventThreshold = 1.0f * b2_lengthUnitsPerMeter;
        def.restitutionThreshold = 1.0f * b2_lengthUnitsPerMeter;
        def.contactPushMaxSpeed = 3.0f * b2_lengthUnitsPerMeter;
        def.contactHertz = 30.0f;
        def.contactDampingRatio = 10.0f;
        def.jointHertz = 60.0f;
        def.jointDampingRatio = 2.0f;
        // 400 meters per second, faster than the speed of sound
        def.maximumLinearSpeed = 400.0f * b2_lengthUnitsPerMeter;
        def.enableSleep = true;
        def.enableContinuous = true;
        def.internalValue = B2_SECRET_COOKIE;
        return def;
    }

    /// Use this to initialize your body definition
    /// @ingroup body
    public static b2BodyDef b2DefaultBodyDef()
    {
        b2BodyDef def = new b2BodyDef();
        def.type = b2BodyType.b2_staticBody;
        def.rotation = b2Rot_identity;
        def.sleepThreshold = 0.05f * b2_lengthUnitsPerMeter;
        def.gravityScale = 1.0f;
        def.enableSleep = true;
        def.isAwake = true;
        def.isEnabled = true;
        def.internalValue = B2_SECRET_COOKIE;
        return def;
    }

    /// Use this to initialize your filter
    /// @ingroup shape
    public static b2Filter b2DefaultFilter()
    {
        b2Filter filter = new b2Filter(B2_DEFAULT_CATEGORY_BITS, B2_DEFAULT_MASK_BITS, 0);
        return filter;
    }

    /// Use this to initialize your query filter
    /// @ingroup shape
    public static b2QueryFilter b2DefaultQueryFilter()
    {
        b2QueryFilter filter = new b2QueryFilter(B2_DEFAULT_CATEGORY_BITS, B2_DEFAULT_MASK_BITS);
        return filter;
    }

    /// Use this to initialize your shape definition
    /// @ingroup shape
    public static b2ShapeDef b2DefaultShapeDef()
    {
        b2ShapeDef def = new b2ShapeDef();
        def.friction = 0.6f;
        def.density = 1.0f;
        def.filter = b2DefaultFilter();
        def.updateBodyMass = true;
        def.internalValue = B2_SECRET_COOKIE;
        return def;
    }

    /// Use this to initialize your surface material
    /// @ingroup shape
    public static b2SurfaceMaterial b2DefaultSurfaceMaterial()
    {
        b2SurfaceMaterial material = new b2SurfaceMaterial();
        material.friction = 0.6f;

        return material;
    }

    /// Use this to initialize your chain definition
    /// @ingroup shape
    public static b2ChainDef b2DefaultChainDef()
    {
        b2SurfaceMaterial defaultMaterial = new b2SurfaceMaterial();
        defaultMaterial.friction = 0.6f;

        b2ChainDef def = new b2ChainDef();
        def.materials = [defaultMaterial];
        def.materialCount = 1;
        def.filter = b2DefaultFilter();
        def.internalValue = B2_SECRET_COOKIE;
        return def;
    }

    public static void b2EmptyDrawPolygon(ReadOnlySpan<b2Vec2> vertices, int vertexCount, b2HexColor color, object context)
    {
        B2_UNUSED((b2Vec2[])null, vertexCount, color, context);
    }

    public static void b2EmptyDrawSolidPolygon(b2Transform transform, ReadOnlySpan<b2Vec2> vertices, int vertexCount, float radius, b2HexColor color, object context)
    {
        B2_UNUSED(transform, (b2Vec2[])null, vertexCount, radius, color, context);
    }

    public static void b2EmptyDrawCircle(b2Vec2 center, float radius, b2HexColor color, object context)
    {
        B2_UNUSED(center, radius, color, context);
    }

    public static void b2EmptyDrawSolidCircle(b2Transform transform, float radius, b2HexColor color, object context)
    {
        B2_UNUSED(transform, radius, color, context);
    }

    public static void b2EmptyDrawSolidCapsule(b2Vec2 p1, b2Vec2 p2, float radius, b2HexColor color, object context)
    {
        B2_UNUSED(p1, p2, radius, color, context);
    }

    public static void b2EmptyDrawSegment(b2Vec2 p1, b2Vec2 p2, b2HexColor color, object context)
    {
        B2_UNUSED(p1, p2, color, context);
    }

    public static void b2EmptyDrawTransform(b2Transform transform, object context)
    {
        B2_UNUSED(transform, context);
    }

    public static void b2EmptyDrawPoint(b2Vec2 p, float size, b2HexColor color, object context)
    {
        B2_UNUSED(p, size, color, context);
    }

    public static void b2EmptyDrawString(b2Vec2 p, string s, b2HexColor color, object context)
    {
        B2_UNUSED(p, s, color, context);
    }

    /// Use this to initialize your drawing interface. This allows you to implement a sub-set
    /// of the drawing functions.
    public static b2DebugDraw b2DefaultDebugDraw()
    {
        b2DebugDraw draw = new b2DebugDraw();

        // These allow the user to skip some implementations and not hit null exceptions.
        draw.DrawPolygon = b2EmptyDrawPolygon;
        draw.DrawSolidPolygon = b2EmptyDrawSolidPolygon;
        draw.DrawCircle = b2EmptyDrawCircle;
        draw.DrawSolidCircle = b2EmptyDrawSolidCircle;
        draw.DrawSolidCapsule = b2EmptyDrawSolidCapsule;
        draw.DrawSegment = b2EmptyDrawSegment;
        draw.DrawTransform = b2EmptyDrawTransform;
        draw.DrawPoint = b2EmptyDrawPoint;
        draw.DrawString = b2EmptyDrawString;
        return draw;
    }
}