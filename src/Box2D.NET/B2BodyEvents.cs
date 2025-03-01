// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Body events are buffered in the Box2D world and are available
    /// as event arrays after the time step is complete.
    /// Note: this data becomes invalid if bodies are destroyed
    public struct B2BodyEvents
    {
        /// Array of move events
        public B2BodyMoveEvent[] moveEvents;

        /// Number of move events
        public int moveCount;

        public B2BodyEvents(B2BodyMoveEvent[] moveEvents, int moveCount)
        {
            this.moveEvents = moveEvents;
            this.moveCount = moveCount;
        }
    }
}
