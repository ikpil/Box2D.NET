// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Solvers;

namespace Box2D.NET
{
    public static class B2PogoJoints
    {
        private static readonly B2Vec2 b2_pogoAxis = new B2Vec2(0.0f, 1.0f);

        /**
         * @defgroup pogo_joint Pogo Joint
         * @brief Functions for the pogo joint. Used for dynamic character movers.
         * @{
         */
        /// Set the rest length of a pogo joint
        /// @param jointId The id for a pogo joint
        /// @param length The new pogo joint length
        public static void b2PogoJoint_SetRestLength(B2JointId jointId, float length)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_pogoJoint);
            joint.uj.pogoJoint.restLength = length;
        }

        /// Get the rest length of a pogo joint
        public static float b2PogoJoint_GetRestLength(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_pogoJoint);
            return joint.uj.pogoJoint.restLength;
        }

        /// Get the spring Hertz
        public static float b2PogoJoint_GetSpringHertz(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_pogoJoint);
            return joint.uj.pogoJoint.hertz;
        }

        /// Set the spring stiffness in Hertz
        public static void b2PogoJoint_SetSpringHertz(B2JointId jointId, float hertz)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_pogoJoint);
            joint.uj.pogoJoint.hertz = hertz;
        }

        /// Set the spring damping ratio, non-dimensional
        public static void b2PogoJoint_SetSpringDampingRatio(B2JointId jointId, float dampingRatio)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_pogoJoint);
            joint.uj.pogoJoint.dampingRatio = dampingRatio;
        }

        /// Get the spring damping ratio
        public static float b2PogoJoint_GetSpringDampingRatio(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_pogoJoint);
            return joint.uj.pogoJoint.dampingRatio;
        }

        /// Get the current length of the pogo.
        public static float b2PogoJoint_GetLength(B2JointId jointId)
        {
            B2World world = b2GetWorld(jointId.world0);
            B2JointSim jointSim = b2GetJointSimCheckType(jointId, B2JointType.b2_pogoJoint);

            // Relative to body A so the difference stays in float precision far from the origin
            B2WorldTransform wxfA = b2GetBodyTransform(world, jointSim.bodyIdA);
            B2Transform transformA = b2ToRelativeTransform(wxfA, wxfA.p);
            B2Transform transformB = b2ToRelativeTransform(b2GetBodyTransform(world, jointSim.bodyIdB), wxfA.p);

            B2Vec2 axis = b2RotateVector(jointSim.localFrameB.q, b2_pogoAxis);
            axis = b2RotateVector(transformB.q, axis);
            B2Vec2 pA = b2TransformPoint(transformA, jointSim.localFrameA.p);
            B2Vec2 pB = b2TransformPoint(transformB, jointSim.localFrameB.p);
            B2Vec2 d = b2Sub(pB, pA);
            float length = b2Dot(d, axis);
            return length;
        }

        /// Get the internal pogo impulse.
        public static float b2PogoJoint_GetImpulse(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_pogoJoint);
            return joint.uj.pogoJoint.impulse;
        }

        /// Get the internal pogo velocity.
        public static float b2PogoJoint_GetVelocity(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_pogoJoint);
            return joint.uj.pogoJoint.velocity;
        }

        /** @} */

        internal static B2Vec2 b2GetPogoJointForce(B2World world, B2JointSim @base)
        {
            B2Vec2 force = b2MulSV(world.inv_h * @base.uj.pogoJoint.impulse, @base.uj.pogoJoint.normal);
            return force;
        }

        internal static void b2PreparePogoJoint(B2JointSim @base, B2StepContext context)
        {
            B2_ASSERT(@base.type == B2JointType.b2_pogoJoint);

            B2World world = context.world;

            // chase body id to the solver set where the body lives
            int idA = @base.bodyIdA;
            int idB = @base.bodyIdB;

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

            ref B2PogoJoint joint = ref @base.uj.pogoJoint;
            joint.indexA = bodyA.setIndex == (int)B2SolverSetType.b2_awakeSet ? localIndexA : B2_NULL_INDEX;
            joint.indexB = bodyB.setIndex == (int)B2SolverSetType.b2_awakeSet ? localIndexB : B2_NULL_INDEX;

            // Compute joint anchor frames with world space rotation, relative to center of mass
            joint.frameA.q = b2MulRot(bodySimA.transform.q, @base.localFrameA.q);
            joint.frameA.p = b2RotateVector(bodySimA.transform.q, b2Sub(@base.localFrameA.p, bodySimA.localCenter));
            joint.frameB.q = b2MulRot(bodySimB.transform.q, @base.localFrameB.q);
            joint.frameB.p = b2RotateVector(bodySimB.transform.q, b2Sub(@base.localFrameB.p, bodySimB.localCenter));

            // Compute the initial center delta. Incremental position updates are relative to this.
            joint.deltaCenter = b2SubPos(bodySimB.center, bodySimA.center);

            B2Vec2 rA = joint.frameA.p;
            B2Vec2 rB = joint.frameB.p;

            // compute effective mass
            float crA = b2Cross(rA, joint.normal);
            float crB = b2Cross(rB, joint.normal);
            float k = mA + mB + iA * crA * crA + iB * crB * crB;
            joint.linearMass = k > 0.0f ? 1.0f / k : 0.0f;

            if (context.enableWarmStarting == false)
            {
                joint.impulse = 0.0f;
            }
        }

        internal static void b2WarmStartPogoJoint(B2JointSim @base, B2StepContext context)
        {
            B2_ASSERT(@base.type == B2JointType.b2_pogoJoint);

            float mA = @base.invMassA;
            float mB = @base.invMassB;
            float iA = @base.invIA;
            float iB = @base.invIB;

            ref B2PogoJoint joint = ref @base.uj.pogoJoint;

            if (joint.hertz == 0.0f)
            {
                joint.impulse = 0.0f;
                return;
            }

            // dummy state for static bodies
            B2BodyState dummyState = B2BodyState.Create(b2_identityBodyState);

            B2BodyState stateA = joint.indexA == B2_NULL_INDEX ? dummyState : context.states[joint.indexA];
            B2BodyState stateB = joint.indexB == B2_NULL_INDEX ? dummyState : context.states[joint.indexB];
            B2Vec2 rA = b2RotateVector(stateA.deltaRotation, joint.frameA.p);
            B2Vec2 rB = b2RotateVector(stateB.deltaRotation, joint.frameB.p);

            B2Vec2 linearImpulse = b2MulSV(joint.impulse, joint.normal);

            if (0 != (stateA.flags & (uint)B2BodyFlags.b2_dynamicFlag))
            {
                stateA.linearVelocity = b2MulSub(stateA.linearVelocity, mA, linearImpulse);
                stateA.angularVelocity -= iA * b2Cross(rA, linearImpulse);
            }

            if (0 != (stateB.flags & (uint)B2BodyFlags.b2_dynamicFlag))
            {
                stateB.linearVelocity = b2MulAdd(stateB.linearVelocity, mB, linearImpulse);
                stateB.angularVelocity += iB * b2Cross(rB, linearImpulse);
            }
        }

        internal static void b2SolvePogoJoint(B2JointSim @base, B2StepContext context, bool useBias)
        {
            B2_ASSERT(@base.type == B2JointType.b2_pogoJoint);

            float mA = @base.invMassA;
            float mB = @base.invMassB;
            float iA = @base.invIA;
            float iB = @base.invIB;

            // dummy state for static bodies
            B2BodyState dummyState = B2BodyState.Create(b2_identityBodyState);

            ref B2PogoJoint joint = ref @base.uj.pogoJoint;
            if (joint.hertz == 0.0f)
            {
                joint.impulse = 0.0f;
                return;
            }

            B2BodyState stateA = joint.indexA == B2_NULL_INDEX ? dummyState : context.states[joint.indexA];
            B2BodyState stateB = joint.indexB == B2_NULL_INDEX ? dummyState : context.states[joint.indexB];

            B2Vec2 vA = stateA.linearVelocity;
            float wA = stateA.angularVelocity;
            B2Vec2 vB = stateB.linearVelocity;
            float wB = stateB.angularVelocity;

            B2Vec2 rA = b2RotateVector(stateA.deltaRotation, joint.frameA.p);
            B2Vec2 rB = b2RotateVector(stateB.deltaRotation, joint.frameB.p);

            float bias = 0.0f;
            if (useBias)
            {
                // This is a custom pogo position correction designed to avoid feeding velocity into
                // the mover body. Otherwise the mover can make huge jumps going up stairs. This still
                // works with continuous collision.
                // However, this position delta is not observer contact forces and can lead to tunnelling.
                B2Vec2 dcA = stateA.deltaPosition;
                B2Vec2 dcB = stateB.deltaPosition;
                B2Vec2 d = b2Add(b2Add(b2Sub(dcB, dcA), b2Sub(rB, rA)), joint.deltaCenter);

                // The pogo axis is usually fixed as the world y-axis. Different than the ground normal.
                B2Vec2 pogoAxis = b2RotateVector(joint.frameB.q, b2_pogoAxis);
                float c = b2Dot(pogoAxis, d) - joint.restLength;

                // This could be divided by dot(joint->normal, pogoAxis) to account for the constraint
                // direction, but I'd rather diminish the pogo recovery rate than risk making it huge.
                joint.velocity = b2SpringDamper(joint.hertz, joint.dampingRatio, c, joint.velocity, context.h);
                bias = -joint.velocity;
            }

            B2Vec2 vr = b2Sub(b2Add(vB, b2CrossSV(wB, rB)), b2Add(vA, b2CrossSV(wA, rA)));
            float cdot = b2Dot(joint.normal, vr);

            float maxTensionImpulse = context.h * joint.maxTensionForce;
            float maxCompressionImpulse = context.h * joint.maxCompressionForce;
            float oldImpulse = joint.impulse;
            float impulse = -joint.linearMass * (cdot + bias);
            joint.impulse = b2ClampFloat(joint.impulse + impulse, -maxTensionImpulse, maxCompressionImpulse);
            impulse = joint.impulse - oldImpulse;

            B2Vec2 P = b2MulSV(impulse, joint.normal);
            vA = b2MulSub(vA, mA, P);
            wA -= iA * b2Cross(rA, P);
            vB = b2MulAdd(vB, mB, P);
            wB += iB * b2Cross(rB, P);

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
    }
}
