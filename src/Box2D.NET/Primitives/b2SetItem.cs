namespace Box2D.NET.Primitives
{
    // @ikpil, must be strcut
    public struct b2SetItem
    {
        public ulong key;
        public uint hash;

        public void Clear()
        {
            key = 0;
            hash = 0;
        }
    }
}