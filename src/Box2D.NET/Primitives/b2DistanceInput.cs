﻿namespace Box2D.NET.Primitives
{
    /// Input for b2ShapeDistance
    public class b2DistanceInput
    {
        /// The proxy for shape A
        public b2ShapeProxy proxyA;

        /// The proxy for shape B
        public b2ShapeProxy proxyB;

        /// The world transform for shape A
        public b2Transform transformA;

        /// The world transform for shape B
        public b2Transform transformB;

        /// Should the proxy radius be considered?
        public bool useRadii;
    }
}