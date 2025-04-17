// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Result returned by b2SolvePlane
    public struct B2PlaneSolverResult
    {
        /// The final position of the mover
        public B2Vec2 position;
        
        /// The number of iterations used by the plane solver. For diagnostics.
        public int iterationCount;

        public B2PlaneSolverResult(B2Vec2 position, int iterationCount)
        {
            this.position = position;
            this.iterationCount = iterationCount;
        }
    }
}
