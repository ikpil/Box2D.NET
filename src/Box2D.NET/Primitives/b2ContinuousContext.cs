// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    public class b2ContinuousContext
    {
        public b2World world;
        public b2BodySim fastBodySim;
        public b2Shape fastShape;
        public b2Vec2 centroid1, centroid2;
        public b2Sweep sweep;
        public float fraction;
    }
}
