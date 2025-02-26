// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    public struct b2TreePlane
    {
        public b2AABB leftAABB;
        public b2AABB rightAABB;
        public int leftCount;
        public int rightCount;
    }
}
