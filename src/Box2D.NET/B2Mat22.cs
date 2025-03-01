// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// A 2-by-2 Matrix
    public struct B2Mat22
    {
        /// columns
        public B2Vec2 cx, cy;

        public B2Mat22(B2Vec2 cx, B2Vec2 cy)
        {
            this.cx = cx;
            this.cy = cy;
        }
    }
}
