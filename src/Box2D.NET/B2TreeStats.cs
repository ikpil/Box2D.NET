// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// These are performance results returned by dynamic tree queries.
    public struct B2TreeStats
    {
        /// Number of internal nodes visited during the query
        public int nodeVisits;

        /// Number of leaf nodes visited during the query
        public int leafVisits;
    }
}
