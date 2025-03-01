// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Contact events are buffered in the Box2D world and are available
    /// as event arrays after the time step is complete.
    /// Note: these may become invalid if bodies and/or shapes are destroyed
    public struct B2ContactEvents
    {
        /// Array of begin touch events
        public B2ContactBeginTouchEvent[] beginEvents;

        /// Array of end touch events
        public B2ContactEndTouchEvent[] endEvents;

        /// Array of hit events
        public B2ContactHitEvent[] hitEvents;

        /// Number of begin touch events
        public int beginCount;

        /// Number of end touch events
        public int endCount;

        /// Number of hit events
        public int hitCount;
    }
}
