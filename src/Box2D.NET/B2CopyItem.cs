// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2CopyItem
    {
        public int oldPair;
        public int newIndex;

        public B2CopyItem(int oldPair, int newIndex)
        {
            this.oldPair = oldPair;
            this.newIndex = newIndex;
        }
    }
}
