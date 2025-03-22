// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Shared
{
    public struct Bone
    {
        public B2BodyId bodyId;
        public B2JointId jointId;
        public float frictionScale;
        public int parentIndex;
    }
}
