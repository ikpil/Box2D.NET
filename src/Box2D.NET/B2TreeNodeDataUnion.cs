// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;

namespace Box2D.NET
{
    [StructLayout(LayoutKind.Explicit)]
    public struct B2TreeNodeDataUnion
    {
        [FieldOffset(0)]
        public int child2; // Child 2 index (internal node)

        [FieldOffset(0)]
        public int userData; // User data (leaf node)
    }
}