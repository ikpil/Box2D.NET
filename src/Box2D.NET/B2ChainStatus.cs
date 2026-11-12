// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Status after converging polygons to chains.
    public enum B2ChainStatus
    {
        b2_chainStatusSuccess,
        b2_chainStatusCornerTouch,
        b2_chainStatusOverlap,
    }
}
