// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Input parameters for b2ShapeCast
    public struct B2ShapeCastPairInput
    {
        public B2ShapeProxy proxyA; // The proxy for shape A
        public B2ShapeProxy proxyB; // The proxy for shape B
        public B2Transform transformA; // The world transform for shape A
        public B2Transform transformB; // The world transform for shape B
        public B2Vec2 translationB; // The translation of shape B
        public float maxFraction; // The fraction of the translation to consider, typically 1
        public bool canEncroach; // Allows shapes with a radius to move slightly closer if already touching
    }
}