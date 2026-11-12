// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    internal struct B2BuilderEdge
    {
        public int vertex1;
        public int vertex2;

        public B2BuilderEdge(int vertex1, int vertex2)
        {
            this.vertex1 = vertex1;
            this.vertex2 = vertex2;
        }
    }
}
