// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET
{
    /**
     * @defgroup math Math
     * @brief Vector math types and functions
     * @{
     */
    public static class B2MathFunction
    {
        /**@}*/
        /**
         * @addtogroup math
         * @{
         */
        /// https://en.wikipedia.org/wiki/Pi
        public const float B2_PI = 3.14159265359f;

        public const float FLT_EPSILON = 1.1920929e-7f;

        public static readonly B2Vec2 b2Vec2_zero = new B2Vec2(0.0f, 0.0f);
        public static readonly B2Rot b2Rot_identity = new B2Rot(1.0f, 0.0f);
        public static readonly B2Transform b2Transform_identity = new B2Transform(new B2Vec2(0.0f, 0.0f), new B2Rot(1.0f, 0.0f));
        public static readonly B2Mat22 b2Mat22_zero = new B2Mat22(new B2Vec2(0.0f, 0.0f), new B2Vec2(0.0f, 0.0f));

        /// @return the minimum of two integers
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int b2MinInt(int a, int b)
        {
            return a < b ? a : b;
        }

        /// @return the maximum of two integers
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int b2MaxInt(int a, int b)
        {
            return a > b ? a : b;
        }

        /// @return the absolute value of an integer
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int b2AbsInt(int a)
        {
            return a < 0 ? -a : a;
        }

        /// @return an integer clamped between a lower and upper bound
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int b2ClampInt(int a, int lower, int upper)
        {
            return a < lower ? lower : (a > upper ? upper : a);
        }

        /// @return the minimum of two floats
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float b2MinFloat(float a, float b)
        {
            return a < b ? a : b;
        }

        /// @return the maximum of two floats
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float b2MaxFloat(float a, float b)
        {
            return a > b ? a : b;
        }

        /// @return the absolute value of a float
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float b2AbsFloat(float a)
        {
            return a < 0 ? -a : a;
        }

        /// @return a float clamped between a lower and upper bound
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float b2ClampFloat(float a, float lower, float upper)
        {
            return a < lower ? lower : (a > upper ? upper : a);
        }


        /// Vector dot product
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float b2Dot(B2Vec2 a, B2Vec2 b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        /// Vector cross product. In 2D this yields a scalar.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float b2Cross(B2Vec2 a, B2Vec2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        /// Perform the cross product on a vector and a scalar. In 2D this produces a vector.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2CrossVS(B2Vec2 v, float s)
        {
            return new B2Vec2(s * v.Y, -s * v.X);
        }

        /// Perform the cross product on a scalar and a vector. In 2D this produces a vector.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2CrossSV(float s, B2Vec2 v)
        {
            return new B2Vec2(-s * v.Y, s * v.X);
        }

        /// Get a left pointing perpendicular vector. Equivalent to b2CrossSV(1.0f, v)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2LeftPerp(B2Vec2 v)
        {
            return new B2Vec2(-v.Y, v.X);
        }

        /// Get a right pointing perpendicular vector. Equivalent to b2CrossVS(v, 1.0f)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2RightPerp(B2Vec2 v)
        {
            return new B2Vec2(v.Y, -v.X);
        }

        /// Vector addition
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2Add(B2Vec2 a, B2Vec2 b)
        {
            return new B2Vec2(a.X + b.X, a.Y + b.Y);
        }

        /// Vector subtraction
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2Sub(B2Vec2 a, B2Vec2 b)
        {
            return new B2Vec2(a.X - b.X, a.Y - b.Y);
        }

        /// Vector negation
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2Neg(B2Vec2 a)
        {
            return new B2Vec2(-a.X, -a.Y);
        }

        /// Vector linear interpolation
        /// https://fgiesen.wordpress.com/2012/08/15/linear-interpolation-past-present-and-future/
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2Lerp(B2Vec2 a, B2Vec2 b, float t)
        {
            return new B2Vec2((1.0f - t) * a.X + t * b.X, (1.0f - t) * a.Y + t * b.Y);
        }

        /// Component-wise multiplication
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2Mul(B2Vec2 a, B2Vec2 b)
        {
            return new B2Vec2(a.X * b.X, a.Y * b.Y);
        }

        /// Multiply a scalar and vector
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2MulSV(float s, B2Vec2 v)
        {
            return new B2Vec2(s * v.X, s * v.Y);
        }

        /// a + s * b
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2MulAdd(B2Vec2 a, float s, B2Vec2 b)
        {
            return new B2Vec2(a.X + s * b.X, a.Y + s * b.Y);
        }

        /// a - s * b
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2MulSub(B2Vec2 a, float s, B2Vec2 b)
        {
            return new B2Vec2(a.X - s * b.X, a.Y - s * b.Y);
        }

        /// Component-wise absolute vector
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2Abs(B2Vec2 a)
        {
            B2Vec2 b;
            b.X = b2AbsFloat(a.X);
            b.Y = b2AbsFloat(a.Y);
            return b;
        }

        /// Component-wise minimum vector
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2Min(B2Vec2 a, B2Vec2 b)
        {
            B2Vec2 c;
            c.X = b2MinFloat(a.X, b.X);
            c.Y = b2MinFloat(a.Y, b.Y);
            return c;
        }

        /// Component-wise maximum vector
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2Max(B2Vec2 a, B2Vec2 b)
        {
            B2Vec2 c;
            c.X = b2MaxFloat(a.X, b.X);
            c.Y = b2MaxFloat(a.Y, b.Y);
            return c;
        }

        /// Component-wise clamp vector v into the range [a, b]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2Clamp(B2Vec2 v, B2Vec2 a, B2Vec2 b)
        {
            B2Vec2 c;
            c.X = b2ClampFloat(v.X, a.X, b.X);
            c.Y = b2ClampFloat(v.Y, a.Y, b.Y);
            return c;
        }

        /// Get the length of this vector (the norm)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float b2Length(B2Vec2 v)
        {
            return MathF.Sqrt(v.X * v.X + v.Y * v.Y);
        }

        /// Get the distance between two points
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float b2Distance(B2Vec2 a, B2Vec2 b)
        {
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        /// Convert a vector into a unit vector if possible, otherwise returns the zero vector.
        /// todo MSVC is not inlining this function in several places per warning 4710
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2Normalize(B2Vec2 v)
        {
            float length = MathF.Sqrt(v.X * v.X + v.Y * v.Y);
            if (length < FLT_EPSILON)
            {
                return new B2Vec2(0.0f, 0.0f);
            }

            float invLength = 1.0f / length;
            B2Vec2 n = new B2Vec2(invLength * v.X, invLength * v.Y);
            return n;
        }

        /// Determines if the provided vector is normalized (norm(a) == 1).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2IsNormalized(B2Vec2 a)
        {
            float aa = b2Dot(a, a);
            return b2AbsFloat(1.0f - aa) < 100.0f * FLT_EPSILON;
        }

        /// Convert a vector into a unit vector if possible, otherwise returns the zero vector. Also
        /// outputs the length.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2GetLengthAndNormalize(ref float length, B2Vec2 v)
        {
            length = MathF.Sqrt(v.X * v.X + v.Y * v.Y);
            if (length < FLT_EPSILON)
            {
                return new B2Vec2(0.0f, 0.0f);
            }

            float invLength = 1.0f / length;
            B2Vec2 n = new B2Vec2(invLength * v.X, invLength * v.Y);
            return n;
        }

        /// Normalize rotation
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Rot b2NormalizeRot(B2Rot q)
        {
            float mag = MathF.Sqrt(q.s * q.s + q.c * q.c);
            float invMag = mag > 0.0 ? 1.0f / mag : 0.0f;
            B2Rot qn = new B2Rot(q.c * invMag, q.s * invMag);
            return qn;
        }

        /// Integrate rotation from angular velocity
        /// @param q1 initial rotation
        /// @param deltaAngle the angular displacement in radians
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Rot b2IntegrateRotation(B2Rot q1, float deltaAngle)
        {
            // dc/dt = -omega * sin(t)
            // ds/dt = omega * cos(t)
            // c2 = c1 - omega * h * s1
            // s2 = s1 + omega * h * c1
            B2Rot q2 = new B2Rot(q1.c - deltaAngle * q1.s, q1.s + deltaAngle * q1.c);
            float mag = MathF.Sqrt(q2.s * q2.s + q2.c * q2.c);
            float invMag = mag > 0.0 ? 1.0f / mag : 0.0f;
            B2Rot qn = new B2Rot(q2.c * invMag, q2.s * invMag);
            return qn;
        }

        /// Get the length squared of this vector
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float b2LengthSquared(B2Vec2 v)
        {
            return v.X * v.X + v.Y * v.Y;
        }

        /// Get the distance squared between points
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float b2DistanceSquared(B2Vec2 a, B2Vec2 b)
        {
            B2Vec2 c = new B2Vec2(b.X - a.X, b.Y - a.Y);
            return c.X * c.X + c.Y * c.Y;
        }

        /// Make a rotation using an angle in radians
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Rot b2MakeRot(float radians)
        {
            B2CosSin cs = b2ComputeCosSin(radians);
            return new B2Rot(cs.cosine, cs.sine);
        }

        /// Make a rotation using a unit vector
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Rot b2MakeRotFromUnitVector(B2Vec2 unitVector)
        {
            B2_ASSERT(b2IsNormalized(unitVector));
            return new B2Rot(unitVector.X, unitVector.Y);
        }

        /// Is this rotation normalized?
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2IsNormalizedRot(B2Rot q)
        {
            // larger tolerance due to failure on mingw 32-bit
            float qq = q.s * q.s + q.c * q.c;
            return 1.0f - 0.0006f < qq && qq < 1.0f + 0.0006f;
        }

        /// Normalized linear interpolation
        /// https://fgiesen.wordpress.com/2012/08/15/linear-interpolation-past-present-and-future/
        ///	https://web.archive.org/web/20170825184056/http://number-none.com/product/Understanding%20Slerp,%20Then%20Not%20Using%20It/
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Rot b2NLerp(B2Rot q1, B2Rot q2, float t)
        {
            float omt = 1.0f - t;
            B2Rot q = new B2Rot
            {
                c = omt * q1.c + t * q2.c,
                s = omt * q1.s + t * q2.s,
            };

            float mag = MathF.Sqrt(q.s * q.s + q.c * q.c);
            float invMag = mag > 0.0 ? 1.0f / mag : 0.0f;
            B2Rot qn = new B2Rot(q.c * invMag, q.s * invMag);
            return qn;
        }

        /// Compute the angular velocity necessary to rotate between two rotations over a give time
        /// @param q1 initial rotation
        /// @param q2 final rotation
        /// @param inv_h inverse time step
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float b2ComputeAngularVelocity(B2Rot q1, B2Rot q2, float inv_h)
        {
            // ds/dt = omega * cos(t)
            // dc/dt = -omega * sin(t)
            // s2 = s1 + omega * h * c1
            // c2 = c1 - omega * h * s1

            // omega * h * s1 = c1 - c2
            // omega * h * c1 = s2 - s1
            // omega * h = (c1 - c2) * s1 + (s2 - s1) * c1;
            // omega * h = s1 * c1 - c2 * s1 + s2 * c1 - s1 * c1
            // omega * h = s2 * c1 - c2 * s1 = sin(a2 - a1) ~= a2 - a1 for small delta
            float omega = inv_h * (q2.s * q1.c - q2.c * q1.s);
            return omega;
        }

        /// Get the angle in radians in the range [-pi, pi]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float b2Rot_GetAngle(B2Rot q)
        {
            return b2Atan2(q.s, q.c);
        }

        /// Get the x-axis
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2Rot_GetXAxis(B2Rot q)
        {
            B2Vec2 v = new B2Vec2(q.c, q.s);
            return v;
        }

        /// Get the y-axis
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2Rot_GetYAxis(B2Rot q)
        {
            B2Vec2 v = new B2Vec2(-q.s, q.c);
            return v;
        }

        /// Multiply two rotations: q * r
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Rot b2MulRot(B2Rot q, B2Rot r)
        {
            // [qc -qs] * [rc -rs] = [qc*rc-qs*rs -qc*rs-qs*rc]
            // [qs  qc]   [rs  rc]   [qs*rc+qc*rs -qs*rs+qc*rc]
            // s(q + r) = qs * rc + qc * rs
            // c(q + r) = qc * rc - qs * rs
            B2Rot qr;
            qr.s = q.s * r.c + q.c * r.s;
            qr.c = q.c * r.c - q.s * r.s;
            return qr;
        }

        /// Transpose multiply two rotations: inv(a) * b
        /// This rotates a vector local in frame b into a vector local in frame a
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Rot b2InvMulRot(B2Rot a, B2Rot b)
        {
            // [ ac as] * [bc -bs] = [ac*bc+qs*bs -ac*bs+as*bc]
            // [-as ac]   [bs  bc]   [-as*bc+ac*bs as*bs+ac*bc]
            // s(a - b) = ac * bs - as * bc
            // c(a - b) = ac * bc + as * bs
            B2Rot r;
            r.s = a.c * b.s - a.s * b.c;
            r.c = a.c * b.c + a.s * b.s;
            return r;
        }

        /// Relative angle between a and b
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float b2RelativeAngle(B2Rot a, B2Rot b)
        {
            // sin(b - a) = bs * ac - bc * as
            // cos(b - a) = bc * ac + bs * as
            float s = a.c * b.s - a.s * b.c;
            float c = a.c * b.c + a.s * b.s;
            return b2Atan2(s, c);
        }

        /// Convert any angle into the range [-pi, pi]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float b2UnwindAngle(float radians)
        {
            // Assuming this is deterministic
            return (float)Math.IEEERemainder(radians, 2.0f * B2_PI);
        }

        /// Rotate a vector
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2RotateVector(B2Rot q, B2Vec2 v)
        {
            return new B2Vec2(q.c * v.X - q.s * v.Y, q.s * v.X + q.c * v.Y);
        }

        /// Inverse rotate a vector
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2InvRotateVector(B2Rot q, B2Vec2 v)
        {
            return new B2Vec2(q.c * v.X + q.s * v.Y, -q.s * v.X + q.c * v.Y);
        }

        /// Transform a point (e.g. local space to world space)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2TransformPoint(ref B2Transform t, B2Vec2 p)
        {
            float x = (t.q.c * p.X - t.q.s * p.Y) + t.p.X;
            float y = (t.q.s * p.X + t.q.c * p.Y) + t.p.Y;

            return new B2Vec2(x, y);
        }

        /// Inverse transform a point (e.g. world space to local space)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2InvTransformPoint(B2Transform t, B2Vec2 p)
        {
            float vx = p.X - t.p.X;
            float vy = p.Y - t.p.Y;
            return new B2Vec2(t.q.c * vx + t.q.s * vy, -t.q.s * vx + t.q.c * vy);
        }

        /// Multiply two transforms. If the result is applied to a point p local to frame B,
        /// the transform would first convert p to a point local to frame A, then into a point
        /// in the world frame.
        /// v2 = A.q.Rot(B.q.Rot(v1) + B.p) + A.p
        ///    = (A.q * B.q).Rot(v1) + A.q.Rot(B.p) + A.p
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Transform b2MulTransforms(B2Transform A, B2Transform B)
        {
            B2Transform C;
            C.q = b2MulRot(A.q, B.q);
            C.p = b2Add(b2RotateVector(A.q, B.p), A.p);
            return C;
        }

        /// Creates a transform that converts a local point in frame B to a local point in frame A.
        /// v2 = A.q' * (B.q * v1 + B.p - A.p)
        ///    = A.q' * B.q * v1 + A.q' * (B.p - A.p)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Transform b2InvMulTransforms(B2Transform A, B2Transform B)
        {
            B2Transform C;
            C.q = b2InvMulRot(A.q, B.q);
            C.p = b2InvRotateVector(A.q, b2Sub(B.p, A.p));
            return C;
        }

        /// Multiply a 2-by-2 matrix times a 2D vector
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2MulMV(B2Mat22 A, B2Vec2 v)
        {
            B2Vec2 u = new B2Vec2
            {
                X = A.cx.X * v.X + A.cy.X * v.Y,
                Y = A.cx.Y * v.X + A.cy.Y * v.Y,
            };
            return u;
        }

        /// Get the inverse of a 2-by-2 matrix
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Mat22 b2GetInverse22(B2Mat22 A)
        {
            float a = A.cx.X, b = A.cy.X, c = A.cx.Y, d = A.cy.Y;
            float det = a * d - b * c;
            if (det != 0.0f)
            {
                det = 1.0f / det;
            }

            B2Mat22 B = new B2Mat22(new B2Vec2(det * d, -det * c), new B2Vec2(-det * b, det * a));
            return B;
        }

        /// Solve A * x = b, where b is a column vector. This is more efficient
        /// than computing the inverse in one-shot cases.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2Solve22(B2Mat22 A, B2Vec2 b)
        {
            float a11 = A.cx.X, a12 = A.cy.X, a21 = A.cx.Y, a22 = A.cy.Y;
            float det = a11 * a22 - a12 * a21;
            if (det != 0.0f)
            {
                det = 1.0f / det;
            }

            B2Vec2 x = new B2Vec2(det * (a22 * b.X - a12 * b.Y), det * (a11 * b.Y - a21 * b.X));
            return x;
        }

        /// Does a fully contain b
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2AABB_Contains(B2AABB a, B2AABB b)
        {
            bool s = true;
            s = s && a.lowerBound.X <= b.lowerBound.X;
            s = s && a.lowerBound.Y <= b.lowerBound.Y;
            s = s && b.upperBound.X <= a.upperBound.X;
            s = s && b.upperBound.Y <= a.upperBound.Y;
            return s;
        }

        /// Get the center of the AABB.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2AABB_Center(B2AABB a)
        {
            B2Vec2 b = new B2Vec2(0.5f * (a.lowerBound.X + a.upperBound.X), 0.5f * (a.lowerBound.Y + a.upperBound.Y));
            return b;
        }

        /// Get the extents of the AABB (half-widths).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2AABB_Extents(B2AABB a)
        {
            B2Vec2 b = new B2Vec2(0.5f * (a.upperBound.X - a.lowerBound.X), 0.5f * (a.upperBound.Y - a.lowerBound.Y));
            return b;
        }

        /// Union of two AABBs
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2AABB b2AABB_Union(B2AABB a, B2AABB b)
        {
            B2AABB c;
            c.lowerBound.X = b2MinFloat(a.lowerBound.X, b.lowerBound.X);
            c.lowerBound.Y = b2MinFloat(a.lowerBound.Y, b.lowerBound.Y);
            c.upperBound.X = b2MaxFloat(a.upperBound.X, b.upperBound.X);
            c.upperBound.Y = b2MaxFloat(a.upperBound.Y, b.upperBound.Y);
            return c;
        }

        /// Do a and b overlap
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2AABB_Overlaps(B2AABB a, B2AABB b)
        {
            return !(b.lowerBound.X > a.upperBound.X || b.lowerBound.Y > a.upperBound.Y || a.lowerBound.X > b.upperBound.X ||
                     a.lowerBound.Y > b.upperBound.Y);
        }

        /// Compute the bounding box of an array of circles
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2AABB b2MakeAABB(ReadOnlySpan<B2Vec2> points, int count, float radius)
        {
            B2_ASSERT(count > 0);
            B2AABB a = new B2AABB(points[0], points[0]);
            for (int i = 1; i < count; ++i)
            {
                a.lowerBound = b2Min(a.lowerBound, points[i]);
                a.upperBound = b2Max(a.upperBound, points[i]);
            }

            B2Vec2 r = new B2Vec2(radius, radius);
            a.lowerBound = b2Sub(a.lowerBound, r);
            a.upperBound = b2Add(a.upperBound, r);

            return a;
        }

        /// Signed separation of a point from a plane
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float b2PlaneSeparation(B2Plane plane, B2Vec2 point)
        {
            return b2Dot(plane.normal, point) - plane.offset;
        }

        /// One-dimensional mass-spring-damper simulation. Returns the new velocity given the position and time step.
        /// You can then compute the new position using:
        /// position += timeStep * newVelocity
        /// This drives towards a zero position. By using implicit integration we get a stable solution
        /// that doesn't require transcendental functions.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float b2SpringDamper(float hertz, float dampingRatio, float position, float velocity, float timeStep)
        {
            float omega = 2.0f * B2_PI * hertz;
            float omegaH = omega * timeStep;
            return (velocity - omega * omegaH * position) / (1.0f + 2.0f * dampingRatio * omegaH + omegaH * omegaH);
        }

        /**@}*/
        //B2_ASSERT( sizeof( int ) == sizeof( int ), "Box2D expects int and int to be the same" );

        /// Is this a valid number? Not NaN or infinity.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2IsValidFloat(float a)
        {
            if (float.IsNaN(a))
            {
                return false;
            }

            if (float.IsInfinity(a))
            {
                return false;
            }

            return true;
        }

        /// Is this a valid vector? Not NaN or infinity.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2IsValidVec2(B2Vec2 v)
        {
            if (float.IsNaN(v.X) || float.IsNaN(v.Y))
            {
                return false;
            }

            if (float.IsInfinity(v.X) || float.IsInfinity(v.Y))
            {
                return false;
            }

            return true;
        }

        /// Is this a valid rotation? Not NaN or infinity. Is normalized.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2IsValidRotation(B2Rot q)
        {
            if (float.IsNaN(q.s) || float.IsNaN(q.c))
            {
                return false;
            }

            if (float.IsInfinity(q.s) || float.IsInfinity(q.c))
            {
                return false;
            }

            return b2IsNormalizedRot(q);
        }

        /// Is this a valid transform? Not NaN or infinity. Rotation is normalized.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2IsValidTransform(B2Transform t)
        {
            if (b2IsValidVec2(t.p) == false)
            {
                return false;
            }

            return b2IsValidRotation(t.q);
        }

        /// Is this a valid bounding box? Not Nan or infinity. Upper bound greater than or equal to lower bound.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2IsValidAABB(B2AABB a)
        {
            B2Vec2 d = b2Sub(a.upperBound, a.lowerBound);
            bool valid = d.X >= 0.0f && d.Y >= 0.0f;
            valid = valid && b2IsValidVec2(a.lowerBound) && b2IsValidVec2(a.upperBound);
            return valid;
        }

        /// Is this a valid plane? Normal is a unit vector. Not Nan or infinity.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2IsValidPlane(B2Plane a)
        {
            return b2IsValidVec2(a.normal) && b2IsNormalized(a.normal) && b2IsValidFloat(a.offset);
        }

        /// Compute an approximate arctangent in the range [-pi, pi]
        /// This is hand coded for cross-platform determinism. The MathF.Atan2
        /// function in the standard library is not cross-platform deterministic.
        ///	Accurate to around 0.0023 degrees
        // https://stackoverflow.com/questions/46210708/atan2-approximation-with-11bits-in-mantissa-on-x86with-sse2-and-armwith-vfpv4
        public static float b2Atan2(float y, float x)
        {
            // Added check for (0,0) to match MathF.Atan2 and avoid NaN
            if (x == 0.0f && y == 0.0f)
            {
                return 0.0f;
            }

            float ax = b2AbsFloat(x);
            float ay = b2AbsFloat(y);
            float mx = b2MaxFloat(ay, ax);
            float mn = b2MinFloat(ay, ax);
            float a = mn / mx;

            // Minimax polynomial approximation to atan(a) on [0,1]
            float s = a * a;
            float c = s * a;
            float q = s * s;
            float r = 0.024840285f * q + 0.18681418f;
            float t = -0.094097948f * q - 0.33213072f;
            r = r * s + t;
            r = r * c + a;

            // Map to full circle
            if (ay > ax)
            {
                r = 1.57079637f - r;
            }

            if (x < 0)
            {
                r = 3.14159274f - r;
            }

            if (y < 0)
            {
                r = -r;
            }

            return r;
        }

        /// Compute the cosine and sine of an angle in radians. Implemented
        /// for cross-platform determinism.
        // Approximate cosine and sine for determinism. In my testing MathF.Cos and MathF.Sin produced
        // the same results on x64 and ARM using MSVC, GCC, and Clang. However, I don't trust
        // this result.
        // https://en.wikipedia.org/wiki/Bh%C4%81skara_I%27s_sine_approximation_formula
        public static B2CosSin b2ComputeCosSin(float radians)
        {
            float x = b2UnwindAngle(radians);
            float pi2 = B2_PI * B2_PI;

            // cosine needs angle in [-pi/2, pi/2]
            float c;
            if (x < -0.5f * B2_PI)
            {
                float y = x + B2_PI;
                float y2 = y * y;
                c = -(pi2 - 4.0f * y2) / (pi2 + y2);
            }
            else if (x > 0.5f * B2_PI)
            {
                float y = x - B2_PI;
                float y2 = y * y;
                c = -(pi2 - 4.0f * y2) / (pi2 + y2);
            }
            else
            {
                float y2 = x * x;
                c = (pi2 - 4.0f * y2) / (pi2 + y2);
            }

            // sine needs angle in [0, pi]
            float s;
            if (x < 0.0f)
            {
                float y = x + B2_PI;
                s = -16.0f * y * (B2_PI - y) / (5.0f * pi2 - 4.0f * y * (B2_PI - y));
            }
            else
            {
                s = 16.0f * x * (B2_PI - x) / (5.0f * pi2 - 4.0f * x * (B2_PI - x));
            }

            float mag = MathF.Sqrt(s * s + c * c);
            float invMag = mag > 0.0 ? 1.0f / mag : 0.0f;
            B2CosSin cs = new B2CosSin { cosine = c * invMag, sine = s * invMag };
            return cs;
        }


        /// Compute the rotation between two unit vectors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Rot b2ComputeRotationBetweenUnitVectors(B2Vec2 v1, B2Vec2 v2)
        {
            B2_ASSERT(b2AbsFloat(1.0f - b2Length(v1)) < 100.0f * FLT_EPSILON);
            B2_ASSERT(b2AbsFloat(1.0f - b2Length(v2)) < 100.0f * FLT_EPSILON);

            B2Rot rot;
            rot.c = b2Dot(v1, v2);
            rot.s = b2Cross(v1, v2);
            return b2NormalizeRot(rot);
        }
    }
}