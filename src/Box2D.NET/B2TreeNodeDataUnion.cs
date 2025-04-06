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
        public int child1; // Children (internal node)
        
        [FieldOffset(4)]
        public int child2; // Children (internal node)

        [FieldOffset(0)]
        public ulong userData; // User data (leaf node)
    }
}