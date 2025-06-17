// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Result returned by b2SolvePlane
    public struct B2PlaneSolverResult
    {
        /// The translation of the mover
        public B2Vec2 translation;
        
        /// The number of iterations used by the plane solver. For diagnostics.
        public int iterationCount;

        public B2PlaneSolverResult(B2Vec2 translation, int iterationCount)
        {
            this.translation = translation;
            this.iterationCount = iterationCount;
        }
    }
}
