﻿namespace Box2D.NET
{
    public struct B2WorldMoverCastContext
    {
        public B2World world;
        public B2QueryFilter filter;
        public float fraction;

        public B2WorldMoverCastContext(B2World world, B2QueryFilter filter, float fraction)
        {
            this.world = world;
            this.filter = filter;
            this.fraction = fraction;
        }
    }
}