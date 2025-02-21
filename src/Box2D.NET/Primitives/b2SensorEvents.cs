﻿namespace Box2D.NET.Primitives
{
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
}