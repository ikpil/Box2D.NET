// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    public class b2SensorQueryContext
    {
        public b2World world;
        public b2SensorTaskContext taskContext;
        public b2Sensor sensor;
        public b2Shape sensorShape;
        public b2Transform transform;
    }
}
