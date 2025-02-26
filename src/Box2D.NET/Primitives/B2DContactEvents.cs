// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
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
}
