// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Shared
{
    public struct FallingHingeData
    {
        public B2BodyId[] bodyIds;
        public int bodyCount;
        public int stepCount;
        public int sleepStep;
        public uint hash;
    }
}
