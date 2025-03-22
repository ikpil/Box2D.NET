﻿// SPDX-FileCopyrightText: 2023 Erin Catto
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
    public static class B2WheelJoints
    {
        public static void b2WheelJoint_EnableSpring(B2JointId jointId, bool enableSpring)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_wheelJoint);

            if (enableSpring != joint.wheelJoint.enableSpring)
            {
                joint.wheelJoint.enableSpring = enableSpring;
                joint.wheelJoint.springImpulse = 0.0f;
            }
        }

        public static bool b2WheelJoint_IsSpringEnabled(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_wheelJoint);
            return joint.wheelJoint.enableSpring;
        }

        public static void b2WheelJoint_SetSpringHertz(B2JointId jointId, float hertz)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_wheelJoint);
            joint.wheelJoint.hertz = hertz;
        }

        public static float b2WheelJoint_GetSpringHertz(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_wheelJoint);
            return joint.wheelJoint.hertz;
        }

        public static void b2WheelJoint_SetSpringDampingRatio(B2JointId jointId, float dampingRatio)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_wheelJoint);
            joint.wheelJoint.dampingRatio = dampingRatio;
        }

        public static float b2WheelJoint_GetSpringDampingRatio(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_wheelJoint);
            return joint.wheelJoint.dampingRatio;
        }

        public static void b2WheelJoint_EnableLimit(B2JointId jointId, bool enableLimit)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_wheelJoint);
            if (joint.wheelJoint.enableLimit != enableLimit)
            {
                joint.wheelJoint.lowerImpulse = 0.0f;
                joint.wheelJoint.upperImpulse = 0.0f;
                joint.wheelJoint.enableLimit = enableLimit;
            }
        }

        public static bool b2WheelJoint_IsLimitEnabled(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_wheelJoint);
            return joint.wheelJoint.enableLimit;
        }

        public static float b2WheelJoint_GetLowerLimit(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_wheelJoint);
            return joint.wheelJoint.lowerTranslation;
        }

        public static float b2WheelJoint_GetUpperLimit(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_wheelJoint);
            return joint.wheelJoint.upperTranslation;
        }

        public static void b2WheelJoint_SetLimits(B2JointId jointId, float lower, float upper)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_wheelJoint);
            if (lower != joint.wheelJoint.lowerTranslation || upper != joint.wheelJoint.upperTranslation)
            {
                joint.wheelJoint.lowerTranslation = b2MinFloat(lower, upper);
                joint.wheelJoint.upperTranslation = b2MaxFloat(lower, upper);
                joint.wheelJoint.lowerImpulse = 0.0f;
                joint.wheelJoint.upperImpulse = 0.0f;
            }
        }

        public static void b2WheelJoint_EnableMotor(B2JointId jointId, bool enableMotor)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_wheelJoint);
            if (joint.wheelJoint.enableMotor != enableMotor)
            {
                joint.wheelJoint.motorImpulse = 0.0f;
                joint.wheelJoint.enableMotor = enableMotor;
            }
        }

        public static bool b2WheelJoint_IsMotorEnabled(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_wheelJoint);
            return joint.wheelJoint.enableMotor;
        }

        public static void b2WheelJoint_SetMotorSpeed(B2JointId jointId, float motorSpeed)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_wheelJoint);
            joint.wheelJoint.motorSpeed = motorSpeed;
        }

        public static float b2WheelJoint_GetMotorSpeed(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_wheelJoint);
            return joint.wheelJoint.motorSpeed;
        }

        public static float b2WheelJoint_GetMotorTorque(B2JointId jointId)
        {
            B2World world = b2GetWorld(jointId.world0);
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_wheelJoint);
            return world.inv_h * joint.wheelJoint.motorImpulse;
        }

        public static void b2WheelJoint_SetMaxMotorTorque(B2JointId jointId, float torque)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_wheelJoint);
            joint.wheelJoint.maxMotorTorque = torque;
        }

        public static float b2WheelJoint_GetMaxMotorTorque(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_wheelJoint);
            return joint.wheelJoint.maxMotorTorque;
        }

        public static B2Vec2 b2GetWheelJointForce(B2World world, B2JointSim @base)
        {
            ref readonly B2WheelJoint joint = ref @base.wheelJoint;

            // This is a frame behind
            B2Vec2 axisA = joint.axisA;
            B2Vec2 perpA = b2LeftPerp(axisA);

            float perpForce = world.inv_h * joint.perpImpulse;
            float axialForce = world.inv_h * (joint.springImpulse + joint.lowerImpulse - joint.upperImpulse);

            B2Vec2 force = b2Add(b2MulSV(perpForce, perpA), b2MulSV(axialForce, axisA));
            return force;
        }

        public static float b2GetWheelJointTorque(B2World world, B2JointSim @base)
        {
            return world.inv_h * @base.wheelJoint.motorImpulse;
        }

        // Linear constraint (point-to-line)
        // d = pB - pA = xB + rB - xA - rA
        // C = dot(ay, d)
        // Cdot = dot(d, cross(wA, ay)) + dot(ay, vB + cross(wB, rB) - vA - cross(wA, rA))
        //      = -dot(ay, vA) - dot(cross(d + rA, ay), wA) + dot(ay, vB) + dot(cross(rB, ay), vB)
        // J = [-ay, -cross(d + rA, ay), ay, cross(rB, ay)]

        // Spring linear constraint
        // C = dot(ax, d)
        // Cdot = = -dot(ax, vA) - dot(cross(d + rA, ax), wA) + dot(ax, vB) + dot(cross(rB, ax), vB)
        // J = [-ax -cross(d+rA, ax) ax cross(rB, ax)]

        // Motor rotational constraint
        // Cdot = wB - wA
        // J = [0 0 -1 0 0 1]

        public static void b2PrepareWheelJoint(B2JointSim @base, B2StepContext context)
        {
            Debug.Assert(@base.type == B2JointType.b2_wheelJoint);

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

            ref B2WheelJoint joint = ref @base.wheelJoint;

            joint.indexA = bodyA.setIndex == (int)B2SetType.b2_awakeSet ? localIndexA : B2_NULL_INDEX;
            joint.indexB = bodyB.setIndex == (int)B2SetType.b2_awakeSet ? localIndexB : B2_NULL_INDEX;

            B2Rot qA = bodySimA.transform.q;
            B2Rot qB = bodySimB.transform.q;

            joint.anchorA = b2RotateVector(qA, b2Sub(@base.localOriginAnchorA, bodySimA.localCenter));
            joint.anchorB = b2RotateVector(qB, b2Sub(@base.localOriginAnchorB, bodySimB.localCenter));
            joint.axisA = b2RotateVector(qA, joint.localAxisA);
            joint.deltaCenter = b2Sub(bodySimB.center, bodySimA.center);

            B2Vec2 rA = joint.anchorA;
            B2Vec2 rB = joint.anchorB;

            B2Vec2 d = b2Add(joint.deltaCenter, b2Sub(rB, rA));
            B2Vec2 axisA = joint.axisA;
            B2Vec2 perpA = b2LeftPerp(axisA);

            // perpendicular constraint (keep wheel on line)
            float s1 = b2Cross(b2Add(d, rA), perpA);
            float s2 = b2Cross(rB, perpA);

            float kp = mA + mB + iA * s1 * s1 + iB * s2 * s2;
            joint.perpMass = kp > 0.0f ? 1.0f / kp : 0.0f;

            // spring constraint
            float a1 = b2Cross(b2Add(d, rA), axisA);
            float a2 = b2Cross(rB, axisA);

            float ka = mA + mB + iA * a1 * a1 + iB * a2 * a2;
            joint.axialMass = ka > 0.0f ? 1.0f / ka : 0.0f;

            joint.springSoftness = b2MakeSoft(joint.hertz, joint.dampingRatio, context.h);

            float km = iA + iB;
            joint.motorMass = km > 0.0f ? 1.0f / km : 0.0f;

            if (context.enableWarmStarting == false)
            {
                joint.perpImpulse = 0.0f;
                joint.springImpulse = 0.0f;
                joint.motorImpulse = 0.0f;
                joint.lowerImpulse = 0.0f;
                joint.upperImpulse = 0.0f;
            }
        }

        public static void b2WarmStartWheelJoint(B2JointSim @base, B2StepContext context)
        {
            Debug.Assert(@base.type == B2JointType.b2_wheelJoint);

            float mA = @base.invMassA;
            float mB = @base.invMassB;
            float iA = @base.invIA;
            float iB = @base.invIB;

            // dummy state for static bodies
            B2BodyState dummyState = B2BodyState.Create(b2_identityBodyState);

            ref readonly B2WheelJoint joint = ref @base.wheelJoint;

            B2BodyState stateA = joint.indexA == B2_NULL_INDEX ? dummyState : context.states[joint.indexA];
            B2BodyState stateB = joint.indexB == B2_NULL_INDEX ? dummyState : context.states[joint.indexB];

            B2Vec2 rA = b2RotateVector(stateA.deltaRotation, joint.anchorA);
            B2Vec2 rB = b2RotateVector(stateB.deltaRotation, joint.anchorB);

            B2Vec2 d = b2Add(b2Add(b2Sub(stateB.deltaPosition, stateA.deltaPosition), joint.deltaCenter), b2Sub(rB, rA));
            B2Vec2 axisA = b2RotateVector(stateA.deltaRotation, joint.axisA);
            B2Vec2 perpA = b2LeftPerp(axisA);

            float a1 = b2Cross(b2Add(d, rA), axisA);
            float a2 = b2Cross(rB, axisA);
            float s1 = b2Cross(b2Add(d, rA), perpA);
            float s2 = b2Cross(rB, perpA);

            float axialImpulse = joint.springImpulse + joint.lowerImpulse - joint.upperImpulse;

            B2Vec2 P = b2Add(b2MulSV(axialImpulse, axisA), b2MulSV(joint.perpImpulse, perpA));
            float LA = axialImpulse * a1 + joint.perpImpulse * s1 + joint.motorImpulse;
            float LB = axialImpulse * a2 + joint.perpImpulse * s2 + joint.motorImpulse;

            stateA.linearVelocity = b2MulSub(stateA.linearVelocity, mA, P);
            stateA.angularVelocity -= iA * LA;
            stateB.linearVelocity = b2MulAdd(stateB.linearVelocity, mB, P);
            stateB.angularVelocity += iB * LB;
        }

        public static void b2SolveWheelJoint(B2JointSim @base, B2StepContext context, bool useBias)
        {
            Debug.Assert(@base.type == B2JointType.b2_wheelJoint);

            float mA = @base.invMassA;
            float mB = @base.invMassB;
            float iA = @base.invIA;
            float iB = @base.invIB;

            // dummy state for static bodies
            B2BodyState dummyState = B2BodyState.Create(b2_identityBodyState);

            ref B2WheelJoint joint = ref @base.wheelJoint;

            B2BodyState stateA = joint.indexA == B2_NULL_INDEX ? dummyState : context.states[joint.indexA];
            B2BodyState stateB = joint.indexB == B2_NULL_INDEX ? dummyState : context.states[joint.indexB];

            B2Vec2 vA = stateA.linearVelocity;
            float wA = stateA.angularVelocity;
            B2Vec2 vB = stateB.linearVelocity;
            float wB = stateB.angularVelocity;

            bool fixedRotation = (iA + iB == 0.0f);

            // current anchors
            B2Vec2 rA = b2RotateVector(stateA.deltaRotation, joint.anchorA);
            B2Vec2 rB = b2RotateVector(stateB.deltaRotation, joint.anchorB);

            B2Vec2 d = b2Add(b2Add(b2Sub(stateB.deltaPosition, stateA.deltaPosition), joint.deltaCenter), b2Sub(rB, rA));
            B2Vec2 axisA = b2RotateVector(stateA.deltaRotation, joint.axisA);
            float translation = b2Dot(axisA, d);

            float a1 = b2Cross(b2Add(d, rA), axisA);
            float a2 = b2Cross(rB, axisA);

            // motor constraint
            if (joint.enableMotor && fixedRotation == false)
            {
                float Cdot = wB - wA - joint.motorSpeed;
                float impulse = -joint.motorMass * Cdot;
                float oldImpulse = joint.motorImpulse;
                float maxImpulse = context.h * joint.maxMotorTorque;
                joint.motorImpulse = b2ClampFloat(joint.motorImpulse + impulse, -maxImpulse, maxImpulse);
                impulse = joint.motorImpulse - oldImpulse;

                wA -= iA * impulse;
                wB += iB * impulse;
            }

            // spring constraint
            if (joint.enableSpring)
            {
                // This is a real spring and should be applied even during relax
                float C = translation;
                float bias = joint.springSoftness.biasRate * C;
                float massScale = joint.springSoftness.massScale;
                float impulseScale = joint.springSoftness.impulseScale;

                float Cdot = b2Dot(axisA, b2Sub(vB, vA)) + a2 * wB - a1 * wA;
                float impulse = -massScale * joint.axialMass * (Cdot + bias) - impulseScale * joint.springImpulse;
                joint.springImpulse += impulse;

                B2Vec2 P = b2MulSV(impulse, axisA);
                float LA = impulse * a1;
                float LB = impulse * a2;

                vA = b2MulSub(vA, mA, P);
                wA -= iA * LA;
                vB = b2MulAdd(vB, mB, P);
                wB += iB * LB;
            }

            if (joint.enableLimit)
            {
                // Lower limit
                {
                    float C = translation - joint.lowerTranslation;
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

                    float Cdot = b2Dot(axisA, b2Sub(vB, vA)) + a2 * wB - a1 * wA;
                    float impulse = -massScale * joint.axialMass * (Cdot + bias) - impulseScale * joint.lowerImpulse;
                    float oldImpulse = joint.lowerImpulse;
                    joint.lowerImpulse = b2MaxFloat(oldImpulse + impulse, 0.0f);
                    impulse = joint.lowerImpulse - oldImpulse;

                    B2Vec2 P = b2MulSV(impulse, axisA);
                    float LA = impulse * a1;
                    float LB = impulse * a2;

                    vA = b2MulSub(vA, mA, P);
                    wA -= iA * LA;
                    vB = b2MulAdd(vB, mB, P);
                    wB += iB * LB;
                }

                // Upper limit
                // Note: signs are flipped to keep C positive when the constraint is satisfied.
                // This also keeps the impulse positive when the limit is active.
                {
                    // sign flipped
                    float C = joint.upperTranslation - translation;
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
                    float Cdot = b2Dot(axisA, b2Sub(vA, vB)) + a1 * wA - a2 * wB;
                    float impulse = -massScale * joint.axialMass * (Cdot + bias) - impulseScale * joint.upperImpulse;
                    float oldImpulse = joint.upperImpulse;
                    joint.upperImpulse = b2MaxFloat(oldImpulse + impulse, 0.0f);
                    impulse = joint.upperImpulse - oldImpulse;

                    B2Vec2 P = b2MulSV(impulse, axisA);
                    float LA = impulse * a1;
                    float LB = impulse * a2;

                    // sign flipped on applied impulse
                    vA = b2MulAdd(vA, mA, P);
                    wA += iA * LA;
                    vB = b2MulSub(vB, mB, P);
                    wB -= iB * LB;
                }
            }

            // point to line constraint
            {
                B2Vec2 perpA = b2LeftPerp(axisA);

                float bias = 0.0f;
                float massScale = 1.0f;
                float impulseScale = 0.0f;
                if (useBias)
                {
                    float C = b2Dot(perpA, d);
                    bias = context.jointSoftness.biasRate * C;
                    massScale = context.jointSoftness.massScale;
                    impulseScale = context.jointSoftness.impulseScale;
                }

                float s1 = b2Cross(b2Add(d, rA), perpA);
                float s2 = b2Cross(rB, perpA);
                float Cdot = b2Dot(perpA, b2Sub(vB, vA)) + s2 * wB - s1 * wA;

                float impulse = -massScale * joint.perpMass * (Cdot + bias) - impulseScale * joint.perpImpulse;
                joint.perpImpulse += impulse;

                B2Vec2 P = b2MulSV(impulse, perpA);
                float LA = impulse * s1;
                float LB = impulse * s2;

                vA = b2MulSub(vA, mA, P);
                wA -= iA * LA;
                vB = b2MulAdd(vB, mB, P);
                wB += iB * LB;
            }

            stateA.linearVelocity = vA;
            stateA.angularVelocity = wA;
            stateB.linearVelocity = vB;
            stateB.angularVelocity = wB;
        }

#if FALSE
    void b2WheelJoint_Dump()
    {
        int32 indexA = joint.bodyA.joint.islandIndex;
        int32 indexB = joint.bodyB.joint.islandIndex;

        b2Dump("  b2WheelJointDef jd;\n");
        b2Dump("  jd.bodyA = sims[%d];\n", indexA);
        b2Dump("  jd.bodyB = sims[%d];\n", indexB);
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

        public static void b2DrawWheelJoint(B2DebugDraw draw, B2JointSim @base, B2Transform transformA, B2Transform transformB)
        {
            Debug.Assert(@base.type == B2JointType.b2_wheelJoint);

            ref readonly B2WheelJoint joint = ref @base.wheelJoint;

            B2Vec2 pA = b2TransformPoint(ref transformA, @base.localOriginAnchorA);
            B2Vec2 pB = b2TransformPoint(ref transformB, @base.localOriginAnchorB);
            B2Vec2 axis = b2RotateVector(transformA.q, joint.localAxisA);

            B2HexColor c1 = B2HexColor.b2_colorGray;
            B2HexColor c2 = B2HexColor.b2_colorGreen;
            B2HexColor c3 = B2HexColor.b2_colorRed;
            B2HexColor c4 = B2HexColor.b2_colorDimGray;
            B2HexColor c5 = B2HexColor.b2_colorBlue;

            draw.DrawSegment(pA, pB, c5, draw.context);

            if (joint.enableLimit)
            {
                B2Vec2 lower = b2MulAdd(pA, joint.lowerTranslation, axis);
                B2Vec2 upper = b2MulAdd(pA, joint.upperTranslation, axis);
                B2Vec2 perp = b2LeftPerp(axis);
                draw.DrawSegment(lower, upper, c1, draw.context);
                draw.DrawSegment(b2MulSub(lower, 0.1f, perp), b2MulAdd(lower, 0.1f, perp), c2, draw.context);
                draw.DrawSegment(b2MulSub(upper, 0.1f, perp), b2MulAdd(upper, 0.1f, perp), c3, draw.context);
            }
            else
            {
                draw.DrawSegment(b2MulSub(pA, 1.0f, axis), b2MulAdd(pA, 1.0f, axis), c1, draw.context);
            }

            draw.DrawPoint(pA, 5.0f, c1, draw.context);
            draw.DrawPoint(pB, 5.0f, c4, draw.context);
        }
    }
}
