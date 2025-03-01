// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public class B2WorldOverlapContext
    {
        public B2World world;
        public b2OverlapResultFcn fcn;
        public B2QueryFilter filter;
        public B2ShapeProxy proxy;
        public B2Transform transform;
        public object userContext;

        public B2WorldOverlapContext(B2World world, b2OverlapResultFcn fcn, B2QueryFilter filter, B2ShapeProxy proxy, B2Transform transform, object userContext)
        {
            this.world = world;
            this.fcn = fcn;
            this.filter = filter;
            this.proxy = proxy;
            this.transform = transform;
            this.userContext = userContext;
        }
    }
}
