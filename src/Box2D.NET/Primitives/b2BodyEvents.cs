namespace Box2D.NET.Primitives
{
    /// Body events are buffered in the Box2D world and are available
    /// as event arrays after the time step is complete.
    /// Note: this data becomes invalid if bodies are destroyed
    public struct b2BodyEvents
    {
        /// Array of move events
        public b2BodyMoveEvent[] moveEvents;

        /// Number of move events
        public int moveCount;

        public b2BodyEvents(b2BodyMoveEvent[] moveEvents, int moveCount)
        {
            this.moveEvents = moveEvents;
            this.moveCount = moveCount;
        }
    }
}