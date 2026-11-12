// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;

namespace Box2D.NET
{
    public struct B2WorldRayCastContext<T> where T : class
    {
        public B2World world;
        public b2CastResultCallbackFcn<T> callbackFcn;
        public B2QueryFilter filter;
        public float fraction;
        public B2Pos origin;
        public T userContext;

        internal B2WorldRayCastContext(B2World world, b2CastResultCallbackFcn<T> callbackFcn, in B2QueryFilter filter, float fraction, in B2Pos origin, T userContext)
        {
            this.world = world;
            this.callbackFcn = callbackFcn;
            this.filter = filter;
            this.fraction = fraction;
            this.origin = origin;
            this.userContext = userContext;
        }
    }

    public struct B2WorldRayCastContext
    {
        public static B2WorldRayCastContext<T> Create<T>(B2World world, b2CastResultCallbackFcn<T> callbackFcn, in B2QueryFilter filter, float fraction, in B2Pos origin, T userContext) where T : class
        {
            return new B2WorldRayCastContext<T>(world, callbackFcn, filter, fraction, origin, userContext);
        }
    }
}
