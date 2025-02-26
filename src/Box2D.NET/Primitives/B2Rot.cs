// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    /// 2D rotation
    /// This is similar to using a complex number for rotation
    public struct B2Rot
    {
        /// cosine and sine
        public float c, s;

        public B2Rot(float c, float s)
        {
            this.c = c;
            this.s = s;
        }
    }
}
