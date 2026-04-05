// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // A node in the dynamic tree.
    // todo externalize this to visualize internal nodes and speed up FindPairs
    public struct B2TreeNode
    {
        // The node bounding box
        public B2AABB aabb; // 16

        // Category bits for collision filtering
        public ulong categoryBits; // 8

        // TODO: @ikpil, check union
        public B2TreeNodeDataUnion children; // 8
        
        // TODO: @ikpil, check union
        public B2TreeNodeConnectionUnion pn;

        public ushort height; // 2
        public ushort flags; // 2
    }
}