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

        public B2Transform localFrameA;
        public B2Transform localFrameB;

        public float invMassA, invMassB;
        public float invIA, invIB;

        public float constraintHertz;
        public float constraintDampingRatio;

        public B2Softness constraintSoftness;

        public float forceThreshold;
        public float torqueThreshold;
        
        // TODO: @ikpil, check union
        public B2JointUnion uj;

        public void Clear()
        {
            jointId = 0;
            bodyIdA = 0;
            bodyIdB = 0;
            type = B2JointType.b2_distanceJoint;
            localFrameA = new B2Transform();
            localFrameB = new B2Transform();
            invMassA = 0.0f;
            invMassB = 0.0f;
            invIA = 0.0f;
            invIB = 0.0f;
            constraintHertz = 0.0f;
            constraintDampingRatio = 0.0f;
            constraintSoftness = new B2Softness();
            forceThreshold = 0;
            torqueThreshold = 0;
            uj = new B2JointUnion();
        }

        public void CopyFrom(B2JointSim other)
        {
            jointId = other.jointId;
            bodyIdA = other.bodyIdA;
            bodyIdB = other.bodyIdB;
            type = other.type;
            localFrameA = other.localFrameA;
            localFrameB = other.localFrameB;
            invMassA = other.invMassA;
            invMassB = other.invMassB;
            invIA = other.invIA;
            invIB = other.invIB;
            constraintHertz = other.constraintHertz;
            constraintDampingRatio = other.constraintDampingRatio;
            constraintSoftness = other.constraintSoftness;
            forceThreshold = other.forceThreshold;
            torqueThreshold = other.torqueThreshold;
            uj = other.uj;
        }
    }
}