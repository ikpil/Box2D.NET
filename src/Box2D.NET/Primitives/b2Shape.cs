// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    public class b2Shape
    {
        public int id;
        public int bodyId;
        public int prevShapeId;
        public int nextShapeId;
        public int sensorIndex;
        public b2ShapeType type;
        public float density;
        public float friction;
        public float restitution;
        public float rollingResistance;
        public float tangentSpeed;
        public int material;

        public b2AABB aabb;
        public b2AABB fatAABB;
        public b2Vec2 localCentroid;
        public int proxyKey;

        public b2Filter filter;
        public object userData;
        public uint customColor;

        // TODO: @ikpil, check union
        // union
        // {
        public b2Capsule capsule;
        public b2Circle circle;
        public b2Polygon polygon;
        public b2Segment segment;
        public b2ChainSegment chainSegment;
        //};

        public ushort generation;
        public bool enableContactEvents;
        public bool enableHitEvents;
        public bool enablePreSolveEvents;
        public bool enlargedAABB;
    }
}
