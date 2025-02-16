// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;

namespace Box2D.NET;

/**
 * @defgroup math Math
 * @brief Vector math types and functions
 * @{
 */

/// 2D vector
/// This can be used to represent a point or free vector
public struct b2Vec2
{
    /// coordinates
    public float x, y;

    public b2Vec2(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
    
    /**
 * @defgroup math_cpp C++ Math
 * @brief Math operator overloads for C++
 *
 * See math_functions.h for details.
 * @{
 */

    // /// Unary add one vector to another
    // void operator+=( b2Vec2& a, b2Vec2 b )
    // {
    //     a.x += b.x;
    //     a.y += b.y;
    // }
    //
    // /// Unary subtract one vector from another
    // void operator-=( b2Vec2& a, b2Vec2 b )
    // {
    //     a.x -= b.x;
    //     a.y -= b.y;
    // }
    //
    // /// Unary multiply a vector by a scalar
    // void operator*=( b2Vec2& a, float b )
    // {
    //     a.x *= b;
    //     a.y *= b;
    // }
    //
    // /// Unary negate a vector
    // b2Vec2 operator-( b2Vec2 a )
    // {
    //     return { -a.x, -a.y };
    // }
    //
    // /// Binary vector addition
    // b2Vec2 operator+( b2Vec2 a, b2Vec2 b )
    // {
    //     return { a.x + b.x, a.y + b.y };
    // }
    //
    // /// Binary vector subtraction
    // b2Vec2 operator-( b2Vec2 a, b2Vec2 b )
    // {
    //     return { a.x - b.x, a.y - b.y };
    // }
    //
    // /// Binary scalar and vector multiplication
    // b2Vec2 operator*( float a, b2Vec2 b )
    // {
    //     return { a * b.x, a * b.y };
    // }
    //
    // /// Binary scalar and vector multiplication
    // b2Vec2 operator*( b2Vec2 a, float b )
    // {
    //     return { a.x * b, a.y * b };
    // }
    //
    // /// Binary vector equality
    // bool operator==( b2Vec2 a, b2Vec2 b )
    // {
    //     return a.x == b.x && a.y == b.y;
    // }
    //
    // /// Binary vector inequality
    // bool operator!=( b2Vec2 a, b2Vec2 b )
    // {
    //     return a.x != b.x || a.y != b.y;
    // }

    /**@}*/

}

/// Cosine and sine pair
/// This uses a custom implementation designed for cross-platform determinism
public struct b2CosSin
{
    /// cosine and sine
    public float cosine;
    public float sine;
}

/// 2D rotation
/// This is similar to using a complex number for rotation
public struct b2Rot
{
    /// cosine and sine
    public float c, s;

    public b2Rot(float c, float s)
    {
        this.c = c;
        this.s = s;
    }
}

/// A 2D rigid transform
public struct b2Transform
{
    public b2Vec2 p;
    public b2Rot q;

    public b2Transform(b2Vec2 p, b2Rot q)
    {
        this.p = p;
        this.q = q;
    }
}

/// A 2-by-2 Matrix
public struct b2Mat22
{
    /// columns
    public b2Vec2 cx, cy;

    public b2Mat22(b2Vec2 cx, b2Vec2 cy)
    {
        this.cx = cx;
        this.cy = cy;
    }
} 

/// Axis-aligned bounding box
public struct b2AABB
{
    public b2Vec2 lowerBound;
    public b2Vec2 upperBound;

    public b2AABB(b2Vec2 lowerBound, b2Vec2 upperBound)
    {
        this.lowerBound = lowerBound;
        this.upperBound = upperBound;
    }
}

public static class math_function
{

    /**@}*/

    /**
     * @addtogroup math
     * @{
     */

    /// https://en.wikipedia.org/wiki/Pi
    public const float B2_PI = 3.14159265359f;
    public const float FLT_EPSILON = 1.1920929e-7f;
    
    public static readonly b2Vec2 b2Vec2_zero = new b2Vec2 { x = 0.0f, y = 0.0f };
    public static readonly b2Rot b2Rot_identity = new b2Rot{ c = 1.0f, s = 0.0f };
    public static readonly b2Transform b2Transform_identity = new b2Transform{ p = { x = 0.0f, y = 0.0f }, q = { c = 1.0f, s = 0.0f } };
    public static readonly b2Mat22 b2Mat22_zero = new b2Mat22{ cx = { x = 0.0f, y = 0.0f }, cy = { x = 0.0f, y = 0.0f } };

    /// @return the minimum of two integers
    public static int b2MinInt( int a, int b )
    {
        return a < b ? a : b;
    }

    /// @return the maximum of two integers
    public static int b2MaxInt( int a, int b )
    {
        return a > b ? a : b;
    }

    /// @return the absolute value of an integer
    public static int b2AbsInt( int a )
    {
        return a < 0 ? -a : a;
    }

    /// @return an integer clamped between a lower and upper bound
    public static int b2ClampInt( int a, int lower, int upper )
    {
        return a < lower ? lower : ( a > upper ? upper : a );
    }

    /// @return the minimum of two floats
    public static float b2MinFloat( float a, float b )
    {
        return a < b ? a : b;
    }

    /// @return the maximum of two floats
    public static float b2MaxFloat( float a, float b )
    {
        return a > b ? a : b;
    }

    /// @return the absolute value of a float
    public static float b2AbsFloat( float a )
    {
        return a < 0 ? -a : a;
    }

    /// @return a float clamped between a lower and upper bound
    public static float b2ClampFloat( float a, float lower, float upper )
    {
        return a < lower ? lower : ( a > upper ? upper : a );
    }



    /// Vector dot product
    public static float b2Dot( b2Vec2 a, b2Vec2 b )
    {
        return a.x * b.x + a.y * b.y;
    }

    /// Vector cross product. In 2D this yields a scalar.
    public static float b2Cross( b2Vec2 a, b2Vec2 b )
    {
        return a.x * b.y - a.y * b.x;
    }

    /// Perform the cross product on a vector and a scalar. In 2D this produces a vector.
    public static b2Vec2 b2CrossVS( b2Vec2 v, float s )
    {
        return new b2Vec2{ x = s * v.y, y = -s * v.x };
    }

    /// Perform the cross product on a scalar and a vector. In 2D this produces a vector.
    public static b2Vec2 b2CrossSV( float s, b2Vec2 v )
    {
        return new b2Vec2{ x = -s * v.y, y = s * v.x };
    }

    /// Get a left pointing perpendicular vector. Equivalent to b2CrossSV(1.0f, v)
    public static b2Vec2 b2LeftPerp( b2Vec2 v )
    {
        return new b2Vec2{ x = -v.y, y = v.x };
    }

    /// Get a right pointing perpendicular vector. Equivalent to b2CrossVS(v, 1.0f)
    public static b2Vec2 b2RightPerp( b2Vec2 v )
    {
        return new b2Vec2{ x = v.y, y = -v.x };
    }

    /// Vector addition
    public static b2Vec2 b2Add( b2Vec2 a, b2Vec2 b )
    {
        return new b2Vec2{ x = a.x + b.x, y = a.y + b.y };
    }

    /// Vector subtraction
    public static b2Vec2 b2Sub( b2Vec2 a, b2Vec2 b )
    {
        return new b2Vec2{ x = a.x - b.x, y = a.y - b.y };
    }

    /// Vector negation
    public static b2Vec2 b2Neg( b2Vec2 a )
    {
        return new b2Vec2{ x = -a.x, y = -a.y };
    }

    /// Vector linear interpolation
    /// https://fgiesen.wordpress.com/2012/08/15/linear-interpolation-past-present-and-future/
    public static b2Vec2 b2Lerp( b2Vec2 a, b2Vec2 b, float t )
    {
        return new b2Vec2{ x = ( 1.0f - t ) * a.x + t * b.x, y = ( 1.0f - t ) * a.y + t * b.y };
    }

    /// Component-wise multiplication
    public static b2Vec2 b2Mul( b2Vec2 a, b2Vec2 b )
    {
        return new b2Vec2{ x = a.x * b.x, y = a.y * b.y };
    }

    /// Multiply a scalar and vector
    public static b2Vec2 b2MulSV( float s, b2Vec2 v )
    {
        return new b2Vec2{ x = s * v.x, y = s * v.y };
    }

    /// a + s * b
    public static b2Vec2 b2MulAdd( b2Vec2 a, float s, b2Vec2 b )
    {
        return new b2Vec2{ x = a.x + s * b.x, y = a.y + s * b.y };
    }

    /// a - s * b
    public static b2Vec2 b2MulSub( b2Vec2 a, float s, b2Vec2 b )
    {
        return new b2Vec2{ x = a.x - s * b.x, y = a.y - s * b.y };
    }

    /// Component-wise absolute vector
    public static b2Vec2 b2Abs( b2Vec2 a )
    {
        b2Vec2 b;
        b.x = b2AbsFloat( a.x );
        b.y = b2AbsFloat( a.y );
        return b;
    }

    /// Component-wise minimum vector
    public static b2Vec2 b2Min( b2Vec2 a, b2Vec2 b )
    {
        b2Vec2 c;
        c.x = b2MinFloat( a.x, b.x );
        c.y = b2MinFloat( a.y, b.y );
        return c;
    }

    /// Component-wise maximum vector
    public static b2Vec2 b2Max( b2Vec2 a, b2Vec2 b )
    {
        b2Vec2 c;
        c.x = b2MaxFloat( a.x, b.x );
        c.y = b2MaxFloat( a.y, b.y );
        return c;
    }

    /// Component-wise clamp vector v into the range [a, b]
    public static b2Vec2 b2Clamp( b2Vec2 v, b2Vec2 a, b2Vec2 b )
    {
        b2Vec2 c;
        c.x = b2ClampFloat( v.x, a.x, b.x );
        c.y = b2ClampFloat( v.y, a.y, b.y );
        return c;
    }

    /// Get the length of this vector (the norm)
    public static float b2Length( b2Vec2 v )
    {
        return MathF.Sqrt( v.x * v.x + v.y * v.y );
    }

    /// Get the distance between two points
    public static float b2Distance( b2Vec2 a, b2Vec2 b )
    {
        float dx = b.x - a.x;
        float dy = b.y - a.y;
        return MathF.Sqrt( dx * dx + dy * dy );
    }

    /// Convert a vector into a unit vector if possible, otherwise returns the zero vector.
    public static b2Vec2 b2Normalize( b2Vec2 v )
    {
        float length = MathF.Sqrt( v.x * v.x + v.y * v.y );
        if ( length < FLT_EPSILON )
        {
            return b2Vec2_zero;
        }

        float invLength = 1.0f / length;
        b2Vec2 n = new b2Vec2{ x = invLength * v.x, y = invLength * v.y };
        return n;
    }

    /// Convert a vector into a unit vector if possible, otherwise returns the zero vector. Also
    /// outputs the length.
    public static b2Vec2 b2GetLengthAndNormalize( ref float length, b2Vec2 v )
    {
        length = b2Length( v );
        if ( length < FLT_EPSILON )
        {
            return b2Vec2_zero;
        }

        float invLength = 1.0f / length;
        b2Vec2 n = new b2Vec2{ x = invLength * v.x, y = invLength * v.y };
        return n;
    }

    /// Normalize rotation
    public static b2Rot b2NormalizeRot( b2Rot q )
    {
        float mag = MathF.Sqrt( q.s * q.s + q.c * q.c );
        float invMag = mag > 0.0 ? 1.0f / mag : 0.0f;
        b2Rot qn = new b2Rot{ c = q.c * invMag, s = q.s * invMag };
        return qn;
    }

    /// Integrate rotation from angular velocity
    /// @param q1 initial rotation
    /// @param deltaAngle the angular displacement in radians
    public static b2Rot b2IntegrateRotation( b2Rot q1, float deltaAngle )
    {
        // dc/dt = -omega * sin(t)
        // ds/dt = omega * cos(t)
        // c2 = c1 - omega * h * s1
        // s2 = s1 + omega * h * c1
        b2Rot q2 = new b2Rot{ c = q1.c - deltaAngle * q1.s, s = q1.s + deltaAngle * q1.c };
        float mag = MathF.Sqrt( q2.s * q2.s + q2.c * q2.c );
        float invMag = mag > 0.0 ? 1.0f / mag : 0.0f;
        b2Rot qn = new b2Rot{ c = q2.c * invMag, s = q2.s * invMag };
        return qn;
    }

    /// Get the length squared of this vector
    public static float b2LengthSquared( b2Vec2 v )
    {
        return v.x * v.x + v.y * v.y;
    }

    /// Get the distance squared between points
    public static float b2DistanceSquared( b2Vec2 a, b2Vec2 b )
    {
        b2Vec2 c = new b2Vec2{ x = b.x - a.x, y = b.y - a.y };
        return c.x * c.x + c.y * c.y;
    }

    /// Make a rotation using an angle in radians
    public static b2Rot b2MakeRot( float radians )
    {
        b2CosSin cs = b2ComputeCosSin( radians );
        return new b2Rot{ c = cs.cosine, s = cs.sine };
    }


    /// Is this rotation normalized?
    public static bool b2IsNormalized( b2Rot q )
    {
        // larger tolerance due to failure on mingw 32-bit
        float qq = q.s * q.s + q.c * q.c;
        return 1.0f - 0.0006f < qq && qq < 1.0f + 0.0006f;
    }

    /// Normalized linear interpolation
    /// https://fgiesen.wordpress.com/2012/08/15/linear-interpolation-past-present-and-future/
    ///	https://web.archive.org/web/20170825184056/http://number-none.com/product/Understanding%20Slerp,%20Then%20Not%20Using%20It/
    public static b2Rot b2NLerp( b2Rot q1, b2Rot q2, float t )
    {
        float omt = 1.0f - t;
        b2Rot q = new b2Rot{
            c = omt * q1.c + t * q2.c,
            s = omt * q1.s + t * q2.s,
        };

        return b2NormalizeRot( q );
    }

    /// Compute the angular velocity necessary to rotate between two rotations over a give time
    /// @param q1 initial rotation
    /// @param q2 final rotation
    /// @param inv_h inverse time step
    public static float b2ComputeAngularVelocity( b2Rot q1, b2Rot q2, float inv_h )
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
        float omega = inv_h * ( q2.s * q1.c - q2.c * q1.s );
        return omega;
    }

    /// Get the angle in radians in the range [-pi, pi]
    public static float b2Rot_GetAngle( b2Rot q )
    {
        return b2Atan2( q.s, q.c );
    }

    /// Get the x-axis
    public static b2Vec2 b2Rot_GetXAxis( b2Rot q )
    {
        b2Vec2 v = new b2Vec2{ x = q.c, y = q.s };
        return v;
    }

    /// Get the y-axis
    public static b2Vec2 b2Rot_GetYAxis( b2Rot q )
    {
        b2Vec2 v = new b2Vec2{ x = -q.s, y = q.c };
        return v;
    }

    /// Multiply two rotations: q * r
    public static b2Rot b2MulRot( b2Rot q, b2Rot r )
    {
        // [qc -qs] * [rc -rs] = [qc*rc-qs*rs -qc*rs-qs*rc]
        // [qs  qc]   [rs  rc]   [qs*rc+qc*rs -qs*rs+qc*rc]
        // s(q + r) = qs * rc + qc * rs
        // c(q + r) = qc * rc - qs * rs
        b2Rot qr;
        qr.s = q.s * r.c + q.c * r.s;
        qr.c = q.c * r.c - q.s * r.s;
        return qr;
    }

    /// Transpose multiply two rotations: qT * r
    public static b2Rot b2InvMulRot( b2Rot q, b2Rot r )
    {
        // [ qc qs] * [rc -rs] = [qc*rc+qs*rs -qc*rs+qs*rc]
        // [-qs qc]   [rs  rc]   [-qs*rc+qc*rs qs*rs+qc*rc]
        // s(q - r) = qc * rs - qs * rc
        // c(q - r) = qc * rc + qs * rs
        b2Rot qr;
        qr.s = q.c * r.s - q.s * r.c;
        qr.c = q.c * r.c + q.s * r.s;
        return qr;
    }

    /// relative angle between b and a (rot_b * inv(rot_a))
    public static float b2RelativeAngle( b2Rot b, b2Rot a )
    {
        // sin(b - a) = bs * ac - bc * as
        // cos(b - a) = bc * ac + bs * as
        float s = b.s * a.c - b.c * a.s;
        float c = b.c * a.c + b.s * a.s;
        return b2Atan2( s, c );
    }

    /// Convert an angle in the range [-2*pi, 2*pi] into the range [-pi, pi]
    public static float b2UnwindAngle( float radians )
    {
        if ( radians < -B2_PI )
        {
            return radians + 2.0f * B2_PI;
        }
        else if ( radians > B2_PI )
        {
            return radians - 2.0f * B2_PI;
        }

        return radians;
    }

    /// Convert any into the range [-pi, pi] (slow)
    public static float b2UnwindLargeAngle( float radians )
    {
        while ( radians > B2_PI )
        {
            radians -= 2.0f * B2_PI;
        }

        while ( radians < -B2_PI )
        {
            radians += 2.0f * B2_PI;
        }

        return radians;
    }

    /// Rotate a vector
    public static b2Vec2 b2RotateVector( b2Rot q, b2Vec2 v )
    {
        return new b2Vec2{ x = q.c * v.x - q.s * v.y, y = q.s * v.x + q.c * v.y };
    }

    /// Inverse rotate a vector
    public static b2Vec2 b2InvRotateVector( b2Rot q, b2Vec2 v )
    {
        return new b2Vec2{ x = q.c * v.x + q.s * v.y, y = -q.s * v.x + q.c * v.y };
    }

    /// Transform a point (e.g. local space to world space)
    public static b2Vec2 b2TransformPoint( b2Transform t, b2Vec2 p )
    {
        float x = ( t.q.c * p.x - t.q.s * p.y ) + t.p.x;
        float y = ( t.q.s * p.x + t.q.c * p.y ) + t.p.y;

        return new b2Vec2{ x = x, y = y };
    }

    /// Inverse transform a point (e.g. world space to local space)
    public static b2Vec2 b2InvTransformPoint( b2Transform t, b2Vec2 p )
    {
        float vx = p.x - t.p.x;
        float vy = p.y - t.p.y;
        return new b2Vec2{ x = t.q.c * vx + t.q.s * vy, y = -t.q.s * vx + t.q.c * vy };
    }

    /// Multiply two transforms. If the result is applied to a point p local to frame B,
    /// the transform would first convert p to a point local to frame A, then into a point
    /// in the world frame.
    /// v2 = A.q.Rot(B.q.Rot(v1) + B.p) + A.p
    ///    = (A.q * B.q).Rot(v1) + A.q.Rot(B.p) + A.p
    public static b2Transform b2MulTransforms( b2Transform A, b2Transform B )
    {
        b2Transform C;
        C.q = b2MulRot( A.q, B.q );
        C.p = b2Add( b2RotateVector( A.q, B.p ), A.p );
        return C;
    }

    /// Creates a transform that converts a local point in frame B to a local point in frame A.
    /// v2 = A.q' * (B.q * v1 + B.p - A.p)
    ///    = A.q' * B.q * v1 + A.q' * (B.p - A.p)
    public static b2Transform b2InvMulTransforms( b2Transform A, b2Transform B )
    {
        b2Transform C;
        C.q = b2InvMulRot( A.q, B.q );
        C.p = b2InvRotateVector( A.q, b2Sub( B.p, A.p ) );
        return C;
    }

    /// Multiply a 2-by-2 matrix times a 2D vector
    public static b2Vec2 b2MulMV( b2Mat22 A, b2Vec2 v )
    {
        b2Vec2 u = new b2Vec2{
            x = A.cx.x * v.x + A.cy.x * v.y,
            y = A.cx.y * v.x + A.cy.y * v.y,
        };
        return u;
    }

    /// Get the inverse of a 2-by-2 matrix
    public static b2Mat22 b2GetInverse22( b2Mat22 A )
    {
        float a = A.cx.x, b = A.cy.x, c = A.cx.y, d = A.cy.y;
        float det = a * d - b * c;
        if ( det != 0.0f )
        {
            det = 1.0f / det;
        }

        b2Mat22 B = new b2Mat22{
            cx = { x = det * d, y = -det * c },
            cy = { x = -det * b, y = det * a },
        };
        return B;
    }

    /// Solve A * x = b, where b is a column vector. This is more efficient
    /// than computing the inverse in one-shot cases.
    public static b2Vec2 b2Solve22( b2Mat22 A, b2Vec2 b )
    {
        float a11 = A.cx.x, a12 = A.cy.x, a21 = A.cx.y, a22 = A.cy.y;
        float det = a11 * a22 - a12 * a21;
        if ( det != 0.0f )
        {
            det = 1.0f / det;
        }
        b2Vec2 x = new b2Vec2{ x = det * ( a22 * b.x - a12 * b.y ), y = det * ( a11 * b.y - a21 * b.x ) };
        return x;
    }

    /// Does a fully contain b
    public static bool b2AABB_Contains( b2AABB a, b2AABB b )
    {
        bool s = true;
        s = s && a.lowerBound.x <= b.lowerBound.x;
        s = s && a.lowerBound.y <= b.lowerBound.y;
        s = s && b.upperBound.x <= a.upperBound.x;
        s = s && b.upperBound.y <= a.upperBound.y;
        return s;
    }

    /// Get the center of the AABB.
    public static b2Vec2 b2AABB_Center( b2AABB a )
    {
        b2Vec2 b = new b2Vec2{ x = 0.5f * ( a.lowerBound.x + a.upperBound.x ), y = 0.5f * ( a.lowerBound.y + a.upperBound.y ) };
        return b;
    }

    /// Get the extents of the AABB (half-widths).
    public static b2Vec2 b2AABB_Extents( b2AABB a )
    {
        b2Vec2 b = new b2Vec2{ x = 0.5f * ( a.upperBound.x - a.lowerBound.x ), y = 0.5f * ( a.upperBound.y - a.lowerBound.y ) };
        return b;
    }

    /// Union of two AABBs
    public static b2AABB b2AABB_Union( b2AABB a, b2AABB b )
    {
        b2AABB c;
        c.lowerBound.x = b2MinFloat( a.lowerBound.x, b.lowerBound.x );
        c.lowerBound.y = b2MinFloat( a.lowerBound.y, b.lowerBound.y );
        c.upperBound.x = b2MaxFloat( a.upperBound.x, b.upperBound.x );
        c.upperBound.y = b2MaxFloat( a.upperBound.y, b.upperBound.y );
        return c;
    }


    /**@}*/




    //Debug.Assert( sizeof( int32_t ) == sizeof( int ), "Box2D expects int32_t and int to be the same" );

    /// Is this a valid number? Not NaN or infinity.
    public static bool b2IsValidFloat( float a )
    {
        if ( float.IsNaN( a ) )
        {
            return false;
        }

        if ( float.IsInfinity( a ) )
        {
            return false;
        }

        return true;
    }

    /// Is this a valid vector? Not NaN or infinity.
    public static bool b2IsValidVec2( b2Vec2 v )
    {
        if ( float.IsNaN( v.x ) || float.IsNaN( v.y ) )
        {
            return false;
        }

        if ( float.IsInfinity( v.x ) || float.IsInfinity( v.y ) )
        {
            return false;
        }

        return true;
    }

    /// Is this a valid rotation? Not NaN or infinity. Is normalized.
    public static bool b2IsValidRotation( b2Rot q )
    {
        if ( float.IsNaN( q.s ) || float.IsNaN( q.c ) )
        {
            return false;
        }

        if ( float.IsInfinity( q.s ) || float.IsInfinity( q.c ) )
        {
            return false;
        }

        return b2IsNormalized( q );
    }

    /// Compute an approximate arctangent in the range [-pi, pi]
    /// This is hand coded for cross-platform determinism. The MathF.Atan2
    /// function in the standard library is not cross-platform deterministic.
    ///	Accurate to around 0.0023 degrees
    // https://stackoverflow.com/questions/46210708/atan2-approximation-with-11bits-in-mantissa-on-x86with-sse2-and-armwith-vfpv4
    public static float b2Atan2( float y, float x )
    {
        // Added check for (0,0) to match MathF.Atan2 and avoid NaN
        if (x == 0.0f && y == 0.0f)
        {
            return 0.0f;
        }

        float ax = b2AbsFloat( x );
        float ay = b2AbsFloat( y );
        float mx = b2MaxFloat( ay, ax );
        float mn = b2MinFloat( ay, ax );
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
        if ( ay > ax )
        {
            r = 1.57079637f - r;
        }

        if ( x < 0 )
        {
            r = 3.14159274f - r;
        }

        if ( y < 0 )
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
    public static b2CosSin b2ComputeCosSin( float radians )
    {
        float x = b2UnwindLargeAngle( radians );
        float pi2 = B2_PI * B2_PI;

        // cosine needs angle in [-pi/2, pi/2]
        float c;
        if ( x < -0.5f * B2_PI )
        {
            float y = x + B2_PI;
            float y2 = y * y;
            c = -( pi2 - 4.0f * y2 ) / ( pi2 + y2 );
        }
        else if ( x > 0.5f * B2_PI )
        {
            float y = x - B2_PI;
            float y2 = y * y;
            c = -( pi2 - 4.0f * y2 ) / ( pi2 + y2 );
        }
        else
        {
            float y2 = x * x;
            c = ( pi2 - 4.0f * y2 ) / ( pi2 + y2 );
        }

        // sine needs angle in [0, pi]
        float s;
        if ( x < 0.0f )
        {
            float y = x + B2_PI;
            s = -16.0f * y * ( B2_PI - y ) / ( 5.0f * pi2 - 4.0f * y * ( B2_PI - y ) );
        }
        else
        {
            s = 16.0f * x * ( B2_PI - x ) / ( 5.0f * pi2 - 4.0f * x * ( B2_PI - x ) );
        }

        float mag = MathF.Sqrt( s * s + c * c );
        float invMag = mag > 0.0 ? 1.0f / mag : 0.0f;
        b2CosSin cs = new b2CosSin{ cosine = c * invMag, sine = s * invMag };
        return cs;
    }


    /// Compute the rotation between two unit vectors
    public static b2Rot b2ComputeRotationBetweenUnitVectors(b2Vec2 v1, b2Vec2 v2)
    {
        Debug.Assert( b2AbsFloat( 1.0f - b2Length( v1 ) ) < 100.0f * FLT_EPSILON );
        Debug.Assert( b2AbsFloat( 1.0f - b2Length( v2 ) ) < 100.0f * FLT_EPSILON );

        b2Rot rot;
        rot.c = b2Dot( v1, v2 );
        rot.s = b2Cross( v1, v2 );
        return b2NormalizeRot( rot );
    }

}