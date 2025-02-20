namespace Box2D.NET.Primitives
{
    /// These are performance results returned by dynamic tree queries.
    public class b2TreeStats
    {
        /// Number of internal nodes visited during the query
        public int nodeVisits;

        /// Number of leaf nodes visited during the query
        public int leafVisits;
    }
}