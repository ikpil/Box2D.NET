// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;

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
        public static int b2MinInt(int a, int b)
        {
            return a < b ? a : b;
        }

        /// @return the maximum of two integers
        public static int b2MaxInt(int a, int b)
        {
            return a > b ? a : b;
        }

        /// @return the absolute value of an integer
        public static int b2AbsInt(int a)
        {
            return a < 0 ? -a : a;
        }

        /// @return an integer clamped between a lower and upper bound
        public static int b2ClampInt(int a, int lower, int upper)
        {
            return a < lower ? lower : (a > upper ? upper : a);
        }

        /// @return the minimum of two floats
        public static float b2MinFloat(float a, float b)
        {
            return a < b ? a : b;
        }

        /// @return the maximum of two floats
        public static float b2MaxFloat(float a, float b)
        {
            return a > b ? a : b;
        }

        /// @return the absolute value of a float
        public static float b2AbsFloat(float a)
        {
            return a < 0 ? -a : a;
        }

        /// @return a float clamped between a lower and upper bound
        public static float b2ClampFloat(float a, float lower, float upper)
        {
            return a < lower ? lower : (a > upper ? upper : a);
        }


        /// Vector dot product
        public static float b2Dot(B2Vec2 a, B2Vec2 b)
        {
            return a.x * b.x + a.y * b.y;
        }

        /// Vector cross product. In 2D this yields a scalar.
        public static float b2Cross(B2Vec2 a, B2Vec2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        /// Perform the cross product on a vector and a scalar. In 2D this produces a vector.
        public static B2Vec2 b2CrossVS(B2Vec2 v, float s)
        {
            return new B2Vec2(s * v.y, -s * v.x);
        }

        /// Perform the cross product on a scalar and a vector. In 2D this produces a vector.
        public static B2Vec2 b2CrossSV(float s, B2Vec2 v)
        {
            return new B2Vec2(-s * v.y, s * v.x);
        }

        /// Get a left pointing perpendicular vector. Equivalent to b2CrossSV(1.0f, v)
        public static B2Vec2 b2LeftPerp(B2Vec2 v)
        {
            return new B2Vec2(-v.y, v.x);
        }

        /// Get a right pointing perpendicular vector. Equivalent to b2CrossVS(v, 1.0f)
        public static B2Vec2 b2RightPerp(B2Vec2 v)
        {
            return new B2Vec2(v.y, -v.x);
        }

        /// Vector addition
        public static B2Vec2 b2Add(B2Vec2 a, B2Vec2 b)
        {
            return new B2Vec2(a.x + b.x, a.y + b.y);
        }

        /// Vector subtraction
        public static B2Vec2 b2Sub(B2Vec2 a, B2Vec2 b)
        {
            return new B2Vec2(a.x - b.x, a.y - b.y);
        }

        /// Vector negation
        public static B2Vec2 b2Neg(B2Vec2 a)
        {
            return new B2Vec2(-a.x, -a.y);
        }

        /// Vector linear interpolation
        /// https://fgiesen.wordpress.com/2012/08/15/linear-interpolation-past-present-and-future/
        public static B2Vec2 b2Lerp(B2Vec2 a, B2Vec2 b, float t)
        {
            return new B2Vec2((1.0f - t) * a.x + t * b.x, (1.0f - t) * a.y + t * b.y);
        }

        /// Component-wise multiplication
        public static B2Vec2 b2Mul(B2Vec2 a, B2Vec2 b)
        {
            return new B2Vec2(a.x * b.x, a.y * b.y);
        }

        /// Multiply a scalar and vector
        public static B2Vec2 b2MulSV(float s, B2Vec2 v)
        {
            return new B2Vec2(s * v.x, s * v.y);
        }

        /// a + s * b
        public static B2Vec2 b2MulAdd(B2Vec2 a, float s, B2Vec2 b)
        {
            return new B2Vec2(a.x + s * b.x, a.y + s * b.y);
        }

        /// a - s * b
        public static B2Vec2 b2MulSub(B2Vec2 a, float s, B2Vec2 b)
        {
            return new B2Vec2(a.x - s * b.x, a.y - s * b.y);
        }

        /// Component-wise absolute vector
        public static B2Vec2 b2Abs(B2Vec2 a)
        {
            B2Vec2 b;
            b.x = b2AbsFloat(a.x);
            b.y = b2AbsFloat(a.y);
            return b;
        }

        /// Component-wise minimum vector
        public static B2Vec2 b2Min(B2Vec2 a, B2Vec2 b)
        {
            B2Vec2 c;
            c.x = b2MinFloat(a.x, b.x);
            c.y = b2MinFloat(a.y, b.y);
            return c;
        }

        /// Component-wise maximum vector
        public static B2Vec2 b2Max(B2Vec2 a, B2Vec2 b)
        {
            B2Vec2 c;
            c.x = b2MaxFloat(a.x, b.x);
            c.y = b2MaxFloat(a.y, b.y);
            return c;
        }

        /// Component-wise clamp vector v into the range [a, b]
        public static B2Vec2 b2Clamp(B2Vec2 v, B2Vec2 a, B2Vec2 b)
        {
            B2Vec2 c;
            c.x = b2ClampFloat(v.x, a.x, b.x);
            c.y = b2ClampFloat(v.y, a.y, b.y);
            return c;
        }

        /// Get the length of this vector (the norm)
        public static float b2Length(B2Vec2 v)
        {
            return MathF.Sqrt(v.x * v.x + v.y * v.y);
        }

        /// Get the distance between two points
        public static float b2Distance(B2Vec2 a, B2Vec2 b)
        {
            float dx = b.x - a.x;
            float dy = b.y - a.y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        /// Convert a vector into a unit vector if possible, otherwise returns the zero vector.
        public static B2Vec2 b2Normalize(B2Vec2 v)
        {
            float length = MathF.Sqrt(v.x * v.x + v.y * v.y);
            if (length < FLT_EPSILON)
            {
                return b2Vec2_zero;
            }

            float invLength = 1.0f / length;
            B2Vec2 n = new B2Vec2(invLength * v.x, invLength * v.y);
            return n;
        }

        /// Convert a vector into a unit vector if possible, otherwise returns the zero vector. Also
        /// outputs the length.
        public static B2Vec2 b2GetLengthAndNormalize(ref float length, B2Vec2 v)
        {
            length = b2Length(v);
            if (length < FLT_EPSILON)
            {
                return b2Vec2_zero;
            }

            float invLength = 1.0f / length;
            B2Vec2 n = new B2Vec2(invLength * v.x, invLength * v.y);
            return n;
        }

        /// Normalize rotation
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
        public static float b2LengthSquared(B2Vec2 v)
        {
            return v.x * v.x + v.y * v.y;
        }

        /// Get the distance squared between points
        public static float b2DistanceSquared(B2Vec2 a, B2Vec2 b)
        {
            B2Vec2 c = new B2Vec2(b.x - a.x, b.y - a.y);
            return c.x * c.x + c.y * c.y;
        }

        /// Make a rotation using an angle in radians
        public static B2Rot b2MakeRot(float radians)
        {
            B2CosSin cs = b2ComputeCosSin(radians);
            return new B2Rot(cs.cosine, cs.sine);
        }


        /// Is this rotation normalized?
        public static bool b2IsNormalized(B2Rot q)
        {
            // larger tolerance due to failure on mingw 32-bit
            float qq = q.s * q.s + q.c * q.c;
            return 1.0f - 0.0006f < qq && qq < 1.0f + 0.0006f;
        }

        /// Normalized linear interpolation
        /// https://fgiesen.wordpress.com/2012/08/15/linear-interpolation-past-present-and-future/
        ///	https://web.archive.org/web/20170825184056/http://number-none.com/product/Understanding%20Slerp,%20Then%20Not%20Using%20It/
        public static B2Rot b2NLerp(B2Rot q1, B2Rot q2, float t)
        {
            float omt = 1.0f - t;
            B2Rot q = new B2Rot
            {
                c = omt * q1.c + t * q2.c,
                s = omt * q1.s + t * q2.s,
            };

            return b2NormalizeRot(q);
        }

        /// Compute the angular velocity necessary to rotate between two rotations over a give time
        /// @param q1 initial rotation
        /// @param q2 final rotation
        /// @param inv_h inverse time step
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
        public static float b2Rot_GetAngle(B2Rot q)
        {
            return b2Atan2(q.s, q.c);
        }

        /// Get the x-axis
        public static B2Vec2 b2Rot_GetXAxis(B2Rot q)
        {
            B2Vec2 v = new B2Vec2(q.c, q.s);
            return v;
        }

        /// Get the y-axis
        public static B2Vec2 b2Rot_GetYAxis(B2Rot q)
        {
            B2Vec2 v = new B2Vec2(-q.s, q.c);
            return v;
        }

        /// Multiply two rotations: q * r
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

        /// Transpose multiply two rotations: qT * r
        public static B2Rot b2InvMulRot(B2Rot q, B2Rot r)
        {
            // [ qc qs] * [rc -rs] = [qc*rc+qs*rs -qc*rs+qs*rc]
            // [-qs qc]   [rs  rc]   [-qs*rc+qc*rs qs*rs+qc*rc]
            // s(q - r) = qc * rs - qs * rc
            // c(q - r) = qc * rc + qs * rs
            B2Rot qr;
            qr.s = q.c * r.s - q.s * r.c;
            qr.c = q.c * r.c + q.s * r.s;
            return qr;
        }

        /// relative angle between b and a (rot_b * inv(rot_a))
        public static float b2RelativeAngle(B2Rot b, B2Rot a)
        {
            // sin(b - a) = bs * ac - bc * as
            // cos(b - a) = bc * ac + bs * as
            float s = b.s * a.c - b.c * a.s;
            float c = b.c * a.c + b.s * a.s;
            return b2Atan2(s, c);
        }

        /// Convert an angle in the range [-2*pi, 2*pi] into the range [-pi, pi]
        public static float b2UnwindAngle(float radians)
        {
            if (radians < -B2_PI)
            {
                return radians + 2.0f * B2_PI;
            }
            else if (radians > B2_PI)
            {
                return radians - 2.0f * B2_PI;
            }

            return radians;
        }

        /// Convert any into the range [-pi, pi] (slow)
        public static float b2UnwindLargeAngle(float radians)
        {
            while (radians > B2_PI)
            {
                radians -= 2.0f * B2_PI;
            }

            while (radians < -B2_PI)
            {
                radians += 2.0f * B2_PI;
            }

            return radians;
        }

        /// Rotate a vector
        public static B2Vec2 b2RotateVector(B2Rot q, B2Vec2 v)
        {
            return new B2Vec2(q.c * v.x - q.s * v.y, q.s * v.x + q.c * v.y);
        }

        /// Inverse rotate a vector
        public static B2Vec2 b2InvRotateVector(B2Rot q, B2Vec2 v)
        {
            return new B2Vec2(q.c * v.x + q.s * v.y, -q.s * v.x + q.c * v.y);
        }

        /// Transform a point (e.g. local space to world space)
        public static B2Vec2 b2TransformPoint(ref B2Transform t, B2Vec2 p)
        {
            float x = (t.q.c * p.x - t.q.s * p.y) + t.p.x;
            float y = (t.q.s * p.x + t.q.c * p.y) + t.p.y;

            return new B2Vec2(x, y);
        }

        /// Inverse transform a point (e.g. world space to local space)
        public static B2Vec2 b2InvTransformPoint(B2Transform t, B2Vec2 p)
        {
            float vx = p.x - t.p.x;
            float vy = p.y - t.p.y;
            return new B2Vec2(t.q.c * vx + t.q.s * vy, -t.q.s * vx + t.q.c * vy);
        }

        /// Multiply two transforms. If the result is applied to a point p local to frame B,
        /// the transform would first convert p to a point local to frame A, then into a point
        /// in the world frame.
        /// v2 = A.q.Rot(B.q.Rot(v1) + B.p) + A.p
        ///    = (A.q * B.q).Rot(v1) + A.q.Rot(B.p) + A.p
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
        public static B2Transform b2InvMulTransforms(B2Transform A, B2Transform B)
        {
            B2Transform C;
            C.q = b2InvMulRot(A.q, B.q);
            C.p = b2InvRotateVector(A.q, b2Sub(B.p, A.p));
            return C;
        }

        /// Multiply a 2-by-2 matrix times a 2D vector
        public static B2Vec2 b2MulMV(B2Mat22 A, B2Vec2 v)
        {
            B2Vec2 u = new B2Vec2
            {
                x = A.cx.x * v.x + A.cy.x * v.y,
                y = A.cx.y * v.x + A.cy.y * v.y,
            };
            return u;
        }

        /// Get the inverse of a 2-by-2 matrix
        public static B2Mat22 b2GetInverse22(B2Mat22 A)
        {
            float a = A.cx.x, b = A.cy.x, c = A.cx.y, d = A.cy.y;
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
        public static B2Vec2 b2Solve22(B2Mat22 A, B2Vec2 b)
        {
            float a11 = A.cx.x, a12 = A.cy.x, a21 = A.cx.y, a22 = A.cy.y;
            float det = a11 * a22 - a12 * a21;
            if (det != 0.0f)
            {
                det = 1.0f / det;
            }

            B2Vec2 x = new B2Vec2(det * (a22 * b.x - a12 * b.y), det * (a11 * b.y - a21 * b.x));
            return x;
        }

        /// Does a fully contain b
        public static bool b2AABB_Contains(B2AABB a, B2AABB b)
        {
            bool s = true;
            s = s && a.lowerBound.x <= b.lowerBound.x;
            s = s && a.lowerBound.y <= b.lowerBound.y;
            s = s && b.upperBound.x <= a.upperBound.x;
            s = s && b.upperBound.y <= a.upperBound.y;
            return s;
        }

        /// Get the center of the AABB.
        public static B2Vec2 b2AABB_Center(B2AABB a)
        {
            B2Vec2 b = new B2Vec2(0.5f * (a.lowerBound.x + a.upperBound.x), 0.5f * (a.lowerBound.y + a.upperBound.y));
            return b;
        }

        /// Get the extents of the AABB (half-widths).
        public static B2Vec2 b2AABB_Extents(B2AABB a)
        {
            B2Vec2 b = new B2Vec2(0.5f * (a.upperBound.x - a.lowerBound.x), 0.5f * (a.upperBound.y - a.lowerBound.y));
            return b;
        }

        /// Union of two AABBs
        public static B2AABB b2AABB_Union(B2AABB a, B2AABB b)
        {
            B2AABB c;
            c.lowerBound.x = b2MinFloat(a.lowerBound.x, b.lowerBound.x);
            c.lowerBound.y = b2MinFloat(a.lowerBound.y, b.lowerBound.y);
            c.upperBound.x = b2MaxFloat(a.upperBound.x, b.upperBound.x);
            c.upperBound.y = b2MaxFloat(a.upperBound.y, b.upperBound.y);
            return c;
        }


        /**@}*/


        //Debug.Assert( sizeof( int ) == sizeof( int ), "Box2D expects int and int to be the same" );

        /// Is this a valid number? Not NaN or infinity.
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
        public static bool b2IsValidVec2(B2Vec2 v)
        {
            if (float.IsNaN(v.x) || float.IsNaN(v.y))
            {
                return false;
            }

            if (float.IsInfinity(v.x) || float.IsInfinity(v.y))
            {
                return false;
            }

            return true;
        }

        /// Is this a valid rotation? Not NaN or infinity. Is normalized.
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

            return b2IsNormalized(q);
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
            float x = b2UnwindLargeAngle(radians);
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
        public static B2Rot b2ComputeRotationBetweenUnitVectors(B2Vec2 v1, B2Vec2 v2)
        {
            Debug.Assert(b2AbsFloat(1.0f - b2Length(v1)) < 100.0f * FLT_EPSILON);
            Debug.Assert(b2AbsFloat(1.0f - b2Length(v2)) < 100.0f * FLT_EPSILON);

            B2Rot rot;
            rot.c = b2Dot(v1, v2);
            rot.s = b2Cross(v1, v2);
            return b2NormalizeRot(rot);
        }
    }
}
