// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2SensorQueryContext
    {
        public B2World world;
        public B2SensorTaskContext taskContext;
        public B2Sensor sensor;
        public B2Shape sensorShape;
        public B2Transform transform;
    }
}
