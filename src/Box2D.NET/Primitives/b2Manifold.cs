﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using Box2D.NET.Core;

namespace Box2D.NET.Primitives
{
    /// A contact manifold describes the contact points between colliding shapes.
    /// @note Box2D uses speculative collision so some contact points may be separated.
    public struct b2Manifold
    {
        /// The unit normal vector in world space, points from shape A to bodyB
        public b2Vec2 normal;

        /// Angular impulse applied for rolling resistance. N * m * s = kg * m^2 / s
        public float rollingImpulse;

        /// The manifold points, up to two are possible in 2D
        public UnsafeArray2<b2ManifoldPoint> points;

        private Vector<int> sda;

        /// The number of contacts points, will be 0, 1, or 2
        public int pointCount;
    }
}
