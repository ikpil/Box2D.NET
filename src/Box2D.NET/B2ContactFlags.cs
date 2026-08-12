// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;

namespace Box2D.NET
{
    // A contact edge is used to connect bodies and contacts together
    // in a contact graph where each body is a node and each contact
    // is an edge. A contact edge belongs to a doubly linked list
    // maintained in each attached body. Each contact has two contact
    // edges, one for each attached body.
    [Flags]
    public enum B2ContactFlags
    {
        // Set when the solid shapes are touching.
        b2_contactTouchingFlag = 0x00000001,

        // Contact has a hit event
        b2_contactHitEventFlag = 0x00000002,

        // This contact wants contact events
        b2_contactEnableContactEvents = 0x00000004,

        b2_contactRecycleFlag = 0x00000008,

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

        // This contact has a cached relative transform
        b2_simRelativeTransformValid = 0x00400000,
    }
}
