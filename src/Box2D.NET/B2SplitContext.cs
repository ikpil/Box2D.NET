// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    internal struct B2SplitContext
    {
        public B2Vec2[] vertices;
        public B2Vec2 point1;
        public B2Vec2 point2;
        public int vertex1;
        public int vertex2;
        public float toleranceSquared;

        // T-junction splits.
        public B2Array<B2EdgeSplit> splits;
    }
}
