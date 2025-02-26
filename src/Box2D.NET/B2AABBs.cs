// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using static Box2D.NET.B2MathFunction;

namespace Box2D.NET
{
    public static class B2AABBs
    {
        // Get surface area of an AABB (the perimeter length)
        public static float b2Perimeter(B2AABB a)
        {
            float wx = a.upperBound.x - a.lowerBound.x;
            float wy = a.upperBound.y - a.lowerBound.y;
            return 2.0f * (wx + wy);
        }

        /// Enlarge a to contain b
        /// @return true if the AABB grew
        public static bool b2EnlargeAABB(ref B2AABB a, B2AABB b)
        {
            bool changed = false;
            if (b.lowerBound.x < a.lowerBound.x)
            {
                a.lowerBound.x = b.lowerBound.x;
                changed = true;
            }

            if (b.lowerBound.y < a.lowerBound.y)
            {
                a.lowerBound.y = b.lowerBound.y;
                changed = true;
            }

            if (a.upperBound.x < b.upperBound.x)
            {
                a.upperBound.x = b.upperBound.x;
                changed = true;
            }

            if (a.upperBound.y < b.upperBound.y)
            {
                a.upperBound.y = b.upperBound.y;
                changed = true;
            }

            return changed;
        }

        /// Do a and b overlap
        public static bool b2AABB_Overlaps(B2AABB a, B2AABB b)
        {
            return !(b.lowerBound.x > a.upperBound.x || b.lowerBound.y > a.upperBound.y || a.lowerBound.x > b.upperBound.x ||
                     a.lowerBound.y > b.upperBound.y);
        }


        /// Is this a valid bounding box? Not Nan or infinity. Upper bound greater than or equal to lower bound.
        public static bool b2IsValidAABB(B2AABB a)
        {
            B2Vec2 d = b2Sub(a.upperBound, a.lowerBound);
            bool valid = d.x >= 0.0f && d.y >= 0.0f;
            valid = valid && b2IsValidVec2(a.lowerBound) && b2IsValidVec2(a.upperBound);
            return valid;
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
            if (absD.x < FLT_EPSILON)
            {
                // parallel
                if (p.x < a.lowerBound.x || a.upperBound.x < p.x)
                {
                    return output;
                }
            }
            else
            {
                float inv_d = 1.0f / d.x;
                float t1 = (a.lowerBound.x - p.x) * inv_d;
                float t2 = (a.upperBound.x - p.x) * inv_d;

                // Sign of the normal vector.
                float s = -1.0f;

                if (t1 > t2)
                {
                    float tmp = t1;
                    t1 = t2;
                    t2 = tmp;
                    s = 1.0f;
                }

                // Push the min up
                if (t1 > tmin)
                {
                    normal.y = 0.0f;
                    normal.x = s;
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
            if (absD.y < FLT_EPSILON)
            {
                // parallel
                if (p.y < a.lowerBound.y || a.upperBound.y < p.y)
                {
                    return output;
                }
            }
            else
            {
                float inv_d = 1.0f / d.y;
                float t1 = (a.lowerBound.y - p.y) * inv_d;
                float t2 = (a.upperBound.y - p.y) * inv_d;

                // Sign of the normal vector.
                float s = -1.0f;

                if (t1 > t2)
                {
                    float tmp = t1;
                    t1 = t2;
                    t2 = tmp;
                    s = 1.0f;
                }

                // Push the min up
                if (t1 > tmin)
                {
                    normal.x = 0.0f;
                    normal.y = s;
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
