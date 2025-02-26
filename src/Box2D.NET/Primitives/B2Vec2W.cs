// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    // Wide vec2
    public struct b2Vec2W
    {
        public b2FloatW X;
        public b2FloatW Y;

        public b2Vec2W(b2FloatW X, b2FloatW Y)
        {
            this.X = X;
            this.Y = Y;
        }
    }
}
