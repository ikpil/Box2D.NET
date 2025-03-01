// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // wide version of b2BodyState
    public struct B2BodyStateW
    {
        public B2Vec2W v;
        public B2FloatW w;
        public B2FloatW flags;
        public B2Vec2W dp;
        public B2RotW dq;
    }
}
