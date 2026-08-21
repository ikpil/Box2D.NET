// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public static partial class B2Recordings
    {
        // Per op arg writers (no framing) and full writers (framing plus args), generated from the
        // manifest. Create ops reach the arg writer directly so the call site can append the returned
        // id inside the same record; void ops reach the full writer through B2_REC.
        // Record a void op. One branch when recording is off, args built inside the branch.
        // C# call sites spell out that B2_REC expansion so the argument struct has the same lifetime.
        // Record a create op and its returned id in one framed record. The id is appended after
        // the args so replay can assert it matches. Place this after the real create call.
        // Codegen pass 1b: arg writers. Each generated function writes its struct fields to the buffer

        internal static void b2RecWriteArgs_CreateWorld(B2Recording rec, in B2RecArgs_CreateWorld a)
        {
            b2RecW_WORLDDEF(ref rec.buffer, a.def);
        }

        internal static void b2RecWriteArgs_DestroyWorld(B2Recording rec, in B2RecArgs_DestroyWorld a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
        }

        internal static void b2RecWriteArgs_Step(B2Recording rec, in B2RecArgs_Step a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_F32(ref rec.buffer, a.dt);
            b2RecW_I32(ref rec.buffer, a.subStepCount);
        }

        internal static void b2RecWriteArgs_WorldEnableSleeping(B2Recording rec, in B2RecArgs_WorldEnableSleeping a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_BOOL(ref rec.buffer, a.flag);
        }

        internal static void b2RecWriteArgs_WorldEnableContinuous(B2Recording rec, in B2RecArgs_WorldEnableContinuous a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_BOOL(ref rec.buffer, a.flag);
        }

        internal static void b2RecWriteArgs_WorldSetRestitutionThreshold(B2Recording rec, in B2RecArgs_WorldSetRestitutionThreshold a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_F32(ref rec.buffer, a.value);
        }

        internal static void b2RecWriteArgs_WorldSetHitEventThreshold(B2Recording rec, in B2RecArgs_WorldSetHitEventThreshold a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_F32(ref rec.buffer, a.value);
        }

        internal static void b2RecWriteArgs_WorldSetGravity(B2Recording rec, in B2RecArgs_WorldSetGravity a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_VEC2(ref rec.buffer, a.gravity);
        }

        internal static void b2RecWriteArgs_WorldExplode(B2Recording rec, in B2RecArgs_WorldExplode a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_EXPLOSIONDEF(ref rec.buffer, a.def);
        }

        internal static void b2RecWriteArgs_WorldSetContactTuning(B2Recording rec, in B2RecArgs_WorldSetContactTuning a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_F32(ref rec.buffer, a.hertz);
            b2RecW_F32(ref rec.buffer, a.dampingRatio);
            b2RecW_F32(ref rec.buffer, a.pushSpeed);
        }

        internal static void b2RecWriteArgs_WorldSetContactRecycleDistance(B2Recording rec, in B2RecArgs_WorldSetContactRecycleDistance a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_F32(ref rec.buffer, a.recycleDistance);
        }

        internal static void b2RecWriteArgs_WorldSetMaximumLinearSpeed(B2Recording rec, in B2RecArgs_WorldSetMaximumLinearSpeed a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_F32(ref rec.buffer, a.maximumLinearSpeed);
        }

        internal static void b2RecWriteArgs_WorldEnableWarmStarting(B2Recording rec, in B2RecArgs_WorldEnableWarmStarting a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_BOOL(ref rec.buffer, a.flag);
        }

        internal static void b2RecWriteArgs_WorldRebuildStaticTree(B2Recording rec, in B2RecArgs_WorldRebuildStaticTree a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
        }

        internal static void b2RecWriteArgs_WorldEnableSpeculative(B2Recording rec, in B2RecArgs_WorldEnableSpeculative a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_BOOL(ref rec.buffer, a.flag);
        }

        internal static void b2RecWriteArgs_CreateBody(B2Recording rec, in B2RecArgs_CreateBody a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_BODYDEF(ref rec.buffer, a.def);
        }

        internal static void b2RecWriteArgs_DestroyBody(B2Recording rec, in B2RecArgs_DestroyBody a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
        }

        internal static void b2RecWriteArgs_BodySetTransform(B2Recording rec, in B2RecArgs_BodySetTransform a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_VEC2(ref rec.buffer, a.position);
            b2RecW_ROT(ref rec.buffer, a.rotation);
        }

        internal static void b2RecWriteArgs_BodySetLinearVelocity(B2Recording rec, in B2RecArgs_BodySetLinearVelocity a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_VEC2(ref rec.buffer, a.v);
        }

        internal static void b2RecWriteArgs_BodySetType(B2Recording rec, in B2RecArgs_BodySetType a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_I32(ref rec.buffer, a.type);
        }

        internal static void b2RecWriteArgs_BodySetName(B2Recording rec, in B2RecArgs_BodySetName a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_STR(ref rec.buffer, a.name);
        }

        internal static void b2RecWriteArgs_BodySetAngularVelocity(B2Recording rec, in B2RecArgs_BodySetAngularVelocity a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_F32(ref rec.buffer, a.w);
        }

        internal static void b2RecWriteArgs_BodySetTargetTransform(B2Recording rec, in B2RecArgs_BodySetTargetTransform a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_XF(ref rec.buffer, a.target);
            b2RecW_F32(ref rec.buffer, a.timeStep);
            b2RecW_BOOL(ref rec.buffer, a.wake);
        }

        internal static void b2RecWriteArgs_BodyApplyForce(B2Recording rec, in B2RecArgs_BodyApplyForce a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_VEC2(ref rec.buffer, a.force);
            b2RecW_VEC2(ref rec.buffer, a.point);
            b2RecW_BOOL(ref rec.buffer, a.wake);
        }

        internal static void b2RecWriteArgs_BodyApplyForceToCenter(B2Recording rec, in B2RecArgs_BodyApplyForceToCenter a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_VEC2(ref rec.buffer, a.force);
            b2RecW_BOOL(ref rec.buffer, a.wake);
        }

        internal static void b2RecWriteArgs_BodyApplyTorque(B2Recording rec, in B2RecArgs_BodyApplyTorque a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_F32(ref rec.buffer, a.torque);
            b2RecW_BOOL(ref rec.buffer, a.wake);
        }

        internal static void b2RecWriteArgs_BodyClearForces(B2Recording rec, in B2RecArgs_BodyClearForces a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
        }

        internal static void b2RecWriteArgs_BodyApplyLinearImpulse(B2Recording rec, in B2RecArgs_BodyApplyLinearImpulse a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_VEC2(ref rec.buffer, a.impulse);
            b2RecW_VEC2(ref rec.buffer, a.point);
            b2RecW_BOOL(ref rec.buffer, a.wake);
        }

        internal static void b2RecWriteArgs_BodyApplyLinearImpulseToCenter(B2Recording rec, in B2RecArgs_BodyApplyLinearImpulseToCenter a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_VEC2(ref rec.buffer, a.impulse);
            b2RecW_BOOL(ref rec.buffer, a.wake);
        }

        internal static void b2RecWriteArgs_BodyApplyAngularImpulse(B2Recording rec, in B2RecArgs_BodyApplyAngularImpulse a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_F32(ref rec.buffer, a.impulse);
            b2RecW_BOOL(ref rec.buffer, a.wake);
        }

        internal static void b2RecWriteArgs_BodySetMassData(B2Recording rec, in B2RecArgs_BodySetMassData a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_MASSDATA(ref rec.buffer, a.massData);
        }

        internal static void b2RecWriteArgs_BodyApplyMassFromShapes(B2Recording rec, in B2RecArgs_BodyApplyMassFromShapes a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
        }

        internal static void b2RecWriteArgs_BodySetLinearDamping(B2Recording rec, in B2RecArgs_BodySetLinearDamping a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_F32(ref rec.buffer, a.damping);
        }

        internal static void b2RecWriteArgs_BodySetAngularDamping(B2Recording rec, in B2RecArgs_BodySetAngularDamping a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_F32(ref rec.buffer, a.damping);
        }

        internal static void b2RecWriteArgs_BodySetGravityScale(B2Recording rec, in B2RecArgs_BodySetGravityScale a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_F32(ref rec.buffer, a.scale);
        }

        internal static void b2RecWriteArgs_BodySetAwake(B2Recording rec, in B2RecArgs_BodySetAwake a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_BOOL(ref rec.buffer, a.awake);
        }

        internal static void b2RecWriteArgs_BodyWakeTouching(B2Recording rec, in B2RecArgs_BodyWakeTouching a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
        }

        internal static void b2RecWriteArgs_BodyEnableSleep(B2Recording rec, in B2RecArgs_BodyEnableSleep a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_BOOL(ref rec.buffer, a.flag);
        }

        internal static void b2RecWriteArgs_BodySetSleepThreshold(B2Recording rec, in B2RecArgs_BodySetSleepThreshold a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_F32(ref rec.buffer, a.threshold);
        }

        internal static void b2RecWriteArgs_BodyDisable(B2Recording rec, in B2RecArgs_BodyDisable a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
        }

        internal static void b2RecWriteArgs_BodyEnable(B2Recording rec, in B2RecArgs_BodyEnable a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
        }

        internal static void b2RecWriteArgs_BodySetMotionLocks(B2Recording rec, in B2RecArgs_BodySetMotionLocks a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_LOCKS(ref rec.buffer, a.locks);
        }

        internal static void b2RecWriteArgs_BodySetBullet(B2Recording rec, in B2RecArgs_BodySetBullet a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_BOOL(ref rec.buffer, a.flag);
        }

        internal static void b2RecWriteArgs_BodyEnableContactRecycling(B2Recording rec, in B2RecArgs_BodyEnableContactRecycling a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_BOOL(ref rec.buffer, a.flag);
        }

        internal static void b2RecWriteArgs_BodyEnableContactEvents(B2Recording rec, in B2RecArgs_BodyEnableContactEvents a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_BOOL(ref rec.buffer, a.flag);
        }

        internal static void b2RecWriteArgs_BodyEnableHitEvents(B2Recording rec, in B2RecArgs_BodyEnableHitEvents a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_BOOL(ref rec.buffer, a.flag);
        }

        internal static void b2RecWriteArgs_CreateCircleShape(B2Recording rec, in B2RecArgs_CreateCircleShape a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_SHAPEDEF(ref rec.buffer, a.def);
            b2RecW_CIRCLE(ref rec.buffer, a.circle);
        }

        internal static void b2RecWriteArgs_CreateCapsuleShape(B2Recording rec, in B2RecArgs_CreateCapsuleShape a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_SHAPEDEF(ref rec.buffer, a.def);
            b2RecW_CAPSULE(ref rec.buffer, a.capsule);
        }

        internal static void b2RecWriteArgs_CreateSegmentShape(B2Recording rec, in B2RecArgs_CreateSegmentShape a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_SHAPEDEF(ref rec.buffer, a.def);
            b2RecW_SEGMENT(ref rec.buffer, a.segment);
        }

        internal static void b2RecWriteArgs_CreatePolygonShape(B2Recording rec, in B2RecArgs_CreatePolygonShape a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_SHAPEDEF(ref rec.buffer, a.def);
            b2RecW_POLYGON(ref rec.buffer, a.polygon);
        }

        internal static void b2RecWriteArgs_CreateChainSegmentShape(B2Recording rec, in B2RecArgs_CreateChainSegmentShape a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_SHAPEDEF(ref rec.buffer, a.def);
            b2RecW_CHAINSEG(ref rec.buffer, a.chainSegment);
        }

        internal static void b2RecWriteArgs_DestroyShape(B2Recording rec, in B2RecArgs_DestroyShape a)
        {
            b2RecW_SHAPEID(ref rec.buffer, a.shape);
            b2RecW_BOOL(ref rec.buffer, a.updateBodyMass);
        }

        internal static void b2RecWriteArgs_ShapeSetDensity(B2Recording rec, in B2RecArgs_ShapeSetDensity a)
        {
            b2RecW_SHAPEID(ref rec.buffer, a.shape);
            b2RecW_F32(ref rec.buffer, a.density);
            b2RecW_BOOL(ref rec.buffer, a.updateBodyMass);
        }

        internal static void b2RecWriteArgs_ShapeSetFriction(B2Recording rec, in B2RecArgs_ShapeSetFriction a)
        {
            b2RecW_SHAPEID(ref rec.buffer, a.shape);
            b2RecW_F32(ref rec.buffer, a.friction);
        }

        internal static void b2RecWriteArgs_ShapeSetRestitution(B2Recording rec, in B2RecArgs_ShapeSetRestitution a)
        {
            b2RecW_SHAPEID(ref rec.buffer, a.shape);
            b2RecW_F32(ref rec.buffer, a.restitution);
        }

        internal static void b2RecWriteArgs_ShapeSetUserMaterial(B2Recording rec, in B2RecArgs_ShapeSetUserMaterial a)
        {
            b2RecW_SHAPEID(ref rec.buffer, a.shape);
            b2RecW_U64(ref rec.buffer, a.material);
        }

        internal static void b2RecWriteArgs_ShapeSetSurfaceMaterial(B2Recording rec, in B2RecArgs_ShapeSetSurfaceMaterial a)
        {
            b2RecW_SHAPEID(ref rec.buffer, a.shape);
            b2RecW_MATERIAL(ref rec.buffer, a.material);
        }

        internal static void b2RecWriteArgs_ShapeSetFilter(B2Recording rec, in B2RecArgs_ShapeSetFilter a)
        {
            b2RecW_SHAPEID(ref rec.buffer, a.shape);
            b2RecW_FILTER(ref rec.buffer, a.filter);
        }

        internal static void b2RecWriteArgs_ShapeEnableSensorEvents(B2Recording rec, in B2RecArgs_ShapeEnableSensorEvents a)
        {
            b2RecW_SHAPEID(ref rec.buffer, a.shape);
            b2RecW_BOOL(ref rec.buffer, a.flag);
        }

        internal static void b2RecWriteArgs_ShapeEnableContactEvents(B2Recording rec, in B2RecArgs_ShapeEnableContactEvents a)
        {
            b2RecW_SHAPEID(ref rec.buffer, a.shape);
            b2RecW_BOOL(ref rec.buffer, a.flag);
        }

        internal static void b2RecWriteArgs_ShapeEnablePreSolveEvents(B2Recording rec, in B2RecArgs_ShapeEnablePreSolveEvents a)
        {
            b2RecW_SHAPEID(ref rec.buffer, a.shape);
            b2RecW_BOOL(ref rec.buffer, a.flag);
        }

        internal static void b2RecWriteArgs_ShapeEnableHitEvents(B2Recording rec, in B2RecArgs_ShapeEnableHitEvents a)
        {
            b2RecW_SHAPEID(ref rec.buffer, a.shape);
            b2RecW_BOOL(ref rec.buffer, a.flag);
        }

        internal static void b2RecWriteArgs_ShapeSetCircle(B2Recording rec, in B2RecArgs_ShapeSetCircle a)
        {
            b2RecW_SHAPEID(ref rec.buffer, a.shape);
            b2RecW_CIRCLE(ref rec.buffer, a.circle);
        }

        internal static void b2RecWriteArgs_ShapeSetCapsule(B2Recording rec, in B2RecArgs_ShapeSetCapsule a)
        {
            b2RecW_SHAPEID(ref rec.buffer, a.shape);
            b2RecW_CAPSULE(ref rec.buffer, a.capsule);
        }

        internal static void b2RecWriteArgs_ShapeSetSegment(B2Recording rec, in B2RecArgs_ShapeSetSegment a)
        {
            b2RecW_SHAPEID(ref rec.buffer, a.shape);
            b2RecW_SEGMENT(ref rec.buffer, a.segment);
        }

        internal static void b2RecWriteArgs_ShapeSetPolygon(B2Recording rec, in B2RecArgs_ShapeSetPolygon a)
        {
            b2RecW_SHAPEID(ref rec.buffer, a.shape);
            b2RecW_POLYGON(ref rec.buffer, a.polygon);
        }

        internal static void b2RecWriteArgs_ShapeSetChainSegment(B2Recording rec, in B2RecArgs_ShapeSetChainSegment a)
        {
            b2RecW_SHAPEID(ref rec.buffer, a.shape);
            b2RecW_CHAINSEG(ref rec.buffer, a.chainSegment);
        }

        internal static void b2RecWriteArgs_ShapeApplyWind(B2Recording rec, in B2RecArgs_ShapeApplyWind a)
        {
            b2RecW_SHAPEID(ref rec.buffer, a.shape);
            b2RecW_VEC2(ref rec.buffer, a.wind);
            b2RecW_F32(ref rec.buffer, a.drag);
            b2RecW_F32(ref rec.buffer, a.lift);
            b2RecW_BOOL(ref rec.buffer, a.wake);
        }

        internal static void b2RecWriteArgs_CreateChain(B2Recording rec, in B2RecArgs_CreateChain a)
        {
            b2RecW_BODYID(ref rec.buffer, a.body);
            b2RecW_CHAINDEF(ref rec.buffer, a.def);
        }

        internal static void b2RecWriteArgs_DestroyChain(B2Recording rec, in B2RecArgs_DestroyChain a)
        {
            b2RecW_CHAINID(ref rec.buffer, a.chain);
        }

        internal static void b2RecWriteArgs_ChainSetSurfaceMaterial(B2Recording rec, in B2RecArgs_ChainSetSurfaceMaterial a)
        {
            b2RecW_CHAINID(ref rec.buffer, a.chain);
            b2RecW_MATERIAL(ref rec.buffer, a.material);
            b2RecW_I32(ref rec.buffer, a.materialIndex);
        }

        internal static void b2RecWriteArgs_CreateDistanceJoint(B2Recording rec, in B2RecArgs_CreateDistanceJoint a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_DISTANCEJOINTDEF(ref rec.buffer, a.def);
        }

        internal static void b2RecWriteArgs_CreateMotorJoint(B2Recording rec, in B2RecArgs_CreateMotorJoint a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_MOTORJOINTDEF(ref rec.buffer, a.def);
        }

        internal static void b2RecWriteArgs_CreateFilterJoint(B2Recording rec, in B2RecArgs_CreateFilterJoint a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_FILTERJOINTDEF(ref rec.buffer, a.def);
        }

        internal static void b2RecWriteArgs_CreatePrismaticJoint(B2Recording rec, in B2RecArgs_CreatePrismaticJoint a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_PRISMATICJOINTDEF(ref rec.buffer, a.def);
        }

        internal static void b2RecWriteArgs_CreateRevoluteJoint(B2Recording rec, in B2RecArgs_CreateRevoluteJoint a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_REVOLUTEJOINTDEF(ref rec.buffer, a.def);
        }

        internal static void b2RecWriteArgs_CreateWeldJoint(B2Recording rec, in B2RecArgs_CreateWeldJoint a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_WELDJOINTDEF(ref rec.buffer, a.def);
        }

        internal static void b2RecWriteArgs_CreateWheelJoint(B2Recording rec, in B2RecArgs_CreateWheelJoint a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_WHEELJOINTDEF(ref rec.buffer, a.def);
        }

        internal static void b2RecWriteArgs_DestroyJoint(B2Recording rec, in B2RecArgs_DestroyJoint a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_BOOL(ref rec.buffer, a.wakeAttached);
        }

        internal static void b2RecWriteArgs_JointSetLocalFrameA(B2Recording rec, in B2RecArgs_JointSetLocalFrameA a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_XF(ref rec.buffer, a.localFrame);
        }

        internal static void b2RecWriteArgs_JointSetLocalFrameB(B2Recording rec, in B2RecArgs_JointSetLocalFrameB a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_XF(ref rec.buffer, a.localFrame);
        }

        internal static void b2RecWriteArgs_JointSetCollideConnected(B2Recording rec, in B2RecArgs_JointSetCollideConnected a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_BOOL(ref rec.buffer, a.shouldCollide);
        }

        internal static void b2RecWriteArgs_JointWakeBodies(B2Recording rec, in B2RecArgs_JointWakeBodies a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
        }

        internal static void b2RecWriteArgs_JointSetConstraintTuning(B2Recording rec, in B2RecArgs_JointSetConstraintTuning a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.hertz);
            b2RecW_F32(ref rec.buffer, a.dampingRatio);
        }

        internal static void b2RecWriteArgs_JointSetForceThreshold(B2Recording rec, in B2RecArgs_JointSetForceThreshold a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.threshold);
        }

        internal static void b2RecWriteArgs_JointSetTorqueThreshold(B2Recording rec, in B2RecArgs_JointSetTorqueThreshold a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.threshold);
        }

        internal static void b2RecWriteArgs_DistanceJointSetLength(B2Recording rec, in B2RecArgs_DistanceJointSetLength a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.length);
        }

        internal static void b2RecWriteArgs_DistanceJointEnableSpring(B2Recording rec, in B2RecArgs_DistanceJointEnableSpring a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_BOOL(ref rec.buffer, a.enableSpring);
        }

        internal static void b2RecWriteArgs_DistanceJointSetSpringForceRange(B2Recording rec, in B2RecArgs_DistanceJointSetSpringForceRange a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.lowerForce);
            b2RecW_F32(ref rec.buffer, a.upperForce);
        }

        internal static void b2RecWriteArgs_DistanceJointSetSpringHertz(B2Recording rec, in B2RecArgs_DistanceJointSetSpringHertz a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.hertz);
        }

        internal static void b2RecWriteArgs_DistanceJointSetSpringDampingRatio(B2Recording rec, in B2RecArgs_DistanceJointSetSpringDampingRatio a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.dampingRatio);
        }

        internal static void b2RecWriteArgs_DistanceJointEnableLimit(B2Recording rec, in B2RecArgs_DistanceJointEnableLimit a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_BOOL(ref rec.buffer, a.enableLimit);
        }

        internal static void b2RecWriteArgs_DistanceJointSetLengthRange(B2Recording rec, in B2RecArgs_DistanceJointSetLengthRange a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.minLength);
            b2RecW_F32(ref rec.buffer, a.maxLength);
        }

        internal static void b2RecWriteArgs_DistanceJointEnableMotor(B2Recording rec, in B2RecArgs_DistanceJointEnableMotor a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_BOOL(ref rec.buffer, a.enableMotor);
        }

        internal static void b2RecWriteArgs_DistanceJointSetMotorSpeed(B2Recording rec, in B2RecArgs_DistanceJointSetMotorSpeed a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.motorSpeed);
        }

        internal static void b2RecWriteArgs_DistanceJointSetMaxMotorForce(B2Recording rec, in B2RecArgs_DistanceJointSetMaxMotorForce a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.force);
        }

        internal static void b2RecWriteArgs_MotorJointSetLinearVelocity(B2Recording rec, in B2RecArgs_MotorJointSetLinearVelocity a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_VEC2(ref rec.buffer, a.velocity);
        }

        internal static void b2RecWriteArgs_MotorJointSetAngularVelocity(B2Recording rec, in B2RecArgs_MotorJointSetAngularVelocity a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.velocity);
        }

        internal static void b2RecWriteArgs_MotorJointSetMaxVelocityForce(B2Recording rec, in B2RecArgs_MotorJointSetMaxVelocityForce a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.maxForce);
        }

        internal static void b2RecWriteArgs_MotorJointSetMaxVelocityTorque(B2Recording rec, in B2RecArgs_MotorJointSetMaxVelocityTorque a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.maxTorque);
        }

        internal static void b2RecWriteArgs_MotorJointSetLinearHertz(B2Recording rec, in B2RecArgs_MotorJointSetLinearHertz a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.hertz);
        }

        internal static void b2RecWriteArgs_MotorJointSetLinearDampingRatio(B2Recording rec, in B2RecArgs_MotorJointSetLinearDampingRatio a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.damping);
        }

        internal static void b2RecWriteArgs_MotorJointSetAngularHertz(B2Recording rec, in B2RecArgs_MotorJointSetAngularHertz a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.hertz);
        }

        internal static void b2RecWriteArgs_MotorJointSetAngularDampingRatio(B2Recording rec, in B2RecArgs_MotorJointSetAngularDampingRatio a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.damping);
        }

        internal static void b2RecWriteArgs_MotorJointSetMaxSpringForce(B2Recording rec, in B2RecArgs_MotorJointSetMaxSpringForce a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.maxForce);
        }

        internal static void b2RecWriteArgs_MotorJointSetMaxSpringTorque(B2Recording rec, in B2RecArgs_MotorJointSetMaxSpringTorque a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.maxTorque);
        }

        internal static void b2RecWriteArgs_PrismaticJointEnableSpring(B2Recording rec, in B2RecArgs_PrismaticJointEnableSpring a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_BOOL(ref rec.buffer, a.enableSpring);
        }

        internal static void b2RecWriteArgs_PrismaticJointSetSpringHertz(B2Recording rec, in B2RecArgs_PrismaticJointSetSpringHertz a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.hertz);
        }

        internal static void b2RecWriteArgs_PrismaticJointSetSpringDampingRatio(B2Recording rec, in B2RecArgs_PrismaticJointSetSpringDampingRatio a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.dampingRatio);
        }

        internal static void b2RecWriteArgs_PrismaticJointSetTargetTranslation(B2Recording rec, in B2RecArgs_PrismaticJointSetTargetTranslation a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.translation);
        }

        internal static void b2RecWriteArgs_PrismaticJointEnableLimit(B2Recording rec, in B2RecArgs_PrismaticJointEnableLimit a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_BOOL(ref rec.buffer, a.enableLimit);
        }

        internal static void b2RecWriteArgs_PrismaticJointSetLimits(B2Recording rec, in B2RecArgs_PrismaticJointSetLimits a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.lower);
            b2RecW_F32(ref rec.buffer, a.upper);
        }

        internal static void b2RecWriteArgs_PrismaticJointEnableMotor(B2Recording rec, in B2RecArgs_PrismaticJointEnableMotor a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_BOOL(ref rec.buffer, a.enableMotor);
        }

        internal static void b2RecWriteArgs_PrismaticJointSetMotorSpeed(B2Recording rec, in B2RecArgs_PrismaticJointSetMotorSpeed a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.motorSpeed);
        }

        internal static void b2RecWriteArgs_PrismaticJointSetMaxMotorForce(B2Recording rec, in B2RecArgs_PrismaticJointSetMaxMotorForce a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.force);
        }

        internal static void b2RecWriteArgs_RevoluteJointEnableSpring(B2Recording rec, in B2RecArgs_RevoluteJointEnableSpring a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_BOOL(ref rec.buffer, a.enableSpring);
        }

        internal static void b2RecWriteArgs_RevoluteJointSetSpringHertz(B2Recording rec, in B2RecArgs_RevoluteJointSetSpringHertz a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.hertz);
        }

        internal static void b2RecWriteArgs_RevoluteJointSetSpringDampingRatio(B2Recording rec, in B2RecArgs_RevoluteJointSetSpringDampingRatio a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.dampingRatio);
        }

        internal static void b2RecWriteArgs_RevoluteJointSetTargetAngle(B2Recording rec, in B2RecArgs_RevoluteJointSetTargetAngle a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.angle);
        }

        internal static void b2RecWriteArgs_RevoluteJointEnableLimit(B2Recording rec, in B2RecArgs_RevoluteJointEnableLimit a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_BOOL(ref rec.buffer, a.enableLimit);
        }

        internal static void b2RecWriteArgs_RevoluteJointSetLimits(B2Recording rec, in B2RecArgs_RevoluteJointSetLimits a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.lower);
            b2RecW_F32(ref rec.buffer, a.upper);
        }

        internal static void b2RecWriteArgs_RevoluteJointEnableMotor(B2Recording rec, in B2RecArgs_RevoluteJointEnableMotor a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_BOOL(ref rec.buffer, a.enableMotor);
        }

        internal static void b2RecWriteArgs_RevoluteJointSetMotorSpeed(B2Recording rec, in B2RecArgs_RevoluteJointSetMotorSpeed a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.motorSpeed);
        }

        internal static void b2RecWriteArgs_RevoluteJointSetMaxMotorTorque(B2Recording rec, in B2RecArgs_RevoluteJointSetMaxMotorTorque a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.torque);
        }

        internal static void b2RecWriteArgs_WeldJointSetLinearHertz(B2Recording rec, in B2RecArgs_WeldJointSetLinearHertz a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.hertz);
        }

        internal static void b2RecWriteArgs_WeldJointSetLinearDampingRatio(B2Recording rec, in B2RecArgs_WeldJointSetLinearDampingRatio a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.dampingRatio);
        }

        internal static void b2RecWriteArgs_WeldJointSetAngularHertz(B2Recording rec, in B2RecArgs_WeldJointSetAngularHertz a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.hertz);
        }

        internal static void b2RecWriteArgs_WeldJointSetAngularDampingRatio(B2Recording rec, in B2RecArgs_WeldJointSetAngularDampingRatio a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.dampingRatio);
        }

        internal static void b2RecWriteArgs_WheelJointEnableSpring(B2Recording rec, in B2RecArgs_WheelJointEnableSpring a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_BOOL(ref rec.buffer, a.enableSpring);
        }

        internal static void b2RecWriteArgs_WheelJointSetSpringHertz(B2Recording rec, in B2RecArgs_WheelJointSetSpringHertz a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.hertz);
        }

        internal static void b2RecWriteArgs_WheelJointSetSpringDampingRatio(B2Recording rec, in B2RecArgs_WheelJointSetSpringDampingRatio a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.dampingRatio);
        }

        internal static void b2RecWriteArgs_WheelJointEnableLimit(B2Recording rec, in B2RecArgs_WheelJointEnableLimit a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_BOOL(ref rec.buffer, a.enableLimit);
        }

        internal static void b2RecWriteArgs_WheelJointSetLimits(B2Recording rec, in B2RecArgs_WheelJointSetLimits a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.lower);
            b2RecW_F32(ref rec.buffer, a.upper);
        }

        internal static void b2RecWriteArgs_WheelJointEnableMotor(B2Recording rec, in B2RecArgs_WheelJointEnableMotor a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_BOOL(ref rec.buffer, a.enableMotor);
        }

        internal static void b2RecWriteArgs_WheelJointSetMotorSpeed(B2Recording rec, in B2RecArgs_WheelJointSetMotorSpeed a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.motorSpeed);
        }

        internal static void b2RecWriteArgs_WheelJointSetMaxMotorTorque(B2Recording rec, in B2RecArgs_WheelJointSetMaxMotorTorque a)
        {
            b2RecW_JOINTID(ref rec.buffer, a.joint);
            b2RecW_F32(ref rec.buffer, a.torque);
        }

        internal static void b2RecWriteArgs_QueryOverlapAABB(B2Recording rec, in B2RecArgs_QueryOverlapAABB a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_AABB(ref rec.buffer, a.aabb);
            b2RecW_QUERYFILTER(ref rec.buffer, a.filter);
        }

        internal static void b2RecWriteArgs_QueryOverlapShape(B2Recording rec, in B2RecArgs_QueryOverlapShape a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_SHAPEPROXY(ref rec.buffer, a.proxy);
            b2RecW_QUERYFILTER(ref rec.buffer, a.filter);
        }

        internal static void b2RecWriteArgs_QueryCastRay(B2Recording rec, in B2RecArgs_QueryCastRay a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_VEC2(ref rec.buffer, a.origin);
            b2RecW_VEC2(ref rec.buffer, a.translation);
            b2RecW_QUERYFILTER(ref rec.buffer, a.filter);
        }

        internal static void b2RecWriteArgs_QueryCastShape(B2Recording rec, in B2RecArgs_QueryCastShape a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_SHAPEPROXY(ref rec.buffer, a.proxy);
            b2RecW_VEC2(ref rec.buffer, a.translation);
            b2RecW_QUERYFILTER(ref rec.buffer, a.filter);
        }

        internal static void b2RecWriteArgs_QueryCollideMover(B2Recording rec, in B2RecArgs_QueryCollideMover a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_CAPSULE(ref rec.buffer, a.mover);
            b2RecW_QUERYFILTER(ref rec.buffer, a.filter);
        }

        internal static void b2RecWriteArgs_QueryCastRayClosest(B2Recording rec, in B2RecArgs_QueryCastRayClosest a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_VEC2(ref rec.buffer, a.origin);
            b2RecW_VEC2(ref rec.buffer, a.translation);
            b2RecW_QUERYFILTER(ref rec.buffer, a.filter);
        }

        internal static void b2RecWriteArgs_QueryCastMover(B2Recording rec, in B2RecArgs_QueryCastMover a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_CAPSULE(ref rec.buffer, a.mover);
            b2RecW_VEC2(ref rec.buffer, a.translation);
            b2RecW_QUERYFILTER(ref rec.buffer, a.filter);
        }

        internal static void b2RecWriteArgs_ShapeTestPoint(B2Recording rec, in B2RecArgs_ShapeTestPoint a)
        {
            b2RecW_SHAPEID(ref rec.buffer, a.shape);
            b2RecW_VEC2(ref rec.buffer, a.point);
        }

        internal static void b2RecWriteArgs_ShapeRayCast(B2Recording rec, in B2RecArgs_ShapeRayCast a)
        {
            b2RecW_SHAPEID(ref rec.buffer, a.shape);
            b2RecW_RAYCASTINPUT(ref rec.buffer, a.input);
        }

        internal static void b2RecWriteArgs_StateHash(B2Recording rec, in B2RecArgs_StateHash a)
        {
            b2RecW_WORLDID(ref rec.buffer, a.world);
            b2RecW_U64(ref rec.buffer, a.hash);
        }

        // Codegen: full writers wrapping begin, arg writer, end

        internal static void b2RecWrite_CreateWorld(B2Recording rec, in B2RecArgs_CreateWorld a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreateWorld);
            b2RecWriteArgs_CreateWorld(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_DestroyWorld(B2Recording rec, in B2RecArgs_DestroyWorld a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.DestroyWorld);
            b2RecWriteArgs_DestroyWorld(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_Step(B2Recording rec, in B2RecArgs_Step a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.Step);
            b2RecWriteArgs_Step(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_WorldEnableSleeping(B2Recording rec, in B2RecArgs_WorldEnableSleeping a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.WorldEnableSleeping);
            b2RecWriteArgs_WorldEnableSleeping(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_WorldEnableContinuous(B2Recording rec, in B2RecArgs_WorldEnableContinuous a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.WorldEnableContinuous);
            b2RecWriteArgs_WorldEnableContinuous(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_WorldSetRestitutionThreshold(B2Recording rec, in B2RecArgs_WorldSetRestitutionThreshold a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.WorldSetRestitutionThreshold);
            b2RecWriteArgs_WorldSetRestitutionThreshold(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_WorldSetHitEventThreshold(B2Recording rec, in B2RecArgs_WorldSetHitEventThreshold a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.WorldSetHitEventThreshold);
            b2RecWriteArgs_WorldSetHitEventThreshold(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_WorldSetGravity(B2Recording rec, in B2RecArgs_WorldSetGravity a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.WorldSetGravity);
            b2RecWriteArgs_WorldSetGravity(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_WorldExplode(B2Recording rec, in B2RecArgs_WorldExplode a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.WorldExplode);
            b2RecWriteArgs_WorldExplode(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_WorldSetContactTuning(B2Recording rec, in B2RecArgs_WorldSetContactTuning a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.WorldSetContactTuning);
            b2RecWriteArgs_WorldSetContactTuning(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_WorldSetContactRecycleDistance(B2Recording rec, in B2RecArgs_WorldSetContactRecycleDistance a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.WorldSetContactRecycleDistance);
            b2RecWriteArgs_WorldSetContactRecycleDistance(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_WorldSetMaximumLinearSpeed(B2Recording rec, in B2RecArgs_WorldSetMaximumLinearSpeed a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.WorldSetMaximumLinearSpeed);
            b2RecWriteArgs_WorldSetMaximumLinearSpeed(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_WorldEnableWarmStarting(B2Recording rec, in B2RecArgs_WorldEnableWarmStarting a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.WorldEnableWarmStarting);
            b2RecWriteArgs_WorldEnableWarmStarting(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_WorldRebuildStaticTree(B2Recording rec, in B2RecArgs_WorldRebuildStaticTree a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.WorldRebuildStaticTree);
            b2RecWriteArgs_WorldRebuildStaticTree(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_WorldEnableSpeculative(B2Recording rec, in B2RecArgs_WorldEnableSpeculative a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.WorldEnableSpeculative);
            b2RecWriteArgs_WorldEnableSpeculative(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_CreateBody(B2Recording rec, in B2RecArgs_CreateBody a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreateBody);
            b2RecWriteArgs_CreateBody(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_DestroyBody(B2Recording rec, in B2RecArgs_DestroyBody a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.DestroyBody);
            b2RecWriteArgs_DestroyBody(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodySetTransform(B2Recording rec, in B2RecArgs_BodySetTransform a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodySetTransform);
            b2RecWriteArgs_BodySetTransform(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodySetLinearVelocity(B2Recording rec, in B2RecArgs_BodySetLinearVelocity a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodySetLinearVelocity);
            b2RecWriteArgs_BodySetLinearVelocity(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodySetType(B2Recording rec, in B2RecArgs_BodySetType a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodySetType);
            b2RecWriteArgs_BodySetType(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodySetName(B2Recording rec, in B2RecArgs_BodySetName a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodySetName);
            b2RecWriteArgs_BodySetName(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodySetAngularVelocity(B2Recording rec, in B2RecArgs_BodySetAngularVelocity a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodySetAngularVelocity);
            b2RecWriteArgs_BodySetAngularVelocity(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodySetTargetTransform(B2Recording rec, in B2RecArgs_BodySetTargetTransform a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodySetTargetTransform);
            b2RecWriteArgs_BodySetTargetTransform(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodyApplyForce(B2Recording rec, in B2RecArgs_BodyApplyForce a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodyApplyForce);
            b2RecWriteArgs_BodyApplyForce(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodyApplyForceToCenter(B2Recording rec, in B2RecArgs_BodyApplyForceToCenter a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodyApplyForceToCenter);
            b2RecWriteArgs_BodyApplyForceToCenter(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodyApplyTorque(B2Recording rec, in B2RecArgs_BodyApplyTorque a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodyApplyTorque);
            b2RecWriteArgs_BodyApplyTorque(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodyClearForces(B2Recording rec, in B2RecArgs_BodyClearForces a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodyClearForces);
            b2RecWriteArgs_BodyClearForces(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodyApplyLinearImpulse(B2Recording rec, in B2RecArgs_BodyApplyLinearImpulse a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodyApplyLinearImpulse);
            b2RecWriteArgs_BodyApplyLinearImpulse(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodyApplyLinearImpulseToCenter(B2Recording rec, in B2RecArgs_BodyApplyLinearImpulseToCenter a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodyApplyLinearImpulseToCenter);
            b2RecWriteArgs_BodyApplyLinearImpulseToCenter(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodyApplyAngularImpulse(B2Recording rec, in B2RecArgs_BodyApplyAngularImpulse a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodyApplyAngularImpulse);
            b2RecWriteArgs_BodyApplyAngularImpulse(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodySetMassData(B2Recording rec, in B2RecArgs_BodySetMassData a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodySetMassData);
            b2RecWriteArgs_BodySetMassData(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodyApplyMassFromShapes(B2Recording rec, in B2RecArgs_BodyApplyMassFromShapes a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodyApplyMassFromShapes);
            b2RecWriteArgs_BodyApplyMassFromShapes(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodySetLinearDamping(B2Recording rec, in B2RecArgs_BodySetLinearDamping a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodySetLinearDamping);
            b2RecWriteArgs_BodySetLinearDamping(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodySetAngularDamping(B2Recording rec, in B2RecArgs_BodySetAngularDamping a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodySetAngularDamping);
            b2RecWriteArgs_BodySetAngularDamping(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodySetGravityScale(B2Recording rec, in B2RecArgs_BodySetGravityScale a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodySetGravityScale);
            b2RecWriteArgs_BodySetGravityScale(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodySetAwake(B2Recording rec, in B2RecArgs_BodySetAwake a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodySetAwake);
            b2RecWriteArgs_BodySetAwake(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodyWakeTouching(B2Recording rec, in B2RecArgs_BodyWakeTouching a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodyWakeTouching);
            b2RecWriteArgs_BodyWakeTouching(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodyEnableSleep(B2Recording rec, in B2RecArgs_BodyEnableSleep a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodyEnableSleep);
            b2RecWriteArgs_BodyEnableSleep(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodySetSleepThreshold(B2Recording rec, in B2RecArgs_BodySetSleepThreshold a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodySetSleepThreshold);
            b2RecWriteArgs_BodySetSleepThreshold(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodyDisable(B2Recording rec, in B2RecArgs_BodyDisable a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodyDisable);
            b2RecWriteArgs_BodyDisable(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodyEnable(B2Recording rec, in B2RecArgs_BodyEnable a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodyEnable);
            b2RecWriteArgs_BodyEnable(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodySetMotionLocks(B2Recording rec, in B2RecArgs_BodySetMotionLocks a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodySetMotionLocks);
            b2RecWriteArgs_BodySetMotionLocks(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodySetBullet(B2Recording rec, in B2RecArgs_BodySetBullet a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodySetBullet);
            b2RecWriteArgs_BodySetBullet(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodyEnableContactRecycling(B2Recording rec, in B2RecArgs_BodyEnableContactRecycling a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodyEnableContactRecycling);
            b2RecWriteArgs_BodyEnableContactRecycling(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodyEnableContactEvents(B2Recording rec, in B2RecArgs_BodyEnableContactEvents a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodyEnableContactEvents);
            b2RecWriteArgs_BodyEnableContactEvents(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_BodyEnableHitEvents(B2Recording rec, in B2RecArgs_BodyEnableHitEvents a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.BodyEnableHitEvents);
            b2RecWriteArgs_BodyEnableHitEvents(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_CreateCircleShape(B2Recording rec, in B2RecArgs_CreateCircleShape a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreateCircleShape);
            b2RecWriteArgs_CreateCircleShape(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_CreateCapsuleShape(B2Recording rec, in B2RecArgs_CreateCapsuleShape a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreateCapsuleShape);
            b2RecWriteArgs_CreateCapsuleShape(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_CreateSegmentShape(B2Recording rec, in B2RecArgs_CreateSegmentShape a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreateSegmentShape);
            b2RecWriteArgs_CreateSegmentShape(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_CreatePolygonShape(B2Recording rec, in B2RecArgs_CreatePolygonShape a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreatePolygonShape);
            b2RecWriteArgs_CreatePolygonShape(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_CreateChainSegmentShape(B2Recording rec, in B2RecArgs_CreateChainSegmentShape a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreateChainSegmentShape);
            b2RecWriteArgs_CreateChainSegmentShape(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_DestroyShape(B2Recording rec, in B2RecArgs_DestroyShape a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.DestroyShape);
            b2RecWriteArgs_DestroyShape(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_ShapeSetDensity(B2Recording rec, in B2RecArgs_ShapeSetDensity a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.ShapeSetDensity);
            b2RecWriteArgs_ShapeSetDensity(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_ShapeSetFriction(B2Recording rec, in B2RecArgs_ShapeSetFriction a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.ShapeSetFriction);
            b2RecWriteArgs_ShapeSetFriction(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_ShapeSetRestitution(B2Recording rec, in B2RecArgs_ShapeSetRestitution a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.ShapeSetRestitution);
            b2RecWriteArgs_ShapeSetRestitution(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_ShapeSetUserMaterial(B2Recording rec, in B2RecArgs_ShapeSetUserMaterial a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.ShapeSetUserMaterial);
            b2RecWriteArgs_ShapeSetUserMaterial(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_ShapeSetSurfaceMaterial(B2Recording rec, in B2RecArgs_ShapeSetSurfaceMaterial a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.ShapeSetSurfaceMaterial);
            b2RecWriteArgs_ShapeSetSurfaceMaterial(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_ShapeSetFilter(B2Recording rec, in B2RecArgs_ShapeSetFilter a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.ShapeSetFilter);
            b2RecWriteArgs_ShapeSetFilter(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_ShapeEnableSensorEvents(B2Recording rec, in B2RecArgs_ShapeEnableSensorEvents a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.ShapeEnableSensorEvents);
            b2RecWriteArgs_ShapeEnableSensorEvents(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_ShapeEnableContactEvents(B2Recording rec, in B2RecArgs_ShapeEnableContactEvents a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.ShapeEnableContactEvents);
            b2RecWriteArgs_ShapeEnableContactEvents(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_ShapeEnablePreSolveEvents(B2Recording rec, in B2RecArgs_ShapeEnablePreSolveEvents a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.ShapeEnablePreSolveEvents);
            b2RecWriteArgs_ShapeEnablePreSolveEvents(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_ShapeEnableHitEvents(B2Recording rec, in B2RecArgs_ShapeEnableHitEvents a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.ShapeEnableHitEvents);
            b2RecWriteArgs_ShapeEnableHitEvents(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_ShapeSetCircle(B2Recording rec, in B2RecArgs_ShapeSetCircle a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.ShapeSetCircle);
            b2RecWriteArgs_ShapeSetCircle(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_ShapeSetCapsule(B2Recording rec, in B2RecArgs_ShapeSetCapsule a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.ShapeSetCapsule);
            b2RecWriteArgs_ShapeSetCapsule(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_ShapeSetSegment(B2Recording rec, in B2RecArgs_ShapeSetSegment a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.ShapeSetSegment);
            b2RecWriteArgs_ShapeSetSegment(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_ShapeSetPolygon(B2Recording rec, in B2RecArgs_ShapeSetPolygon a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.ShapeSetPolygon);
            b2RecWriteArgs_ShapeSetPolygon(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_ShapeSetChainSegment(B2Recording rec, in B2RecArgs_ShapeSetChainSegment a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.ShapeSetChainSegment);
            b2RecWriteArgs_ShapeSetChainSegment(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_ShapeApplyWind(B2Recording rec, in B2RecArgs_ShapeApplyWind a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.ShapeApplyWind);
            b2RecWriteArgs_ShapeApplyWind(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_CreateChain(B2Recording rec, in B2RecArgs_CreateChain a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreateChain);
            b2RecWriteArgs_CreateChain(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_DestroyChain(B2Recording rec, in B2RecArgs_DestroyChain a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.DestroyChain);
            b2RecWriteArgs_DestroyChain(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_ChainSetSurfaceMaterial(B2Recording rec, in B2RecArgs_ChainSetSurfaceMaterial a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.ChainSetSurfaceMaterial);
            b2RecWriteArgs_ChainSetSurfaceMaterial(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_CreateDistanceJoint(B2Recording rec, in B2RecArgs_CreateDistanceJoint a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreateDistanceJoint);
            b2RecWriteArgs_CreateDistanceJoint(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_CreateMotorJoint(B2Recording rec, in B2RecArgs_CreateMotorJoint a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreateMotorJoint);
            b2RecWriteArgs_CreateMotorJoint(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_CreateFilterJoint(B2Recording rec, in B2RecArgs_CreateFilterJoint a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreateFilterJoint);
            b2RecWriteArgs_CreateFilterJoint(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_CreatePrismaticJoint(B2Recording rec, in B2RecArgs_CreatePrismaticJoint a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreatePrismaticJoint);
            b2RecWriteArgs_CreatePrismaticJoint(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_CreateRevoluteJoint(B2Recording rec, in B2RecArgs_CreateRevoluteJoint a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreateRevoluteJoint);
            b2RecWriteArgs_CreateRevoluteJoint(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_CreateWeldJoint(B2Recording rec, in B2RecArgs_CreateWeldJoint a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreateWeldJoint);
            b2RecWriteArgs_CreateWeldJoint(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_CreateWheelJoint(B2Recording rec, in B2RecArgs_CreateWheelJoint a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreateWheelJoint);
            b2RecWriteArgs_CreateWheelJoint(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_DestroyJoint(B2Recording rec, in B2RecArgs_DestroyJoint a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.DestroyJoint);
            b2RecWriteArgs_DestroyJoint(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_JointSetLocalFrameA(B2Recording rec, in B2RecArgs_JointSetLocalFrameA a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.JointSetLocalFrameA);
            b2RecWriteArgs_JointSetLocalFrameA(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_JointSetLocalFrameB(B2Recording rec, in B2RecArgs_JointSetLocalFrameB a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.JointSetLocalFrameB);
            b2RecWriteArgs_JointSetLocalFrameB(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_JointSetCollideConnected(B2Recording rec, in B2RecArgs_JointSetCollideConnected a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.JointSetCollideConnected);
            b2RecWriteArgs_JointSetCollideConnected(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_JointWakeBodies(B2Recording rec, in B2RecArgs_JointWakeBodies a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.JointWakeBodies);
            b2RecWriteArgs_JointWakeBodies(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_JointSetConstraintTuning(B2Recording rec, in B2RecArgs_JointSetConstraintTuning a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.JointSetConstraintTuning);
            b2RecWriteArgs_JointSetConstraintTuning(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_JointSetForceThreshold(B2Recording rec, in B2RecArgs_JointSetForceThreshold a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.JointSetForceThreshold);
            b2RecWriteArgs_JointSetForceThreshold(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_JointSetTorqueThreshold(B2Recording rec, in B2RecArgs_JointSetTorqueThreshold a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.JointSetTorqueThreshold);
            b2RecWriteArgs_JointSetTorqueThreshold(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_DistanceJointSetLength(B2Recording rec, in B2RecArgs_DistanceJointSetLength a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.DistanceJointSetLength);
            b2RecWriteArgs_DistanceJointSetLength(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_DistanceJointEnableSpring(B2Recording rec, in B2RecArgs_DistanceJointEnableSpring a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.DistanceJointEnableSpring);
            b2RecWriteArgs_DistanceJointEnableSpring(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_DistanceJointSetSpringForceRange(B2Recording rec, in B2RecArgs_DistanceJointSetSpringForceRange a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.DistanceJointSetSpringForceRange);
            b2RecWriteArgs_DistanceJointSetSpringForceRange(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_DistanceJointSetSpringHertz(B2Recording rec, in B2RecArgs_DistanceJointSetSpringHertz a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.DistanceJointSetSpringHertz);
            b2RecWriteArgs_DistanceJointSetSpringHertz(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_DistanceJointSetSpringDampingRatio(B2Recording rec, in B2RecArgs_DistanceJointSetSpringDampingRatio a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.DistanceJointSetSpringDampingRatio);
            b2RecWriteArgs_DistanceJointSetSpringDampingRatio(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_DistanceJointEnableLimit(B2Recording rec, in B2RecArgs_DistanceJointEnableLimit a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.DistanceJointEnableLimit);
            b2RecWriteArgs_DistanceJointEnableLimit(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_DistanceJointSetLengthRange(B2Recording rec, in B2RecArgs_DistanceJointSetLengthRange a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.DistanceJointSetLengthRange);
            b2RecWriteArgs_DistanceJointSetLengthRange(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_DistanceJointEnableMotor(B2Recording rec, in B2RecArgs_DistanceJointEnableMotor a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.DistanceJointEnableMotor);
            b2RecWriteArgs_DistanceJointEnableMotor(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_DistanceJointSetMotorSpeed(B2Recording rec, in B2RecArgs_DistanceJointSetMotorSpeed a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.DistanceJointSetMotorSpeed);
            b2RecWriteArgs_DistanceJointSetMotorSpeed(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_DistanceJointSetMaxMotorForce(B2Recording rec, in B2RecArgs_DistanceJointSetMaxMotorForce a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.DistanceJointSetMaxMotorForce);
            b2RecWriteArgs_DistanceJointSetMaxMotorForce(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_MotorJointSetLinearVelocity(B2Recording rec, in B2RecArgs_MotorJointSetLinearVelocity a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.MotorJointSetLinearVelocity);
            b2RecWriteArgs_MotorJointSetLinearVelocity(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_MotorJointSetAngularVelocity(B2Recording rec, in B2RecArgs_MotorJointSetAngularVelocity a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.MotorJointSetAngularVelocity);
            b2RecWriteArgs_MotorJointSetAngularVelocity(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_MotorJointSetMaxVelocityForce(B2Recording rec, in B2RecArgs_MotorJointSetMaxVelocityForce a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.MotorJointSetMaxVelocityForce);
            b2RecWriteArgs_MotorJointSetMaxVelocityForce(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_MotorJointSetMaxVelocityTorque(B2Recording rec, in B2RecArgs_MotorJointSetMaxVelocityTorque a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.MotorJointSetMaxVelocityTorque);
            b2RecWriteArgs_MotorJointSetMaxVelocityTorque(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_MotorJointSetLinearHertz(B2Recording rec, in B2RecArgs_MotorJointSetLinearHertz a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.MotorJointSetLinearHertz);
            b2RecWriteArgs_MotorJointSetLinearHertz(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_MotorJointSetLinearDampingRatio(B2Recording rec, in B2RecArgs_MotorJointSetLinearDampingRatio a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.MotorJointSetLinearDampingRatio);
            b2RecWriteArgs_MotorJointSetLinearDampingRatio(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_MotorJointSetAngularHertz(B2Recording rec, in B2RecArgs_MotorJointSetAngularHertz a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.MotorJointSetAngularHertz);
            b2RecWriteArgs_MotorJointSetAngularHertz(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_MotorJointSetAngularDampingRatio(B2Recording rec, in B2RecArgs_MotorJointSetAngularDampingRatio a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.MotorJointSetAngularDampingRatio);
            b2RecWriteArgs_MotorJointSetAngularDampingRatio(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_MotorJointSetMaxSpringForce(B2Recording rec, in B2RecArgs_MotorJointSetMaxSpringForce a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.MotorJointSetMaxSpringForce);
            b2RecWriteArgs_MotorJointSetMaxSpringForce(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_MotorJointSetMaxSpringTorque(B2Recording rec, in B2RecArgs_MotorJointSetMaxSpringTorque a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.MotorJointSetMaxSpringTorque);
            b2RecWriteArgs_MotorJointSetMaxSpringTorque(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_PrismaticJointEnableSpring(B2Recording rec, in B2RecArgs_PrismaticJointEnableSpring a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.PrismaticJointEnableSpring);
            b2RecWriteArgs_PrismaticJointEnableSpring(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_PrismaticJointSetSpringHertz(B2Recording rec, in B2RecArgs_PrismaticJointSetSpringHertz a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.PrismaticJointSetSpringHertz);
            b2RecWriteArgs_PrismaticJointSetSpringHertz(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_PrismaticJointSetSpringDampingRatio(B2Recording rec, in B2RecArgs_PrismaticJointSetSpringDampingRatio a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.PrismaticJointSetSpringDampingRatio);
            b2RecWriteArgs_PrismaticJointSetSpringDampingRatio(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_PrismaticJointSetTargetTranslation(B2Recording rec, in B2RecArgs_PrismaticJointSetTargetTranslation a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.PrismaticJointSetTargetTranslation);
            b2RecWriteArgs_PrismaticJointSetTargetTranslation(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_PrismaticJointEnableLimit(B2Recording rec, in B2RecArgs_PrismaticJointEnableLimit a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.PrismaticJointEnableLimit);
            b2RecWriteArgs_PrismaticJointEnableLimit(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_PrismaticJointSetLimits(B2Recording rec, in B2RecArgs_PrismaticJointSetLimits a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.PrismaticJointSetLimits);
            b2RecWriteArgs_PrismaticJointSetLimits(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_PrismaticJointEnableMotor(B2Recording rec, in B2RecArgs_PrismaticJointEnableMotor a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.PrismaticJointEnableMotor);
            b2RecWriteArgs_PrismaticJointEnableMotor(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_PrismaticJointSetMotorSpeed(B2Recording rec, in B2RecArgs_PrismaticJointSetMotorSpeed a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.PrismaticJointSetMotorSpeed);
            b2RecWriteArgs_PrismaticJointSetMotorSpeed(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_PrismaticJointSetMaxMotorForce(B2Recording rec, in B2RecArgs_PrismaticJointSetMaxMotorForce a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.PrismaticJointSetMaxMotorForce);
            b2RecWriteArgs_PrismaticJointSetMaxMotorForce(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_RevoluteJointEnableSpring(B2Recording rec, in B2RecArgs_RevoluteJointEnableSpring a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.RevoluteJointEnableSpring);
            b2RecWriteArgs_RevoluteJointEnableSpring(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_RevoluteJointSetSpringHertz(B2Recording rec, in B2RecArgs_RevoluteJointSetSpringHertz a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.RevoluteJointSetSpringHertz);
            b2RecWriteArgs_RevoluteJointSetSpringHertz(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_RevoluteJointSetSpringDampingRatio(B2Recording rec, in B2RecArgs_RevoluteJointSetSpringDampingRatio a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.RevoluteJointSetSpringDampingRatio);
            b2RecWriteArgs_RevoluteJointSetSpringDampingRatio(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_RevoluteJointSetTargetAngle(B2Recording rec, in B2RecArgs_RevoluteJointSetTargetAngle a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.RevoluteJointSetTargetAngle);
            b2RecWriteArgs_RevoluteJointSetTargetAngle(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_RevoluteJointEnableLimit(B2Recording rec, in B2RecArgs_RevoluteJointEnableLimit a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.RevoluteJointEnableLimit);
            b2RecWriteArgs_RevoluteJointEnableLimit(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_RevoluteJointSetLimits(B2Recording rec, in B2RecArgs_RevoluteJointSetLimits a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.RevoluteJointSetLimits);
            b2RecWriteArgs_RevoluteJointSetLimits(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_RevoluteJointEnableMotor(B2Recording rec, in B2RecArgs_RevoluteJointEnableMotor a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.RevoluteJointEnableMotor);
            b2RecWriteArgs_RevoluteJointEnableMotor(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_RevoluteJointSetMotorSpeed(B2Recording rec, in B2RecArgs_RevoluteJointSetMotorSpeed a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.RevoluteJointSetMotorSpeed);
            b2RecWriteArgs_RevoluteJointSetMotorSpeed(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_RevoluteJointSetMaxMotorTorque(B2Recording rec, in B2RecArgs_RevoluteJointSetMaxMotorTorque a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.RevoluteJointSetMaxMotorTorque);
            b2RecWriteArgs_RevoluteJointSetMaxMotorTorque(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_WeldJointSetLinearHertz(B2Recording rec, in B2RecArgs_WeldJointSetLinearHertz a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.WeldJointSetLinearHertz);
            b2RecWriteArgs_WeldJointSetLinearHertz(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_WeldJointSetLinearDampingRatio(B2Recording rec, in B2RecArgs_WeldJointSetLinearDampingRatio a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.WeldJointSetLinearDampingRatio);
            b2RecWriteArgs_WeldJointSetLinearDampingRatio(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_WeldJointSetAngularHertz(B2Recording rec, in B2RecArgs_WeldJointSetAngularHertz a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.WeldJointSetAngularHertz);
            b2RecWriteArgs_WeldJointSetAngularHertz(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_WeldJointSetAngularDampingRatio(B2Recording rec, in B2RecArgs_WeldJointSetAngularDampingRatio a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.WeldJointSetAngularDampingRatio);
            b2RecWriteArgs_WeldJointSetAngularDampingRatio(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_WheelJointEnableSpring(B2Recording rec, in B2RecArgs_WheelJointEnableSpring a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.WheelJointEnableSpring);
            b2RecWriteArgs_WheelJointEnableSpring(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_WheelJointSetSpringHertz(B2Recording rec, in B2RecArgs_WheelJointSetSpringHertz a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.WheelJointSetSpringHertz);
            b2RecWriteArgs_WheelJointSetSpringHertz(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_WheelJointSetSpringDampingRatio(B2Recording rec, in B2RecArgs_WheelJointSetSpringDampingRatio a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.WheelJointSetSpringDampingRatio);
            b2RecWriteArgs_WheelJointSetSpringDampingRatio(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_WheelJointEnableLimit(B2Recording rec, in B2RecArgs_WheelJointEnableLimit a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.WheelJointEnableLimit);
            b2RecWriteArgs_WheelJointEnableLimit(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_WheelJointSetLimits(B2Recording rec, in B2RecArgs_WheelJointSetLimits a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.WheelJointSetLimits);
            b2RecWriteArgs_WheelJointSetLimits(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_WheelJointEnableMotor(B2Recording rec, in B2RecArgs_WheelJointEnableMotor a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.WheelJointEnableMotor);
            b2RecWriteArgs_WheelJointEnableMotor(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_WheelJointSetMotorSpeed(B2Recording rec, in B2RecArgs_WheelJointSetMotorSpeed a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.WheelJointSetMotorSpeed);
            b2RecWriteArgs_WheelJointSetMotorSpeed(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_WheelJointSetMaxMotorTorque(B2Recording rec, in B2RecArgs_WheelJointSetMaxMotorTorque a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.WheelJointSetMaxMotorTorque);
            b2RecWriteArgs_WheelJointSetMaxMotorTorque(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_QueryOverlapAABB(B2Recording rec, in B2RecArgs_QueryOverlapAABB a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.QueryOverlapAABB);
            b2RecWriteArgs_QueryOverlapAABB(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_QueryOverlapShape(B2Recording rec, in B2RecArgs_QueryOverlapShape a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.QueryOverlapShape);
            b2RecWriteArgs_QueryOverlapShape(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_QueryCastRay(B2Recording rec, in B2RecArgs_QueryCastRay a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.QueryCastRay);
            b2RecWriteArgs_QueryCastRay(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_QueryCastShape(B2Recording rec, in B2RecArgs_QueryCastShape a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.QueryCastShape);
            b2RecWriteArgs_QueryCastShape(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_QueryCollideMover(B2Recording rec, in B2RecArgs_QueryCollideMover a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.QueryCollideMover);
            b2RecWriteArgs_QueryCollideMover(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_QueryCastRayClosest(B2Recording rec, in B2RecArgs_QueryCastRayClosest a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.QueryCastRayClosest);
            b2RecWriteArgs_QueryCastRayClosest(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_QueryCastMover(B2Recording rec, in B2RecArgs_QueryCastMover a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.QueryCastMover);
            b2RecWriteArgs_QueryCastMover(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_ShapeTestPoint(B2Recording rec, in B2RecArgs_ShapeTestPoint a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.ShapeTestPoint);
            b2RecWriteArgs_ShapeTestPoint(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_ShapeRayCast(B2Recording rec, in B2RecArgs_ShapeRayCast a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.ShapeRayCast);
            b2RecWriteArgs_ShapeRayCast(rec, in a);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWrite_StateHash(B2Recording rec, in B2RecArgs_StateHash a)
        {
            b2RecBeginRecord(rec, B2RecOpcode.StateHash);
            b2RecWriteArgs_StateHash(rec, in a);
            b2RecEndRecord(rec);
        }

        // Create ops also need a writer that appends the returned id inside the same record.
        // Generated only for ops carrying a RET tag; the id type comes from that tag.
        // Codegen: create-op writers that append the returned id inside the record. The RET tag
        // selects the id type and its write primitive; RET_NONE ops generate nothing.

        internal static void b2RecWriteRet_CreateBody(B2Recording rec, in B2RecArgs_CreateBody a, B2BodyId id)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreateBody);
            b2RecWriteArgs_CreateBody(rec, in a);
            b2RecW_BODYID(ref rec.buffer, id);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWriteRet_CreateCircleShape(B2Recording rec, in B2RecArgs_CreateCircleShape a, B2ShapeId id)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreateCircleShape);
            b2RecWriteArgs_CreateCircleShape(rec, in a);
            b2RecW_SHAPEID(ref rec.buffer, id);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWriteRet_CreateCapsuleShape(B2Recording rec, in B2RecArgs_CreateCapsuleShape a, B2ShapeId id)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreateCapsuleShape);
            b2RecWriteArgs_CreateCapsuleShape(rec, in a);
            b2RecW_SHAPEID(ref rec.buffer, id);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWriteRet_CreateSegmentShape(B2Recording rec, in B2RecArgs_CreateSegmentShape a, B2ShapeId id)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreateSegmentShape);
            b2RecWriteArgs_CreateSegmentShape(rec, in a);
            b2RecW_SHAPEID(ref rec.buffer, id);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWriteRet_CreatePolygonShape(B2Recording rec, in B2RecArgs_CreatePolygonShape a, B2ShapeId id)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreatePolygonShape);
            b2RecWriteArgs_CreatePolygonShape(rec, in a);
            b2RecW_SHAPEID(ref rec.buffer, id);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWriteRet_CreateChainSegmentShape(B2Recording rec, in B2RecArgs_CreateChainSegmentShape a, B2ShapeId id)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreateChainSegmentShape);
            b2RecWriteArgs_CreateChainSegmentShape(rec, in a);
            b2RecW_SHAPEID(ref rec.buffer, id);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWriteRet_CreateChain(B2Recording rec, in B2RecArgs_CreateChain a, B2ChainId id)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreateChain);
            b2RecWriteArgs_CreateChain(rec, in a);
            b2RecW_CHAINID(ref rec.buffer, id);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWriteRet_CreateDistanceJoint(B2Recording rec, in B2RecArgs_CreateDistanceJoint a, B2JointId id)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreateDistanceJoint);
            b2RecWriteArgs_CreateDistanceJoint(rec, in a);
            b2RecW_JOINTID(ref rec.buffer, id);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWriteRet_CreateMotorJoint(B2Recording rec, in B2RecArgs_CreateMotorJoint a, B2JointId id)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreateMotorJoint);
            b2RecWriteArgs_CreateMotorJoint(rec, in a);
            b2RecW_JOINTID(ref rec.buffer, id);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWriteRet_CreateFilterJoint(B2Recording rec, in B2RecArgs_CreateFilterJoint a, B2JointId id)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreateFilterJoint);
            b2RecWriteArgs_CreateFilterJoint(rec, in a);
            b2RecW_JOINTID(ref rec.buffer, id);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWriteRet_CreatePrismaticJoint(B2Recording rec, in B2RecArgs_CreatePrismaticJoint a, B2JointId id)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreatePrismaticJoint);
            b2RecWriteArgs_CreatePrismaticJoint(rec, in a);
            b2RecW_JOINTID(ref rec.buffer, id);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWriteRet_CreateRevoluteJoint(B2Recording rec, in B2RecArgs_CreateRevoluteJoint a, B2JointId id)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreateRevoluteJoint);
            b2RecWriteArgs_CreateRevoluteJoint(rec, in a);
            b2RecW_JOINTID(ref rec.buffer, id);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWriteRet_CreateWeldJoint(B2Recording rec, in B2RecArgs_CreateWeldJoint a, B2JointId id)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreateWeldJoint);
            b2RecWriteArgs_CreateWeldJoint(rec, in a);
            b2RecW_JOINTID(ref rec.buffer, id);
            b2RecEndRecord(rec);
        }

        internal static void b2RecWriteRet_CreateWheelJoint(B2Recording rec, in B2RecArgs_CreateWheelJoint a, B2JointId id)
        {
            b2RecBeginRecord(rec, B2RecOpcode.CreateWheelJoint);
            b2RecWriteArgs_CreateWheelJoint(rec, in a);
            b2RecW_JOINTID(ref rec.buffer, id);
            b2RecEndRecord(rec);
        }
    }
}
