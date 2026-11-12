// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2WorldShapeCastContext<T> where T : class
    {
        public B2World world;
        public b2CastResultCallbackFcn<T> callbackFcn;
        public B2QueryFilter filter;
        public float fraction;
        public B2Pos origin;

        // origin relative input
        public B2ShapeCastInput input;
        public T userContext;
    }
}
