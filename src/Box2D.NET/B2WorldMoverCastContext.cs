// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2WorldMoverCastContext
    {
        public B2World world;
        public B2QueryFilter filter;
        public float fraction;
        public B2Pos origin;

        // origin relative input
        public B2ShapeCastInput input;
    }
}
