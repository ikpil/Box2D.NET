// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2WorldOverlapContext
    {
        public B2World world;
        public b2OverlapResultCallbackFcn callbackFcn;
        public B2QueryFilter filter;
        public B2ShapeProxy proxy;
        public B2Pos origin;
        public object userContext;

        public B2WorldOverlapContext(B2World world, b2OverlapResultCallbackFcn callbackFcn, in B2QueryFilter filter, in B2ShapeProxy proxy, in B2Pos origin, object userContext)
        {
            this.world = world;
            this.callbackFcn = callbackFcn;
            this.filter = filter;
            this.proxy = proxy;
            this.origin = origin;
            this.userContext = userContext;
        }
    }
}
