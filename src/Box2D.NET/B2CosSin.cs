// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Cosine and sine pair
    /// This uses a custom implementation designed for cross-platform determinism
    public struct B2CosSin
    {
        /// cosine and sine
        public float cosine;

        public float sine;
    }
}
