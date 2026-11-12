// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Similar for the narrow phase.
    public struct B2ContactCollideSpan
    {
        public int start;
        public B2ContactSim[] contacts;
    }
}
