// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Solvers;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET
{
    public static class B2RevoluteJoints
    {
        public static void b2RevoluteJoint_EnableSpring(B2JointId jointId, bool enableSpring)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_revoluteJoint);
            if (enableSpring != joint.uj.revoluteJoint.enableSpring)
            {
                joint.uj.revoluteJoint.enableSpring = enableSpring;
                joint.uj.revoluteJoint.springImpulse = 0.0f;
            }
        }

        public static bool b2RevoluteJoint_IsSpringEnabled(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_revoluteJoint);
            return joint.uj.revoluteJoint.enableSpring;
        }

        public static void b2RevoluteJoint_SetSpringHertz(B2JointId jointId, float hertz)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_revoluteJoint);
            joint.uj.revoluteJoint.hertz = hertz;
        }

        public static float b2RevoluteJoint_GetSpringHertz(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_revoluteJoint);
            return joint.uj.revoluteJoint.hertz;
        }

        public static void b2RevoluteJoint_SetSpringDampingRatio(B2JointId jointId, float dampingRatio)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_revoluteJoint);
            joint.uj.revoluteJoint.dampingRatio = dampingRatio;
        }

        public static float b2RevoluteJoint_GetSpringDampingRatio(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_revoluteJoint);
            return joint.uj.revoluteJoint.dampingRatio;
        }

        /// Set the revolute joint spring target angle, radians
        public static void b2RevoluteJoint_SetTargetAngle(B2JointId jointId, float angle)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_revoluteJoint);
            joint.uj.revoluteJoint.targetAngle = angle;
        }

        /// Get the revolute joint spring target angle, radians
        public static float b2RevoluteJoint_GetTargetAngle(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_revoluteJoint);
            return joint.uj.revoluteJoint.targetAngle;
        }

        public static float b2RevoluteJoint_GetAngle(B2JointId jointId)
        {
            B2World world = b2GetWorld(jointId.world0);
            B2JointSim jointSim = b2GetJointSimCheckType(jointId, B2JointType.b2_revoluteJoint);
            B2Transform transformA = b2GetBodyTransform(world, jointSim.bodyIdA);
            B2Transform transformB = b2GetBodyTransform(world, jointSim.bodyIdB);

            B2Rot qA = b2MulRot(transformA.q, jointSim.localFrameA.q);
            B2Rot qB = b2MulRot(transformB.q, jointSim.localFrameB.q);

            float angle = b2RelativeAngle(qA, qB);
            return angle;
        }

        public static void b2RevoluteJoint_EnableLimit(B2JointId jointId, bool enableLimit)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_revoluteJoint);
            if (enableLimit != joint.uj.revoluteJoint.enableLimit)
            {
                joint.uj.revoluteJoint.enableLimit = enableLimit;
                joint.uj.revoluteJoint.lowerImpulse = 0.0f;
                joint.uj.revoluteJoint.upperImpulse = 0.0f;
            }
        }

        public static bool b2RevoluteJoint_IsLimitEnabled(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_revoluteJoint);
            return joint.uj.revoluteJoint.enableLimit;
        }

        public static float b2RevoluteJoint_GetLowerLimit(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_revoluteJoint);
            return joint.uj.revoluteJoint.lowerAngle;
        }

        public static float b2RevoluteJoint_GetUpperLimit(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_revoluteJoint);
            return joint.uj.revoluteJoint.upperAngle;
        }

        // Set the revolute joint limits in radians. It is expected that lower <= upper
        // and that -0.99 * B2_PI <= lower && upper <= -0.99 * B2_PI.
        public static void b2RevoluteJoint_SetLimits(B2JointId jointId, float lower, float upper)
        {
            B2_ASSERT(lower <= upper);
            B2_ASSERT(lower >= -0.99f * B2_PI);
            B2_ASSERT(upper <= 0.99f * B2_PI);

            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_revoluteJoint);
            if (lower != joint.uj.revoluteJoint.lowerAngle || upper != joint.uj.revoluteJoint.upperAngle)
            {
                joint.uj.revoluteJoint.lowerAngle = b2MinFloat(lower, upper);
                joint.uj.revoluteJoint.upperAngle = b2MaxFloat(lower, upper);
                joint.uj.revoluteJoint.lowerImpulse = 0.0f;
                joint.uj.revoluteJoint.upperImpulse = 0.0f;
            }
        }

        public static void b2RevoluteJoint_EnableMotor(B2JointId jointId, bool enableMotor)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_revoluteJoint);
            if (enableMotor != joint.uj.revoluteJoint.enableMotor)
            {
                joint.uj.revoluteJoint.enableMotor = enableMotor;
                joint.uj.revoluteJoint.motorImpulse = 0.0f;
            }
        }

        public static bool b2RevoluteJoint_IsMotorEnabled(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_revoluteJoint);
            return joint.uj.revoluteJoint.enableMotor;
        }

        public static void b2RevoluteJoint_SetMotorSpeed(B2JointId jointId, float motorSpeed)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_revoluteJoint);
            joint.uj.revoluteJoint.motorSpeed = motorSpeed;
        }

        public static float b2RevoluteJoint_GetMotorSpeed(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_revoluteJoint);
            return joint.uj.revoluteJoint.motorSpeed;
        }

        public static float b2RevoluteJoint_GetMotorTorque(B2JointId jointId)
        {
            B2World world = b2GetWorld(jointId.world0);
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_revoluteJoint);
            return world.inv_h * joint.uj.revoluteJoint.motorImpulse;
        }

        public static void b2RevoluteJoint_SetMaxMotorTorque(B2JointId jointId, float torque)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_revoluteJoint);
            joint.uj.revoluteJoint.maxMotorTorque = torque;
        }

        public static float b2RevoluteJoint_GetMaxMotorTorque(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_revoluteJoint);
            return joint.uj.revoluteJoint.maxMotorTorque;
        }

        public static B2Vec2 b2GetRevoluteJointForce(B2World world, B2JointSim @base)
        {
            B2Vec2 force = b2MulSV(world.inv_h, @base.uj.revoluteJoint.linearImpulse);
            return force;
        }

        public static float b2GetRevoluteJointTorque(B2World world, B2JointSim @base)
        {
            ref readonly B2RevoluteJoint revolute = ref @base.uj.revoluteJoint;
            float torque = world.inv_h * (revolute.motorImpulse + revolute.lowerImpulse - revolute.upperImpulse);
            return torque;
        }

        // Point-to-point constraint
        // C = p2 - p1
        // Cdot = v2 - v1
        //      = v2 + cross(w2, r2) - v1 - cross(w1, r1)
        // J = [-I -r1_skew I r2_skew ]
        // Identity used:
        // w k % (rx i + ry j) = w * (-ry i + rx j)

        // Motor constraint
        // Cdot = w2 - w1
        // J = [0 0 -1 0 0 1]
        // K = invI1 + invI2

        public static void b2PrepareRevoluteJoint(B2JointSim @base, B2StepContext context)
        {
            B2_ASSERT(@base.type == B2JointType.b2_revoluteJoint);

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

            ref B2RevoluteJoint joint = ref @base.uj.revoluteJoint;

            joint.indexA = bodyA.setIndex == (int)B2SetType.b2_awakeSet ? localIndexA : B2_NULL_INDEX;
            joint.indexB = bodyB.setIndex == (int)B2SetType.b2_awakeSet ? localIndexB : B2_NULL_INDEX;

            // Compute joint anchor frames with world space rotation, relative to center of mass.
            // Avoid round-off here as much as possible.
            // b2Vec2 pf = (xf.p - c) + rot(xf.q, f.p)
            // pf = xf.p - (xf.p + rot(xf.q, lc)) + rot(xf.q, f.p)
            // pf = rot(xf.q, f.p - lc)
            joint.frameA.q = b2MulRot(bodySimA.transform.q, @base.localFrameA.q);
            joint.frameA.p = b2RotateVector(bodySimA.transform.q, b2Sub(@base.localFrameA.p, bodySimA.localCenter));
            joint.frameB.q = b2MulRot(bodySimB.transform.q, @base.localFrameB.q);
            joint.frameB.p = b2RotateVector(bodySimB.transform.q, b2Sub(@base.localFrameB.p, bodySimB.localCenter));

            // Compute the initial center delta. Incremental position updates are relative to this.
            joint.deltaCenter = b2Sub(bodySimB.center, bodySimA.center);

            float k = iA + iB;
            joint.axialMass = k > 0.0f ? 1.0f / k : 0.0f;

            joint.springSoftness = b2MakeSoft(joint.hertz, joint.dampingRatio, context.h);

            if (context.enableWarmStarting == false)
            {
                joint.linearImpulse = b2Vec2_zero;
                joint.springImpulse = 0.0f;
                joint.motorImpulse = 0.0f;
                joint.lowerImpulse = 0.0f;
                joint.upperImpulse = 0.0f;
            }
        }

        public static void b2WarmStartRevoluteJoint(B2JointSim @base, B2StepContext context)
        {
            B2_ASSERT(@base.type == B2JointType.b2_revoluteJoint);

            float mA = @base.invMassA;
            float mB = @base.invMassB;
            float iA = @base.invIA;
            float iB = @base.invIB;

            // dummy state for static bodies
            B2BodyState dummyState = B2BodyState.Create(b2_identityBodyState);

            ref readonly B2RevoluteJoint joint = ref @base.uj.revoluteJoint;
            B2BodyState stateA = joint.indexA == B2_NULL_INDEX ? dummyState : context.states[joint.indexA];
            B2BodyState stateB = joint.indexB == B2_NULL_INDEX ? dummyState : context.states[joint.indexB];

            B2Vec2 rA = b2RotateVector(stateA.deltaRotation, joint.frameA.p);
            B2Vec2 rB = b2RotateVector(stateB.deltaRotation, joint.frameB.p);

            float axialImpulse = joint.springImpulse + joint.motorImpulse + joint.lowerImpulse - joint.upperImpulse;

            stateA.linearVelocity = b2MulSub(stateA.linearVelocity, mA, joint.linearImpulse);
            stateA.angularVelocity -= iA * (b2Cross(rA, joint.linearImpulse) + axialImpulse);

            stateB.linearVelocity = b2MulAdd(stateB.linearVelocity, mB, joint.linearImpulse);
            stateB.angularVelocity += iB * (b2Cross(rB, joint.linearImpulse) + axialImpulse);
        }

        public static void b2SolveRevoluteJoint(B2JointSim @base, B2StepContext context, bool useBias)
        {
            B2_ASSERT(@base.type == B2JointType.b2_revoluteJoint);

            float mA = @base.invMassA;
            float mB = @base.invMassB;
            float iA = @base.invIA;
            float iB = @base.invIB;

            // dummy state for static bodies
            B2BodyState dummyState = B2BodyState.Create(b2_identityBodyState);

            ref B2RevoluteJoint joint = ref @base.uj.revoluteJoint;

            B2BodyState stateA = joint.indexA == B2_NULL_INDEX ? dummyState : context.states[joint.indexA];
            B2BodyState stateB = joint.indexB == B2_NULL_INDEX ? dummyState : context.states[joint.indexB];

            B2Vec2 vA = stateA.linearVelocity;
            float wA = stateA.angularVelocity;
            B2Vec2 vB = stateB.linearVelocity;
            float wB = stateB.angularVelocity;

            B2Rot qA = b2MulRot(stateA.deltaRotation, joint.frameA.q);
            B2Rot qB = b2MulRot(stateB.deltaRotation, joint.frameB.q);
            B2Rot relQ = b2InvMulRot(qA, qB);

            bool fixedRotation = (iA + iB == 0.0f);

            // Solve spring.
            if (joint.enableSpring && fixedRotation == false)
            {
                float jointAngle = b2Rot_GetAngle(relQ);
                float jointAngleDelta = b2UnwindAngle(jointAngle - joint.targetAngle);

                float C = jointAngleDelta;
                float bias = joint.springSoftness.biasRate * C;
                float massScale = joint.springSoftness.massScale;
                float impulseScale = joint.springSoftness.impulseScale;

                float Cdot = wB - wA;
                float impulse = -massScale * joint.axialMass * (Cdot + bias) - impulseScale * joint.springImpulse;
                joint.springImpulse += impulse;

                wA -= iA * impulse;
                wB += iB * impulse;
            }

            // Solve motor constraint.
            if (joint.enableMotor && fixedRotation == false)
            {
                float Cdot = wB - wA - joint.motorSpeed;
                float impulse = -joint.axialMass * Cdot;
                float oldImpulse = joint.motorImpulse;
                float maxImpulse = context.h * joint.maxMotorTorque;
                joint.motorImpulse = b2ClampFloat(joint.motorImpulse + impulse, -maxImpulse, maxImpulse);
                impulse = joint.motorImpulse - oldImpulse;

                wA -= iA * impulse;
                wB += iB * impulse;
            }

            if (joint.enableLimit && fixedRotation == false)
            {
                float jointAngle = b2Rot_GetAngle(relQ);

                // Lower limit
                {
                    float C = jointAngle - joint.lowerAngle;
                    float bias = 0.0f;
                    float massScale = 1.0f;
                    float impulseScale = 0.0f;
                    if (C > 0.0f)
                    {
                        // speculation
                        bias = C * context.inv_h;
                    }
                    else if (useBias)
                    {
                        bias = @base.constraintSoftness.biasRate * C;
                        massScale = @base.constraintSoftness.massScale;
                        impulseScale = @base.constraintSoftness.impulseScale;
                    }

                    float Cdot = wB - wA;
                    float oldImpulse = joint.lowerImpulse;
                    float impulse = -massScale * joint.axialMass * (Cdot + bias) - impulseScale * oldImpulse;
                    joint.lowerImpulse = b2MaxFloat(oldImpulse + impulse, 0.0f);
                    impulse = joint.lowerImpulse - oldImpulse;

                    wA -= iA * impulse;
                    wB += iB * impulse;
                }

                // Upper limit
                // Note: signs are flipped to keep C positive when the constraint is satisfied.
                // This also keeps the impulse positive when the limit is active.
                {
                    float C = joint.upperAngle - jointAngle;
                    float bias = 0.0f;
                    float massScale = 1.0f;
                    float impulseScale = 0.0f;
                    if (C > 0.0f)
                    {
                        // speculation
                        bias = C * context.inv_h;
                    }
                    else if (useBias)
                    {
                        bias = @base.constraintSoftness.biasRate * C;
                        massScale = @base.constraintSoftness.massScale;
                        impulseScale = @base.constraintSoftness.impulseScale;
                    }

                    // sign flipped on Cdot
                    float Cdot = wA - wB;
                    float oldImpulse = joint.upperImpulse;
                    float impulse = -massScale * joint.axialMass * (Cdot + bias) - impulseScale * oldImpulse;
                    joint.upperImpulse = b2MaxFloat(oldImpulse + impulse, 0.0f);
                    impulse = joint.upperImpulse - oldImpulse;

                    // sign flipped on applied impulse
                    wA += iA * impulse;
                    wB -= iB * impulse;
                }
            }

            // Solve point-to-point constraint
            {
                // J = [-I -r1_skew I r2_skew]
                // r_skew = [-ry; rx]
                // K = [ mA+r1y^2*iA+mB+r2y^2*iB,  -r1y*iA*r1x-r2y*iB*r2x]
                //     [  -r1y*iA*r1x-r2y*iB*r2x, mA+r1x^2*iA+mB+r2x^2*iB]

                // current anchors
                B2Vec2 rA = b2RotateVector(stateA.deltaRotation, joint.frameA.p);
                B2Vec2 rB = b2RotateVector(stateB.deltaRotation, joint.frameB.p);

                B2Vec2 Cdot = b2Sub(b2Add(vB, b2CrossSV(wB, rB)), b2Add(vA, b2CrossSV(wA, rA)));

                B2Vec2 bias = b2Vec2_zero;
                float massScale = 1.0f;
                float impulseScale = 0.0f;
                if (useBias)
                {
                    B2Vec2 dcA = stateA.deltaPosition;
                    B2Vec2 dcB = stateB.deltaPosition;

                    B2Vec2 separation = b2Add(b2Add(b2Sub(dcB, dcA), b2Sub(rB, rA)), joint.deltaCenter);
                    bias = b2MulSV(@base.constraintSoftness.biasRate, separation);
                    massScale = @base.constraintSoftness.massScale;
                    impulseScale = @base.constraintSoftness.impulseScale;
                }

                B2Mat22 K;
                K.cx.X = mA + mB + rA.Y * rA.Y * iA + rB.Y * rB.Y * iB;
                K.cy.X = -rA.Y * rA.X * iA - rB.Y * rB.X * iB;
                K.cx.Y = K.cy.X;
                K.cy.Y = mA + mB + rA.X * rA.X * iA + rB.X * rB.X * iB;
                B2Vec2 b = b2Solve22(K, b2Add(Cdot, bias));

                B2Vec2 impulse;
                impulse.X = -massScale * b.X - impulseScale * joint.linearImpulse.X;
                impulse.Y = -massScale * b.Y - impulseScale * joint.linearImpulse.Y;
                joint.linearImpulse.X += impulse.X;
                joint.linearImpulse.Y += impulse.Y;

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
    void b2RevoluteJoint::Dump()
    {
        int32 indexA = joint.bodyA.joint.islandIndex;
        int32 indexB = joint.bodyB.joint.islandIndex;

        b2Dump("  b2RevoluteJointDef jd;\n");
        b2Dump("  jd.bodyA = bodies[%d];\n", indexA);
        b2Dump("  jd.bodyB = bodies[%d];\n", indexB);
        b2Dump("  jd.collideConnected = bool(%d);\n", joint.collideConnected);
        b2Dump("  jd.localAnchorA.Set(%.9g, %.9g);\n", joint.localAnchorA.x, joint.localAnchorA.y);
        b2Dump("  jd.localAnchorB.Set(%.9g, %.9g);\n", joint.localAnchorB.x, joint.localAnchorB.y);
        b2Dump("  jd.referenceAngle = %.9g;\n", joint.referenceAngle);
        b2Dump("  jd.enableLimit = bool(%d);\n", joint.enableLimit);
        b2Dump("  jd.lowerAngle = %.9g;\n", joint.lowerAngle);
        b2Dump("  jd.upperAngle = %.9g;\n", joint.upperAngle);
        b2Dump("  jd.enableMotor = bool(%d);\n", joint.enableMotor);
        b2Dump("  jd.motorSpeed = %.9g;\n", joint.motorSpeed);
        b2Dump("  jd.maxMotorTorque = %.9g;\n", joint.maxMotorTorque);
        b2Dump("  joints[%d] = joint.world.CreateJoint(&jd);\n", joint.index);
    }
#endif

        public static void b2DrawRevoluteJoint(B2DebugDraw draw, B2JointSim @base, B2Transform transformA, B2Transform transformB, float drawSize)
        {
            B2_ASSERT(@base.type == B2JointType.b2_revoluteJoint);

            ref readonly B2RevoluteJoint joint = ref @base.uj.revoluteJoint;

            B2Transform frameA = b2MulTransforms(transformA, @base.localFrameA);
            B2Transform frameB = b2MulTransforms(transformB, @base.localFrameB);

            float radius = 0.25f * drawSize;
            draw.DrawCircleFcn(frameB.p, radius, B2HexColor.b2_colorGray, draw.context);

            B2Vec2 rx = new B2Vec2(radius, 0.0f);
            B2Vec2 r = b2RotateVector(frameA.q, rx);
            draw.DrawSegmentFcn(frameA.p, b2Add(frameA.p, r), B2HexColor.b2_colorGray, draw.context);

            r = b2RotateVector(frameB.q, rx);
            draw.DrawSegmentFcn(frameB.p, b2Add(frameB.p, r), B2HexColor.b2_colorBlue, draw.context);

            if (draw.drawJointExtras)
            {
                float jointAngle = b2RelativeAngle(frameA.q, frameB.q);
                string buffer = $"{(180.0f * jointAngle / B2_PI):F1} deg";
                draw.DrawStringFcn(b2Add(frameA.p, r), buffer, B2HexColor.b2_colorWhite, draw.context);
            }

            float lowerAngle = joint.lowerAngle;
            float upperAngle = joint.upperAngle;

            if (joint.enableLimit)
            {
                B2Rot rotLo = b2MulRot(frameA.q, b2MakeRot(lowerAngle));
                B2Vec2 rlo = b2RotateVector(rotLo, rx);

                B2Rot rotHi = b2MulRot(frameA.q, b2MakeRot(upperAngle));
                B2Vec2 rhi = b2RotateVector(rotHi, rx);

                draw.DrawSegmentFcn(frameB.p, b2Add(frameB.p, rlo), B2HexColor.b2_colorGreen, draw.context);
                draw.DrawSegmentFcn(frameB.p, b2Add(frameB.p, rhi), B2HexColor.b2_colorRed, draw.context);
            }

            if (joint.enableSpring)
            {
                B2Rot q = b2MulRot(frameA.q, b2MakeRot(joint.targetAngle));
                B2Vec2 v = b2RotateVector(q, rx);
                draw.DrawSegmentFcn(frameB.p, b2Add(frameB.p, v), B2HexColor.b2_colorViolet, draw.context);
            }

            B2HexColor color = B2HexColor.b2_colorGold;
            draw.DrawSegmentFcn(transformA.p, frameA.p, color, draw.context);
            draw.DrawSegmentFcn(frameA.p, frameB.p, color, draw.context);
            draw.DrawSegmentFcn(transformB.p, frameB.p, color, draw.context);

            // char buffer[32];
            // sprintf(buffer, "%.1f", b2Length(joint->impulse));
            // draw->DrawString(pA, buffer, draw->context);
        }
    }
}