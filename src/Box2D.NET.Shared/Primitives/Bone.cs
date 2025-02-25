// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;

namespace Box2D.NET.Shared.Primitives
{
    public struct Bone
    {
        public b2BodyId bodyId;
        public b2JointId jointId;
        public float frictionScale;
        public int parentIndex;
    }
}
