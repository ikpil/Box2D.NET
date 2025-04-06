// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2ContactConstraintPoint
    {
        public B2Vec2 anchorA, anchorB;
        public float baseSeparation;
        public float relativeVelocity;
        public float normalImpulse;
        public float tangentImpulse;
        public float totalNormalImpulse;
        public float normalMass;
        public float tangentMass;
    }
}
