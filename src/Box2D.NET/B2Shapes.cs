﻿// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using Box2D.NET.Core;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Cores;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Contacts;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2IdPools;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2BoardPhases;
using static Box2D.NET.B2Distances;

namespace Box2D.NET
{
    public static class B2Shapes
    {
        public static float b2GetShapeRadius(B2Shape shape)
        {
            switch (shape.type)
            {
                case B2ShapeType.b2_capsuleShape:
                    return shape.capsule.radius;
                case B2ShapeType.b2_circleShape:
                    return shape.circle.radius;
                case B2ShapeType.b2_polygonShape:
                    return shape.polygon.radius;
                default:
                    return 0.0f;
            }
        }

        public static B2Shape b2GetShape(B2World world, B2ShapeId shapeId)
        {
            int id = shapeId.index1 - 1;
            B2Shape shape = b2Array_Get(ref world.shapes, id);
            Debug.Assert(shape.id == id && shape.generation == shapeId.generation);
            return shape;
        }

        public static B2ChainShape b2GetChainShape(B2World world, B2ChainId chainId)
        {
            int id = chainId.index1 - 1;
            B2ChainShape chain = b2Array_Get(ref world.chainShapes, id);
            Debug.Assert(chain.id == id && chain.generation == chainId.generation);
            return chain;
        }

        public static void b2UpdateShapeAABBs(B2Shape shape, B2Transform transform, B2BodyType proxyType)
        {
            // Compute a bounding box with a speculative margin
            float speculativeDistance = B2_SPECULATIVE_DISTANCE;
            float aabbMargin = B2_AABB_MARGIN;

            B2AABB aabb = b2ComputeShapeAABB(shape, transform);
            aabb.lowerBound.x -= speculativeDistance;
            aabb.lowerBound.y -= speculativeDistance;
            aabb.upperBound.x += speculativeDistance;
            aabb.upperBound.y += speculativeDistance;
            shape.aabb = aabb;

            // Smaller margin for static bodies. Cannot be zero due to TOI tolerance.
            float margin = proxyType == B2BodyType.b2_staticBody ? speculativeDistance : aabbMargin;
            B2AABB fatAABB;
            fatAABB.lowerBound.x = aabb.lowerBound.x - margin;
            fatAABB.lowerBound.y = aabb.lowerBound.y - margin;
            fatAABB.upperBound.x = aabb.upperBound.x + margin;
            fatAABB.upperBound.y = aabb.upperBound.y + margin;
            shape.fatAABB = fatAABB;
        }

        public static B2Shape b2CreateShapeInternal<T>(B2World world, B2Body body, B2Transform transform, ref B2ShapeDef def, ref T geometry, B2ShapeType shapeType)
        {
            Debug.Assert(b2IsValidFloat(def.density) && def.density >= 0.0f);
            Debug.Assert(b2IsValidFloat(def.friction) && def.friction >= 0.0f);
            Debug.Assert(b2IsValidFloat(def.restitution) && def.restitution >= 0.0f);

            int shapeId = b2AllocId(world.shapeIdPool);

            if (shapeId == world.shapes.count)
            {
                b2Array_Push(ref world.shapes, new B2Shape());
            }
            else
            {
                Debug.Assert(world.shapes.data[shapeId].id == B2_NULL_INDEX);
            }

            B2Shape shape = b2Array_Get(ref world.shapes, shapeId);
            
            switch (geometry)
            {
                case B2Capsule capsule:
                    shape.capsule = capsule.Clone();
                    break;

                case B2Circle circle:
                    shape.circle = circle.Clone();
                    break;

                case B2Polygon polygon:
                    shape.polygon = polygon.Clone();
                    break;

                case B2Segment segment:
                    shape.segment = segment.Clone();
                    break;

                case B2ChainSegment chainSegment:
                    shape.chainSegment = chainSegment.Clone();
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
            shape.aabb = new B2AABB(b2Vec2_zero, b2Vec2_zero);
            shape.fatAABB = new B2AABB(b2Vec2_zero, b2Vec2_zero);
            shape.generation += 1;

            if (body.setIndex != (int)B2SetType.b2_disabledSet)
            {
                B2BodyType proxyType = body.type;
                b2CreateShapeProxy(shape, world.broadPhase, proxyType, transform, def.invokeContactCreation || def.isSensor);
            }

            // Add to shape doubly linked list
            if (body.headShapeId != B2_NULL_INDEX)
            {
                B2Shape headShape = b2Array_Get(ref world.shapes, body.headShapeId);
                headShape.prevShapeId = shapeId;
            }

            shape.prevShapeId = B2_NULL_INDEX;
            shape.nextShapeId = body.headShapeId;
            body.headShapeId = shapeId;
            body.shapeCount += 1;

            if (def.isSensor)
            {
                shape.sensorIndex = world.sensors.count;
                B2Sensor sensor = new B2Sensor()
                {
                    overlaps1 = b2Array_Create<B2ShapeRef>(16),
                    overlaps2 = b2Array_Create<B2ShapeRef>(16),
                    shapeId = shapeId,
                };
                b2Array_Push(ref world.sensors, sensor);
            }
            else
            {
                shape.sensorIndex = B2_NULL_INDEX;
            }

            b2ValidateSolverSets(world);

            return shape;
        }

        public static B2ShapeId b2CreateShape<T>(B2BodyId bodyId, ref B2ShapeDef def, ref T geometry, B2ShapeType shapeType)
        {
            B2_CHECK_DEF(ref def);
            Debug.Assert(b2IsValidFloat(def.density) && def.density >= 0.0f);
            Debug.Assert(b2IsValidFloat(def.friction) && def.friction >= 0.0f);
            Debug.Assert(b2IsValidFloat(def.restitution) && def.restitution >= 0.0f);

            B2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return new B2ShapeId();
            }

            B2Body body = b2GetBodyFullId(world, bodyId);
            B2Transform transform = b2GetBodyTransformQuick(world, body);

            B2Shape shape = b2CreateShapeInternal(world, body, transform, ref def, ref geometry, shapeType);

            if (def.updateBodyMass == true)
            {
                b2UpdateBodyMassData(world, body);
            }

            b2ValidateSolverSets(world);

            B2ShapeId id = new B2ShapeId(shape.id + 1, bodyId.world0, shape.generation);
            return id;
        }

        public static B2ShapeId b2CreateCircleShape(B2BodyId bodyId, ref B2ShapeDef def, ref B2Circle circle)
        {
            return b2CreateShape(bodyId, ref def, ref circle, B2ShapeType.b2_circleShape);
        }

        public static B2ShapeId b2CreateCapsuleShape(B2BodyId bodyId, ref B2ShapeDef def, ref B2Capsule capsule)
        {
            float lengthSqr = b2DistanceSquared(capsule.center1, capsule.center2);
            if (lengthSqr <= B2_LINEAR_SLOP * B2_LINEAR_SLOP)
            {
                B2Circle circle = new B2Circle(b2Lerp(capsule.center1, capsule.center2, 0.5f), capsule.radius);
                return b2CreateShape(bodyId, ref def, ref circle, B2ShapeType.b2_circleShape);
            }

            return b2CreateShape(bodyId, ref def, ref capsule, B2ShapeType.b2_capsuleShape);
        }

        public static B2ShapeId b2CreatePolygonShape(B2BodyId bodyId, ref B2ShapeDef def, ref B2Polygon polygon)
        {
            Debug.Assert(b2IsValidFloat(polygon.radius) && polygon.radius >= 0.0f);
            return b2CreateShape(bodyId, ref def, ref polygon, B2ShapeType.b2_polygonShape);
        }

        public static B2ShapeId b2CreateSegmentShape(B2BodyId bodyId, ref B2ShapeDef def, ref B2Segment segment)
        {
            float lengthSqr = b2DistanceSquared(segment.point1, segment.point2);
            if (lengthSqr <= B2_LINEAR_SLOP * B2_LINEAR_SLOP)
            {
                Debug.Assert(false);
                return b2_nullShapeId;
            }

            return b2CreateShape(bodyId, ref def, ref segment, B2ShapeType.b2_segmentShape);
        }

// Destroy a shape on a body. This doesn't need to be called when destroying a body.
        public static void b2DestroyShapeInternal(B2World world, B2Shape shape, B2Body body, bool wakeBodies)
        {
            int shapeId = shape.id;

            // Remove the shape from the body's doubly linked list.
            if (shape.prevShapeId != B2_NULL_INDEX)
            {
                B2Shape prevShape = b2Array_Get(ref world.shapes, shape.prevShapeId);
                prevShape.nextShapeId = shape.nextShapeId;
            }

            if (shape.nextShapeId != B2_NULL_INDEX)
            {
                B2Shape nextShape = b2Array_Get(ref world.shapes, shape.nextShapeId);
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

                B2Contact contact = b2Array_Get(ref world.contacts, contactId);
                contactKey = contact.edges[edgeIndex].nextKey;

                if (contact.shapeIdA == shapeId || contact.shapeIdB == shapeId)
                {
                    b2DestroyContact(world, contact, wakeBodies);
                }
            }

            if (shape.sensorIndex != B2_NULL_INDEX)
            {
                B2Sensor sensor = b2Array_Get(ref world.sensors, shape.sensorIndex);
                for (int i = 0; i < sensor.overlaps2.count; ++i)
                {
                    B2ShapeRef @ref = sensor.overlaps2.data[i];
                    B2SensorEndTouchEvent @event = new B2SensorEndTouchEvent()
                    {
                        sensorShapeId = new B2ShapeId(shapeId + 1, world.worldId, shape.generation),
                        visitorShapeId = new B2ShapeId(@ref.shapeId + 1, world.worldId, @ref.generation),
                    };

                    b2Array_Push(ref world.sensorEndEvents[world.endEventArrayIndex], @event);
                }

                // Destroy sensor
                b2Array_Destroy(ref sensor.overlaps1);
                b2Array_Destroy(ref sensor.overlaps2);

                int movedIndex = b2Array_RemoveSwap(ref world.sensors, shape.sensorIndex);
                if (movedIndex != B2_NULL_INDEX)
                {
                    // Fixup moved sensor
                    B2Sensor movedSensor = b2Array_Get(ref world.sensors, shape.sensorIndex);
                    B2Shape otherSensorShape = b2Array_Get(ref world.shapes, movedSensor.shapeId);
                    otherSensorShape.sensorIndex = shape.sensorIndex;
                }
            }

            // Return shape to free list.
            b2FreeId(world.shapeIdPool, shapeId);
            shape.id = B2_NULL_INDEX;

            b2ValidateSolverSets(world);
        }

        public static void b2DestroyShape(B2ShapeId shapeId, bool updateBodyMass)
        {
            B2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return;
            }

            B2Shape shape = b2GetShape(world, shapeId);

            // need to wake bodies because this might be a static body
            bool wakeBodies = true;

            B2Body body = b2Array_Get(ref world.bodies, shape.bodyId);
            b2DestroyShapeInternal(world, shape, body, wakeBodies);

            if (updateBodyMass == true)
            {
                b2UpdateBodyMassData(world, body);
            }
        }

        public static B2ChainId b2CreateChain(B2BodyId bodyId, ref B2ChainDef def)
        {
            B2_CHECK_DEF(ref def);
            Debug.Assert(def.count >= 4);
            Debug.Assert(def.materialCount == 1 || def.materialCount == def.count);

            B2World world = b2GetWorldLocked(bodyId.world0);
            if (world == null)
            {
                return new B2ChainId();
            }

            B2Body body = b2GetBodyFullId(world, bodyId);
            B2Transform transform = b2GetBodyTransformQuick(world, body);

            int chainId = b2AllocId(world.chainIdPool);

            if (chainId == world.chainShapes.count)
            {
                b2Array_Push(ref world.chainShapes, new B2ChainShape());
            }
            else
            {
                Debug.Assert(world.chainShapes.data[chainId].id == B2_NULL_INDEX);
            }

            B2ChainShape chainShape = b2Array_Get(ref world.chainShapes, chainId);

            chainShape.id = chainId;
            chainShape.bodyId = body.id;
            chainShape.nextChainId = body.headChainId;
            chainShape.generation += 1;

            int materialCount = def.materialCount;
            chainShape.materialCount = materialCount;
            chainShape.materials = b2Alloc<B2SurfaceMaterial>(materialCount);

            for (int i = 0; i < materialCount; ++i)
            {
                ref B2SurfaceMaterial material = ref def.materials[i];
                Debug.Assert(b2IsValidFloat(material.friction) && material.friction >= 0.0f);
                Debug.Assert(b2IsValidFloat(material.restitution) && material.restitution >= 0.0f);
                Debug.Assert(b2IsValidFloat(material.rollingResistance) && material.rollingResistance >= 0.0f);
                Debug.Assert(b2IsValidFloat(material.tangentSpeed));

                chainShape.materials[i] = material;
            }

            body.headChainId = chainId;

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.userData = def.userData;
            shapeDef.filter = def.filter;
            shapeDef.enableContactEvents = false;
            shapeDef.enableHitEvents = false;

            B2Vec2[] points = def.points;
            int n = def.count;

            if (def.isLoop)
            {
                chainShape.count = n;
                chainShape.shapeIndices = b2Alloc<int>(chainShape.count);

                B2ChainSegment chainSegment = new B2ChainSegment();

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
                    ref B2SurfaceMaterial material = ref def.materials[materialIndex];
                    shapeDef.friction = material.friction;
                    shapeDef.restitution = material.restitution;
                    shapeDef.rollingResistance = material.rollingResistance;
                    shapeDef.tangentSpeed = material.tangentSpeed;
                    shapeDef.customColor = material.customColor;
                    shapeDef.material = material.material;

                    B2Shape shape = b2CreateShapeInternal(world, body, transform, ref shapeDef, ref chainSegment, B2ShapeType.b2_chainSegmentShape);
                    chainShape.shapeIndices[i] = shape.id;
                }

                {
                    chainSegment.ghost1 = points[n - 3];
                    chainSegment.segment.point1 = points[n - 2];
                    chainSegment.segment.point2 = points[n - 1];
                    chainSegment.ghost2 = points[0];
                    chainSegment.chainId = chainId;

                    int materialIndex = materialCount == 1 ? 0 : n - 2;
                    ref B2SurfaceMaterial material = ref def.materials[materialIndex];
                    shapeDef.friction = material.friction;
                    shapeDef.restitution = material.restitution;
                    shapeDef.rollingResistance = material.rollingResistance;
                    shapeDef.tangentSpeed = material.tangentSpeed;
                    shapeDef.customColor = material.customColor;
                    shapeDef.material = material.material;

                    B2Shape shape = b2CreateShapeInternal(world, body, transform, ref shapeDef, ref chainSegment, B2ShapeType.b2_chainSegmentShape);
                    chainShape.shapeIndices[n - 2] = shape.id;
                }

                {
                    chainSegment.ghost1 = points[n - 2];
                    chainSegment.segment.point1 = points[n - 1];
                    chainSegment.segment.point2 = points[0];
                    chainSegment.ghost2 = points[1];
                    chainSegment.chainId = chainId;

                    int materialIndex = materialCount == 1 ? 0 : n - 1;
                    ref B2SurfaceMaterial material = ref def.materials[materialIndex];
                    shapeDef.friction = material.friction;
                    shapeDef.restitution = material.restitution;
                    shapeDef.rollingResistance = material.rollingResistance;
                    shapeDef.tangentSpeed = material.tangentSpeed;
                    shapeDef.customColor = material.customColor;
                    shapeDef.material = material.material;

                    B2Shape shape = b2CreateShapeInternal(world, body, transform, ref shapeDef, ref chainSegment, B2ShapeType.b2_chainSegmentShape);
                    chainShape.shapeIndices[n - 1] = shape.id;
                }
            }
            else
            {
                chainShape.count = n - 3;
                chainShape.shapeIndices = b2Alloc<int>(chainShape.count);

                B2ChainSegment chainSegment = new B2ChainSegment();

                for (int i = 0; i < n - 3; ++i)
                {
                    chainSegment.ghost1 = points[i];
                    chainSegment.segment.point1 = points[i + 1];
                    chainSegment.segment.point2 = points[i + 2];
                    chainSegment.ghost2 = points[i + 3];
                    chainSegment.chainId = chainId;

                    // Material is associated with leading point of solid segment
                    int materialIndex = materialCount == 1 ? 0 : i + 1;
                    ref B2SurfaceMaterial material = ref def.materials[materialIndex];
                    shapeDef.friction = material.friction;
                    shapeDef.restitution = material.restitution;
                    shapeDef.rollingResistance = material.rollingResistance;
                    shapeDef.tangentSpeed = material.tangentSpeed;
                    shapeDef.customColor = material.customColor;
                    shapeDef.material = material.material;

                    B2Shape shape = b2CreateShapeInternal(world, body, transform, ref shapeDef, ref chainSegment, B2ShapeType.b2_chainSegmentShape);
                    chainShape.shapeIndices[i] = shape.id;
                }
            }

            B2ChainId id = new B2ChainId(chainId + 1, world.worldId, chainShape.generation);
            return id;
        }

        public static void b2FreeChainData(B2ChainShape chain)
        {
            b2Free(chain.shapeIndices, chain.count);
            chain.shapeIndices = null;

            b2Free(chain.materials, chain.materialCount);
            chain.materials = null;
        }

        public static void b2DestroyChain(B2ChainId chainId)
        {
            B2World world = b2GetWorldLocked(chainId.world0);
            if (world == null)
            {
                return;
            }

            B2ChainShape chain = b2GetChainShape(world, chainId);
            bool wakeBodies = true;

            B2Body body = b2Array_Get(ref world.bodies, chain.bodyId);

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
                B2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                b2DestroyShapeInternal(world, shape, body, wakeBodies);
            }

            b2FreeChainData(chain);

            // Return chain to free list.
            b2FreeId(world.chainIdPool, chain.id);
            chain.id = B2_NULL_INDEX;

            b2ValidateSolverSets(world);
        }

        public static B2WorldId b2Chain_GetWorld(B2ChainId chainId)
        {
            B2World world = b2GetWorld(chainId.world0);
            return new B2WorldId((ushort)(chainId.world0 + 1), world.generation);
        }

        public static int b2Chain_GetSegmentCount(B2ChainId chainId)
        {
            B2World world = b2GetWorldLocked(chainId.world0);
            if (world == null)
            {
                return 0;
            }

            B2ChainShape chain = b2GetChainShape(world, chainId);
            return chain.count;
        }

        public static int b2Chain_GetSegments(B2ChainId chainId, B2ShapeId[] segmentArray, int capacity)
        {
            B2World world = b2GetWorldLocked(chainId.world0);
            if (world == null)
            {
                return 0;
            }

            B2ChainShape chain = b2GetChainShape(world, chainId);

            int count = b2MinInt(chain.count, capacity);
            for (int i = 0; i < count; ++i)
            {
                int shapeId = chain.shapeIndices[i];
                B2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                segmentArray[i] = new B2ShapeId(shapeId + 1, chainId.world0, shape.generation);
            }

            return count;
        }

        public static B2AABB b2ComputeShapeAABB(B2Shape shape, B2Transform xf)
        {
            switch (shape.type)
            {
                case B2ShapeType.b2_capsuleShape:
                    return b2ComputeCapsuleAABB(shape.capsule, xf);
                case B2ShapeType.b2_circleShape:
                    return b2ComputeCircleAABB(shape.circle, xf);
                case B2ShapeType.b2_polygonShape:
                    return b2ComputePolygonAABB(shape.polygon, xf);
                case B2ShapeType.b2_segmentShape:
                    return b2ComputeSegmentAABB(shape.segment, xf);
                case B2ShapeType.b2_chainSegmentShape:
                    return b2ComputeSegmentAABB(shape.chainSegment.segment, xf);
                default:
                {
                    Debug.Assert(false);
                    B2AABB empty = new B2AABB(xf.p, xf.p);
                    return empty;
                }
            }
        }

        public static B2Vec2 b2GetShapeCentroid(B2Shape shape)
        {
            switch (shape.type)
            {
                case B2ShapeType.b2_capsuleShape:
                    return b2Lerp(shape.capsule.center1, shape.capsule.center2, 0.5f);
                case B2ShapeType.b2_circleShape:
                    return shape.circle.center;
                case B2ShapeType.b2_polygonShape:
                    return shape.polygon.centroid;
                case B2ShapeType.b2_segmentShape:
                    return b2Lerp(shape.segment.point1, shape.segment.point2, 0.5f);
                case B2ShapeType.b2_chainSegmentShape:
                    return b2Lerp(shape.chainSegment.segment.point1, shape.chainSegment.segment.point2, 0.5f);
                default:
                    return b2Vec2_zero;
            }
        }

// todo_erin maybe compute this on shape creation
        public static float b2GetShapePerimeter(B2Shape shape)
        {
            switch (shape.type)
            {
                case B2ShapeType.b2_capsuleShape:
                    return 2.0f * b2Length(b2Sub(shape.capsule.center1, shape.capsule.center2)) +
                           2.0f * B2_PI * shape.capsule.radius;
                case B2ShapeType.b2_circleShape:
                    return 2.0f * B2_PI * shape.circle.radius;
                case B2ShapeType.b2_polygonShape:
                {
                    ReadOnlySpan<B2Vec2> points = shape.polygon.vertices.AsSpan();
                    int count = shape.polygon.count;
                    float perimeter = 2.0f * B2_PI * shape.polygon.radius;
                    Debug.Assert(count > 0);
                    B2Vec2 prev = points[count - 1];
                    for (int i = 0; i < count; ++i)
                    {
                        B2Vec2 next = points[i];
                        perimeter += b2Length(b2Sub(next, prev));
                        prev = next;
                    }

                    return perimeter;
                }
                case B2ShapeType.b2_segmentShape:
                    return 2.0f * b2Length(b2Sub(shape.segment.point1, shape.segment.point2));
                case B2ShapeType.b2_chainSegmentShape:
                    return 2.0f * b2Length(b2Sub(shape.chainSegment.segment.point1, shape.chainSegment.segment.point2));
                default:
                    return 0.0f;
            }
        }

// This projects the shape perimeter onto an infinite line
        public static float b2GetShapeProjectedPerimeter(B2Shape shape, B2Vec2 line)
        {
            switch (shape.type)
            {
                case B2ShapeType.b2_capsuleShape:
                {
                    B2Vec2 axis = b2Sub(shape.capsule.center2, shape.capsule.center1);
                    float projectedLength = b2AbsFloat(b2Dot(axis, line));
                    return projectedLength + 2.0f * shape.capsule.radius;
                }

                case B2ShapeType.b2_circleShape:
                    return 2.0f * shape.circle.radius;

                case B2ShapeType.b2_polygonShape:
                {
                    ReadOnlySpan<B2Vec2> points = shape.polygon.vertices.AsSpan();
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

                case B2ShapeType.b2_segmentShape:
                {
                    float value1 = b2Dot(shape.segment.point1, line);
                    float value2 = b2Dot(shape.segment.point2, line);
                    return b2AbsFloat(value2 - value1);
                }

                case B2ShapeType.b2_chainSegmentShape:
                {
                    float value1 = b2Dot(shape.chainSegment.segment.point1, line);
                    float value2 = b2Dot(shape.chainSegment.segment.point2, line);
                    return b2AbsFloat(value2 - value1);
                }

                default:
                    return 0.0f;
            }
        }

        public static B2MassData b2ComputeShapeMass(B2Shape shape)
        {
            switch (shape.type)
            {
                case B2ShapeType.b2_capsuleShape:
                    return b2ComputeCapsuleMass(shape.capsule, shape.density);
                case B2ShapeType.b2_circleShape:
                    return b2ComputeCircleMass(shape.circle, shape.density);
                case B2ShapeType.b2_polygonShape:
                    return b2ComputePolygonMass(shape.polygon, shape.density);
                default:
                    return new B2MassData();
            }
        }

        public static B2ShapeExtent b2ComputeShapeExtent(B2Shape shape, B2Vec2 localCenter)
        {
            B2ShapeExtent extent = new B2ShapeExtent();

            switch (shape.type)
            {
                case B2ShapeType.b2_capsuleShape:
                {
                    float radius = shape.capsule.radius;
                    extent.minExtent = radius;
                    B2Vec2 c1 = b2Sub(shape.capsule.center1, localCenter);
                    B2Vec2 c2 = b2Sub(shape.capsule.center2, localCenter);
                    extent.maxExtent = MathF.Sqrt(b2MaxFloat(b2LengthSquared(c1), b2LengthSquared(c2))) + radius;
                }
                    break;

                case B2ShapeType.b2_circleShape:
                {
                    float radius = shape.circle.radius;
                    extent.minExtent = radius;
                    extent.maxExtent = b2Length(b2Sub(shape.circle.center, localCenter)) + radius;
                }
                    break;

                case B2ShapeType.b2_polygonShape:
                {
                    B2Polygon poly = shape.polygon;
                    float minExtent = B2_HUGE;
                    float maxExtentSqr = 0.0f;
                    int count = poly.count;
                    for (int i = 0; i < count; ++i)
                    {
                        B2Vec2 v = poly.vertices[i];
                        float planeOffset = b2Dot(poly.normals[i], b2Sub(v, poly.centroid));
                        minExtent = b2MinFloat(minExtent, planeOffset);

                        float distanceSqr = b2LengthSquared(b2Sub(v, localCenter));
                        maxExtentSqr = b2MaxFloat(maxExtentSqr, distanceSqr);
                    }

                    extent.minExtent = minExtent + poly.radius;
                    extent.maxExtent = MathF.Sqrt(maxExtentSqr) + poly.radius;
                }
                    break;

                case B2ShapeType.b2_segmentShape:
                {
                    extent.minExtent = 0.0f;
                    B2Vec2 c1 = b2Sub(shape.segment.point1, localCenter);
                    B2Vec2 c2 = b2Sub(shape.segment.point2, localCenter);
                    extent.maxExtent = MathF.Sqrt(b2MaxFloat(b2LengthSquared(c1), b2LengthSquared(c2)));
                }
                    break;

                case B2ShapeType.b2_chainSegmentShape:
                {
                    extent.minExtent = 0.0f;
                    B2Vec2 c1 = b2Sub(shape.chainSegment.segment.point1, localCenter);
                    B2Vec2 c2 = b2Sub(shape.chainSegment.segment.point2, localCenter);
                    extent.maxExtent = MathF.Sqrt(b2MaxFloat(b2LengthSquared(c1), b2LengthSquared(c2)));
                }
                    break;

                default:
                    break;
            }

            return extent;
        }

        public static B2CastOutput b2RayCastShape(ref B2RayCastInput input, B2Shape shape, B2Transform transform)
        {
            B2RayCastInput localInput = input;
            localInput.origin = b2InvTransformPoint(transform, input.origin);
            localInput.translation = b2InvRotateVector(transform.q, input.translation);

            B2CastOutput output = new B2CastOutput();
            switch (shape.type)
            {
                case B2ShapeType.b2_capsuleShape:
                    output = b2RayCastCapsule(ref localInput, shape.capsule);
                    break;
                case B2ShapeType.b2_circleShape:
                    output = b2RayCastCircle(ref localInput, shape.circle);
                    break;
                case B2ShapeType.b2_polygonShape:
                    output = b2RayCastPolygon(ref localInput, shape.polygon);
                    break;
                case B2ShapeType.b2_segmentShape:
                    output = b2RayCastSegment(ref localInput, shape.segment, false);
                    break;
                case B2ShapeType.b2_chainSegmentShape:
                    output = b2RayCastSegment(ref localInput, shape.chainSegment.segment, true);
                    break;
                default:
                    return output;
            }

            output.point = b2TransformPoint(ref transform, output.point);
            output.normal = b2RotateVector(transform.q, output.normal);
            return output;
        }

        public static B2CastOutput b2ShapeCastShape(ref B2ShapeCastInput input, B2Shape shape, B2Transform transform)
        {
            B2ShapeCastInput localInput = input;

            for (int i = 0; i < localInput.count; ++i)
            {
                localInput.points[i] = b2InvTransformPoint(transform, input.points[i]);
            }

            localInput.translation = b2InvRotateVector(transform.q, input.translation);

            B2CastOutput output = new B2CastOutput();
            switch (shape.type)
            {
                case B2ShapeType.b2_capsuleShape:
                    output = b2ShapeCastCapsule(ref localInput, shape.capsule);
                    break;
                case B2ShapeType.b2_circleShape:
                    output = b2ShapeCastCircle(ref localInput, shape.circle);
                    break;
                case B2ShapeType.b2_polygonShape:
                    output = b2ShapeCastPolygon(ref localInput, shape.polygon);
                    break;
                case B2ShapeType.b2_segmentShape:
                    output = b2ShapeCastSegment(ref localInput, shape.segment);
                    break;
                case B2ShapeType.b2_chainSegmentShape:
                    output = b2ShapeCastSegment(ref localInput, shape.chainSegment.segment);
                    break;
                default:
                    return output;
            }

            output.point = b2TransformPoint(ref transform, output.point);
            output.normal = b2RotateVector(transform.q, output.normal);
            return output;
        }

        public static void b2CreateShapeProxy(B2Shape shape, B2BroadPhase bp, B2BodyType type, B2Transform transform, bool forcePairCreation)
        {
            Debug.Assert(shape.proxyKey == B2_NULL_INDEX);

            b2UpdateShapeAABBs(shape, transform, type);

            // Create proxies in the broad-phase.
            shape.proxyKey =
                b2BroadPhase_CreateProxy(bp, type, shape.fatAABB, shape.filter.categoryBits, shape.id, forcePairCreation);
            Debug.Assert(B2_PROXY_TYPE(shape.proxyKey) < B2BodyType.b2_bodyTypeCount);
        }

        public static void b2DestroyShapeProxy(B2Shape shape, B2BroadPhase bp)
        {
            if (shape.proxyKey != B2_NULL_INDEX)
            {
                b2BroadPhase_DestroyProxy(bp, shape.proxyKey);
                shape.proxyKey = B2_NULL_INDEX;
            }
        }

        public static B2ShapeProxy b2MakeShapeDistanceProxy(B2Shape shape)
        {
            switch (shape.type)
            {
                case B2ShapeType.b2_capsuleShape:
                    return b2MakeProxy(shape.capsule.center1, shape.capsule.center2, 2, shape.capsule.radius);
                case B2ShapeType.b2_circleShape:
                    return b2MakeProxy(shape.circle.center, 1, shape.circle.radius);
                case B2ShapeType.b2_polygonShape:
                    return b2MakeProxy(shape.polygon.vertices.AsSpan(), shape.polygon.count, shape.polygon.radius);
                case B2ShapeType.b2_segmentShape:
                    return b2MakeProxy(shape.segment.point1, shape.segment.point2, 2, 0.0f);
                case B2ShapeType.b2_chainSegmentShape:
                    return b2MakeProxy(shape.chainSegment.segment.point1, shape.chainSegment.segment.point2, 2, 0.0f);
                default:
                {
                    Debug.Assert(false);
                    B2ShapeProxy empty = new B2ShapeProxy();
                    return empty;
                }
            }
        }

        public static B2BodyId b2Shape_GetBody(B2ShapeId shapeId)
        {
            B2World world = b2GetWorld(shapeId.world0);
            B2Shape shape = b2GetShape(world, shapeId);
            return b2MakeBodyId(world, shape.bodyId);
        }

        public static B2WorldId b2Shape_GetWorld(B2ShapeId shapeId)
        {
            B2World world = b2GetWorld(shapeId.world0);
            return new B2WorldId((ushort)(shapeId.world0 + 1), world.generation);
        }

        public static void b2Shape_SetUserData(B2ShapeId shapeId, object userData)
        {
            B2World world = b2GetWorld(shapeId.world0);
            B2Shape shape = b2GetShape(world, shapeId);
            shape.userData = userData;
        }

        public static object b2Shape_GetUserData(B2ShapeId shapeId)
        {
            B2World world = b2GetWorld(shapeId.world0);
            B2Shape shape = b2GetShape(world, shapeId);
            return shape.userData;
        }

        public static bool b2Shape_IsSensor(B2ShapeId shapeId)
        {
            B2World world = b2GetWorld(shapeId.world0);
            B2Shape shape = b2GetShape(world, shapeId);
            return shape.sensorIndex != B2_NULL_INDEX;
        }

        public static bool b2Shape_TestPoint(B2ShapeId shapeId, B2Vec2 point)
        {
            B2World world = b2GetWorld(shapeId.world0);
            B2Shape shape = b2GetShape(world, shapeId);

            B2Transform transform = b2GetBodyTransform(world, shape.bodyId);
            B2Vec2 localPoint = b2InvTransformPoint(transform, point);

            switch (shape.type)
            {
                case B2ShapeType.b2_capsuleShape:
                    return b2PointInCapsule(localPoint, shape.capsule);

                case B2ShapeType.b2_circleShape:
                    return b2PointInCircle(localPoint, shape.circle);

                case B2ShapeType.b2_polygonShape:
                    return b2PointInPolygon(localPoint, shape.polygon);

                default:
                    return false;
            }
        }

// todo_erin untested
        public static B2CastOutput b2Shape_RayCast(B2ShapeId shapeId, ref B2RayCastInput input)
        {
            B2World world = b2GetWorld(shapeId.world0);
            B2Shape shape = b2GetShape(world, shapeId);

            B2Transform transform = b2GetBodyTransform(world, shape.bodyId);

            // input in local coordinates
            B2RayCastInput localInput;
            localInput.origin = b2InvTransformPoint(transform, input.origin);
            localInput.translation = b2InvRotateVector(transform.q, input.translation);
            localInput.maxFraction = input.maxFraction;

            B2CastOutput output = new B2CastOutput();
            switch (shape.type)
            {
                case B2ShapeType.b2_capsuleShape:
                    output = b2RayCastCapsule(ref localInput, shape.capsule);
                    break;

                case B2ShapeType.b2_circleShape:
                    output = b2RayCastCircle(ref localInput, shape.circle);
                    break;

                case B2ShapeType.b2_segmentShape:
                    output = b2RayCastSegment(ref localInput, shape.segment, false);
                    break;

                case B2ShapeType.b2_polygonShape:
                    output = b2RayCastPolygon(ref localInput, shape.polygon);
                    break;

                case B2ShapeType.b2_chainSegmentShape:
                    output = b2RayCastSegment(ref localInput, shape.chainSegment.segment, true);
                    break;

                default:
                    Debug.Assert(false);
                    return output;
            }

            if (output.hit)
            {
                // convert to world coordinates
                output.normal = b2RotateVector(transform.q, output.normal);
                output.point = b2TransformPoint(ref transform, output.point);
            }

            return output;
        }

        public static void b2Shape_SetDensity(B2ShapeId shapeId, float density, bool updateBodyMass)
        {
            Debug.Assert(b2IsValidFloat(density) && density >= 0.0f);

            B2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return;
            }

            B2Shape shape = b2GetShape(world, shapeId);
            if (density == shape.density)
            {
                // early return to avoid expensive function
                return;
            }

            shape.density = density;

            if (updateBodyMass == true)
            {
                B2Body body = b2Array_Get(ref world.bodies, shape.bodyId);
                b2UpdateBodyMassData(world, body);
            }
        }

        public static float b2Shape_GetDensity(B2ShapeId shapeId)
        {
            B2World world = b2GetWorld(shapeId.world0);
            B2Shape shape = b2GetShape(world, shapeId);
            return shape.density;
        }

        public static void b2Shape_SetFriction(B2ShapeId shapeId, float friction)
        {
            Debug.Assert(b2IsValidFloat(friction) && friction >= 0.0f);

            B2World world = b2GetWorld(shapeId.world0);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return;
            }

            B2Shape shape = b2GetShape(world, shapeId);
            shape.friction = friction;
        }

        public static float b2Shape_GetFriction(B2ShapeId shapeId)
        {
            B2World world = b2GetWorld(shapeId.world0);
            B2Shape shape = b2GetShape(world, shapeId);
            return shape.friction;
        }

        public static void b2Shape_SetRestitution(B2ShapeId shapeId, float restitution)
        {
            Debug.Assert(b2IsValidFloat(restitution) && restitution >= 0.0f);

            B2World world = b2GetWorld(shapeId.world0);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return;
            }

            B2Shape shape = b2GetShape(world, shapeId);
            shape.restitution = restitution;
        }

        public static float b2Shape_GetRestitution(B2ShapeId shapeId)
        {
            B2World world = b2GetWorld(shapeId.world0);
            B2Shape shape = b2GetShape(world, shapeId);
            return shape.restitution;
        }

        public static void b2Shape_SetMaterial(B2ShapeId shapeId, int material)
        {
            B2World world = b2GetWorld(shapeId.world0);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return;
            }

            B2Shape shape = b2GetShape(world, shapeId);
            shape.material = material;
        }

        public static int b2Shape_GetMaterial(B2ShapeId shapeId)
        {
            B2World world = b2GetWorld(shapeId.world0);
            B2Shape shape = b2GetShape(world, shapeId);
            return shape.material;
        }

        public static B2Filter b2Shape_GetFilter(B2ShapeId shapeId)
        {
            B2World world = b2GetWorld(shapeId.world0);
            B2Shape shape = b2GetShape(world, shapeId);
            return shape.filter;
        }

        public static void b2ResetProxy(B2World world, B2Shape shape, bool wakeBodies, bool destroyProxy)
        {
            B2Body body = b2Array_Get(ref world.bodies, shape.bodyId);

            int shapeId = shape.id;

            // destroy all contacts associated with this shape
            int contactKey = body.headContactKey;
            while (contactKey != B2_NULL_INDEX)
            {
                int contactId = contactKey >> 1;
                int edgeIndex = contactKey & 1;

                B2Contact contact = b2Array_Get(ref world.contacts, contactId);
                contactKey = contact.edges[edgeIndex].nextKey;

                if (contact.shapeIdA == shapeId || contact.shapeIdB == shapeId)
                {
                    b2DestroyContact(world, contact, wakeBodies);
                }
            }

            B2Transform transform = b2GetBodyTransformQuick(world, body);
            if (shape.proxyKey != B2_NULL_INDEX)
            {
                B2BodyType proxyType = B2_PROXY_TYPE(shape.proxyKey);
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
                B2BodyType proxyType = body.type;
                b2UpdateShapeAABBs(shape, transform, proxyType);
            }

            b2ValidateSolverSets(world);
        }

        public static void b2Shape_SetFilter(B2ShapeId shapeId, B2Filter filter)
        {
            B2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return;
            }

            B2Shape shape = b2GetShape(world, shapeId);
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

        public static void b2Shape_EnableContactEvents(B2ShapeId shapeId, bool flag)
        {
            B2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return;
            }

            B2Shape shape = b2GetShape(world, shapeId);
            shape.enableContactEvents = flag;
        }

        public static bool b2Shape_AreContactEventsEnabled(B2ShapeId shapeId)
        {
            B2World world = b2GetWorld(shapeId.world0);
            B2Shape shape = b2GetShape(world, shapeId);
            return shape.enableContactEvents;
        }

        public static void b2Shape_EnablePreSolveEvents(B2ShapeId shapeId, bool flag)
        {
            B2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return;
            }

            B2Shape shape = b2GetShape(world, shapeId);
            shape.enablePreSolveEvents = flag;
        }

        public static bool b2Shape_ArePreSolveEventsEnabled(B2ShapeId shapeId)
        {
            B2World world = b2GetWorld(shapeId.world0);
            B2Shape shape = b2GetShape(world, shapeId);
            return shape.enablePreSolveEvents;
        }

        public static void b2Shape_EnableHitEvents(B2ShapeId shapeId, bool flag)
        {
            B2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return;
            }

            B2Shape shape = b2GetShape(world, shapeId);
            shape.enableHitEvents = flag;
        }

        public static bool b2Shape_AreHitEventsEnabled(B2ShapeId shapeId)
        {
            B2World world = b2GetWorld(shapeId.world0);
            B2Shape shape = b2GetShape(world, shapeId);
            return shape.enableHitEvents;
        }

        public static B2ShapeType b2Shape_GetType(B2ShapeId shapeId)
        {
            B2World world = b2GetWorld(shapeId.world0);
            B2Shape shape = b2GetShape(world, shapeId);
            return shape.type;
        }

        public static B2Circle b2Shape_GetCircle(B2ShapeId shapeId)
        {
            B2World world = b2GetWorld(shapeId.world0);
            B2Shape shape = b2GetShape(world, shapeId);
            Debug.Assert(shape.type == B2ShapeType.b2_circleShape);
            return shape.circle;
        }

        public static B2Segment b2Shape_GetSegment(B2ShapeId shapeId)
        {
            B2World world = b2GetWorld(shapeId.world0);
            B2Shape shape = b2GetShape(world, shapeId);
            Debug.Assert(shape.type == B2ShapeType.b2_segmentShape);
            return shape.segment;
        }

        public static B2ChainSegment b2Shape_GetChainSegment(B2ShapeId shapeId)
        {
            B2World world = b2GetWorld(shapeId.world0);
            B2Shape shape = b2GetShape(world, shapeId);
            Debug.Assert(shape.type == B2ShapeType.b2_chainSegmentShape);
            return shape.chainSegment;
        }

        public static B2Capsule b2Shape_GetCapsule(B2ShapeId shapeId)
        {
            B2World world = b2GetWorld(shapeId.world0);
            B2Shape shape = b2GetShape(world, shapeId);
            Debug.Assert(shape.type == B2ShapeType.b2_capsuleShape);
            return shape.capsule;
        }

        public static B2Polygon b2Shape_GetPolygon(B2ShapeId shapeId)
        {
            B2World world = b2GetWorld(shapeId.world0);
            B2Shape shape = b2GetShape(world, shapeId);
            Debug.Assert(shape.type == B2ShapeType.b2_polygonShape);
            return shape.polygon;
        }

        public static void b2Shape_SetCircle(B2ShapeId shapeId, B2Circle circle)
        {
            B2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return;
            }

            B2Shape shape = b2GetShape(world, shapeId);
            shape.circle = new B2Circle(circle.center, circle.radius);
            shape.type = B2ShapeType.b2_circleShape;

            // need to wake bodies so they can react to the shape change
            bool wakeBodies = true;
            bool destroyProxy = true;
            b2ResetProxy(world, shape, wakeBodies, destroyProxy);
        }

        public static void b2Shape_SetCapsule(B2ShapeId shapeId, B2Capsule capsule)
        {
            B2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return;
            }

            B2Shape shape = b2GetShape(world, shapeId);
            shape.capsule = new B2Capsule(capsule.center1, capsule.center2, capsule.radius);
            shape.type = B2ShapeType.b2_capsuleShape;

            // need to wake bodies so they can react to the shape change
            bool wakeBodies = true;
            bool destroyProxy = true;
            b2ResetProxy(world, shape, wakeBodies, destroyProxy);
        }

        public static void b2Shape_SetSegment(B2ShapeId shapeId, B2Segment segment)
        {
            B2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return;
            }

            B2Shape shape = b2GetShape(world, shapeId);
            shape.segment = new B2Segment(segment.point1, segment.point2);
            shape.type = B2ShapeType.b2_segmentShape;

            // need to wake bodies so they can react to the shape change
            bool wakeBodies = true;
            bool destroyProxy = true;
            b2ResetProxy(world, shape, wakeBodies, destroyProxy);
        }

        public static void b2Shape_SetPolygon(B2ShapeId shapeId, B2Polygon polygon)
        {
            B2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return;
            }

            B2Shape shape = b2GetShape(world, shapeId);
            shape.polygon = polygon.Clone();
            shape.type = B2ShapeType.b2_polygonShape;

            // need to wake bodies so they can react to the shape change
            bool wakeBodies = true;
            bool destroyProxy = true;
            b2ResetProxy(world, shape, wakeBodies, destroyProxy);
        }

        public static B2ChainId b2Shape_GetParentChain(B2ShapeId shapeId)
        {
            B2World world = b2GetWorld(shapeId.world0);
            B2Shape shape = b2GetShape(world, shapeId);
            if (shape.type == B2ShapeType.b2_chainSegmentShape)
            {
                int chainId = shape.chainSegment.chainId;
                if (chainId != B2_NULL_INDEX)
                {
                    B2ChainShape chain = b2Array_Get(ref world.chainShapes, chainId);
                    B2ChainId id = new B2ChainId(chainId + 1, shapeId.world0, chain.generation);
                    return id;
                }
            }

            return new B2ChainId();
        }

        public static void b2Chain_SetFriction(B2ChainId chainId, float friction)
        {
            Debug.Assert(b2IsValidFloat(friction) && friction >= 0.0f);

            B2World world = b2GetWorldLocked(chainId.world0);
            if (world == null)
            {
                return;
            }

            B2ChainShape chainShape = b2GetChainShape(world, chainId);

            int materialCount = chainShape.materialCount;
            for (int i = 0; i < materialCount; ++i)
            {
                chainShape.materials[i].friction = friction;
            }

            int count = chainShape.count;

            for (int i = 0; i < count; ++i)
            {
                int shapeId = chainShape.shapeIndices[i];
                B2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                shape.friction = friction;
            }
        }

        public static float b2Chain_GetFriction(B2ChainId chainId)
        {
            B2World world = b2GetWorld(chainId.world0);
            B2ChainShape chainShape = b2GetChainShape(world, chainId);
            return chainShape.materials[0].friction;
        }

        public static void b2Chain_SetRestitution(B2ChainId chainId, float restitution)
        {
            Debug.Assert(b2IsValidFloat(restitution));

            B2World world = b2GetWorldLocked(chainId.world0);
            if (world == null)
            {
                return;
            }

            B2ChainShape chainShape = b2GetChainShape(world, chainId);

            int materialCount = chainShape.materialCount;
            for (int i = 0; i < materialCount; ++i)
            {
                chainShape.materials[i].restitution = restitution;
            }

            int count = chainShape.count;

            for (int i = 0; i < count; ++i)
            {
                int shapeId = chainShape.shapeIndices[i];
                B2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                shape.restitution = restitution;
            }
        }

        public static float b2Chain_GetRestitution(B2ChainId chainId)
        {
            B2World world = b2GetWorld(chainId.world0);
            B2ChainShape chainShape = b2GetChainShape(world, chainId);
            return chainShape.materials[0].restitution;
        }

        public static void b2Chain_SetMaterial(B2ChainId chainId, int material)
        {
            B2World world = b2GetWorldLocked(chainId.world0);
            if (world == null)
            {
                return;
            }

            B2ChainShape chainShape = b2GetChainShape(world, chainId);
            int materialCount = chainShape.materialCount;
            for (int i = 0; i < materialCount; ++i)
            {
                chainShape.materials[i].material = material;
            }

            int count = chainShape.count;

            for (int i = 0; i < count; ++i)
            {
                int shapeId = chainShape.shapeIndices[i];
                B2Shape shape = b2Array_Get(ref world.shapes, shapeId);
                shape.material = material;
            }
        }

        public static int b2Chain_GetMaterial(B2ChainId chainId)
        {
            B2World world = b2GetWorld(chainId.world0);
            B2ChainShape chainShape = b2GetChainShape(world, chainId);
            return chainShape.materials[0].material;
        }

        public static int b2Shape_GetContactCapacity(B2ShapeId shapeId)
        {
            B2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return 0;
            }

            B2Shape shape = b2GetShape(world, shapeId);
            if (shape.sensorIndex != B2_NULL_INDEX)
            {
                return 0;
            }

            B2Body body = b2Array_Get(ref world.bodies, shape.bodyId);

            // Conservative and fast
            return body.contactCount;
        }

        public static int b2Shape_GetContactData(B2ShapeId shapeId, Span<B2ContactData> contactData, int capacity)
        {
            B2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return 0;
            }

            B2Shape shape = b2GetShape(world, shapeId);
            if (shape.sensorIndex != B2_NULL_INDEX)
            {
                return 0;
            }

            B2Body body = b2Array_Get(ref world.bodies, shape.bodyId);
            int contactKey = body.headContactKey;
            int index = 0;
            while (contactKey != B2_NULL_INDEX && index < capacity)
            {
                int contactId = contactKey >> 1;
                int edgeIndex = contactKey & 1;

                B2Contact contact = b2Array_Get(ref world.contacts, contactId);

                // Does contact involve this shape and is it touching?
                if ((contact.shapeIdA == shapeId.index1 - 1 || contact.shapeIdB == shapeId.index1 - 1) &&
                    (contact.flags & (int)B2ContactFlags.b2_contactTouchingFlag) != 0)
                {
                    B2Shape shapeA = world.shapes.data[contact.shapeIdA];
                    B2Shape shapeB = world.shapes.data[contact.shapeIdB];

                    contactData[index].shapeIdA = new B2ShapeId(shapeA.id + 1, shapeId.world0, shapeA.generation);
                    contactData[index].shapeIdB = new B2ShapeId(shapeB.id + 1, shapeId.world0, shapeB.generation);

                    B2ContactSim contactSim = b2GetContactSim(world, contact);
                    contactData[index].manifold = contactSim.manifold;
                    index += 1;
                }

                contactKey = contact.edges[edgeIndex].nextKey;
            }

            Debug.Assert(index <= capacity);

            return index;
        }

        public static int b2Shape_GetSensorCapacity(B2ShapeId shapeId)
        {
            B2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return 0;
            }

            B2Shape shape = b2GetShape(world, shapeId);
            if (shape.sensorIndex == B2_NULL_INDEX)
            {
                return 0;
            }

            B2Sensor sensor = b2Array_Get(ref world.sensors, shape.sensorIndex);
            return sensor.overlaps2.count;
        }

        public static int b2Shape_GetSensorOverlaps(B2ShapeId shapeId, Span<B2ShapeId> overlaps, int capacity)
        {
            B2World world = b2GetWorldLocked(shapeId.world0);
            if (world == null)
            {
                return 0;
            }

            B2Shape shape = b2GetShape(world, shapeId);
            if (shape.sensorIndex == B2_NULL_INDEX)
            {
                return 0;
            }

            B2Sensor sensor = b2Array_Get(ref world.sensors, shape.sensorIndex);

            int count = b2MinInt(sensor.overlaps2.count, capacity);
            B2ShapeRef[] refs = sensor.overlaps2.data;
            for (int i = 0; i < count; ++i)
            {
                overlaps[i] = new B2ShapeId(refs[i].shapeId + 1, shapeId.world0, refs[i].generation);
            }

            return count;
        }

        public static B2AABB b2Shape_GetAABB(B2ShapeId shapeId)
        {
            B2World world = b2GetWorld(shapeId.world0);
            if (world == null)
            {
                return new B2AABB();
            }

            B2Shape shape = b2GetShape(world, shapeId);
            return shape.aabb;
        }

        public static B2MassData b2Shape_GetMassData(B2ShapeId shapeId)
        {
            B2World world = b2GetWorld(shapeId.world0);
            if (world == null)
            {
                return new B2MassData();
            }

            B2Shape shape = b2GetShape(world, shapeId);
            return b2ComputeShapeMass(shape);
        }

        public static B2Vec2 b2Shape_GetClosestPoint(B2ShapeId shapeId, B2Vec2 target)
        {
            B2World world = b2GetWorld(shapeId.world0);
            if (world == null)
            {
                return new B2Vec2();
            }

            B2Shape shape = b2GetShape(world, shapeId);
            B2Body body = b2Array_Get(ref world.bodies, shape.bodyId);
            B2Transform transform = b2GetBodyTransformQuick(world, body);

            B2DistanceInput input = new B2DistanceInput();
            input.proxyA = b2MakeShapeDistanceProxy(shape);
            input.proxyB = b2MakeProxy(target, 1, 0.0f);
            input.transformA = transform;
            input.transformB = b2Transform_identity;
            input.useRadii = true;

            B2SimplexCache cache = new B2SimplexCache();
            B2DistanceOutput output = b2ShapeDistance(ref cache, ref input, null, 0);

            return output.pointA;
        }
    }
}
