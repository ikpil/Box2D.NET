// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Shared.Primitives
{
    public class RainData
    {
        public Group[] groups = new Group[(int)RainConstants.RAIN_ROW_COUNT * (int)RainConstants.RAIN_COLUMN_COUNT];
        public float gridSize;
        public int gridCount;
        public int columnCount;
        public int columnIndex;
    }
}
