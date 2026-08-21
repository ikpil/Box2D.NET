// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2WorldRayCastContext<T> where T : class
    {
        public B2World world;
        public b2CastResultFcn<T> fcn;
        public B2QueryFilter filter;
        public float fraction;
        public T userContext;
        internal B2RecQueryWriter recording;

        internal B2WorldRayCastContext(B2World world, b2CastResultFcn<T> fcn, in B2QueryFilter filter, float fraction, T userContext)
        {
            this.world = world;
            this.fcn = fcn;
            this.filter = filter;
            this.fraction = fraction;
            this.userContext = userContext;
            this.recording = default;
        }
    }
}
