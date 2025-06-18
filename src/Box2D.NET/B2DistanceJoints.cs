// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Cores;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Solvers;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Joints;


namespace Box2D.NET
{
    public static class B2DistanceJoints
    {
        public static void b2DistanceJoint_SetLength(B2JointId jointId, float length)
        {
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_distanceJoint);
            ref B2DistanceJoint joint = ref @base.uj.distanceJoint;

            joint.length = b2ClampFloat(length, B2_LINEAR_SLOP, B2_HUGE);
            joint.impulse = 0.0f;
            joint.lowerImpulse = 0.0f;
            joint.upperImpulse = 0.0f;
        }

        public static float b2DistanceJoint_GetLength(B2JointId jointId)
        {
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_distanceJoint);
            ref B2DistanceJoint joint = ref @base.uj.distanceJoint;
            return joint.length;
        }

        public static void b2DistanceJoint_EnableLimit(B2JointId jointId, bool enableLimit)
        {
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_distanceJoint);
            ref B2DistanceJoint joint = ref @base.uj.distanceJoint;
            joint.enableLimit = enableLimit;
        }

        public static bool b2DistanceJoint_IsLimitEnabled(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_distanceJoint);
            return joint.uj.distanceJoint.enableLimit;
        }

        public static void b2DistanceJoint_SetLengthRange(B2JointId jointId, float minLength, float maxLength)
        {
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_distanceJoint);
            ref B2DistanceJoint joint = ref @base.uj.distanceJoint;

            minLength = b2ClampFloat(minLength, B2_LINEAR_SLOP, B2_HUGE);
            maxLength = b2ClampFloat(maxLength, B2_LINEAR_SLOP, B2_HUGE);
            joint.minLength = b2MinFloat(minLength, maxLength);
            joint.maxLength = b2MaxFloat(minLength, maxLength);
            joint.impulse = 0.0f;
            joint.lowerImpulse = 0.0f;
            joint.upperImpulse = 0.0f;
        }

        public static float b2DistanceJoint_GetMinLength(B2JointId jointId)
        {
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_distanceJoint);
            ref B2DistanceJoint joint = ref @base.uj.distanceJoint;
            return joint.minLength;
        }

        public static float b2DistanceJoint_GetMaxLength(B2JointId jointId)
        {
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_distanceJoint);
            ref B2DistanceJoint joint = ref @base.uj.distanceJoint;
            return joint.maxLength;
        }

        public static float b2DistanceJoint_GetCurrentLength(B2JointId jointId)
        {
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_distanceJoint);

            B2World world = b2GetWorld(jointId.world0);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return 0.0f;
            }

            B2Transform transformA = b2GetBodyTransform(world, @base.bodyIdA);
            B2Transform transformB = b2GetBodyTransform(world, @base.bodyIdB);

            B2Vec2 pA = b2TransformPoint(ref transformA, @base.localFrameA.p);
            B2Vec2 pB = b2TransformPoint(ref transformB, @base.localFrameB.p);
            B2Vec2 d = b2Sub(pB, pA);
            float length = b2Length(d);
            return length;
        }

        public static void b2DistanceJoint_EnableSpring(B2JointId jointId, bool enableSpring)
        {
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_distanceJoint);
            @base.uj.distanceJoint.enableSpring = enableSpring;
        }

        public static bool b2DistanceJoint_IsSpringEnabled(B2JointId jointId)
        {
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_distanceJoint);
            return @base.uj.distanceJoint.enableSpring;
        }

        public static void b2DistanceJoint_SetSpringHertz(B2JointId jointId, float hertz)
        {
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_distanceJoint);
            @base.uj.distanceJoint.hertz = hertz;
        }

        public static void b2DistanceJoint_SetSpringDampingRatio(B2JointId jointId, float dampingRatio)
        {
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_distanceJoint);
            @base.uj.distanceJoint.dampingRatio = dampingRatio;
        }

        public static float b2DistanceJoint_GetSpringHertz(B2JointId jointId)
        {
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_distanceJoint);
            ref B2DistanceJoint joint = ref @base.uj.distanceJoint;
            return joint.hertz;
        }

        public static float b2DistanceJoint_GetSpringDampingRatio(B2JointId jointId)
        {
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_distanceJoint);
            ref B2DistanceJoint joint = ref @base.uj.distanceJoint;
            return joint.dampingRatio;
        }

        public static void b2DistanceJoint_EnableMotor(B2JointId jointId, bool enableMotor)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_distanceJoint);
            if (enableMotor != joint.uj.distanceJoint.enableMotor)
            {
                joint.uj.distanceJoint.enableMotor = enableMotor;
                joint.uj.distanceJoint.motorImpulse = 0.0f;
            }
        }

        public static bool b2DistanceJoint_IsMotorEnabled(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_distanceJoint);
            return joint.uj.distanceJoint.enableMotor;
        }

        public static void b2DistanceJoint_SetMotorSpeed(B2JointId jointId, float motorSpeed)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_distanceJoint);
            joint.uj.distanceJoint.motorSpeed = motorSpeed;
        }

        public static float b2DistanceJoint_GetMotorSpeed(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_distanceJoint);
            return joint.uj.distanceJoint.motorSpeed;
        }

        public static float b2DistanceJoint_GetMotorForce(B2JointId jointId)
        {
            B2World world = b2GetWorld(jointId.world0);
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_distanceJoint);
            return world.inv_h * @base.uj.distanceJoint.motorImpulse;
        }

        public static void b2DistanceJoint_SetMaxMotorForce(B2JointId jointId, float force)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_distanceJoint);
            joint.uj.distanceJoint.maxMotorForce = force;
        }

        public static float b2DistanceJoint_GetMaxMotorForce(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_distanceJoint);
            return joint.uj.distanceJoint.maxMotorForce;
        }

        public static B2Vec2 b2GetDistanceJointForce(B2World world, B2JointSim @base)
        {
            ref readonly B2DistanceJoint joint = ref @base.uj.distanceJoint;

            B2Transform transformA = b2GetBodyTransform(world, @base.bodyIdA);
            B2Transform transformB = b2GetBodyTransform(world, @base.bodyIdB);

            B2Vec2 pA = b2TransformPoint(ref transformA, @base.localFrameA.p);
            B2Vec2 pB = b2TransformPoint(ref transformB, @base.localFrameB.p);
            B2Vec2 d = b2Sub(pB, pA);
            B2Vec2 axis = b2Normalize(d);
            float force = (joint.impulse + joint.lowerImpulse - joint.upperImpulse + joint.motorImpulse) * world.inv_h;
            return b2MulSV(force, axis);
        }

// 1-D constrained system
// m (v2 - v1) = lambda
// v2 + (beta/h) * x1 + gamma * lambda = 0, gamma has units of inverse mass.
// x2 = x1 + h * v2

// 1-D mass-damper-spring system
// m (v2 - v1) + h * d * v2 + h * k *

// C = norm(p2 - p1) - L
// u = (p2 - p1) / norm(p2 - p1)
// Cdot = dot(u, v2 + cross(w2, r2) - v1 - cross(w1, r1))
// J = [-u -cross(r1, u) u cross(r2, u)]
// K = J * invM * JT
//   = invMass1 + invI1 * cross(r1, u)^2 + invMass2 + invI2 * cross(r2, u)^2

        public static void b2PrepareDistanceJoint(B2JointSim @base, B2StepContext context)
        {
            B2_ASSERT(@base.type == B2JointType.b2_distanceJoint);

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

            ref B2DistanceJoint joint = ref @base.uj.distanceJoint;

            joint.indexA = bodyA.setIndex == (int)B2SetType.b2_awakeSet ? localIndexA : B2_NULL_INDEX;
            joint.indexB = bodyB.setIndex == (int)B2SetType.b2_awakeSet ? localIndexB : B2_NULL_INDEX;

            // initial anchors in world space
            joint.anchorA = b2RotateVector(bodySimA.transform.q, b2Sub(@base.localFrameA.p, bodySimA.localCenter));
            joint.anchorB = b2RotateVector(bodySimB.transform.q, b2Sub(@base.localFrameB.p, bodySimB.localCenter));
            joint.deltaCenter = b2Sub(bodySimB.center, bodySimA.center);

            B2Vec2 rA = joint.anchorA;
            B2Vec2 rB = joint.anchorB;
            B2Vec2 separation = b2Add(b2Sub(rB, rA), joint.deltaCenter);
            B2Vec2 axis = b2Normalize(separation);

            // compute effective mass
            float crA = b2Cross(rA, axis);
            float crB = b2Cross(rB, axis);
            float k = mA + mB + iA * crA * crA + iB * crB * crB;
            joint.axialMass = k > 0.0f ? 1.0f / k : 0.0f;

            joint.distanceSoftness = b2MakeSoft(joint.hertz, joint.dampingRatio, context.h);

            if (context.enableWarmStarting == false)
            {
                joint.impulse = 0.0f;
                joint.lowerImpulse = 0.0f;
                joint.upperImpulse = 0.0f;
                joint.motorImpulse = 0.0f;
            }
        }

        public static void b2WarmStartDistanceJoint(B2JointSim @base, B2StepContext context)
        {
            B2_ASSERT(@base.type == B2JointType.b2_distanceJoint);

            float mA = @base.invMassA;
            float mB = @base.invMassB;
            float iA = @base.invIA;
            float iB = @base.invIB;

            // dummy state for static bodies
            B2BodyState dummyState = B2BodyState.Create(b2_identityBodyState);

            ref readonly B2DistanceJoint joint = ref @base.uj.distanceJoint;
            B2BodyState stateA = joint.indexA == B2_NULL_INDEX ? dummyState : context.states[joint.indexA];
            B2BodyState stateB = joint.indexB == B2_NULL_INDEX ? dummyState : context.states[joint.indexB];

            B2Vec2 rA = b2RotateVector(stateA.deltaRotation, joint.anchorA);
            B2Vec2 rB = b2RotateVector(stateB.deltaRotation, joint.anchorB);

            B2Vec2 ds = b2Add(b2Sub(stateB.deltaPosition, stateA.deltaPosition), b2Sub(rB, rA));
            B2Vec2 separation = b2Add(joint.deltaCenter, ds);
            B2Vec2 axis = b2Normalize(separation);

            float axialImpulse = joint.impulse + joint.lowerImpulse - joint.upperImpulse + joint.motorImpulse;
            B2Vec2 P = b2MulSV(axialImpulse, axis);

            stateA.linearVelocity = b2MulSub(stateA.linearVelocity, mA, P);
            stateA.angularVelocity -= iA * b2Cross(rA, P);
            stateB.linearVelocity = b2MulAdd(stateB.linearVelocity, mB, P);
            stateB.angularVelocity += iB * b2Cross(rB, P);
        }

        public static void b2SolveDistanceJoint(B2JointSim @base, B2StepContext context, bool useBias)
        {
            B2_ASSERT(@base.type == B2JointType.b2_distanceJoint);

            float mA = @base.invMassA;
            float mB = @base.invMassB;
            float iA = @base.invIA;
            float iB = @base.invIB;

            // dummy state for static bodies
            B2BodyState dummyState = B2BodyState.Create(b2_identityBodyState);

            ref B2DistanceJoint joint = ref @base.uj.distanceJoint;
            B2BodyState stateA = joint.indexA == B2_NULL_INDEX ? dummyState : context.states[joint.indexA];
            B2BodyState stateB = joint.indexB == B2_NULL_INDEX ? dummyState : context.states[joint.indexB];

            B2Vec2 vA = stateA.linearVelocity;
            float wA = stateA.angularVelocity;
            B2Vec2 vB = stateB.linearVelocity;
            float wB = stateB.angularVelocity;

            // current anchors
            B2Vec2 rA = b2RotateVector(stateA.deltaRotation, joint.anchorA);
            B2Vec2 rB = b2RotateVector(stateB.deltaRotation, joint.anchorB);

            // current separation
            B2Vec2 ds = b2Add(b2Sub(stateB.deltaPosition, stateA.deltaPosition), b2Sub(rB, rA));
            B2Vec2 separation = b2Add(joint.deltaCenter, ds);

            float length = b2Length(separation);
            B2Vec2 axis = b2Normalize(separation);

            // joint is soft if
            // - spring is enabled
            // - and (joint limit is disabled or limits are not equal)
            if (joint.enableSpring && (joint.minLength < joint.maxLength || joint.enableLimit == false))
            {
                // spring
                if (joint.hertz > 0.0f)
                {
                    // Cdot = dot(u, v + cross(w, r))
                    B2Vec2 vr = b2Add(b2Sub(vB, vA), b2Sub(b2CrossSV(wB, rB), b2CrossSV(wA, rA)));
                    float Cdot = b2Dot(axis, vr);
                    float C = length - joint.length;
                    float bias = joint.distanceSoftness.biasRate * C;

                    float m = joint.distanceSoftness.massScale * joint.axialMass;
                    float impulse = -m * (Cdot + bias) - joint.distanceSoftness.impulseScale * joint.impulse;
                    joint.impulse += impulse;

                    B2Vec2 P = b2MulSV(impulse, axis);
                    vA = b2MulSub(vA, mA, P);
                    wA -= iA * b2Cross(rA, P);
                    vB = b2MulAdd(vB, mB, P);
                    wB += iB * b2Cross(rB, P);
                }

                if (joint.enableLimit)
                {
                    // lower limit
                    {
                        B2Vec2 vr = b2Add(b2Sub(vB, vA), b2Sub(b2CrossSV(wB, rB), b2CrossSV(wA, rA)));
                        float Cdot = b2Dot(axis, vr);

                        float C = length - joint.minLength;

                        float bias = 0.0f;
                        float massCoeff = 1.0f;
                        float impulseCoeff = 0.0f;
                        if (C > 0.0f)
                        {
                            // speculative
                            bias = C * context.inv_h;
                        }
                        else if (useBias)
                        {
                            bias = @base.constraintSoftness.biasRate * C;
                            massCoeff = @base.constraintSoftness.massScale;
                            impulseCoeff = @base.constraintSoftness.impulseScale;
                        }

                        float impulse = -massCoeff * joint.axialMass * (Cdot + bias) - impulseCoeff * joint.lowerImpulse;
                        float newImpulse = b2MaxFloat(0.0f, joint.lowerImpulse + impulse);
                        impulse = newImpulse - joint.lowerImpulse;
                        joint.lowerImpulse = newImpulse;

                        B2Vec2 P = b2MulSV(impulse, axis);
                        vA = b2MulSub(vA, mA, P);
                        wA -= iA * b2Cross(rA, P);
                        vB = b2MulAdd(vB, mB, P);
                        wB += iB * b2Cross(rB, P);
                    }

                    // upper
                    {
                        B2Vec2 vr = b2Add(b2Sub(vA, vB), b2Sub(b2CrossSV(wA, rA), b2CrossSV(wB, rB)));
                        float Cdot = b2Dot(axis, vr);

                        float C = joint.maxLength - length;

                        float bias = 0.0f;
                        float massScale = 1.0f;
                        float impulseScale = 0.0f;
                        if (C > 0.0f)
                        {
                            // speculative
                            bias = C * context.inv_h;
                        }
                        else if (useBias)
                        {
                            bias = @base.constraintSoftness.biasRate * C;
                            massScale = @base.constraintSoftness.massScale;
                            impulseScale = @base.constraintSoftness.impulseScale;
                        }

                        float impulse = -massScale * joint.axialMass * (Cdot + bias) - impulseScale * joint.upperImpulse;
                        float newImpulse = b2MaxFloat(0.0f, joint.upperImpulse + impulse);
                        impulse = newImpulse - joint.upperImpulse;
                        joint.upperImpulse = newImpulse;

                        B2Vec2 P = b2MulSV(-impulse, axis);
                        vA = b2MulSub(vA, mA, P);
                        wA -= iA * b2Cross(rA, P);
                        vB = b2MulAdd(vB, mB, P);
                        wB += iB * b2Cross(rB, P);
                    }
                }

                if (joint.enableMotor)
                {
                    B2Vec2 vr = b2Add(b2Sub(vB, vA), b2Sub(b2CrossSV(wB, rB), b2CrossSV(wA, rA)));
                    float Cdot = b2Dot(axis, vr);
                    float impulse = joint.axialMass * (joint.motorSpeed - Cdot);
                    float oldImpulse = joint.motorImpulse;
                    float maxImpulse = context.h * joint.maxMotorForce;
                    joint.motorImpulse = b2ClampFloat(joint.motorImpulse + impulse, -maxImpulse, maxImpulse);
                    impulse = joint.motorImpulse - oldImpulse;

                    B2Vec2 P = b2MulSV(impulse, axis);
                    vA = b2MulSub(vA, mA, P);
                    wA -= iA * b2Cross(rA, P);
                    vB = b2MulAdd(vB, mB, P);
                    wB += iB * b2Cross(rB, P);
                }
            }
            else
            {
                // rigid constraint
                B2Vec2 vr = b2Add(b2Sub(vB, vA), b2Sub(b2CrossSV(wB, rB), b2CrossSV(wA, rA)));
                float Cdot = b2Dot(axis, vr);

                float C = length - joint.length;

                float bias = 0.0f;
                float massScale = 1.0f;
                float impulseScale = 0.0f;
                if (useBias)
                {
                    bias = @base.constraintSoftness.biasRate * C;
                    massScale = @base.constraintSoftness.massScale;
                    impulseScale = @base.constraintSoftness.impulseScale;
                }

                float impulse = -massScale * joint.axialMass * (Cdot + bias) - impulseScale * joint.impulse;
                joint.impulse += impulse;

                B2Vec2 P = b2MulSV(impulse, axis);
                vA = b2MulSub(vA, mA, P);
                wA -= iA * b2Cross(rA, P);
                vB = b2MulAdd(vB, mB, P);
                wB += iB * b2Cross(rB, P);
            }

            stateA.linearVelocity = vA;
            stateA.angularVelocity = wA;
            stateB.linearVelocity = vB;
            stateB.angularVelocity = wB;
        }

#if FALSE
    public static void b2DistanceJoint::Dump()
    {
    	int32 indexA = m_bodyA.m_islandIndex;
    	int32 indexB = m_bodyB.m_islandIndex;
    
    	b2Dump("  b2DistanceJointDef jd;\n");
    	b2Dump("  jd.bodyA = sims[%d];\n", indexA);
    	b2Dump("  jd.bodyB = sims[%d];\n", indexB);
    	b2Dump("  jd.collideConnected = bool(%d);\n", m_collideConnected);
    	b2Dump("  jd.localAnchorA.Set(%.9g, %.9g);\n", m_localAnchorA.x, m_localAnchorA.y);
    	b2Dump("  jd.localAnchorB.Set(%.9g, %.9g);\n", m_localAnchorB.x, m_localAnchorB.y);
    	b2Dump("  jd.length = %.9g;\n", m_length);
    	b2Dump("  jd.minLength = %.9g;\n", m_minLength);
    	b2Dump("  jd.maxLength = %.9g;\n", m_maxLength);
    	b2Dump("  jd.stiffness = %.9g;\n", m_stiffness);
    	b2Dump("  jd.damping = %.9g;\n", m_damping);
    	b2Dump("  joints[%d] = m_world.CreateJoint(&jd);\n", m_index);
    }
#endif

        public static void b2DrawDistanceJoint(B2DebugDraw draw, B2JointSim @base, B2Transform transformA, B2Transform transformB)
        {
            B2_ASSERT(@base.type == B2JointType.b2_distanceJoint);

            ref readonly B2DistanceJoint joint = ref @base.uj.distanceJoint;

            B2Vec2 pA = b2TransformPoint(ref transformA, @base.localFrameA.p);
            B2Vec2 pB = b2TransformPoint(ref transformB, @base.localFrameB.p);

            B2Vec2 axis = b2Normalize(b2Sub(pB, pA));

            if (joint.minLength < joint.maxLength && joint.enableLimit)
            {
                B2Vec2 pMin = b2MulAdd(pA, joint.minLength, axis);
                B2Vec2 pMax = b2MulAdd(pA, joint.maxLength, axis);
                B2Vec2 offset = b2MulSV(0.05f * b2_lengthUnitsPerMeter, b2RightPerp(axis));

                if (joint.minLength > B2_LINEAR_SLOP)
                {
                    // draw.DrawPoint(pMin, 4.0f, c2, draw.context);
                    draw.DrawSegmentFcn(b2Sub(pMin, offset), b2Add(pMin, offset), B2HexColor.b2_colorLightGreen, draw.context);
                }

                if (joint.maxLength < B2_HUGE)
                {
                    // draw.DrawPoint(pMax, 4.0f, c3, draw.context);
                    draw.DrawSegmentFcn(b2Sub(pMax, offset), b2Add(pMax, offset), B2HexColor.b2_colorRed, draw.context);
                }

                if (joint.minLength > B2_LINEAR_SLOP && joint.maxLength < B2_HUGE)
                {
                    draw.DrawSegmentFcn(pMin, pMax, B2HexColor.b2_colorGray, draw.context);
                }
            }

            draw.DrawSegmentFcn(pA, pB, B2HexColor.b2_colorWhite, draw.context);
            draw.DrawPointFcn(pA, 4.0f, B2HexColor.b2_colorWhite, draw.context);
            draw.DrawPointFcn(pB, 4.0f, B2HexColor.b2_colorWhite, draw.context);

            if (joint.hertz > 0.0f && joint.enableSpring)
            {
                B2Vec2 pRest = b2MulAdd(pA, joint.length, axis);
                draw.DrawPointFcn(pRest, 4.0f, B2HexColor.b2_colorBlue, draw.context);
            }
        }
    }
}