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
        public B2Transform transform; // Transform of shape B in shape A's frame, the relative pose B in A
        public B2Vec2 translationB; // The translation of shape B, in A's frame
        public float maxFraction; // The fraction of the translation to consider, typically 1
        public bool canEncroach; // Allows shapes with a radius to move slightly closer if already touching
    }
}