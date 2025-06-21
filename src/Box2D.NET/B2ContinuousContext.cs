// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2ContinuousContext
    {
        public B2World world;
        public B2BodySim fastBodySim;
        public B2Shape fastShape;
        public B2Vec2 centroid1, centroid2;
        public B2Sweep sweep;
        public float fraction;
        public B2FixedArray8<B2SensorHit> sensorHits; // B2_MAX_CONTINUOUS_SENSOR_HITS
        public B2FixedArray8<float> sensorFractions; // B2_MAX_CONTINUOUS_SENSOR_HITS
        public int sensorCount;
    }
}
