// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Describes the TOI output
    public enum B2TOIState
    {
        b2_toiStateUnknown,
        b2_toiStateFailed,
        b2_toiStateOverlapped,
        b2_toiStateHit,
        b2_toiStateSeparated
    }
}
