// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Prepare/store run as a flat parallel-for over the whole wide-constraint
    // range. Each span maps a slice of that range back to the owning color's
    // contacts so workers can decode flat wide-slot indices without touching
    // graph state. The spans array has one entry per active color plus a sentinel
    // whose start == wideContactCount.
    public struct B2ContactPrepareSpan
    {
        public int start;
        public int count;
        public B2ContactSim[] contacts;
    }
}
