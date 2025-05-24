// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Cores;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Profiling;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Contacts;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2DistanceJoints;
using static Box2D.NET.B2MotorJoints;
using static Box2D.NET.B2MouseJoints;
using static Box2D.NET.B2PrismaticJoints;
using static Box2D.NET.B2RevoluteJoints;
using static Box2D.NET.B2WeldJoints;
using static Box2D.NET.B2WheelJoints;
using static Box2D.NET.B2IdPools;
using static Box2D.NET.B2SolverSets;
using static Box2D.NET.B2ConstraintGraphs;
using static Box2D.NET.B2Islands;
using static Box2D.NET.B2BoardPhases;


namespace Box2D.NET
{
    public static class B2Joints
    {
        /// Use this to initialize your joint definition
        /// @ingroup distance_joint
        public static B2DistanceJointDef b2DefaultDistanceJointDef()
        {
            B2DistanceJointDef def = new B2DistanceJointDef();
            def.length = 1.0f;
            def.maxLength = B2_HUGE;
            def.internalValue = B2_SECRET_COOKIE;
            return def;
        }

        public static B2MotorJointDef b2DefaultMotorJointDef()
        {
            B2MotorJointDef def = new B2MotorJointDef();
            def.maxForce = 1.0f;
            def.maxTorque = 1.0f;
            def.correctionFactor = 0.3f;
            def.internalValue = B2_SECRET_COOKIE;
            return def;
        }

        public static B2MouseJointDef b2DefaultMouseJointDef()
        {
            B2MouseJointDef def = new B2MouseJointDef();
            def.hertz = 4.0f;
            def.dampingRatio = 1.0f;
            def.maxForce = 1.0f;
            def.internalValue = B2_SECRET_COOKIE;
            return def;
        }

        /// Use this to initialize your joint definition
        /// @ingroup filter_joint
        public static b2FilterJointDef b2DefaultFilterJointDef()
        {
            b2FilterJointDef def = new b2FilterJointDef();
            def.internalValue = B2_SECRET_COOKIE;
            return def;
        }

        public static B2PrismaticJointDef b2DefaultPrismaticJointDef()
        {
            B2PrismaticJointDef def = new B2PrismaticJointDef();
            def.localAxisA = new B2Vec2(1.0f, 0.0f);
            def.internalValue = B2_SECRET_COOKIE;
            return def;
        }

        public static B2RevoluteJointDef b2DefaultRevoluteJointDef()
        {
            B2RevoluteJointDef def = new B2RevoluteJointDef();
            def.drawSize = 0.25f;
            def.internalValue = B2_SECRET_COOKIE;
            return def;
        }

        public static B2WeldJointDef b2DefaultWeldJointDef()
        {
            B2WeldJointDef def = new B2WeldJointDef();
            def.internalValue = B2_SECRET_COOKIE;
            return def;
        }

        public static B2WheelJointDef b2DefaultWheelJointDef()
        {
            B2WheelJointDef def = new B2WheelJointDef();
            def.localAxisA.Y = 1.0f;
            def.enableSpring = true;
            def.hertz = 1.0f;
            def.dampingRatio = 0.7f;
            def.internalValue = B2_SECRET_COOKIE;
            return def;
        }

        public static B2ExplosionDef b2DefaultExplosionDef()
        {
            B2ExplosionDef def = new B2ExplosionDef();
            def.maskBits = B2_DEFAULT_MASK_BITS;
            return def;
        }

        public static B2Joint b2GetJointFullId(B2World world, B2JointId jointId)
        {
            int id = jointId.index1 - 1;
            B2Joint joint = b2Array_Get(ref world.joints, id);
            B2_ASSERT(joint.jointId == id && joint.generation == jointId.generation);
            return joint;
        }

        public static B2JointSim b2GetJointSim(B2World world, B2Joint joint)
        {
            if (joint.setIndex == (int)B2SetType.b2_awakeSet)
            {
                B2_ASSERT(0 <= joint.colorIndex && joint.colorIndex < B2_GRAPH_COLOR_COUNT);
                ref B2GraphColor color = ref world.constraintGraph.colors[joint.colorIndex];
                return b2Array_Get(ref color.jointSims, joint.localIndex);
            }

            B2SolverSet set = b2Array_Get(ref world.solverSets, joint.setIndex);
            return b2Array_Get(ref set.jointSims, joint.localIndex);
        }

        public static B2JointSim b2GetJointSimCheckType(B2JointId jointId, B2JointType type)
        {
            B2_UNUSED(type);

            B2World world = b2GetWorld(jointId.world0);
            B2_ASSERT(world.locked == false);
            if (world.locked)
            {
                return null;
            }

            B2Joint joint = b2GetJointFullId(world, jointId);
            B2_ASSERT(joint.type == type);
            B2JointSim jointSim = b2GetJointSim(world, joint);
            B2_ASSERT(jointSim.type == type);
            return jointSim;
        }


        public static B2JointPair b2CreateJoint(B2World world, B2Body bodyA, B2Body bodyB, object userData, float drawSize, B2JointType type, bool collideConnected)
        {
            int bodyIdA = bodyA.id;
            int bodyIdB = bodyB.id;
            int maxSetIndex = b2MaxInt(bodyA.setIndex, bodyB.setIndex);

            // Create joint id and joint
            int jointId = b2AllocId(world.jointIdPool);
            if (jointId == world.joints.count)
            {
                b2Array_Push(ref world.joints, new B2Joint());
            }

            B2Joint joint = b2Array_Get(ref world.joints, jointId);
            joint.jointId = jointId;
            joint.userData = userData;
            joint.generation += 1;
            joint.setIndex = B2_NULL_INDEX;
            joint.colorIndex = B2_NULL_INDEX;
            joint.localIndex = B2_NULL_INDEX;
            joint.islandId = B2_NULL_INDEX;
            joint.islandPrev = B2_NULL_INDEX;
            joint.islandNext = B2_NULL_INDEX;
            joint.drawSize = drawSize;
            joint.type = type;
            joint.collideConnected = collideConnected;
            joint.isMarked = false;

            // Doubly linked list on bodyA
            joint.edges[0].bodyId = bodyIdA;
            joint.edges[0].prevKey = B2_NULL_INDEX;
            joint.edges[0].nextKey = bodyA.headJointKey;

            int keyA = (jointId << 1) | 0;
            if (bodyA.headJointKey != B2_NULL_INDEX)
            {
                B2Joint jointA = b2Array_Get(ref world.joints, bodyA.headJointKey >> 1);
                ref B2JointEdge edgeA = ref jointA.edges[bodyA.headJointKey & 1];
                edgeA.prevKey = keyA;
            }

            bodyA.headJointKey = keyA;
            bodyA.jointCount += 1;

            // Doubly linked list on bodyB
            joint.edges[1].bodyId = bodyIdB;
            joint.edges[1].prevKey = B2_NULL_INDEX;
            joint.edges[1].nextKey = bodyB.headJointKey;

            int keyB = (jointId << 1) | 1;
            if (bodyB.headJointKey != B2_NULL_INDEX)
            {
                B2Joint jointB = b2Array_Get(ref world.joints, bodyB.headJointKey >> 1);
                ref B2JointEdge edgeB = ref jointB.edges[(bodyB.headJointKey & 1)];
                edgeB.prevKey = keyB;
            }

            bodyB.headJointKey = keyB;
            bodyB.jointCount += 1;

            B2JointSim jointSim = null;

            if (bodyA.setIndex == (int)B2SetType.b2_disabledSet || bodyB.setIndex == (int)B2SetType.b2_disabledSet)
            {
                // if either body is disabled, create in disabled set
                B2SolverSet set = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_disabledSet);
                joint.setIndex = (int)B2SetType.b2_disabledSet;
                joint.localIndex = set.jointSims.count;

                jointSim = b2Array_Add(ref set.jointSims);
                //memset( jointSim, 0, sizeof( b2JointSim ) );
                jointSim.Clear();

                jointSim.jointId = jointId;
                jointSim.bodyIdA = bodyIdA;
                jointSim.bodyIdB = bodyIdB;
            }
            else if (bodyA.setIndex == (int)B2SetType.b2_staticSet && bodyB.setIndex == (int)B2SetType.b2_staticSet)
            {
                // joint is connecting static bodies
                B2SolverSet set = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_staticSet);
                joint.setIndex = (int)B2SetType.b2_staticSet;
                joint.localIndex = set.jointSims.count;

                jointSim = b2Array_Add(ref set.jointSims);
                //memset( jointSim, 0, sizeof( b2JointSim ) );
                jointSim.Clear();

                jointSim.jointId = jointId;
                jointSim.bodyIdA = bodyIdA;
                jointSim.bodyIdB = bodyIdB;
            }
            else if (bodyA.setIndex == (int)B2SetType.b2_awakeSet || bodyB.setIndex == (int)B2SetType.b2_awakeSet)
            {
                // if either body is sleeping, wake it
                if (maxSetIndex >= (int)B2SetType.b2_firstSleepingSet)
                {
                    b2WakeSolverSet(world, maxSetIndex);
                }

                joint.setIndex = (int)B2SetType.b2_awakeSet;

                jointSim = b2CreateJointInGraph(world, joint);
                jointSim.jointId = jointId;
                jointSim.bodyIdA = bodyIdA;
                jointSim.bodyIdB = bodyIdB;
            }
            else
            {
                // joint connected between sleeping and/or static bodies
                B2_ASSERT(bodyA.setIndex >= (int)B2SetType.b2_firstSleepingSet || bodyB.setIndex >= (int)B2SetType.b2_firstSleepingSet);
                B2_ASSERT(bodyA.setIndex != (int)B2SetType.b2_staticSet || bodyB.setIndex != (int)B2SetType.b2_staticSet);

                // joint should go into the sleeping set (not static set)
                int setIndex = maxSetIndex;

                B2SolverSet set = b2Array_Get(ref world.solverSets, setIndex);
                joint.setIndex = setIndex;
                joint.localIndex = set.jointSims.count;

                jointSim = b2Array_Add(ref set.jointSims);
                //memset( jointSim, 0, sizeof( b2JointSim ) );
                jointSim.Clear();

                jointSim.jointId = jointId;
                jointSim.bodyIdA = bodyIdA;
                jointSim.bodyIdB = bodyIdB;

                if (bodyA.setIndex != bodyB.setIndex && bodyA.setIndex >= (int)B2SetType.b2_firstSleepingSet &&
                    bodyB.setIndex >= (int)B2SetType.b2_firstSleepingSet)
                {
                    // merge sleeping sets
                    b2MergeSolverSets(world, bodyA.setIndex, bodyB.setIndex);
                    B2_ASSERT(bodyA.setIndex == bodyB.setIndex);

                    // fix potentially invalid set index
                    setIndex = bodyA.setIndex;

                    B2SolverSet mergedSet = b2Array_Get(ref world.solverSets, setIndex);

                    // Careful! The joint sim pointer was orphaned by the set merge.
                    jointSim = b2Array_Get(ref mergedSet.jointSims, joint.localIndex);
                }

                B2_ASSERT(joint.setIndex == setIndex);
            }

            B2_ASSERT(jointSim.jointId == jointId);
            B2_ASSERT(jointSim.bodyIdA == bodyIdA);
            B2_ASSERT(jointSim.bodyIdB == bodyIdB);

            if (joint.setIndex > (int)B2SetType.b2_disabledSet)
            {
                // Add edge to island graph
                bool mergeIslands = true;
                b2LinkJoint(world, joint, mergeIslands);
            }

            b2ValidateSolverSets(world);

            return new B2JointPair(joint, jointSim);
        }

        public static void b2DestroyContactsBetweenBodies(B2World world, B2Body bodyA, B2Body bodyB)
        {
            int contactKey;
            int otherBodyId;

            // use the smaller of the two contact lists
            if (bodyA.contactCount < bodyB.contactCount)
            {
                contactKey = bodyA.headContactKey;
                otherBodyId = bodyB.id;
            }
            else
            {
                contactKey = bodyB.headContactKey;
                otherBodyId = bodyA.id;
            }

            // no need to wake bodies when a joint removes collision between them
            bool wakeBodies = false;

            // destroy the contacts
            while (contactKey != B2_NULL_INDEX)
            {
                int contactId = contactKey >> 1;
                int edgeIndex = contactKey & 1;

                B2Contact contact = b2Array_Get(ref world.contacts, contactId);
                contactKey = contact.edges[edgeIndex].nextKey;

                int otherEdgeIndex = edgeIndex ^ 1;
                if (contact.edges[otherEdgeIndex].bodyId == otherBodyId)
                {
                    // Careful, this removes the contact from the current doubly linked list
                    b2DestroyContact(world, contact, wakeBodies);
                }
            }

            b2ValidateSolverSets(world);
        }

        public static B2JointId b2CreateDistanceJoint(B2WorldId worldId, ref B2DistanceJointDef def)
        {
            B2_CHECK_DEF(ref def);
            B2World world = b2GetWorldFromId(worldId);

            B2_ASSERT(world.locked == false);

            if (world.locked)
            {
                return new B2JointId();
            }

            B2_ASSERT(b2Body_IsValid(def.bodyIdA));
            B2_ASSERT(b2Body_IsValid(def.bodyIdB));
            B2_ASSERT(b2IsValidFloat(def.length) && def.length > 0.0f);

            B2Body bodyA = b2GetBodyFullId(world, def.bodyIdA);
            B2Body bodyB = b2GetBodyFullId(world, def.bodyIdB);

            B2JointPair pair = b2CreateJoint(world, bodyA, bodyB, def.userData, 1.0f, B2JointType.b2_distanceJoint, def.collideConnected);

            B2JointSim joint = pair.jointSim;
            joint.type = B2JointType.b2_distanceJoint;
            joint.localOriginAnchorA = def.localAnchorA;
            joint.localOriginAnchorB = def.localAnchorB;

            B2DistanceJoint empty = new B2DistanceJoint();
            joint.uj.distanceJoint = empty;
            joint.uj.distanceJoint.length = b2MaxFloat(def.length, B2_LINEAR_SLOP);
            joint.uj.distanceJoint.hertz = def.hertz;
            joint.uj.distanceJoint.dampingRatio = def.dampingRatio;
            joint.uj.distanceJoint.minLength = b2MaxFloat(def.minLength, B2_LINEAR_SLOP);
            joint.uj.distanceJoint.maxLength = b2MaxFloat(def.minLength, def.maxLength);
            joint.uj.distanceJoint.maxMotorForce = def.maxMotorForce;
            joint.uj.distanceJoint.motorSpeed = def.motorSpeed;
            joint.uj.distanceJoint.enableSpring = def.enableSpring;
            joint.uj.distanceJoint.enableLimit = def.enableLimit;
            joint.uj.distanceJoint.enableMotor = def.enableMotor;
            joint.uj.distanceJoint.impulse = 0.0f;
            joint.uj.distanceJoint.lowerImpulse = 0.0f;
            joint.uj.distanceJoint.upperImpulse = 0.0f;
            joint.uj.distanceJoint.motorImpulse = 0.0f;

            // If the joint prevents collisions, then destroy all contacts between attached bodies
            if (def.collideConnected == false)
            {
                b2DestroyContactsBetweenBodies(world, bodyA, bodyB);
            }

            B2JointId jointId = new B2JointId(joint.jointId + 1, world.worldId, pair.joint.generation);
            return jointId;
        }

        public static B2JointId b2CreateMotorJoint(B2WorldId worldId, ref B2MotorJointDef def)
        {
            B2_CHECK_DEF(ref def);
            B2World world = b2GetWorldFromId(worldId);

            B2_ASSERT(world.locked == false);

            if (world.locked)
            {
                return new B2JointId();
            }

            B2Body bodyA = b2GetBodyFullId(world, def.bodyIdA);
            B2Body bodyB = b2GetBodyFullId(world, def.bodyIdB);

            B2JointPair pair = b2CreateJoint(world, bodyA, bodyB, def.userData, 1.0f, B2JointType.b2_motorJoint, def.collideConnected);
            B2JointSim joint = pair.jointSim;

            joint.type = B2JointType.b2_motorJoint;
            joint.localOriginAnchorA = new B2Vec2(0.0f, 0.0f);
            joint.localOriginAnchorB = new B2Vec2(0.0f, 0.0f);
            joint.uj.motorJoint = new B2MotorJoint();
            joint.uj.motorJoint.linearOffset = def.linearOffset;
            joint.uj.motorJoint.angularOffset = def.angularOffset;
            joint.uj.motorJoint.maxForce = def.maxForce;
            joint.uj.motorJoint.maxTorque = def.maxTorque;
            joint.uj.motorJoint.correctionFactor = b2ClampFloat(def.correctionFactor, 0.0f, 1.0f);

            // If the joint prevents collisions, then destroy all contacts between attached bodies
            if (def.collideConnected == false)
            {
                b2DestroyContactsBetweenBodies(world, bodyA, bodyB);
            }

            B2JointId jointId = new B2JointId(joint.jointId + 1, world.worldId, pair.joint.generation);
            return jointId;
        }

        public static B2JointId b2CreateMouseJoint(B2WorldId worldId, ref B2MouseJointDef def)
        {
            B2_CHECK_DEF(ref def);
            B2World world = b2GetWorldFromId(worldId);

            B2_ASSERT(world.locked == false);

            if (world.locked)
            {
                return new B2JointId();
            }

            B2Body bodyA = b2GetBodyFullId(world, def.bodyIdA);
            B2Body bodyB = b2GetBodyFullId(world, def.bodyIdB);

            B2Transform transformA = b2GetBodyTransformQuick(world, bodyA);
            B2Transform transformB = b2GetBodyTransformQuick(world, bodyB);

            B2JointPair pair = b2CreateJoint(world, bodyA, bodyB, def.userData, 1.0f, B2JointType.b2_mouseJoint, def.collideConnected);

            B2JointSim joint = pair.jointSim;
            joint.type = B2JointType.b2_mouseJoint;
            joint.localOriginAnchorA = b2InvTransformPoint(transformA, def.target);
            joint.localOriginAnchorB = b2InvTransformPoint(transformB, def.target);

            B2MouseJoint empty = new B2MouseJoint();
            joint.uj.mouseJoint = empty;
            joint.uj.mouseJoint.targetA = def.target;
            joint.uj.mouseJoint.hertz = def.hertz;
            joint.uj.mouseJoint.dampingRatio = def.dampingRatio;
            joint.uj.mouseJoint.maxForce = def.maxForce;

            B2JointId jointId = new B2JointId(joint.jointId + 1, world.worldId, pair.joint.generation);
            return jointId;
        }

        /**
         * @defgroup filter_joint Filter Joint
         * @brief Functions for the filter joint.
         *
         * The filter joint is used to disable collision between two bodies. As a side effect of being a joint, it also
         * keeps the two bodies in the same simulation island.
         * @{
         */
        /// Create a filter joint.
        /// @see b2FilterJointDef for details
        public static B2JointId b2CreateFilterJoint(B2WorldId worldId, ref b2FilterJointDef def)
        {
            B2_CHECK_DEF(ref def);
            B2World world = b2GetWorldFromId(worldId);

            B2_ASSERT(world.locked == false);

            if (world.locked)
            {
                return new B2JointId();
            }

            B2Body bodyA = b2GetBodyFullId(world, def.bodyIdA);
            B2Body bodyB = b2GetBodyFullId(world, def.bodyIdB);

            bool collideConnected = false;
            B2JointPair pair = b2CreateJoint(world, bodyA, bodyB, def.userData, 1.0f, B2JointType.b2_filterJoint, collideConnected);

            B2JointSim joint = pair.jointSim;
            joint.type = B2JointType.b2_filterJoint;
            joint.localOriginAnchorA = b2Vec2_zero;
            joint.localOriginAnchorB = b2Vec2_zero;

            B2JointId jointId = new B2JointId(joint.jointId + 1, world.worldId, pair.joint.generation);
            return jointId;
        }

        public static B2JointId b2CreateRevoluteJoint(B2WorldId worldId, ref B2RevoluteJointDef def)
        {
            B2_CHECK_DEF(ref def);
            B2_ASSERT(def.lowerAngle <= def.upperAngle);
            B2_ASSERT(def.lowerAngle >= -0.99f * B2_PI);
            B2_ASSERT(def.upperAngle <= 0.99f * B2_PI);

            B2World world = b2GetWorldFromId(worldId);

            B2_ASSERT(world.locked == false);

            if (world.locked)
            {
                return new B2JointId();
            }

            B2Body bodyA = b2GetBodyFullId(world, def.bodyIdA);
            B2Body bodyB = b2GetBodyFullId(world, def.bodyIdB);

            B2JointPair pair =
                b2CreateJoint(world, bodyA, bodyB, def.userData, def.drawSize, B2JointType.b2_revoluteJoint, def.collideConnected);

            B2JointSim joint = pair.jointSim;
            joint.type = B2JointType.b2_revoluteJoint;
            joint.localOriginAnchorA = def.localAnchorA;
            joint.localOriginAnchorB = def.localAnchorB;

            B2RevoluteJoint empty = new B2RevoluteJoint();
            joint.uj.revoluteJoint = empty;

            joint.uj.revoluteJoint.referenceAngle = b2ClampFloat(def.referenceAngle, -B2_PI, B2_PI);
            joint.uj.revoluteJoint.linearImpulse = b2Vec2_zero;
            joint.uj.revoluteJoint.axialMass = 0.0f;
            joint.uj.revoluteJoint.springImpulse = 0.0f;
            joint.uj.revoluteJoint.motorImpulse = 0.0f;
            joint.uj.revoluteJoint.lowerImpulse = 0.0f;
            joint.uj.revoluteJoint.upperImpulse = 0.0f;
            joint.uj.revoluteJoint.hertz = def.hertz;
            joint.uj.revoluteJoint.dampingRatio = def.dampingRatio;
            joint.uj.revoluteJoint.lowerAngle = def.lowerAngle;
            joint.uj.revoluteJoint.upperAngle = def.upperAngle;
            joint.uj.revoluteJoint.maxMotorTorque = def.maxMotorTorque;
            joint.uj.revoluteJoint.motorSpeed = def.motorSpeed;
            joint.uj.revoluteJoint.enableSpring = def.enableSpring;
            joint.uj.revoluteJoint.enableLimit = def.enableLimit;
            joint.uj.revoluteJoint.enableMotor = def.enableMotor;

            // If the joint prevents collisions, then destroy all contacts between attached bodies
            if (def.collideConnected == false)
            {
                b2DestroyContactsBetweenBodies(world, bodyA, bodyB);
            }

            B2JointId jointId = new B2JointId(joint.jointId + 1, world.worldId, pair.joint.generation);
            return jointId;
        }

        public static B2JointId b2CreatePrismaticJoint(B2WorldId worldId, B2PrismaticJointDef def)
        {
            B2_CHECK_DEF(ref def);
            B2_ASSERT(def.lowerTranslation <= def.upperTranslation);

            B2World world = b2GetWorldFromId(worldId);

            B2_ASSERT(world.locked == false);

            if (world.locked)
            {
                return new B2JointId();
            }

            B2Body bodyA = b2GetBodyFullId(world, def.bodyIdA);
            B2Body bodyB = b2GetBodyFullId(world, def.bodyIdB);

            B2JointPair pair = b2CreateJoint(world, bodyA, bodyB, def.userData, 1.0f, B2JointType.b2_prismaticJoint, def.collideConnected);

            B2JointSim joint = pair.jointSim;
            joint.type = B2JointType.b2_prismaticJoint;
            joint.localOriginAnchorA = def.localAnchorA;
            joint.localOriginAnchorB = def.localAnchorB;

            B2PrismaticJoint empty = new B2PrismaticJoint();
            joint.uj.prismaticJoint = empty;

            joint.uj.prismaticJoint.localAxisA = b2Normalize(def.localAxisA);
            joint.uj.prismaticJoint.referenceAngle = def.referenceAngle;
            joint.uj.prismaticJoint.impulse = b2Vec2_zero;
            joint.uj.prismaticJoint.axialMass = 0.0f;
            joint.uj.prismaticJoint.springImpulse = 0.0f;
            joint.uj.prismaticJoint.motorImpulse = 0.0f;
            joint.uj.prismaticJoint.lowerImpulse = 0.0f;
            joint.uj.prismaticJoint.upperImpulse = 0.0f;
            joint.uj.prismaticJoint.hertz = def.hertz;
            joint.uj.prismaticJoint.dampingRatio = def.dampingRatio;
            joint.uj.prismaticJoint.lowerTranslation = def.lowerTranslation;
            joint.uj.prismaticJoint.upperTranslation = def.upperTranslation;
            joint.uj.prismaticJoint.maxMotorForce = def.maxMotorForce;
            joint.uj.prismaticJoint.motorSpeed = def.motorSpeed;
            joint.uj.prismaticJoint.enableSpring = def.enableSpring;
            joint.uj.prismaticJoint.enableLimit = def.enableLimit;
            joint.uj.prismaticJoint.enableMotor = def.enableMotor;

            // If the joint prevents collisions, then destroy all contacts between attached bodies
            if (def.collideConnected == false)
            {
                b2DestroyContactsBetweenBodies(world, bodyA, bodyB);
            }

            B2JointId jointId = new B2JointId(joint.jointId + 1, world.worldId, pair.joint.generation);
            return jointId;
        }

        public static B2JointId b2CreateWeldJoint(B2WorldId worldId, ref B2WeldJointDef def)
        {
            B2_CHECK_DEF(ref def);
            B2World world = b2GetWorldFromId(worldId);

            B2_ASSERT(world.locked == false);

            if (world.locked)
            {
                return new B2JointId();
            }

            B2Body bodyA = b2GetBodyFullId(world, def.bodyIdA);
            B2Body bodyB = b2GetBodyFullId(world, def.bodyIdB);

            B2JointPair pair = b2CreateJoint(world, bodyA, bodyB, def.userData, 1.0f, B2JointType.b2_weldJoint, def.collideConnected);

            B2JointSim joint = pair.jointSim;
            joint.type = B2JointType.b2_weldJoint;
            joint.localOriginAnchorA = def.localAnchorA;
            joint.localOriginAnchorB = def.localAnchorB;

            B2WeldJoint empty = new B2WeldJoint();
            joint.uj.weldJoint = empty;
            joint.uj.weldJoint.referenceAngle = def.referenceAngle;
            joint.uj.weldJoint.linearHertz = def.linearHertz;
            joint.uj.weldJoint.linearDampingRatio = def.linearDampingRatio;
            joint.uj.weldJoint.angularHertz = def.angularHertz;
            joint.uj.weldJoint.angularDampingRatio = def.angularDampingRatio;
            joint.uj.weldJoint.linearImpulse = b2Vec2_zero;
            joint.uj.weldJoint.angularImpulse = 0.0f;

            // If the joint prevents collisions, then destroy all contacts between attached bodies
            if (def.collideConnected == false)
            {
                b2DestroyContactsBetweenBodies(world, bodyA, bodyB);
            }

            B2JointId jointId = new B2JointId(joint.jointId + 1, world.worldId, pair.joint.generation);
            return jointId;
        }

        public static B2JointId b2CreateWheelJoint(B2WorldId worldId, ref B2WheelJointDef def)
        {
            B2_CHECK_DEF(ref def);
            B2_ASSERT(def.lowerTranslation <= def.upperTranslation);

            B2World world = b2GetWorldFromId(worldId);

            B2_ASSERT(world.locked == false);

            if (world.locked)
            {
                return new B2JointId();
            }

            B2Body bodyA = b2GetBodyFullId(world, def.bodyIdA);
            B2Body bodyB = b2GetBodyFullId(world, def.bodyIdB);

            B2JointPair pair = b2CreateJoint(world, bodyA, bodyB, def.userData, 1.0f, B2JointType.b2_wheelJoint, def.collideConnected);

            B2JointSim joint = pair.jointSim;
            joint.type = B2JointType.b2_wheelJoint;
            joint.localOriginAnchorA = def.localAnchorA;
            joint.localOriginAnchorB = def.localAnchorB;

            joint.uj.wheelJoint = new B2WheelJoint();
            joint.uj.wheelJoint.localAxisA = b2Normalize(def.localAxisA);
            joint.uj.wheelJoint.perpMass = 0.0f;
            joint.uj.wheelJoint.axialMass = 0.0f;
            joint.uj.wheelJoint.motorImpulse = 0.0f;
            joint.uj.wheelJoint.lowerImpulse = 0.0f;
            joint.uj.wheelJoint.upperImpulse = 0.0f;
            joint.uj.wheelJoint.lowerTranslation = def.lowerTranslation;
            joint.uj.wheelJoint.upperTranslation = def.upperTranslation;
            joint.uj.wheelJoint.maxMotorTorque = def.maxMotorTorque;
            joint.uj.wheelJoint.motorSpeed = def.motorSpeed;
            joint.uj.wheelJoint.hertz = def.hertz;
            joint.uj.wheelJoint.dampingRatio = def.dampingRatio;
            joint.uj.wheelJoint.enableSpring = def.enableSpring;
            joint.uj.wheelJoint.enableLimit = def.enableLimit;
            joint.uj.wheelJoint.enableMotor = def.enableMotor;

            // If the joint prevents collisions, then destroy all contacts between attached bodies
            if (def.collideConnected == false)
            {
                b2DestroyContactsBetweenBodies(world, bodyA, bodyB);
            }

            B2JointId jointId = new B2JointId(joint.jointId + 1, world.worldId, pair.joint.generation);
            return jointId;
        }

        public static void b2DestroyJointInternal(B2World world, B2Joint joint, bool wakeBodies)
        {
            int jointId = joint.jointId;

            ref B2JointEdge edgeA = ref joint.edges[0];
            ref B2JointEdge edgeB = ref joint.edges[1];

            int idA = edgeA.bodyId;
            int idB = edgeB.bodyId;
            B2Body bodyA = b2Array_Get(ref world.bodies, idA);
            B2Body bodyB = b2Array_Get(ref world.bodies, idB);

            // Remove from body A
            if (edgeA.prevKey != B2_NULL_INDEX)
            {
                B2Joint prevJoint = b2Array_Get(ref world.joints, edgeA.prevKey >> 1);
                ref B2JointEdge prevEdge = ref prevJoint.edges[edgeA.prevKey & 1];
                prevEdge.nextKey = edgeA.nextKey;
            }

            if (edgeA.nextKey != B2_NULL_INDEX)
            {
                B2Joint nextJoint = b2Array_Get(ref world.joints, edgeA.nextKey >> 1);
                ref B2JointEdge nextEdge = ref nextJoint.edges[edgeA.nextKey & 1];
                nextEdge.prevKey = edgeA.prevKey;
            }

            int edgeKeyA = (jointId << 1) | 0;
            if (bodyA.headJointKey == edgeKeyA)
            {
                bodyA.headJointKey = edgeA.nextKey;
            }

            bodyA.jointCount -= 1;

            // Remove from body B
            if (edgeB.prevKey != B2_NULL_INDEX)
            {
                B2Joint prevJoint = b2Array_Get(ref world.joints, edgeB.prevKey >> 1);
                ref B2JointEdge prevEdge = ref prevJoint.edges[edgeB.prevKey & 1];
                prevEdge.nextKey = edgeB.nextKey;
            }

            if (edgeB.nextKey != B2_NULL_INDEX)
            {
                B2Joint nextJoint = b2Array_Get(ref world.joints, edgeB.nextKey >> 1);
                ref B2JointEdge nextEdge = ref nextJoint.edges[edgeB.nextKey & 1];
                nextEdge.prevKey = edgeB.prevKey;
            }

            int edgeKeyB = (jointId << 1) | 1;
            if (bodyB.headJointKey == edgeKeyB)
            {
                bodyB.headJointKey = edgeB.nextKey;
            }

            bodyB.jointCount -= 1;

            if (joint.islandId != B2_NULL_INDEX)
            {
                B2_ASSERT(joint.setIndex > (int)B2SetType.b2_disabledSet);
                b2UnlinkJoint(world, joint);
            }
            else
            {
                B2_ASSERT(joint.setIndex <= (int)B2SetType.b2_disabledSet);
            }

            // Remove joint from solver set that owns it
            int setIndex = joint.setIndex;
            int localIndex = joint.localIndex;

            if (setIndex == (int)B2SetType.b2_awakeSet)
            {
                b2RemoveJointFromGraph(world, joint.edges[0].bodyId, joint.edges[1].bodyId, joint.colorIndex, localIndex);
            }
            else
            {
                B2SolverSet set = b2Array_Get(ref world.solverSets, setIndex);
                int movedIndex = b2Array_RemoveSwap(ref set.jointSims, localIndex);
                if (movedIndex != B2_NULL_INDEX)
                {
                    // Fix moved joint
                    B2JointSim movedJointSim = set.jointSims.data[localIndex];
                    int movedId = movedJointSim.jointId;
                    B2Joint movedJoint = b2Array_Get(ref world.joints, movedId);
                    B2_ASSERT(movedJoint.localIndex == movedIndex);
                    movedJoint.localIndex = localIndex;
                }
            }

            // Free joint and id (preserve joint generation)
            joint.setIndex = B2_NULL_INDEX;
            joint.localIndex = B2_NULL_INDEX;
            joint.colorIndex = B2_NULL_INDEX;
            joint.jointId = B2_NULL_INDEX;
            b2FreeId(world.jointIdPool, jointId);

            if (wakeBodies)
            {
                b2WakeBody(world, bodyA);
                b2WakeBody(world, bodyB);
            }

            b2ValidateSolverSets(world);
        }

        public static void b2DestroyJoint(B2JointId jointId)
        {
            B2World world = b2GetWorld(jointId.world0);
            B2_ASSERT(world.locked == false);

            if (world.locked)
            {
                return;
            }

            B2Joint joint = b2GetJointFullId(world, jointId);

            b2DestroyJointInternal(world, joint, true);
        }

        public static B2JointType b2Joint_GetType(B2JointId jointId)
        {
            B2World world = b2GetWorld(jointId.world0);
            B2Joint joint = b2GetJointFullId(world, jointId);
            return joint.type;
        }

        public static B2BodyId b2Joint_GetBodyA(B2JointId jointId)
        {
            B2World world = b2GetWorld(jointId.world0);
            B2Joint joint = b2GetJointFullId(world, jointId);
            return b2MakeBodyId(world, joint.edges[0].bodyId);
        }

        public static B2BodyId b2Joint_GetBodyB(B2JointId jointId)
        {
            B2World world = b2GetWorld(jointId.world0);
            B2Joint joint = b2GetJointFullId(world, jointId);
            return b2MakeBodyId(world, joint.edges[1].bodyId);
        }

        public static B2WorldId b2Joint_GetWorld(B2JointId jointId)
        {
            B2World world = b2GetWorld(jointId.world0);
            return new B2WorldId((ushort)(jointId.world0 + 1), world.generation);
        }

        public static B2Vec2 b2Joint_GetLocalAnchorA(B2JointId jointId)
        {
            B2World world = b2GetWorld(jointId.world0);
            B2Joint joint = b2GetJointFullId(world, jointId);
            B2JointSim jointSim = b2GetJointSim(world, joint);
            return jointSim.localOriginAnchorA;
        }

        public static B2Vec2 b2Joint_GetLocalAnchorB(B2JointId jointId)
        {
            B2World world = b2GetWorld(jointId.world0);
            B2Joint joint = b2GetJointFullId(world, jointId);
            B2JointSim jointSim = b2GetJointSim(world, joint);
            return jointSim.localOriginAnchorB;
        }

        public static void b2Joint_SetCollideConnected(B2JointId jointId, bool shouldCollide)
        {
            B2World world = b2GetWorldLocked(jointId.world0);
            if (world == null)
            {
                return;
            }

            B2Joint joint = b2GetJointFullId(world, jointId);
            if (joint.collideConnected == shouldCollide)
            {
                return;
            }

            joint.collideConnected = shouldCollide;

            B2Body bodyA = b2Array_Get(ref world.bodies, joint.edges[0].bodyId);
            B2Body bodyB = b2Array_Get(ref world.bodies, joint.edges[1].bodyId);

            if (shouldCollide)
            {
                // need to tell the broad-phase to look for new pairs for one of the
                // two bodies. Pick the one with the fewest shapes.
                int shapeCountA = bodyA.shapeCount;
                int shapeCountB = bodyB.shapeCount;

                int shapeId = shapeCountA < shapeCountB ? bodyA.headShapeId : bodyB.headShapeId;
                while (shapeId != B2_NULL_INDEX)
                {
                    B2Shape shape = b2Array_Get(ref world.shapes, shapeId);

                    if (shape.proxyKey != B2_NULL_INDEX)
                    {
                        b2BufferMove(world.broadPhase, shape.proxyKey);
                    }

                    shapeId = shape.nextShapeId;
                }
            }
            else
            {
                b2DestroyContactsBetweenBodies(world, bodyA, bodyB);
            }
        }

        public static bool b2Joint_GetCollideConnected(B2JointId jointId)
        {
            B2World world = b2GetWorld(jointId.world0);
            B2Joint joint = b2GetJointFullId(world, jointId);
            return joint.collideConnected;
        }

        public static void b2Joint_SetUserData(B2JointId jointId, object userData)
        {
            B2World world = b2GetWorld(jointId.world0);
            B2Joint joint = b2GetJointFullId(world, jointId);
            joint.userData = userData;
        }

        public static object b2Joint_GetUserData(B2JointId jointId)
        {
            B2World world = b2GetWorld(jointId.world0);
            B2Joint joint = b2GetJointFullId(world, jointId);
            return joint.userData;
        }

        public static void b2Joint_WakeBodies(B2JointId jointId)
        {
            B2World world = b2GetWorldLocked(jointId.world0);
            if (world == null)
            {
                return;
            }

            B2Joint joint = b2GetJointFullId(world, jointId);
            B2Body bodyA = b2Array_Get(ref world.bodies, joint.edges[0].bodyId);
            B2Body bodyB = b2Array_Get(ref world.bodies, joint.edges[1].bodyId);

            b2WakeBody(world, bodyA);
            b2WakeBody(world, bodyB);
        }

        public static B2Vec2 b2Joint_GetConstraintForce(B2JointId jointId)
        {
            B2World world = b2GetWorld(jointId.world0);
            B2Joint joint = b2GetJointFullId(world, jointId);
            B2JointSim @base = b2GetJointSim(world, joint);

            switch (joint.type)
            {
                case B2JointType.b2_distanceJoint:
                    return b2GetDistanceJointForce(world, @base);

                case B2JointType.b2_motorJoint:
                    return b2GetMotorJointForce(world, @base);

                case B2JointType.b2_mouseJoint:
                    return b2GetMouseJointForce(world, @base);

                case B2JointType.b2_filterJoint:
                    return b2Vec2_zero;

                case B2JointType.b2_prismaticJoint:
                    return b2GetPrismaticJointForce(world, @base);

                case B2JointType.b2_revoluteJoint:
                    return b2GetRevoluteJointForce(world, @base);

                case B2JointType.b2_weldJoint:
                    return b2GetWeldJointForce(world, @base);

                case B2JointType.b2_wheelJoint:
                    return b2GetWheelJointForce(world, @base);

                default:
                    B2_ASSERT(false);
                    return b2Vec2_zero;
            }
        }

        public static float b2Joint_GetConstraintTorque(B2JointId jointId)
        {
            B2World world = b2GetWorld(jointId.world0);
            B2Joint joint = b2GetJointFullId(world, jointId);
            B2JointSim @base = b2GetJointSim(world, joint);

            switch (joint.type)
            {
                case B2JointType.b2_distanceJoint:
                    return 0.0f;

                case B2JointType.b2_motorJoint:
                    return b2GetMotorJointTorque(world, @base);

                case B2JointType.b2_mouseJoint:
                    return b2GetMouseJointTorque(world, @base);

                case B2JointType.b2_filterJoint:
                    return 0.0f;

                case B2JointType.b2_prismaticJoint:
                    return b2GetPrismaticJointTorque(world, @base);

                case B2JointType.b2_revoluteJoint:
                    return b2GetRevoluteJointTorque(world, @base);

                case B2JointType.b2_weldJoint:
                    return b2GetWeldJointTorque(world, @base);

                case B2JointType.b2_wheelJoint:
                    return b2GetWheelJointTorque(world, @base);

                default:
                    B2_ASSERT(false);
                    return 0.0f;
            }
        }

        public static void b2PrepareJoint(B2JointSim joint, B2StepContext context)
        {
            switch (joint.type)
            {
                case B2JointType.b2_distanceJoint:
                    b2PrepareDistanceJoint(joint, context);
                    break;

                case B2JointType.b2_motorJoint:
                    b2PrepareMotorJoint(joint, context);
                    break;

                case B2JointType.b2_mouseJoint:
                    b2PrepareMouseJoint(joint, context);
                    break;

                case B2JointType.b2_filterJoint:
                    break;

                case B2JointType.b2_prismaticJoint:
                    b2PreparePrismaticJoint(joint, context);
                    break;

                case B2JointType.b2_revoluteJoint:
                    b2PrepareRevoluteJoint(joint, context);
                    break;

                case B2JointType.b2_weldJoint:
                    b2PrepareWeldJoint(joint, context);
                    break;

                case B2JointType.b2_wheelJoint:
                    b2PrepareWheelJoint(joint, context);
                    break;

                default:
                    B2_ASSERT(false);
                    break;
            }
        }

        public static void b2WarmStartJoint(B2JointSim joint, B2StepContext context)
        {
            switch (joint.type)
            {
                case B2JointType.b2_distanceJoint:
                    b2WarmStartDistanceJoint(joint, context);
                    break;

                case B2JointType.b2_motorJoint:
                    b2WarmStartMotorJoint(joint, context);
                    break;

                case B2JointType.b2_mouseJoint:
                    b2WarmStartMouseJoint(joint, context);
                    break;

                case B2JointType.b2_filterJoint:
                    break;

                case B2JointType.b2_prismaticJoint:
                    b2WarmStartPrismaticJoint(joint, context);
                    break;

                case B2JointType.b2_revoluteJoint:
                    b2WarmStartRevoluteJoint(joint, context);
                    break;

                case B2JointType.b2_weldJoint:
                    b2WarmStartWeldJoint(joint, context);
                    break;

                case B2JointType.b2_wheelJoint:
                    b2WarmStartWheelJoint(joint, context);
                    break;

                default:
                    B2_ASSERT(false);
                    break;
            }
        }

        public static void b2SolveJoint(B2JointSim joint, B2StepContext context, bool useBias)
        {
            switch (joint.type)
            {
                case B2JointType.b2_distanceJoint:
                    b2SolveDistanceJoint(joint, context, useBias);
                    break;

                case B2JointType.b2_motorJoint:
                    b2SolveMotorJoint(joint, context, useBias);
                    break;

                case B2JointType.b2_mouseJoint:
                    b2SolveMouseJoint(joint, context);
                    break;

                case B2JointType.b2_filterJoint:
                    break;

                case B2JointType.b2_prismaticJoint:
                    b2SolvePrismaticJoint(joint, context, useBias);
                    break;

                case B2JointType.b2_revoluteJoint:
                    b2SolveRevoluteJoint(joint, context, useBias);
                    break;

                case B2JointType.b2_weldJoint:
                    b2SolveWeldJoint(joint, context, useBias);
                    break;

                case B2JointType.b2_wheelJoint:
                    b2SolveWheelJoint(joint, context, useBias);
                    break;

                default:
                    B2_ASSERT(false);
                    break;
            }
        }

        public static void b2PrepareOverflowJoints(B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.prepare_joints, "PrepJoints", B2HexColor.b2_colorOldLace, true);

            ref B2ConstraintGraph graph = ref context.graph;
            B2JointSim[] joints = graph.colors[B2_OVERFLOW_INDEX].jointSims.data;
            int jointCount = graph.colors[B2_OVERFLOW_INDEX].jointSims.count;

            for (int i = 0; i < jointCount; ++i)
            {
                B2JointSim joint = joints[i];
                b2PrepareJoint(joint, context);
            }

            b2TracyCZoneEnd(B2TracyCZone.prepare_joints);
        }

        public static void b2WarmStartOverflowJoints(B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.prepare_joints, "PrepJoints", B2HexColor.b2_colorOldLace, true);

            ref B2ConstraintGraph graph = ref context.graph;
            B2JointSim[] joints = graph.colors[B2_OVERFLOW_INDEX].jointSims.data;
            int jointCount = graph.colors[B2_OVERFLOW_INDEX].jointSims.count;

            for (int i = 0; i < jointCount; ++i)
            {
                B2JointSim joint = joints[i];
                b2WarmStartJoint(joint, context);
            }

            b2TracyCZoneEnd(B2TracyCZone.prepare_joints);
        }

        public static void b2SolveOverflowJoints(B2StepContext context, bool useBias)
        {
            b2TracyCZoneNC(B2TracyCZone.solve_joints, "SolveJoints", B2HexColor.b2_colorLemonChiffon, true);

            ref B2ConstraintGraph graph = ref context.graph;
            B2JointSim[] joints = graph.colors[B2_OVERFLOW_INDEX].jointSims.data;
            int jointCount = graph.colors[B2_OVERFLOW_INDEX].jointSims.count;

            for (int i = 0; i < jointCount; ++i)
            {
                B2JointSim joint = joints[i];
                b2SolveJoint(joint, context, useBias);
            }

            b2TracyCZoneEnd(B2TracyCZone.solve_joints);
        }

        public static void b2DrawJoint(B2DebugDraw draw, B2World world, B2Joint joint)
        {
            B2Body bodyA = b2Array_Get(ref world.bodies, joint.edges[0].bodyId);
            B2Body bodyB = b2Array_Get(ref world.bodies, joint.edges[1].bodyId);
            if (bodyA.setIndex == (int)B2SetType.b2_disabledSet || bodyB.setIndex == (int)B2SetType.b2_disabledSet)
            {
                return;
            }

            B2JointSim jointSim = b2GetJointSim(world, joint);

            B2Transform transformA = b2GetBodyTransformQuick(world, bodyA);
            B2Transform transformB = b2GetBodyTransformQuick(world, bodyB);
            B2Vec2 pA = b2TransformPoint(ref transformA, jointSim.localOriginAnchorA);
            B2Vec2 pB = b2TransformPoint(ref transformB, jointSim.localOriginAnchorB);

            B2HexColor color = B2HexColor.b2_colorDarkSeaGreen;

            switch (joint.type)
            {
                case B2JointType.b2_distanceJoint:
                    b2DrawDistanceJoint(draw, jointSim, transformA, transformB);
                    break;

                case B2JointType.b2_mouseJoint:
                {
                    B2Vec2 target = jointSim.uj.mouseJoint.targetA;

                    B2HexColor c1 = B2HexColor.b2_colorGreen;
                    draw.DrawPointFcn(target, 4.0f, c1, draw.context);
                    draw.DrawPointFcn(pB, 4.0f, c1, draw.context);

                    B2HexColor c2 = B2HexColor.b2_colorLightGray;
                    draw.DrawSegmentFcn(target, pB, c2, draw.context);
                }
                    break;

                case B2JointType.b2_filterJoint:
                {
                    draw.DrawSegmentFcn(pA, pB, B2HexColor.b2_colorGold, draw.context);
                }
                    break;

                case B2JointType.b2_prismaticJoint:
                    b2DrawPrismaticJoint(draw, jointSim, transformA, transformB);
                    break;

                case B2JointType.b2_revoluteJoint:
                    b2DrawRevoluteJoint(draw, jointSim, transformA, transformB, joint.drawSize);
                    break;

                case B2JointType.b2_wheelJoint:
                    b2DrawWheelJoint(draw, jointSim, transformA, transformB);
                    break;

                default:
                    draw.DrawSegmentFcn(transformA.p, pA, color, draw.context);
                    draw.DrawSegmentFcn(pA, pB, color, draw.context);
                    draw.DrawSegmentFcn(transformB.p, pB, color, draw.context);
                    break;
            }

            if (draw.drawGraphColors)
            {
                Span<B2HexColor> colors = stackalloc B2HexColor[B2_GRAPH_COLOR_COUNT]
                {
                    B2HexColor.b2_colorRed, B2HexColor.b2_colorOrange, B2HexColor.b2_colorYellow, B2HexColor.b2_colorGreen,
                    B2HexColor.b2_colorCyan, B2HexColor.b2_colorBlue, B2HexColor.b2_colorViolet, B2HexColor.b2_colorPink,
                    B2HexColor.b2_colorChocolate, B2HexColor.b2_colorGoldenRod, B2HexColor.b2_colorCoral, B2HexColor.b2_colorBlack
                };

                int colorIndex = joint.colorIndex;
                if (colorIndex != B2_NULL_INDEX)
                {
                    B2Vec2 p = b2Lerp(pA, pB, 0.5f);
                    draw.DrawPointFcn(p, 5.0f, colors[colorIndex], draw.context);
                }
            }
        }
    }
}