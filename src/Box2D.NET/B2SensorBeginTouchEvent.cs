// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
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
    public struct B2SensorBeginTouchEvent
    {
        /// The id of the sensor shape
        public B2ShapeId sensorShapeId;

        /// The id of the shape that began touching the sensor shape
        public B2ShapeId visitorShapeId;

        public B2SensorBeginTouchEvent(B2ShapeId sensorShapeId, B2ShapeId visitorShapeId)
        {
            this.sensorShapeId = sensorShapeId;
            this.visitorShapeId = visitorShapeId;
        }
    }
}