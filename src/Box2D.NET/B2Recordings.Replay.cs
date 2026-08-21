// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.IO;
using System.Runtime.CompilerServices;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Cores;
using static Box2D.NET.B2DistanceJoints;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2MotorJoints;
using static Box2D.NET.B2PrismaticJoints;
using static Box2D.NET.B2RevoluteJoints;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2WeldJoints;
using static Box2D.NET.B2WheelJoints;
using static Box2D.NET.B2Worlds;

namespace Box2D.NET
{
    /**
     * @defgroup replay Replay
     * These functions allow you to replay a recorded simulation.
     * @{
     */
    public static partial class B2Recordings
    {
        // Append a created body to the outliner tracking list. Ordinals are creation order and never reused.
        private static void b2RecTrackBodyCreate(B2RecPlayer player, B2BodyId id)
        {
            b2RecGrow(ref player.bodyIds, ref player.bodyIdCap, player.bodyIdCount + 1, player.bodyIdCount,
                B2SizeOf<B2BodyId>.Size);
            player.bodyIds[player.bodyIdCount] = id;
            player.bodyIdCount += 1;
        }

        // Leave a hole so later ordinals do not shift, keeping a stored selection stable across the playthrough
        private static void b2RecTrackBodyDestroy(B2RecPlayer player, B2BodyId id)
        {
            for (int i = 0; i < player.bodyIdCount; ++i)
            {
                if (player.bodyIds[i] == id)
                {
                    player.bodyIds[i] = b2_nullBodyId;
                    return;
                }
            }
        }

        // A create op appends its returned id after the args. Replay compares index1 and generation
        // only, since world0 differs between record and replay. A mismatch means structural drift.

        private static void b2RecCheckId(B2RecReader rdr, string kind, int gotIndex, uint gotGeneration, int recordedIndex,
            uint recordedGeneration)
        {
            if (gotIndex != recordedIndex || gotGeneration != recordedGeneration)
            {
                Console.WriteLine("b2ReplayFile: {0} id mismatch (rec index1={1} gen={2}, got index1={3} gen={4})",
                    kind, recordedIndex, recordedGeneration, gotIndex, gotGeneration);
                rdr.ok = false;
            }
        }

        private static bool b2RecReplayOverlapTrampoline(B2ShapeId id, object ctx)
        {
            ref B2RecReplayQueryCtx rc = ref Unsafe.Unbox<B2RecReplayQueryCtx>(ctx);
            if (rc.cursor >= rc.count)
            {
                rc.rdr.diverged = true;
                return false;
            }
            B2RecRecordedHit h = rc.hits[rc.cursor++];
            if (id.index1 != h.id.index1 || id.generation != h.id.generation)
            {
                rc.rdr.diverged = true;
            }
            return h.userReturnB;
        }

        private static float b2RecReplayCastTrampoline(B2ShapeId id, B2Vec2 point, B2Vec2 normal, float fraction, ref object ctx)
        {
            ref B2RecReplayQueryCtx rc = ref Unsafe.Unbox<B2RecReplayQueryCtx>(ctx);
            if (rc.cursor >= rc.count)
            {
                rc.rdr.diverged = true;
                return 0.0f;
            }
            B2RecRecordedHit h = rc.hits[rc.cursor++];
            if (id.index1 != h.id.index1 || id.generation != h.id.generation || b2RecVec2Differs(point, h.point) ||
                b2RecVec2Differs(normal, h.normal) || b2RecF32Differs(fraction, h.fraction))
            {
                rc.rdr.diverged = true;
            }
            return h.userReturnF;
        }

        private static bool b2RecReplayPlaneTrampoline(B2ShapeId id, ref B2PlaneResult plane, object ctx)
        {
            ref B2RecReplayQueryCtx rc = ref Unsafe.Unbox<B2RecReplayQueryCtx>(ctx);
            if (rc.cursor >= rc.count)
            {
                rc.rdr.diverged = true;
                return true;
            }
            B2RecRecordedHit h = rc.hits[rc.cursor++];
            if (id.index1 != h.id.index1 || id.generation != h.id.generation ||
                b2RecVec2Differs(plane.plane.normal, h.plane.plane.normal) ||
                b2RecF32Differs(plane.plane.offset, h.plane.plane.offset) || b2RecVec2Differs(plane.point, h.plane.point) || plane.hit != h.plane.hit)
            {
                rc.rdr.diverged = true;
            }
            return h.userReturnB;
        }

        // Per-frame query stash: push a draw record and copy its hits into frameHits.
        // Ids in hits[] are already remapped to the replay world by the caller.

        private static ref B2RecDrawQuery b2RecStashQueryBegin(B2RecPlayer player, int kind, B2RecRecordedHit[] hits, int hitCount)
        {
            b2RecGrowFrameQueries(player);
            int queryIndex = player.frameQueryCount;
            player.frameQueries[queryIndex] = default;
            ref B2RecDrawQuery q = ref player.frameQueries[queryIndex];
            q.kind = kind;
            q.hitStart = player.frameHitCount;
            q.hitCount = hitCount;
            b2RecGrowFrameHits(player, hitCount);
            for (int i = 0; i < hitCount; ++i)
            {
                player.frameHits[player.frameHitCount + i] = hits[i];
            }
            player.frameHitCount += hitCount;
            player.frameQueryCount++;
            return ref q;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool b2RecF32Differs(float a, float b)
        {
            // Float bit comparators: compare raw bits so NaN != NaN is handled consistently
            return BitConverter.SingleToInt32Bits(a) != BitConverter.SingleToInt32Bits(b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool b2RecVec2Differs(B2Vec2 a, B2Vec2 b)
        {
            return b2RecF32Differs(a.X, b.X) || b2RecF32Differs(a.Y, b.Y);
        }

        // Returns the opcode just dispatched, or -1 at end of file or on a fatal read error.
        private static int b2RecDispatchOne(B2RecPlayer player)
        {
            B2RecReader rdr = player.rdr;
            if (rdr.cursor >= rdr.size || !rdr.ok)
            {
                return -1;
            }

            byte opcode = b2RecR_U8(rdr);
            if (!rdr.ok)
            {
                return -1;
            }
            uint payloadSize = b2RecR_U24(rdr);
            if (!rdr.ok)
            {
                return -1;
            }
            int payloadStart = rdr.cursor;

            // Codegen pass 2 builds the read-and-dispatch switch cases. Each case reads the ARG fields
            // into a b2RecArgs_<Name> then dispatches. Create ops read the returned id in their dispatcher.
            switch ((B2RecOpcode)opcode)
            {
                case B2RecOpcode.CreateWorld:
                {
                    B2RecArgs_CreateWorld a = default;
                    a.def = b2RecR_WORLDDEF(rdr);
                    b2RecDispatch_CreateWorld(in a, rdr);
                    break;
                }
                case B2RecOpcode.DestroyWorld:
                {
                    B2RecArgs_DestroyWorld a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    b2RecDispatch_DestroyWorld(in a, rdr);
                    break;
                }
                case B2RecOpcode.Step:
                {
                    B2RecArgs_Step a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.dt = b2RecR_F32(rdr);
                    a.subStepCount = b2RecR_I32(rdr);
                    b2RecDispatch_Step(in a, rdr);
                    break;
                }
                case B2RecOpcode.WorldEnableSleeping:
                {
                    B2RecArgs_WorldEnableSleeping a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.flag = b2RecR_BOOL(rdr);
                    b2RecDispatch_WorldEnableSleeping(in a, rdr);
                    break;
                }
                case B2RecOpcode.WorldEnableContinuous:
                {
                    B2RecArgs_WorldEnableContinuous a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.flag = b2RecR_BOOL(rdr);
                    b2RecDispatch_WorldEnableContinuous(in a, rdr);
                    break;
                }
                case B2RecOpcode.WorldSetRestitutionThreshold:
                {
                    B2RecArgs_WorldSetRestitutionThreshold a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.value = b2RecR_F32(rdr);
                    b2RecDispatch_WorldSetRestitutionThreshold(in a, rdr);
                    break;
                }
                case B2RecOpcode.WorldSetHitEventThreshold:
                {
                    B2RecArgs_WorldSetHitEventThreshold a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.value = b2RecR_F32(rdr);
                    b2RecDispatch_WorldSetHitEventThreshold(in a, rdr);
                    break;
                }
                case B2RecOpcode.WorldSetGravity:
                {
                    B2RecArgs_WorldSetGravity a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.gravity = b2RecR_VEC2(rdr);
                    b2RecDispatch_WorldSetGravity(in a, rdr);
                    break;
                }
                case B2RecOpcode.WorldExplode:
                {
                    B2RecArgs_WorldExplode a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.def = b2RecR_EXPLOSIONDEF(rdr);
                    b2RecDispatch_WorldExplode(in a, rdr);
                    break;
                }
                case B2RecOpcode.WorldSetContactTuning:
                {
                    B2RecArgs_WorldSetContactTuning a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.hertz = b2RecR_F32(rdr);
                    a.dampingRatio = b2RecR_F32(rdr);
                    a.pushSpeed = b2RecR_F32(rdr);
                    b2RecDispatch_WorldSetContactTuning(in a, rdr);
                    break;
                }
                case B2RecOpcode.WorldSetContactRecycleDistance:
                {
                    B2RecArgs_WorldSetContactRecycleDistance a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.recycleDistance = b2RecR_F32(rdr);
                    b2RecDispatch_WorldSetContactRecycleDistance(in a, rdr);
                    break;
                }
                case B2RecOpcode.WorldSetMaximumLinearSpeed:
                {
                    B2RecArgs_WorldSetMaximumLinearSpeed a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.maximumLinearSpeed = b2RecR_F32(rdr);
                    b2RecDispatch_WorldSetMaximumLinearSpeed(in a, rdr);
                    break;
                }
                case B2RecOpcode.WorldEnableWarmStarting:
                {
                    B2RecArgs_WorldEnableWarmStarting a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.flag = b2RecR_BOOL(rdr);
                    b2RecDispatch_WorldEnableWarmStarting(in a, rdr);
                    break;
                }
                case B2RecOpcode.WorldRebuildStaticTree:
                {
                    B2RecArgs_WorldRebuildStaticTree a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    b2RecDispatch_WorldRebuildStaticTree(in a, rdr);
                    break;
                }
                case B2RecOpcode.WorldEnableSpeculative:
                {
                    B2RecArgs_WorldEnableSpeculative a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.flag = b2RecR_BOOL(rdr);
                    b2RecDispatch_WorldEnableSpeculative(in a, rdr);
                    break;
                }
                case B2RecOpcode.CreateBody:
                {
                    B2RecArgs_CreateBody a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.def = b2RecR_BODYDEF(rdr);
                    b2RecDispatch_CreateBody(in a, rdr);
                    break;
                }
                case B2RecOpcode.DestroyBody:
                {
                    B2RecArgs_DestroyBody a = default;
                    a.body = b2RecR_BODYID(rdr);
                    b2RecDispatch_DestroyBody(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodySetTransform:
                {
                    B2RecArgs_BodySetTransform a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.position = b2RecR_VEC2(rdr);
                    a.rotation = b2RecR_ROT(rdr);
                    b2RecDispatch_BodySetTransform(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodySetLinearVelocity:
                {
                    B2RecArgs_BodySetLinearVelocity a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.v = b2RecR_VEC2(rdr);
                    b2RecDispatch_BodySetLinearVelocity(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodySetType:
                {
                    B2RecArgs_BodySetType a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.type = b2RecR_I32(rdr);
                    b2RecDispatch_BodySetType(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodySetName:
                {
                    B2RecArgs_BodySetName a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.name = b2RecR_STR(rdr);
                    b2RecDispatch_BodySetName(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodySetAngularVelocity:
                {
                    B2RecArgs_BodySetAngularVelocity a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.w = b2RecR_F32(rdr);
                    b2RecDispatch_BodySetAngularVelocity(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodySetTargetTransform:
                {
                    B2RecArgs_BodySetTargetTransform a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.target = b2RecR_XF(rdr);
                    a.timeStep = b2RecR_F32(rdr);
                    a.wake = b2RecR_BOOL(rdr);
                    b2RecDispatch_BodySetTargetTransform(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodyApplyForce:
                {
                    B2RecArgs_BodyApplyForce a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.force = b2RecR_VEC2(rdr);
                    a.point = b2RecR_VEC2(rdr);
                    a.wake = b2RecR_BOOL(rdr);
                    b2RecDispatch_BodyApplyForce(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodyApplyForceToCenter:
                {
                    B2RecArgs_BodyApplyForceToCenter a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.force = b2RecR_VEC2(rdr);
                    a.wake = b2RecR_BOOL(rdr);
                    b2RecDispatch_BodyApplyForceToCenter(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodyApplyTorque:
                {
                    B2RecArgs_BodyApplyTorque a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.torque = b2RecR_F32(rdr);
                    a.wake = b2RecR_BOOL(rdr);
                    b2RecDispatch_BodyApplyTorque(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodyClearForces:
                {
                    B2RecArgs_BodyClearForces a = default;
                    a.body = b2RecR_BODYID(rdr);
                    b2RecDispatch_BodyClearForces(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodyApplyLinearImpulse:
                {
                    B2RecArgs_BodyApplyLinearImpulse a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.impulse = b2RecR_VEC2(rdr);
                    a.point = b2RecR_VEC2(rdr);
                    a.wake = b2RecR_BOOL(rdr);
                    b2RecDispatch_BodyApplyLinearImpulse(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodyApplyLinearImpulseToCenter:
                {
                    B2RecArgs_BodyApplyLinearImpulseToCenter a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.impulse = b2RecR_VEC2(rdr);
                    a.wake = b2RecR_BOOL(rdr);
                    b2RecDispatch_BodyApplyLinearImpulseToCenter(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodyApplyAngularImpulse:
                {
                    B2RecArgs_BodyApplyAngularImpulse a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.impulse = b2RecR_F32(rdr);
                    a.wake = b2RecR_BOOL(rdr);
                    b2RecDispatch_BodyApplyAngularImpulse(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodySetMassData:
                {
                    B2RecArgs_BodySetMassData a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.massData = b2RecR_MASSDATA(rdr);
                    b2RecDispatch_BodySetMassData(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodyApplyMassFromShapes:
                {
                    B2RecArgs_BodyApplyMassFromShapes a = default;
                    a.body = b2RecR_BODYID(rdr);
                    b2RecDispatch_BodyApplyMassFromShapes(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodySetLinearDamping:
                {
                    B2RecArgs_BodySetLinearDamping a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.damping = b2RecR_F32(rdr);
                    b2RecDispatch_BodySetLinearDamping(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodySetAngularDamping:
                {
                    B2RecArgs_BodySetAngularDamping a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.damping = b2RecR_F32(rdr);
                    b2RecDispatch_BodySetAngularDamping(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodySetGravityScale:
                {
                    B2RecArgs_BodySetGravityScale a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.scale = b2RecR_F32(rdr);
                    b2RecDispatch_BodySetGravityScale(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodySetAwake:
                {
                    B2RecArgs_BodySetAwake a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.awake = b2RecR_BOOL(rdr);
                    b2RecDispatch_BodySetAwake(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodyWakeTouching:
                {
                    B2RecArgs_BodyWakeTouching a = default;
                    a.body = b2RecR_BODYID(rdr);
                    b2RecDispatch_BodyWakeTouching(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodyEnableSleep:
                {
                    B2RecArgs_BodyEnableSleep a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.flag = b2RecR_BOOL(rdr);
                    b2RecDispatch_BodyEnableSleep(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodySetSleepThreshold:
                {
                    B2RecArgs_BodySetSleepThreshold a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.threshold = b2RecR_F32(rdr);
                    b2RecDispatch_BodySetSleepThreshold(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodyDisable:
                {
                    B2RecArgs_BodyDisable a = default;
                    a.body = b2RecR_BODYID(rdr);
                    b2RecDispatch_BodyDisable(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodyEnable:
                {
                    B2RecArgs_BodyEnable a = default;
                    a.body = b2RecR_BODYID(rdr);
                    b2RecDispatch_BodyEnable(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodySetMotionLocks:
                {
                    B2RecArgs_BodySetMotionLocks a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.locks = b2RecR_LOCKS(rdr);
                    b2RecDispatch_BodySetMotionLocks(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodySetBullet:
                {
                    B2RecArgs_BodySetBullet a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.flag = b2RecR_BOOL(rdr);
                    b2RecDispatch_BodySetBullet(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodyEnableContactRecycling:
                {
                    B2RecArgs_BodyEnableContactRecycling a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.flag = b2RecR_BOOL(rdr);
                    b2RecDispatch_BodyEnableContactRecycling(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodyEnableContactEvents:
                {
                    B2RecArgs_BodyEnableContactEvents a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.flag = b2RecR_BOOL(rdr);
                    b2RecDispatch_BodyEnableContactEvents(in a, rdr);
                    break;
                }
                case B2RecOpcode.BodyEnableHitEvents:
                {
                    B2RecArgs_BodyEnableHitEvents a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.flag = b2RecR_BOOL(rdr);
                    b2RecDispatch_BodyEnableHitEvents(in a, rdr);
                    break;
                }
                case B2RecOpcode.CreateCircleShape:
                {
                    B2RecArgs_CreateCircleShape a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.def = b2RecR_SHAPEDEF(rdr);
                    a.circle = b2RecR_CIRCLE(rdr);
                    b2RecDispatch_CreateCircleShape(in a, rdr);
                    break;
                }
                case B2RecOpcode.CreateCapsuleShape:
                {
                    B2RecArgs_CreateCapsuleShape a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.def = b2RecR_SHAPEDEF(rdr);
                    a.capsule = b2RecR_CAPSULE(rdr);
                    b2RecDispatch_CreateCapsuleShape(in a, rdr);
                    break;
                }
                case B2RecOpcode.CreateSegmentShape:
                {
                    B2RecArgs_CreateSegmentShape a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.def = b2RecR_SHAPEDEF(rdr);
                    a.segment = b2RecR_SEGMENT(rdr);
                    b2RecDispatch_CreateSegmentShape(in a, rdr);
                    break;
                }
                case B2RecOpcode.CreatePolygonShape:
                {
                    B2RecArgs_CreatePolygonShape a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.def = b2RecR_SHAPEDEF(rdr);
                    a.polygon = b2RecR_POLYGON(rdr);
                    b2RecDispatch_CreatePolygonShape(in a, rdr);
                    break;
                }
                case B2RecOpcode.CreateChainSegmentShape:
                {
                    B2RecArgs_CreateChainSegmentShape a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.def = b2RecR_SHAPEDEF(rdr);
                    a.chainSegment = b2RecR_CHAINSEG(rdr);
                    b2RecDispatch_CreateChainSegmentShape(in a, rdr);
                    break;
                }
                case B2RecOpcode.DestroyShape:
                {
                    B2RecArgs_DestroyShape a = default;
                    a.shape = b2RecR_SHAPEID(rdr);
                    a.updateBodyMass = b2RecR_BOOL(rdr);
                    b2RecDispatch_DestroyShape(in a, rdr);
                    break;
                }
                case B2RecOpcode.ShapeSetDensity:
                {
                    B2RecArgs_ShapeSetDensity a = default;
                    a.shape = b2RecR_SHAPEID(rdr);
                    a.density = b2RecR_F32(rdr);
                    a.updateBodyMass = b2RecR_BOOL(rdr);
                    b2RecDispatch_ShapeSetDensity(in a, rdr);
                    break;
                }
                case B2RecOpcode.ShapeSetFriction:
                {
                    B2RecArgs_ShapeSetFriction a = default;
                    a.shape = b2RecR_SHAPEID(rdr);
                    a.friction = b2RecR_F32(rdr);
                    b2RecDispatch_ShapeSetFriction(in a, rdr);
                    break;
                }
                case B2RecOpcode.ShapeSetRestitution:
                {
                    B2RecArgs_ShapeSetRestitution a = default;
                    a.shape = b2RecR_SHAPEID(rdr);
                    a.restitution = b2RecR_F32(rdr);
                    b2RecDispatch_ShapeSetRestitution(in a, rdr);
                    break;
                }
                case B2RecOpcode.ShapeSetUserMaterial:
                {
                    B2RecArgs_ShapeSetUserMaterial a = default;
                    a.shape = b2RecR_SHAPEID(rdr);
                    a.material = b2RecR_U64(rdr);
                    b2RecDispatch_ShapeSetUserMaterial(in a, rdr);
                    break;
                }
                case B2RecOpcode.ShapeSetSurfaceMaterial:
                {
                    B2RecArgs_ShapeSetSurfaceMaterial a = default;
                    a.shape = b2RecR_SHAPEID(rdr);
                    a.material = b2RecR_MATERIAL(rdr);
                    b2RecDispatch_ShapeSetSurfaceMaterial(in a, rdr);
                    break;
                }
                case B2RecOpcode.ShapeSetFilter:
                {
                    B2RecArgs_ShapeSetFilter a = default;
                    a.shape = b2RecR_SHAPEID(rdr);
                    a.filter = b2RecR_FILTER(rdr);
                    b2RecDispatch_ShapeSetFilter(in a, rdr);
                    break;
                }
                case B2RecOpcode.ShapeEnableSensorEvents:
                {
                    B2RecArgs_ShapeEnableSensorEvents a = default;
                    a.shape = b2RecR_SHAPEID(rdr);
                    a.flag = b2RecR_BOOL(rdr);
                    b2RecDispatch_ShapeEnableSensorEvents(in a, rdr);
                    break;
                }
                case B2RecOpcode.ShapeEnableContactEvents:
                {
                    B2RecArgs_ShapeEnableContactEvents a = default;
                    a.shape = b2RecR_SHAPEID(rdr);
                    a.flag = b2RecR_BOOL(rdr);
                    b2RecDispatch_ShapeEnableContactEvents(in a, rdr);
                    break;
                }
                case B2RecOpcode.ShapeEnablePreSolveEvents:
                {
                    B2RecArgs_ShapeEnablePreSolveEvents a = default;
                    a.shape = b2RecR_SHAPEID(rdr);
                    a.flag = b2RecR_BOOL(rdr);
                    b2RecDispatch_ShapeEnablePreSolveEvents(in a, rdr);
                    break;
                }
                case B2RecOpcode.ShapeEnableHitEvents:
                {
                    B2RecArgs_ShapeEnableHitEvents a = default;
                    a.shape = b2RecR_SHAPEID(rdr);
                    a.flag = b2RecR_BOOL(rdr);
                    b2RecDispatch_ShapeEnableHitEvents(in a, rdr);
                    break;
                }
                case B2RecOpcode.ShapeSetCircle:
                {
                    B2RecArgs_ShapeSetCircle a = default;
                    a.shape = b2RecR_SHAPEID(rdr);
                    a.circle = b2RecR_CIRCLE(rdr);
                    b2RecDispatch_ShapeSetCircle(in a, rdr);
                    break;
                }
                case B2RecOpcode.ShapeSetCapsule:
                {
                    B2RecArgs_ShapeSetCapsule a = default;
                    a.shape = b2RecR_SHAPEID(rdr);
                    a.capsule = b2RecR_CAPSULE(rdr);
                    b2RecDispatch_ShapeSetCapsule(in a, rdr);
                    break;
                }
                case B2RecOpcode.ShapeSetSegment:
                {
                    B2RecArgs_ShapeSetSegment a = default;
                    a.shape = b2RecR_SHAPEID(rdr);
                    a.segment = b2RecR_SEGMENT(rdr);
                    b2RecDispatch_ShapeSetSegment(in a, rdr);
                    break;
                }
                case B2RecOpcode.ShapeSetPolygon:
                {
                    B2RecArgs_ShapeSetPolygon a = default;
                    a.shape = b2RecR_SHAPEID(rdr);
                    a.polygon = b2RecR_POLYGON(rdr);
                    b2RecDispatch_ShapeSetPolygon(in a, rdr);
                    break;
                }
                case B2RecOpcode.ShapeSetChainSegment:
                {
                    B2RecArgs_ShapeSetChainSegment a = default;
                    a.shape = b2RecR_SHAPEID(rdr);
                    a.chainSegment = b2RecR_CHAINSEG(rdr);
                    b2RecDispatch_ShapeSetChainSegment(in a, rdr);
                    break;
                }
                case B2RecOpcode.ShapeApplyWind:
                {
                    B2RecArgs_ShapeApplyWind a = default;
                    a.shape = b2RecR_SHAPEID(rdr);
                    a.wind = b2RecR_VEC2(rdr);
                    a.drag = b2RecR_F32(rdr);
                    a.lift = b2RecR_F32(rdr);
                    a.wake = b2RecR_BOOL(rdr);
                    b2RecDispatch_ShapeApplyWind(in a, rdr);
                    break;
                }
                case B2RecOpcode.CreateChain:
                {
                    B2RecArgs_CreateChain a = default;
                    a.body = b2RecR_BODYID(rdr);
                    a.def = b2RecR_CHAINDEF(rdr);
                    b2RecDispatch_CreateChain(in a, rdr);
                    break;
                }
                case B2RecOpcode.DestroyChain:
                {
                    B2RecArgs_DestroyChain a = default;
                    a.chain = b2RecR_CHAINID(rdr);
                    b2RecDispatch_DestroyChain(in a, rdr);
                    break;
                }
                case B2RecOpcode.ChainSetSurfaceMaterial:
                {
                    B2RecArgs_ChainSetSurfaceMaterial a = default;
                    a.chain = b2RecR_CHAINID(rdr);
                    a.material = b2RecR_MATERIAL(rdr);
                    a.materialIndex = b2RecR_I32(rdr);
                    b2RecDispatch_ChainSetSurfaceMaterial(in a, rdr);
                    break;
                }
                case B2RecOpcode.CreateDistanceJoint:
                {
                    B2RecArgs_CreateDistanceJoint a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.def = b2RecR_DISTANCEJOINTDEF(rdr);
                    b2RecDispatch_CreateDistanceJoint(in a, rdr);
                    break;
                }
                case B2RecOpcode.CreateMotorJoint:
                {
                    B2RecArgs_CreateMotorJoint a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.def = b2RecR_MOTORJOINTDEF(rdr);
                    b2RecDispatch_CreateMotorJoint(in a, rdr);
                    break;
                }
                case B2RecOpcode.CreateFilterJoint:
                {
                    B2RecArgs_CreateFilterJoint a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.def = b2RecR_FILTERJOINTDEF(rdr);
                    b2RecDispatch_CreateFilterJoint(in a, rdr);
                    break;
                }
                case B2RecOpcode.CreatePrismaticJoint:
                {
                    B2RecArgs_CreatePrismaticJoint a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.def = b2RecR_PRISMATICJOINTDEF(rdr);
                    b2RecDispatch_CreatePrismaticJoint(in a, rdr);
                    break;
                }
                case B2RecOpcode.CreateRevoluteJoint:
                {
                    B2RecArgs_CreateRevoluteJoint a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.def = b2RecR_REVOLUTEJOINTDEF(rdr);
                    b2RecDispatch_CreateRevoluteJoint(in a, rdr);
                    break;
                }
                case B2RecOpcode.CreateWeldJoint:
                {
                    B2RecArgs_CreateWeldJoint a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.def = b2RecR_WELDJOINTDEF(rdr);
                    b2RecDispatch_CreateWeldJoint(in a, rdr);
                    break;
                }
                case B2RecOpcode.CreateWheelJoint:
                {
                    B2RecArgs_CreateWheelJoint a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.def = b2RecR_WHEELJOINTDEF(rdr);
                    b2RecDispatch_CreateWheelJoint(in a, rdr);
                    break;
                }
                case B2RecOpcode.DestroyJoint:
                {
                    B2RecArgs_DestroyJoint a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.wakeAttached = b2RecR_BOOL(rdr);
                    b2RecDispatch_DestroyJoint(in a, rdr);
                    break;
                }
                case B2RecOpcode.JointSetLocalFrameA:
                {
                    B2RecArgs_JointSetLocalFrameA a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.localFrame = b2RecR_XF(rdr);
                    b2RecDispatch_JointSetLocalFrameA(in a, rdr);
                    break;
                }
                case B2RecOpcode.JointSetLocalFrameB:
                {
                    B2RecArgs_JointSetLocalFrameB a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.localFrame = b2RecR_XF(rdr);
                    b2RecDispatch_JointSetLocalFrameB(in a, rdr);
                    break;
                }
                case B2RecOpcode.JointSetCollideConnected:
                {
                    B2RecArgs_JointSetCollideConnected a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.shouldCollide = b2RecR_BOOL(rdr);
                    b2RecDispatch_JointSetCollideConnected(in a, rdr);
                    break;
                }
                case B2RecOpcode.JointWakeBodies:
                {
                    B2RecArgs_JointWakeBodies a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    b2RecDispatch_JointWakeBodies(in a, rdr);
                    break;
                }
                case B2RecOpcode.JointSetConstraintTuning:
                {
                    B2RecArgs_JointSetConstraintTuning a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.hertz = b2RecR_F32(rdr);
                    a.dampingRatio = b2RecR_F32(rdr);
                    b2RecDispatch_JointSetConstraintTuning(in a, rdr);
                    break;
                }
                case B2RecOpcode.JointSetForceThreshold:
                {
                    B2RecArgs_JointSetForceThreshold a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.threshold = b2RecR_F32(rdr);
                    b2RecDispatch_JointSetForceThreshold(in a, rdr);
                    break;
                }
                case B2RecOpcode.JointSetTorqueThreshold:
                {
                    B2RecArgs_JointSetTorqueThreshold a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.threshold = b2RecR_F32(rdr);
                    b2RecDispatch_JointSetTorqueThreshold(in a, rdr);
                    break;
                }
                case B2RecOpcode.DistanceJointSetLength:
                {
                    B2RecArgs_DistanceJointSetLength a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.length = b2RecR_F32(rdr);
                    b2RecDispatch_DistanceJointSetLength(in a, rdr);
                    break;
                }
                case B2RecOpcode.DistanceJointEnableSpring:
                {
                    B2RecArgs_DistanceJointEnableSpring a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.enableSpring = b2RecR_BOOL(rdr);
                    b2RecDispatch_DistanceJointEnableSpring(in a, rdr);
                    break;
                }
                case B2RecOpcode.DistanceJointSetSpringForceRange:
                {
                    B2RecArgs_DistanceJointSetSpringForceRange a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.lowerForce = b2RecR_F32(rdr);
                    a.upperForce = b2RecR_F32(rdr);
                    b2RecDispatch_DistanceJointSetSpringForceRange(in a, rdr);
                    break;
                }
                case B2RecOpcode.DistanceJointSetSpringHertz:
                {
                    B2RecArgs_DistanceJointSetSpringHertz a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.hertz = b2RecR_F32(rdr);
                    b2RecDispatch_DistanceJointSetSpringHertz(in a, rdr);
                    break;
                }
                case B2RecOpcode.DistanceJointSetSpringDampingRatio:
                {
                    B2RecArgs_DistanceJointSetSpringDampingRatio a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.dampingRatio = b2RecR_F32(rdr);
                    b2RecDispatch_DistanceJointSetSpringDampingRatio(in a, rdr);
                    break;
                }
                case B2RecOpcode.DistanceJointEnableLimit:
                {
                    B2RecArgs_DistanceJointEnableLimit a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.enableLimit = b2RecR_BOOL(rdr);
                    b2RecDispatch_DistanceJointEnableLimit(in a, rdr);
                    break;
                }
                case B2RecOpcode.DistanceJointSetLengthRange:
                {
                    B2RecArgs_DistanceJointSetLengthRange a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.minLength = b2RecR_F32(rdr);
                    a.maxLength = b2RecR_F32(rdr);
                    b2RecDispatch_DistanceJointSetLengthRange(in a, rdr);
                    break;
                }
                case B2RecOpcode.DistanceJointEnableMotor:
                {
                    B2RecArgs_DistanceJointEnableMotor a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.enableMotor = b2RecR_BOOL(rdr);
                    b2RecDispatch_DistanceJointEnableMotor(in a, rdr);
                    break;
                }
                case B2RecOpcode.DistanceJointSetMotorSpeed:
                {
                    B2RecArgs_DistanceJointSetMotorSpeed a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.motorSpeed = b2RecR_F32(rdr);
                    b2RecDispatch_DistanceJointSetMotorSpeed(in a, rdr);
                    break;
                }
                case B2RecOpcode.DistanceJointSetMaxMotorForce:
                {
                    B2RecArgs_DistanceJointSetMaxMotorForce a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.force = b2RecR_F32(rdr);
                    b2RecDispatch_DistanceJointSetMaxMotorForce(in a, rdr);
                    break;
                }
                case B2RecOpcode.MotorJointSetLinearVelocity:
                {
                    B2RecArgs_MotorJointSetLinearVelocity a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.velocity = b2RecR_VEC2(rdr);
                    b2RecDispatch_MotorJointSetLinearVelocity(in a, rdr);
                    break;
                }
                case B2RecOpcode.MotorJointSetAngularVelocity:
                {
                    B2RecArgs_MotorJointSetAngularVelocity a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.velocity = b2RecR_F32(rdr);
                    b2RecDispatch_MotorJointSetAngularVelocity(in a, rdr);
                    break;
                }
                case B2RecOpcode.MotorJointSetMaxVelocityForce:
                {
                    B2RecArgs_MotorJointSetMaxVelocityForce a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.maxForce = b2RecR_F32(rdr);
                    b2RecDispatch_MotorJointSetMaxVelocityForce(in a, rdr);
                    break;
                }
                case B2RecOpcode.MotorJointSetMaxVelocityTorque:
                {
                    B2RecArgs_MotorJointSetMaxVelocityTorque a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.maxTorque = b2RecR_F32(rdr);
                    b2RecDispatch_MotorJointSetMaxVelocityTorque(in a, rdr);
                    break;
                }
                case B2RecOpcode.MotorJointSetLinearHertz:
                {
                    B2RecArgs_MotorJointSetLinearHertz a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.hertz = b2RecR_F32(rdr);
                    b2RecDispatch_MotorJointSetLinearHertz(in a, rdr);
                    break;
                }
                case B2RecOpcode.MotorJointSetLinearDampingRatio:
                {
                    B2RecArgs_MotorJointSetLinearDampingRatio a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.damping = b2RecR_F32(rdr);
                    b2RecDispatch_MotorJointSetLinearDampingRatio(in a, rdr);
                    break;
                }
                case B2RecOpcode.MotorJointSetAngularHertz:
                {
                    B2RecArgs_MotorJointSetAngularHertz a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.hertz = b2RecR_F32(rdr);
                    b2RecDispatch_MotorJointSetAngularHertz(in a, rdr);
                    break;
                }
                case B2RecOpcode.MotorJointSetAngularDampingRatio:
                {
                    B2RecArgs_MotorJointSetAngularDampingRatio a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.damping = b2RecR_F32(rdr);
                    b2RecDispatch_MotorJointSetAngularDampingRatio(in a, rdr);
                    break;
                }
                case B2RecOpcode.MotorJointSetMaxSpringForce:
                {
                    B2RecArgs_MotorJointSetMaxSpringForce a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.maxForce = b2RecR_F32(rdr);
                    b2RecDispatch_MotorJointSetMaxSpringForce(in a, rdr);
                    break;
                }
                case B2RecOpcode.MotorJointSetMaxSpringTorque:
                {
                    B2RecArgs_MotorJointSetMaxSpringTorque a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.maxTorque = b2RecR_F32(rdr);
                    b2RecDispatch_MotorJointSetMaxSpringTorque(in a, rdr);
                    break;
                }
                case B2RecOpcode.PrismaticJointEnableSpring:
                {
                    B2RecArgs_PrismaticJointEnableSpring a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.enableSpring = b2RecR_BOOL(rdr);
                    b2RecDispatch_PrismaticJointEnableSpring(in a, rdr);
                    break;
                }
                case B2RecOpcode.PrismaticJointSetSpringHertz:
                {
                    B2RecArgs_PrismaticJointSetSpringHertz a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.hertz = b2RecR_F32(rdr);
                    b2RecDispatch_PrismaticJointSetSpringHertz(in a, rdr);
                    break;
                }
                case B2RecOpcode.PrismaticJointSetSpringDampingRatio:
                {
                    B2RecArgs_PrismaticJointSetSpringDampingRatio a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.dampingRatio = b2RecR_F32(rdr);
                    b2RecDispatch_PrismaticJointSetSpringDampingRatio(in a, rdr);
                    break;
                }
                case B2RecOpcode.PrismaticJointSetTargetTranslation:
                {
                    B2RecArgs_PrismaticJointSetTargetTranslation a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.translation = b2RecR_F32(rdr);
                    b2RecDispatch_PrismaticJointSetTargetTranslation(in a, rdr);
                    break;
                }
                case B2RecOpcode.PrismaticJointEnableLimit:
                {
                    B2RecArgs_PrismaticJointEnableLimit a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.enableLimit = b2RecR_BOOL(rdr);
                    b2RecDispatch_PrismaticJointEnableLimit(in a, rdr);
                    break;
                }
                case B2RecOpcode.PrismaticJointSetLimits:
                {
                    B2RecArgs_PrismaticJointSetLimits a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.lower = b2RecR_F32(rdr);
                    a.upper = b2RecR_F32(rdr);
                    b2RecDispatch_PrismaticJointSetLimits(in a, rdr);
                    break;
                }
                case B2RecOpcode.PrismaticJointEnableMotor:
                {
                    B2RecArgs_PrismaticJointEnableMotor a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.enableMotor = b2RecR_BOOL(rdr);
                    b2RecDispatch_PrismaticJointEnableMotor(in a, rdr);
                    break;
                }
                case B2RecOpcode.PrismaticJointSetMotorSpeed:
                {
                    B2RecArgs_PrismaticJointSetMotorSpeed a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.motorSpeed = b2RecR_F32(rdr);
                    b2RecDispatch_PrismaticJointSetMotorSpeed(in a, rdr);
                    break;
                }
                case B2RecOpcode.PrismaticJointSetMaxMotorForce:
                {
                    B2RecArgs_PrismaticJointSetMaxMotorForce a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.force = b2RecR_F32(rdr);
                    b2RecDispatch_PrismaticJointSetMaxMotorForce(in a, rdr);
                    break;
                }
                case B2RecOpcode.RevoluteJointEnableSpring:
                {
                    B2RecArgs_RevoluteJointEnableSpring a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.enableSpring = b2RecR_BOOL(rdr);
                    b2RecDispatch_RevoluteJointEnableSpring(in a, rdr);
                    break;
                }
                case B2RecOpcode.RevoluteJointSetSpringHertz:
                {
                    B2RecArgs_RevoluteJointSetSpringHertz a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.hertz = b2RecR_F32(rdr);
                    b2RecDispatch_RevoluteJointSetSpringHertz(in a, rdr);
                    break;
                }
                case B2RecOpcode.RevoluteJointSetSpringDampingRatio:
                {
                    B2RecArgs_RevoluteJointSetSpringDampingRatio a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.dampingRatio = b2RecR_F32(rdr);
                    b2RecDispatch_RevoluteJointSetSpringDampingRatio(in a, rdr);
                    break;
                }
                case B2RecOpcode.RevoluteJointSetTargetAngle:
                {
                    B2RecArgs_RevoluteJointSetTargetAngle a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.angle = b2RecR_F32(rdr);
                    b2RecDispatch_RevoluteJointSetTargetAngle(in a, rdr);
                    break;
                }
                case B2RecOpcode.RevoluteJointEnableLimit:
                {
                    B2RecArgs_RevoluteJointEnableLimit a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.enableLimit = b2RecR_BOOL(rdr);
                    b2RecDispatch_RevoluteJointEnableLimit(in a, rdr);
                    break;
                }
                case B2RecOpcode.RevoluteJointSetLimits:
                {
                    B2RecArgs_RevoluteJointSetLimits a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.lower = b2RecR_F32(rdr);
                    a.upper = b2RecR_F32(rdr);
                    b2RecDispatch_RevoluteJointSetLimits(in a, rdr);
                    break;
                }
                case B2RecOpcode.RevoluteJointEnableMotor:
                {
                    B2RecArgs_RevoluteJointEnableMotor a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.enableMotor = b2RecR_BOOL(rdr);
                    b2RecDispatch_RevoluteJointEnableMotor(in a, rdr);
                    break;
                }
                case B2RecOpcode.RevoluteJointSetMotorSpeed:
                {
                    B2RecArgs_RevoluteJointSetMotorSpeed a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.motorSpeed = b2RecR_F32(rdr);
                    b2RecDispatch_RevoluteJointSetMotorSpeed(in a, rdr);
                    break;
                }
                case B2RecOpcode.RevoluteJointSetMaxMotorTorque:
                {
                    B2RecArgs_RevoluteJointSetMaxMotorTorque a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.torque = b2RecR_F32(rdr);
                    b2RecDispatch_RevoluteJointSetMaxMotorTorque(in a, rdr);
                    break;
                }
                case B2RecOpcode.WeldJointSetLinearHertz:
                {
                    B2RecArgs_WeldJointSetLinearHertz a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.hertz = b2RecR_F32(rdr);
                    b2RecDispatch_WeldJointSetLinearHertz(in a, rdr);
                    break;
                }
                case B2RecOpcode.WeldJointSetLinearDampingRatio:
                {
                    B2RecArgs_WeldJointSetLinearDampingRatio a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.dampingRatio = b2RecR_F32(rdr);
                    b2RecDispatch_WeldJointSetLinearDampingRatio(in a, rdr);
                    break;
                }
                case B2RecOpcode.WeldJointSetAngularHertz:
                {
                    B2RecArgs_WeldJointSetAngularHertz a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.hertz = b2RecR_F32(rdr);
                    b2RecDispatch_WeldJointSetAngularHertz(in a, rdr);
                    break;
                }
                case B2RecOpcode.WeldJointSetAngularDampingRatio:
                {
                    B2RecArgs_WeldJointSetAngularDampingRatio a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.dampingRatio = b2RecR_F32(rdr);
                    b2RecDispatch_WeldJointSetAngularDampingRatio(in a, rdr);
                    break;
                }
                case B2RecOpcode.WheelJointEnableSpring:
                {
                    B2RecArgs_WheelJointEnableSpring a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.enableSpring = b2RecR_BOOL(rdr);
                    b2RecDispatch_WheelJointEnableSpring(in a, rdr);
                    break;
                }
                case B2RecOpcode.WheelJointSetSpringHertz:
                {
                    B2RecArgs_WheelJointSetSpringHertz a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.hertz = b2RecR_F32(rdr);
                    b2RecDispatch_WheelJointSetSpringHertz(in a, rdr);
                    break;
                }
                case B2RecOpcode.WheelJointSetSpringDampingRatio:
                {
                    B2RecArgs_WheelJointSetSpringDampingRatio a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.dampingRatio = b2RecR_F32(rdr);
                    b2RecDispatch_WheelJointSetSpringDampingRatio(in a, rdr);
                    break;
                }
                case B2RecOpcode.WheelJointEnableLimit:
                {
                    B2RecArgs_WheelJointEnableLimit a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.enableLimit = b2RecR_BOOL(rdr);
                    b2RecDispatch_WheelJointEnableLimit(in a, rdr);
                    break;
                }
                case B2RecOpcode.WheelJointSetLimits:
                {
                    B2RecArgs_WheelJointSetLimits a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.lower = b2RecR_F32(rdr);
                    a.upper = b2RecR_F32(rdr);
                    b2RecDispatch_WheelJointSetLimits(in a, rdr);
                    break;
                }
                case B2RecOpcode.WheelJointEnableMotor:
                {
                    B2RecArgs_WheelJointEnableMotor a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.enableMotor = b2RecR_BOOL(rdr);
                    b2RecDispatch_WheelJointEnableMotor(in a, rdr);
                    break;
                }
                case B2RecOpcode.WheelJointSetMotorSpeed:
                {
                    B2RecArgs_WheelJointSetMotorSpeed a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.motorSpeed = b2RecR_F32(rdr);
                    b2RecDispatch_WheelJointSetMotorSpeed(in a, rdr);
                    break;
                }
                case B2RecOpcode.WheelJointSetMaxMotorTorque:
                {
                    B2RecArgs_WheelJointSetMaxMotorTorque a = default;
                    a.joint = b2RecR_JOINTID(rdr);
                    a.torque = b2RecR_F32(rdr);
                    b2RecDispatch_WheelJointSetMaxMotorTorque(in a, rdr);
                    break;
                }
                case B2RecOpcode.QueryOverlapAABB:
                {
                    B2RecArgs_QueryOverlapAABB a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.aabb = b2RecR_AABB(rdr);
                    a.filter = b2RecR_QUERYFILTER(rdr);
                    b2RecDispatch_QueryOverlapAABB(in a, rdr);
                    break;
                }
                case B2RecOpcode.QueryOverlapShape:
                {
                    B2RecArgs_QueryOverlapShape a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.proxy = b2RecR_SHAPEPROXY(rdr);
                    a.filter = b2RecR_QUERYFILTER(rdr);
                    b2RecDispatch_QueryOverlapShape(in a, rdr);
                    break;
                }
                case B2RecOpcode.QueryCastRay:
                {
                    B2RecArgs_QueryCastRay a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.origin = b2RecR_VEC2(rdr);
                    a.translation = b2RecR_VEC2(rdr);
                    a.filter = b2RecR_QUERYFILTER(rdr);
                    b2RecDispatch_QueryCastRay(in a, rdr);
                    break;
                }
                case B2RecOpcode.QueryCastShape:
                {
                    B2RecArgs_QueryCastShape a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.proxy = b2RecR_SHAPEPROXY(rdr);
                    a.translation = b2RecR_VEC2(rdr);
                    a.filter = b2RecR_QUERYFILTER(rdr);
                    b2RecDispatch_QueryCastShape(in a, rdr);
                    break;
                }
                case B2RecOpcode.QueryCollideMover:
                {
                    B2RecArgs_QueryCollideMover a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.mover = b2RecR_CAPSULE(rdr);
                    a.filter = b2RecR_QUERYFILTER(rdr);
                    b2RecDispatch_QueryCollideMover(in a, rdr);
                    break;
                }
                case B2RecOpcode.QueryCastRayClosest:
                {
                    B2RecArgs_QueryCastRayClosest a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.origin = b2RecR_VEC2(rdr);
                    a.translation = b2RecR_VEC2(rdr);
                    a.filter = b2RecR_QUERYFILTER(rdr);
                    b2RecDispatch_QueryCastRayClosest(in a, rdr);
                    break;
                }
                case B2RecOpcode.QueryCastMover:
                {
                    B2RecArgs_QueryCastMover a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.mover = b2RecR_CAPSULE(rdr);
                    a.translation = b2RecR_VEC2(rdr);
                    a.filter = b2RecR_QUERYFILTER(rdr);
                    b2RecDispatch_QueryCastMover(in a, rdr);
                    break;
                }
                case B2RecOpcode.ShapeTestPoint:
                {
                    B2RecArgs_ShapeTestPoint a = default;
                    a.shape = b2RecR_SHAPEID(rdr);
                    a.point = b2RecR_VEC2(rdr);
                    b2RecDispatch_ShapeTestPoint(in a, rdr);
                    break;
                }
                case B2RecOpcode.ShapeRayCast:
                {
                    B2RecArgs_ShapeRayCast a = default;
                    a.shape = b2RecR_SHAPEID(rdr);
                    a.input = b2RecR_RAYCASTINPUT(rdr);
                    b2RecDispatch_ShapeRayCast(in a, rdr);
                    break;
                }
                case B2RecOpcode.StateHash:
                {
                    B2RecArgs_StateHash a = default;
                    a.world = b2RecR_WORLDID(rdr);
                    a.hash = b2RecR_U64(rdr);
                    b2RecDispatch_StateHash(in a, rdr);
                    break;
                }
                default:
                    Console.WriteLine("b2ReplayFile: unknown opcode 0x{0:X2}, skipping {1} bytes", opcode, payloadSize);
                    // payloadStart is in bounds, so size - payloadStart is the bytes left to skip over
                    if (payloadSize > (uint)(rdr.size - payloadStart))
                    {
                        rdr.ok = false;
                    }
                    else
                    {
                        rdr.cursor = payloadStart + (int)payloadSize;
                    }
                    break;
            }
            return opcode;
        }

        // Dispatch records until the CreateWorld record has produced a valid world.
        private static void b2RecPumpToWorld(B2RecPlayer player)
        {
            while (!b2World_IsValid(player.rdr.replayWorldId))
            {
                if (b2RecDispatchOne(player) < 0)
                {
                    break;
                }
            }
        }

        // Walk the records once without dispatching to count steps and read the first step's tuning.
        // The framing is opcode u8 + payload u24 + payload, so we can skip records blind.
        private static void b2RecScanFile(B2RecPlayer player)
        {
            byte[] data = player.data;
            int size = player.size;
            int cursor = player.headerEnd;
            int frameCount = 0;
            bool gotStep = false;
            while (cursor + 4 <= size)
            {
                byte opcode = data[cursor];
                uint payloadSize = (uint)data[cursor + 1] | (uint)data[cursor + 2] << 8 | (uint)data[cursor + 3] << 16;
                int payloadStart = cursor + 4;
                if (payloadStart + (int)payloadSize > size)
                {
                    break;
                }

                if (opcode == (byte)B2RecOpcode.Step) // Step: [u32 world][f32 dt][i32 subStepCount]
                {
                    frameCount += 1;
                    if (!gotStep && payloadSize >= 12)
                    {
                        uint dtBits = (uint)data[payloadStart + 4] | (uint)data[payloadStart + 5] << 8 |
                                      (uint)data[payloadStart + 6] << 16 | (uint)data[payloadStart + 7] << 24;
                        player.recordedDt = BitConverter.Int32BitsToSingle(unchecked((int)dtBits));
                        player.recordedSubStepCount = data[payloadStart + 8] | data[payloadStart + 9] << 8 |
                                                      data[payloadStart + 10] << 16 | data[payloadStart + 11] << 24;
                        gotStep = true;
                    }
                }
                cursor = payloadStart + (int)payloadSize;
            }
            player.frameCount = frameCount;
        }

        /// Open a replay file for incremental playback and replay up to the first step.
        /// @param path Path to the recording file
        /// @param workerCount Worker count for the replay world. 0 uses the recorded count.
        /// @return A player handle, or NULL if the file is missing or malformed
        public static B2RecPlayer b2RecPlayer_Create(string path, int workerCount)
        {
            if (path == null)
            {
                Console.WriteLine("b2ReplayFile: path is NULL");
                return null;
            }

            byte[] data;
            try { data = File.ReadAllBytes(path); }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                Console.WriteLine("b2ReplayFile: cannot open '{0}'", path);
                return null;
            }

            // Validate header
            if (data.Length < 32)
            {
                Console.WriteLine("b2ReplayFile: file too small");
                return null;
            }

            B2RecReader header = new B2RecReader();
            header.data = data;
            header.size = 32;
            header.cursor = 0;
            header.ok = true;
            B2RecHeader hdr = new B2RecHeader();
            hdr.magic = b2RecR_U32(header);
            hdr.versionMajor = b2RecR_U16(header);
            hdr.versionMinor = b2RecR_U16(header);
            hdr.buildHash = b2RecR_U32(header);
            hdr.simdWidth = b2RecR_U8(header);
            hdr.pointerWidth = b2RecR_U8(header);
            hdr.bigEndian = b2RecR_U8(header);
            hdr.reserved0 = b2RecR_U8(header);
            hdr.wallClockUnix = b2RecR_U64(header);
            hdr.reserved = b2RecR_U64(header);

            if (hdr.magic != B2_REC_MAGIC)
            {
                Console.WriteLine("b2ReplayFile: bad magic (got 0x{0:X8})", hdr.magic);
                return null;
            }
            if (hdr.versionMajor != 1)
            {
                Console.WriteLine("b2ReplayFile: version mismatch (file={0}, runtime=1)", hdr.versionMajor);
                return null;
            }
            if (hdr.pointerWidth != IntPtr.Size)
            {
                Console.WriteLine("b2ReplayFile: pointer width mismatch (file={0}, runtime={1})", hdr.pointerWidth, IntPtr.Size);
                return null;
            }
            if (hdr.bigEndian != 0)
            {
                Console.WriteLine("b2ReplayFile: big-endian recording not supported");
                return null;
            }

            B2RecPlayer player = new B2RecPlayer();
            player.data = data;
            player.size = data.Length;
            player.headerEnd = 32;
            player.buildHash = hdr.buildHash;
            player.wallClock = hdr.wallClockUnix;
            player.frame = 0;
            player.frameCount = 0;
            player.recordedWorkerCount = 0;
            player.recordedDt = 0.0f;
            player.recordedSubStepCount = 0;
            player.divergeFrame = -1;
            player.atEnd = false;
            player.rdr = new B2RecReader();
            player.rdr.data = data;
            player.rdr.size = data.Length;
            player.rdr.cursor = 32; // past header
            player.rdr.replayWorldId = b2_nullWorldId;
            player.rdr.workerCount = workerCount;
            player.rdr.ok = true;
            player.rdr.diverged = false;
            player.rdr.chainPoints = null;
            player.rdr.chainPointCap = 0;
            player.rdr.chainMaterials = null;
            player.rdr.chainMaterialCap = 0;
            player.rdr.hits = null;
            player.rdr.hitCap = 0;
            player.rdr.owner = player;
            player.frameQueries = null;
            player.frameQueryCount = 0;
            player.frameQueryCap = 0;
            player.frameHits = null;
            player.frameHitCount = 0;
            player.frameHitCap = 0;
            player.bodyIds = null;
            player.bodyIdCount = 0;
            player.bodyIdCap = 0;

            // Count steps and read the first step's tuning so the viewer can show length and hz up front
            b2RecScanFile(player);

            // The first record is CreateWorld; replay it so the world exists before the first step
            b2RecPumpToWorld(player);
            if (!b2World_IsValid(player.rdr.replayWorldId))
            {
                Console.WriteLine("b2ReplayFile: no CreateWorld record");
                b2RecPlayer_Destroy(player);
                return null;
            }
            return player;
        }

        /// Advance the replay by one recorded step.
        /// @return true if a step executed, false once the end of the recording is reached
        public static bool b2RecPlayer_StepFrame(B2RecPlayer player)
        {
            if (player.atEnd)
            {
                return false;
            }

            // Reset per-frame query store before dispatching new records
            player.frameQueryCount = 0;
            player.frameHitCount = 0;

            // Run this frame's Step, then consume the records that trail it (StateHash, queries, any
            // between-frame mutators) up to the next Step. The queries and hash for a frame are recorded
            // after its Step, so grouping them with that Step keeps them paired with the world state they
            // were computed against. Stopping before the next Step is what advances exactly one frame.
            bool stepped = false;
            for (;;)
            {
                // Peek the next opcode without consuming it. The next frame's Step ends this frame.
                if (player.rdr.cursor >= player.rdr.size || !player.rdr.ok)
                {
                    player.atEnd = true;
                    return stepped;
                }
                if (stepped && player.rdr.data[player.rdr.cursor] == (byte)B2RecOpcode.Step)
                {
                    return true;
                }

                int opcode = b2RecDispatchOne(player);
                if (opcode < 0)
                {
                    player.atEnd = true;
                    return stepped;
                }
                if (opcode == (byte)B2RecOpcode.Step)
                {
                    player.frame += 1;
                    stepped = true;
                }
                // Latch the first frame that diverged for the timeline marker
                if (player.divergeFrame < 0 && player.rdr.diverged)
                {
                    player.divergeFrame = player.frame;
                }
            }
        }

        /// Rewind the player to the first step, recreating the replay world from the file.
        public static void b2RecPlayer_Restart(B2RecPlayer player)
        {
            if (b2World_IsValid(player.rdr.replayWorldId))
            {
                b2DestroyWorld(player.rdr.replayWorldId);
            }
            player.rdr.cursor = player.headerEnd;
            player.rdr.replayWorldId = b2_nullWorldId;
            player.rdr.ok = true;
            player.rdr.diverged = false;
            player.frame = 0;
            player.divergeFrame = -1;
            player.atEnd = false;
            player.bodyIdCount = 0; // rebuilt deterministically as the replay re-runs
            b2RecPumpToWorld(player);
        }

        /// Get the id of the replayed world.
        public static B2WorldId b2RecPlayer_GetWorldId(B2RecPlayer player)
        {
            return player != null ? player.rdr.replayWorldId : b2_nullWorldId;
        }

        /// Get the number of steps replayed so far.
        public static int b2RecPlayer_GetFrame(B2RecPlayer player)
        {
            return player != null ? player.frame : 0;
        }

        /// Seek to a recorded step. Seeking backward rewinds and re-runs from the start, so the
        /// cost grows with the target frame. Clamps to the recording bounds.
        public static void b2RecPlayer_SeekFrame(B2RecPlayer player, int targetFrame)
        {
            if (player == null)
            {
                return;
            }
            if (targetFrame < 0)
            {
                targetFrame = 0;
            }

            // Backward seek has to rewind and replay from the start
            if (targetFrame < player.frame)
            {
                b2RecPlayer_Restart(player);
            }
            while (player.frame < targetFrame && b2RecPlayer_StepFrame(player))
            {
            }
        }

        /// Get static metadata for the recording (frame count, recorded tuning, build, time).
        public static B2RecPlayerInfo b2RecPlayer_GetInfo(B2RecPlayer player)
        {
            B2RecPlayerInfo info = new B2RecPlayerInfo();
            if (player != null)
            {
                info.frameCount = player.frameCount;
                info.workerCount = player.recordedWorkerCount;
                info.timeStep = player.recordedDt;
                info.subStepCount = player.recordedSubStepCount;
                info.buildHash = player.buildHash;
                info.wallClock = player.wallClock;
            }
            return info;
        }

        /// Get the engine build hash recorded in the file, or 0 if unstamped. Compare with
        /// b2GetBuildHash to tell whether the file was made by a different build.
        public static uint b2RecPlayer_GetBuildHash(B2RecPlayer player)
        {
            return player != null ? player.buildHash : 0;
        }

        /// Returns true once the end of the recording has been reached.
        public static bool b2RecPlayer_IsAtEnd(B2RecPlayer player)
        {
            return player == null || player.atEnd;
        }

        /// Returns true if a recorded state hash failed to reproduce, meaning replay diverged.
        public static bool b2RecPlayer_HasDiverged(B2RecPlayer player)
        {
            return player != null && player.rdr.diverged;
        }

        /// Get the first step at which replay diverged, or -1 if it has not diverged.
        public static int b2RecPlayer_GetDivergeFrame(B2RecPlayer player)
        {
            return player != null ? player.divergeFrame : -1;
        }

        /// Close a player and free its replay world and file buffer.
        public static void b2RecPlayer_Destroy(B2RecPlayer player)
        {
            if (player == null)
            {
                return;
            }
            if (b2World_IsValid(player.rdr.replayWorldId))
            {
                b2DestroyWorld(player.rdr.replayWorldId);
            }
            player.rdr.replayWorldId = b2_nullWorldId;
            player.data = null;
            player.rdr.data = null;
            player.rdr.size = 0;
            player.atEnd = true;
            player.rdr.chainPoints = null;
            player.rdr.chainPointCap = 0;
            player.rdr.chainMaterials = null;
            player.rdr.chainMaterialCap = 0;
            player.rdr.hits = null;
            player.rdr.hitCap = 0;
            player.frameQueries = null;
            player.frameQueryCount = 0;
            player.frameQueryCap = 0;
            player.frameHits = null;
            player.frameHitCount = 0;
            player.frameHitCap = 0;
            player.bodyIds = null;
            player.bodyIdCount = 0;
            player.bodyIdCap = 0;
        }

        // Highlight each reported overlap shape by its AABB. Skip any destroyed since the query,
        // per the b2Shape_GetAABB contract that overlap results may contain stale shapes.
        private static void b2RecDrawHitAABBs(B2RecPlayer player, in B2RecDrawQuery q, in B2DebugDraw draw)
        {
            if (draw.DrawPolygonFcn == null)
            {
                return;
            }

            for (int hi = q.hitStart; hi < q.hitStart + q.hitCount; ++hi)
            {
                B2ShapeId id = player.frameHits[hi].id;
                if (b2Shape_IsValid(id) == false)
                {
                    continue;
                }

                B2AABB b = b2Shape_GetAABB(id);
                B2Vec2[] vs =
                {
                    b.lowerBound,
                    new B2Vec2(b.upperBound.X, b.lowerBound.Y),
                    b.upperBound,
                    new B2Vec2(b.lowerBound.X, b.upperBound.Y),
                };
                draw.DrawPolygonFcn(vs, 4, B2HexColor.b2_colorMagenta, draw.context);
            }
        }

        /// Draw spatial queries recorded during the most recently replayed frame.
        /// Call after b2World_Draw so queries are layered on top of the world.
        /// @param player A valid player handle
        /// @param draw Debug draw callbacks. NULL draw function pointers are skipped.
        /// In C#, null delegate fields are skipped.
        /// @param queryIndex Index into the frame's queries to draw, or -1 to draw all of them.
        public static void b2RecPlayer_DrawFrameQueries(B2RecPlayer player, in B2DebugDraw draw, int queryIndex)
        {
            if (player == null)
            {
                return;
            }

            // queryIndex < 0 draws all queries, otherwise just the one selected in the viewer
            for (int qi = 0; qi < player.frameQueryCount; ++qi)
            {
                if (queryIndex >= 0 && qi != queryIndex)
                {
                    continue;
                }

                B2RecDrawQuery q = player.frameQueries[qi];

                switch (q.kind)
                {
                    case (int)B2RecQueryKind.B2_RECQ_CAST_RAY:
                    case (int)B2RecQueryKind.B2_RECQ_CAST_RAY_CLOSEST:
                    {
                        // Ray origin to endpoint
                        B2Vec2 end = b2Add(q.origin, q.translation);
                        if (draw.DrawLineFcn != null)
                        {
                            draw.DrawLineFcn(q.origin, end, B2HexColor.b2_colorYellow, draw.context);
                        }

                        // Per-hit point + short normal
                        for (int hi = q.hitStart; hi < q.hitStart + q.hitCount; ++hi)
                        {
                            B2RecRecordedHit h = player.frameHits[hi];
                            if (draw.DrawPointFcn != null)
                            {
                                draw.DrawPointFcn(h.point, 4.0f, B2HexColor.b2_colorYellow, draw.context);
                            }

                            if (draw.DrawLineFcn != null)
                            {
                                B2Vec2 np = b2MulAdd(h.point, 0.2f, h.normal);
                                draw.DrawLineFcn(h.point, np, B2HexColor.b2_colorLightYellow, draw.context);
                            }
                        }
                        break;
                    }

                    case (int)B2RecQueryKind.B2_RECQ_CAST_SHAPE:
                    {
                        // Shape cast: draw per-hit points along the swept path
                        for (int hi = q.hitStart; hi < q.hitStart + q.hitCount; ++hi)
                        {
                            B2RecRecordedHit h = player.frameHits[hi];
                            if (draw.DrawPointFcn != null)
                            {
                                draw.DrawPointFcn(h.point, 4.0f, B2HexColor.b2_colorSkyBlue, draw.context);
                            }

                            if (draw.DrawLineFcn != null)
                            {
                                B2Vec2 np = b2MulAdd(h.point, 0.2f, h.normal);
                                draw.DrawLineFcn(h.point, np, B2HexColor.b2_colorLightSkyBlue, draw.context);
                            }
                        }
                        break;
                    }

                    case (int)B2RecQueryKind.B2_RECQ_CAST_MOVER:
                    {
                        if (draw.DrawSolidCapsuleFcn != null)
                        {
                            draw.DrawSolidCapsuleFcn(q.mover.center1, q.mover.center2, q.mover.radius,
                                B2HexColor.b2_colorLightSkyBlue, draw.context);
                        }
                        break;
                    }

                    case (int)B2RecQueryKind.B2_RECQ_OVERLAP_AABB:
                    {
                        B2AABB aabb = q.aabb;
                        B2Vec2[] vs =
                        {
                            aabb.lowerBound,
                            new B2Vec2(aabb.upperBound.X, aabb.lowerBound.Y),
                            aabb.upperBound,
                            new B2Vec2(aabb.lowerBound.X, aabb.upperBound.Y),
                        };
                        if (draw.DrawPolygonFcn != null)
                        {
                            draw.DrawPolygonFcn(vs, 4, B2HexColor.b2_colorLimeGreen, draw.context);
                        }
                        b2RecDrawHitAABBs(player, q, draw);
                        break;
                    }

                    case (int)B2RecQueryKind.B2_RECQ_OVERLAP_SHAPE:
                    {
                        if (q.proxy.count == 1)
                        {
                            if (draw.DrawCircleFcn != null)
                            {
                                draw.DrawCircleFcn(q.proxy.points[0], q.proxy.radius, B2HexColor.b2_colorLimeGreen, draw.context);
                            }
                        }
                        else if (q.proxy.count >= 2 && draw.DrawPolygonFcn != null)
                        {
                            draw.DrawPolygonFcn(q.proxy.points.AsReadOnlySpan(), q.proxy.count, B2HexColor.b2_colorLimeGreen, draw.context);
                        }
                        b2RecDrawHitAABBs(player, q, draw);
                        break;
                    }

                    case (int)B2RecQueryKind.B2_RECQ_COLLIDE_MOVER:
                    {
                        if (draw.DrawSolidCapsuleFcn != null)
                        {
                            draw.DrawSolidCapsuleFcn(q.mover.center1, q.mover.center2, q.mover.radius,
                                B2HexColor.b2_colorTan, draw.context);
                        }

                        // Per-hit plane point and normal
                        for (int hi = q.hitStart; hi < q.hitStart + q.hitCount; ++hi)
                        {
                            B2PlaneResult plane = player.frameHits[hi].plane;
                            if (plane.hit && draw.DrawLineFcn != null)
                            {
                                B2Vec2 np = b2MulAdd(plane.point, 0.2f, plane.plane.normal);
                                draw.DrawLineFcn(plane.point, np, B2HexColor.b2_colorOrange, draw.context);
                            }
                        }
                        break;
                    }

                    case (int)B2RecQueryKind.B2_RECQ_SHAPE_TEST_POINT:
                    {
                        B2HexColor color = q.boolResult ? B2HexColor.b2_colorAqua : B2HexColor.b2_colorRed;
                        if (draw.DrawPointFcn != null)
                        {
                            draw.DrawPointFcn(q.origin, 6.0f, color, draw.context);
                        }
                        break;
                    }

                    case (int)B2RecQueryKind.B2_RECQ_SHAPE_RAY_CAST:
                    {
                        B2Vec2 end = b2Add(q.origin, q.translation);
                        if (draw.DrawLineFcn != null)
                        {
                            draw.DrawLineFcn(q.origin, end, B2HexColor.b2_colorViolet, draw.context);
                        }
                        if (q.castOut.hit && draw.DrawPointFcn != null)
                        {
                            draw.DrawPointFcn(q.castOut.point, 4.0f, B2HexColor.b2_colorViolet, draw.context);
                        }
                        break;
                    }
                    default:
                        break;
                }
            }
        }

        // Public query inspection. The internal b2RecQueryKind values match the public b2RecQueryType, so
        // the kind copies across as a plain cast. Pin the first and last kinds to catch enum drift.

        /// Get the number of spatial queries recorded for the most recently replayed frame.
        public static int b2RecPlayer_GetFrameQueryCount(B2RecPlayer player)
        {
            return player != null ? player.frameQueryCount : 0;
        }

        /// Get a recorded query from the most recently replayed frame by index.
        public static B2RecQueryInfo b2RecPlayer_GetFrameQuery(B2RecPlayer player, int index)
        {
            B2RecQueryInfo info = new B2RecQueryInfo();
            if (player == null || index < 0 || index >= player.frameQueryCount)
            {
                return info;
            }

            B2RecDrawQuery query = player.frameQueries[index];
            info.type = (B2RecQueryType)query.kind;
            info.filter = query.filter;
            info.aabb = query.aabb;
            info.origin = query.origin;
            info.translation = query.translation;
            info.shape = query.shape;
            info.hitCount = query.hitCount;
            return info;
        }

        /// Get one result of a recorded query from the most recently replayed frame.
        public static B2RecQueryHit b2RecPlayer_GetFrameQueryHit(B2RecPlayer player, int queryIndex, int hitIndex)
        {
            B2RecQueryHit hit = new B2RecQueryHit();
            if (player == null || queryIndex < 0 || queryIndex >= player.frameQueryCount)
            {
                return hit;
            }

            B2RecDrawQuery query = player.frameQueries[queryIndex];
            if (hitIndex < 0 || hitIndex >= query.hitCount)
            {
                return hit;
            }

            B2RecRecordedHit recordedHit = player.frameHits[query.hitStart + hitIndex];
            hit.shape = recordedHit.id;
            hit.point = recordedHit.point;
            hit.normal = recordedHit.normal;
            hit.fraction = recordedHit.fraction;
            return hit;
        }

        /// Get the number of body slots tracked for the outliner. This is the creation-order span and
        /// includes holes for destroyed bodies, so it only grows as the replay advances.
        public static int b2RecPlayer_GetBodyCount(B2RecPlayer player)
        {
            return player != null ? player.bodyIdCount : 0;
        }

        /// Get a tracked body by creation ordinal. Returns b2_nullBodyId for a destroyed slot, an ordinal not
        /// yet reached at the current frame, or an out-of-range index. Validate with b2Body_IsValid.
        public static B2BodyId b2RecPlayer_GetBodyId(B2RecPlayer player, int index)
        {
            return player != null && 0 <= index && index < player.bodyIdCount ? player.bodyIds[index] : b2_nullBodyId;
        }

        /// Replay a file by re-running the engine and asserting recorded ids and state match.
        /// @param path Path to the recording file
        /// @param workerCount Worker count to use for replay. 0 uses the recorded count.
        /// @return true if replay completed without divergence, false on any mismatch
        public static bool b2ValidateReplayFile(string path, int workerCount)
        {
            B2RecPlayer player = b2RecPlayer_Create(path, workerCount);
            if (player == null)
            {
                return false;
            }

            while (b2RecPlayer_StepFrame(player))
            {
                if (b2RecPlayer_HasDiverged(player))
                {
                    break;
                }
            }
            bool valid = player.rdr.ok && !player.rdr.diverged;
            b2RecPlayer_Destroy(player);
            return valid;
        }

        /** @} */
    }
}
