// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2SetItem
    {
        public ulong key;
        public uint hash;

        public void Clear()
        {
            key = 0;
            hash = 0;
        }
    }
}
