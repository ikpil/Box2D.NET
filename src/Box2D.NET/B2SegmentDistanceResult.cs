// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /**@}*/
    /**
     * @defgroup distance Distance
     * Functions for computing the distance between shapes.
     *
     * These are advanced functions you can use to perform distance calculations. There
     * are functions for computing the closest points between shapes, doing linear shape casts,
     * and doing rotational shape casts. The latter is called time of impact (TOI).
     * @{
     */
    /// Result of computing the distance between two line segments
    public struct B2SegmentDistanceResult
    {
        /// The closest point on the first segment
        public B2Vec2 closest1;

        /// The closest point on the second segment
        public B2Vec2 closest2;

        /// The barycentric coordinate on the first segment
        public float fraction1;

        /// The barycentric coordinate on the second segment
        public float fraction2;

        /// The squared distance between the closest points
        public float distanceSquared;
    }
}
