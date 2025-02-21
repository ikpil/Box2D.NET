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
}