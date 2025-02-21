namespace Box2D.NET.Primitives
{
    // This is used to move islands across solver sets
    public class b2IslandSim
    {
        public int islandId;

        public void CopyFrom(b2IslandSim other)
        {
            islandId = other.islandId;
        }
    }
}