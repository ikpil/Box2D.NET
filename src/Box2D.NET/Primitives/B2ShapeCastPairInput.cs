// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    /// Input parameters for b2ShapeCast
    public struct b2ShapeCastPairInput
    {
        public b2ShapeProxy proxyA; // The proxy for shape A
        public b2ShapeProxy proxyB; // The proxy for shape B
        public b2Transform transformA; // The world transform for shape A
        public b2Transform transformB; // The world transform for shape B
        public b2Vec2 translationB; // The translation of shape B
        public float maxFraction; // The fraction of the translation to consider, typically 1
    }
}
