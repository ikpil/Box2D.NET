// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Solvers;

namespace Box2D.NET
{
    public static class B2MotorJoints
    {
        /**
         * @defgroup motor_joint Motor Joint
         * @brief Functions for the motor joint.
         *
         * The motor joint is designed to control the movement of a body while still being
         * responsive to collisions. A spring controls the position and rotation. A velocity motor
         * can be used to control velocity and allows for friction in top-down games. Both types
         * of control can be combined. For example, you can have a spring with friction.
         * Position and velocity control have force and torque limits.
         * @{
         */
        /// Set the desired relative linear velocity in meters per second
        public static void b2MotorJoint_SetLinearVelocity(B2JointId jointId, B2Vec2 velocity)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            joint.uj.motorJoint.linearVelocity = velocity;
        }

        /// Get the desired relative linear velocity in meters per second
        public static B2Vec2 b2MotorJoint_GetLinearVelocity(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            return joint.uj.motorJoint.linearVelocity;
        }

        /// Set the desired relative angular velocity in radians per second
        public static void b2MotorJoint_SetAngularVelocity(B2JointId jointId, float velocity)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            joint.uj.motorJoint.angularVelocity = velocity;
        }

        /// Get the desired relative angular velocity in radians per second
        public static float b2MotorJoint_GetAngularVelocity(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            return joint.uj.motorJoint.angularVelocity;
        }

        /// Set the motor joint maximum force, usually in newtons
        public static void b2MotorJoint_SetMaxVelocityForce(B2JointId jointId, float maxForce)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            joint.uj.motorJoint.maxVelocityForce = maxForce;
        }

        /// Get the motor joint maximum force, usually in newtons
        public static float b2MotorJoint_GetMaxVelocityForce(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            return joint.uj.motorJoint.maxVelocityForce;
        }

        /// Set the motor joint maximum torque, usually in newton-meters
        public static void b2MotorJoint_SetMaxVelocityTorque(B2JointId jointId, float maxTorque)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            joint.uj.motorJoint.maxVelocityTorque = maxTorque;
        }

        /// Get the motor joint maximum torque, usually in newton-meters
        public static float b2MotorJoint_GetMaxVelocityTorque(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            return joint.uj.motorJoint.maxVelocityTorque;
        }


        /// Set the spring linear hertz stiffness
        public static void b2MotorJoint_SetLinearHertz(B2JointId jointId, float hertz)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            joint.uj.motorJoint.linearHertz = hertz;
        }

        /// Get the spring linear hertz stiffness
        public static float b2MotorJoint_GetLinearHertz(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            return joint.uj.motorJoint.linearHertz;
        }

        /// Set the spring linear damping ratio. Use 1.0 for critical damping.
        public static void b2MotorJoint_SetLinearDampingRatio(B2JointId jointId, float damping)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            joint.uj.motorJoint.linearDampingRatio = damping;
        }

        /// Get the spring linear damping ratio.
        public static float b2MotorJoint_GetLinearDampingRatio(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            return joint.uj.motorJoint.linearDampingRatio;
        }

        /// Set the spring angular hertz stiffness
        public static void b2MotorJoint_SetAngularHertz(B2JointId jointId, float hertz)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            joint.uj.motorJoint.angularHertz = hertz;
        }

        /// Get the spring angular hertz stiffness
        public static float b2MotorJoint_GetAngularHertz(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            return joint.uj.motorJoint.angularHertz;
        }

        /// Set the spring angular damping ratio. Use 1.0 for critical damping.
        public static void b2MotorJoint_SetAngularDampingRatio(B2JointId jointId, float damping)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            joint.uj.motorJoint.angularDampingRatio = damping;
        }

        /// Get the spring angular damping ratio.
        public static float b2MotorJoint_GetAngularDampingRatio(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            return joint.uj.motorJoint.angularDampingRatio;
        }

        /// Set the maximum spring force in newtons.
        public static void b2MotorJoint_SetMaxSpringForce(B2JointId jointId, float maxForce)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            joint.uj.motorJoint.maxSpringForce = b2MaxFloat(0.0f, maxForce);
        }

        /// Get the maximum spring force in newtons.
        public static float b2MotorJoint_GetMaxSpringForce(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            return joint.uj.motorJoint.maxSpringForce;
        }

        /// Set the maximum spring torque in newtons * meters
        public static void b2MotorJoint_SetMaxSpringTorque(B2JointId jointId, float maxTorque)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            joint.uj.motorJoint.maxSpringTorque = b2MaxFloat(0.0f, maxTorque);
        }

        /// Get the maximum spring torque in newtons * meters
        public static float b2MotorJoint_GetMaxSpringTorque(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_motorJoint);
            return joint.uj.motorJoint.maxSpringTorque;
        }

        internal static B2Vec2 b2GetMotorJointForce(B2World world, B2JointSim @base)
        {
            B2Vec2 force = b2MulSV(world.inv_h, b2Add(@base.uj.motorJoint.linearVelocityImpulse, @base.uj.motorJoint.linearSpringImpulse));
            return force;
        }

        internal static float b2GetMotorJointTorque(B2World world, B2JointSim @base)
        {
            return world.inv_h * (@base.uj.motorJoint.angularVelocityImpulse + @base.uj.motorJoint.angularSpringImpulse);
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

        internal static void b2PrepareMotorJoint(B2JointSim @base, B2StepContext context)
        {
            B2_ASSERT(@base.type == B2JointType.b2_motorJoint);

            // chase body id to the solver set where the body lives
            int idA = @base.bodyIdA;
            int idB = @base.bodyIdB;

            B2World world = context.world;

            B2Body bodyA = b2Array_Get(ref world.bodies, idA);
            B2Body bodyB = b2Array_Get(ref world.bodies, idB);

            B2_ASSERT(bodyA.setIndex == (int)B2SolverSetType.b2_awakeSet || bodyB.setIndex == (int)B2SolverSetType.b2_awakeSet);

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
            joint.indexA = bodyA.setIndex == (int)B2SolverSetType.b2_awakeSet ? localIndexA : B2_NULL_INDEX;
            joint.indexB = bodyB.setIndex == (int)B2SolverSetType.b2_awakeSet ? localIndexB : B2_NULL_INDEX;

            // Compute joint anchor frames with world space rotation, relative to center of mass
            joint.frameA.q = b2MulRot(bodySimA.transform.q, @base.localFrameA.q);
            joint.frameA.p = b2RotateVector(bodySimA.transform.q, b2Sub(@base.localFrameA.p, bodySimA.localCenter));
            joint.frameB.q = b2MulRot(bodySimB.transform.q, @base.localFrameB.q);
            joint.frameB.p = b2RotateVector(bodySimB.transform.q, b2Sub(@base.localFrameB.p, bodySimB.localCenter));

            // Compute the initial center delta. Incremental position updates are relative to this.
            joint.deltaCenter = b2Sub(bodySimB.center, bodySimA.center);

            B2Vec2 rA = joint.frameA.p;
            B2Vec2 rB = joint.frameB.p;

            joint.linearSpring = b2MakeSoft(joint.linearHertz, joint.linearDampingRatio, context.h);
            joint.angularSpring = b2MakeSoft(joint.angularHertz, joint.angularDampingRatio, context.h);

            B2Mat22 kl;
            kl.cx.X = mA + mB + rA.Y * rA.Y * iA + rB.Y * rB.Y * iB;
            kl.cx.Y = -rA.Y * rA.X * iA - rB.Y * rB.X * iB;
            kl.cy.X = kl.cx.Y;
            kl.cy.Y = mA + mB + rA.X * rA.X * iA + rB.X * rB.X * iB;
            joint.linearMass = b2GetInverse22(kl);

            float ka = iA + iB;
            joint.angularMass = ka > 0.0f ? 1.0f / ka : 0.0f;

            if (context.enableWarmStarting == false)
            {
                joint.linearVelocityImpulse = b2Vec2_zero;
                joint.angularVelocityImpulse = 0.0f;
                joint.linearSpringImpulse = b2Vec2_zero;
                joint.angularSpringImpulse = 0.0f;
            }
        }

        internal static void b2WarmStartMotorJoint(B2JointSim @base, B2StepContext context)
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

            B2Vec2 linearImpulse = b2Add(joint.linearVelocityImpulse, joint.linearSpringImpulse);
            float angularImpulse = joint.angularVelocityImpulse + joint.angularSpringImpulse;

            if (0 != (stateA.flags & (uint)B2BodyFlags.b2_dynamicFlag))
            {
                stateA.linearVelocity = b2MulSub(stateA.linearVelocity, mA, linearImpulse);
                stateA.angularVelocity -= iA * (b2Cross(rA, linearImpulse) + angularImpulse);
            }

            if (0 != (stateB.flags & (uint)B2BodyFlags.b2_dynamicFlag))
            {
                stateB.linearVelocity = b2MulAdd(stateB.linearVelocity, mB, linearImpulse);
                stateB.angularVelocity += iB * (b2Cross(rB, linearImpulse) + angularImpulse);
            }
        }

        internal static void b2SolveMotorJoint(B2JointSim @base, B2StepContext context)
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

            // angular spring
            if (joint.maxSpringTorque > 0.0f && joint.angularHertz > 0.0f)
            {
                B2Rot qA = b2MulRot(stateA.deltaRotation, joint.frameA.q);
                B2Rot qB = b2MulRot(stateB.deltaRotation, joint.frameB.q);
                B2Rot relQ = b2InvMulRot(qA, qB);

                float c = b2Rot_GetAngle(relQ);
                float bias = joint.angularSpring.biasRate * c;
                float massScale = joint.angularSpring.massScale;
                float impulseScale = joint.angularSpring.impulseScale;

                float cdot = wB - wA;

                float maxImpulse = context.h * joint.maxSpringTorque;
                float oldImpulse = joint.angularSpringImpulse;
                float impulse = -massScale * joint.angularMass * (cdot + bias) - impulseScale * oldImpulse;
                joint.angularSpringImpulse = b2ClampFloat(oldImpulse + impulse, -maxImpulse, maxImpulse);
                impulse = joint.angularSpringImpulse - oldImpulse;

                wA -= iA * impulse;
                wB += iB * impulse;
            }

            // angular velocity
            if (joint.maxVelocityTorque > 0.0)
            {
                float cdot = wB - wA - joint.angularVelocity;
                float impulse = -joint.angularMass * cdot;

                float maxImpulse = context.h * joint.maxVelocityTorque;
                float oldImpulse = joint.angularVelocityImpulse;
                joint.angularVelocityImpulse = b2ClampFloat(oldImpulse + impulse, -maxImpulse, maxImpulse);
                impulse = joint.angularVelocityImpulse - oldImpulse;

                wA -= iA * impulse;
                wB += iB * impulse;
            }

            B2Vec2 rA = b2RotateVector(stateA.deltaRotation, joint.frameA.p);
            B2Vec2 rB = b2RotateVector(stateB.deltaRotation, joint.frameB.p);

            // linear spring
            if (joint.maxSpringForce > 0.0f && joint.linearHertz > 0.0f)
            {
                B2Vec2 dcA = stateA.deltaPosition;
                B2Vec2 dcB = stateB.deltaPosition;
                B2Vec2 c = b2Add(b2Add(b2Sub(dcB, dcA), b2Sub(rB, rA)), joint.deltaCenter);

                B2Vec2 bias = b2MulSV(joint.linearSpring.biasRate, c);
                float massScale = joint.linearSpring.massScale;
                float impulseScale = joint.linearSpring.impulseScale;

                B2Vec2 cdot = b2Sub(b2Add(vB, b2CrossSV(wB, rB)), b2Add(vA, b2CrossSV(wA, rA)));
                cdot = b2Add(cdot, bias);

                // Updating the effective mass here may be overkill
                B2Mat22 kl;
                kl.cx.X = mA + mB + rA.Y * rA.Y * iA + rB.Y * rB.Y * iB;
                kl.cx.Y = -rA.Y * rA.X * iA - rB.Y * rB.X * iB;
                kl.cy.X = kl.cx.Y;
                kl.cy.Y = mA + mB + rA.X * rA.X * iA + rB.X * rB.X * iB;
                joint.linearMass = b2GetInverse22(kl);

                B2Vec2 b = b2MulMV(joint.linearMass, cdot);

                B2Vec2 oldImpulse = joint.linearSpringImpulse;
                B2Vec2 impulse = new B2Vec2(
                    -massScale * b.X - impulseScale * oldImpulse.X,
                    -massScale * b.Y - impulseScale * oldImpulse.Y
                );

                float maxImpulse = context.h * joint.maxSpringForce;
                joint.linearSpringImpulse = b2Add(joint.linearSpringImpulse, impulse);

                if (b2LengthSquared(joint.linearSpringImpulse) > maxImpulse * maxImpulse)
                {
                    joint.linearSpringImpulse = b2Normalize(joint.linearSpringImpulse);
                    joint.linearSpringImpulse.X *= maxImpulse;
                    joint.linearSpringImpulse.Y *= maxImpulse;
                }

                impulse = b2Sub(joint.linearSpringImpulse, oldImpulse);

                vA = b2MulSub(vA, mA, impulse);
                wA -= iA * b2Cross(rA, impulse);
                vB = b2MulAdd(vB, mB, impulse);
                wB += iB * b2Cross(rB, impulse);
            }

            // linear velocity
            if (joint.maxVelocityForce > 0.0f)
            {
                B2Vec2 cdot = b2Sub(b2Add(vB, b2CrossSV(wB, rB)), b2Add(vA, b2CrossSV(wA, rA)));
                cdot = b2Sub(cdot, joint.linearVelocity);
                B2Vec2 b = b2MulMV(joint.linearMass, cdot);
                B2Vec2 impulse = new B2Vec2(-b.X, -b.Y);

                B2Vec2 oldImpulse = joint.linearVelocityImpulse;
                float maxImpulse = context.h * joint.maxVelocityForce;
                joint.linearVelocityImpulse = b2Add(joint.linearVelocityImpulse, impulse);

                if (b2LengthSquared(joint.linearVelocityImpulse) > maxImpulse * maxImpulse)
                {
                    joint.linearVelocityImpulse = b2Normalize(joint.linearVelocityImpulse);
                    joint.linearVelocityImpulse.X *= maxImpulse;
                    joint.linearVelocityImpulse.Y *= maxImpulse;
                }

                impulse = b2Sub(joint.linearVelocityImpulse, oldImpulse);

                vA = b2MulSub(vA, mA, impulse);
                wA -= iA * b2Cross(rA, impulse);
                vB = b2MulAdd(vB, mB, impulse);
                wB += iB * b2Cross(rB, impulse);
            }

            if (0 != (stateA.flags & (uint)B2BodyFlags.b2_dynamicFlag))
            {
                stateA.linearVelocity = vA;
                stateA.angularVelocity = wA;
            }

            if (0 != (stateB.flags & (uint)B2BodyFlags.b2_dynamicFlag))
            {
                stateB.linearVelocity = vB;
                stateB.angularVelocity = wB;
            }
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