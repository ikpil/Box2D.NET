// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Cores;
using static Box2D.NET.B2MathFunction;

namespace Box2D.NET
{
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
    public delegate bool b2CustomFilterFcn(B2ShapeId shapeIdA, B2ShapeId shapeIdB, object context);

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
    public delegate bool b2PreSolveFcn(B2ShapeId shapeIdA, B2ShapeId shapeIdB, ref B2Manifold manifold, object context);

    /// Prototype callback for overlap queries.
    /// Called for each shape found in the query.
    /// @see b2World_OverlapABB
    /// @return false to terminate the query.
    /// @ingroup world
    public delegate bool b2OverlapResultFcn(B2ShapeId shapeId, object context);

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
    public delegate float b2CastResultFcn(B2ShapeId shapeId, B2Vec2 point, B2Vec2 normal, float fraction, object context);


    public static class B2Types
    {
        /// Use this to initialize your world definition
        /// @ingroup world
        public static B2WorldDef b2DefaultWorldDef()
        {
            B2WorldDef def = new B2WorldDef();
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
        public static B2BodyDef b2DefaultBodyDef()
        {
            B2BodyDef def = new B2BodyDef();
            def.type = B2BodyType.b2_staticBody;
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
        public static B2Filter b2DefaultFilter()
        {
            B2Filter filter = new B2Filter(B2_DEFAULT_CATEGORY_BITS, B2_DEFAULT_MASK_BITS, 0);
            return filter;
        }

        /// Use this to initialize your query filter
        /// @ingroup shape
        public static B2QueryFilter b2DefaultQueryFilter()
        {
            B2QueryFilter filter = new B2QueryFilter(B2_DEFAULT_CATEGORY_BITS, B2_DEFAULT_MASK_BITS);
            return filter;
        }

        /// Use this to initialize your shape definition
        /// @ingroup shape
        public static B2ShapeDef b2DefaultShapeDef()
        {
            B2ShapeDef def = new B2ShapeDef();
            def.friction = 0.6f;
            def.density = 1.0f;
            def.filter = b2DefaultFilter();
            def.updateBodyMass = true;
            def.internalValue = B2_SECRET_COOKIE;
            return def;
        }

        /// Use this to initialize your surface material
        /// @ingroup shape
        public static B2SurfaceMaterial b2DefaultSurfaceMaterial()
        {
            B2SurfaceMaterial material = new B2SurfaceMaterial();
            material.friction = 0.6f;

            return material;
        }
        
        /// Use this to initialize your chain definition
        /// @ingroup shape
        public static B2ChainDef b2DefaultChainDef()
        {
            B2SurfaceMaterial defaultMaterial = new B2SurfaceMaterial();
            defaultMaterial.friction = 0.6f;

            B2ChainDef def = new B2ChainDef();
            def.materials = new B2SurfaceMaterial[] { defaultMaterial };
            def.materialCount = 1;
            def.filter = b2DefaultFilter();
            def.internalValue = B2_SECRET_COOKIE;
            return def;
        }

        public static void b2EmptyDrawPolygon(ReadOnlySpan<B2Vec2> vertices, int vertexCount, B2HexColor color, object context)
        {
            B2_UNUSED((B2Vec2[])null, vertexCount, color, context);
        }

        public static void b2EmptyDrawSolidPolygon(ref B2Transform transform, ReadOnlySpan<B2Vec2> vertices, int vertexCount, float radius, B2HexColor color, object context)
        {
            B2_UNUSED(transform, (B2Vec2[])null, vertexCount, radius, color, context);
        }

        public static void b2EmptyDrawCircle(B2Vec2 center, float radius, B2HexColor color, object context)
        {
            B2_UNUSED(center, radius, color, context);
        }

        public static void b2EmptyDrawSolidCircle(ref B2Transform transform, float radius, B2HexColor color, object context)
        {
            B2_UNUSED(transform, radius, color, context);
        }

        public static void b2EmptyDrawSolidCapsule(B2Vec2 p1, B2Vec2 p2, float radius, B2HexColor color, object context)
        {
            B2_UNUSED(p1, p2, radius, color, context);
        }

        public static void b2EmptyDrawSegment(B2Vec2 p1, B2Vec2 p2, B2HexColor color, object context)
        {
            B2_UNUSED(p1, p2, color, context);
        }

        public static void b2EmptyDrawTransform(B2Transform transform, object context)
        {
            B2_UNUSED(transform, context);
        }

        public static void b2EmptyDrawPoint(B2Vec2 p, float size, B2HexColor color, object context)
        {
            B2_UNUSED(p, size, color, context);
        }

        public static void b2EmptyDrawString(B2Vec2 p, string s, B2HexColor color, object context)
        {
            B2_UNUSED(p, s, color, context);
        }

        /// Use this to initialize your drawing interface. This allows you to implement a sub-set
        /// of the drawing functions.
        public static B2DebugDraw b2DefaultDebugDraw()
        {
            B2DebugDraw draw = new B2DebugDraw();

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
}
