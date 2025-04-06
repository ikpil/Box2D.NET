// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2MoverContext
    {
        public B2World world;
        public B2QueryFilter filter;
        public B2ShapeProxy proxy;
        public B2Transform transform;
        public object userContext;
    }
}
