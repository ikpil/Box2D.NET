// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;

namespace Box2D.NET
{
    /// A node in the dynamic tree. Siblings sit together at an even index so two nodes fit in
    /// a 64 byte cache line. The root is at index zero and index one always empty.
    [StructLayout(LayoutKind.Explicit)]
    public struct B2TreeNode
    {
        /// The node bounding box
        [FieldOffset(0)]
        public B2AABB aabb;

        /// In 3D this space is used by the AABB z components.
        [FieldOffset(16)]
        public ulong padding;

        /// bit 31 : 1 for leaf node
        /// bit 30 : 1 for moved flag
        /// bits 0-29 : index of the sibling pair node or the proxy id for a leaf
        [FieldOffset(24)]
        public uint flagIndex;

        /// The height of an internal node. A leaf has zero height.
        [FieldOffset(28)]
        public int height;

        /// The shape index for a leaf. Truncated from proxy user data.
        [FieldOffset(28)]
        public int shapeIndex;
    }
}
