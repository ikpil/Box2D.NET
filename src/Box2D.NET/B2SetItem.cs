// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2SetItem
    {
        public ulong key;
        // storing lower 32 bits of hash
        // this is wasteful because I just need to know if the item is occupied
        // I could require the key to be non-zero and use 0 to indicate an empty slot
        public uint hash;

        public void Clear()
        {
            key = 0;
            hash = 0;
        }
    }
}
