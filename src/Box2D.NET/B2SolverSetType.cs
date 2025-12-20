// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public enum B2SolverSetType
    {
        // Static set for static bodies and joints between static bodies
        b2_staticSet = 0,

        // Disabled set for disabled bodies and their joints
        b2_disabledSet = 1,

        // Awake set for awake bodies and awake non-touching contacts. Awake touching contacts
        // and awake joints live in the constraint graph
        b2_awakeSet = 2,

        // The index of the first sleeping set. Each island that goes to sleep is put into
        // a sleeping set. This holds all bodies, contacts, and joints from the sleeping island.
        // A separate set for each sleeping island makes it very efficient to wake a single island.
        b2_firstSleepingSet = 3,
    }
}