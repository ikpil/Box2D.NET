// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Separate storage for tree leaves.
    public struct B2TreeProxy
    {
        /// User data is an index instead of void* because it is used internally as a shape index.
        public ulong userData;

        /// Category bits for collision filtering.
        public ulong categoryBits;

        /// The leaf node. B2_NULL_INDEX for a free proxy.
        public int node;

        /// Next free proxy.
        public int next;
    }
}
