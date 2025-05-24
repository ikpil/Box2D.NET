// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET
{
    public static class B2Hulls
    {
        // quickhull recursion
        public static B2Hull b2RecurseHull(B2Vec2 p1, B2Vec2 p2, Span<B2Vec2> ps, int count)
        {
            B2Hull hull = new B2Hull();
            hull.count = 0;

            if (count == 0)
            {
                return hull;
            }

            // create an edge vector pointing from p1 to p2
            B2Vec2 e = b2Normalize(b2Sub(p2, p1));

            // discard points left of e and find point furthest to the right of e
            Span<B2Vec2> rightPoints = stackalloc B2Vec2[B2_MAX_POLYGON_VERTICES];
            int rightCount = 0;

            int bestIndex = 0;
            float bestDistance = b2Cross(b2Sub(ps[bestIndex], p1), e);
            if (bestDistance > 0.0f)
            {
                rightPoints[rightCount++] = ps[bestIndex];
            }

            for (int i = 1; i < count; ++i)
            {
                float distance = b2Cross(b2Sub(ps[i], p1), e);
                if (distance > bestDistance)
                {
                    bestIndex = i;
                    bestDistance = distance;
                }

                if (distance > 0.0f)
                {
                    rightPoints[rightCount++] = ps[i];
                }
            }

            if (bestDistance < 2.0f * B2_LINEAR_SLOP)
            {
                return hull;
            }

            B2Vec2 bestPoint = ps[bestIndex];

            // compute hull to the right of p1-bestPoint
            B2Hull hull1 = b2RecurseHull(p1, bestPoint, rightPoints, rightCount);

            // compute hull to the right of bestPoint-p2
            B2Hull hull2 = b2RecurseHull(bestPoint, p2, rightPoints, rightCount);

            // stitch together hulls
            for (int i = 0; i < hull1.count; ++i)
            {
                hull.points[hull.count++] = hull1.points[i];
            }

            hull.points[hull.count++] = bestPoint;

            for (int i = 0; i < hull2.count; ++i)
            {
                hull.points[hull.count++] = hull2.points[i];
            }

            B2_ASSERT(hull.count < B2_MAX_POLYGON_VERTICES);

            return hull;
        }

        /// Compute the convex hull of a set of points. Returns an empty hull if it fails.
        /// Some failure cases:
        /// - all points very close together
        /// - all points on a line
        /// - less than 3 points
        /// - more than B2_MAX_POLYGON_VERTICES points
        /// This welds close points and removes collinear points.
        /// @warning Do not modify a hull once it has been computed
        // quickhull algorithm
        // - merges vertices based on B2_LINEAR_SLOP
        // - removes collinear points using B2_LINEAR_SLOP
        // - returns an empty hull if it fails
        public static B2Hull b2ComputeHull(ReadOnlySpan<B2Vec2> points, int count)
        {
            B2Hull hull = new B2Hull();
            hull.count = 0;

            if (count < 3 || count > B2_MAX_POLYGON_VERTICES)
            {
                // check your data
                return hull;
            }

            count = b2MinInt(count, B2_MAX_POLYGON_VERTICES);

            B2AABB aabb = new B2AABB(new B2Vec2(float.MaxValue, float.MaxValue), new B2Vec2(-float.MaxValue, -float.MaxValue));

            // Perform aggressive point welding. First point always remains.
            // Also compute the bounding box for later.
            Span<B2Vec2> ps = stackalloc B2Vec2[B2_MAX_POLYGON_VERTICES];
            int n = 0;
            float linearSlop = B2_LINEAR_SLOP;
            float tolSqr = 16.0f * linearSlop * linearSlop;
            for (int i = 0; i < count; ++i)
            {
                aabb.lowerBound = b2Min(aabb.lowerBound, points[i]);
                aabb.upperBound = b2Max(aabb.upperBound, points[i]);

                B2Vec2 vi = points[i];

                bool unique = true;
                for (int j = 0; j < i; ++j)
                {
                    B2Vec2 vj = points[j];

                    float distSqr = b2DistanceSquared(vi, vj);
                    if (distSqr < tolSqr)
                    {
                        unique = false;
                        break;
                    }
                }

                if (unique)
                {
                    ps[n++] = vi;
                }
            }

            if (n < 3)
            {
                // all points very close together, check your data and check your scale
                return hull;
            }

            // Find an extreme point as the first point on the hull
            B2Vec2 c = b2AABB_Center(aabb);
            int f1 = 0;
            float dsq1 = b2DistanceSquared(c, ps[f1]);
            for (int i = 1; i < n; ++i)
            {
                float dsq = b2DistanceSquared(c, ps[i]);
                if (dsq > dsq1)
                {
                    f1 = i;
                    dsq1 = dsq;
                }
            }

            // remove p1 from working set
            B2Vec2 p1 = ps[f1];
            ps[f1] = ps[n - 1];
            n = n - 1;

            int f2 = 0;
            float dsq2 = b2DistanceSquared(p1, ps[f2]);
            for (int i = 1; i < n; ++i)
            {
                float dsq = b2DistanceSquared(p1, ps[i]);
                if (dsq > dsq2)
                {
                    f2 = i;
                    dsq2 = dsq;
                }
            }

            // remove p2 from working set
            B2Vec2 p2 = ps[f2];
            ps[f2] = ps[n - 1];
            n = n - 1;

            // split the points into points that are left and right of the line p1-p2.
            Span<B2Vec2> rightPoints = stackalloc B2Vec2[B2_MAX_POLYGON_VERTICES - 2];
            int rightCount = 0;

            Span<B2Vec2> leftPoints = stackalloc B2Vec2[B2_MAX_POLYGON_VERTICES - 2];
            int leftCount = 0;

            B2Vec2 e = b2Normalize(b2Sub(p2, p1));

            for (int i = 0; i < n; ++i)
            {
                float d = b2Cross(b2Sub(ps[i], p1), e);

                // slop used here to skip points that are very close to the line p1-p2
                if (d >= 2.0f * linearSlop)
                {
                    rightPoints[rightCount++] = ps[i];
                }
                else if (d <= -2.0f * linearSlop)
                {
                    leftPoints[leftCount++] = ps[i];
                }
            }

            // compute hulls on right and left
            B2Hull hull1 = b2RecurseHull(p1, p2, rightPoints, rightCount);
            B2Hull hull2 = b2RecurseHull(p2, p1, leftPoints, leftCount);

            if (hull1.count == 0 && hull2.count == 0)
            {
                // all points collinear
                return hull;
            }

            // stitch hulls together, preserving CCW winding order
            hull.points[hull.count++] = p1;

            for (int i = 0; i < hull1.count; ++i)
            {
                hull.points[hull.count++] = hull1.points[i];
            }

            hull.points[hull.count++] = p2;

            for (int i = 0; i < hull2.count; ++i)
            {
                hull.points[hull.count++] = hull2.points[i];
            }

            B2_ASSERT(hull.count <= B2_MAX_POLYGON_VERTICES);

            // merge collinear
            bool searching = true;
            while (searching && hull.count > 2)
            {
                searching = false;

                for (int i = 0; i < hull.count; ++i)
                {
                    int i1 = i;
                    int i2 = (i + 1) % hull.count;
                    int i3 = (i + 2) % hull.count;

                    B2Vec2 s1 = hull.points[i1];
                    B2Vec2 s2 = hull.points[i2];
                    B2Vec2 s3 = hull.points[i3];

                    // unit edge vector for s1-s3
                    B2Vec2 r = b2Normalize(b2Sub(s3, s1));

                    float distance = b2Cross(b2Sub(s2, s1), r);
                    if (distance <= 2.0f * linearSlop)
                    {
                        // remove midpoint from hull
                        for (int j = i2; j < hull.count - 1; ++j)
                        {
                            hull.points[j] = hull.points[j + 1];
                        }

                        hull.count -= 1;

                        // continue searching for collinear points
                        searching = true;

                        break;
                    }
                }
            }

            if (hull.count < 3)
            {
                // all points collinear, shouldn't be reached since this was validated above
                hull.count = 0;
            }

            return hull;
        }

        /// This determines if a hull is valid. Checks for:
        /// - convexity
        /// - collinear points
        /// This is expensive and should not be called at runtime.
        public static bool b2ValidateHull(ref B2Hull hull)
        {
            if (hull.count < 3 || B2_MAX_POLYGON_VERTICES < hull.count)
            {
                return false;
            }

            // test that every point is behind every edge
            for (int i = 0; i < hull.count; ++i)
            {
                // create an edge vector
                int i1 = i;
                int i2 = i < hull.count - 1 ? i1 + 1 : 0;
                B2Vec2 p = hull.points[i1];
                B2Vec2 e = b2Normalize(b2Sub(hull.points[i2], p));

                for (int j = 0; j < hull.count; ++j)
                {
                    // skip points that subtend the current edge
                    if (j == i1 || j == i2)
                    {
                        continue;
                    }

                    float distance = b2Cross(b2Sub(hull.points[j], p), e);
                    if (distance >= 0.0f)
                    {
                        return false;
                    }
                }
            }

            // test for collinear points
            float linearSlop = B2_LINEAR_SLOP;
            for (int i = 0; i < hull.count; ++i)
            {
                int i1 = i;
                int i2 = (i + 1) % hull.count;
                int i3 = (i + 2) % hull.count;

                B2Vec2 p1 = hull.points[i1];
                B2Vec2 p2 = hull.points[i2];
                B2Vec2 p3 = hull.points[i3];

                B2Vec2 e = b2Normalize(b2Sub(p3, p1));

                float distance = b2Cross(b2Sub(p2, p1), e);
                if (distance <= linearSlop)
                {
                    // p1-p2-p3 are collinear
                    return false;
                }
            }

            return true;
        }
    }
}