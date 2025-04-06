// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Solvers;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Joints;

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

        public static float b2RevoluteJoint_GetAngle(B2JointId jointId)
        {
            B2World world = b2GetWorld(jointId.world0);
            B2JointSim jointSim = b2GetJointSimCheckType(jointId, B2JointType.b2_revoluteJoint);
            B2Transform transformA = b2GetBodyTransform(world, jointSim.bodyIdA);
            B2Transform transformB = b2GetBodyTransform(world, jointSim.bodyIdB);

            float angle = b2RelativeAngle(transformB.q, transformA.q) - jointSim.uj.revoluteJoint.referenceAngle;
            angle = b2UnwindAngle(angle);
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

        public static void b2RevoluteJoint_SetLimits(B2JointId jointId, float lower, float upper)
        {
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
            Debug.Assert(@base.type == B2JointType.b2_revoluteJoint);

            // chase body id to the solver set where the body lives
            int idA = @base.bodyIdA;
            int idB = @base.bodyIdB;

            B2World world = context.world;

            B2Body bodyA = b2Array_Get(ref world.bodies, idA);
            B2Body bodyB = b2Array_Get(ref world.bodies, idB);

            Debug.Assert(bodyA.setIndex == (int)B2SetType.b2_awakeSet || bodyB.setIndex == (int)B2SetType.b2_awakeSet);
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

            // initial anchors in world space
            joint.anchorA = b2RotateVector(bodySimA.transform.q, b2Sub(@base.localOriginAnchorA, bodySimA.localCenter));
            joint.anchorB = b2RotateVector(bodySimB.transform.q, b2Sub(@base.localOriginAnchorB, bodySimB.localCenter));
            joint.deltaCenter = b2Sub(bodySimB.center, bodySimA.center);
            joint.deltaAngle = b2RelativeAngle(bodySimB.transform.q, bodySimA.transform.q) - joint.referenceAngle;
            joint.deltaAngle = b2UnwindAngle(joint.deltaAngle);

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
            Debug.Assert(@base.type == B2JointType.b2_revoluteJoint);

            float mA = @base.invMassA;
            float mB = @base.invMassB;
            float iA = @base.invIA;
            float iB = @base.invIB;

            // dummy state for static bodies
            B2BodyState dummyState = B2BodyState.Create(b2_identityBodyState);

            ref readonly B2RevoluteJoint joint = ref @base.uj.revoluteJoint;
            B2BodyState stateA = joint.indexA == B2_NULL_INDEX ? dummyState : context.states[joint.indexA];
            B2BodyState stateB = joint.indexB == B2_NULL_INDEX ? dummyState : context.states[joint.indexB];

            B2Vec2 rA = b2RotateVector(stateA.deltaRotation, joint.anchorA);
            B2Vec2 rB = b2RotateVector(stateB.deltaRotation, joint.anchorB);

            float axialImpulse = joint.springImpulse + joint.motorImpulse + joint.lowerImpulse - joint.upperImpulse;

            stateA.linearVelocity = b2MulSub(stateA.linearVelocity, mA, joint.linearImpulse);
            stateA.angularVelocity -= iA * (b2Cross(rA, joint.linearImpulse) + axialImpulse);

            stateB.linearVelocity = b2MulAdd(stateB.linearVelocity, mB, joint.linearImpulse);
            stateB.angularVelocity += iB * (b2Cross(rB, joint.linearImpulse) + axialImpulse);
        }

        public static void b2SolveRevoluteJoint(B2JointSim @base, B2StepContext context, bool useBias)
        {
            Debug.Assert(@base.type == B2JointType.b2_revoluteJoint);

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

            bool fixedRotation = (iA + iB == 0.0f);
            // const float maxBias = context.maxBiasVelocity;

            // Solve spring.
            if (joint.enableSpring && fixedRotation == false)
            {
                float C = b2RelativeAngle(stateB.deltaRotation, stateA.deltaRotation) + joint.deltaAngle;
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
                float jointAngle = b2RelativeAngle(stateB.deltaRotation, stateA.deltaRotation) + joint.deltaAngle;
                jointAngle = b2UnwindAngle(jointAngle);

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
                        bias = context.jointSoftness.biasRate * C;
                        massScale = context.jointSoftness.massScale;
                        impulseScale = context.jointSoftness.impulseScale;
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
                        bias = context.jointSoftness.biasRate * C;
                        massScale = context.jointSoftness.massScale;
                        impulseScale = context.jointSoftness.impulseScale;
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
                B2Vec2 rA = b2RotateVector(stateA.deltaRotation, joint.anchorA);
                B2Vec2 rB = b2RotateVector(stateB.deltaRotation, joint.anchorB);

                B2Vec2 Cdot = b2Sub(b2Add(vB, b2CrossSV(wB, rB)), b2Add(vA, b2CrossSV(wA, rA)));

                B2Vec2 bias = b2Vec2_zero;
                float massScale = 1.0f;
                float impulseScale = 0.0f;
                if (useBias)
                {
                    B2Vec2 dcA = stateA.deltaPosition;
                    B2Vec2 dcB = stateB.deltaPosition;

                    B2Vec2 separation = b2Add(b2Add(b2Sub(dcB, dcA), b2Sub(rB, rA)), joint.deltaCenter);
                    bias = b2MulSV(context.jointSoftness.biasRate, separation);
                    massScale = context.jointSoftness.massScale;
                    impulseScale = context.jointSoftness.impulseScale;
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
            Debug.Assert(@base.type == B2JointType.b2_revoluteJoint);

            ref readonly B2RevoluteJoint joint = ref @base.uj.revoluteJoint;

            B2Vec2 pA = b2TransformPoint(ref transformA, @base.localOriginAnchorA);
            B2Vec2 pB = b2TransformPoint(ref transformB, @base.localOriginAnchorB);

            B2HexColor c1 = B2HexColor.b2_colorGray;
            B2HexColor c2 = B2HexColor.b2_colorGreen;
            B2HexColor c3 = B2HexColor.b2_colorRed;

            float L = drawSize;
            // draw.drawPoint(pA, 3.0f, b2HexColor.b2_colorGray40, draw.context);
            // draw.drawPoint(pB, 3.0f, b2HexColor.b2_colorLightBlue, draw.context);
            draw.DrawCircleFcn(pB, L, c1, draw.context);

            float angle = b2RelativeAngle(transformB.q, transformA.q);

            B2Rot rot = b2MakeRot(angle);
            B2Vec2 r = new B2Vec2(L * rot.c, L * rot.s);
            B2Vec2 pC = b2Add(pB, r);
            draw.DrawSegmentFcn(pB, pC, c1, draw.context);

            if (draw.drawJointExtras)
            {
                float jointAngle = b2UnwindAngle(angle - joint.referenceAngle);
                string buffer = $" {180.0f * jointAngle / B2_PI} deg";
                draw.DrawStringFcn(pC, buffer, B2HexColor.b2_colorWhite, draw.context);
            }

            float lowerAngle = joint.lowerAngle + joint.referenceAngle;
            float upperAngle = joint.upperAngle + joint.referenceAngle;

            if (joint.enableLimit)
            {
                B2Rot rotLo = b2MakeRot(lowerAngle);
                B2Vec2 rlo = new B2Vec2(L * rotLo.c, L * rotLo.s);

                B2Rot rotHi = b2MakeRot(upperAngle);
                B2Vec2 rhi = new B2Vec2(L * rotHi.c, L * rotHi.s);

                draw.DrawSegmentFcn(pB, b2Add(pB, rlo), c2, draw.context);
                draw.DrawSegmentFcn(pB, b2Add(pB, rhi), c3, draw.context);

                B2Rot rotRef = b2MakeRot(joint.referenceAngle);
                B2Vec2 @ref = new B2Vec2(L * rotRef.c, L * rotRef.s);
                draw.DrawSegmentFcn(pB, b2Add(pB, @ref), B2HexColor.b2_colorBlue, draw.context);
            }

            B2HexColor color = B2HexColor.b2_colorGold;
            draw.DrawSegmentFcn(transformA.p, pA, color, draw.context);
            draw.DrawSegmentFcn(pA, pB, color, draw.context);
            draw.DrawSegmentFcn(transformB.p, pB, color, draw.context);

            // char buffer[32];
            // sprintf(buffer, "%.1f", b2Length(joint.impulse));
            // draw.DrawString(pA, buffer, draw.context);
        }
    }
}