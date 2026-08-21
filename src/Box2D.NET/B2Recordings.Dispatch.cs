// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Cores;
using static Box2D.NET.B2DistanceJoints;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2MotorJoints;
using static Box2D.NET.B2PrismaticJoints;
using static Box2D.NET.B2RevoluteJoints;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2WeldJoints;
using static Box2D.NET.B2WheelJoints;
using static Box2D.NET.B2Worlds;

namespace Box2D.NET
{
    public static partial class B2Recordings
    {
        // Per-op dispatch. These names mirror recording_replay.c so an upstream function
        // search lands on the corresponding managed replay path.
        private static void b2RecDispatch_CreateWorld(in B2RecArgs_CreateWorld a, B2RecReader rdr)
        {
            B2WorldDef def = a.def;
            if (rdr.owner != null)
            {
                // Keep the recorded count even when the replay overrides the effective one
                rdr.owner.recordedWorkerCount = def.workerCount;
            }
            // Never re-record during replay. Task pointers stay NULL so the internal scheduler is used
            def.recordingPath = null;
            def.enqueueTask = null;
            def.finishTask = null;
            def.userTaskContext = null;
            if (rdr.workerCount > 0)
            {
                def.workerCount = rdr.workerCount;
            }
            rdr.replayWorldId = b2CreateWorld(def);
            if (!b2World_IsValid(rdr.replayWorldId))
            {
                Console.WriteLine("b2ReplayFile: b2CreateWorld failed during replay");
                rdr.ok = false;
            }
        }

        private static void b2RecDispatch_DestroyWorld(in B2RecArgs_DestroyWorld a, B2RecReader rdr)
        {
            // The recorded session ended here. The player owns the replay world's lifetime and tears it
            // down in b2RecPlayer_Destroy/Restart, so a viewer can keep drawing the final step. There is
            // one world per recording and this is always the last record, so leaving it alive is safe.
        }

        private static void b2RecDispatch_Step(in B2RecArgs_Step a, B2RecReader rdr)
        {
            b2World_Step(rdr.replayWorldId, a.dt, a.subStepCount);
        }

        private static void b2RecDispatch_WorldEnableSleeping(in B2RecArgs_WorldEnableSleeping a, B2RecReader rdr)
        {
            b2World_EnableSleeping(rdr.replayWorldId, a.flag);
        }

        private static void b2RecDispatch_WorldEnableContinuous(in B2RecArgs_WorldEnableContinuous a, B2RecReader rdr)
        {
            b2World_EnableContinuous(rdr.replayWorldId, a.flag);
        }

        private static void b2RecDispatch_WorldSetRestitutionThreshold(in B2RecArgs_WorldSetRestitutionThreshold a, B2RecReader rdr)
        {
            b2World_SetRestitutionThreshold(rdr.replayWorldId, a.value);
        }

        private static void b2RecDispatch_WorldSetHitEventThreshold(in B2RecArgs_WorldSetHitEventThreshold a, B2RecReader rdr)
        {
            b2World_SetHitEventThreshold(rdr.replayWorldId, a.value);
        }

        private static void b2RecDispatch_WorldSetGravity(in B2RecArgs_WorldSetGravity a, B2RecReader rdr)
        {
            b2World_SetGravity(rdr.replayWorldId, a.gravity);
        }

        private static void b2RecDispatch_WorldExplode(in B2RecArgs_WorldExplode a, B2RecReader rdr)
        {
            b2World_Explode(rdr.replayWorldId, a.def);
        }

        private static void b2RecDispatch_WorldSetContactTuning(in B2RecArgs_WorldSetContactTuning a, B2RecReader rdr)
        {
            b2World_SetContactTuning(rdr.replayWorldId, a.hertz, a.dampingRatio, a.pushSpeed);
        }

        private static void b2RecDispatch_WorldSetContactRecycleDistance(in B2RecArgs_WorldSetContactRecycleDistance a, B2RecReader rdr)
        {
            b2World_SetContactRecycleDistance(rdr.replayWorldId, a.recycleDistance);
        }

        private static void b2RecDispatch_WorldSetMaximumLinearSpeed(in B2RecArgs_WorldSetMaximumLinearSpeed a, B2RecReader rdr)
        {
            b2World_SetMaximumLinearSpeed(rdr.replayWorldId, a.maximumLinearSpeed);
        }

        private static void b2RecDispatch_WorldEnableWarmStarting(in B2RecArgs_WorldEnableWarmStarting a, B2RecReader rdr)
        {
            b2World_EnableWarmStarting(rdr.replayWorldId, a.flag);
        }

        private static void b2RecDispatch_WorldRebuildStaticTree(in B2RecArgs_WorldRebuildStaticTree a, B2RecReader rdr)
        {
            b2World_RebuildStaticTree(rdr.replayWorldId);
        }

        private static void b2RecDispatch_WorldEnableSpeculative(in B2RecArgs_WorldEnableSpeculative a, B2RecReader rdr)
        {
            b2World_EnableSpeculative(rdr.replayWorldId, a.flag);
        }

        private static void b2RecDispatch_CreateBody(in B2RecArgs_CreateBody a, B2RecReader rdr)
        {
            // Recorded id is appended after args (written before b2RecEndRecord)
            B2BodyId recId = b2RecR_BODYID(rdr);
            B2BodyId gotId = b2CreateBody(rdr.replayWorldId, a.def);
            b2RecCheckBodyId(rdr, gotId, recId);
            if (rdr.owner != null)
            {
                b2RecTrackBodyCreate(rdr.owner, gotId);
            }
        }

        private static void b2RecDispatch_DestroyBody(in B2RecArgs_DestroyBody a, B2RecReader rdr)
        {
            B2BodyId id = b2RecMakeBodyId(rdr, a.body);
            if (rdr.owner != null)
            {
                b2RecTrackBodyDestroy(rdr.owner, id);
            }
            b2DestroyBody(id);
        }

        private static void b2RecDispatch_BodySetTransform(in B2RecArgs_BodySetTransform a, B2RecReader rdr)
        {
            b2Body_SetTransform(b2RecMakeBodyId(rdr, a.body), a.position, a.rotation);
        }

        private static void b2RecDispatch_BodySetLinearVelocity(in B2RecArgs_BodySetLinearVelocity a, B2RecReader rdr)
        {
            b2Body_SetLinearVelocity(b2RecMakeBodyId(rdr, a.body), a.v);
        }

        private static void b2RecDispatch_BodySetType(in B2RecArgs_BodySetType a, B2RecReader rdr)
        {
            b2Body_SetType(b2RecMakeBodyId(rdr, a.body), (B2BodyType)a.type);
        }

        private static void b2RecDispatch_BodySetName(in B2RecArgs_BodySetName a, B2RecReader rdr)
        {
            b2Body_SetName(b2RecMakeBodyId(rdr, a.body), a.name);
        }

        private static void b2RecDispatch_BodySetAngularVelocity(in B2RecArgs_BodySetAngularVelocity a, B2RecReader rdr)
        {
            b2Body_SetAngularVelocity(b2RecMakeBodyId(rdr, a.body), a.w);
        }

        private static void b2RecDispatch_BodySetTargetTransform(in B2RecArgs_BodySetTargetTransform a, B2RecReader rdr)
        {
            b2Body_SetTargetTransform(b2RecMakeBodyId(rdr, a.body), a.target, a.timeStep, a.wake);
        }

        private static void b2RecDispatch_BodyApplyForce(in B2RecArgs_BodyApplyForce a, B2RecReader rdr)
        {
            b2Body_ApplyForce(b2RecMakeBodyId(rdr, a.body), a.force, a.point, a.wake);
        }

        private static void b2RecDispatch_BodyApplyForceToCenter(in B2RecArgs_BodyApplyForceToCenter a, B2RecReader rdr)
        {
            b2Body_ApplyForceToCenter(b2RecMakeBodyId(rdr, a.body), a.force, a.wake);
        }

        private static void b2RecDispatch_BodyApplyTorque(in B2RecArgs_BodyApplyTorque a, B2RecReader rdr)
        {
            b2Body_ApplyTorque(b2RecMakeBodyId(rdr, a.body), a.torque, a.wake);
        }

        private static void b2RecDispatch_BodyClearForces(in B2RecArgs_BodyClearForces a, B2RecReader rdr)
        {
            b2Body_ClearForces(b2RecMakeBodyId(rdr, a.body));
        }

        private static void b2RecDispatch_BodyApplyLinearImpulse(in B2RecArgs_BodyApplyLinearImpulse a, B2RecReader rdr)
        {
            b2Body_ApplyLinearImpulse(b2RecMakeBodyId(rdr, a.body), a.impulse, a.point, a.wake);
        }

        private static void b2RecDispatch_BodyApplyLinearImpulseToCenter(in B2RecArgs_BodyApplyLinearImpulseToCenter a, B2RecReader rdr)
        {
            b2Body_ApplyLinearImpulseToCenter(b2RecMakeBodyId(rdr, a.body), a.impulse, a.wake);
        }

        private static void b2RecDispatch_BodyApplyAngularImpulse(in B2RecArgs_BodyApplyAngularImpulse a, B2RecReader rdr)
        {
            b2Body_ApplyAngularImpulse(b2RecMakeBodyId(rdr, a.body), a.impulse, a.wake);
        }

        private static void b2RecDispatch_BodySetMassData(in B2RecArgs_BodySetMassData a, B2RecReader rdr)
        {
            b2Body_SetMassData(b2RecMakeBodyId(rdr, a.body), a.massData);
        }

        private static void b2RecDispatch_BodyApplyMassFromShapes(in B2RecArgs_BodyApplyMassFromShapes a, B2RecReader rdr)
        {
            b2Body_ApplyMassFromShapes(b2RecMakeBodyId(rdr, a.body));
        }

        private static void b2RecDispatch_BodySetLinearDamping(in B2RecArgs_BodySetLinearDamping a, B2RecReader rdr)
        {
            b2Body_SetLinearDamping(b2RecMakeBodyId(rdr, a.body), a.damping);
        }

        private static void b2RecDispatch_BodySetAngularDamping(in B2RecArgs_BodySetAngularDamping a, B2RecReader rdr)
        {
            b2Body_SetAngularDamping(b2RecMakeBodyId(rdr, a.body), a.damping);
        }

        private static void b2RecDispatch_BodySetGravityScale(in B2RecArgs_BodySetGravityScale a, B2RecReader rdr)
        {
            b2Body_SetGravityScale(b2RecMakeBodyId(rdr, a.body), a.scale);
        }

        private static void b2RecDispatch_BodySetAwake(in B2RecArgs_BodySetAwake a, B2RecReader rdr)
        {
            b2Body_SetAwake(b2RecMakeBodyId(rdr, a.body), a.awake);
        }

        private static void b2RecDispatch_BodyWakeTouching(in B2RecArgs_BodyWakeTouching a, B2RecReader rdr)
        {
            b2Body_WakeTouching(b2RecMakeBodyId(rdr, a.body));
        }

        private static void b2RecDispatch_BodyEnableSleep(in B2RecArgs_BodyEnableSleep a, B2RecReader rdr)
        {
            b2Body_EnableSleep(b2RecMakeBodyId(rdr, a.body), a.flag);
        }

        private static void b2RecDispatch_BodySetSleepThreshold(in B2RecArgs_BodySetSleepThreshold a, B2RecReader rdr)
        {
            b2Body_SetSleepThreshold(b2RecMakeBodyId(rdr, a.body), a.threshold);
        }

        private static void b2RecDispatch_BodyDisable(in B2RecArgs_BodyDisable a, B2RecReader rdr)
        {
            b2Body_Disable(b2RecMakeBodyId(rdr, a.body));
        }

        private static void b2RecDispatch_BodyEnable(in B2RecArgs_BodyEnable a, B2RecReader rdr)
        {
            b2Body_Enable(b2RecMakeBodyId(rdr, a.body));
        }

        private static void b2RecDispatch_BodySetMotionLocks(in B2RecArgs_BodySetMotionLocks a, B2RecReader rdr)
        {
            b2Body_SetMotionLocks(b2RecMakeBodyId(rdr, a.body), a.locks);
        }

        private static void b2RecDispatch_BodySetBullet(in B2RecArgs_BodySetBullet a, B2RecReader rdr)
        {
            b2Body_SetBullet(b2RecMakeBodyId(rdr, a.body), a.flag);
        }

        private static void b2RecDispatch_BodyEnableContactRecycling(in B2RecArgs_BodyEnableContactRecycling a, B2RecReader rdr)
        {
            b2Body_EnableContactRecycling(b2RecMakeBodyId(rdr, a.body), a.flag);
        }

        private static void b2RecDispatch_BodyEnableContactEvents(in B2RecArgs_BodyEnableContactEvents a, B2RecReader rdr)
        {
            b2Body_EnableContactEvents(b2RecMakeBodyId(rdr, a.body), a.flag);
        }

        private static void b2RecDispatch_BodyEnableHitEvents(in B2RecArgs_BodyEnableHitEvents a, B2RecReader rdr)
        {
            b2Body_EnableHitEvents(b2RecMakeBodyId(rdr, a.body), a.flag);
        }

        private static void b2RecDispatch_CreateCircleShape(in B2RecArgs_CreateCircleShape a, B2RecReader rdr)
        {
            B2ShapeId recId = b2RecR_SHAPEID(rdr);
            B2BodyId bodyId = b2RecMakeBodyId(rdr, a.body);
            B2ShapeId gotId = b2CreateCircleShape(bodyId, a.def, a.circle);
            b2RecCheckShapeId(rdr, gotId, recId);
        }

        private static void b2RecDispatch_CreateCapsuleShape(in B2RecArgs_CreateCapsuleShape a, B2RecReader rdr)
        {
            B2ShapeId recId = b2RecR_SHAPEID(rdr);
            B2BodyId bodyId = b2RecMakeBodyId(rdr, a.body);
            B2ShapeId gotId = b2CreateCapsuleShape(bodyId, a.def, a.capsule);
            b2RecCheckShapeId(rdr, gotId, recId);
        }

        private static void b2RecDispatch_CreateSegmentShape(in B2RecArgs_CreateSegmentShape a, B2RecReader rdr)
        {
            B2ShapeId recId = b2RecR_SHAPEID(rdr);
            B2BodyId bodyId = b2RecMakeBodyId(rdr, a.body);
            B2ShapeId gotId = b2CreateSegmentShape(bodyId, a.def, a.segment);
            b2RecCheckShapeId(rdr, gotId, recId);
        }

        private static void b2RecDispatch_CreatePolygonShape(in B2RecArgs_CreatePolygonShape a, B2RecReader rdr)
        {
            B2ShapeId recId = b2RecR_SHAPEID(rdr);
            B2BodyId bodyId = b2RecMakeBodyId(rdr, a.body);
            B2ShapeId gotId = b2CreatePolygonShape(bodyId, a.def, a.polygon);
            b2RecCheckShapeId(rdr, gotId, recId);
        }

        private static void b2RecDispatch_CreateChainSegmentShape(in B2RecArgs_CreateChainSegmentShape a, B2RecReader rdr)
        {
            B2ShapeId recId = b2RecR_SHAPEID(rdr);
            B2BodyId bodyId = b2RecMakeBodyId(rdr, a.body);
            B2ShapeId gotId = b2CreateChainSegmentShape(bodyId, a.def, a.chainSegment);
            b2RecCheckShapeId(rdr, gotId, recId);
        }

        private static void b2RecDispatch_DestroyShape(in B2RecArgs_DestroyShape a, B2RecReader rdr)
        {
            b2DestroyShape(b2RecMakeShapeId(rdr, a.shape), a.updateBodyMass);
        }

        private static void b2RecDispatch_ShapeSetDensity(in B2RecArgs_ShapeSetDensity a, B2RecReader rdr)
        {
            b2Shape_SetDensity(b2RecMakeShapeId(rdr, a.shape), a.density, a.updateBodyMass);
        }

        private static void b2RecDispatch_ShapeSetFriction(in B2RecArgs_ShapeSetFriction a, B2RecReader rdr)
        {
            b2Shape_SetFriction(b2RecMakeShapeId(rdr, a.shape), a.friction);
        }

        private static void b2RecDispatch_ShapeSetRestitution(in B2RecArgs_ShapeSetRestitution a, B2RecReader rdr)
        {
            b2Shape_SetRestitution(b2RecMakeShapeId(rdr, a.shape), a.restitution);
        }

        private static void b2RecDispatch_ShapeSetUserMaterial(in B2RecArgs_ShapeSetUserMaterial a, B2RecReader rdr)
        {
            b2Shape_SetUserMaterial(b2RecMakeShapeId(rdr, a.shape), a.material);
        }

        private static void b2RecDispatch_ShapeSetSurfaceMaterial(in B2RecArgs_ShapeSetSurfaceMaterial a, B2RecReader rdr)
        {
            b2Shape_SetSurfaceMaterial(b2RecMakeShapeId(rdr, a.shape), a.material);
        }

        private static void b2RecDispatch_ShapeSetFilter(in B2RecArgs_ShapeSetFilter a, B2RecReader rdr)
        {
            b2Shape_SetFilter(b2RecMakeShapeId(rdr, a.shape), a.filter);
        }

        private static void b2RecDispatch_ShapeEnableSensorEvents(in B2RecArgs_ShapeEnableSensorEvents a, B2RecReader rdr)
        {
            b2Shape_EnableSensorEvents(b2RecMakeShapeId(rdr, a.shape), a.flag);
        }

        private static void b2RecDispatch_ShapeEnableContactEvents(in B2RecArgs_ShapeEnableContactEvents a, B2RecReader rdr)
        {
            b2Shape_EnableContactEvents(b2RecMakeShapeId(rdr, a.shape), a.flag);
        }

        private static void b2RecDispatch_ShapeEnablePreSolveEvents(in B2RecArgs_ShapeEnablePreSolveEvents a, B2RecReader rdr)
        {
            b2Shape_EnablePreSolveEvents(b2RecMakeShapeId(rdr, a.shape), a.flag);
        }

        private static void b2RecDispatch_ShapeEnableHitEvents(in B2RecArgs_ShapeEnableHitEvents a, B2RecReader rdr)
        {
            b2Shape_EnableHitEvents(b2RecMakeShapeId(rdr, a.shape), a.flag);
        }

        private static void b2RecDispatch_ShapeSetCircle(in B2RecArgs_ShapeSetCircle a, B2RecReader rdr)
        {
            b2Shape_SetCircle(b2RecMakeShapeId(rdr, a.shape), a.circle);
        }

        private static void b2RecDispatch_ShapeSetCapsule(in B2RecArgs_ShapeSetCapsule a, B2RecReader rdr)
        {
            b2Shape_SetCapsule(b2RecMakeShapeId(rdr, a.shape), a.capsule);
        }

        private static void b2RecDispatch_ShapeSetSegment(in B2RecArgs_ShapeSetSegment a, B2RecReader rdr)
        {
            b2Shape_SetSegment(b2RecMakeShapeId(rdr, a.shape), a.segment);
        }

        private static void b2RecDispatch_ShapeSetPolygon(in B2RecArgs_ShapeSetPolygon a, B2RecReader rdr)
        {
            B2Polygon polygon = a.polygon;
            b2Shape_SetPolygon(b2RecMakeShapeId(rdr, a.shape), ref polygon);
        }

        private static void b2RecDispatch_ShapeSetChainSegment(in B2RecArgs_ShapeSetChainSegment a, B2RecReader rdr)
        {
            b2Shape_SetChainSegment(b2RecMakeShapeId(rdr, a.shape), a.chainSegment);
        }

        private static void b2RecDispatch_ShapeApplyWind(in B2RecArgs_ShapeApplyWind a, B2RecReader rdr)
        {
            b2Shape_ApplyWind(b2RecMakeShapeId(rdr, a.shape), a.wind, a.drag, a.lift, a.wake);
        }

        private static void b2RecDispatch_CreateChain(in B2RecArgs_CreateChain a, B2RecReader rdr)
        {
            B2ChainId recId = b2RecR_CHAINID(rdr);
            if (!rdr.ok)
            {
                // A corrupt point/material count left the def degenerate, do not build a chain from it
                return;
            }
            B2BodyId bodyId = b2RecMakeBodyId(rdr, a.body);
            B2ChainId gotId = b2CreateChain(bodyId, a.def);
            b2RecCheckChainId(rdr, gotId, recId);
        }

        private static void b2RecDispatch_DestroyChain(in B2RecArgs_DestroyChain a, B2RecReader rdr)
        {
            b2DestroyChain(b2RecMakeChainId(rdr, a.chain));
        }

        private static void b2RecDispatch_ChainSetSurfaceMaterial(in B2RecArgs_ChainSetSurfaceMaterial a, B2RecReader rdr)
        {
            b2Chain_SetSurfaceMaterial(b2RecMakeChainId(rdr, a.chain), a.material, a.materialIndex);
        }

        // Joint create: body ids in the def are remapped to the replay world before the call.

        private static void b2RecDispatch_CreateDistanceJoint(in B2RecArgs_CreateDistanceJoint a, B2RecReader rdr)
        {
            B2JointId recId = b2RecR_JOINTID(rdr);
            B2DistanceJointDef def = a.def;
            def.@base.bodyIdA = b2RecMakeBodyId(rdr, def.@base.bodyIdA);
            def.@base.bodyIdB = b2RecMakeBodyId(rdr, def.@base.bodyIdB);
            b2RecCheckJointId(rdr, b2CreateDistanceJoint(rdr.replayWorldId, def), recId);
        }

        private static void b2RecDispatch_CreateMotorJoint(in B2RecArgs_CreateMotorJoint a, B2RecReader rdr)
        {
            B2JointId recId = b2RecR_JOINTID(rdr);
            B2MotorJointDef def = a.def;
            def.@base.bodyIdA = b2RecMakeBodyId(rdr, def.@base.bodyIdA);
            def.@base.bodyIdB = b2RecMakeBodyId(rdr, def.@base.bodyIdB);
            b2RecCheckJointId(rdr, b2CreateMotorJoint(rdr.replayWorldId, def), recId);
        }

        private static void b2RecDispatch_CreateFilterJoint(in B2RecArgs_CreateFilterJoint a, B2RecReader rdr)
        {
            B2JointId recId = b2RecR_JOINTID(rdr);
            B2FilterJointDef def = a.def;
            def.@base.bodyIdA = b2RecMakeBodyId(rdr, def.@base.bodyIdA);
            def.@base.bodyIdB = b2RecMakeBodyId(rdr, def.@base.bodyIdB);
            b2RecCheckJointId(rdr, b2CreateFilterJoint(rdr.replayWorldId, def), recId);
        }

        private static void b2RecDispatch_CreatePrismaticJoint(in B2RecArgs_CreatePrismaticJoint a, B2RecReader rdr)
        {
            B2JointId recId = b2RecR_JOINTID(rdr);
            B2PrismaticJointDef def = a.def;
            def.@base.bodyIdA = b2RecMakeBodyId(rdr, def.@base.bodyIdA);
            def.@base.bodyIdB = b2RecMakeBodyId(rdr, def.@base.bodyIdB);
            b2RecCheckJointId(rdr, b2CreatePrismaticJoint(rdr.replayWorldId, def), recId);
        }

        private static void b2RecDispatch_CreateRevoluteJoint(in B2RecArgs_CreateRevoluteJoint a, B2RecReader rdr)
        {
            B2JointId recId = b2RecR_JOINTID(rdr);
            B2RevoluteJointDef def = a.def;
            def.@base.bodyIdA = b2RecMakeBodyId(rdr, def.@base.bodyIdA);
            def.@base.bodyIdB = b2RecMakeBodyId(rdr, def.@base.bodyIdB);
            b2RecCheckJointId(rdr, b2CreateRevoluteJoint(rdr.replayWorldId, def), recId);
        }

        private static void b2RecDispatch_CreateWeldJoint(in B2RecArgs_CreateWeldJoint a, B2RecReader rdr)
        {
            B2JointId recId = b2RecR_JOINTID(rdr);
            B2WeldJointDef def = a.def;
            def.@base.bodyIdA = b2RecMakeBodyId(rdr, def.@base.bodyIdA);
            def.@base.bodyIdB = b2RecMakeBodyId(rdr, def.@base.bodyIdB);
            b2RecCheckJointId(rdr, b2CreateWeldJoint(rdr.replayWorldId, def), recId);
        }

        private static void b2RecDispatch_CreateWheelJoint(in B2RecArgs_CreateWheelJoint a, B2RecReader rdr)
        {
            B2JointId recId = b2RecR_JOINTID(rdr);
            B2WheelJointDef def = a.def;
            def.@base.bodyIdA = b2RecMakeBodyId(rdr, def.@base.bodyIdA);
            def.@base.bodyIdB = b2RecMakeBodyId(rdr, def.@base.bodyIdB);
            b2RecCheckJointId(rdr, b2CreateWheelJoint(rdr.replayWorldId, def), recId);
        }

        private static void b2RecDispatch_DestroyJoint(in B2RecArgs_DestroyJoint a, B2RecReader rdr)
        {
            b2DestroyJoint(b2RecMakeJointId(rdr, a.joint), a.wakeAttached);
        }

        private static void b2RecDispatch_JointSetLocalFrameA(in B2RecArgs_JointSetLocalFrameA a, B2RecReader rdr)
        {
            b2Joint_SetLocalFrameA(b2RecMakeJointId(rdr, a.joint), a.localFrame);
        }

        private static void b2RecDispatch_JointSetLocalFrameB(in B2RecArgs_JointSetLocalFrameB a, B2RecReader rdr)
        {
            b2Joint_SetLocalFrameB(b2RecMakeJointId(rdr, a.joint), a.localFrame);
        }

        private static void b2RecDispatch_JointSetCollideConnected(in B2RecArgs_JointSetCollideConnected a, B2RecReader rdr)
        {
            b2Joint_SetCollideConnected(b2RecMakeJointId(rdr, a.joint), a.shouldCollide);
        }

        private static void b2RecDispatch_JointWakeBodies(in B2RecArgs_JointWakeBodies a, B2RecReader rdr)
        {
            b2Joint_WakeBodies(b2RecMakeJointId(rdr, a.joint));
        }

        private static void b2RecDispatch_JointSetConstraintTuning(in B2RecArgs_JointSetConstraintTuning a, B2RecReader rdr)
        {
            b2Joint_SetConstraintTuning(b2RecMakeJointId(rdr, a.joint), a.hertz, a.dampingRatio);
        }

        private static void b2RecDispatch_JointSetForceThreshold(in B2RecArgs_JointSetForceThreshold a, B2RecReader rdr)
        {
            b2Joint_SetForceThreshold(b2RecMakeJointId(rdr, a.joint), a.threshold);
        }

        private static void b2RecDispatch_JointSetTorqueThreshold(in B2RecArgs_JointSetTorqueThreshold a, B2RecReader rdr)
        {
            b2Joint_SetTorqueThreshold(b2RecMakeJointId(rdr, a.joint), a.threshold);
        }

        private static void b2RecDispatch_DistanceJointSetLength(in B2RecArgs_DistanceJointSetLength a, B2RecReader rdr)
        {
            b2DistanceJoint_SetLength(b2RecMakeJointId(rdr, a.joint), a.length);
        }

        private static void b2RecDispatch_DistanceJointEnableSpring(in B2RecArgs_DistanceJointEnableSpring a, B2RecReader rdr)
        {
            b2DistanceJoint_EnableSpring(b2RecMakeJointId(rdr, a.joint), a.enableSpring);
        }

        private static void b2RecDispatch_DistanceJointSetSpringForceRange(in B2RecArgs_DistanceJointSetSpringForceRange a, B2RecReader rdr)
        {
            b2DistanceJoint_SetSpringForceRange(b2RecMakeJointId(rdr, a.joint), a.lowerForce, a.upperForce);
        }

        private static void b2RecDispatch_DistanceJointSetSpringHertz(in B2RecArgs_DistanceJointSetSpringHertz a, B2RecReader rdr)
        {
            b2DistanceJoint_SetSpringHertz(b2RecMakeJointId(rdr, a.joint), a.hertz);
        }

        private static void b2RecDispatch_DistanceJointSetSpringDampingRatio(in B2RecArgs_DistanceJointSetSpringDampingRatio a, B2RecReader rdr)
        {
            b2DistanceJoint_SetSpringDampingRatio(b2RecMakeJointId(rdr, a.joint), a.dampingRatio);
        }

        private static void b2RecDispatch_DistanceJointEnableLimit(in B2RecArgs_DistanceJointEnableLimit a, B2RecReader rdr)
        {
            b2DistanceJoint_EnableLimit(b2RecMakeJointId(rdr, a.joint), a.enableLimit);
        }

        private static void b2RecDispatch_DistanceJointSetLengthRange(in B2RecArgs_DistanceJointSetLengthRange a, B2RecReader rdr)
        {
            b2DistanceJoint_SetLengthRange(b2RecMakeJointId(rdr, a.joint), a.minLength, a.maxLength);
        }

        private static void b2RecDispatch_DistanceJointEnableMotor(in B2RecArgs_DistanceJointEnableMotor a, B2RecReader rdr)
        {
            b2DistanceJoint_EnableMotor(b2RecMakeJointId(rdr, a.joint), a.enableMotor);
        }

        private static void b2RecDispatch_DistanceJointSetMotorSpeed(in B2RecArgs_DistanceJointSetMotorSpeed a, B2RecReader rdr)
        {
            b2DistanceJoint_SetMotorSpeed(b2RecMakeJointId(rdr, a.joint), a.motorSpeed);
        }

        private static void b2RecDispatch_DistanceJointSetMaxMotorForce(in B2RecArgs_DistanceJointSetMaxMotorForce a, B2RecReader rdr)
        {
            b2DistanceJoint_SetMaxMotorForce(b2RecMakeJointId(rdr, a.joint), a.force);
        }

        private static void b2RecDispatch_MotorJointSetLinearVelocity(in B2RecArgs_MotorJointSetLinearVelocity a, B2RecReader rdr)
        {
            b2MotorJoint_SetLinearVelocity(b2RecMakeJointId(rdr, a.joint), a.velocity);
        }

        private static void b2RecDispatch_MotorJointSetAngularVelocity(in B2RecArgs_MotorJointSetAngularVelocity a, B2RecReader rdr)
        {
            b2MotorJoint_SetAngularVelocity(b2RecMakeJointId(rdr, a.joint), a.velocity);
        }

        private static void b2RecDispatch_MotorJointSetMaxVelocityForce(in B2RecArgs_MotorJointSetMaxVelocityForce a, B2RecReader rdr)
        {
            b2MotorJoint_SetMaxVelocityForce(b2RecMakeJointId(rdr, a.joint), a.maxForce);
        }

        private static void b2RecDispatch_MotorJointSetMaxVelocityTorque(in B2RecArgs_MotorJointSetMaxVelocityTorque a, B2RecReader rdr)
        {
            b2MotorJoint_SetMaxVelocityTorque(b2RecMakeJointId(rdr, a.joint), a.maxTorque);
        }

        private static void b2RecDispatch_MotorJointSetLinearHertz(in B2RecArgs_MotorJointSetLinearHertz a, B2RecReader rdr)
        {
            b2MotorJoint_SetLinearHertz(b2RecMakeJointId(rdr, a.joint), a.hertz);
        }

        private static void b2RecDispatch_MotorJointSetLinearDampingRatio(in B2RecArgs_MotorJointSetLinearDampingRatio a, B2RecReader rdr)
        {
            b2MotorJoint_SetLinearDampingRatio(b2RecMakeJointId(rdr, a.joint), a.damping);
        }

        private static void b2RecDispatch_MotorJointSetAngularHertz(in B2RecArgs_MotorJointSetAngularHertz a, B2RecReader rdr)
        {
            b2MotorJoint_SetAngularHertz(b2RecMakeJointId(rdr, a.joint), a.hertz);
        }

        private static void b2RecDispatch_MotorJointSetAngularDampingRatio(in B2RecArgs_MotorJointSetAngularDampingRatio a, B2RecReader rdr)
        {
            b2MotorJoint_SetAngularDampingRatio(b2RecMakeJointId(rdr, a.joint), a.damping);
        }

        private static void b2RecDispatch_MotorJointSetMaxSpringForce(in B2RecArgs_MotorJointSetMaxSpringForce a, B2RecReader rdr)
        {
            b2MotorJoint_SetMaxSpringForce(b2RecMakeJointId(rdr, a.joint), a.maxForce);
        }

        private static void b2RecDispatch_MotorJointSetMaxSpringTorque(in B2RecArgs_MotorJointSetMaxSpringTorque a, B2RecReader rdr)
        {
            b2MotorJoint_SetMaxSpringTorque(b2RecMakeJointId(rdr, a.joint), a.maxTorque);
        }

        private static void b2RecDispatch_PrismaticJointEnableSpring(in B2RecArgs_PrismaticJointEnableSpring a, B2RecReader rdr)
        {
            b2PrismaticJoint_EnableSpring(b2RecMakeJointId(rdr, a.joint), a.enableSpring);
        }

        private static void b2RecDispatch_PrismaticJointSetSpringHertz(in B2RecArgs_PrismaticJointSetSpringHertz a, B2RecReader rdr)
        {
            b2PrismaticJoint_SetSpringHertz(b2RecMakeJointId(rdr, a.joint), a.hertz);
        }

        private static void b2RecDispatch_PrismaticJointSetSpringDampingRatio(in B2RecArgs_PrismaticJointSetSpringDampingRatio a, B2RecReader rdr)
        {
            b2PrismaticJoint_SetSpringDampingRatio(b2RecMakeJointId(rdr, a.joint), a.dampingRatio);
        }

        private static void b2RecDispatch_PrismaticJointSetTargetTranslation(in B2RecArgs_PrismaticJointSetTargetTranslation a, B2RecReader rdr)
        {
            b2PrismaticJoint_SetTargetTranslation(b2RecMakeJointId(rdr, a.joint), a.translation);
        }

        private static void b2RecDispatch_PrismaticJointEnableLimit(in B2RecArgs_PrismaticJointEnableLimit a, B2RecReader rdr)
        {
            b2PrismaticJoint_EnableLimit(b2RecMakeJointId(rdr, a.joint), a.enableLimit);
        }

        private static void b2RecDispatch_PrismaticJointSetLimits(in B2RecArgs_PrismaticJointSetLimits a, B2RecReader rdr)
        {
            b2PrismaticJoint_SetLimits(b2RecMakeJointId(rdr, a.joint), a.lower, a.upper);
        }

        private static void b2RecDispatch_PrismaticJointEnableMotor(in B2RecArgs_PrismaticJointEnableMotor a, B2RecReader rdr)
        {
            b2PrismaticJoint_EnableMotor(b2RecMakeJointId(rdr, a.joint), a.enableMotor);
        }

        private static void b2RecDispatch_PrismaticJointSetMotorSpeed(in B2RecArgs_PrismaticJointSetMotorSpeed a, B2RecReader rdr)
        {
            b2PrismaticJoint_SetMotorSpeed(b2RecMakeJointId(rdr, a.joint), a.motorSpeed);
        }

        private static void b2RecDispatch_PrismaticJointSetMaxMotorForce(in B2RecArgs_PrismaticJointSetMaxMotorForce a, B2RecReader rdr)
        {
            b2PrismaticJoint_SetMaxMotorForce(b2RecMakeJointId(rdr, a.joint), a.force);
        }

        private static void b2RecDispatch_RevoluteJointEnableSpring(in B2RecArgs_RevoluteJointEnableSpring a, B2RecReader rdr)
        {
            b2RevoluteJoint_EnableSpring(b2RecMakeJointId(rdr, a.joint), a.enableSpring);
        }

        private static void b2RecDispatch_RevoluteJointSetSpringHertz(in B2RecArgs_RevoluteJointSetSpringHertz a, B2RecReader rdr)
        {
            b2RevoluteJoint_SetSpringHertz(b2RecMakeJointId(rdr, a.joint), a.hertz);
        }

        private static void b2RecDispatch_RevoluteJointSetSpringDampingRatio(in B2RecArgs_RevoluteJointSetSpringDampingRatio a, B2RecReader rdr)
        {
            b2RevoluteJoint_SetSpringDampingRatio(b2RecMakeJointId(rdr, a.joint), a.dampingRatio);
        }

        private static void b2RecDispatch_RevoluteJointSetTargetAngle(in B2RecArgs_RevoluteJointSetTargetAngle a, B2RecReader rdr)
        {
            b2RevoluteJoint_SetTargetAngle(b2RecMakeJointId(rdr, a.joint), a.angle);
        }

        private static void b2RecDispatch_RevoluteJointEnableLimit(in B2RecArgs_RevoluteJointEnableLimit a, B2RecReader rdr)
        {
            b2RevoluteJoint_EnableLimit(b2RecMakeJointId(rdr, a.joint), a.enableLimit);
        }

        private static void b2RecDispatch_RevoluteJointSetLimits(in B2RecArgs_RevoluteJointSetLimits a, B2RecReader rdr)
        {
            b2RevoluteJoint_SetLimits(b2RecMakeJointId(rdr, a.joint), a.lower, a.upper);
        }

        private static void b2RecDispatch_RevoluteJointEnableMotor(in B2RecArgs_RevoluteJointEnableMotor a, B2RecReader rdr)
        {
            b2RevoluteJoint_EnableMotor(b2RecMakeJointId(rdr, a.joint), a.enableMotor);
        }

        private static void b2RecDispatch_RevoluteJointSetMotorSpeed(in B2RecArgs_RevoluteJointSetMotorSpeed a, B2RecReader rdr)
        {
            b2RevoluteJoint_SetMotorSpeed(b2RecMakeJointId(rdr, a.joint), a.motorSpeed);
        }

        private static void b2RecDispatch_RevoluteJointSetMaxMotorTorque(in B2RecArgs_RevoluteJointSetMaxMotorTorque a, B2RecReader rdr)
        {
            b2RevoluteJoint_SetMaxMotorTorque(b2RecMakeJointId(rdr, a.joint), a.torque);
        }

        private static void b2RecDispatch_WeldJointSetLinearHertz(in B2RecArgs_WeldJointSetLinearHertz a, B2RecReader rdr)
        {
            b2WeldJoint_SetLinearHertz(b2RecMakeJointId(rdr, a.joint), a.hertz);
        }

        private static void b2RecDispatch_WeldJointSetLinearDampingRatio(in B2RecArgs_WeldJointSetLinearDampingRatio a, B2RecReader rdr)
        {
            b2WeldJoint_SetLinearDampingRatio(b2RecMakeJointId(rdr, a.joint), a.dampingRatio);
        }

        private static void b2RecDispatch_WeldJointSetAngularHertz(in B2RecArgs_WeldJointSetAngularHertz a, B2RecReader rdr)
        {
            b2WeldJoint_SetAngularHertz(b2RecMakeJointId(rdr, a.joint), a.hertz);
        }

        private static void b2RecDispatch_WeldJointSetAngularDampingRatio(in B2RecArgs_WeldJointSetAngularDampingRatio a, B2RecReader rdr)
        {
            b2WeldJoint_SetAngularDampingRatio(b2RecMakeJointId(rdr, a.joint), a.dampingRatio);
        }

        private static void b2RecDispatch_WheelJointEnableSpring(in B2RecArgs_WheelJointEnableSpring a, B2RecReader rdr)
        {
            b2WheelJoint_EnableSpring(b2RecMakeJointId(rdr, a.joint), a.enableSpring);
        }

        private static void b2RecDispatch_WheelJointSetSpringHertz(in B2RecArgs_WheelJointSetSpringHertz a, B2RecReader rdr)
        {
            b2WheelJoint_SetSpringHertz(b2RecMakeJointId(rdr, a.joint), a.hertz);
        }

        private static void b2RecDispatch_WheelJointSetSpringDampingRatio(in B2RecArgs_WheelJointSetSpringDampingRatio a, B2RecReader rdr)
        {
            b2WheelJoint_SetSpringDampingRatio(b2RecMakeJointId(rdr, a.joint), a.dampingRatio);
        }

        private static void b2RecDispatch_WheelJointEnableLimit(in B2RecArgs_WheelJointEnableLimit a, B2RecReader rdr)
        {
            b2WheelJoint_EnableLimit(b2RecMakeJointId(rdr, a.joint), a.enableLimit);
        }

        private static void b2RecDispatch_WheelJointSetLimits(in B2RecArgs_WheelJointSetLimits a, B2RecReader rdr)
        {
            b2WheelJoint_SetLimits(b2RecMakeJointId(rdr, a.joint), a.lower, a.upper);
        }

        private static void b2RecDispatch_WheelJointEnableMotor(in B2RecArgs_WheelJointEnableMotor a, B2RecReader rdr)
        {
            b2WheelJoint_EnableMotor(b2RecMakeJointId(rdr, a.joint), a.enableMotor);
        }

        private static void b2RecDispatch_WheelJointSetMotorSpeed(in B2RecArgs_WheelJointSetMotorSpeed a, B2RecReader rdr)
        {
            b2WheelJoint_SetMotorSpeed(b2RecMakeJointId(rdr, a.joint), a.motorSpeed);
        }

        private static void b2RecDispatch_WheelJointSetMaxMotorTorque(in B2RecArgs_WheelJointSetMaxMotorTorque a, B2RecReader rdr)
        {
            b2WheelJoint_SetMaxMotorTorque(b2RecMakeJointId(rdr, a.joint), a.torque);
        }

        private static void b2RecDispatch_QueryOverlapAABB(in B2RecArgs_QueryOverlapAABB a, B2RecReader rdr)
        {
            uint n = b2RecR_U32(rdr);
            b2RecEnsureHits(rdr, (int)n);
            if (!rdr.ok)
            {
                return;
            }
            for (uint i = 0; i < n; ++i)
            {
                rdr.hits[i].id = b2RecMakeShapeId(rdr, b2RecR_SHAPEID(rdr));
                rdr.hits[i].userReturnB = b2RecR_BOOL(rdr);
            }
            b2RecR_TREESTATS(rdr);
            if (!rdr.ok)
                return;
            object rc = new B2RecReplayQueryCtx { rdr = rdr, hits = rdr.hits, count = (int)n, cursor = 0 };
            b2World_OverlapAABB(rdr.replayWorldId, a.aabb, a.filter, b2RecReplayOverlapTrampoline, rc);
            if (Unsafe.Unbox<B2RecReplayQueryCtx>(rc).cursor != (int)n)
                rdr.diverged = true;
            if (rdr.owner != null)
            {
                ref B2RecDrawQuery q = ref b2RecStashQueryBegin(rdr.owner, (int)B2RecQueryKind.B2_RECQ_OVERLAP_AABB, rdr.hits, (int)n);
                q.filter = a.filter;
                q.aabb = a.aabb;
            }
        }

        private static void b2RecDispatch_QueryOverlapShape(in B2RecArgs_QueryOverlapShape a, B2RecReader rdr)
        {
            uint n = b2RecR_U32(rdr);
            b2RecEnsureHits(rdr, (int)n);
            if (!rdr.ok)
            {
                return;
            }
            for (uint i = 0; i < n; ++i)
            {
                rdr.hits[i].id = b2RecMakeShapeId(rdr, b2RecR_SHAPEID(rdr));
                rdr.hits[i].userReturnB = b2RecR_BOOL(rdr);
            }
            b2RecR_TREESTATS(rdr);
            if (!rdr.ok)
                return;
            object rc = new B2RecReplayQueryCtx { rdr = rdr, hits = rdr.hits, count = (int)n, cursor = 0 };
            B2ShapeProxy proxy = a.proxy;
            b2World_OverlapShape(rdr.replayWorldId, ref proxy, a.filter, b2RecReplayOverlapTrampoline, rc);
            if (Unsafe.Unbox<B2RecReplayQueryCtx>(rc).cursor != (int)n)
                rdr.diverged = true;
            if (rdr.owner != null)
            {
                ref B2RecDrawQuery q = ref b2RecStashQueryBegin(rdr.owner, (int)B2RecQueryKind.B2_RECQ_OVERLAP_SHAPE, rdr.hits, (int)n);
                q.filter = a.filter;
                q.proxy = a.proxy;
            }
        }

        // Cast ray dispatcher

        private static void b2RecDispatch_QueryCastRay(in B2RecArgs_QueryCastRay a, B2RecReader rdr)
        {
            uint n = b2RecR_U32(rdr);
            b2RecEnsureHits(rdr, (int)n);
            if (!rdr.ok)
            {
                return;
            }
            for (uint i = 0; i < n; ++i)
            {
                rdr.hits[i].id = b2RecMakeShapeId(rdr, b2RecR_SHAPEID(rdr));
                rdr.hits[i].point = b2RecR_VEC2(rdr);
                rdr.hits[i].normal = b2RecR_VEC2(rdr);
                rdr.hits[i].fraction = b2RecR_F32(rdr);
                rdr.hits[i].userReturnF = b2RecR_F32(rdr);
            }
            b2RecR_TREESTATS(rdr);
            if (!rdr.ok)
                return;
            object rc = new B2RecReplayQueryCtx { rdr = rdr, hits = rdr.hits, count = (int)n, cursor = 0 };
            b2World_CastRay(rdr.replayWorldId, a.origin, a.translation, a.filter, b2RecReplayCastTrampoline, rc);
            if (Unsafe.Unbox<B2RecReplayQueryCtx>(rc).cursor != (int)n)
                rdr.diverged = true;
            if (rdr.owner != null)
            {
                ref B2RecDrawQuery q = ref b2RecStashQueryBegin(rdr.owner, (int)B2RecQueryKind.B2_RECQ_CAST_RAY, rdr.hits, (int)n);
                q.filter = a.filter;
                q.origin = a.origin;
                q.translation = a.translation;
            }
        }

        private static void b2RecDispatch_QueryCastShape(in B2RecArgs_QueryCastShape a, B2RecReader rdr)
        {
            uint n = b2RecR_U32(rdr);
            b2RecEnsureHits(rdr, (int)n);
            if (!rdr.ok)
            {
                return;
            }
            for (uint i = 0; i < n; ++i)
            {
                rdr.hits[i].id = b2RecMakeShapeId(rdr, b2RecR_SHAPEID(rdr));
                rdr.hits[i].point = b2RecR_VEC2(rdr);
                rdr.hits[i].normal = b2RecR_VEC2(rdr);
                rdr.hits[i].fraction = b2RecR_F32(rdr);
                rdr.hits[i].userReturnF = b2RecR_F32(rdr);
            }
            b2RecR_TREESTATS(rdr);
            if (!rdr.ok)
                return;
            object rc = new B2RecReplayQueryCtx { rdr = rdr, hits = rdr.hits, count = (int)n, cursor = 0 };
            B2ShapeProxy proxy = a.proxy;
            b2World_CastShape(rdr.replayWorldId, ref proxy, a.translation, a.filter, b2RecReplayCastTrampoline, rc);
            if (Unsafe.Unbox<B2RecReplayQueryCtx>(rc).cursor != (int)n)
                rdr.diverged = true;
            if (rdr.owner != null)
            {
                ref B2RecDrawQuery q = ref b2RecStashQueryBegin(rdr.owner, (int)B2RecQueryKind.B2_RECQ_CAST_SHAPE, rdr.hits, (int)n);
                q.filter = a.filter;
                q.proxy = a.proxy;
                q.translation = a.translation;
            }
        }

        // CollideMover dispatcher

        private static void b2RecDispatch_QueryCollideMover(in B2RecArgs_QueryCollideMover a, B2RecReader rdr)
        {
            uint n = b2RecR_U32(rdr);
            b2RecEnsureHits(rdr, (int)n);
            if (!rdr.ok)
            {
                return;
            }
            for (uint i = 0; i < n; ++i)
            {
                rdr.hits[i].id = b2RecMakeShapeId(rdr, b2RecR_SHAPEID(rdr));
                rdr.hits[i].plane = b2RecR_PLANERESULT(rdr);
                rdr.hits[i].userReturnB = b2RecR_BOOL(rdr);
            }
            // CollideMover has no TREESTATS tail (returns void)
            if (!rdr.ok)
                return;
            object rc = new B2RecReplayQueryCtx { rdr = rdr, hits = rdr.hits, count = (int)n, cursor = 0 };
            b2World_CollideMover(rdr.replayWorldId, a.mover, a.filter, b2RecReplayPlaneTrampoline, rc);
            if (Unsafe.Unbox<B2RecReplayQueryCtx>(rc).cursor != (int)n)
                rdr.diverged = true;
            if (rdr.owner != null)
            {
                ref B2RecDrawQuery q = ref b2RecStashQueryBegin(rdr.owner, (int)B2RecQueryKind.B2_RECQ_COLLIDE_MOVER, rdr.hits, (int)n);
                q.filter = a.filter;
                q.mover = a.mover;
            }
        }

        // CastRayClosest dispatcher

        private static void b2RecDispatch_QueryCastRayClosest(in B2RecArgs_QueryCastRayClosest a, B2RecReader rdr)
        {
            B2RayResult rec = b2RecR_RAYRESULT(rdr);
            if (!rdr.ok)
                return;
            B2RayResult got = b2World_CastRayClosest(rdr.replayWorldId, a.origin, a.translation, a.filter);
            if (got.hit != rec.hit ||
                (got.hit && (got.shapeId.index1 != rec.shapeId.index1 || got.shapeId.generation != rec.shapeId.generation ||
                             b2RecVec2Differs(got.point, rec.point) || b2RecVec2Differs(got.normal, rec.normal) ||
                             b2RecF32Differs(got.fraction, rec.fraction))))
            {
                rdr.diverged = true;
            }
            if (rdr.owner != null)
            {
                // Stash the closest result as a single pooled hit so the shared draw loop renders its point
                B2RecRecordedHit h = default;
                h.id = b2RecMakeShapeId(rdr, rec.shapeId);
                h.point = rec.point;
                h.normal = rec.normal;
                h.fraction = rec.fraction;
                B2RecRecordedHit[] hits = { h };
                ref B2RecDrawQuery q = ref b2RecStashQueryBegin(rdr.owner, (int)B2RecQueryKind.B2_RECQ_CAST_RAY_CLOSEST, hits, rec.hit ? 1 : 0);
                q.filter = a.filter;
                q.origin = a.origin;
                q.translation = a.translation;
            }
        }

        // CastMover dispatcher

        private static void b2RecDispatch_QueryCastMover(in B2RecArgs_QueryCastMover a, B2RecReader rdr)
        {
            float rec = b2RecR_F32(rdr);
            if (!rdr.ok)
                return;
            float got = b2World_CastMover(rdr.replayWorldId, a.mover, a.translation, a.filter);
            if (b2RecF32Differs(got, rec))
                rdr.diverged = true;
            if (rdr.owner != null)
            {
                ref B2RecDrawQuery q = ref b2RecStashQueryBegin(rdr.owner, (int)B2RecQueryKind.B2_RECQ_CAST_MOVER, null, 0);
                q.filter = a.filter;
                q.mover = a.mover;
                q.translation = a.translation;
                q.castFraction = rec;
            }
        }

        // ShapeTestPoint dispatcher

        private static void b2RecDispatch_ShapeTestPoint(in B2RecArgs_ShapeTestPoint a, B2RecReader rdr)
        {
            bool rec = b2RecR_BOOL(rdr);
            if (!rdr.ok)
                return;
            B2ShapeId id = b2RecMakeShapeId(rdr, a.shape);
            bool got = b2Shape_TestPoint(id, a.point);
            if (got != rec)
                rdr.diverged = true;
            if (rdr.owner != null)
            {
                ref B2RecDrawQuery q = ref b2RecStashQueryBegin(rdr.owner, (int)B2RecQueryKind.B2_RECQ_SHAPE_TEST_POINT, null, 0);
                q.shape = id;
                q.origin = a.point;
                q.boolResult = rec;
            }
        }

        // ShapeRayCast dispatcher

        private static void b2RecDispatch_ShapeRayCast(in B2RecArgs_ShapeRayCast a, B2RecReader rdr)
        {
            B2CastOutput rec = b2RecR_CASTOUTPUT(rdr);
            if (!rdr.ok)
                return;
            B2ShapeId id = b2RecMakeShapeId(rdr, a.shape);
            B2RayCastInput input = a.input;
            B2CastOutput got = b2Shape_RayCast(id, input);
            if (got.hit != rec.hit ||
                (got.hit && (b2RecVec2Differs(got.normal, rec.normal) || b2RecVec2Differs(got.point, rec.point) ||
                             b2RecF32Differs(got.fraction, rec.fraction))))
            {
                rdr.diverged = true;
            }
            if (rdr.owner != null)
            {
                ref B2RecDrawQuery q = ref b2RecStashQueryBegin(rdr.owner, (int)B2RecQueryKind.B2_RECQ_SHAPE_RAY_CAST, null, 0);
                q.shape = id;
                q.origin = input.origin;
                q.translation = input.translation;
                q.castOut = rec;
            }
        }

        private static void b2RecDispatch_StateHash(in B2RecArgs_StateHash a, B2RecReader rdr)
        {
            B2World world = b2GetWorldFromId(rdr.replayWorldId);
            ulong computed = b2HashWorldState(world);
            ulong recorded = a.hash;
            if (computed != recorded)
            {
                Console.WriteLine("b2ReplayFile: StateHash mismatch (recorded=0x{0:X}, computed=0x{1:X})", recorded, computed);
                // Non-fatal: reading continues so a viewer can show where divergence begins
                rdr.diverged = true;
            }
        }

    }
}
