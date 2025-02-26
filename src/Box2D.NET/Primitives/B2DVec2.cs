// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
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

        /*
         * @defgroup math_cpp C++ Math
         * @brief Math operator overloads for C++
         *
         * See math_functions.h for details.
         */

        // /// Unary add one vector to another
        // public static void operator+=( b2Vec2& a, b2Vec2 b )
        // {
        //     a.x += b.x;
        //     a.y += b.y;
        // }
        //
        // /// Unary subtract one vector from another
        // public static void operator-=( b2Vec2& a, b2Vec2 b )
        // {
        //     a.x -= b.x;
        //     a.y -= b.y;
        // }
        //
        // /// Unary multiply a vector by a scalar
        // public static void operator*=( b2Vec2& a, float b )
        // {
        //     a.x *= b;
        //     a.y *= b;
        // }
        
        /// Unary negate a vector
        public static b2Vec2 operator-( b2Vec2 a )
        {
            return new b2Vec2(-a.x, -a.y);
        }
        
        /// Binary vector addition
        public static b2Vec2 operator+( b2Vec2 a, b2Vec2 b )
        {
            return new b2Vec2(a.x + b.x, a.y + b.y);
        }
        
        /// Binary vector subtraction
        public static b2Vec2 operator-( b2Vec2 a, b2Vec2 b )
        {
            return new b2Vec2(a.x - b.x, a.y - b.y);
        }
        
        /// Binary scalar and vector multiplication
        public static b2Vec2 operator*( float a, b2Vec2 b )
        {
            return new b2Vec2(a * b.x, a * b.y);
        }
        
        /// Binary scalar and vector multiplication
        public static b2Vec2 operator*( b2Vec2 a, float b )
        {
            return new b2Vec2(a.x * b, a.y * b);
        }
        
        /// Binary vector equality
        public static bool operator==( b2Vec2 a, b2Vec2 b )
        {
            return a.x == b.x && a.y == b.y;
        }
        
        /// Binary vector inequality
        public static bool operator!=( b2Vec2 a, b2Vec2 b )
        {
            return a.x != b.x || a.y != b.y;
        }
    }
}
