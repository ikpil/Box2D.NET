namespace Box2D.NET.Primitives
{
    /// The query filter is used to filter collisions between queries and shapes. For example,
    /// you may want a ray-cast representing a projectile to hit players and the static environment
    /// but not debris.
    /// @ingroup shape
    public struct b2QueryFilter
    {
        /// The collision category bits of this query. Normally you would just set one bit.
        public ulong categoryBits;

        /// The collision mask bits. This states the shape categories that this
        /// query would accept for collision.
        public ulong maskBits;

        public b2QueryFilter(ulong categoryBits, ulong maskBits)
        {
            this.categoryBits = categoryBits;
            this.maskBits = maskBits;
        }
    }
}