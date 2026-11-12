// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// An array of chain loops created by merging polygons.
    public struct B2ChainResult
    {
        public B2ChainData[] chains;
        public int chainCount;
        public B2ChainStatus status;
    }
}
