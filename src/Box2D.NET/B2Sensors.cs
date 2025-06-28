// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2DynamicTrees;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Profiling;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Contacts;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2BitSets;
using static Box2D.NET.B2CTZs;


namespace Box2D.NET
{
    public static class B2Sensors
    {
        // Sensor shapes need to
        // - detect begin and end overlap events
        // - events must be reported in deterministic order
        // - maintain an active list of overlaps for query

        // Assumption
        // - sensors don't detect shapes on the same body

        // Algorithm
        // Query all sensors for overlaps
        // Check against previous overlaps

        // Data structures
        // Each sensor has an double buffered array of overlaps
        // These overlaps use a shape reference with index and generation

        public static bool b2SensorQueryCallback(int proxyId, ulong userData, ref B2SensorQueryContext context)
        {
            B2_UNUSED(proxyId);

            int shapeId = (int)userData;

            ref B2SensorQueryContext queryContext = ref context;
            B2Shape sensorShape = queryContext.sensorShape;
            int sensorShapeId = sensorShape.id;

            if (shapeId == sensorShapeId)
            {
                return true;
            }

            B2World world = queryContext.world;
            B2Shape otherShape = b2Array_Get(ref world.shapes, shapeId);

            // Are sensor events enabled on the other shape?
            if (otherShape.enableSensorEvents == false)
            {
                return true;
            }

            // Skip shapes on the same body
            if (otherShape.bodyId == sensorShape.bodyId)
            {
                return true;
            }

            // Check filter
            if (b2ShouldShapesCollide(sensorShape.filter, otherShape.filter) == false)
            {
                return true;
            }

            // Custom user filter
            b2CustomFilterFcn customFilterFcn = queryContext.world.customFilterFcn;
            if (customFilterFcn != null)
            {
                B2ShapeId idA = new B2ShapeId(sensorShapeId + 1, world.worldId, sensorShape.generation);
                B2ShapeId idB = new B2ShapeId(shapeId + 1, world.worldId, otherShape.generation);
                bool shouldCollide = customFilterFcn(idA, idB, queryContext.world.customFilterContext);
                if (shouldCollide == false)
                {
                    return true;
                }
            }

            B2Transform otherTransform = b2GetBodyTransform(world, otherShape.bodyId);

            B2DistanceInput input = new B2DistanceInput();
            input.proxyA = b2MakeShapeDistanceProxy(sensorShape);
            input.proxyB = b2MakeShapeDistanceProxy(otherShape);
            input.transformA = queryContext.transform;
            input.transformB = otherTransform;
            input.useRadii = true;
            B2SimplexCache cache = new B2SimplexCache();
            B2DistanceOutput output = b2ShapeDistance(ref input, ref cache, null, 0);

            bool overlaps = output.distance < 10.0f * FLT_EPSILON;
            if (overlaps == false)
            {
                return true;
            }

            // Record the overlap
            B2Sensor sensor = queryContext.sensor;
            ref B2Visitor shapeRef = ref b2Array_Add(ref sensor.overlaps2);
            shapeRef.shapeId = shapeId;
            shapeRef.generation = otherShape.generation;

            return true;
        }

        public static int b2CompareVisitors(ref B2Visitor a, ref B2Visitor b)
        {
            ref readonly B2Visitor sa = ref a;
            ref readonly B2Visitor sb = ref b;

            if (sa.shapeId < sb.shapeId)
            {
                return -1;
            }

            return 1;
        }

        public static void b2SensorTask(int startIndex, int endIndex, uint threadIndex, object context)
        {
            b2TracyCZoneNC(B2TracyCZone.sensor_task, "Overlap", B2HexColor.b2_colorBrown, true);

            B2World world = context as B2World;
            B2_ASSERT((int)threadIndex < world.workerCount);
            B2SensorTaskContext taskContext = world.sensorTaskContexts.data[threadIndex];

            B2_ASSERT(startIndex < endIndex);

            B2DynamicTree[] trees = world.broadPhase.trees;
            for (int sensorIndex = startIndex; sensorIndex < endIndex; ++sensorIndex)
            {
                B2Sensor sensor = b2Array_Get(ref world.sensors, sensorIndex);
                B2Shape sensorShape = b2Array_Get(ref world.shapes, sensor.shapeId);

                // Swap overlap arrays
                B2Array<B2Visitor> temp = sensor.overlaps1;
                sensor.overlaps1 = sensor.overlaps2;
                sensor.overlaps2 = temp;
                b2Array_Clear(ref sensor.overlaps2);

                // Append sensor hits
                int hitCount = sensor.hits.count;
                for (int i = 0; i < hitCount; ++i)
                {
                    b2Array_Push(ref sensor.overlaps2, sensor.hits.data[i]);
                }

                // Clear the hits
                b2Array_Clear(ref sensor.hits);

                B2Body body = b2Array_Get(ref world.bodies, sensorShape.bodyId);
                if (body.setIndex == (int)B2SetType.b2_disabledSet || sensorShape.enableSensorEvents == false)
                {
                    if (sensor.overlaps1.count != 0)
                    {
                        // This sensor is dropping all overlaps because it has been disabled.
                        b2SetBit(ref taskContext.eventBits, sensorIndex);
                    }

                    continue;
                }

                B2Transform transform = b2GetBodyTransformQuick(world, body);

                B2SensorQueryContext queryContext = new B2SensorQueryContext()
                {
                    world = world,
                    taskContext = taskContext,
                    sensor = sensor,
                    sensorShape = sensorShape,
                    transform = transform,
                };

                B2_ASSERT(sensorShape.sensorIndex == sensorIndex);
                B2AABB queryBounds = sensorShape.aabb;

                // Query all trees
                b2DynamicTree_Query(trees[0], queryBounds, sensorShape.filter.maskBits, b2SensorQueryCallback, ref queryContext);
                b2DynamicTree_Query(trees[1], queryBounds, sensorShape.filter.maskBits, b2SensorQueryCallback, ref queryContext);
                b2DynamicTree_Query(trees[2], queryBounds, sensorShape.filter.maskBits, b2SensorQueryCallback, ref queryContext);

                // Sort the overlaps to enable finding begin and end events.
                Array.Sort(sensor.overlaps2.data, 0, sensor.overlaps2.count, B2ShapeRefComparer.Shared);

                // Remove duplicates from overlaps2 (sorted). Duplicates are possible due to the hit events appended earlier.
                int uniqueCount = 0;
                int overlapCount = sensor.overlaps2.count;
                Span<B2Visitor> overlapData = sensor.overlaps2.data;
                for (int i = 0; i < overlapCount; ++i)
                {
                    if (uniqueCount == 0 || overlapData[i].shapeId != overlapData[uniqueCount - 1].shapeId)
                    {
                        overlapData[uniqueCount] = overlapData[i];
                        uniqueCount += 1;
                    }
                }

                sensor.overlaps2.count = uniqueCount;

                int count1 = sensor.overlaps1.count;
                int count2 = sensor.overlaps2.count;
                if (count1 != count2)
                {
                    // something changed
                    b2SetBit(ref taskContext.eventBits, sensorIndex);
                }
                else
                {
                    for (int i = 0; i < count1; ++i)
                    {
                        ref readonly B2Visitor s1 = ref sensor.overlaps1.data[i];
                        ref readonly B2Visitor s2 = ref sensor.overlaps2.data[i];

                        if (s1.shapeId != s2.shapeId || s1.generation != s2.generation)
                        {
                            // something changed
                            b2SetBit(ref taskContext.eventBits, sensorIndex);
                            break;
                        }
                    }
                }
            }

            b2TracyCZoneEnd(B2TracyCZone.sensor_task);
        }

        public static void b2OverlapSensors(B2World world)
        {
            int sensorCount = world.sensors.count;
            if (sensorCount == 0)
            {
                return;
            }

            B2_ASSERT(world.workerCount > 0);

            b2TracyCZoneNC(B2TracyCZone.overlap_sensors, "Sensors", B2HexColor.b2_colorMediumPurple, true);

            for (int i = 0; i < world.workerCount; ++i)
            {
                b2SetBitCountAndClear(ref world.sensorTaskContexts.data[i].eventBits, sensorCount);
            }

            // Parallel-for sensors overlaps
            int minRange = 16;
            object userSensorTask = world.enqueueTaskFcn(b2SensorTask, sensorCount, minRange, world, world.userTaskContext);
            world.taskCount += 1;
            if (userSensorTask != null)
            {
                world.finishTaskFcn(userSensorTask, world.userTaskContext);
            }

            b2TracyCZoneNC(B2TracyCZone.sensor_state, "Events", B2HexColor.b2_colorLightSlateGray, true);

            ref B2BitSet bitSet = ref world.sensorTaskContexts.data[0].eventBits;
            for (int i = 1; i < world.workerCount; ++i)
            {
                b2InPlaceUnion(ref bitSet, ref world.sensorTaskContexts.data[i].eventBits);
            }

            // Iterate sensors bits and publish events
            // Process sensor state changes. Iterate over set bits
            ulong[] bits = bitSet.bits;
            int blockCount = bitSet.blockCount;

            for (uint k = 0; k < blockCount; ++k)
            {
                ulong word = bits[k];
                while (word != 0)
                {
                    uint ctz = b2CTZ64(word);
                    int sensorIndex = (int)(64 * k + ctz);

                    B2Sensor sensor = b2Array_Get(ref world.sensors, sensorIndex);
                    B2Shape sensorShape = b2Array_Get(ref world.shapes, sensor.shapeId);
                    B2ShapeId sensorId = new B2ShapeId(sensor.shapeId + 1, world.worldId, sensorShape.generation);

                    int count1 = sensor.overlaps1.count;
                    int count2 = sensor.overlaps2.count;
                    ref readonly B2Visitor[] refs1 = ref sensor.overlaps1.data;
                    ref readonly B2Visitor[] refs2 = ref sensor.overlaps2.data;

                    // overlaps1 can have overlaps that end
                    // overlaps2 can have overlaps that begin
                    int index1 = 0, index2 = 0;
                    while (index1 < count1 && index2 < count2)
                    {
                        ref readonly B2Visitor r1 = ref refs1[index1];
                        ref readonly B2Visitor r2 = ref refs2[index2];
                        if (r1.shapeId == r2.shapeId)
                        {
                            if (r1.generation < r2.generation)
                            {
                                // end
                                B2ShapeId visitorId = new B2ShapeId(r1.shapeId + 1, world.worldId, r1.generation);
                                B2SensorEndTouchEvent @event = new B2SensorEndTouchEvent(sensorId, visitorId);
                                b2Array_Push(ref world.sensorEndEvents[world.endEventArrayIndex], @event);
                                index1 += 1;
                            }
                            else if (r1.generation > r2.generation)
                            {
                                // begin
                                B2ShapeId visitorId = new B2ShapeId(r2.shapeId + 1, world.worldId, r2.generation);
                                B2SensorBeginTouchEvent @event = new B2SensorBeginTouchEvent(sensorId, visitorId);
                                b2Array_Push(ref world.sensorBeginEvents, @event);
                                index2 += 1;
                            }
                            else
                            {
                                // persisted
                                index1 += 1;
                                index2 += 1;
                            }
                        }
                        else if (r1.shapeId < r2.shapeId)
                        {
                            // end
                            B2ShapeId visitorId = new B2ShapeId(r1.shapeId + 1, world.worldId, r1.generation);
                            B2SensorEndTouchEvent @event = new B2SensorEndTouchEvent(sensorId, visitorId);
                            b2Array_Push(ref world.sensorEndEvents[world.endEventArrayIndex], @event);
                            index1 += 1;
                        }
                        else
                        {
                            // begin
                            B2ShapeId visitorId = new B2ShapeId(r2.shapeId + 1, world.worldId, r2.generation);
                            B2SensorBeginTouchEvent @event = new B2SensorBeginTouchEvent(sensorId, visitorId);
                            b2Array_Push(ref world.sensorBeginEvents, @event);
                            index2 += 1;
                        }
                    }

                    while (index1 < count1)
                    {
                        // end
                        ref readonly B2Visitor r1 = ref refs1[index1];
                        B2ShapeId visitorId = new B2ShapeId(r1.shapeId + 1, world.worldId, r1.generation);
                        B2SensorEndTouchEvent @event = new B2SensorEndTouchEvent(sensorId, visitorId);
                        b2Array_Push(ref world.sensorEndEvents[world.endEventArrayIndex], @event);
                        index1 += 1;
                    }

                    while (index2 < count2)
                    {
                        // begin
                        ref readonly B2Visitor r2 = ref refs2[index2];
                        B2ShapeId visitorId = new B2ShapeId(r2.shapeId + 1, world.worldId, r2.generation);
                        B2SensorBeginTouchEvent @event = new B2SensorBeginTouchEvent(sensorId, visitorId);
                        b2Array_Push(ref world.sensorBeginEvents, @event);
                        index2 += 1;
                    }

                    // Clear the smallest set bit
                    word = word & (word - 1);
                }
            }

            b2TracyCZoneEnd(B2TracyCZone.sensor_state);
            b2TracyCZoneEnd(B2TracyCZone.overlap_sensors);
        }

        public static void b2DestroySensor(B2World world, B2Shape sensorShape)
        {
            B2Sensor sensor = b2Array_Get(ref world.sensors, sensorShape.sensorIndex);
            for (int i = 0; i < sensor.overlaps2.count; ++i)
            {
                ref readonly B2Visitor @ref = ref sensor.overlaps2.data[i];
                B2SensorEndTouchEvent @event = new B2SensorEndTouchEvent()
                {
                    sensorShapeId = new B2ShapeId(sensorShape.id + 1, world.worldId, sensorShape.generation),
                    visitorShapeId = new B2ShapeId(@ref.shapeId + 1, world.worldId, @ref.generation),
                };

                b2Array_Push(ref world.sensorEndEvents[world.endEventArrayIndex], @event);
            }

            // Destroy sensor
            b2Array_Destroy(ref sensor.hits);
            b2Array_Destroy(ref sensor.overlaps1);
            b2Array_Destroy(ref sensor.overlaps2);

            int movedIndex = b2Array_RemoveSwap(ref world.sensors, sensorShape.sensorIndex);
            if (movedIndex != B2_NULL_INDEX)
            {
                // Fixup moved sensor
                B2Sensor movedSensor = b2Array_Get(ref world.sensors, sensorShape.sensorIndex);
                B2Shape otherSensorShape = b2Array_Get(ref world.shapes, movedSensor.shapeId);
                otherSensorShape.sensorIndex = sensorShape.sensorIndex;
            }
        }
    }
}