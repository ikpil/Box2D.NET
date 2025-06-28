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
    }
}