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

        /// Transform of shape B in shape A's frame, the relative pose B in A
        /// (b2InvMulTransforms( worldA, worldB )). The query is origin independent and runs in frame A.
        public B2Transform transform;

        /// Should the proxy radius be considered?
        public bool useRadii;
    }
}
