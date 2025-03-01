// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Simplex from the GJK algorithm
    public class B2Simplex
    {
        public B2SimplexVertex v1 = new B2SimplexVertex();
        public B2SimplexVertex v2 = new B2SimplexVertex();
        public B2SimplexVertex v3 = new B2SimplexVertex(); // vertices
        public int count; // number of valid vertices
    }
}
