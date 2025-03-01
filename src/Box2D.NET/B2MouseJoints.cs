﻿// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Solvers;
using static Box2D.NET.B2Joints;


namespace Box2D.NET
{
    public static class B2MouseJoints
    {
        public static void b2MouseJoint_SetTarget(B2JointId jointId, B2Vec2 target)
        {
            Debug.Assert(b2IsValidVec2(target));
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_mouseJoint);
            @base.mouseJoint.targetA = target;
        }

        public static B2Vec2 b2MouseJoint_GetTarget(B2JointId jointId)
        {
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_mouseJoint);
            return @base.mouseJoint.targetA;
        }

        public static void b2MouseJoint_SetSpringHertz(B2JointId jointId, float hertz)
        {
            Debug.Assert(b2IsValidFloat(hertz) && hertz >= 0.0f);
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_mouseJoint);
            @base.mouseJoint.hertz = hertz;
        }

        public static float b2MouseJoint_GetSpringHertz(B2JointId jointId)
        {
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_mouseJoint);
            return @base.mouseJoint.hertz;
        }

        public static void b2MouseJoint_SetSpringDampingRatio(B2JointId jointId, float dampingRatio)
        {
            Debug.Assert(b2IsValidFloat(dampingRatio) && dampingRatio >= 0.0f);
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_mouseJoint);
            @base.mouseJoint.dampingRatio = dampingRatio;
        }

        public static float b2MouseJoint_GetSpringDampingRatio(B2JointId jointId)
        {
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_mouseJoint);
            return @base.mouseJoint.dampingRatio;
        }

        public static void b2MouseJoint_SetMaxForce(B2JointId jointId, float maxForce)
        {
            Debug.Assert(b2IsValidFloat(maxForce) && maxForce >= 0.0f);
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_mouseJoint);
            @base.mouseJoint.maxForce = maxForce;
        }

        public static float b2MouseJoint_GetMaxForce(B2JointId jointId)
        {
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_mouseJoint);
            return @base.mouseJoint.maxForce;
        }

        public static B2Vec2 b2GetMouseJointForce(B2World world, B2JointSim @base)
        {
            B2Vec2 force = b2MulSV(world.inv_h, @base.mouseJoint.linearImpulse);
            return force;
        }

        public static float b2GetMouseJointTorque(B2World world, B2JointSim @base)
        {
            return world.inv_h * @base.mouseJoint.angularImpulse;
        }

        public static void b2PrepareMouseJoint(B2JointSim @base, B2StepContext context)
        {
            Debug.Assert(@base.type == B2JointType.b2_mouseJoint);

            // chase body id to the solver set where the body lives
            int idB = @base.bodyIdB;

            B2World world = context.world;

            B2Body bodyB = b2Array_Get(ref world.bodies, idB);

            Debug.Assert(bodyB.setIndex == (int)B2SetType.b2_awakeSet);
            B2SolverSet setB = b2Array_Get(ref world.solverSets, bodyB.setIndex);

            int localIndexB = bodyB.localIndex;
            B2BodySim bodySimB = b2Array_Get(ref setB.bodySims, localIndexB);

            @base.invMassB = bodySimB.invMass;
            @base.invIB = bodySimB.invInertia;

            B2MouseJoint joint = @base.mouseJoint;
            joint.indexB = bodyB.setIndex == (int)B2SetType.b2_awakeSet ? localIndexB : B2_NULL_INDEX;
            joint.anchorB = b2RotateVector(bodySimB.transform.q, b2Sub(@base.localOriginAnchorB, bodySimB.localCenter));

            joint.linearSoftness = b2MakeSoft(joint.hertz, joint.dampingRatio, context.h);

            float angularHertz = 0.5f;
            float angularDampingRatio = 0.1f;
            joint.angularSoftness = b2MakeSoft(angularHertz, angularDampingRatio, context.h);

            B2Vec2 rB = joint.anchorB;
            float mB = bodySimB.invMass;
            float iB = bodySimB.invInertia;

            // K = [(1/m1 + 1/m2) * eye(2) - skew(r1) * invI1 * skew(r1) - skew(r2) * invI2 * skew(r2)]
            //   = [1/m1+1/m2     0    ] + invI1 * [r1.y*r1.y -r1.x*r1.y] + invI2 * [r1.y*r1.y -r1.x*r1.y]
            //     [    0     1/m1+1/m2]           [-r1.x*r1.y r1.x*r1.x]           [-r1.x*r1.y r1.x*r1.x]
            B2Mat22 K;
            K.cx.x = mB + iB * rB.y * rB.y;
            K.cx.y = -iB * rB.x * rB.y;
            K.cy.x = K.cx.y;
            K.cy.y = mB + iB * rB.x * rB.x;

            joint.linearMass = b2GetInverse22(K);
            joint.deltaCenter = b2Sub(bodySimB.center, joint.targetA);

            if (context.enableWarmStarting == false)
            {
                joint.linearImpulse = b2Vec2_zero;
                joint.angularImpulse = 0.0f;
            }
        }

        public static void b2WarmStartMouseJoint(B2JointSim @base, B2StepContext context)
        {
            Debug.Assert(@base.type == B2JointType.b2_mouseJoint);

            float mB = @base.invMassB;
            float iB = @base.invIB;

            B2MouseJoint joint = @base.mouseJoint;

            B2BodyState stateB = context.states[joint.indexB];
            B2Vec2 vB = stateB.linearVelocity;
            float wB = stateB.angularVelocity;

            B2Rot dqB = stateB.deltaRotation;
            B2Vec2 rB = b2RotateVector(dqB, joint.anchorB);

            vB = b2MulAdd(vB, mB, joint.linearImpulse);
            wB += iB * (b2Cross(rB, joint.linearImpulse) + joint.angularImpulse);

            stateB.linearVelocity = vB;
            stateB.angularVelocity = wB;
        }

        public static void b2SolveMouseJoint(B2JointSim @base, B2StepContext context)
        {
            float mB = @base.invMassB;
            float iB = @base.invIB;

            B2MouseJoint joint = @base.mouseJoint;
            B2BodyState stateB = context.states[joint.indexB];

            B2Vec2 vB = stateB.linearVelocity;
            float wB = stateB.angularVelocity;

            // Softness with no bias to reduce rotation speed
            {
                float massScale = joint.angularSoftness.massScale;
                float impulseScale = joint.angularSoftness.impulseScale;

                float impulse = iB > 0.0f ? -wB / iB : 0.0f;
                impulse = massScale * impulse - impulseScale * joint.angularImpulse;
                joint.angularImpulse += impulse;

                wB += iB * impulse;
            }

            float maxImpulse = joint.maxForce * context.h;

            {
                B2Rot dqB = stateB.deltaRotation;
                B2Vec2 rB = b2RotateVector(dqB, joint.anchorB);
                B2Vec2 Cdot = b2Add(vB, b2CrossSV(wB, rB));

                B2Vec2 separation = b2Add(b2Add(stateB.deltaPosition, rB), joint.deltaCenter);
                B2Vec2 bias = b2MulSV(joint.linearSoftness.biasRate, separation);

                float massScale = joint.linearSoftness.massScale;
                float impulseScale = joint.linearSoftness.impulseScale;

                B2Vec2 b = b2MulMV(joint.linearMass, b2Add(Cdot, bias));

                B2Vec2 impulse;
                impulse.x = -massScale * b.x - impulseScale * joint.linearImpulse.x;
                impulse.y = -massScale * b.y - impulseScale * joint.linearImpulse.y;

                B2Vec2 oldImpulse = joint.linearImpulse;
                joint.linearImpulse.x += impulse.x;
                joint.linearImpulse.y += impulse.y;

                float mag = b2Length(joint.linearImpulse);
                if (mag > maxImpulse)
                {
                    joint.linearImpulse = b2MulSV(maxImpulse, b2Normalize(joint.linearImpulse));
                }

                impulse.x = joint.linearImpulse.x - oldImpulse.x;
                impulse.y = joint.linearImpulse.y - oldImpulse.y;

                vB = b2MulAdd(vB, mB, impulse);
                wB += iB * b2Cross(rB, impulse);
            }

            stateB.linearVelocity = vB;
            stateB.angularVelocity = wB;
        }
    }
}
