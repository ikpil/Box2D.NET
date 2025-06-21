// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;

namespace Box2D.NET
{
    /// Prototype for user allocation function
    /// @param size the allocation size in bytes
    /// @param alignment the required alignment, guaranteed to be a power of 2
    public delegate byte[] b2AllocFcn(uint size, int alignment);

    /// Prototype for user free function
    /// @param mem the memory previously allocated through `b2AllocFcn`
    public delegate void b2FreeFcn(byte[] mem);

    /// Prototype for the user assert callback. Return 0 to skip the debugger break.
    public delegate int b2AssertFcn(string condition, string fileName, int lineNumber);

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
    /// @ingroup world
    public delegate float b2FrictionCallback(float frictionA, int userMaterialIdA, float frictionB, int userMaterialIdB);

    /// Optional restitution mixing callback. This intentionally provides no context objects because this is called
    /// from a worker thread.
    /// @warning This function should not attempt to modify Box2D state or user application state.
    /// @ingroup world
    public delegate float b2RestitutionCallback(float restitutionA, int userMaterialIdA, float restitutionB, int userMaterialIdB);

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
    public delegate bool b2PreSolveFcn(B2ShapeId shapeIdA, B2ShapeId shapeIdB, B2Vec2 point, B2Vec2 normal, object context);

    /// Prototype callback for overlap queries.
    /// Called for each shape found in the query.
    /// @see b2World_OverlapABB
    /// @return false to terminate the query.
    /// @ingroup world
    public delegate bool b2OverlapResultFcn(B2ShapeId shapeId, object context);

    /// Prototype callback for ray and shape casts.
    /// Called for each shape found in the query. You control how the ray cast
    /// proceeds by returning a float:
    /// return -1: ignore this shape and continue
    /// return 0: terminate the ray cast
    /// return fraction: clip the ray to this point
    /// return 1: don't clip the ray and continue
    /// A cast with initial overlap will return a zero fraction and a zero normal.
    /// @param shapeId the shape hit by the ray
    /// @param point the point of initial intersection
    /// @param normal the normal vector at the point of intersection, zero for a shape cast with initial overlap
    /// @param fraction the fraction along the ray at the point of intersection, zero for a shape cast with initial overlap
    /// @param context the user context
    /// @return -1 to filter, 0 to terminate, fraction to clip the ray for closest hit, 1 to continue
    /// @see b2World_CastRay
    /// @ingroup world
    public delegate float b2CastResultFcn(B2ShapeId shapeId, B2Vec2 point, B2Vec2 normal, float fraction, object context);

    // Used to collect collision planes for character movers.
    // Return true to continue gathering planes.
    public delegate bool b2PlaneResultFcn(B2ShapeId shapeId, ref B2PlaneResult plane, object context);

    // Manifold functions should compute important results in local space to improve precision. However, this
    // interface function takes two world transforms instead of a relative transform for these reasons:
    //
    // First:
    // The anchors need to be computed relative to the shape origin in world space. This is necessary so the
    // solver does not need to access static body transforms. Not even in constraint preparation. This approach
    // has world space vectors yet retains precision.
    //
    // Second:
    // b2ManifoldPoint::point is very useful for debugging and it is in world space.
    //
    // Third:
    // The user may call the manifold functions directly and they should be easy to use and have easy to use
    // results.
    public delegate B2Manifold b2ManifoldFcn(B2Shape shapeA, B2Transform xfA, B2Shape shapeB, B2Transform xfB, ref B2SimplexCache cache);

    /// This function receives proxies found in the AABB query.
    /// @return true if the query should continue
    public delegate bool b2TreeQueryCallbackFcn<T>(int proxyId, ulong userData, ref T context);

    /// This function receives clipped ray cast input for a proxy. The function
    /// returns the new ray fraction.
    /// - return a value of 0 to terminate the ray cast
    /// - return a value less than input->maxFraction to clip the ray
    /// - return a value of input->maxFraction to continue the ray cast without clipping
    public delegate float b2TreeRayCastCallbackFcn<T>(ref B2RayCastInput input, int proxyId, ulong userData, ref T context) where T : struct;

    /// This function receives clipped ray cast input for a proxy. The function
    /// returns the new ray fraction.
    /// - return a value of 0 to terminate the ray cast
    /// - return a value less than input->maxFraction to clip the ray
    /// - return a value of input->maxFraction to continue the ray cast without clipping
    public delegate float b2TreeShapeCastCallbackFcn<T>(ref B2ShapeCastInput input, int proxyId, ulong userData, ref T context) where T : struct;

    // -----------------------------------------------------------------------------------------------------------------
    // Draw
    // -----------------------------------------------------------------------------------------------------------------

    /// Draw a closed polygon provided in CCW order.
    //void ( *DrawPolygon )( const B2Vec2* vertices, int vertexCount, b2HexColor color, object context );
    public delegate void DrawPolygonFcn(ReadOnlySpan<B2Vec2> vertices, int vertexCount, B2HexColor color, object context);

    /// Draw a solid closed polygon provided in CCW order.
    public delegate void DrawSolidPolygonFcn(ref B2Transform transform, ReadOnlySpan<B2Vec2> vertices, int vertexCount, float radius, B2HexColor color, object context);

    /// Draw a circle.
    public delegate void DrawCircleFcn(B2Vec2 center, float radius, B2HexColor color, object context);

    /// Draw a solid circle.
    public delegate void DrawSolidCircleFcn(ref B2Transform transform, float radius, B2HexColor color, object context);

    /// Draw a solid capsule.
    public delegate void DrawSolidCapsuleFcn(B2Vec2 p1, B2Vec2 p2, float radius, B2HexColor color, object context);

    /// Draw a line segment.
    public delegate void DrawSegmentFcn(B2Vec2 p1, B2Vec2 p2, B2HexColor color, object context);

    /// Draw a transform. Choose your own length scale.
    public delegate void DrawTransformFcn(B2Transform transform, object context);

    /// Draw a point.
    public delegate void DrawPointFcn(B2Vec2 p, float size, B2HexColor color, object context);

    /// Draw a string in world space
    public delegate void DrawStringFcn(B2Vec2 p, string s, B2HexColor color, object context);
}