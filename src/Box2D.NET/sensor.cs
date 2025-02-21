// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Box2D.NET.Primitives;
using static Box2D.NET.array;
using static Box2D.NET.dynamic_tree;
using static Box2D.NET.core;
using static Box2D.NET.constants;
using static Box2D.NET.contact;
using static Box2D.NET.math_function;
using static Box2D.NET.shape;
using static Box2D.NET.body;
using static Box2D.NET.distance;
using static Box2D.NET.bitset;
using static Box2D.NET.ctz;


namespace Box2D.NET
{
    public static class sensor
    {
        // Sensor shapes need to
        // - detect begin and end overlap events
        // - events must be reported in deterministic order
        // - maintain an active list of overlaps for query

        // Assumption
        // - sensors don't detect other sensors

        // Algorithm
        // Query all sensors for overlaps
        // Check against previous overlaps

        // Data structures
        // Each sensor has an double buffered array of overlaps
        // These overlaps use a shape reference with index and generation

        public static bool b2SensorQueryCallback(int proxyId, int shapeId, object context)
        {
            B2_UNUSED(proxyId);

            b2SensorQueryContext queryContext = context as b2SensorQueryContext;
            b2Shape sensorShape = queryContext.sensorShape;
            int sensorShapeId = sensorShape.id;

            if (shapeId == sensorShapeId)
            {
                return true;
            }

            b2World world = queryContext.world;
            b2Shape otherShape = b2Array_Get(ref world.shapes, shapeId);

            // Sensors don't overlap with other sensors
            if (otherShape.sensorIndex != B2_NULL_INDEX)
            {
                return true;
            }

            // Check filter
            if (b2ShouldShapesCollide(sensorShape.filter, otherShape.filter) == false)
            {
                return true;
            }

            b2Transform otherTransform = b2GetBodyTransform(world, otherShape.bodyId);

            b2DistanceInput input = new b2DistanceInput();
            input.proxyA = b2MakeShapeDistanceProxy(sensorShape);
            input.proxyB = b2MakeShapeDistanceProxy(otherShape);
            input.transformA = queryContext.transform;
            input.transformB = otherTransform;
            input.useRadii = true;
            b2SimplexCache cache = new b2SimplexCache();
            b2DistanceOutput output = b2ShapeDistance(ref cache, input, null, 0);

            bool overlaps = output.distance < 10.0f * FLT_EPSILON;
            if (overlaps == false)
            {
                return true;
            }

            // Record the overlap
            b2Sensor sensor = queryContext.sensor;
            ref b2ShapeRef shapeRef = ref b2Array_Add(ref sensor.overlaps2);
            shapeRef.shapeId = shapeId;
            shapeRef.generation = otherShape.generation;

            return true;
        }

        public static int b2CompareShapeRefs(b2ShapeRef a, b2ShapeRef b)
        {
            b2ShapeRef sa = a;
            b2ShapeRef sb = b;

            if (sa.shapeId < sb.shapeId)
            {
                return -1;
            }

            if (sa.shapeId == sb.shapeId)
            {
                if (sa.generation < sb.generation)
                {
                    return -1;
                }

                if (sa.generation == sb.generation)
                {
                    return 0;
                }
            }

            return 1;
        }

        public static void b2SensorTask(int startIndex, int endIndex, uint threadIndex, object context)
        {
            b2TracyCZoneNC(b2TracyCZone.sensor_task, "Overlap", b2HexColor.b2_colorBrown, true);

            b2World world = context as b2World;
            Debug.Assert((int)threadIndex < world.workerCount);
            b2SensorTaskContext taskContext = world.sensorTaskContexts.data[threadIndex];

            Debug.Assert(startIndex < endIndex);

            b2DynamicTree[] trees = world.broadPhase.trees;
            for (int sensorIndex = startIndex; sensorIndex < endIndex; ++sensorIndex)
            {
                b2Sensor sensor = b2Array_Get(ref world.sensors, sensorIndex);
                b2Shape sensorShape = b2Array_Get(ref world.shapes, sensor.shapeId);

                // swap overlap arrays
                b2Array<b2ShapeRef> temp = sensor.overlaps1;
                sensor.overlaps1 = sensor.overlaps2;
                sensor.overlaps2 = temp;
                b2Array_Clear(ref sensor.overlaps2);

                b2Transform transform = b2GetBodyTransform(world, sensorShape.bodyId);

                b2SensorQueryContext queryContext = new b2SensorQueryContext()
                {
                    world = world,
                    taskContext = taskContext,
                    sensorShape = sensorShape,
                    sensor = sensor,
                    transform = transform,
                };

                Debug.Assert(sensorShape.sensorIndex == sensorIndex);
                b2AABB queryBounds = sensorShape.aabb;

                // Query all trees
                b2DynamicTree_Query(trees[0], queryBounds, sensorShape.filter.maskBits, b2SensorQueryCallback, queryContext);
                b2DynamicTree_Query(trees[1], queryBounds, sensorShape.filter.maskBits, b2SensorQueryCallback, queryContext);
                b2DynamicTree_Query(trees[2], queryBounds, sensorShape.filter.maskBits, b2SensorQueryCallback, queryContext);

                // Sort the overlaps to enable finding begin and end events.
                Array.Sort(sensor.overlaps2.data, 0, sensor.overlaps2.count, b2ShapeRefComparer.Shared);

                int count1 = sensor.overlaps1.count;
                int count2 = sensor.overlaps2.count;
                if (count1 != count2)
                {
                    // something changed
                    b2SetBit(taskContext.eventBits, sensorIndex);
                }
                else
                {
                    for (int i = 0; i < count1; ++i)
                    {
                        b2ShapeRef s1 = sensor.overlaps1.data[i];
                        b2ShapeRef s2 = sensor.overlaps2.data[i];

                        if (s1.shapeId != s2.shapeId || s1.generation != s2.generation)
                        {
                            // something changed
                            b2SetBit(taskContext.eventBits, sensorIndex);
                            break;
                        }
                    }
                }
            }

            b2TracyCZoneEnd(b2TracyCZone.sensor_task);
        }

        public static void b2OverlapSensors(b2World world)
        {
            int sensorCount = world.sensors.count;
            if (sensorCount == 0)
            {
                return;
            }

            Debug.Assert(world.workerCount > 0);

            b2TracyCZoneNC(b2TracyCZone.overlap_sensors, "Sensors", b2HexColor.b2_colorMediumPurple, true);

            for (int i = 0; i < world.workerCount; ++i)
            {
                b2SetBitCountAndClear(world.sensorTaskContexts.data[i].eventBits, sensorCount);
            }

            // Parallel-for sensors overlaps
            int minRange = 16;
            object userSensorTask = world.enqueueTaskFcn(b2SensorTask, sensorCount, minRange, world, world.userTaskContext);
            world.taskCount += 1;
            if (userSensorTask != null)
            {
                world.finishTaskFcn(userSensorTask, world.userTaskContext);
            }

            b2TracyCZoneNC(b2TracyCZone.sensor_state, "Events", b2HexColor.b2_colorLightSlateGray, true);

            b2BitSet bitSet = world.sensorTaskContexts.data[0].eventBits;
            for (int i = 1; i < world.workerCount; ++i)
            {
                b2InPlaceUnion(bitSet, world.sensorTaskContexts.data[i].eventBits);
            }

            // Iterate sensors bits and publish events
            // Process contact state changes. Iterate over set bits
            ulong[] bits = bitSet.bits;
            int blockCount = bitSet.blockCount;

            for (uint k = 0; k < blockCount; ++k)
            {
                ulong word = bits[k];
                while (word != 0)
                {
                    uint ctz = b2CTZ64(word);
                    int sensorIndex = (int)(64 * k + ctz);

                    b2Sensor sensor = b2Array_Get(ref world.sensors, sensorIndex);
                    b2Shape sensorShape = b2Array_Get(ref world.shapes, sensor.shapeId);
                    b2ShapeId sensorId = new b2ShapeId(sensor.shapeId + 1, world.worldId, sensorShape.generation);

                    int count1 = sensor.overlaps1.count;
                    int count2 = sensor.overlaps2.count;
                    b2ShapeRef[] refs1 = sensor.overlaps1.data;
                    b2ShapeRef[] refs2 = sensor.overlaps2.data;

                    // overlaps1 can have overlaps that end
                    // overlaps2 can have overlaps that begin
                    int index1 = 0, index2 = 0;
                    while (index1 < count1 && index2 < count2)
                    {
                        b2ShapeRef r1 = refs1[index1];
                        b2ShapeRef r2 = refs2[index2];
                        if (r1.shapeId == r2.shapeId)
                        {
                            if (r1.generation < r2.generation)
                            {
                                // end
                                b2ShapeId visitorId = new b2ShapeId(r1.shapeId + 1, world.worldId, r1.generation);
                                b2SensorEndTouchEvent @event = new b2SensorEndTouchEvent(sensorId, visitorId);
                                b2Array_Push(ref world.sensorEndEvents[world.endEventArrayIndex], @event);
                                index1 += 1;
                            }
                            else if (r1.generation > r2.generation)
                            {
                                // begin
                                b2ShapeId visitorId = new b2ShapeId(r2.shapeId + 1, world.worldId, r2.generation);
                                b2SensorBeginTouchEvent @event = new b2SensorBeginTouchEvent(sensorId, visitorId);
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
                            b2ShapeId visitorId = new b2ShapeId(r1.shapeId + 1, world.worldId, r1.generation);
                            b2SensorEndTouchEvent @event = new b2SensorEndTouchEvent(sensorId, visitorId);
                            b2Array_Push(ref world.sensorEndEvents[world.endEventArrayIndex], @event);
                            index1 += 1;
                        }
                        else
                        {
                            // begin
                            b2ShapeId visitorId = new b2ShapeId(r2.shapeId + 1, world.worldId, r2.generation);
                            b2SensorBeginTouchEvent @event = new b2SensorBeginTouchEvent(sensorId, visitorId);
                            b2Array_Push(ref world.sensorBeginEvents, @event);
                            index2 += 1;
                        }
                    }

                    while (index1 < count1)
                    {
                        // end
                        b2ShapeRef r1 = refs1[index1];
                        b2ShapeId visitorId = new b2ShapeId(r1.shapeId + 1, world.worldId, r1.generation);
                        b2SensorEndTouchEvent @event = new b2SensorEndTouchEvent(sensorId, visitorId);
                        b2Array_Push(ref world.sensorEndEvents[world.endEventArrayIndex], @event);
                        index1 += 1;
                    }

                    while (index2 < count2)
                    {
                        // begin
                        b2ShapeRef r2 = refs2[index2];
                        b2ShapeId visitorId = new b2ShapeId(r2.shapeId + 1, world.worldId, r2.generation);
                        b2SensorBeginTouchEvent @event = new b2SensorBeginTouchEvent(sensorId, visitorId);
                        b2Array_Push(ref world.sensorBeginEvents, @event);
                        index2 += 1;
                    }

                    // Clear the smallest set bit
                    word = word & (word - 1);
                }
            }

            b2TracyCZoneEnd(b2TracyCZone.sensor_state);
            b2TracyCZoneEnd(b2TracyCZone.overlap_sensors);
        }

        public static void b2DestroySensor(b2World world, b2Shape sensorShape)
        {
            b2Sensor sensor = b2Array_Get(ref world.sensors, sensorShape.sensorIndex);
            for (int i = 0; i < sensor.overlaps2.count; ++i)
            {
                b2ShapeRef @ref = sensor.overlaps2.data[i];
                b2SensorEndTouchEvent @event = new b2SensorEndTouchEvent()
                {
                    sensorShapeId = new b2ShapeId(sensorShape.id + 1, world.worldId, sensorShape.generation),
                    visitorShapeId = new b2ShapeId(@ref.shapeId + 1, world.worldId, @ref.generation),
                };

                b2Array_Push(ref world.sensorEndEvents[world.endEventArrayIndex], @event);
            }

            // Destroy sensor
            b2Array_Destroy(ref sensor.overlaps1);
            b2Array_Destroy(ref sensor.overlaps2);

            int movedIndex = b2Array_RemoveSwap(ref world.sensors, sensorShape.sensorIndex);
            if (movedIndex != B2_NULL_INDEX)
            {
                // Fixup moved sensor
                b2Sensor movedSensor = b2Array_Get(ref world.sensors, sensorShape.sensorIndex);
                b2Shape otherSensorShape = b2Array_Get(ref world.shapes, movedSensor.shapeId);
                otherSensorShape.sensorIndex = sensorShape.sensorIndex;
            }
        }
    }
}