// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// The @base joint class. Joints are used to constraint two bodies together in
    /// various fashions. Some joints also feature limits and motors.
    public class B2JointSim
    {
        public int jointId;

        public int bodyIdA;
        public int bodyIdB;

        public B2JointType type;

        // Anchors relative to body origin
        public B2Vec2 localOriginAnchorA;
        public B2Vec2 localOriginAnchorB;

        public float invMassA, invMassB;
        public float invIA, invIB;


        public float constraintHertz;
        public float constraintDampingRatio;

        public B2Softness constraintSoftness;

        // TODO: @ikpil, check union
        public B2JointUnion uj;

        public void Clear()
        {
            jointId = 0;
            bodyIdA = 0;
            bodyIdB = 0;
            type = B2JointType.b2_distanceJoint;
            localOriginAnchorA = new B2Vec2();
            localOriginAnchorB = new B2Vec2();
            invMassA = 0.0f;
            invMassB = 0.0f;
            invIA = 0.0f;
            invIB = 0.0f;
            constraintHertz = 0.0f;
            constraintDampingRatio = 0.0f;
            constraintSoftness = new B2Softness();
            uj = new B2JointUnion();
        }

        public void CopyFrom(B2JointSim other)
        {
            jointId = other.jointId;
            bodyIdA = other.bodyIdA;
            bodyIdB = other.bodyIdB;
            type = other.type;
            localOriginAnchorA = other.localOriginAnchorA;
            localOriginAnchorB = other.localOriginAnchorB;
            invMassA = other.invMassA;
            invMassB = other.invMassB;
            invIA = other.invIA;
            invIB = other.invIB;
            constraintHertz = other.constraintHertz;
            constraintDampingRatio = other.constraintDampingRatio;
            constraintSoftness = other.constraintSoftness;
            uj = other.uj;
        }
    }
}