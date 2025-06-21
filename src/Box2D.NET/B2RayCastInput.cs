// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /**
     * @defgroup geometry Geometry
     * @brief Geometry types and algorithms
     *
     * Definitions of circles, capsules, segments, and polygons. Various algorithms to compute hulls, mass properties, and so on.
     * Functions should take the shape as the first argument to assist editor auto-complete.
     * @{
     */
    /// Low level ray cast input data
    public struct B2RayCastInput
    {
        /// Start point of the ray cast
        public B2Vec2 origin;

        /// Translation of the ray cast
        public B2Vec2 translation;

        /// The maximum fraction of the translation to consider, typically 1
        public float maxFraction;

        public B2RayCastInput(B2Vec2 origin, B2Vec2 translation, float maxFraction)
        {
            this.origin = origin;
            this.translation = translation;
            this.maxFraction = maxFraction;
        }
    }
}
