// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public class B2ChainShape
    {
        public int id;
        public int bodyId;
        public int nextChainId;
        public int segmentCount;
        public ushort generation;
        public int[] shapeIndices;
    }
}
