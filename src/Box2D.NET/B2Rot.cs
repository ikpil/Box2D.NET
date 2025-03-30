// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;

namespace Box2D.NET
{
    /// 2D rotation
    /// This is similar to using a complex number for rotation
    [StructLayout(LayoutKind.Sequential)]
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
