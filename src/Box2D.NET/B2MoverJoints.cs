// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
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
    public static class B2MoverJoints
    {
        /**
         * @defgroup mover_joint Mover Joint
         * @brief Functions for the mover joint.
         *
         * The mover joint is designed for a dynamic character mover.
         * @{
         */
        /// Set the desired relative linear velocity in meters per second
        public static void b2MoverJoint_SetLinearVelocity(B2JointId jointId, B2Vec2 velocity)
        {
            if (B2_CHECK_INPUT(b2IsValidVec2(velocity)) == false) return;

            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_moverJoint);
            joint.uj.moverJoint.linearVelocity = velocity;
        }

        /// Get the desired relative linear velocity in meters per second
        public static B2Vec2 b2MoverJoint_GetLinearVelocity(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_moverJoint);
            return joint.uj.moverJoint.linearVelocity;
        }

        /// Set the motor joint maximum force, usually in newtons
        public static void b2MoverJoint_SetMaxVelocityForce(B2JointId jointId, B2Vec2 maxForce)
        {
            if (B2_CHECK_INPUT(b2IsValidVec2(maxForce)) == false) return;

            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_moverJoint);
            joint.uj.moverJoint.maxVelocityForce = maxForce;
        }

        /// Get the motor joint maximum force, usually in newtons
        public static B2Vec2 b2MoverJoint_GetMaxVelocityForce(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_moverJoint);
            return joint.uj.moverJoint.maxVelocityForce;
        }

        /**@}*/

        internal static B2Vec2 b2GetMoverJointForce(B2World world, B2JointSim @base)
        {
            B2Vec2 force = b2MulSV(world.inv_h, @base.uj.moverJoint.linearVelocityImpulse);
            return force;
        }

        internal static void b2PrepareMoverJoint(B2JointSim @base, B2StepContext context)
        {
            B2_ASSERT(@base.type == B2JointType.b2_moverJoint);

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
            float mB = bodySimB.invMass;

            @base.invMassA = mA;
            @base.invMassB = mB;
            @base.invIA = 0.0f;
            @base.invIB = 0.0f;

            ref B2MoverJoint joint = ref @base.uj.moverJoint;
            joint.indexA = bodyA.setIndex == (int)B2SolverSetType.b2_awakeSet ? localIndexA : B2_NULL_INDEX;
            joint.indexB = bodyB.setIndex == (int)B2SolverSetType.b2_awakeSet ? localIndexB : B2_NULL_INDEX;

            float k = mA + mB;
            joint.linearMass = k > 0.0f ? 1.0f / k : 0.0f;

            if (context.enableWarmStarting == false)
            {
                joint.linearVelocityImpulse = b2Vec2_zero;
            }
        }

        internal static void b2WarmStartMoverJoint(B2JointSim @base, B2StepContext context)
        {
            B2_ASSERT(@base.type == B2JointType.b2_moverJoint);

            float mA = @base.invMassA;
            float mB = @base.invMassB;

            ref readonly B2MoverJoint joint = ref @base.uj.moverJoint;

            // dummy state for static bodies
            B2BodyState dummyState = B2BodyState.Create(b2_identityBodyState);

            B2BodyState stateA = joint.indexA == B2_NULL_INDEX ? dummyState : context.states[joint.indexA];
            B2BodyState stateB = joint.indexB == B2_NULL_INDEX ? dummyState : context.states[joint.indexB];

            if (0 != (stateA.flags & (uint)B2BodyFlags.b2_dynamicFlag))
            {
                stateA.linearVelocity = b2MulSub(stateA.linearVelocity, mA, joint.linearVelocityImpulse);
            }

            if (0 != (stateB.flags & (uint)B2BodyFlags.b2_dynamicFlag))
            {
                stateB.linearVelocity = b2MulAdd(stateB.linearVelocity, mB, joint.linearVelocityImpulse);
            }
        }

        internal static void b2SolveMoverJoint(B2JointSim @base, B2StepContext context)
        {
            B2_ASSERT(@base.type == B2JointType.b2_moverJoint);

            float mA = @base.invMassA;
            float mB = @base.invMassB;

            // dummy state for static bodies
            B2BodyState dummyState = B2BodyState.Create(b2_identityBodyState);

            ref B2MoverJoint joint = ref @base.uj.moverJoint;
            B2BodyState stateA = joint.indexA == B2_NULL_INDEX ? dummyState : context.states[joint.indexA];
            B2BodyState stateB = joint.indexB == B2_NULL_INDEX ? dummyState : context.states[joint.indexB];

            B2Vec2 vA = stateA.linearVelocity;
            B2Vec2 vB = stateB.linearVelocity;

            // linear velocity
            if (joint.maxVelocityForce.X > 0.0f || joint.maxVelocityForce.Y > 0.0f)
            {
                B2Vec2 cdot = b2Sub(vB, vA);
                cdot = b2Sub(cdot, joint.linearVelocity);
                B2Vec2 b = b2MulSV(joint.linearMass, cdot);
                B2Vec2 impulse = new B2Vec2(-b.X, -b.Y);

                B2Vec2 oldImpulse = joint.linearVelocityImpulse;
                joint.linearVelocityImpulse = b2Add(joint.linearVelocityImpulse, impulse);
                B2Vec2 maxImpulse = b2MulSV(context.h, joint.maxVelocityForce);

                // clamp x and y separately
                joint.linearVelocityImpulse = b2Clamp(joint.linearVelocityImpulse, b2Neg(maxImpulse), maxImpulse);

                impulse = b2Sub(joint.linearVelocityImpulse, oldImpulse);
                vA = b2MulSub(vA, mA, impulse);
                vB = b2MulAdd(vB, mB, impulse);
            }
            else
            {
                joint.linearVelocityImpulse = b2Vec2_zero;
            }

            if (0 != (stateA.flags & (uint)B2BodyFlags.b2_dynamicFlag))
            {
                stateA.linearVelocity = vA;
            }

            if (0 != (stateB.flags & (uint)B2BodyFlags.b2_dynamicFlag))
            {
                stateB.linearVelocity = vB;
            }
        }
    }
}
