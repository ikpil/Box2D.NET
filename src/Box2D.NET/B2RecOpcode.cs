// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // X-macro manifest for the recording system.
    // Include with B2_REC_OP and ARG defined; no commas between ARG tokens.
    //
    // B2_REC_OP( opcode, Name, RET, ARGS )
    //   RET in { RET_NONE, RET_BODYID, RET_SHAPEID }
    //   ARGS = zero or more ARG( TAG, fieldName ) tokens, NO commas between them
    //
    // The C# port keeps the generated result explicit: this opcode enum, one B2RecArgs_<Name>
    // struct per operation, and the corresponding typed writer and replay functions.
    //
    // Opcode ranges:
    //   0x00-0x0F  world lifecycle and config
    //   0x10-0x1F  body create/destroy
    //   0x20-0x3F  body mutators
    //   0x40-0x4F  shape create/destroy
    //   0x50-0x6F  shape mutators
    //   0x70-0x7F  chain
    //   0x80       step
    //   0x90-0xDF  joints (create, generic, per-type)
    //   0xE0-0xEF  spatial queries
    //   0xF0-0xFF  markers
    internal enum B2RecOpcode : byte
    {
        CreateWorld = 0x00,
        DestroyWorld = 0x01,
        Step = 0x80,

        // World config. The world arg is informational; replay always targets its own world.
        WorldEnableSleeping = 0x02,
        WorldEnableContinuous = 0x03,
        WorldSetRestitutionThreshold = 0x04,
        WorldSetHitEventThreshold = 0x05,
        WorldSetGravity = 0x06,
        WorldExplode = 0x07,
        WorldSetContactTuning = 0x08,
        WorldSetContactRecycleDistance = 0x09,
        WorldSetMaximumLinearSpeed = 0x0A,
        WorldEnableWarmStarting = 0x0B,
        WorldRebuildStaticTree = 0x0C,
        WorldEnableSpeculative = 0x0D,

        // Body
        CreateBody = 0x10,
        DestroyBody = 0x11,
        BodySetTransform = 0x20,
        BodySetLinearVelocity = 0x21,
        BodySetType = 0x22,
        BodySetName = 0x23,
        BodySetAngularVelocity = 0x24,
        BodySetTargetTransform = 0x25,
        BodyApplyForce = 0x26,
        BodyApplyForceToCenter = 0x27,
        BodyApplyTorque = 0x28,
        BodyClearForces = 0x29,
        BodyApplyLinearImpulse = 0x2A,
        BodyApplyLinearImpulseToCenter = 0x2B,
        BodyApplyAngularImpulse = 0x2C,
        BodySetMassData = 0x2D,
        BodyApplyMassFromShapes = 0x2E,
        BodySetLinearDamping = 0x2F,
        BodySetAngularDamping = 0x30,
        BodySetGravityScale = 0x31,
        BodySetAwake = 0x32,
        BodyWakeTouching = 0x33,
        BodyEnableSleep = 0x34,
        BodySetSleepThreshold = 0x35,
        BodyDisable = 0x36,
        BodyEnable = 0x37,
        BodySetMotionLocks = 0x38,
        BodySetBullet = 0x39,
        BodyEnableContactRecycling = 0x3A,
        BodyEnableContactEvents = 0x3B,
        BodyEnableHitEvents = 0x3C,

        // Shape create/destroy
        CreateCircleShape = 0x40,
        CreateCapsuleShape = 0x41,
        CreateSegmentShape = 0x42,
        CreatePolygonShape = 0x43,
        CreateChainSegmentShape = 0x44,
        DestroyShape = 0x45,
        // Shape mutators
        ShapeSetDensity = 0x50,
        ShapeSetFriction = 0x51,
        ShapeSetRestitution = 0x52,
        ShapeSetUserMaterial = 0x53,
        ShapeSetSurfaceMaterial = 0x54,
        ShapeSetFilter = 0x55,
        ShapeEnableSensorEvents = 0x56,
        ShapeEnableContactEvents = 0x57,
        ShapeEnablePreSolveEvents = 0x58,
        ShapeEnableHitEvents = 0x59,
        ShapeSetCircle = 0x5A,
        ShapeSetCapsule = 0x5B,
        ShapeSetSegment = 0x5C,
        ShapeSetPolygon = 0x5D,
        ShapeSetChainSegment = 0x5E,
        ShapeApplyWind = 0x5F,

        // Chain
        CreateChain = 0x70,
        DestroyChain = 0x71,
        ChainSetSurfaceMaterial = 0x72,

        // Joint create and destroy
        CreateDistanceJoint = 0x90,
        CreateMotorJoint = 0x91,
        CreateFilterJoint = 0x92,
        CreatePrismaticJoint = 0x93,
        CreateRevoluteJoint = 0x94,
        CreateWeldJoint = 0x95,
        CreateWheelJoint = 0x96,
        DestroyJoint = 0x97,
        // Generic joint mutators
        JointSetLocalFrameA = 0x98,
        JointSetLocalFrameB = 0x99,
        JointSetCollideConnected = 0x9A,
        JointWakeBodies = 0x9B,
        JointSetConstraintTuning = 0x9C,
        JointSetForceThreshold = 0x9D,
        JointSetTorqueThreshold = 0x9E,

        // Distance joint
        DistanceJointSetLength = 0xA0,
        DistanceJointEnableSpring = 0xA1,
        DistanceJointSetSpringForceRange = 0xA2,
        DistanceJointSetSpringHertz = 0xA3,
        DistanceJointSetSpringDampingRatio = 0xA4,
        DistanceJointEnableLimit = 0xA5,
        DistanceJointSetLengthRange = 0xA6,
        DistanceJointEnableMotor = 0xA7,
        DistanceJointSetMotorSpeed = 0xA8,
        DistanceJointSetMaxMotorForce = 0xA9,

        // Motor joint
        MotorJointSetLinearVelocity = 0xAA,
        MotorJointSetAngularVelocity = 0xAB,
        MotorJointSetMaxVelocityForce = 0xAC,
        MotorJointSetMaxVelocityTorque = 0xAD,
        MotorJointSetLinearHertz = 0xAE,
        MotorJointSetLinearDampingRatio = 0xAF,
        MotorJointSetAngularHertz = 0xB0,
        MotorJointSetAngularDampingRatio = 0xB1,
        MotorJointSetMaxSpringForce = 0xB2,
        MotorJointSetMaxSpringTorque = 0xB3,

        // Prismatic joint
        PrismaticJointEnableSpring = 0xB4,
        PrismaticJointSetSpringHertz = 0xB5,
        PrismaticJointSetSpringDampingRatio = 0xB6,
        PrismaticJointSetTargetTranslation = 0xB7,
        PrismaticJointEnableLimit = 0xB8,
        PrismaticJointSetLimits = 0xB9,
        PrismaticJointEnableMotor = 0xBA,
        PrismaticJointSetMotorSpeed = 0xBB,
        PrismaticJointSetMaxMotorForce = 0xBC,

        // Revolute joint
        RevoluteJointEnableSpring = 0xBD,
        RevoluteJointSetSpringHertz = 0xBE,
        RevoluteJointSetSpringDampingRatio = 0xBF,
        RevoluteJointSetTargetAngle = 0xC0,
        RevoluteJointEnableLimit = 0xC1,
        RevoluteJointSetLimits = 0xC2,
        RevoluteJointEnableMotor = 0xC3,
        RevoluteJointSetMotorSpeed = 0xC4,
        RevoluteJointSetMaxMotorTorque = 0xC5,

        // Weld joint
        WeldJointSetLinearHertz = 0xC6,
        WeldJointSetLinearDampingRatio = 0xC7,
        WeldJointSetAngularHertz = 0xC8,
        WeldJointSetAngularDampingRatio = 0xC9,

        // Wheel joint
        WheelJointEnableSpring = 0xCA,
        WheelJointSetSpringHertz = 0xCB,
        WheelJointSetSpringDampingRatio = 0xCC,
        WheelJointEnableLimit = 0xCD,
        WheelJointSetLimits = 0xCE,
        WheelJointEnableMotor = 0xCF,
        WheelJointSetMotorSpeed = 0xD0,
        WheelJointSetMaxMotorTorque = 0xD1,

        // Spatial queries. Inputs through the manifest (reader side). Hit tail / results hand-written.
        QueryOverlapAABB = 0xE0,
        QueryOverlapShape = 0xE1,
        QueryCastRay = 0xE2,
        QueryCastShape = 0xE3,
        QueryCollideMover = 0xE4,
        QueryCastRayClosest = 0xE5,
        QueryCastMover = 0xE6,
        ShapeTestPoint = 0xE7,
        ShapeRayCast = 0xE8,
        StateHash = 0xF1,
    }
}
