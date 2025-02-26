// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    public class b2MouseJoint
    {
        public b2Vec2 targetA;
        public float hertz;
        public float dampingRatio;
        public float maxForce;

        public b2Vec2 linearImpulse;
        public float angularImpulse;

        public b2Softness linearSoftness;
        public b2Softness angularSoftness;
        public int indexB;
        public b2Vec2 anchorB;
        public b2Vec2 deltaCenter;
        public b2Mat22 linearMass;
    }
}
