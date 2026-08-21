// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    internal struct B2RecArgs_BodySetTargetTransform
    {
        internal B2BodyId body;
        internal B2Transform target;
        internal float timeStep;
        internal bool wake;
    }
}
