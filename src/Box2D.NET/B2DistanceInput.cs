// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Input for b2ShapeDistance
    public struct B2DistanceInput
    {
        /// The proxy for shape A
        public B2ShapeProxy proxyA;

        /// The proxy for shape B
        public B2ShapeProxy proxyB;

        /// The world transform for shape A
        public B2Transform transformA;

        /// The world transform for shape B
        public B2Transform transformB;

        /// Should the proxy radius be considered?
        public bool useRadii;
    }
}
