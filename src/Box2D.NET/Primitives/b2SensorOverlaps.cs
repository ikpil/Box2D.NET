namespace Box2D.NET.Primitives
{
    // Sensors are shapes that live in the broad-phase but never have contacts.
    // At the end of the time step all sensors are queried for overlap with any other shapes.
    // Sensors ignore body type and sleeping.
    // Sensors generate events when there is a new overlap or and overlap disappears.
    // The sensor overlaps don't get cleared until the next time step regardless of the overlapped
    // shapes being destroyed.
    // When a sensor is destroyed.
    public struct b2SensorOverlaps
    {
        public b2Array<int> overlaps;
    }
}