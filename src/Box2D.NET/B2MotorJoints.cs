// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Joints;


namespace Box2D.NET
{
    public static class B2MotorJoints
    {
        /**
         * @defgroup motor_joint Motor Joint
         * @brief Functions for the motor joint.
         *
         * The motor joint is used to drive the relative transform between two bodies. The target
         * is set by updating the local frames using b2Joint_SetLocalFrameA or b2Joint_SetLocalFrameB.
         * @{
         */
        public static void b2MotorJoint_SetMaxForce(B2JointId jointId, float maxForce)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            joint.uj.motorJoint.maxForce = b2MaxFloat(0.0f, maxForce);
        }

        public static float b2MotorJoint_GetMaxForce(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            return joint.uj.motorJoint.maxForce;
        }

        public static void b2MotorJoint_SetMaxTorque(B2JointId jointId, float maxTorque)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            joint.uj.motorJoint.maxTorque = b2MaxFloat(0.0f, maxTorque);
        }

        public static float b2MotorJoint_GetMaxTorque(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            return joint.uj.motorJoint.maxTorque;
        }

        public static void b2MotorJoint_SetCorrectionFactor(B2JointId jointId, float correctionFactor)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            joint.uj.motorJoint.correctionFactor = b2ClampFloat(correctionFactor, 0.0f, 1.0f);
        }

        public static float b2MotorJoint_GetCorrectionFactor(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            return joint.uj.motorJoint.correctionFactor;
        }

        public static B2Vec2 b2GetMotorJointForce(B2World world, B2JointSim @base)
        {
            B2Vec2 force = b2MulSV(world.inv_h, @base.uj.motorJoint.linearImpulse);
            return force;
        }

        public static float b2GetMotorJointTorque(B2World world, B2JointSim @base)
        {
            return world.inv_h * @base.uj.motorJoint.angularImpulse;
        }

        // Point-to-point constraint
        // C = p2 - p1
        // Cdot = v2 - v1
        //      = v2 + cross(w2, r2) - v1 - cross(w1, r1)
        // J = [-I -r1_skew I r2_skew ]
        // Identity used:
        // w k % (rx i + ry j) = w * (-ry i + rx j)

        // Angle constraint
        // C = angle2 - angle1 - referenceAngle
        // Cdot = w2 - w1
        // J = [0 0 -1 0 0 1]
        // K = invI1 + invI2

        public static void b2PrepareMotorJoint(B2JointSim @base, B2StepContext context)
        {
            B2_ASSERT(@base.type == B2JointType.b2_motorJoint);

            // chase body id to the solver set where the body lives
            int idA = @base.bodyIdA;
            int idB = @base.bodyIdB;

            B2World world = context.world;

            B2Body bodyA = b2Array_Get(ref world.bodies, idA);
            B2Body bodyB = b2Array_Get(ref world.bodies, idB);

            B2_ASSERT(bodyA.setIndex == (int)B2SetType.b2_awakeSet || bodyB.setIndex == (int)B2SetType.b2_awakeSet);

            B2SolverSet setA = b2Array_Get(ref world.solverSets, bodyA.setIndex);
            B2SolverSet setB = b2Array_Get(ref world.solverSets, bodyB.setIndex);

            int localIndexA = bodyA.localIndex;
            int localIndexB = bodyB.localIndex;

            B2BodySim bodySimA = b2Array_Get(ref setA.bodySims, localIndexA);
            B2BodySim bodySimB = b2Array_Get(ref setB.bodySims, localIndexB);

            float mA = bodySimA.invMass;
            float iA = bodySimA.invInertia;
            float mB = bodySimB.invMass;
            float iB = bodySimB.invInertia;

            @base.invMassA = mA;
            @base.invMassB = mB;
            @base.invIA = iA;
            @base.invIB = iB;

            ref B2MotorJoint joint = ref @base.uj.motorJoint;
            joint.indexA = bodyA.setIndex == (int)B2SetType.b2_awakeSet ? localIndexA : B2_NULL_INDEX;
            joint.indexB = bodyB.setIndex == (int)B2SetType.b2_awakeSet ? localIndexB : B2_NULL_INDEX;

            // Compute joint anchor frames with world space rotation, relative to center of mass
            joint.frameA.q = b2MulRot(bodySimA.transform.q, @base.localFrameA.q);
            joint.frameA.p = b2RotateVector(bodySimA.transform.q, b2Sub(@base.localFrameA.p, bodySimA.localCenter));
            joint.frameB.q = b2MulRot(bodySimB.transform.q, @base.localFrameB.q);
            joint.frameB.p = b2RotateVector(bodySimB.transform.q, b2Sub(@base.localFrameB.p, bodySimB.localCenter));

            // Compute the initial center delta. Incremental position updates are relative to this.
            joint.deltaCenter = b2Sub(bodySimB.center, bodySimA.center);

            B2Vec2 rA = joint.frameA.p;
            B2Vec2 rB = joint.frameB.p;

            B2Mat22 K;
            K.cx.X = mA + mB + rA.Y * rA.Y * iA + rB.Y * rB.Y * iB;
            K.cx.Y = -rA.Y * rA.X * iA - rB.Y * rB.X * iB;
            K.cy.X = K.cx.Y;
            K.cy.Y = mA + mB + rA.X * rA.X * iA + rB.X * rB.X * iB;
            joint.linearMass = b2GetInverse22(K);

            float ka = iA + iB;
            joint.angularMass = ka > 0.0f ? 1.0f / ka : 0.0f;

            if (context.enableWarmStarting == false)
            {
                joint.linearImpulse = b2Vec2_zero;
                joint.angularImpulse = 0.0f;
            }
        }

        public static void b2WarmStartMotorJoint(B2JointSim @base, B2StepContext context)
        {
            B2_ASSERT(@base.type == B2JointType.b2_motorJoint);

            float mA = @base.invMassA;
            float mB = @base.invMassB;
            float iA = @base.invIA;
            float iB = @base.invIB;

            ref readonly B2MotorJoint joint = ref @base.uj.motorJoint;

            // dummy state for static bodies
            B2BodyState dummyState = B2BodyState.Create(b2_identityBodyState);

            B2BodyState stateA = joint.indexA == B2_NULL_INDEX ? dummyState : context.states[joint.indexA];
            B2BodyState stateB = joint.indexB == B2_NULL_INDEX ? dummyState : context.states[joint.indexB];

            B2Vec2 rA = b2RotateVector(stateA.deltaRotation, joint.frameA.p);
            B2Vec2 rB = b2RotateVector(stateB.deltaRotation, joint.frameB.p);

            stateA.linearVelocity = b2MulSub(stateA.linearVelocity, mA, joint.linearImpulse);
            stateA.angularVelocity -= iA * (b2Cross(rA, joint.linearImpulse) + joint.angularImpulse);
            stateB.linearVelocity = b2MulAdd(stateB.linearVelocity, mB, joint.linearImpulse);
            stateB.angularVelocity += iB * (b2Cross(rB, joint.linearImpulse) + joint.angularImpulse);
        }

        public static void b2SolveMotorJoint(B2JointSim @base, B2StepContext context)
        {
            B2_ASSERT(@base.type == B2JointType.b2_motorJoint);

            float mA = @base.invMassA;
            float mB = @base.invMassB;
            float iA = @base.invIA;
            float iB = @base.invIB;

            // dummy state for static bodies
            B2BodyState dummyState = B2BodyState.Create(b2_identityBodyState);

            ref B2MotorJoint joint = ref @base.uj.motorJoint;
            B2BodyState stateA = joint.indexA == B2_NULL_INDEX ? dummyState : context.states[joint.indexA];
            B2BodyState stateB = joint.indexB == B2_NULL_INDEX ? dummyState : context.states[joint.indexB];

            B2Vec2 vA = stateA.linearVelocity;
            float wA = stateA.angularVelocity;
            B2Vec2 vB = stateB.linearVelocity;
            float wB = stateB.angularVelocity;

            // angular constraint
            {
                B2Rot qA = b2MulRot(stateA.deltaRotation, joint.frameA.q);
                B2Rot qB = b2MulRot(stateB.deltaRotation, joint.frameB.q);
                B2Rot relQ = b2InvMulRot(qA, qB);

                float jointAngle = b2Rot_GetAngle(relQ);
                float angularBias = context.inv_h * joint.correctionFactor * jointAngle;

                float Cdot = wB - wA;
                float impulse = -joint.angularMass * (Cdot + angularBias);

                float oldImpulse = joint.angularImpulse;
                float maxImpulse = context.h * joint.maxTorque;
                joint.angularImpulse = b2ClampFloat(joint.angularImpulse + impulse, -maxImpulse, maxImpulse);
                impulse = joint.angularImpulse - oldImpulse;

                wA -= iA * impulse;
                wB += iB * impulse;
            }

            // linear constraint
            {
                B2Vec2 rA = b2RotateVector(stateA.deltaRotation, joint.frameA.p);
                B2Vec2 rB = b2RotateVector(stateB.deltaRotation, joint.frameB.p);

                B2Vec2 ds = b2Add(b2Sub(stateB.deltaPosition, stateA.deltaPosition), b2Sub(rB, rA));
                B2Vec2 linearSeparation = b2Add(joint.deltaCenter, ds);
                B2Vec2 linearBias = b2MulSV(context.inv_h * joint.correctionFactor, linearSeparation);

                B2Vec2 Cdot = b2Sub(b2Add(vB, b2CrossSV(wB, rB)), b2Add(vA, b2CrossSV(wA, rA)));
                B2Vec2 b = b2MulMV(joint.linearMass, b2Add(Cdot, linearBias));
                B2Vec2 impulse = new B2Vec2(-b.X, -b.Y);

                B2Vec2 oldImpulse = joint.linearImpulse;
                float maxImpulse = context.h * joint.maxForce;
                joint.linearImpulse = b2Add(joint.linearImpulse, impulse);

                if (b2LengthSquared(joint.linearImpulse) > maxImpulse * maxImpulse)
                {
                    joint.linearImpulse = b2Normalize(joint.linearImpulse);
                    joint.linearImpulse.X *= maxImpulse;
                    joint.linearImpulse.Y *= maxImpulse;
                }

                impulse = b2Sub(joint.linearImpulse, oldImpulse);

                vA = b2MulSub(vA, mA, impulse);
                wA -= iA * b2Cross(rA, impulse);
                vB = b2MulAdd(vB, mB, impulse);
                wB += iB * b2Cross(rB, impulse);
            }

            stateA.linearVelocity = vA;
            stateA.angularVelocity = wA;
            stateB.linearVelocity = vB;
            stateB.angularVelocity = wB;
        }

#if FALSE
    void b2DumpMotorJoint()
    {
        int32 indexA = m_bodyA.m_islandIndex;
        int32 indexB = m_bodyB.m_islandIndex;

        b2Dump("  b2MotorJointDef jd;\n");
        b2Dump("  jd.bodyA = sims[%d];\n", indexA);
        b2Dump("  jd.bodyB = sims[%d];\n", indexB);
        b2Dump("  jd.collideConnected = bool(%d);\n", m_collideConnected);
        b2Dump("  jd.localAnchorA.Set(%.9g, %.9g);\n", m_localAnchorA.x, m_localAnchorA.y);
        b2Dump("  jd.localAnchorB.Set(%.9g, %.9g);\n", m_localAnchorB.x, m_localAnchorB.y);
        b2Dump("  jd.referenceAngle = %.9g;\n", m_referenceAngle);
        b2Dump("  jd.stiffness = %.9g;\n", m_stiffness);
        b2Dump("  jd.damping = %.9g;\n", m_damping);
        b2Dump("  joints[%d] = m_world.CreateJoint(&jd);\n", m_index);
    }
#endif
    }
}