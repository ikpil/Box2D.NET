// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    public class b2WeldJoint
    {
        public float referenceAngle;
        public float linearHertz;
        public float linearDampingRatio;
        public float angularHertz;
        public float angularDampingRatio;

        public b2Softness linearSoftness;
        public b2Softness angularSoftness;
        public b2Vec2 linearImpulse;
        public float angularImpulse;

        public int indexA;
        public int indexB;
        public b2Vec2 anchorA;
        public b2Vec2 anchorB;
        public b2Vec2 deltaCenter;
        public float deltaAngle;
        public float axialMass;
    }
}
