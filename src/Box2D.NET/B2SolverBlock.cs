// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Solver block describes a multithreaded unit of work.
    public class B2SolverBlock
    {
        public int startIndex;
        public ushort count;

        // b2SolverBlockType
        public byte blockType;
        public byte colorIndex;

        public B2AtomicInt syncIndex;
    }
}
