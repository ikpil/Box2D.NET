// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2QueryPairContext
    {
        public B2World world;
        public B2MoveResult moveResult;
        public B2BodyType queryTreeType;
        public int queryProxyKey;
        public int queryShapeIndex;
    }
}
