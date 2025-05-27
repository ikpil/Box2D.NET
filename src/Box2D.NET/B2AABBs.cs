// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;
using static Box2D.NET.B2MathFunction;

namespace Box2D.NET
{
    public static class B2AABBs
    {
        // Get surface area of an AABB (the perimeter length)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float b2Perimeter(B2AABB a)
        {
            float wx = a.upperBound.X - a.lowerBound.X;
            float wy = a.upperBound.Y - a.lowerBound.Y;
            return 2.0f * (wx + wy);
        }

        /// Enlarge a to contain b
        /// @return true if the AABB grew
        public static bool b2EnlargeAABB(ref B2AABB a, B2AABB b)
        {
            bool changed = false;
            if (b.lowerBound.X < a.lowerBound.X)
            {
                a.lowerBound.X = b.lowerBound.X;
                changed = true;
            }

            if (b.lowerBound.Y < a.lowerBound.Y)
            {
                a.lowerBound.Y = b.lowerBound.Y;
                changed = true;
            }

            if (a.upperBound.X < b.upperBound.X)
            {
                a.upperBound.X = b.upperBound.X;
                changed = true;
            }

            if (a.upperBound.Y < b.upperBound.Y)
            {
                a.upperBound.Y = b.upperBound.Y;
                changed = true;
            }

            return changed;
        }


        // Ray cast an AABB
        // From Real-time Collision Detection, p179.
        public static B2CastOutput b2AABB_RayCast(B2AABB a, B2Vec2 p1, B2Vec2 p2)
        {
            // Radius not handled
            B2CastOutput output = new B2CastOutput();

            float tmin = -float.MaxValue;
            float tmax = float.MaxValue;

            B2Vec2 p = p1;
            B2Vec2 d = b2Sub(p2, p1);
            B2Vec2 absD = b2Abs(d);

            B2Vec2 normal = b2Vec2_zero;

            // x-coordinate
            if (absD.X < FLT_EPSILON)
            {
                // parallel
                if (p.X < a.lowerBound.X || a.upperBound.X < p.X)
                {
                    return output;
                }
            }
            else
            {
                float inv_d = 1.0f / d.X;
                float t1 = (a.lowerBound.X - p.X) * inv_d;
                float t2 = (a.upperBound.X - p.X) * inv_d;

                // Sign of the normal vector.
                float s = -1.0f;

                if (t1 > t2)
                {
                    (t1, t2) = (t2, t1);
                    s = 1.0f;
                }

                // Push the min up
                if (t1 > tmin)
                {
                    normal.Y = 0.0f;
                    normal.X = s;
                    tmin = t1;
                }

                // Pull the max down
                tmax = b2MinFloat(tmax, t2);

                if (tmin > tmax)
                {
                    return output;
                }
            }

            // y-coordinate
            if (absD.Y < FLT_EPSILON)
            {
                // parallel
                if (p.Y < a.lowerBound.Y || a.upperBound.Y < p.Y)
                {
                    return output;
                }
            }
            else
            {
                float inv_d = 1.0f / d.Y;
                float t1 = (a.lowerBound.Y - p.Y) * inv_d;
                float t2 = (a.upperBound.Y - p.Y) * inv_d;

                // Sign of the normal vector.
                float s = -1.0f;

                if (t1 > t2)
                {
                    (t1, t2) = (t2, t1);
                    s = 1.0f;
                }

                // Push the min up
                if (t1 > tmin)
                {
                    normal.X = 0.0f;
                    normal.Y = s;
                    tmin = t1;
                }

                // Pull the max down
                tmax = b2MinFloat(tmax, t2);

                if (tmin > tmax)
                {
                    return output;
                }
            }

            // Does the ray start inside the box?
            // Does the ray intersect beyond the max fraction?
            if (tmin < 0.0f || 1.0f < tmin)
            {
                return output;
            }

            // Intersection.
            output.fraction = tmin;
            output.normal = normal;
            output.point = b2Lerp(p1, p2, tmin);
            output.hit = true;
            return output;
        }
    }
}