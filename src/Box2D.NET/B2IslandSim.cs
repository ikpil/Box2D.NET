// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // This is used to move islands across solver sets
    public class B2IslandSim
    {
        public int islandId;

        public void CopyFrom(B2IslandSim other)
        {
            islandId = other.islandId;
        }
    }
}
