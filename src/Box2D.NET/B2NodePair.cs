// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2NodePair
    {
        public B2TreeNode a;
        public B2TreeNode b;

        public B2NodePair(B2TreeNode a, B2TreeNode b)
        {
            this.a = a;
            this.b = b;
        }
    }
}
