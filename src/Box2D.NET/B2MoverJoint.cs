// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2MoverJoint
    {
        public B2Vec2 linearVelocity;
        public B2Vec2 maxVelocityForce;

        public B2Vec2 linearVelocityImpulse;

        public int indexA;
        public int indexB;
        public B2Transform frameA;
        public B2Transform frameB;
        public float linearMass;
    }
}
