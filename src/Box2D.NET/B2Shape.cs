// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public class B2Shape
    {
        public int id;
        public int bodyId;
        public int prevShapeId;
        public int nextShapeId;
        public int sensorIndex;
        public B2ShapeType type;
        public float density;
        public float friction;
        public float restitution;
        public float rollingResistance;
        public float tangentSpeed;
        public int material;

        public B2AABB aabb;
        public B2AABB fatAABB;
        public B2Vec2 localCentroid;
        public int proxyKey;

        public B2Filter filter;
        public object userData;
        public uint customColor;

        // TODO: @ikpil, check union
        // union
        // {
        public B2Capsule capsule;
        public B2Circle circle;
        public B2Polygon polygon;
        public B2Segment segment;
        public B2ChainSegment chainSegment;
        //};

        public ushort generation;
        public bool enableContactEvents;
        public bool enableHitEvents;
        public bool enablePreSolveEvents;
        public bool enlargedAABB;
    }
}
