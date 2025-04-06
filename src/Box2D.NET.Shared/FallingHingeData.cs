namespace Box2D.NET.Shared
{
    public struct FallingHingeData
    {
        public B2BodyId[] bodyIds;
        public int bodyCount;
        public int stepCount;
        public int sleepStep;
        public uint hash;
    }
}