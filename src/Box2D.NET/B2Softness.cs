// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2Softness
    {
        public float biasRate;
        public float massScale;
        public float impulseScale;

        public B2Softness(float biasRate, float massScale, float impulseScale)
        {
            this.biasRate = biasRate;
            this.massScale = massScale;
            this.impulseScale = impulseScale;
        }
    }
}
