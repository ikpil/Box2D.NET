// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Result returned by b2SolvePlanes.
    public struct B2PlaneSolverResult
    {
        /// The final relative translation.
        public B2Vec2 delta;
        
        /// The number of iterations used by the plane solver. For diagnostics.
        public int iterationCount;

        public B2PlaneSolverResult(B2Vec2 delta, int iterationCount)
        {
            this.delta = delta;
            this.iterationCount = iterationCount;
        }
    }
}
