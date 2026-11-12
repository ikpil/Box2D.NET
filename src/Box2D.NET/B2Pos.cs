// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Box2D.NET
{
    /// A world position. Upstream Box2D stores this in double precision in large world mode
    /// (BOX2D_DOUBLE_PRECISION). Box2D.NET only supports single precision, so this holds floats,
    /// but it is kept as a distinct type so the API matches upstream.
    [StructLayout(LayoutKind.Sequential)]
    public struct B2Pos : IEquatable<B2Pos>
    {
        /// coordinates
        public float X, Y;

        public B2Pos(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// Offset a world position by a vector
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Pos operator +(B2Pos a, B2Vec2 b)
        {
            return new B2Pos(a.X + b.X, a.Y + b.Y);
        }

        /// Offset a world position by a vector
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Pos operator -(B2Pos a, B2Vec2 b)
        {
            return new B2Pos(a.X - b.X, a.Y - b.Y);
        }

        /// Delta between two world positions
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 operator -(B2Pos a, B2Pos b)
        {
            return new B2Vec2(a.X - b.X, a.Y - b.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(B2Pos a, B2Pos b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(B2Pos a, B2Pos b)
        {
            return !(a == b);
        }

        // Bitwise compare, see B2Vec2.Equals
        public bool Equals(B2Pos other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            return obj is B2Pos other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (X, Y).GetHashCode();
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
