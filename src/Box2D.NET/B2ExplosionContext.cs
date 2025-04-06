// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2ExplosionContext
    {
        public B2World world;
        public B2Vec2 position;
        public float radius;
        public float falloff;
        public float impulsePerLength;

        public B2ExplosionContext(B2World world, B2Vec2 position, float radius, float falloff, float impulsePerLength)
        {
            this.world = world;
            this.position = position;
            this.radius = radius;
            this.falloff = falloff;
            this.impulsePerLength = impulsePerLength;
        }
    }
}
