namespace Box2D.NET
{
    // Used to track shapes that hit sensors using time of impact
    public struct B2SensorHit
    {
        public int sensorId;
        public int visitorId;
    }
}