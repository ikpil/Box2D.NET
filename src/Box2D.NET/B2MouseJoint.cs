// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public class B2MouseJoint
    {
        public B2Vec2 targetA;
        public float hertz;
        public float dampingRatio;
        public float maxForce;

        public B2Vec2 linearImpulse;
        public float angularImpulse;

        public B2Softness linearSoftness;
        public B2Softness angularSoftness;
        public int indexB;
        public B2Vec2 anchorB;
        public B2Vec2 deltaCenter;
        public B2Mat22 linearMass;
    }
}
