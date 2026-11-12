// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    // Wide vec2
    public struct B2Vec2W
    {
        public Vector4 X;
        public Vector4 Y;

        public B2Vec2W(in Vector4 X, in Vector4 Y)
        {
            this.X = X;
            this.Y = Y;
        }
    }
}
