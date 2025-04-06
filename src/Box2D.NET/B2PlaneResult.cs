﻿namespace Box2D.NET
{
    public struct B2PlaneResult
    {
        public B2Plane plane;
        public B2Vec2 point;
        public bool hit;

        public B2PlaneResult(B2Plane plane, B2Vec2 point, bool hit)
        {
            this.plane = plane;
            this.point = point;
            this.hit = hit;
        }
    }
}