// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Solvers;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET
{
    public static class B2MouseJoints
    {
        /// Set the mouse joint spring stiffness in Hertz
        public static void b2MouseJoint_SetSpringHertz(B2JointId jointId, float hertz)
        {
            B2_ASSERT(b2IsValidFloat(hertz) && hertz >= 0.0f);
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_mouseJoint);
            @base.uj.mouseJoint.hertz = hertz;
        }

        /// Get the mouse joint spring stiffness in Hertz
        public static float b2MouseJoint_GetSpringHertz(B2JointId jointId)
        {
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_mouseJoint);
            return @base.uj.mouseJoint.hertz;
        }

        public static void b2MouseJoint_SetSpringDampingRatio(B2JointId jointId, float dampingRatio)
        {
            B2_ASSERT(b2IsValidFloat(dampingRatio) && dampingRatio >= 0.0f);
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_mouseJoint);
            @base.uj.mouseJoint.dampingRatio = dampingRatio;
        }

        public static float b2MouseJoint_GetSpringDampingRatio(B2JointId jointId)
        {
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_mouseJoint);
            return @base.uj.mouseJoint.dampingRatio;
        }

        public static void b2MouseJoint_SetMaxForce(B2JointId jointId, float maxForce)
        {
            B2_ASSERT(b2IsValidFloat(maxForce) && maxForce >= 0.0f);
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_mouseJoint);
            @base.uj.mouseJoint.maxForce = maxForce;
        }

        public static float b2MouseJoint_GetMaxForce(B2JointId jointId)
        {
            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_mouseJoint);
            return @base.uj.mouseJoint.maxForce;
        }

        public static B2Vec2 b2GetMouseJointForce(B2World world, B2JointSim @base)
        {
            B2Vec2 force = b2MulSV(world.inv_h, @base.uj.mouseJoint.linearImpulse);
            return force;
        }

        public static float b2GetMouseJointTorque(B2World world, B2JointSim @base)
        {
            return world.inv_h * @base.uj.mouseJoint.angularImpulse;
        }

        public static void b2PrepareMouseJoint(B2JointSim @base, B2StepContext context)
        {
            B2_ASSERT(@base.type == B2JointType.b2_mouseJoint);

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

            ref B2MouseJoint joint = ref @base.uj.mouseJoint;

            joint.indexA = bodyA.setIndex == (int)B2SetType.b2_awakeSet ? localIndexA : B2_NULL_INDEX;
            joint.indexB = bodyB.setIndex == (int)B2SetType.b2_awakeSet ? localIndexB : B2_NULL_INDEX;

            // Compute joint anchor frames with world space rotation, relative to center of mass
            joint.frameA.q = b2MulRot(bodySimA.transform.q, @base.localFrameA.q);
            joint.frameA.p = b2RotateVector(bodySimA.transform.q, b2Sub(@base.localFrameA.p, bodySimA.localCenter));
            joint.frameB.q = b2MulRot(bodySimB.transform.q, @base.localFrameB.q);
            joint.frameB.p = b2RotateVector(bodySimB.transform.q, b2Sub(@base.localFrameB.p, bodySimB.localCenter));

            // Compute the initial center delta. Incremental position updates are relative to this.
            joint.deltaCenter = b2Sub(bodySimB.center, bodySimA.center);

            joint.linearSoftness = b2MakeSoft(joint.hertz, joint.dampingRatio, context.h);

            float angularHertz = 0.5f;
            float angularDampingRatio = 0.1f;
            joint.angularSoftness = b2MakeSoft(angularHertz, angularDampingRatio, context.h);

            B2Vec2 rA = joint.frameA.p;
            B2Vec2 rB = joint.frameB.p;

            // K = [(1/m1 + 1/m2) * eye(2) - skew(r1) * invI1 * skew(r1) - skew(r2) * invI2 * skew(r2)]
            //   = [1/m1+1/m2     0    ] + invI1 * [r1.y*r1.y -r1.x*r1.y] + invI2 * [r1.y*r1.y -r1.x*r1.y]
            //     [    0     1/m1+1/m2]           [-r1.x*r1.y r1.x*r1.x]           [-r1.x*r1.y r1.x*r1.x]
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

        public static void b2WarmStartMouseJoint(B2JointSim @base, B2StepContext context)
        {
            B2_ASSERT(@base.type == B2JointType.b2_mouseJoint);

            float mA = @base.invMassA;
            float mB = @base.invMassB;
            float iA = @base.invIA;
            float iB = @base.invIB;

            ref B2MouseJoint joint = ref @base.uj.mouseJoint;

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
            ;
        }

        public static void b2SolveMouseJoint(B2JointSim @base, B2StepContext context)
        {
            B2_ASSERT(@base.type == B2JointType.b2_mouseJoint);

            float mA = @base.invMassA;
            float mB = @base.invMassB;
            float iA = @base.invIA;
            float iB = @base.invIB;

            // dummy state for static bodies
            B2BodyState dummyState = B2BodyState.Create(b2_identityBodyState);

            ref B2MouseJoint joint = ref @base.uj.mouseJoint;
            B2BodyState stateA = joint.indexA == B2_NULL_INDEX ? dummyState : context.states[joint.indexA];
            B2BodyState stateB = joint.indexB == B2_NULL_INDEX ? dummyState : context.states[joint.indexB];

            B2Vec2 vA = stateA.linearVelocity;
            float wA = stateA.angularVelocity;
            B2Vec2 vB = stateB.linearVelocity;
            float wB = stateB.angularVelocity;

            // Softness with no bias to reduce rotation speed
            {
                float massScale = joint.angularSoftness.massScale;
                float impulseScale = joint.angularSoftness.impulseScale;

                float Cdot = wB - wA;
                float impulse = -massScale * joint.angularMass * Cdot - impulseScale * joint.angularImpulse;
                joint.angularImpulse += impulse;

                wA -= iA * impulse;
                wB += iB * impulse;
            }

            float maxImpulse = joint.maxForce * context.h;

            {
                B2Vec2 rA = b2RotateVector(stateA.deltaRotation, joint.frameA.p);
                B2Vec2 rB = b2RotateVector(stateB.deltaRotation, joint.frameB.p);

                B2Vec2 Cdot = b2Sub(b2Add(vB, b2CrossSV(wB, rB)), b2Add(vA, b2CrossSV(wA, rA)));

                B2Vec2 dcA = stateA.deltaPosition;
                B2Vec2 dcB = stateB.deltaPosition;
                B2Vec2 C = b2Add(b2Add(b2Sub(dcB, dcA), b2Sub(rB, rA)), joint.deltaCenter);
                B2Vec2 bias = b2MulSV(joint.linearSoftness.biasRate, C);

                float massScale = joint.linearSoftness.massScale;
                float impulseScale = joint.linearSoftness.impulseScale;

                B2Vec2 b = b2MulMV(joint.linearMass, b2Add(Cdot, bias));

                B2Vec2 impulse;
                impulse.X = -massScale * b.X - impulseScale * joint.linearImpulse.X;
                impulse.Y = -massScale * b.Y - impulseScale * joint.linearImpulse.Y;

                B2Vec2 oldImpulse = joint.linearImpulse;
                joint.linearImpulse.X += impulse.X;
                joint.linearImpulse.Y += impulse.Y;

                float lengthSquared = b2LengthSquared(joint.linearImpulse);
                if (lengthSquared > maxImpulse * maxImpulse)
                {
                    joint.linearImpulse = b2MulSV(maxImpulse, b2Normalize(joint.linearImpulse));
                }

                impulse.Y = joint.linearImpulse.Y - oldImpulse.Y;
                impulse.X = joint.linearImpulse.X - oldImpulse.X;

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
    }
}