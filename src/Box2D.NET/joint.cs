// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT


using System.Diagnostics;
using Box2D.NET.Primitives;
using static Box2D.NET.array;
using static Box2D.NET.core;
using static Box2D.NET.constants;
using static Box2D.NET.contact;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.world;
using static Box2D.NET.distance_joint;
using static Box2D.NET.motor_joint;
using static Box2D.NET.mouse_joint;
using static Box2D.NET.prismatic_joint;
using static Box2D.NET.revolute_joint;
using static Box2D.NET.weld_joint;
using static Box2D.NET.wheel_joint;
using static Box2D.NET.id_pool;
using static Box2D.NET.solver_set;
using static Box2D.NET.constraint_graph;
using static Box2D.NET.island;
using static Box2D.NET.board_phase;


namespace Box2D.NET
{
    public static class joint
    {
        /// Use this to initialize your joint definition
        /// @ingroup distance_joint
        public static b2DistanceJointDef b2DefaultDistanceJointDef()
        {
            b2DistanceJointDef def = new b2DistanceJointDef();
            def.length = 1.0f;
            def.maxLength = B2_HUGE;
            def.internalValue = B2_SECRET_COOKIE;
            return def;
        }

        public static b2MotorJointDef b2DefaultMotorJointDef()
        {
            b2MotorJointDef def = new b2MotorJointDef();
            def.maxForce = 1.0f;
            def.maxTorque = 1.0f;
            def.correctionFactor = 0.3f;
            def.internalValue = B2_SECRET_COOKIE;
            return def;
        }

        public static b2MouseJointDef b2DefaultMouseJointDef()
        {
            b2MouseJointDef def = new b2MouseJointDef();
            def.hertz = 4.0f;
            def.dampingRatio = 1.0f;
            def.maxForce = 1.0f;
            def.internalValue = B2_SECRET_COOKIE;
            return def;
        }

        public static b2NullJointDef b2DefaultNullJointDef()
        {
            b2NullJointDef def = new b2NullJointDef();
            def.internalValue = B2_SECRET_COOKIE;
            return def;
        }

        public static b2PrismaticJointDef b2DefaultPrismaticJointDef()
        {
            b2PrismaticJointDef def = new b2PrismaticJointDef();
            def.localAxisA = new b2Vec2(1.0f, 0.0f);
            def.internalValue = B2_SECRET_COOKIE;
            return def;
        }

        public static b2RevoluteJointDef b2DefaultRevoluteJointDef()
        {
            b2RevoluteJointDef def = new b2RevoluteJointDef();
            def.drawSize = 0.25f;
            def.internalValue = B2_SECRET_COOKIE;
            return def;
        }

        public static b2WeldJointDef b2DefaultWeldJointDef()
        {
            b2WeldJointDef def = new b2WeldJointDef();
            def.internalValue = B2_SECRET_COOKIE;
            return def;
        }

        public static b2WheelJointDef b2DefaultWheelJointDef()
        {
            b2WheelJointDef def = new b2WheelJointDef();
            def.localAxisA.y = 1.0f;
            def.enableSpring = true;
            def.hertz = 1.0f;
            def.dampingRatio = 0.7f;
            def.internalValue = B2_SECRET_COOKIE;
            return def;
        }

        public static b2ExplosionDef b2DefaultExplosionDef()
        {
            b2ExplosionDef def = new b2ExplosionDef();
            def.maskBits = B2_DEFAULT_MASK_BITS;
            return def;
        }

        public static b2Joint b2GetJointFullId(b2World world, b2JointId jointId)
        {
            int id = jointId.index1 - 1;
            b2Joint joint = b2Array_Get(ref world.joints, id);
            Debug.Assert(joint.jointId == id && joint.generation == jointId.generation);
            return joint;
        }

        public static b2JointSim b2GetJointSim(b2World world, b2Joint joint)
        {
            if (joint.setIndex == (int)b2SetType.b2_awakeSet)
            {
                Debug.Assert(0 <= joint.colorIndex && joint.colorIndex < B2_GRAPH_COLOR_COUNT);
                b2GraphColor color = world.constraintGraph.colors[joint.colorIndex];
                return b2Array_Get(ref color.jointSims, joint.localIndex);
            }

            b2SolverSet set = b2Array_Get(ref world.solverSets, joint.setIndex);
            return b2Array_Get(ref set.jointSims, joint.localIndex);
        }

        public static b2JointSim b2GetJointSimCheckType(b2JointId jointId, b2JointType type)
        {
            B2_UNUSED(type);

            b2World world = b2GetWorld(jointId.world0);
            Debug.Assert(world.locked == false);
            if (world.locked)
            {
                return null;
            }

            b2Joint joint = b2GetJointFullId(world, jointId);
            Debug.Assert(joint.type == type);
            b2JointSim jointSim = b2GetJointSim(world, joint);
            Debug.Assert(jointSim.type == type);
            return jointSim;
        }


        public static b2JointPair b2CreateJoint(b2World world, b2Body bodyA, b2Body bodyB, object userData, float drawSize, b2JointType type, bool collideConnected)
        {
            int bodyIdA = bodyA.id;
            int bodyIdB = bodyB.id;
            int maxSetIndex = b2MaxInt(bodyA.setIndex, bodyB.setIndex);

            // Create joint id and joint
            int jointId = b2AllocId(world.jointIdPool);
            if (jointId == world.joints.count)
            {
                b2Array_Push(ref world.joints, new b2Joint());
            }

            b2Joint joint = b2Array_Get(ref world.joints, jointId);
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

            // @ikpil, create the b2JointEdge here for now, and later I'll see how to proceed
            joint.edges = new b2JointEdge[2];
            joint.edges[0] = new b2JointEdge();
            joint.edges[1] = new b2JointEdge();

            // Doubly linked list on bodyA
            joint.edges[0].bodyId = bodyIdA;
            joint.edges[0].prevKey = B2_NULL_INDEX;
            joint.edges[0].nextKey = bodyA.headJointKey;

            int keyA = (jointId << 1) | 0;
            if (bodyA.headJointKey != B2_NULL_INDEX)
            {
                b2Joint jointA = b2Array_Get(ref world.joints, bodyA.headJointKey >> 1);
                b2JointEdge edgeA = jointA.edges[bodyA.headJointKey & 1];
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
                b2Joint jointB = b2Array_Get(ref world.joints, bodyB.headJointKey >> 1);
                b2JointEdge edgeB = jointB.edges[(bodyB.headJointKey & 1)];
                edgeB.prevKey = keyB;
            }

            bodyB.headJointKey = keyB;
            bodyB.jointCount += 1;

            b2JointSim jointSim = null;

            if (bodyA.setIndex == (int)b2SetType.b2_disabledSet || bodyB.setIndex == (int)b2SetType.b2_disabledSet)
            {
                // if either body is disabled, create in disabled set
                b2SolverSet set = b2Array_Get(ref world.solverSets, (int)b2SetType.b2_disabledSet);
                joint.setIndex = (int)b2SetType.b2_disabledSet;
                joint.localIndex = set.jointSims.count;

                jointSim = b2Array_Add(ref set.jointSims);
                //memset( jointSim, 0, sizeof( b2JointSim ) );

                jointSim.jointId = jointId;
                jointSim.bodyIdA = bodyIdA;
                jointSim.bodyIdB = bodyIdB;
            }
            else if (bodyA.setIndex == (int)b2SetType.b2_staticSet && bodyB.setIndex == (int)b2SetType.b2_staticSet)
            {
                // joint is connecting static bodies
                b2SolverSet set = b2Array_Get(ref world.solverSets, (int)b2SetType.b2_staticSet);
                joint.setIndex = (int)b2SetType.b2_staticSet;
                joint.localIndex = set.jointSims.count;

                jointSim = b2Array_Add(ref set.jointSims);
                //memset( jointSim, 0, sizeof( b2JointSim ) );

                jointSim.jointId = jointId;
                jointSim.bodyIdA = bodyIdA;
                jointSim.bodyIdB = bodyIdB;
            }
            else if (bodyA.setIndex == (int)b2SetType.b2_awakeSet || bodyB.setIndex == (int)b2SetType.b2_awakeSet)
            {
                // if either body is sleeping, wake it
                if (maxSetIndex >= (int)b2SetType.b2_firstSleepingSet)
                {
                    b2WakeSolverSet(world, maxSetIndex);
                }

                joint.setIndex = (int)b2SetType.b2_awakeSet;

                jointSim = b2CreateJointInGraph(world, joint);
                jointSim.jointId = jointId;
                jointSim.bodyIdA = bodyIdA;
                jointSim.bodyIdB = bodyIdB;
            }
            else
            {
                // joint connected between sleeping and/or static bodies
                Debug.Assert(bodyA.setIndex >= (int)b2SetType.b2_firstSleepingSet || bodyB.setIndex >= (int)b2SetType.b2_firstSleepingSet);
                Debug.Assert(bodyA.setIndex != (int)b2SetType.b2_staticSet || bodyB.setIndex != (int)b2SetType.b2_staticSet);

                // joint should go into the sleeping set (not static set)
                int setIndex = maxSetIndex;

                b2SolverSet set = b2Array_Get(ref world.solverSets, setIndex);
                joint.setIndex = setIndex;
                joint.localIndex = set.jointSims.count;

                jointSim = b2Array_Add(ref set.jointSims);
                //memset( jointSim, 0, sizeof( b2JointSim ) );

                jointSim.jointId = jointId;
                jointSim.bodyIdA = bodyIdA;
                jointSim.bodyIdB = bodyIdB;

                if (bodyA.setIndex != bodyB.setIndex && bodyA.setIndex >= (int)b2SetType.b2_firstSleepingSet &&
                    bodyB.setIndex >= (int)b2SetType.b2_firstSleepingSet)
                {
                    // merge sleeping sets
                    b2MergeSolverSets(world, bodyA.setIndex, bodyB.setIndex);
                    Debug.Assert(bodyA.setIndex == bodyB.setIndex);

                    // fix potentially invalid set index
                    setIndex = bodyA.setIndex;

                    b2SolverSet mergedSet = b2Array_Get(ref world.solverSets, setIndex);

                    // Careful! The joint sim pointer was orphaned by the set merge.
                    jointSim = b2Array_Get(ref mergedSet.jointSims, joint.localIndex);
                }

                Debug.Assert(joint.setIndex == setIndex);
            }

            Debug.Assert(jointSim.jointId == jointId);
            Debug.Assert(jointSim.bodyIdA == bodyIdA);
            Debug.Assert(jointSim.bodyIdB == bodyIdB);

            if (joint.setIndex > (int)b2SetType.b2_disabledSet)
            {
                // Add edge to island graph
                bool mergeIslands = true;
                b2LinkJoint(world, joint, mergeIslands);
            }

            b2ValidateSolverSets(world);

            return new b2JointPair(joint, jointSim);
        }

        public static void b2DestroyContactsBetweenBodies(b2World world, b2Body bodyA, b2Body bodyB)
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

                b2Contact contact = b2Array_Get(ref world.contacts, contactId);
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

        public static b2JointId b2CreateDistanceJoint(b2WorldId worldId, b2DistanceJointDef def)
        {
            B2_CHECK_DEF(def);
            b2World world = b2GetWorldFromId(worldId);

            Debug.Assert(world.locked == false);

            if (world.locked)
            {
                return new b2JointId();
            }

            Debug.Assert(b2Body_IsValid(def.bodyIdA));
            Debug.Assert(b2Body_IsValid(def.bodyIdB));
            Debug.Assert(b2IsValidFloat(def.length) && def.length > 0.0f);

            b2Body bodyA = b2GetBodyFullId(world, def.bodyIdA);
            b2Body bodyB = b2GetBodyFullId(world, def.bodyIdB);

            b2JointPair pair = b2CreateJoint(world, bodyA, bodyB, def.userData, 1.0f, b2JointType.b2_distanceJoint, def.collideConnected);

            b2JointSim joint = pair.jointSim;
            joint.type = b2JointType.b2_distanceJoint;
            joint.localOriginAnchorA = def.localAnchorA;
            joint.localOriginAnchorB = def.localAnchorB;

            b2DistanceJoint empty = new b2DistanceJoint();
            joint.distanceJoint = empty;
            joint.distanceJoint.length = b2MaxFloat(def.length, B2_LINEAR_SLOP);
            joint.distanceJoint.hertz = def.hertz;
            joint.distanceJoint.dampingRatio = def.dampingRatio;
            joint.distanceJoint.minLength = b2MaxFloat(def.minLength, B2_LINEAR_SLOP);
            joint.distanceJoint.maxLength = b2MaxFloat(def.minLength, def.maxLength);
            joint.distanceJoint.maxMotorForce = def.maxMotorForce;
            joint.distanceJoint.motorSpeed = def.motorSpeed;
            joint.distanceJoint.enableSpring = def.enableSpring;
            joint.distanceJoint.enableLimit = def.enableLimit;
            joint.distanceJoint.enableMotor = def.enableMotor;
            joint.distanceJoint.impulse = 0.0f;
            joint.distanceJoint.lowerImpulse = 0.0f;
            joint.distanceJoint.upperImpulse = 0.0f;
            joint.distanceJoint.motorImpulse = 0.0f;

            // If the joint prevents collisions, then destroy all contacts between attached bodies
            if (def.collideConnected == false)
            {
                b2DestroyContactsBetweenBodies(world, bodyA, bodyB);
            }

            b2JointId jointId = new b2JointId(joint.jointId + 1, world.worldId, pair.joint.generation);
            return jointId;
        }

        public static b2JointId b2CreateMotorJoint(b2WorldId worldId, b2MotorJointDef def)
        {
            B2_CHECK_DEF(def);
            b2World world = b2GetWorldFromId(worldId);

            Debug.Assert(world.locked == false);

            if (world.locked)
            {
                return new b2JointId();
            }

            b2Body bodyA = b2GetBodyFullId(world, def.bodyIdA);
            b2Body bodyB = b2GetBodyFullId(world, def.bodyIdB);

            b2JointPair pair = b2CreateJoint(world, bodyA, bodyB, def.userData, 1.0f, b2JointType.b2_motorJoint, def.collideConnected);
            b2JointSim joint = pair.jointSim;

            joint.type = b2JointType.b2_motorJoint;
            joint.localOriginAnchorA = new b2Vec2(0.0f, 0.0f);
            joint.localOriginAnchorB = new b2Vec2(0.0f, 0.0f);
            joint.motorJoint = new b2MotorJoint();
            joint.motorJoint.linearOffset = def.linearOffset;
            joint.motorJoint.angularOffset = def.angularOffset;
            joint.motorJoint.maxForce = def.maxForce;
            joint.motorJoint.maxTorque = def.maxTorque;
            joint.motorJoint.correctionFactor = b2ClampFloat(def.correctionFactor, 0.0f, 1.0f);

            // If the joint prevents collisions, then destroy all contacts between attached bodies
            if (def.collideConnected == false)
            {
                b2DestroyContactsBetweenBodies(world, bodyA, bodyB);
            }

            b2JointId jointId = new b2JointId(joint.jointId + 1, world.worldId, pair.joint.generation);
            return jointId;
        }

        public static b2JointId b2CreateMouseJoint(b2WorldId worldId, b2MouseJointDef def)
        {
            B2_CHECK_DEF(def);
            b2World world = b2GetWorldFromId(worldId);

            Debug.Assert(world.locked == false);

            if (world.locked)
            {
                return new b2JointId();
            }

            b2Body bodyA = b2GetBodyFullId(world, def.bodyIdA);
            b2Body bodyB = b2GetBodyFullId(world, def.bodyIdB);

            b2Transform transformA = b2GetBodyTransformQuick(world, bodyA);
            b2Transform transformB = b2GetBodyTransformQuick(world, bodyB);

            b2JointPair pair = b2CreateJoint(world, bodyA, bodyB, def.userData, 1.0f, b2JointType.b2_mouseJoint, def.collideConnected);

            b2JointSim joint = pair.jointSim;
            joint.type = b2JointType.b2_mouseJoint;
            joint.localOriginAnchorA = b2InvTransformPoint(transformA, def.target);
            joint.localOriginAnchorB = b2InvTransformPoint(transformB, def.target);

            b2MouseJoint empty = new b2MouseJoint();
            joint.mouseJoint = empty;
            joint.mouseJoint.targetA = def.target;
            joint.mouseJoint.hertz = def.hertz;
            joint.mouseJoint.dampingRatio = def.dampingRatio;
            joint.mouseJoint.maxForce = def.maxForce;

            b2JointId jointId = new b2JointId(joint.jointId + 1, world.worldId, pair.joint.generation);
            return jointId;
        }

        public static b2JointId b2CreateNullJoint(b2WorldId worldId, b2NullJointDef def)
        {
            B2_CHECK_DEF(def);
            b2World world = b2GetWorldFromId(worldId);

            Debug.Assert(world.locked == false);

            if (world.locked)
            {
                return new b2JointId();
            }

            b2Body bodyA = b2GetBodyFullId(world, def.bodyIdA);
            b2Body bodyB = b2GetBodyFullId(world, def.bodyIdB);

            bool collideConnected = false;
            b2JointPair pair = b2CreateJoint(world, bodyA, bodyB, def.userData, 1.0f, b2JointType.b2_nullJoint, collideConnected);

            b2JointSim joint = pair.jointSim;
            joint.type = b2JointType.b2_nullJoint;
            joint.localOriginAnchorA = b2Vec2_zero;
            joint.localOriginAnchorB = b2Vec2_zero;

            b2JointId jointId = new b2JointId(joint.jointId + 1, world.worldId, pair.joint.generation);
            return jointId;
        }

        public static b2JointId b2CreateRevoluteJoint(b2WorldId worldId, b2RevoluteJointDef def)
        {
            B2_CHECK_DEF(def);
            b2World world = b2GetWorldFromId(worldId);

            Debug.Assert(world.locked == false);

            if (world.locked)
            {
                return new b2JointId();
            }

            b2Body bodyA = b2GetBodyFullId(world, def.bodyIdA);
            b2Body bodyB = b2GetBodyFullId(world, def.bodyIdB);

            b2JointPair pair =
                b2CreateJoint(world, bodyA, bodyB, def.userData, def.drawSize, b2JointType.b2_revoluteJoint, def.collideConnected);

            b2JointSim joint = pair.jointSim;
            joint.type = b2JointType.b2_revoluteJoint;
            joint.localOriginAnchorA = def.localAnchorA;
            joint.localOriginAnchorB = def.localAnchorB;

            b2RevoluteJoint empty = new b2RevoluteJoint();
            joint.revoluteJoint = empty;

            joint.revoluteJoint.referenceAngle = b2ClampFloat(def.referenceAngle, -B2_PI, B2_PI);
            joint.revoluteJoint.linearImpulse = b2Vec2_zero;
            joint.revoluteJoint.axialMass = 0.0f;
            joint.revoluteJoint.springImpulse = 0.0f;
            joint.revoluteJoint.motorImpulse = 0.0f;
            joint.revoluteJoint.lowerImpulse = 0.0f;
            joint.revoluteJoint.upperImpulse = 0.0f;
            joint.revoluteJoint.hertz = def.hertz;
            joint.revoluteJoint.dampingRatio = def.dampingRatio;
            joint.revoluteJoint.lowerAngle = b2MinFloat(def.lowerAngle, def.upperAngle);
            joint.revoluteJoint.upperAngle = b2MaxFloat(def.lowerAngle, def.upperAngle);
            joint.revoluteJoint.lowerAngle = b2ClampFloat(joint.revoluteJoint.lowerAngle, -B2_PI, B2_PI);
            joint.revoluteJoint.upperAngle = b2ClampFloat(joint.revoluteJoint.upperAngle, -B2_PI, B2_PI);
            joint.revoluteJoint.maxMotorTorque = def.maxMotorTorque;
            joint.revoluteJoint.motorSpeed = def.motorSpeed;
            joint.revoluteJoint.enableSpring = def.enableSpring;
            joint.revoluteJoint.enableLimit = def.enableLimit;
            joint.revoluteJoint.enableMotor = def.enableMotor;

            // If the joint prevents collisions, then destroy all contacts between attached bodies
            if (def.collideConnected == false)
            {
                b2DestroyContactsBetweenBodies(world, bodyA, bodyB);
            }

            b2JointId jointId = new b2JointId(joint.jointId + 1, world.worldId, pair.joint.generation);
            return jointId;
        }

        public static b2JointId b2CreatePrismaticJoint(b2WorldId worldId, b2PrismaticJointDef def)
        {
            B2_CHECK_DEF(def);
            b2World world = b2GetWorldFromId(worldId);

            Debug.Assert(world.locked == false);

            if (world.locked)
            {
                return new b2JointId();
            }

            b2Body bodyA = b2GetBodyFullId(world, def.bodyIdA);
            b2Body bodyB = b2GetBodyFullId(world, def.bodyIdB);

            b2JointPair pair = b2CreateJoint(world, bodyA, bodyB, def.userData, 1.0f, b2JointType.b2_prismaticJoint, def.collideConnected);

            b2JointSim joint = pair.jointSim;
            joint.type = b2JointType.b2_prismaticJoint;
            joint.localOriginAnchorA = def.localAnchorA;
            joint.localOriginAnchorB = def.localAnchorB;

            b2PrismaticJoint empty = new b2PrismaticJoint();
            joint.prismaticJoint = empty;

            joint.prismaticJoint.localAxisA = b2Normalize(def.localAxisA);
            joint.prismaticJoint.referenceAngle = def.referenceAngle;
            joint.prismaticJoint.impulse = b2Vec2_zero;
            joint.prismaticJoint.axialMass = 0.0f;
            joint.prismaticJoint.springImpulse = 0.0f;
            joint.prismaticJoint.motorImpulse = 0.0f;
            joint.prismaticJoint.lowerImpulse = 0.0f;
            joint.prismaticJoint.upperImpulse = 0.0f;
            joint.prismaticJoint.hertz = def.hertz;
            joint.prismaticJoint.dampingRatio = def.dampingRatio;
            joint.prismaticJoint.lowerTranslation = def.lowerTranslation;
            joint.prismaticJoint.upperTranslation = def.upperTranslation;
            joint.prismaticJoint.maxMotorForce = def.maxMotorForce;
            joint.prismaticJoint.motorSpeed = def.motorSpeed;
            joint.prismaticJoint.enableSpring = def.enableSpring;
            joint.prismaticJoint.enableLimit = def.enableLimit;
            joint.prismaticJoint.enableMotor = def.enableMotor;

            // If the joint prevents collisions, then destroy all contacts between attached bodies
            if (def.collideConnected == false)
            {
                b2DestroyContactsBetweenBodies(world, bodyA, bodyB);
            }

            b2JointId jointId = new b2JointId(joint.jointId + 1, world.worldId, pair.joint.generation);
            return jointId;
        }

        public static b2JointId b2CreateWeldJoint(b2WorldId worldId, b2WeldJointDef def)
        {
            B2_CHECK_DEF(def);
            b2World world = b2GetWorldFromId(worldId);

            Debug.Assert(world.locked == false);

            if (world.locked)
            {
                return new b2JointId();
            }

            b2Body bodyA = b2GetBodyFullId(world, def.bodyIdA);
            b2Body bodyB = b2GetBodyFullId(world, def.bodyIdB);

            b2JointPair pair = b2CreateJoint(world, bodyA, bodyB, def.userData, 1.0f, b2JointType.b2_weldJoint, def.collideConnected);

            b2JointSim joint = pair.jointSim;
            joint.type = b2JointType.b2_weldJoint;
            joint.localOriginAnchorA = def.localAnchorA;
            joint.localOriginAnchorB = def.localAnchorB;

            b2WeldJoint empty = new b2WeldJoint();
            joint.weldJoint = empty;
            joint.weldJoint.referenceAngle = def.referenceAngle;
            joint.weldJoint.linearHertz = def.linearHertz;
            joint.weldJoint.linearDampingRatio = def.linearDampingRatio;
            joint.weldJoint.angularHertz = def.angularHertz;
            joint.weldJoint.angularDampingRatio = def.angularDampingRatio;
            joint.weldJoint.linearImpulse = b2Vec2_zero;
            joint.weldJoint.angularImpulse = 0.0f;

            // If the joint prevents collisions, then destroy all contacts between attached bodies
            if (def.collideConnected == false)
            {
                b2DestroyContactsBetweenBodies(world, bodyA, bodyB);
            }

            b2JointId jointId = new b2JointId(joint.jointId + 1, world.worldId, pair.joint.generation);
            return jointId;
        }

        public static b2JointId b2CreateWheelJoint(b2WorldId worldId, b2WheelJointDef def)
        {
            B2_CHECK_DEF(def);
            b2World world = b2GetWorldFromId(worldId);

            Debug.Assert(world.locked == false);

            if (world.locked)
            {
                return new b2JointId();
            }

            b2Body bodyA = b2GetBodyFullId(world, def.bodyIdA);
            b2Body bodyB = b2GetBodyFullId(world, def.bodyIdB);

            b2JointPair pair = b2CreateJoint(world, bodyA, bodyB, def.userData, 1.0f, b2JointType.b2_wheelJoint, def.collideConnected);

            b2JointSim joint = pair.jointSim;
            joint.type = b2JointType.b2_wheelJoint;
            joint.localOriginAnchorA = def.localAnchorA;
            joint.localOriginAnchorB = def.localAnchorB;

            joint.wheelJoint = new b2WheelJoint();
            joint.wheelJoint.localAxisA = b2Normalize(def.localAxisA);
            joint.wheelJoint.perpMass = 0.0f;
            joint.wheelJoint.axialMass = 0.0f;
            joint.wheelJoint.motorImpulse = 0.0f;
            joint.wheelJoint.lowerImpulse = 0.0f;
            joint.wheelJoint.upperImpulse = 0.0f;
            joint.wheelJoint.lowerTranslation = def.lowerTranslation;
            joint.wheelJoint.upperTranslation = def.upperTranslation;
            joint.wheelJoint.maxMotorTorque = def.maxMotorTorque;
            joint.wheelJoint.motorSpeed = def.motorSpeed;
            joint.wheelJoint.hertz = def.hertz;
            joint.wheelJoint.dampingRatio = def.dampingRatio;
            joint.wheelJoint.enableSpring = def.enableSpring;
            joint.wheelJoint.enableLimit = def.enableLimit;
            joint.wheelJoint.enableMotor = def.enableMotor;

            // If the joint prevents collisions, then destroy all contacts between attached bodies
            if (def.collideConnected == false)
            {
                b2DestroyContactsBetweenBodies(world, bodyA, bodyB);
            }

            b2JointId jointId = new b2JointId(joint.jointId + 1, world.worldId, pair.joint.generation);
            return jointId;
        }

        public static void b2DestroyJointInternal(b2World world, b2Joint joint, bool wakeBodies)
        {
            int jointId = joint.jointId;

            b2JointEdge edgeA = joint.edges[0];
            b2JointEdge edgeB = joint.edges[1];

            int idA = edgeA.bodyId;
            int idB = edgeB.bodyId;
            b2Body bodyA = b2Array_Get(ref world.bodies, idA);
            b2Body bodyB = b2Array_Get(ref world.bodies, idB);

            // Remove from body A
            if (edgeA.prevKey != B2_NULL_INDEX)
            {
                b2Joint prevJoint = b2Array_Get(ref world.joints, edgeA.prevKey >> 1);
                b2JointEdge prevEdge = prevJoint.edges[edgeA.prevKey & 1];
                prevEdge.nextKey = edgeA.nextKey;
            }

            if (edgeA.nextKey != B2_NULL_INDEX)
            {
                b2Joint nextJoint = b2Array_Get(ref world.joints, edgeA.nextKey >> 1);
                b2JointEdge nextEdge = nextJoint.edges[edgeA.nextKey & 1];
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
                b2Joint prevJoint = b2Array_Get(ref world.joints, edgeB.prevKey >> 1);
                b2JointEdge prevEdge = prevJoint.edges[edgeB.prevKey & 1];
                prevEdge.nextKey = edgeB.nextKey;
            }

            if (edgeB.nextKey != B2_NULL_INDEX)
            {
                b2Joint nextJoint = b2Array_Get(ref world.joints, edgeB.nextKey >> 1);
                b2JointEdge nextEdge = nextJoint.edges[edgeB.nextKey & 1];
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
                Debug.Assert(joint.setIndex > (int)b2SetType.b2_disabledSet);
                b2UnlinkJoint(world, joint);
            }
            else
            {
                Debug.Assert(joint.setIndex <= (int)b2SetType.b2_disabledSet);
            }

            // Remove joint from solver set that owns it
            int setIndex = joint.setIndex;
            int localIndex = joint.localIndex;

            if (setIndex == (int)b2SetType.b2_awakeSet)
            {
                b2RemoveJointFromGraph(world, joint.edges[0].bodyId, joint.edges[1].bodyId, joint.colorIndex, localIndex);
            }
            else
            {
                b2SolverSet set = b2Array_Get(ref world.solverSets, setIndex);
                int movedIndex = b2Array_RemoveSwap(ref set.jointSims, localIndex);
                if (movedIndex != B2_NULL_INDEX)
                {
                    // Fix moved joint
                    b2JointSim movedJointSim = set.jointSims.data[localIndex];
                    int movedId = movedJointSim.jointId;
                    b2Joint movedJoint = b2Array_Get(ref world.joints, movedId);
                    Debug.Assert(movedJoint.localIndex == movedIndex);
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

        public static void b2DestroyJoint(b2JointId jointId)
        {
            b2World world = b2GetWorld(jointId.world0);
            Debug.Assert(world.locked == false);

            if (world.locked)
            {
                return;
            }

            b2Joint joint = b2GetJointFullId(world, jointId);

            b2DestroyJointInternal(world, joint, true);
        }

        public static b2JointType b2Joint_GetType(b2JointId jointId)
        {
            b2World world = b2GetWorld(jointId.world0);
            b2Joint joint = b2GetJointFullId(world, jointId);
            return joint.type;
        }

        public static b2BodyId b2Joint_GetBodyA(b2JointId jointId)
        {
            b2World world = b2GetWorld(jointId.world0);
            b2Joint joint = b2GetJointFullId(world, jointId);
            return b2MakeBodyId(world, joint.edges[0].bodyId);
        }

        public static b2BodyId b2Joint_GetBodyB(b2JointId jointId)
        {
            b2World world = b2GetWorld(jointId.world0);
            b2Joint joint = b2GetJointFullId(world, jointId);
            return b2MakeBodyId(world, joint.edges[1].bodyId);
        }

        public static b2WorldId b2Joint_GetWorld(b2JointId jointId)
        {
            b2World world = b2GetWorld(jointId.world0);
            return new b2WorldId((ushort)(jointId.world0 + 1), world.generation);
        }

        public static b2Vec2 b2Joint_GetLocalAnchorA(b2JointId jointId)
        {
            b2World world = b2GetWorld(jointId.world0);
            b2Joint joint = b2GetJointFullId(world, jointId);
            b2JointSim jointSim = b2GetJointSim(world, joint);
            return jointSim.localOriginAnchorA;
        }

        public static b2Vec2 b2Joint_GetLocalAnchorB(b2JointId jointId)
        {
            b2World world = b2GetWorld(jointId.world0);
            b2Joint joint = b2GetJointFullId(world, jointId);
            b2JointSim jointSim = b2GetJointSim(world, joint);
            return jointSim.localOriginAnchorB;
        }

        public static void b2Joint_SetCollideConnected(b2JointId jointId, bool shouldCollide)
        {
            b2World world = b2GetWorldLocked(jointId.world0);
            if (world == null)
            {
                return;
            }

            b2Joint joint = b2GetJointFullId(world, jointId);
            if (joint.collideConnected == shouldCollide)
            {
                return;
            }

            joint.collideConnected = shouldCollide;

            b2Body bodyA = b2Array_Get(ref world.bodies, joint.edges[0].bodyId);
            b2Body bodyB = b2Array_Get(ref world.bodies, joint.edges[1].bodyId);

            if (shouldCollide)
            {
                // need to tell the broad-phase to look for new pairs for one of the
                // two bodies. Pick the one with the fewest shapes.
                int shapeCountA = bodyA.shapeCount;
                int shapeCountB = bodyB.shapeCount;

                int shapeId = shapeCountA < shapeCountB ? bodyA.headShapeId : bodyB.headShapeId;
                while (shapeId != B2_NULL_INDEX)
                {
                    b2Shape shape = b2Array_Get(ref world.shapes, shapeId);

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

        public static bool b2Joint_GetCollideConnected(b2JointId jointId)
        {
            b2World world = b2GetWorld(jointId.world0);
            b2Joint joint = b2GetJointFullId(world, jointId);
            return joint.collideConnected;
        }

        public static void b2Joint_SetUserData(b2JointId jointId, object userData)
        {
            b2World world = b2GetWorld(jointId.world0);
            b2Joint joint = b2GetJointFullId(world, jointId);
            joint.userData = userData;
        }

        public static object b2Joint_GetUserData(b2JointId jointId)
        {
            b2World world = b2GetWorld(jointId.world0);
            b2Joint joint = b2GetJointFullId(world, jointId);
            return joint.userData;
        }

        public static void b2Joint_WakeBodies(b2JointId jointId)
        {
            b2World world = b2GetWorldLocked(jointId.world0);
            if (world == null)
            {
                return;
            }

            b2Joint joint = b2GetJointFullId(world, jointId);
            b2Body bodyA = b2Array_Get(ref world.bodies, joint.edges[0].bodyId);
            b2Body bodyB = b2Array_Get(ref world.bodies, joint.edges[1].bodyId);

            b2WakeBody(world, bodyA);
            b2WakeBody(world, bodyB);
        }

        public static b2Vec2 b2Joint_GetConstraintForce(b2JointId jointId)
        {
            b2World world = b2GetWorld(jointId.world0);
            b2Joint joint = b2GetJointFullId(world, jointId);
            b2JointSim @base = b2GetJointSim(world, joint);

            switch (joint.type)
            {
                case b2JointType.b2_distanceJoint:
                    return b2GetDistanceJointForce(world, @base);

                case b2JointType.b2_motorJoint:
                    return b2GetMotorJointForce(world, @base);

                case b2JointType.b2_mouseJoint:
                    return b2GetMouseJointForce(world, @base);

                case b2JointType.b2_nullJoint:
                    return b2Vec2_zero;

                case b2JointType.b2_prismaticJoint:
                    return b2GetPrismaticJointForce(world, @base);

                case b2JointType.b2_revoluteJoint:
                    return b2GetRevoluteJointForce(world, @base);

                case b2JointType.b2_weldJoint:
                    return b2GetWeldJointForce(world, @base);

                case b2JointType.b2_wheelJoint:
                    return b2GetWheelJointForce(world, @base);

                default:
                    Debug.Assert(false);
                    return b2Vec2_zero;
            }
        }

        public static float b2Joint_GetConstraintTorque(b2JointId jointId)
        {
            b2World world = b2GetWorld(jointId.world0);
            b2Joint joint = b2GetJointFullId(world, jointId);
            b2JointSim @base = b2GetJointSim(world, joint);

            switch (joint.type)
            {
                case b2JointType.b2_distanceJoint:
                    return 0.0f;

                case b2JointType.b2_motorJoint:
                    return b2GetMotorJointTorque(world, @base);

                case b2JointType.b2_mouseJoint:
                    return b2GetMouseJointTorque(world, @base);

                case b2JointType.b2_nullJoint:
                    return 0.0f;

                case b2JointType.b2_prismaticJoint:
                    return b2GetPrismaticJointTorque(world, @base);

                case b2JointType.b2_revoluteJoint:
                    return b2GetRevoluteJointTorque(world, @base);

                case b2JointType.b2_weldJoint:
                    return b2GetWeldJointTorque(world, @base);

                case b2JointType.b2_wheelJoint:
                    return b2GetWheelJointTorque(world, @base);

                default:
                    Debug.Assert(false);
                    return 0.0f;
            }
        }

        public static void b2PrepareJoint(b2JointSim joint, b2StepContext context)
        {
            switch (joint.type)
            {
                case b2JointType.b2_distanceJoint:
                    b2PrepareDistanceJoint(joint, context);
                    break;

                case b2JointType.b2_motorJoint:
                    b2PrepareMotorJoint(joint, context);
                    break;

                case b2JointType.b2_mouseJoint:
                    b2PrepareMouseJoint(joint, context);
                    break;

                case b2JointType.b2_nullJoint:
                    break;

                case b2JointType.b2_prismaticJoint:
                    b2PreparePrismaticJoint(joint, context);
                    break;

                case b2JointType.b2_revoluteJoint:
                    b2PrepareRevoluteJoint(joint, context);
                    break;

                case b2JointType.b2_weldJoint:
                    b2PrepareWeldJoint(joint, context);
                    break;

                case b2JointType.b2_wheelJoint:
                    b2PrepareWheelJoint(joint, context);
                    break;

                default:
                    Debug.Assert(false);
                    break;
            }
        }

        public static void b2WarmStartJoint(b2JointSim joint, b2StepContext context)
        {
            switch (joint.type)
            {
                case b2JointType.b2_distanceJoint:
                    b2WarmStartDistanceJoint(joint, context);
                    break;

                case b2JointType.b2_motorJoint:
                    b2WarmStartMotorJoint(joint, context);
                    break;

                case b2JointType.b2_mouseJoint:
                    b2WarmStartMouseJoint(joint, context);
                    break;

                case b2JointType.b2_nullJoint:
                    break;

                case b2JointType.b2_prismaticJoint:
                    b2WarmStartPrismaticJoint(joint, context);
                    break;

                case b2JointType.b2_revoluteJoint:
                    b2WarmStartRevoluteJoint(joint, context);
                    break;

                case b2JointType.b2_weldJoint:
                    b2WarmStartWeldJoint(joint, context);
                    break;

                case b2JointType.b2_wheelJoint:
                    b2WarmStartWheelJoint(joint, context);
                    break;

                default:
                    Debug.Assert(false);
                    break;
            }
        }

        public static void b2SolveJoint(b2JointSim joint, b2StepContext context, bool useBias)
        {
            switch (joint.type)
            {
                case b2JointType.b2_distanceJoint:
                    b2SolveDistanceJoint(joint, context, useBias);
                    break;

                case b2JointType.b2_motorJoint:
                    b2SolveMotorJoint(joint, context, useBias);
                    break;

                case b2JointType.b2_mouseJoint:
                    b2SolveMouseJoint(joint, context);
                    break;

                case b2JointType.b2_nullJoint:
                    break;

                case b2JointType.b2_prismaticJoint:
                    b2SolvePrismaticJoint(joint, context, useBias);
                    break;

                case b2JointType.b2_revoluteJoint:
                    b2SolveRevoluteJoint(joint, context, useBias);
                    break;

                case b2JointType.b2_weldJoint:
                    b2SolveWeldJoint(joint, context, useBias);
                    break;

                case b2JointType.b2_wheelJoint:
                    b2SolveWheelJoint(joint, context, useBias);
                    break;

                default:
                    Debug.Assert(false);
                    break;
            }
        }

        public static void b2PrepareOverflowJoints(b2StepContext context)
        {
            b2TracyCZoneNC(b2TracyCZone.prepare_joints, "PrepJoints", b2HexColor.b2_colorOldLace, true);

            b2ConstraintGraph graph = context.graph;
            b2JointSim[] joints = graph.colors[B2_OVERFLOW_INDEX].jointSims.data;
            int jointCount = graph.colors[B2_OVERFLOW_INDEX].jointSims.count;

            for (int i = 0; i < jointCount; ++i)
            {
                b2JointSim joint = joints[i];
                b2PrepareJoint(joint, context);
            }

            b2TracyCZoneEnd(b2TracyCZone.prepare_joints);
        }

        public static void b2WarmStartOverflowJoints(b2StepContext context)
        {
            b2TracyCZoneNC(b2TracyCZone.prepare_joints, "PrepJoints", b2HexColor.b2_colorOldLace, true);

            b2ConstraintGraph graph = context.graph;
            b2JointSim[] joints = graph.colors[B2_OVERFLOW_INDEX].jointSims.data;
            int jointCount = graph.colors[B2_OVERFLOW_INDEX].jointSims.count;

            for (int i = 0; i < jointCount; ++i)
            {
                b2JointSim joint = joints[i];
                b2WarmStartJoint(joint, context);
            }

            b2TracyCZoneEnd(b2TracyCZone.prepare_joints);
        }

        public static void b2SolveOverflowJoints(b2StepContext context, bool useBias)
        {
            b2TracyCZoneNC(b2TracyCZone.solve_joints, "SolveJoints", b2HexColor.b2_colorLemonChiffon, true);

            b2ConstraintGraph graph = context.graph;
            b2JointSim[] joints = graph.colors[B2_OVERFLOW_INDEX].jointSims.data;
            int jointCount = graph.colors[B2_OVERFLOW_INDEX].jointSims.count;

            for (int i = 0; i < jointCount; ++i)
            {
                b2JointSim joint = joints[i];
                b2SolveJoint(joint, context, useBias);
            }

            b2TracyCZoneEnd(b2TracyCZone.solve_joints);
        }

        public static void b2DrawJoint(b2DebugDraw draw, b2World world, b2Joint joint)
        {
            b2Body bodyA = b2Array_Get(ref world.bodies, joint.edges[0].bodyId);
            b2Body bodyB = b2Array_Get(ref world.bodies, joint.edges[1].bodyId);
            if (bodyA.setIndex == (int)b2SetType.b2_disabledSet || bodyB.setIndex == (int)b2SetType.b2_disabledSet)
            {
                return;
            }

            b2JointSim jointSim = b2GetJointSim(world, joint);

            b2Transform transformA = b2GetBodyTransformQuick(world, bodyA);
            b2Transform transformB = b2GetBodyTransformQuick(world, bodyB);
            b2Vec2 pA = b2TransformPoint(transformA, jointSim.localOriginAnchorA);
            b2Vec2 pB = b2TransformPoint(transformB, jointSim.localOriginAnchorB);

            b2HexColor color = b2HexColor.b2_colorDarkSeaGreen;

            switch (joint.type)
            {
                case b2JointType.b2_distanceJoint:
                    b2DrawDistanceJoint(draw, jointSim, transformA, transformB);
                    break;

                case b2JointType.b2_mouseJoint:
                {
                    b2Vec2 target = jointSim.mouseJoint.targetA;

                    b2HexColor c1 = b2HexColor.b2_colorGreen;
                    draw.DrawPoint(target, 4.0f, c1, draw.context);
                    draw.DrawPoint(pB, 4.0f, c1, draw.context);

                    b2HexColor c2 = b2HexColor.b2_colorLightGray;
                    draw.DrawSegment(target, pB, c2, draw.context);
                }
                    break;

                case b2JointType.b2_nullJoint:
                {
                    draw.DrawSegment(pA, pB, b2HexColor.b2_colorGold, draw.context);
                }
                    break;

                case b2JointType.b2_prismaticJoint:
                    b2DrawPrismaticJoint(draw, jointSim, transformA, transformB);
                    break;

                case b2JointType.b2_revoluteJoint:
                    b2DrawRevoluteJoint(draw, jointSim, transformA, transformB, joint.drawSize);
                    break;

                case b2JointType.b2_wheelJoint:
                    b2DrawWheelJoint(draw, jointSim, transformA, transformB);
                    break;

                default:
                    draw.DrawSegment(transformA.p, pA, color, draw.context);
                    draw.DrawSegment(pA, pB, color, draw.context);
                    draw.DrawSegment(transformB.p, pB, color, draw.context);
                    break;
            }

            if (draw.drawGraphColors)
            {
                b2HexColor[] colors = new b2HexColor[B2_GRAPH_COLOR_COUNT]
                {
                    b2HexColor.b2_colorRed, b2HexColor.b2_colorOrange, b2HexColor.b2_colorYellow, b2HexColor.b2_colorGreen,
                    b2HexColor.b2_colorCyan, b2HexColor.b2_colorBlue, b2HexColor.b2_colorViolet, b2HexColor.b2_colorPink,
                    b2HexColor.b2_colorChocolate, b2HexColor.b2_colorGoldenRod, b2HexColor.b2_colorCoral, b2HexColor.b2_colorBlack
                };

                int colorIndex = joint.colorIndex;
                if (colorIndex != B2_NULL_INDEX)
                {
                    b2Vec2 p = b2Lerp(pA, pB, 0.5f);
                    draw.DrawPoint(p, 5.0f, colors[colorIndex], draw.context);
                }
            }
        }
    }
}