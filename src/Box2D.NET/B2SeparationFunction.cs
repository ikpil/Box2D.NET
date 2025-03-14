// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2SeparationFunction
    {
        public B2ShapeProxy proxyA;
        public B2ShapeProxy proxyB;
        public B2Sweep sweepA, sweepB;
        public B2Vec2 localPoint;
        public B2Vec2 axis;
        public B2SeparationType type;
    }
}
