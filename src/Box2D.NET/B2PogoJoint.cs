// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // The pogo joint is a hybrid between a contact and a joint
    public struct B2PogoJoint
    {
        // Settings
        public B2Vec2 normal;
        public float restLength;
        public float hertz;
        public float dampingRatio;
        public float maxTensionForce;
        public float maxCompressionForce;

        // Runtime
        public float impulse;
        public int indexA;
        public int indexB;
        public B2Transform frameA;
        public B2Transform frameB;
        public B2Vec2 deltaCenter;
        public float linearMass;
        public float velocity;
    }
}
