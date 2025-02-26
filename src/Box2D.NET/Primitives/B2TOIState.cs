// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    /// Describes the TOI output
    public enum b2TOIState
    {
        b2_toiStateUnknown,
        b2_toiStateFailed,
        b2_toiStateOverlapped,
        b2_toiStateHit,
        b2_toiStateSeparated
    }
}
