// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Box2D.NET
{
    /// 2D vector
    /// This can be used to represent a point or free vector
    [StructLayout(LayoutKind.Sequential)]
    public struct B2Vec2
    {
        /// coordinates
        public float X, Y;

        public B2Vec2(float x, float y)
        {
            X = x;
            Y = y;
        }

        /*
         * @defgroup math_cpp C++ Math
         * @brief Math operator overloads for C++
         *
         * See math_functions.h for details.
         */

        /// Unary negate a vector
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 operator -(B2Vec2 a)
        {
            return new B2Vec2(-a.X, -a.Y);
        }

        /// Binary vector addition
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 operator +(B2Vec2 a, B2Vec2 b)
        {
            return new B2Vec2(a.X + b.X, a.Y + b.Y);
        }

        /// Binary vector subtraction
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 operator -(B2Vec2 a, B2Vec2 b)
        {
            return new B2Vec2(a.X - b.X, a.Y - b.Y);
        }

        /// Binary scalar and vector multiplication
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 operator *(float a, B2Vec2 b)
        {
            return new B2Vec2(a * b.X, a * b.Y);
        }

        /// Binary scalar and vector multiplication
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 operator *(B2Vec2 a, float b)
        {
            return new B2Vec2(a.X * b, a.Y * b);
        }

        /// Binary vector equality
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(B2Vec2 a, B2Vec2 b)
        {
            return a.X.Equals(b.X) && a.Y.Equals(b.Y);
        }

        /// Binary vector inequality
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(B2Vec2 a, B2Vec2 b)
        {
            return !(a == b);
        }
    }
}