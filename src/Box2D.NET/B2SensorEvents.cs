// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Sensor events are buffered in the world and are available
    /// as begin/end overlap event arrays after the time step is complete.
    /// Note: these may become invalid if bodies and/or shapes are destroyed
    public struct B2SensorEvents
    {
        /// Array of sensor begin touch events
        public B2SensorBeginTouchEvent[] beginEvents;

        /// Array of sensor end touch events
        public B2SensorEndTouchEvent[] endEvents;

        /// The number of begin touch events
        public int beginCount;

        /// The number of end touch events
        public int endCount;
    }
}
