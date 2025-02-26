// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    // wide version of b2BodyState
    public struct b2BodyStateW
    {
        public b2Vec2W v;
        public b2FloatW w;
        public b2FloatW flags;
        public b2Vec2W dp;
        public b2RotW dq;
    }
}
