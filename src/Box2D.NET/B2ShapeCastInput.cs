// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Low level shape cast input in generic form. This allows casting an arbitrary point
    /// cloud wrap with a radius. For example, a circle is a single point with a non-zero radius.
    /// A capsule is two points with a non-zero radius. A box is four points with a zero radius.
    public struct B2ShapeCastInput
    {
        /// A generic shape
        public B2ShapeProxy proxy;

        /// The translation of the shape cast
        public B2Vec2 translation;

        /// The maximum fraction of the translation to consider, typically 1
        public float maxFraction;
        
        /// Allow shape cast to encroach when initially touching. This only works if the radius is greater than zero.
        public bool canEncroach;
    }
}
