// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;

namespace Box2D.NET
{
    // Shifted to be distinct from b2ContactFlags
    [Flags]
    public enum B2ContactSimFlags
    {
        // Set when the shapes are touching
        b2_simTouchingFlag = 0x00010000,

        // This contact no longer has overlapping AABBs
        b2_simDisjoint = 0x00020000,

        // This contact started touching
        b2_simStartedTouching = 0x00040000,

        // This contact stopped touching
        b2_simStoppedTouching = 0x00080000,

        // This contact has a hit event
        b2_simEnableHitEvent = 0x00100000,

        // This contact wants pre-solve events
        b2_simEnablePreSolveEvents = 0x00200000,
    }
}
