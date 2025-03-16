﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// A node in the dynamic tree. This is private data placed here for performance reasons.
    public struct B2TreeNode
    {
        /// The node bounding box
        public B2AABB aabb; // 16

        /// Category bits for collision filtering
        public ulong categoryBits; // 8

        // TODO: @ikpil, check union
        // union
        // {
        private int _parent;
        //}; // 4

        /// The node parent index (allocated node)
        public int parent { get => _parent; set => _parent = value; } // this is union

        /// The node freelist next index (free node)
        public int next { get => _parent; set => _parent = value; } // this is union

        /// Child 1 index (internal node)
        public int child1; // 4

        // TODO: @ikpil, check union
        // union
        //{
        private int _child2;
        //}; // 4

        /// Child 2 index (internal node)
        public int child2 { get => _child2; set => _child2 = value; } // this is union

        /// User data (leaf node)
        public int userData { get => _child2; set => _child2 = value; } // this is union

        public ushort height; // 2
        public ushort flags; // 2
    }
}
