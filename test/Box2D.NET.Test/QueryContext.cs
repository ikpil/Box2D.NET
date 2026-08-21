// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Collections.Generic;

namespace Box2D.NET.Test
{
    internal sealed class QueryContext
    {
        internal List<int> generations = new List<int>();
        internal int generation;
    }
}
