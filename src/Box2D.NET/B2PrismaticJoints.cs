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
    public static class B2PrismaticJoints
    {
        public static void b2PrismaticJoint_EnableSpring(B2JointId jointId, bool enableSpring)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_prismaticJoint);
            if (enableSpring != joint.prismaticJoint.enableSpring)
            {
                joint.prismaticJoint.enableSpring = enableSpring;
                joint.prismaticJoint.springImpulse = 0.0f;
            }
        }

        public static bool b2PrismaticJoint_IsSpringEnabled(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_prismaticJoint);
            return joint.prismaticJoint.enableSpring;
        }

        public static void b2PrismaticJoint_SetSpringHertz(B2JointId jointId, float hertz)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_prismaticJoint);
            joint.prismaticJoint.hertz = hertz;
        }

        public static float b2PrismaticJoint_GetSpringHertz(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_prismaticJoint);
            return joint.prismaticJoint.hertz;
        }

        public static void b2PrismaticJoint_SetSpringDampingRatio(B2JointId jointId, float dampingRatio)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_prismaticJoint);
            joint.prismaticJoint.dampingRatio = dampingRatio;
        }

        public static float b2PrismaticJoint_GetSpringDampingRatio(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_prismaticJoint);
            return joint.prismaticJoint.dampingRatio;
        }

        public static void b2PrismaticJoint_EnableLimit(B2JointId jointId, bool enableLimit)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_prismaticJoint);
            if (enableLimit != joint.prismaticJoint.enableLimit)
            {
                joint.prismaticJoint.enableLimit = enableLimit;
                joint.prismaticJoint.lowerImpulse = 0.0f;
                joint.prismaticJoint.upperImpulse = 0.0f;
            }
        }

        public static bool b2PrismaticJoint_IsLimitEnabled(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_prismaticJoint);
            return joint.prismaticJoint.enableLimit;
        }

        public static float b2PrismaticJoint_GetLowerLimit(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_prismaticJoint);
            return joint.prismaticJoint.lowerTranslation;
        }

        public static float b2PrismaticJoint_GetUpperLimit(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_prismaticJoint);
            return joint.prismaticJoint.upperTranslation;
        }

        public static void b2PrismaticJoint_SetLimits(B2JointId jointId, float lower, float upper)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_prismaticJoint);
            // TODO: @ikpil, check epsilon
            if (lower != joint.prismaticJoint.lowerTranslation || upper != joint.prismaticJoint.upperTranslation)
            {
                joint.prismaticJoint.lowerTranslation = b2MinFloat(lower, upper);
                joint.prismaticJoint.upperTranslation = b2MaxFloat(lower, upper);
                joint.prismaticJoint.lowerImpulse = 0.0f;
                joint.prismaticJoint.upperImpulse = 0.0f;
            }
        }

        public static void b2PrismaticJoint_EnableMotor(B2JointId jointId, bool enableMotor)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_prismaticJoint);
            if (enableMotor != joint.prismaticJoint.enableMotor)
            {
                joint.prismaticJoint.enableMotor = enableMotor;
                joint.prismaticJoint.motorImpulse = 0.0f;
            }
        }

        public static bool b2PrismaticJoint_IsMotorEnabled(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_prismaticJoint);
            return joint.prismaticJoint.enableMotor;
        }

        public static void b2PrismaticJoint_SetMotorSpeed(B2JointId jointId, float motorSpeed)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_prismaticJoint);
            joint.prismaticJoint.motorSpeed = motorSpeed;
        }

        public static float b2PrismaticJoint_GetMotorSpeed(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_prismaticJoint);
            return joint.prismaticJoint.motorSpeed;
        }

        public static float b2PrismaticJoint_GetMotorForce(B2JointId jointId)
        {
            B2World world = b2GetWorld(jointId.world0);
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_prismaticJoint);
            return world.inv_h * @base.prismaticJoint.motorImpulse;
        }

        public static void b2PrismaticJoint_SetMaxMotorForce(B2JointId jointId, float force)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_prismaticJoint);
            joint.prismaticJoint.maxMotorForce = force;
        }

        public static float b2PrismaticJoint_GetMaxMotorForce(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_prismaticJoint);
            return joint.prismaticJoint.maxMotorForce;
        }

        public static float b2PrismaticJoint_GetTranslation(B2JointId jointId)
        {
            B2World world = b2GetWorld(jointId.world0);
            B2JointSim jointSim = b2GetJointSimCheckType(jointId, B2JointType.b2_prismaticJoint);
            B2Transform transformA = b2GetBodyTransform(world, jointSim.bodyIdA);
            B2Transform transformB = b2GetBodyTransform(world, jointSim.bodyIdB);

            ref readonly B2PrismaticJoint joint = ref jointSim.prismaticJoint;
            B2Vec2 axisA = b2RotateVector(transformA.q, joint.localAxisA);
            B2Vec2 pA = b2TransformPoint(ref transformA, jointSim.localOriginAnchorA);
            B2Vec2 pB = b2TransformPoint(ref transformB, jointSim.localOriginAnchorB);
            B2Vec2 d = b2Sub(pB, pA);
            float translation = b2Dot(d, axisA);
            return translation;
        }

        public static float b2PrismaticJoint_GetSpeed(B2JointId jointId)
        {
            B2World world = b2GetWorld(jointId.world0);
            B2Joint joint = b2GetJointFullId(world, jointId);
            Debug.Assert(joint.type == B2JointType.b2_prismaticJoint);
            B2JointSim jointSim = b2GetJointSim(world, joint);
            Debug.Assert(jointSim.type == B2JointType.b2_prismaticJoint);

            B2Body bodyA = b2Array_Get(ref world.bodies, jointSim.bodyIdA);
            B2Body bodyB = b2Array_Get(ref world.bodies, jointSim.bodyIdB);
            B2BodySim bodySimA = b2GetBodySim(world, bodyA);
            B2BodySim bodySimB = b2GetBodySim(world, bodyB);
            B2BodyState bodyStateA = b2GetBodyState(world, bodyA);
            B2BodyState bodyStateB = b2GetBodyState(world, bodyB);

            B2Transform transformA = bodySimA.transform;
            B2Transform transformB = bodySimB.transform;

            ref readonly B2PrismaticJoint prismatic = ref jointSim.prismaticJoint;
            B2Vec2 axisA = b2RotateVector(transformA.q, prismatic.localAxisA);
            B2Vec2 cA = bodySimA.center;
            B2Vec2 cB = bodySimB.center;
            B2Vec2 rA = b2RotateVector(transformA.q, b2Sub(jointSim.localOriginAnchorA, bodySimA.localCenter));
            B2Vec2 rB = b2RotateVector(transformB.q, b2Sub(jointSim.localOriginAnchorB, bodySimB.localCenter));

            B2Vec2 d = b2Add(b2Sub(cB, cA), b2Sub(rB, rA));

            B2Vec2 vA = null != bodyStateA ? bodyStateA.linearVelocity : b2Vec2_zero;
            B2Vec2 vB = null != bodyStateB ? bodyStateB.linearVelocity : b2Vec2_zero;
            float wA = null != bodyStateA ? bodyStateA.angularVelocity : 0.0f;
            float wB = null != bodyStateB ? bodyStateB.angularVelocity : 0.0f;

            B2Vec2 vRel = b2Sub(b2Add(vB, b2CrossSV(wB, rB)), b2Add(vA, b2CrossSV(wA, rA)));
            float speed = b2Dot(d, b2CrossSV(wA, axisA)) + b2Dot(axisA, vRel);
            return speed;
        }

        public static B2Vec2 b2GetPrismaticJointForce(B2World world, B2JointSim @base)
        {
            int idA = @base.bodyIdA;
            B2Transform transformA = b2GetBodyTransform(world, idA);

            ref readonly B2PrismaticJoint joint = ref @base.prismaticJoint;

            B2Vec2 axisA = b2RotateVector(transformA.q, joint.localAxisA);
            B2Vec2 perpA = b2LeftPerp(axisA);

            float inv_h = world.inv_h;
            float perpForce = inv_h * joint.impulse.X;
            float axialForce = inv_h * (joint.motorImpulse + joint.lowerImpulse - joint.upperImpulse);

            B2Vec2 force = b2Add(b2MulSV(perpForce, perpA), b2MulSV(axialForce, axisA));
            return force;
        }

        public static float b2GetPrismaticJointTorque(B2World world, B2JointSim @base)
        {
            return world.inv_h * @base.prismaticJoint.impulse.Y;
        }

// Linear constraint (point-to-line)
// d = p2 - p1 = x2 + r2 - x1 - r1
// C = dot(perp, d)
// Cdot = dot(d, cross(w1, perp)) + dot(perp, v2 + cross(w2, r2) - v1 - cross(w1, r1))
//      = -dot(perp, v1) - dot(cross(d + r1, perp), w1) + dot(perp, v2) + dot(cross(r2, perp), v2)
// J = [-perp, -cross(d + r1, perp), perp, cross(r2,perp)]
//
// Angular constraint
// C = a2 - a1 + a_initial
// Cdot = w2 - w1
// J = [0 0 -1 0 0 1]
//
// K = J * invM * JT
//
// J = [-a -s1 a s2]
//     [0  -1  0  1]
// a = perp
// s1 = cross(d + r1, a) = cross(p2 - x1, a)
// s2 = cross(r2, a) = cross(p2 - x2, a)

// Motor/Limit linear constraint
// C = dot(ax1, d)
// Cdot = -dot(ax1, v1) - dot(cross(d + r1, ax1), w1) + dot(ax1, v2) + dot(cross(r2, ax1), v2)
// J = [-ax1 -cross(d+r1,ax1) ax1 cross(r2,ax1)]

// Predictive limit is applied even when the limit is not active.
// Prevents a constraint speed that can lead to a constraint error in one time step.
// Want C2 = C1 + h * Cdot >= 0
// Or:
// Cdot + C1/h >= 0
// I do not apply a negative constraint error because that is handled in position correction.
// So:
// Cdot + max(C1, 0)/h >= 0

// Block Solver
// We develop a block solver that includes the angular and linear constraints. This makes the limit stiffer.
//
// The Jacobian has 2 rows:
// J = [-uT -s1 uT s2] // linear
//     [0   -1   0  1] // angular
//
// u = perp
// s1 = cross(d + r1, u), s2 = cross(r2, u)
// a1 = cross(d + r1, v), a2 = cross(r2, v)

        public static void b2PreparePrismaticJoint(B2JointSim @base, B2StepContext context)
        {
            Debug.Assert(@base.type == B2JointType.b2_prismaticJoint);

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

            ref B2PrismaticJoint joint = ref @base.prismaticJoint;
            joint.indexA = bodyA.setIndex == (int)B2SetType.b2_awakeSet ? localIndexA : B2_NULL_INDEX;
            joint.indexB = bodyB.setIndex == (int)B2SetType.b2_awakeSet ? localIndexB : B2_NULL_INDEX;

            B2Rot qA = bodySimA.transform.q;
            B2Rot qB = bodySimB.transform.q;

            joint.anchorA = b2RotateVector(qA, b2Sub(@base.localOriginAnchorA, bodySimA.localCenter));
            joint.anchorB = b2RotateVector(qB, b2Sub(@base.localOriginAnchorB, bodySimB.localCenter));
            joint.axisA = b2RotateVector(qA, joint.localAxisA);
            joint.deltaCenter = b2Sub(bodySimB.center, bodySimA.center);
            joint.deltaAngle = b2RelativeAngle(qB, qA) - joint.referenceAngle;
            joint.deltaAngle = b2UnwindAngle(joint.deltaAngle);

            B2Vec2 rA = joint.anchorA;
            B2Vec2 rB = joint.anchorB;

            B2Vec2 d = b2Add(joint.deltaCenter, b2Sub(rB, rA));
            float a1 = b2Cross(b2Add(d, rA), joint.axisA);
            float a2 = b2Cross(rB, joint.axisA);

            // effective masses
            float k = mA + mB + iA * a1 * a1 + iB * a2 * a2;
            joint.axialMass = k > 0.0f ? 1.0f / k : 0.0f;

            joint.springSoftness = b2MakeSoft(joint.hertz, joint.dampingRatio, context.h);

            if (context.enableWarmStarting == false)
            {
                joint.impulse = b2Vec2_zero;
                joint.springImpulse = 0.0f;
                joint.motorImpulse = 0.0f;
                joint.lowerImpulse = 0.0f;
                joint.upperImpulse = 0.0f;
            }
        }

        public static void b2WarmStartPrismaticJoint(B2JointSim @base, B2StepContext context)
        {
            Debug.Assert(@base.type == B2JointType.b2_prismaticJoint);

            float mA = @base.invMassA;
            float mB = @base.invMassB;
            float iA = @base.invIA;
            float iB = @base.invIB;

            // dummy state for static bodies
            B2BodyState dummyState = B2BodyState.Create(b2_identityBodyState);

            ref readonly B2PrismaticJoint joint = ref @base.prismaticJoint;

            B2BodyState stateA = joint.indexA == B2_NULL_INDEX ? dummyState : context.states[joint.indexA];
            B2BodyState stateB = joint.indexB == B2_NULL_INDEX ? dummyState : context.states[joint.indexB];

            B2Vec2 rA = b2RotateVector(stateA.deltaRotation, joint.anchorA);
            B2Vec2 rB = b2RotateVector(stateB.deltaRotation, joint.anchorB);

            B2Vec2 d = b2Add(b2Add(b2Sub(stateB.deltaPosition, stateA.deltaPosition), joint.deltaCenter), b2Sub(rB, rA));
            B2Vec2 axisA = b2RotateVector(stateA.deltaRotation, joint.axisA);

            // impulse is applied at anchor point on body B
            float a1 = b2Cross(b2Add(d, rA), axisA);
            float a2 = b2Cross(rB, axisA);
            float axialImpulse = joint.springImpulse + joint.motorImpulse + joint.lowerImpulse - joint.upperImpulse;

            // perpendicular constraint
            B2Vec2 perpA = b2LeftPerp(axisA);
            float s1 = b2Cross(b2Add(d, rA), perpA);
            float s2 = b2Cross(rB, perpA);
            float perpImpulse = joint.impulse.X;
            float angleImpulse = joint.impulse.Y;

            B2Vec2 P = b2Add(b2MulSV(axialImpulse, axisA), b2MulSV(perpImpulse, perpA));
            float LA = axialImpulse * a1 + perpImpulse * s1 + angleImpulse;
            float LB = axialImpulse * a2 + perpImpulse * s2 + angleImpulse;

            stateA.linearVelocity = b2MulSub(stateA.linearVelocity, mA, P);
            stateA.angularVelocity -= iA * LA;
            stateB.linearVelocity = b2MulAdd(stateB.linearVelocity, mB, P);
            stateB.angularVelocity += iB * LB;
        }

        public static void b2SolvePrismaticJoint(B2JointSim @base, B2StepContext context, bool useBias)
        {
            Debug.Assert(@base.type == B2JointType.b2_prismaticJoint);

            float mA = @base.invMassA;
            float mB = @base.invMassB;
            float iA = @base.invIA;
            float iB = @base.invIB;

            // dummy state for static bodies
            B2BodyState dummyState = B2BodyState.Create(b2_identityBodyState);

            ref B2PrismaticJoint joint = ref @base.prismaticJoint;

            B2BodyState stateA = joint.indexA == B2_NULL_INDEX ? dummyState : context.states[joint.indexA];
            B2BodyState stateB = joint.indexB == B2_NULL_INDEX ? dummyState : context.states[joint.indexB];

            B2Vec2 vA = stateA.linearVelocity;
            float wA = stateA.angularVelocity;
            B2Vec2 vB = stateB.linearVelocity;
            float wB = stateB.angularVelocity;

            // current anchors
            B2Vec2 rA = b2RotateVector(stateA.deltaRotation, joint.anchorA);
            B2Vec2 rB = b2RotateVector(stateB.deltaRotation, joint.anchorB);

            B2Vec2 d = b2Add(b2Add(b2Sub(stateB.deltaPosition, stateA.deltaPosition), joint.deltaCenter), b2Sub(rB, rA));
            B2Vec2 axisA = b2RotateVector(stateA.deltaRotation, joint.axisA);
            float translation = b2Dot(axisA, d);

            // These scalars are for torques generated by axial forces
            float a1 = b2Cross(b2Add(d, rA), axisA);
            float a2 = b2Cross(rB, axisA);

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

            // Solve motor constraint
            if (joint.enableMotor)
            {
                float Cdot = b2Dot(axisA, b2Sub(vB, vA)) + a2 * wB - a1 * wA;
                float impulse = joint.axialMass * (joint.motorSpeed - Cdot);
                float oldImpulse = joint.motorImpulse;
                float maxImpulse = context.h * joint.maxMotorForce;
                joint.motorImpulse = b2ClampFloat(joint.motorImpulse + impulse, -maxImpulse, maxImpulse);
                impulse = joint.motorImpulse - oldImpulse;

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

                    float oldImpulse = joint.lowerImpulse;
                    float Cdot = b2Dot(axisA, b2Sub(vB, vA)) + a2 * wB - a1 * wA;
                    float impulse = -joint.axialMass * massScale * (Cdot + bias) - impulseScale * oldImpulse;
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

                    float oldImpulse = joint.upperImpulse;
                    // sign flipped
                    float Cdot = b2Dot(axisA, b2Sub(vA, vB)) + a1 * wA - a2 * wB;
                    float impulse = -joint.axialMass * massScale * (Cdot + bias) - impulseScale * oldImpulse;
                    joint.upperImpulse = b2MaxFloat(oldImpulse + impulse, 0.0f);
                    impulse = joint.upperImpulse - oldImpulse;

                    B2Vec2 P = b2MulSV(impulse, axisA);
                    float LA = impulse * a1;
                    float LB = impulse * a2;

                    // sign flipped
                    vA = b2MulAdd(vA, mA, P);
                    wA += iA * LA;
                    vB = b2MulSub(vB, mB, P);
                    wB -= iB * LB;
                }
            }

            // Solve the prismatic constraint in block form
            {
                B2Vec2 perpA = b2LeftPerp(axisA);

                // These scalars are for torques generated by the perpendicular constraint force
                float s1 = b2Cross(b2Add(d, rA), perpA);
                float s2 = b2Cross(rB, perpA);

                B2Vec2 Cdot;
                Cdot.X = b2Dot(perpA, b2Sub(vB, vA)) + s2 * wB - s1 * wA;
                Cdot.Y = wB - wA;

                B2Vec2 bias = b2Vec2_zero;
                float massScale = 1.0f;
                float impulseScale = 0.0f;
                if (useBias)
                {
                    B2Vec2 C;
                    C.X = b2Dot(perpA, d);
                    C.Y = b2RelativeAngle(stateB.deltaRotation, stateA.deltaRotation) + joint.deltaAngle;

                    bias = b2MulSV(context.jointSoftness.biasRate, C);
                    massScale = context.jointSoftness.massScale;
                    impulseScale = context.jointSoftness.impulseScale;
                }

                float k11 = mA + mB + iA * s1 * s1 + iB * s2 * s2;
                float k12 = iA * s1 + iB * s2;
                float k22 = iA + iB;
                if (k22 == 0.0f)
                {
                    // For bodies with fixed rotation.
                    k22 = 1.0f;
                }

                B2Mat22 K = new B2Mat22(new B2Vec2(k11, k12), new B2Vec2(k12, k22));

                B2Vec2 b = b2Solve22(K, b2Add(Cdot, bias));
                B2Vec2 impulse;
                impulse.X = -massScale * b.X - impulseScale * joint.impulse.X;
                impulse.Y = -massScale * b.Y - impulseScale * joint.impulse.Y;

                joint.impulse.X += impulse.X;
                joint.impulse.Y += impulse.Y;

                B2Vec2 P = b2MulSV(impulse.X, perpA);
                float LA = impulse.X * s1 + impulse.Y;
                float LB = impulse.X * s2 + impulse.Y;

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
void b2PrismaticJoint::Dump()
{
	int32 indexA = joint.bodyA.joint.islandIndex;
	int32 indexB = joint.bodyB.joint.islandIndex;

	b2Dump("  b2PrismaticJointDef jd;\n");
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

        public static void b2DrawPrismaticJoint(B2DebugDraw draw, B2JointSim @base, B2Transform transformA, B2Transform transformB)
        {
            Debug.Assert(@base.type == B2JointType.b2_prismaticJoint);

            ref readonly B2PrismaticJoint joint = ref @base.prismaticJoint;

            B2Vec2 pA = b2TransformPoint(ref transformA, @base.localOriginAnchorA);
            B2Vec2 pB = b2TransformPoint(ref transformB, @base.localOriginAnchorB);

            B2Vec2 axis = b2RotateVector(transformA.q, joint.localAxisA);

            B2HexColor c1 = B2HexColor.b2_colorGray;
            B2HexColor c2 = B2HexColor.b2_colorGreen;
            B2HexColor c3 = B2HexColor.b2_colorRed;
            B2HexColor c4 = B2HexColor.b2_colorBlue;
            B2HexColor c5 = B2HexColor.b2_colorDimGray;

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
