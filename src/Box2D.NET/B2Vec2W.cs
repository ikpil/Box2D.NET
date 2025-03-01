// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Wide vec2
    public struct B2Vec2W
    {
        public B2FloatW X;
        public B2FloatW Y;

        public B2Vec2W(B2FloatW X, B2FloatW Y)
        {
            this.X = X;
            this.Y = Y;
        }
    }
}
