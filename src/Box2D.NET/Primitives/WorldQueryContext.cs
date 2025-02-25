// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    public class WorldQueryContext
    {
        public b2World world;
        public b2OverlapResultFcn fcn;
        public b2QueryFilter filter;
        public object userContext;

        public WorldQueryContext(b2World world, b2OverlapResultFcn fcn, b2QueryFilter filter, object userContext)
        {
            this.world = world;
            this.fcn = fcn;
            this.filter = filter;
            this.userContext = userContext;
        }
    }
}
