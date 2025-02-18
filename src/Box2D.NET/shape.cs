// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using static Box2D.NET.array;
using static Box2D.NET.core;
using static Box2D.NET.types;
using static Box2D.NET.constants;
using static Box2D.NET.contact;
using static Box2D.NET.math_function;
using static Box2D.NET.id;
using static Box2D.NET.body;
using static Box2D.NET.world;
using static Box2D.NET.id_pool;
using static Box2D.NET.geometry;
using static Box2D.NET.board_phase;
using static Box2D.NET.distance;

namespace Box2D.NET
{
    public class b2Shape
    {
        public int id;
        public int bodyId;
        public int prevShapeId;
        public int nextShapeId;
        public int sensorIndex;
        public b2ShapeType type;
        public float density;
        public float friction;
        public float restitution;
        public float rollingResistance;
        public float tangentSpeed;
        public int material;

        public b2AABB aabb;
        public b2AABB fatAABB;
        public b2Vec2 localCentroid;
        public int proxyKey;

        public b2Filter filter;
        public object userData;
        public uint customColor;

        // TODO: @ikpil, ...... how to handle this?
        // union
        // {
        public b2Capsule capsule;
        public b2Circle circle;
        public b2Polygon polygon;
        public b2Segment segment;
        public b2ChainSegment chainSegment;
        //};

        public ushort generation;
        public bool enableContactEvents;
        public bool enableHitEvents;
        public bool enablePreSolveEvents;
        public bool enlargedAABB;
    }

    public class b2ChainShape
    {
        public int id;
        public int bodyId;
        public int nextChainId;
        public int count;
        public int materialCount;
        public int[] shapeIndices;
        public b2SurfaceMaterial[] materials;
        public ushort generation;
    }

    public class b2ShapeExtent
    {
        public float minExtent;
        public float maxExtent;
    }

// Sensors are shapes that live in the broad-phase but never have contacts.
// At the end of the time step all sensors are queried for overlap with any other shapes.
// Sensors ignore body type and sleeping.
// Sensors generate events when there is a new overlap or and overlap disappears.
// The sensor overlaps don't get cleared until the next time step regardless of the overlapped
// shapes being destroyed.
// When a sensor is destroyed.
    public struct b2SensorOverlaps
    {
        public b2Array<int> overlaps;
    }

    public class shape
    {
        public static float b2GetShapeRadius(b2Shape shape)
        {
            switch (shape.type)
            {
                case b2ShapeType.b2_capsuleShape:
                    return shape.capsule.radius;
                case b2ShapeType.b2_circleShape:
                    return shape.circle.radius;
                case b2ShapeType.b2_polygonShape:
                    return shape.polygon.radius;
                default:
                    return 0.0f;
            }
        }

        public static b2Shape b2GetShape(b2World world, b2ShapeId shapeId)
        {
            int id = shapeId.index1 - 1;
            b2Shape shape = b2Array_Get(world.shapes, id);
            Debug.Assert(shape.id == id && shape.generation == shapeId.generation);
            return shape;
        }

        public static b2ChainShape b2GetChainShape(b2World world, b2ChainId chainId)
        {
            int id = chainId.index1 - 1;
            b2ChainShape chain = b2Array_Get(world.chainShapes, id);
            Debug.Assert(chain.id == id && chain.generation == chainId.generation);
            return chain;
        }

        public static void b2UpdateShapeAABBs(b2Shape shape, b2Transform transform, b2BodyType proxyType)
        {
            // Compute a bounding box with a speculative margin
            float speculativeDistance = B2_SPECULATIVE_DISTANCE;
            float aabbMargin = B2_AABB_MARGIN;

            b2AABB aabb = b2ComputeShapeAABB(shape, transform);
            aabb.lowerBound.x -= speculativeDistance;
            aabb.lowerBound.y -= speculativeDistance;
            aabb.upperBound.x += speculativeDistance;
            aabb.upperBound.y += speculativeDistance;
            shape.aabb = aabb;

            // Smaller margin for static bodies. Cannot be zero due to TOI tolerance.
            float margin = proxyType == b2BodyType.b2_staticBody ? speculativeDistance : aabbMargin;
            b2AABB fatAABB;
            fatAABB.lowerBound.x = aabb.lowerBound.x - margin;
            fatAABB.lowerBound.y = aabb.lowerBound.y - margin;
            fatAABB.upperBound.x = aabb.upperBound.x + margin;
            fatAABB.upperBound.y = aabb.upperBound.y + margin;
            shape.fatAABB = fatAABB;
        }

        public static b2Shape b2CreateShapeInternal(b2World world, b2Body body, b2Transform transform, b2ShapeDef def, object geometry, b2ShapeType shapeType)
        {
            Debug.Assert(b2IsValidFloat(def.density) && def.density >= 0.0f);
            Debug.Assert(b2IsValidFloat(def.friction) && def.friction >= 0.0f);
            Debug.Assert(b2IsValidFloat(def.restitution) && def.restitution >= 0.0f);

            int shapeId = b2AllocId(world.shapeIdPool);

            if (shapeId == world.shapes.count)
            {
                b2Array_Push(world.shapes, new b2Shape());
            }
            else
            {
                Debug.Assert(world.shapes.data[shapeId].id == B2_NULL_INDEX);
            }

            b2Shape shape = b2Array_Get(world.shapes, shapeId);

            switch (shapeType)
            {
                case b2ShapeType.b2_capsuleShape:
                    shape.capsule = (b2Capsule)geometry;
                    break;

                case b2ShapeType.b2_circleShape:
                    shape.circle = (b2Circle)geometry;
                    break;

                case b2ShapeType.b2_polygonShape:
                    shape.polygon = (b2Polygon)geometry;
                    break;

                case b2ShapeType.b2_segmentShape:
                    shape.segment = (b2Segment)geometry;
                    break;

                case b2ShapeType.b2_chainSegmentShape:
                    shape.chainSegment = (b2ChainSegment)geometry;
                    break;

                default:
                    Debug.Assert(false);
                    break;
            }

            shape.id = shapeId;
            shape.bodyId = body.id;
            shape.type = shapeType;
            shape.density = def.density;
            shape.friction = def.friction;
            shape.restitution = def.restitution;
            shape.rollingResistance = def.rollingResistance;
            shape.tangentSpeed = def.tangentSpeed;
            shape.material = def.material;
            shape.filter = def.filter;
            shape.userData = def.userData;
            shape.customColor = def.customColor;
            shape.enlargedAABB = false;
            shape.enableContactEvents = def.enableContactEvents;
            shape.enableHitEvents = def.enableHitEvents;
            shape.enablePreSolveEvents = def.enablePreSolveEvents;
            shape.proxyKey = B2_NULL_INDEX;
            shape.localCentroid = b2GetShapeCentroid(shape);
            shape.aabb = new b2AABB(b2Vec2_zero, b2Vec2_zero);
            shape.fatAABB = new b2AABB(b2Vec2_zero, b2Vec2_zero);
            shape.generation += 1;

            if (body.setIndex != (int)b2SetType.b2_disabledSet)
            {
                b2BodyType proxyType = body.type;
                b2CreateShapeProxy(shape, world.broadPhase, proxyType, transform, def.invokeContactCreation || def.isSensor);
            }

            // Add to shape doubly linked list
            if (body.headShapeId != B2_NULL_INDEX)
            {
                b2Shape headShape = b2Array_Get(world.shapes, body.headShapeId);
                headShape.prevShapeId = shapeId;
            }

            shape.prevShapeId = B2_NULL_INDEX;
            shape.nextShapeId = body.headShapeId;
            body.headShapeId = shapeId;
            body.shapeCount += 1;

            if (def.isSensor)
            {
                shape.sensorIndex = world.sensors.count;
                b2Sensor sensor = new b2Sensor()
                {
                    overlaps1 = b2Array_Create<b2ShapeRef>(16),
                    overlaps2 = b2Array_Create<b2ShapeRef>(16),
                    shapeId = shapeId,
                };
                b2Array_Push(world.sensors, sensor);
            }
            else
            {
                shape.sensorIndex = B2_NULL_INDEX;
            }

            b2ValidateSolverSets(world);

            return shape;
        }

        public static b2ShapeId b2CreateShape(b2BodyId bodyId, b2ShapeDef def, object geometry, b2ShapeType shapeType)
        {
            B2_CHECK_DEF(def);
            Debug.Assert(b2IsValidFloat(def.density) && def.density >= 0.0f);
            Debug.Assert(b2IsValidFloat(def.friction) && def.friction >= 0.0f);
            Debug.Assert(b2IsValidFloat(def.restitution) && def.restitution >= 0.0f);

            b2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return new b2ShapeId();
            }

            b2Body body = b2GetBodyFullId(world, bodyId);
            b2Transform transform = b2GetBodyTransformQuick(world, body);

            b2Shape shape = b2CreateShapeInternal(world, body, transform, def, geometry, shapeType);

            if (def.updateBodyMass == true)
            {
                b2UpdateBodyMassData(world, body);
            }

            b2ValidateSolverSets(world);

            b2ShapeId id = new b2ShapeId(shape.id + 1, bodyId.world0, shape.generation);
            return id;
        }

        public static b2ShapeId b2CreateCircleShape(b2BodyId bodyId, b2ShapeDef def, b2Circle circle)
        {
            return b2CreateShape(bodyId, def, circle, b2ShapeType.b2_circleShape);
        }

        public static b2ShapeId b2CreateCapsuleShape(b2BodyId bodyId, b2ShapeDef def, b2Capsule capsule)
        {
            float lengthSqr = b2DistanceSquared(capsule.center1, capsule.center2);
            if (lengthSqr <= B2_LINEAR_SLOP * B2_LINEAR_SLOP)
            {
                b2Circle circle = new b2Circle(b2Lerp(capsule.center1, capsule.center2, 0.5f), capsule.radius);
                return b2CreateShape(bodyId, def, circle, b2ShapeType.b2_circleShape);
            }

            return b2CreateShape(bodyId, def, capsule, b2ShapeType.b2_capsuleShape);
        }

        public static b2ShapeId b2CreatePolygonShape(b2BodyId bodyId, b2ShapeDef def, b2Polygon polygon)
        {
            Debug.Assert(b2IsValidFloat(polygon.radius) && polygon.radius >= 0.0f);
            return b2CreateShape(bodyId, def, polygon, b2ShapeType.b2_polygonShape);
        }

        public static b2ShapeId b2CreateSegmentShape(b2BodyId bodyId, b2ShapeDef def, b2Segment segment)
        {
            float lengthSqr = b2DistanceSquared(segment.point1, segment.point2);
            if (lengthSqr <= B2_LINEAR_SLOP * B2_LINEAR_SLOP)
            {
                Debug.Assert(false);
                return b2_nullShapeId;
            }

            return b2CreateShape(bodyId, def, segment, b2ShapeType.b2_segmentShape);
        }

// Destroy a shape on a body. This doesn't need to be called when destroying a body.
        public static void b2DestroyShapeInternal(b2World world, b2Shape shape, b2Body body, bool wakeBodies)
        {
            int shapeId = shape.id;

            // Remove the shape from the body's doubly linked list.
            if (shape.prevShapeId != B2_NULL_INDEX)
            {
                b2Shape prevShape = b2Array_Get(world.shapes, shape.prevShapeId);
                prevShape.nextShapeId = shape.nextShapeId;
            }

            if (shape.nextShapeId != B2_NULL_INDEX)
            {
                b2Shape nextShape = b2Array_Get(world.shapes, shape.nextShapeId);
                nextShape.prevShapeId = shape.prevShapeId;
            }

            if (shapeId == body.headShapeId)
            {
                body.headShapeId = shape.nextShapeId;
            }

            body.shapeCount -= 1;

            // Remove from broad-phase.
            b2DestroyShapeProxy(shape, world.broadPhase);

            // Destroy any contacts associated with the shape.
            int contactKey = body.headContactKey;
            while (contactKey != B2_NULL_INDEX)
            {
                int contactId = contactKey >> 1;
                int edgeIndex = contactKey & 1;

                b2Contact contact = b2Array_Get(world.contacts, contactId);
                contactKey = contact.edges[edgeIndex].nextKey;

                if (contact.shapeIdA == shapeId || contact.shapeIdB == shapeId)
                {
                    b2DestroyContact(world, contact, wakeBodies);
                }
            }

            if (shape.sensorIndex != B2_NULL_INDEX)
            {
                b2Sensor sensor = b2Array_Get(world.sensors, shape.sensorIndex);
                for (int i = 0; i < sensor.overlaps2.count; ++i)
                {
                    b2ShapeRef @ref = sensor.overlaps2.data[i];
                    b2SensorEndTouchEvent @event = new b2SensorEndTouchEvent()
                    {
                        sensorShapeId = new b2ShapeId(shapeId + 1, world.worldId, shape.generation),
                        visitorShapeId = new b2ShapeId(@ref.shapeId + 1, world.worldId, @ref.generation),
                    };

                    b2Array_Push(world.sensorEndEvents[world.endEventArrayIndex], @event);
                }

                // Destroy sensor
                b2Array_Destroy(sensor.overlaps1);
                b2Array_Destroy(sensor.overlaps2);

                int movedIndex = b2Array_RemoveSwap(world.sensors, shape.sensorIndex);
                if (movedIndex != B2_NULL_INDEX)
                {
                    // Fixup moved sensor
                    b2Sensor movedSensor = b2Array_Get(world.sensors, shape.sensorIndex);
                    b2Shape otherSensorShape = b2Array_Get(world.shapes, movedSensor.shapeId);
                    otherSensorShape.sensorIndex = shape.sensorIndex;
                }
            }

            // Return shape to free list.
            b2FreeId(world.shapeIdPool, shapeId);
            shape.id = B2_NULL_INDEX;

            b2ValidateSolverSets(world);
        }

        public static void b2DestroyShape(b2ShapeId shapeId, bool updateBodyMass)
        {
            b2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return;
            }

            b2Shape shape = b2GetShape(world, shapeId);

            // need to wake bodies because this might be a static body
            bool wakeBodies = true;

            b2Body body = b2Array_Get(world.bodies, shape.bodyId);
            b2DestroyShapeInternal(world, shape, body, wakeBodies);

            if (updateBodyMass == true)
            {
                b2UpdateBodyMassData(world, body);
            }
        }

        public static b2ChainId b2CreateChain(b2BodyId bodyId, b2ChainDef def)
        {
            B2_CHECK_DEF(def);
            Debug.Assert(def.count >= 4);
            Debug.Assert(def.materialCount == 1 || def.materialCount == def.count);

            b2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return new b2ChainId();
            }

            b2Body body = b2GetBodyFullId(world, bodyId);
            b2Transform transform = b2GetBodyTransformQuick(world, body);

            int chainId = b2AllocId(world.chainIdPool);

            if (chainId == world.chainShapes.count)
            {
                b2Array_Push(world.chainShapes, new b2ChainShape());
            }
            else
            {
                Debug.Assert(world.chainShapes.data[chainId].id == B2_NULL_INDEX);
            }

            b2ChainShape chainShape = b2Array_Get(world.chainShapes, chainId);

            chainShape.id = chainId;
            chainShape.bodyId = body.id;
            chainShape.nextChainId = body.headChainId;
            chainShape.generation += 1;

            int materialCount = def.materialCount;
            chainShape.materialCount = materialCount;
            chainShape.materials = b2Alloc<b2SurfaceMaterial>(materialCount);

            for (int i = 0; i < materialCount; ++i)
            {
                b2SurfaceMaterial material = def.materials[i];
                Debug.Assert(b2IsValidFloat(material.friction) && material.friction >= 0.0f);
                Debug.Assert(b2IsValidFloat(material.restitution) && material.restitution >= 0.0f);
                Debug.Assert(b2IsValidFloat(material.rollingResistance) && material.rollingResistance >= 0.0f);
                Debug.Assert(b2IsValidFloat(material.tangentSpeed));

                chainShape.materials[i] = material.Clone();
            }

            body.headChainId = chainId;

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.userData = def.userData;
            shapeDef.filter = def.filter;
            shapeDef.enableContactEvents = false;
            shapeDef.enableHitEvents = false;

            b2Vec2[] points = def.points;
            int n = def.count;

            if (def.isLoop)
            {
                chainShape.count = n;
                chainShape.shapeIndices = b2Alloc<int>(chainShape.count);

                b2ChainSegment chainSegment = new b2ChainSegment();

                int prevIndex = n - 1;
                for (int i = 0; i < n - 2; ++i)
                {
                    chainSegment.ghost1 = points[prevIndex];
                    chainSegment.segment.point1 = points[i];
                    chainSegment.segment.point2 = points[i + 1];
                    chainSegment.ghost2 = points[i + 2];
                    chainSegment.chainId = chainId;
                    prevIndex = i;

                    int materialIndex = materialCount == 1 ? 0 : i;
                    b2SurfaceMaterial material = def.materials[materialIndex];
                    shapeDef.friction = material.friction;
                    shapeDef.restitution = material.restitution;
                    shapeDef.rollingResistance = material.rollingResistance;
                    shapeDef.tangentSpeed = material.tangentSpeed;
                    shapeDef.customColor = material.customColor;
                    shapeDef.material = material.material;

                    b2Shape shape = b2CreateShapeInternal(world, body, transform, shapeDef, chainSegment, b2ShapeType.b2_chainSegmentShape);
                    chainShape.shapeIndices[i] = shape.id;
                }

                {
                    chainSegment.ghost1 = points[n - 3];
                    chainSegment.segment.point1 = points[n - 2];
                    chainSegment.segment.point2 = points[n - 1];
                    chainSegment.ghost2 = points[0];
                    chainSegment.chainId = chainId;

                    int materialIndex = materialCount == 1 ? 0 : n - 2;
                    b2SurfaceMaterial material = def.materials[materialIndex];
                    shapeDef.friction = material.friction;
                    shapeDef.restitution = material.restitution;
                    shapeDef.rollingResistance = material.rollingResistance;
                    shapeDef.tangentSpeed = material.tangentSpeed;
                    shapeDef.customColor = material.customColor;
                    shapeDef.material = material.material;

                    b2Shape shape = b2CreateShapeInternal(world, body, transform, shapeDef, chainSegment, b2ShapeType.b2_chainSegmentShape);
                    chainShape.shapeIndices[n - 2] = shape.id;
                }

                {
                    chainSegment.ghost1 = points[n - 2];
                    chainSegment.segment.point1 = points[n - 1];
                    chainSegment.segment.point2 = points[0];
                    chainSegment.ghost2 = points[1];
                    chainSegment.chainId = chainId;

                    int materialIndex = materialCount == 1 ? 0 : n - 1;
                    b2SurfaceMaterial material = def.materials[materialIndex];
                    shapeDef.friction = material.friction;
                    shapeDef.restitution = material.restitution;
                    shapeDef.rollingResistance = material.rollingResistance;
                    shapeDef.tangentSpeed = material.tangentSpeed;
                    shapeDef.customColor = material.customColor;
                    shapeDef.material = material.material;

                    b2Shape shape = b2CreateShapeInternal(world, body, transform, shapeDef, chainSegment, b2ShapeType.b2_chainSegmentShape);
                    chainShape.shapeIndices[n - 1] = shape.id;
                }
            }
            else
            {
                chainShape.count = n - 3;
                chainShape.shapeIndices = b2Alloc<int>(chainShape.count);

                b2ChainSegment chainSegment = new b2ChainSegment();

                for (int i = 0; i < n - 3; ++i)
                {
                    chainSegment.ghost1 = points[i];
                    chainSegment.segment.point1 = points[i + 1];
                    chainSegment.segment.point2 = points[i + 2];
                    chainSegment.ghost2 = points[i + 3];
                    chainSegment.chainId = chainId;

                    // Material is associated with leading point of solid segment
                    int materialIndex = materialCount == 1 ? 0 : i + 1;
                    b2SurfaceMaterial material = def.materials[materialIndex];
                    shapeDef.friction = material.friction;
                    shapeDef.restitution = material.restitution;
                    shapeDef.rollingResistance = material.rollingResistance;
                    shapeDef.tangentSpeed = material.tangentSpeed;
                    shapeDef.customColor = material.customColor;
                    shapeDef.material = material.material;

                    b2Shape shape = b2CreateShapeInternal(world, body, transform, shapeDef, chainSegment, b2ShapeType.b2_chainSegmentShape);
                    chainShape.shapeIndices[i] = shape.id;
                }
            }

            b2ChainId id = new b2ChainId(chainId + 1, world.worldId, chainShape.generation);
            return id;
        }

        public static void b2FreeChainData(b2ChainShape chain)
        {
            b2Free(chain.shapeIndices, chain.count);
            chain.shapeIndices = null;

            b2Free(chain.materials, chain.materialCount);
            chain.materials = null;
        }

        public static void b2DestroyChain(b2ChainId chainId)
        {
            b2World world = b2GetWorldLocked(chainId.world0);
            if (world == null)
            {
                return;
            }

            b2ChainShape chain = b2GetChainShape(world, chainId);
            bool wakeBodies = true;

            b2Body body = b2Array_Get(world.bodies, chain.bodyId);

            // TODO: @ikpil, check!
            // Remove the chain from the body's singly linked list.
            int chainIdPtr = body.headChainId;
            bool found = false;
            while (chainIdPtr != B2_NULL_INDEX)
            {
                if (chainIdPtr == chain.id)
                {
                    chainIdPtr = chain.nextChainId;
                    body.headChainId = chain.nextChainId;
                    found = true;
                    break;
                }

                chainIdPtr = world.chainShapes.data[chainIdPtr].nextChainId;
            }

            Debug.Assert(found == true);
            if (found == false)
            {
                return;
            }

            int count = chain.count;
            for (int i = 0; i < count; ++i)
            {
                int shapeId = chain.shapeIndices[i];
                b2Shape shape = b2Array_Get(world.shapes, shapeId);
                b2DestroyShapeInternal(world, shape, body, wakeBodies);
            }

            b2FreeChainData(chain);

            // Return chain to free list.
            b2FreeId(world.chainIdPool, chain.id);
            chain.id = B2_NULL_INDEX;

            b2ValidateSolverSets(world);
        }

        public static b2WorldId b2Chain_GetWorld(b2ChainId chainId)
        {
            b2World world = b2GetWorld(chainId.world0);
            return new b2WorldId((ushort)(chainId.world0 + 1), world.generation);
        }

        public static int b2Chain_GetSegmentCount(b2ChainId chainId)
        {
            b2World world = b2GetWorldLocked(chainId.world0);
            if (world == null)
            {
                return 0;
            }

            b2ChainShape chain = b2GetChainShape(world, chainId);
            return chain.count;
        }

        public static int b2Chain_GetSegments(b2ChainId chainId, b2ShapeId[] segmentArray, int capacity)
        {
            b2World world = b2GetWorldLocked(chainId.world0);
            if (world == null)
            {
                return 0;
            }

            b2ChainShape chain = b2GetChainShape(world, chainId);

            int count = b2MinInt(chain.count, capacity);
            for (int i = 0; i < count; ++i)
            {
                int shapeId = chain.shapeIndices[i];
                b2Shape shape = b2Array_Get(world.shapes, shapeId);
                segmentArray[i] = new b2ShapeId(shapeId + 1, chainId.world0, shape.generation);
            }

            return count;
        }

        public static b2AABB b2ComputeShapeAABB(b2Shape shape, b2Transform xf)
        {
            switch (shape.type)
            {
                case b2ShapeType.b2_capsuleShape:
                    return b2ComputeCapsuleAABB(shape.capsule, xf);
                case b2ShapeType.b2_circleShape:
                    return b2ComputeCircleAABB(shape.circle, xf);
                case b2ShapeType.b2_polygonShape:
                    return b2ComputePolygonAABB(shape.polygon, xf);
                case b2ShapeType.b2_segmentShape:
                    return b2ComputeSegmentAABB(shape.segment, xf);
                case b2ShapeType.b2_chainSegmentShape:
                    return b2ComputeSegmentAABB(shape.chainSegment.segment, xf);
                default:
                {
                    Debug.Assert(false);
                    b2AABB empty = new b2AABB(xf.p, xf.p);
                    return empty;
                }
            }
        }

        public static b2Vec2 b2GetShapeCentroid(b2Shape shape)
        {
            switch (shape.type)
            {
                case b2ShapeType.b2_capsuleShape:
                    return b2Lerp(shape.capsule.center1, shape.capsule.center2, 0.5f);
                case b2ShapeType.b2_circleShape:
                    return shape.circle.center;
                case b2ShapeType.b2_polygonShape:
                    return shape.polygon.centroid;
                case b2ShapeType.b2_segmentShape:
                    return b2Lerp(shape.segment.point1, shape.segment.point2, 0.5f);
                case b2ShapeType.b2_chainSegmentShape:
                    return b2Lerp(shape.chainSegment.segment.point1, shape.chainSegment.segment.point2, 0.5f);
                default:
                    return b2Vec2_zero;
            }
        }

// todo_erin maybe compute this on shape creation
        public static float b2GetShapePerimeter(b2Shape shape)
        {
            switch (shape.type)
            {
                case b2ShapeType.b2_capsuleShape:
                    return 2.0f * b2Length(b2Sub(shape.capsule.center1, shape.capsule.center2)) +
                           2.0f * B2_PI * shape.capsule.radius;
                case b2ShapeType.b2_circleShape:
                    return 2.0f * B2_PI * shape.circle.radius;
                case b2ShapeType.b2_polygonShape:
                {
                    b2Vec2[] points = shape.polygon.vertices;
                    int count = shape.polygon.count;
                    float perimeter = 2.0f * B2_PI * shape.polygon.radius;
                    Debug.Assert(count > 0);
                    b2Vec2 prev = points[count - 1];
                    for (int i = 0; i < count; ++i)
                    {
                        b2Vec2 next = points[i];
                        perimeter += b2Length(b2Sub(next, prev));
                        prev = next;
                    }

                    return perimeter;
                }
                case b2ShapeType.b2_segmentShape:
                    return 2.0f * b2Length(b2Sub(shape.segment.point1, shape.segment.point2));
                case b2ShapeType.b2_chainSegmentShape:
                    return 2.0f * b2Length(b2Sub(shape.chainSegment.segment.point1, shape.chainSegment.segment.point2));
                default:
                    return 0.0f;
            }
        }

// This projects the shape perimeter onto an infinite line
        public static float b2GetShapeProjectedPerimeter(b2Shape shape, b2Vec2 line)
        {
            switch (shape.type)
            {
                case b2ShapeType.b2_capsuleShape:
                {
                    b2Vec2 axis = b2Sub(shape.capsule.center2, shape.capsule.center1);
                    float projectedLength = b2AbsFloat(b2Dot(axis, line));
                    return projectedLength + 2.0f * shape.capsule.radius;
                }

                case b2ShapeType.b2_circleShape:
                    return 2.0f * shape.circle.radius;

                case b2ShapeType.b2_polygonShape:
                {
                    b2Vec2[] points = shape.polygon.vertices;
                    int count = shape.polygon.count;
                    Debug.Assert(count > 0);
                    float value = b2Dot(points[0], line);
                    float lower = value;
                    float upper = value;
                    for (int i = 1; i < count; ++i)
                    {
                        value = b2Dot(points[i], line);
                        lower = b2MinFloat(lower, value);
                        upper = b2MaxFloat(upper, value);
                    }

                    return (upper - lower) + 2.0f * shape.polygon.radius;
                }

                case b2ShapeType.b2_segmentShape:
                {
                    float value1 = b2Dot(shape.segment.point1, line);
                    float value2 = b2Dot(shape.segment.point2, line);
                    return b2AbsFloat(value2 - value1);
                }

                case b2ShapeType.b2_chainSegmentShape:
                {
                    float value1 = b2Dot(shape.chainSegment.segment.point1, line);
                    float value2 = b2Dot(shape.chainSegment.segment.point2, line);
                    return b2AbsFloat(value2 - value1);
                }

                default:
                    return 0.0f;
            }
        }

        public static b2MassData b2ComputeShapeMass(b2Shape shape)
        {
            switch (shape.type)
            {
                case b2ShapeType.b2_capsuleShape:
                    return b2ComputeCapsuleMass(shape.capsule, shape.density);
                case b2ShapeType.b2_circleShape:
                    return b2ComputeCircleMass(shape.circle, shape.density);
                case b2ShapeType.b2_polygonShape:
                    return b2ComputePolygonMass(shape.polygon, shape.density);
                default:
                    return new b2MassData();
            }
        }

        public static b2ShapeExtent b2ComputeShapeExtent(b2Shape shape, b2Vec2 localCenter)
        {
            b2ShapeExtent extent = new b2ShapeExtent();

            switch (shape.type)
            {
                case b2ShapeType.b2_capsuleShape:
                {
                    float radius = shape.capsule.radius;
                    extent.minExtent = radius;
                    b2Vec2 c1 = b2Sub(shape.capsule.center1, localCenter);
                    b2Vec2 c2 = b2Sub(shape.capsule.center2, localCenter);
                    extent.maxExtent = MathF.Sqrt(b2MaxFloat(b2LengthSquared(c1), b2LengthSquared(c2))) + radius;
                }
                    break;

                case b2ShapeType.b2_circleShape:
                {
                    float radius = shape.circle.radius;
                    extent.minExtent = radius;
                    extent.maxExtent = b2Length(b2Sub(shape.circle.center, localCenter)) + radius;
                }
                    break;

                case b2ShapeType.b2_polygonShape:
                {
                    b2Polygon poly = shape.polygon;
                    float minExtent = B2_HUGE;
                    float maxExtentSqr = 0.0f;
                    int count = poly.count;
                    for (int i = 0; i < count; ++i)
                    {
                        b2Vec2 v = poly.vertices[i];
                        float planeOffset = b2Dot(poly.normals[i], b2Sub(v, poly.centroid));
                        minExtent = b2MinFloat(minExtent, planeOffset);

                        float distanceSqr = b2LengthSquared(b2Sub(v, localCenter));
                        maxExtentSqr = b2MaxFloat(maxExtentSqr, distanceSqr);
                    }

                    extent.minExtent = minExtent + poly.radius;
                    extent.maxExtent = MathF.Sqrt(maxExtentSqr) + poly.radius;
                }
                    break;

                case b2ShapeType.b2_segmentShape:
                {
                    extent.minExtent = 0.0f;
                    b2Vec2 c1 = b2Sub(shape.segment.point1, localCenter);
                    b2Vec2 c2 = b2Sub(shape.segment.point2, localCenter);
                    extent.maxExtent = MathF.Sqrt(b2MaxFloat(b2LengthSquared(c1), b2LengthSquared(c2)));
                }
                    break;

                case b2ShapeType.b2_chainSegmentShape:
                {
                    extent.minExtent = 0.0f;
                    b2Vec2 c1 = b2Sub(shape.chainSegment.segment.point1, localCenter);
                    b2Vec2 c2 = b2Sub(shape.chainSegment.segment.point2, localCenter);
                    extent.maxExtent = MathF.Sqrt(b2MaxFloat(b2LengthSquared(c1), b2LengthSquared(c2)));
                }
                    break;

                default:
                    break;
            }

            return extent;
        }

        public static b2CastOutput b2RayCastShape(b2RayCastInput input, b2Shape shape, b2Transform transform)
        {
            b2RayCastInput localInput = input;
            localInput.origin = b2InvTransformPoint(transform, input.origin);
            localInput.translation = b2InvRotateVector(transform.q, input.translation);

            b2CastOutput output = new b2CastOutput();
            switch (shape.type)
            {
                case b2ShapeType.b2_capsuleShape:
                    output = b2RayCastCapsule(localInput, shape.capsule);
                    break;
                case b2ShapeType.b2_circleShape:
                    output = b2RayCastCircle(localInput, shape.circle);
                    break;
                case b2ShapeType.b2_polygonShape:
                    output = b2RayCastPolygon(localInput, shape.polygon);
                    break;
                case b2ShapeType.b2_segmentShape:
                    output = b2RayCastSegment(localInput, shape.segment, false);
                    break;
                case b2ShapeType.b2_chainSegmentShape:
                    output = b2RayCastSegment(localInput, shape.chainSegment.segment, true);
                    break;
                default:
                    return output;
            }

            output.point = b2TransformPoint(transform, output.point);
            output.normal = b2RotateVector(transform.q, output.normal);
            return output;
        }

        public static b2CastOutput b2ShapeCastShape(b2ShapeCastInput input, b2Shape shape, b2Transform transform)
        {
            b2ShapeCastInput localInput = input;

            for (int i = 0; i < localInput.count; ++i)
            {
                localInput.points[i] = b2InvTransformPoint(transform, input.points[i]);
            }

            localInput.translation = b2InvRotateVector(transform.q, input.translation);

            b2CastOutput output = new b2CastOutput();
            switch (shape.type)
            {
                case b2ShapeType.b2_capsuleShape:
                    output = b2ShapeCastCapsule(localInput, shape.capsule);
                    break;
                case b2ShapeType.b2_circleShape:
                    output = b2ShapeCastCircle(localInput, shape.circle);
                    break;
                case b2ShapeType.b2_polygonShape:
                    output = b2ShapeCastPolygon(localInput, shape.polygon);
                    break;
                case b2ShapeType.b2_segmentShape:
                    output = b2ShapeCastSegment(localInput, shape.segment);
                    break;
                case b2ShapeType.b2_chainSegmentShape:
                    output = b2ShapeCastSegment(localInput, shape.chainSegment.segment);
                    break;
                default:
                    return output;
            }

            output.point = b2TransformPoint(transform, output.point);
            output.normal = b2RotateVector(transform.q, output.normal);
            return output;
        }

        public static void b2CreateShapeProxy(b2Shape shape, b2BroadPhase bp, b2BodyType type, b2Transform transform, bool forcePairCreation)
        {
            Debug.Assert(shape.proxyKey == B2_NULL_INDEX);

            b2UpdateShapeAABBs(shape, transform, type);

            // Create proxies in the broad-phase.
            shape.proxyKey =
                b2BroadPhase_CreateProxy(bp, type, shape.fatAABB, shape.filter.categoryBits, shape.id, forcePairCreation);
            Debug.Assert(B2_PROXY_TYPE(shape.proxyKey) < b2BodyType.b2_bodyTypeCount);
        }

        public static void b2DestroyShapeProxy(b2Shape shape, b2BroadPhase bp)
        {
            if (shape.proxyKey != B2_NULL_INDEX)
            {
                b2BroadPhase_DestroyProxy(bp, shape.proxyKey);
                shape.proxyKey = B2_NULL_INDEX;
            }
        }

        public static b2ShapeProxy b2MakeShapeDistanceProxy(b2Shape shape)
        {
            switch (shape.type)
            {
                case b2ShapeType.b2_capsuleShape:
                    return b2MakeProxy([shape.capsule.center1], 2, shape.capsule.radius);
                case b2ShapeType.b2_circleShape:
                    return b2MakeProxy([shape.circle.center], 1, shape.circle.radius);
                case b2ShapeType.b2_polygonShape:
                    return b2MakeProxy(shape.polygon.vertices, shape.polygon.count, shape.polygon.radius);
                case b2ShapeType.b2_segmentShape:
                    return b2MakeProxy([shape.segment.point1], 2, 0.0f);
                case b2ShapeType.b2_chainSegmentShape:
                    return b2MakeProxy([shape.chainSegment.segment.point1], 2, 0.0f);
                default:
                {
                    Debug.Assert(false);
                    b2ShapeProxy empty = new b2ShapeProxy();
                    return empty;
                }
            }
        }

        public static b2BodyId b2Shape_GetBody(b2ShapeId shapeId)
        {
            b2World world = b2GetWorld(shapeId.world0);
            b2Shape shape = b2GetShape(world, shapeId);
            return b2MakeBodyId(world, shape.bodyId);
        }

        public static b2WorldId b2Shape_GetWorld(b2ShapeId shapeId)
        {
            b2World world = b2GetWorld(shapeId.world0);
            return new b2WorldId((ushort)(shapeId.world0 + 1), world.generation);
        }

        public static void b2Shape_SetUserData(b2ShapeId shapeId, object userData)
        {
            b2World world = b2GetWorld(shapeId.world0);
            b2Shape shape = b2GetShape(world, shapeId);
            shape.userData = userData;
        }

        public static object b2Shape_GetUserData(b2ShapeId shapeId)
        {
            b2World world = b2GetWorld(shapeId.world0);
            b2Shape shape = b2GetShape(world, shapeId);
            return shape.userData;
        }

        public static bool b2Shape_IsSensor(b2ShapeId shapeId)
        {
            b2World world = b2GetWorld(shapeId.world0);
            b2Shape shape = b2GetShape(world, shapeId);
            return shape.sensorIndex != B2_NULL_INDEX;
        }

        public static bool b2Shape_TestPoint(b2ShapeId shapeId, b2Vec2 point)
        {
            b2World world = b2GetWorld(shapeId.world0);
            b2Shape shape = b2GetShape(world, shapeId);

            b2Transform transform = b2GetBodyTransform(world, shape.bodyId);
            b2Vec2 localPoint = b2InvTransformPoint(transform, point);

            switch (shape.type)
            {
                case b2ShapeType.b2_capsuleShape:
                    return b2PointInCapsule(localPoint, shape.capsule);

                case b2ShapeType.b2_circleShape:
                    return b2PointInCircle(localPoint, shape.circle);

                case b2ShapeType.b2_polygonShape:
                    return b2PointInPolygon(localPoint, shape.polygon);

                default:
                    return false;
            }
        }

// todo_erin untested
        public static b2CastOutput b2Shape_RayCast(b2ShapeId shapeId, b2RayCastInput input)
        {
            b2World world = b2GetWorld(shapeId.world0);
            b2Shape shape = b2GetShape(world, shapeId);

            b2Transform transform = b2GetBodyTransform(world, shape.bodyId);

            // input in local coordinates
            b2RayCastInput localInput;
            localInput.origin = b2InvTransformPoint(transform, input.origin);
            localInput.translation = b2InvRotateVector(transform.q, input.translation);
            localInput.maxFraction = input.maxFraction;

            b2CastOutput output = new b2CastOutput();
            switch (shape.type)
            {
                case b2ShapeType.b2_capsuleShape:
                    output = b2RayCastCapsule(localInput, shape.capsule);
                    break;

                case b2ShapeType.b2_circleShape:
                    output = b2RayCastCircle(localInput, shape.circle);
                    break;

                case b2ShapeType.b2_segmentShape:
                    output = b2RayCastSegment(localInput, shape.segment, false);
                    break;

                case b2ShapeType.b2_polygonShape:
                    output = b2RayCastPolygon(localInput, shape.polygon);
                    break;

                case b2ShapeType.b2_chainSegmentShape:
                    output = b2RayCastSegment(localInput, shape.chainSegment.segment, true);
                    break;

                default:
                    Debug.Assert(false);
                    return output;
            }

            if (output.hit)
            {
                // convert to world coordinates
                output.normal = b2RotateVector(transform.q, output.normal);
                output.point = b2TransformPoint(transform, output.point);
            }

            return output;
        }

        public static void b2Shape_SetDensity(b2ShapeId shapeId, float density, bool updateBodyMass)
        {
            Debug.Assert(b2IsValidFloat(density) && density >= 0.0f);

            b2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return;
            }

            b2Shape shape = b2GetShape(world, shapeId);
            if (density == shape.density)
            {
                // early return to avoid expensive function
                return;
            }

            shape.density = density;

            if (updateBodyMass == true)
            {
                b2Body body = b2Array_Get(world.bodies, shape.bodyId);
                b2UpdateBodyMassData(world, body);
            }
        }

        public static float b2Shape_GetDensity(b2ShapeId shapeId)
        {
            b2World world = b2GetWorld(shapeId.world0);
            b2Shape shape = b2GetShape(world, shapeId);
            return shape.density;
        }

        public static void b2Shape_SetFriction(b2ShapeId shapeId, float friction)
        {
            Debug.Assert(b2IsValidFloat(friction) && friction >= 0.0f);

            b2World world = b2GetWorld(shapeId.world0);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return;
            }

            b2Shape shape = b2GetShape(world, shapeId);
            shape.friction = friction;
        }

        public static float b2Shape_GetFriction(b2ShapeId shapeId)
        {
            b2World world = b2GetWorld(shapeId.world0);
            b2Shape shape = b2GetShape(world, shapeId);
            return shape.friction;
        }

        public static void b2Shape_SetRestitution(b2ShapeId shapeId, float restitution)
        {
            Debug.Assert(b2IsValidFloat(restitution) && restitution >= 0.0f);

            b2World world = b2GetWorld(shapeId.world0);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return;
            }

            b2Shape shape = b2GetShape(world, shapeId);
            shape.restitution = restitution;
        }

        public static float b2Shape_GetRestitution(b2ShapeId shapeId)
        {
            b2World world = b2GetWorld(shapeId.world0);
            b2Shape shape = b2GetShape(world, shapeId);
            return shape.restitution;
        }

        public static void b2Shape_SetMaterial(b2ShapeId shapeId, int material)
        {
            b2World world = b2GetWorld(shapeId.world0);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return;
            }

            b2Shape shape = b2GetShape(world, shapeId);
            shape.material = material;
        }

        public static int b2Shape_GetMaterial(b2ShapeId shapeId)
        {
            b2World world = b2GetWorld(shapeId.world0);
            b2Shape shape = b2GetShape(world, shapeId);
            return shape.material;
        }

        public static b2Filter b2Shape_GetFilter(b2ShapeId shapeId)
        {
            b2World world = b2GetWorld(shapeId.world0);
            b2Shape shape = b2GetShape(world, shapeId);
            return shape.filter;
        }

        public static void b2ResetProxy(b2World world, b2Shape shape, bool wakeBodies, bool destroyProxy)
        {
            b2Body body = b2Array_Get(world.bodies, shape.bodyId);

            int shapeId = shape.id;

            // destroy all contacts associated with this shape
            int contactKey = body.headContactKey;
            while (contactKey != B2_NULL_INDEX)
            {
                int contactId = contactKey >> 1;
                int edgeIndex = contactKey & 1;

                b2Contact contact = b2Array_Get(world.contacts, contactId);
                contactKey = contact.edges[edgeIndex].nextKey;

                if (contact.shapeIdA == shapeId || contact.shapeIdB == shapeId)
                {
                    b2DestroyContact(world, contact, wakeBodies);
                }
            }

            b2Transform transform = b2GetBodyTransformQuick(world, body);
            if (shape.proxyKey != B2_NULL_INDEX)
            {
                b2BodyType proxyType = B2_PROXY_TYPE(shape.proxyKey);
                b2UpdateShapeAABBs(shape, transform, proxyType);

                if (destroyProxy)
                {
                    b2BroadPhase_DestroyProxy(world.broadPhase, shape.proxyKey);

                    bool forcePairCreation = true;
                    shape.proxyKey = b2BroadPhase_CreateProxy(world.broadPhase, proxyType, shape.fatAABB, shape.filter.categoryBits,
                        shapeId, forcePairCreation);
                }
                else
                {
                    b2BroadPhase_MoveProxy(world.broadPhase, shape.proxyKey, shape.fatAABB);
                }
            }
            else
            {
                b2BodyType proxyType = body.type;
                b2UpdateShapeAABBs(shape, transform, proxyType);
            }

            b2ValidateSolverSets(world);
        }

        public static void b2Shape_SetFilter(b2ShapeId shapeId, b2Filter filter)
        {
            b2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return;
            }

            b2Shape shape = b2GetShape(world, shapeId);
            if (filter.maskBits == shape.filter.maskBits && filter.categoryBits == shape.filter.categoryBits &&
                filter.groupIndex == shape.filter.groupIndex)
            {
                return;
            }

            // If the category bits change, I need to destroy the proxy because it affects the tree sorting.
            bool destroyProxy = filter.categoryBits != shape.filter.categoryBits;

            shape.filter = filter;

            // need to wake bodies because a filter change may destroy contacts
            bool wakeBodies = true;
            b2ResetProxy(world, shape, wakeBodies, destroyProxy);

            // note: this does not immediately update sensor overlaps. Instead sensor
            // overlaps are updated the next time step
        }

        public static void b2Shape_EnableContactEvents(b2ShapeId shapeId, bool flag)
        {
            b2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return;
            }

            b2Shape shape = b2GetShape(world, shapeId);
            shape.enableContactEvents = flag;
        }

        public static bool b2Shape_AreContactEventsEnabled(b2ShapeId shapeId)
        {
            b2World world = b2GetWorld(shapeId.world0);
            b2Shape shape = b2GetShape(world, shapeId);
            return shape.enableContactEvents;
        }

        public static void b2Shape_EnablePreSolveEvents(b2ShapeId shapeId, bool flag)
        {
            b2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return;
            }

            b2Shape shape = b2GetShape(world, shapeId);
            shape.enablePreSolveEvents = flag;
        }

        public static bool b2Shape_ArePreSolveEventsEnabled(b2ShapeId shapeId)
        {
            b2World world = b2GetWorld(shapeId.world0);
            b2Shape shape = b2GetShape(world, shapeId);
            return shape.enablePreSolveEvents;
        }

        public static void b2Shape_EnableHitEvents(b2ShapeId shapeId, bool flag)
        {
            b2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return;
            }

            b2Shape shape = b2GetShape(world, shapeId);
            shape.enableHitEvents = flag;
        }

        public static bool b2Shape_AreHitEventsEnabled(b2ShapeId shapeId)
        {
            b2World world = b2GetWorld(shapeId.world0);
            b2Shape shape = b2GetShape(world, shapeId);
            return shape.enableHitEvents;
        }

        public static b2ShapeType b2Shape_GetType(b2ShapeId shapeId)
        {
            b2World world = b2GetWorld(shapeId.world0);
            b2Shape shape = b2GetShape(world, shapeId);
            return shape.type;
        }

        public static b2Circle b2Shape_GetCircle(b2ShapeId shapeId)
        {
            b2World world = b2GetWorld(shapeId.world0);
            b2Shape shape = b2GetShape(world, shapeId);
            Debug.Assert(shape.type == b2ShapeType.b2_circleShape);
            return shape.circle;
        }

        public static b2Segment b2Shape_GetSegment(b2ShapeId shapeId)
        {
            b2World world = b2GetWorld(shapeId.world0);
            b2Shape shape = b2GetShape(world, shapeId);
            Debug.Assert(shape.type == b2ShapeType.b2_segmentShape);
            return shape.segment;
        }

        public static b2ChainSegment b2Shape_GetChainSegment(b2ShapeId shapeId)
        {
            b2World world = b2GetWorld(shapeId.world0);
            b2Shape shape = b2GetShape(world, shapeId);
            Debug.Assert(shape.type == b2ShapeType.b2_chainSegmentShape);
            return shape.chainSegment;
        }

        public static b2Capsule b2Shape_GetCapsule(b2ShapeId shapeId)
        {
            b2World world = b2GetWorld(shapeId.world0);
            b2Shape shape = b2GetShape(world, shapeId);
            Debug.Assert(shape.type == b2ShapeType.b2_capsuleShape);
            return shape.capsule;
        }

        public static b2Polygon b2Shape_GetPolygon(b2ShapeId shapeId)
        {
            b2World world = b2GetWorld(shapeId.world0);
            b2Shape shape = b2GetShape(world, shapeId);
            Debug.Assert(shape.type == b2ShapeType.b2_polygonShape);
            return shape.polygon;
        }

        public static void b2Shape_SetCircle(b2ShapeId shapeId, b2Circle circle)
        {
            b2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return;
            }

            b2Shape shape = b2GetShape(world, shapeId);
            shape.circle = new b2Circle(circle.center, circle.radius);
            shape.type = b2ShapeType.b2_circleShape;

            // need to wake bodies so they can react to the shape change
            bool wakeBodies = true;
            bool destroyProxy = true;
            b2ResetProxy(world, shape, wakeBodies, destroyProxy);
        }

        public static void b2Shape_SetCapsule(b2ShapeId shapeId, b2Capsule capsule)
        {
            b2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return;
            }

            b2Shape shape = b2GetShape(world, shapeId);
            shape.capsule = new b2Capsule(capsule.center1, capsule.center2, capsule.radius);
            shape.type = b2ShapeType.b2_capsuleShape;

            // need to wake bodies so they can react to the shape change
            bool wakeBodies = true;
            bool destroyProxy = true;
            b2ResetProxy(world, shape, wakeBodies, destroyProxy);
        }

        public static void b2Shape_SetSegment(b2ShapeId shapeId, b2Segment segment)
        {
            b2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return;
            }

            b2Shape shape = b2GetShape(world, shapeId);
            shape.segment = new b2Segment(segment.point1, segment.point2);
            shape.type = b2ShapeType.b2_segmentShape;

            // need to wake bodies so they can react to the shape change
            bool wakeBodies = true;
            bool destroyProxy = true;
            b2ResetProxy(world, shape, wakeBodies, destroyProxy);
        }

        public static void b2Shape_SetPolygon(b2ShapeId shapeId, b2Polygon polygon)
        {
            b2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return;
            }

            b2Shape shape = b2GetShape(world, shapeId);
            shape.polygon = polygon.Clone();
            shape.type = b2ShapeType.b2_polygonShape;

            // need to wake bodies so they can react to the shape change
            bool wakeBodies = true;
            bool destroyProxy = true;
            b2ResetProxy(world, shape, wakeBodies, destroyProxy);
        }

        public static b2ChainId b2Shape_GetParentChain(b2ShapeId shapeId)
        {
            b2World world = b2GetWorld(shapeId.world0);
            b2Shape shape = b2GetShape(world, shapeId);
            if (shape.type == b2ShapeType.b2_chainSegmentShape)
            {
                int chainId = shape.chainSegment.chainId;
                if (chainId != B2_NULL_INDEX)
                {
                    b2ChainShape chain = b2Array_Get(world.chainShapes, chainId);
                    b2ChainId id = new b2ChainId(chainId + 1, shapeId.world0, chain.generation);
                    return id;
                }
            }

            return new b2ChainId();
        }

        public static void b2Chain_SetFriction(b2ChainId chainId, float friction)
        {
            Debug.Assert(b2IsValidFloat(friction) && friction >= 0.0f);

            b2World world = b2GetWorldLocked(chainId.world0);
            if (world == null)
            {
                return;
            }

            b2ChainShape chainShape = b2GetChainShape(world, chainId);

            int materialCount = chainShape.materialCount;
            for (int i = 0; i < materialCount; ++i)
            {
                chainShape.materials[i].friction = friction;
            }

            int count = chainShape.count;

            for (int i = 0; i < count; ++i)
            {
                int shapeId = chainShape.shapeIndices[i];
                b2Shape shape = b2Array_Get(world.shapes, shapeId);
                shape.friction = friction;
            }
        }

        public static float b2Chain_GetFriction(b2ChainId chainId)
        {
            b2World world = b2GetWorld(chainId.world0);
            b2ChainShape chainShape = b2GetChainShape(world, chainId);
            return chainShape.materials[0].friction;
        }

        public static void b2Chain_SetRestitution(b2ChainId chainId, float restitution)
        {
            Debug.Assert(b2IsValidFloat(restitution));

            b2World world = b2GetWorldLocked(chainId.world0);
            if (world == null)
            {
                return;
            }

            b2ChainShape chainShape = b2GetChainShape(world, chainId);

            int materialCount = chainShape.materialCount;
            for (int i = 0; i < materialCount; ++i)
            {
                chainShape.materials[i].restitution = restitution;
            }

            int count = chainShape.count;

            for (int i = 0; i < count; ++i)
            {
                int shapeId = chainShape.shapeIndices[i];
                b2Shape shape = b2Array_Get(world.shapes, shapeId);
                shape.restitution = restitution;
            }
        }

        public static float b2Chain_GetRestitution(b2ChainId chainId)
        {
            b2World world = b2GetWorld(chainId.world0);
            b2ChainShape chainShape = b2GetChainShape(world, chainId);
            return chainShape.materials[0].restitution;
        }

        public static void b2Chain_SetMaterial(b2ChainId chainId, int material)
        {
            b2World world = b2GetWorldLocked(chainId.world0);
            if (world == null)
            {
                return;
            }

            b2ChainShape chainShape = b2GetChainShape(world, chainId);
            int materialCount = chainShape.materialCount;
            for (int i = 0; i < materialCount; ++i)
            {
                chainShape.materials[i].material = material;
            }

            int count = chainShape.count;

            for (int i = 0; i < count; ++i)
            {
                int shapeId = chainShape.shapeIndices[i];
                b2Shape shape = b2Array_Get(world.shapes, shapeId);
                shape.material = material;
            }
        }

        public static int b2Chain_GetMaterial(b2ChainId chainId)
        {
            b2World world = b2GetWorld(chainId.world0);
            b2ChainShape chainShape = b2GetChainShape(world, chainId);
            return chainShape.materials[0].material;
        }

        public static int b2Shape_GetContactCapacity(b2ShapeId shapeId)
        {
            b2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return 0;
            }

            b2Shape shape = b2GetShape(world, shapeId);
            if (shape.sensorIndex != B2_NULL_INDEX)
            {
                return 0;
            }

            b2Body body = b2Array_Get(world.bodies, shape.bodyId);

            // Conservative and fast
            return body.contactCount;
        }

        public static int b2Shape_GetContactData(b2ShapeId shapeId, b2ContactData[] contactData, int capacity)
        {
            b2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return 0;
            }

            b2Shape shape = b2GetShape(world, shapeId);
            if (shape.sensorIndex != B2_NULL_INDEX)
            {
                return 0;
            }

            b2Body body = b2Array_Get(world.bodies, shape.bodyId);
            int contactKey = body.headContactKey;
            int index = 0;
            while (contactKey != B2_NULL_INDEX && index < capacity)
            {
                int contactId = contactKey >> 1;
                int edgeIndex = contactKey & 1;

                b2Contact contact = b2Array_Get(world.contacts, contactId);

                // Does contact involve this shape and is it touching?
                if ((contact.shapeIdA == shapeId.index1 - 1 || contact.shapeIdB == shapeId.index1 - 1) &&
                    (contact.flags & (int)b2ContactFlags.b2_contactTouchingFlag) != 0)
                {
                    b2Shape shapeA = world.shapes.data[contact.shapeIdA];
                    b2Shape shapeB = world.shapes.data[contact.shapeIdB];

                    contactData[index].shapeIdA = new b2ShapeId(shapeA.id + 1, shapeId.world0, shapeA.generation);
                    contactData[index].shapeIdB = new b2ShapeId(shapeB.id + 1, shapeId.world0, shapeB.generation);

                    b2ContactSim contactSim = b2GetContactSim(world, contact);
                    contactData[index].manifold = contactSim.manifold;
                    index += 1;
                }

                contactKey = contact.edges[edgeIndex].nextKey;
            }

            Debug.Assert(index <= capacity);

            return index;
        }

        public static int b2Shape_GetSensorCapacity(b2ShapeId shapeId)
        {
            b2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return 0;
            }

            b2Shape shape = b2GetShape(world, shapeId);
            if (shape.sensorIndex == B2_NULL_INDEX)
            {
                return 0;
            }

            b2Sensor sensor = b2Array_Get(world.sensors, shape.sensorIndex);
            return sensor.overlaps2.count;
        }

        public static int b2Shape_GetSensorOverlaps(b2ShapeId shapeId, b2ShapeId[] overlaps, int capacity)
        {
            b2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return 0;
            }

            b2Shape shape = b2GetShape(world, shapeId);
            if (shape.sensorIndex == B2_NULL_INDEX)
            {
                return 0;
            }

            b2Sensor sensor = b2Array_Get(world.sensors, shape.sensorIndex);

            int count = b2MinInt(sensor.overlaps2.count, capacity);
            b2ShapeRef[] refs = sensor.overlaps2.data;
            for (int i = 0; i < count; ++i)
            {
                overlaps[i] = new b2ShapeId(refs[i].shapeId + 1, shapeId.world0, refs[i].generation);
            }

            return count;
        }

        public static b2AABB b2Shape_GetAABB(b2ShapeId shapeId)
        {
            b2World world = b2GetWorld(shapeId.world0);
            if (world == null)
            {
                return new b2AABB();
            }

            b2Shape shape = b2GetShape(world, shapeId);
            return shape.aabb;
        }

        public static b2MassData b2Shape_GetMassData(b2ShapeId shapeId)
        {
            b2World world = b2GetWorld(shapeId.world0);
            if (world == null)
            {
                return new b2MassData();
            }

            b2Shape shape = b2GetShape(world, shapeId);
            return b2ComputeShapeMass(shape);
        }

        public static b2Vec2 b2Shape_GetClosestPoint(b2ShapeId shapeId, b2Vec2 target)
        {
            b2World world = b2GetWorld(shapeId.world0);
            if (world == null)
            {
                return new b2Vec2();
            }

            b2Shape shape = b2GetShape(world, shapeId);
            b2Body body = b2Array_Get(world.bodies, shape.bodyId);
            b2Transform transform = b2GetBodyTransformQuick(world, body);

            b2DistanceInput input = new b2DistanceInput();
            input.proxyA = b2MakeShapeDistanceProxy(shape);
            input.proxyB = b2MakeProxy([target], 1, 0.0f);
            input.transformA = transform;
            input.transformB = b2Transform_identity;
            input.useRadii = true;

            b2SimplexCache cache = new b2SimplexCache();
            b2DistanceOutput output = b2ShapeDistance(cache, input, null, 0);

            return output.pointA;
        }
    }
}