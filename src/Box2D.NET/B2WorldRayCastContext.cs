// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2WorldRayCastContext
    {
        public B2World world;
        public b2CastResultFcn fcn;
        public B2QueryFilter filter;
        public float fraction;
        public object userContext;

        public B2WorldRayCastContext(B2World world, b2CastResultFcn fcn, B2QueryFilter filter, float fraction, object userContext)
        {
            this.world = world;
            this.fcn = fcn;
            this.filter = filter;
            this.fraction = fraction;
            this.userContext = userContext;
        }
    }
}