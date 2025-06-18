// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2WeldJoint
    {
        public float linearHertz;
        public float linearDampingRatio;
        public float angularHertz;
        public float angularDampingRatio;

        public B2Softness linearSoftness;
        public B2Softness angularSoftness;
        public B2Vec2 linearImpulse;
        public float angularImpulse;

        public int indexA;
        public int indexB;
        public B2Transform frameA;
        public B2Transform frameB;
        public B2Vec2 deltaCenter;
        public float axialMass;
    }
}
