namespace Box2D.NET.Primitives
{
    /**
     * @defgroup geometry Geometry
     * @brief Geometry types and algorithms
     *
     * Definitions of circles, capsules, segments, and polygons. Various algorithms to compute hulls, mass properties, and so on.
     * @{
     */
    /// Low level ray cast input data
    public struct b2RayCastInput // todo @ikpil, check! for class
    {
        /// Start point of the ray cast
        public b2Vec2 origin;

        /// Translation of the ray cast
        public b2Vec2 translation;

        /// The maximum fraction of the translation to consider, typically 1
        public float maxFraction;

        public b2RayCastInput(b2Vec2 origin, b2Vec2 translation, float maxFraction)
        {
            this.origin = origin;
            this.translation = translation;
            this.maxFraction = maxFraction;
        }
    }
}