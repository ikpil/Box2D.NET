// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // A unit of multithreaded work along with atomic synchronization. The syncIndex grows
    // monotonically allowing the solver block to be re-used across sub-steps.
    // TODO: @ikpil, this is a struct in C. It is a class here so the atomic syncIndex
    // can be passed by reference out of an array.
    public class B2SyncBlock
    {
        public B2SolverBlock block;
        public B2AtomicInt syncIndex;
    }
}
