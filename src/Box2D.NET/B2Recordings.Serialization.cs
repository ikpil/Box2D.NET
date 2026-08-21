// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;
using System.Text;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Cores;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Types;

namespace Box2D.NET
{
    public static partial class B2Recordings
    {
        // Magic value 'B2RC' in little-endian: bytes B2, R, C yield 0x43523242
        private const uint B2_REC_MAGIC = 0x43523242u;

        // Low level buffer helpers
        // Buffer helpers

        internal static void b2RecBufAppend(ref B2RecBuffer buf, ReadOnlySpan<byte> data, int size)
        {
            if (size <= 0)
            {
                return;
            }

            B2_ASSERT(size <= data.Length);

            int requiredCapacity = checked(buf.size + size);
            if (requiredCapacity > buf.capacity)
            {
                int newCapacity = checked(buf.capacity * 2);
                int minimumCapacity = checked(requiredCapacity + 64);
                if (newCapacity < minimumCapacity)
                {
                    newCapacity = minimumCapacity;
                }

                Array.Resize(ref buf.data, newCapacity);
                buf.capacity = newCapacity;
            }

            data.Slice(0, size).CopyTo(buf.data.AsSpan(buf.size, size));
            buf.size += size;
        }

        internal static void b2RecBufFree(ref B2RecBuffer buf)
        {
            if (buf.data != null)
            {
                buf.data = null;
                buf.capacity = 0;
                buf.size = 0;
            }
        }

        // Patch helpers for the query hit-count backfill
        // Patch helpers for query hit-count backfill

        internal static int b2RecReserveU32(ref B2RecBuffer buf)
        {
            int offset = buf.size;
            Span<byte> zero = stackalloc byte[4] { 0, 0, 0, 0 };
            b2RecBufAppend(ref buf, zero, 4);
            return offset;
        }

        internal static void b2RecPatchU32(ref B2RecBuffer buf, int offset, uint v)
        {
            B2_ASSERT(offset >= 0 && offset + 4 <= buf.size);

            byte[] p = buf.data;
            p[offset] = (byte)v;
            p[offset + 1] = (byte)(v >> 8);
            p[offset + 2] = (byte)(v >> 16);
            p[offset + 3] = (byte)(v >> 24);
        }

        // Commit a finished query record under the lock. The local buffer is still owned by the caller.
        // Concurrent query commits are serialized so records never interleave in the shared buffer
        internal static void b2RecCommitRecord(B2Recording rec, B2RecOpcode opcode, in B2RecBuffer payload)
        {
            int payloadSize = payload.size;
            B2_ASSERT(payloadSize >= 0 && payloadSize < 1 << 24);

            lock (rec.@lock)
            {
                b2RecW_U8(ref rec.buffer, (byte)opcode);
                Span<byte> size = stackalloc byte[3]
                {
                    (byte)payloadSize, (byte)(payloadSize >> 8), (byte)(payloadSize >> 16),
                };
                b2RecBufAppend(ref rec.buffer, size, 3);
                b2RecBufAppend(ref rec.buffer, payload.data, payloadSize);
            }
        }

        internal static void b2RecQueryBegin(ref B2RecQueryWriter w, object context)
        {
            w.buf = default;
            w.userFcn.overlapFcn = null;
            w.userContext = context;
            w.hitCount = 0;
            w.countOffset = 0;
        }

        internal static void b2RecQueryCommit(B2Recording rec, B2RecOpcode opcode, ref B2RecQueryWriter w)
        {
            b2RecCommitRecord(rec, opcode, w.buf);
            b2RecBufFree(ref w.buf);
        }

        // Record framing

        internal static void b2RecBeginRecord(B2Recording rec, B2RecOpcode opcode)
        {
            b2RecW_U8(ref rec.buffer, (byte)opcode);
            rec.recordStart = rec.buffer.size;

            // Placeholder 3 bytes for the u24 payload size (backpatched in b2RecEndRecord)
            Span<byte> zero = stackalloc byte[3] { 0, 0, 0 };
            b2RecBufAppend(ref rec.buffer, zero, 3);
        }

        internal static void b2RecEndRecord(B2Recording rec)
        {
            int payloadSize = rec.buffer.size - rec.recordStart - 3;
            B2_ASSERT(payloadSize >= 0 && payloadSize < 1 << 24);

            byte[] p = rec.buffer.data;
            p[rec.recordStart] = (byte)payloadSize;
            p[rec.recordStart + 1] = (byte)(payloadSize >> 8);
            p[rec.recordStart + 2] = (byte)(payloadSize >> 16);
        }

        internal static void b2FlushRecording(B2Recording recording)
        {
            if (recording.buffer.size > 0)
            {
                recording.file.Write(recording.buffer.data.AsSpan(0, recording.buffer.size));
                recording.buffer.size = 0;
            }
        }

        // Write primitives

        internal static void b2RecW_U8(ref B2RecBuffer buf, byte v)
        {
            Span<byte> b = stackalloc byte[1] { v };
            b2RecBufAppend(ref buf, b, 1);
        }

        internal static void b2RecW_U16(ref B2RecBuffer buf, ushort v)
        {
            Span<byte> b = stackalloc byte[2] { (byte)v, (byte)(v >> 8) };
            b2RecBufAppend(ref buf, b, 2);
        }

        internal static void b2RecW_U32(ref B2RecBuffer buf, uint v)
        {
            Span<byte> b = stackalloc byte[4]
            {
                (byte)v, (byte)(v >> 8), (byte)(v >> 16), (byte)(v >> 24),
            };
            b2RecBufAppend(ref buf, b, 4);
        }

        internal static void b2RecW_U64(ref B2RecBuffer buf, ulong v)
        {
            Span<byte> b = stackalloc byte[8]
            {
                (byte)v, (byte)(v >> 8), (byte)(v >> 16), (byte)(v >> 24),
                (byte)(v >> 32), (byte)(v >> 40), (byte)(v >> 48), (byte)(v >> 56),
            };
            b2RecBufAppend(ref buf, b, 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void b2RecW_I32(ref B2RecBuffer buf, int v)
        {
            b2RecW_U32(ref buf, unchecked((uint)v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void b2RecW_F32(ref B2RecBuffer buf, float v)
        {
            uint bits = unchecked((uint)BitConverter.SingleToInt32Bits(v));
            b2RecW_U32(ref buf, bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void b2RecW_BOOL(ref B2RecBuffer buf, bool v)
        {
            b2RecW_U8(ref buf, v ? (byte)1 : (byte)0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void b2RecW_VEC2(ref B2RecBuffer buf, B2Vec2 v)
        {
            b2RecW_F32(ref buf, v.X);
            b2RecW_F32(ref buf, v.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void b2RecW_ROT(ref B2RecBuffer buf, B2Rot v)
        {
            b2RecW_F32(ref buf, v.c);
            b2RecW_F32(ref buf, v.s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void b2RecW_XF(ref B2RecBuffer buf, B2Transform v)
        {
            b2RecW_VEC2(ref buf, v.p);
            b2RecW_ROT(ref buf, v.q);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void b2RecW_WORLDID(ref B2RecBuffer buf, B2WorldId v)
        {
            b2RecW_U32(ref buf, b2StoreWorldId(v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void b2RecW_BODYID(ref B2RecBuffer buf, B2BodyId v)
        {
            b2RecW_U64(ref buf, b2StoreBodyId(v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void b2RecW_SHAPEID(ref B2RecBuffer buf, B2ShapeId v)
        {
            b2RecW_U64(ref buf, b2StoreShapeId(v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void b2RecW_CHAINID(ref B2RecBuffer buf, B2ChainId v)
        {
            b2RecW_U64(ref buf, b2StoreChainId(v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void b2RecW_JOINTID(ref B2RecBuffer buf, B2JointId v)
        {
            b2RecW_U64(ref buf, b2StoreJointId(v));
        }

        // Geometry is pointer-free POD, pointerWidth in the header gates the layout
        // Fields are written in native declaration order so the managed port produces the same
        // little-endian payload without relying on CLR padding.
        internal static void b2RecW_CIRCLE(ref B2RecBuffer buffer, B2Circle value)
        {
            b2RecW_VEC2(ref buffer, value.center);
            b2RecW_F32(ref buffer, value.radius);
        }

        internal static void b2RecW_CAPSULE(ref B2RecBuffer buffer, B2Capsule value)
        {
            b2RecW_VEC2(ref buffer, value.center1);
            b2RecW_VEC2(ref buffer, value.center2);
            b2RecW_F32(ref buffer, value.radius);
        }

        internal static void b2RecW_SEGMENT(ref B2RecBuffer buffer, B2Segment value)
        {
            b2RecW_VEC2(ref buffer, value.point1);
            b2RecW_VEC2(ref buffer, value.point2);
        }

        internal static void b2RecW_POLYGON(ref B2RecBuffer buf, B2Polygon v)
        {
            for (int i = 0; i < B2_MAX_POLYGON_VERTICES; ++i)
            {
                b2RecW_VEC2(ref buf, v.vertices[i]);
            }
            for (int i = 0; i < B2_MAX_POLYGON_VERTICES; ++i)
            {
                b2RecW_VEC2(ref buf, v.normals[i]);
            }
            b2RecW_VEC2(ref buf, v.centroid);
            b2RecW_F32(ref buf, v.radius);
            b2RecW_I32(ref buf, v.count);
        }

        internal static void b2RecW_CHAINSEG(ref B2RecBuffer buffer, B2ChainSegment value)
        {
            b2RecW_VEC2(ref buffer, value.ghost1);
            b2RecW_SEGMENT(ref buffer, value.segment);
            b2RecW_VEC2(ref buffer, value.ghost2);
            b2RecW_I32(ref buffer, value.chainId);
        }

        internal static void b2RecW_FILTER(ref B2RecBuffer buffer, B2Filter value)
        {
            b2RecW_U64(ref buffer, value.categoryBits);
            b2RecW_U64(ref buffer, value.maskBits);
            b2RecW_I32(ref buffer, value.groupIndex);
        }

        internal static void b2RecW_MATERIAL(ref B2RecBuffer buffer, B2SurfaceMaterial value)
        {
            b2RecW_F32(ref buffer, value.friction);
            b2RecW_F32(ref buffer, value.restitution);
            b2RecW_F32(ref buffer, value.rollingResistance);
            b2RecW_F32(ref buffer, value.tangentSpeed);
            b2RecW_U64(ref buffer, value.userMaterialId);
            b2RecW_U32(ref buffer, value.customColor);
        }

        internal static void b2RecW_MASSDATA(ref B2RecBuffer buffer, B2MassData value)
        {
            b2RecW_F32(ref buffer, value.mass);
            b2RecW_VEC2(ref buffer, value.center);
            b2RecW_F32(ref buffer, value.rotationalInertia);
        }

        internal static void b2RecW_LOCKS(ref B2RecBuffer buffer, B2MotionLocks value)
        {
            b2RecW_BOOL(ref buffer, value.linearX);
            b2RecW_BOOL(ref buffer, value.linearY);
            b2RecW_BOOL(ref buffer, value.angularZ);
        }

        internal static void b2RecW_STR(ref B2RecBuffer buf, string s)
        {
            if (s == null)
            {
                b2RecW_U16(ref buf, 0xFFFF);
                return;
            }

            ReadOnlySpan<char> chars = s.AsSpan();

            // Match the null-terminated upstream string.
            int nullIndex = chars.IndexOf('\0');
            if (nullIndex >= 0)
            {
                chars = chars.Slice(0, nullIndex);
            }

            int byteCount = Encoding.UTF8.GetByteCount(chars);
            int len = Math.Min(byteCount, 65534);

            b2RecW_U16(ref buf, (ushort)len);
            if (len == 0)
            {
                return;
            }

            const int charChunkCapacity = 256;

            // A UTF-16 code unit requires at most three UTF-8 bytes.
            Span<byte> bytes = stackalloc byte[charChunkCapacity * 3];

            int charOffset = 0;
            int byteOffset = 0;

            while (byteOffset < len)
            {
                int charCount = Math.Min(
                    charChunkCapacity,
                    chars.Length - charOffset);

                // Do not divide a valid surrogate pair between chunks.
                if (charOffset + charCount < chars.Length &&
                    char.IsHighSurrogate(chars[charOffset + charCount - 1]) &&
                    char.IsLowSurrogate(chars[charOffset + charCount]))
                {
                    charCount -= 1;
                }

                int written = Encoding.UTF8.GetBytes(
                    chars.Slice(charOffset, charCount),
                    bytes);

                int copyCount = Math.Min(written, len - byteOffset);
                b2RecBufAppend(ref buf, bytes, copyCount);

                charOffset += charCount;
                byteOffset += copyCount;

                // The 65,534-byte limit divided this UTF-8 chunk.
                if (copyCount < written)
                {
                    break;
                }
            }

            B2_ASSERT(byteOffset == len);
        }

        // Hand-written def helpers. Zero all pointer and cookie fields before serializing
        // Readers call b2Default*Def() first to get the cookie, then overwrite fields
        internal static void b2RecW_WORLDDEF(ref B2RecBuffer buffer, B2WorldDef value)
        {
            b2RecW_VEC2(ref buffer, value.gravity);
            b2RecW_F32(ref buffer, value.restitutionThreshold);
            b2RecW_F32(ref buffer, value.hitEventThreshold);
            b2RecW_F32(ref buffer, value.contactHertz);
            b2RecW_F32(ref buffer, value.contactDampingRatio);
            b2RecW_F32(ref buffer, value.contactSpeed);
            b2RecW_F32(ref buffer, value.maximumLinearSpeed);
            b2RecW_BOOL(ref buffer, value.enableSleep);
            b2RecW_BOOL(ref buffer, value.enableContinuous);
            b2RecW_BOOL(ref buffer, value.enableContactSoftening);
            b2RecW_I32(ref buffer, value.workerCount);
            // userData written as zero, host pointer not preserved
            b2RecW_U64(ref buffer, 0);
            // capacity
            b2RecW_I32(ref buffer, value.capacity.staticShapeCount);
            b2RecW_I32(ref buffer, value.capacity.dynamicShapeCount);
            b2RecW_I32(ref buffer, value.capacity.staticBodyCount);
            b2RecW_I32(ref buffer, value.capacity.dynamicBodyCount);
            b2RecW_I32(ref buffer, value.capacity.contactCount);
            // recordingPath, enqueueTask, finishTask, userTaskContext,
            // frictionCallback, restitutionCallback, internalValue: all omitted
        }

        internal static void b2RecW_BODYDEF(ref B2RecBuffer buffer, B2BodyDef value)
        {
            b2RecW_I32(ref buffer, (int)value.type);
            b2RecW_VEC2(ref buffer, value.position);
            b2RecW_ROT(ref buffer, value.rotation);
            b2RecW_VEC2(ref buffer, value.linearVelocity);
            b2RecW_F32(ref buffer, value.angularVelocity);
            b2RecW_F32(ref buffer, value.linearDamping);
            b2RecW_F32(ref buffer, value.angularDamping);
            b2RecW_F32(ref buffer, value.gravityScale);
            b2RecW_F32(ref buffer, value.sleepThreshold);
            b2RecW_STR(ref buffer, value.name);
            // userData: not preserved
            b2RecW_U64(ref buffer, 0);
            b2RecW_LOCKS(ref buffer, value.motionLocks);
            b2RecW_BOOL(ref buffer, value.enableSleep);
            b2RecW_BOOL(ref buffer, value.isAwake);
            b2RecW_BOOL(ref buffer, value.isBullet);
            b2RecW_BOOL(ref buffer, value.isEnabled);
            b2RecW_BOOL(ref buffer, value.allowFastRotation);
            b2RecW_BOOL(ref buffer, value.enableContactRecycling);
            // internalValue omitted
        }

        internal static void b2RecW_SHAPEDEF(ref B2RecBuffer buffer, B2ShapeDef value)
        {
            // userData: not preserved
            b2RecW_U64(ref buffer, 0);
            b2RecW_MATERIAL(ref buffer, value.material);
            b2RecW_F32(ref buffer, value.density);
            b2RecW_FILTER(ref buffer, value.filter);
            b2RecW_BOOL(ref buffer, value.enableCustomFiltering);
            b2RecW_BOOL(ref buffer, value.isSensor);
            b2RecW_BOOL(ref buffer, value.enableSensorEvents);
            b2RecW_BOOL(ref buffer, value.enableContactEvents);
            b2RecW_BOOL(ref buffer, value.enableHitEvents);
            b2RecW_BOOL(ref buffer, value.enablePreSolveEvents);
            b2RecW_BOOL(ref buffer, value.invokeContactCreation);
            b2RecW_BOOL(ref buffer, value.updateBodyMass);
            // internalValue omitted
        }

        // Variable-length def: point and material arrays are length-prefixed and inlined.
        // Arrays are cloned by b2CreateChain so they only need to outlive the dispatch call.
        internal static void b2RecW_CHAINDEF(ref B2RecBuffer buf, B2ChainDef v)
        {
            // userData: not preserved
            b2RecW_U64(ref buf, 0);
            b2RecW_I32(ref buf, v.count);
            for (int i = 0; i < v.count; ++i)
            {
                b2RecW_VEC2(ref buf, v.points[i]);
            }
            b2RecW_I32(ref buf, v.materialCount);
            for (int i = 0; i < v.materialCount; ++i)
            {
                b2RecW_MATERIAL(ref buf, v.materials[i]);
            }
            b2RecW_FILTER(ref buf, v.filter);
            b2RecW_BOOL(ref buf, v.isLoop);
            b2RecW_BOOL(ref buf, v.enableSensorEvents);
            // internalValue omitted
        }

        internal static void b2RecW_EXPLOSIONDEF(ref B2RecBuffer buffer, B2ExplosionDef value)
        {
            b2RecW_U64(ref buffer, value.maskBits);
            b2RecW_VEC2(ref buffer, value.position);
            b2RecW_F32(ref buffer, value.radius);
            b2RecW_F32(ref buffer, value.falloff);
            b2RecW_F32(ref buffer, value.impulsePerLength);
        }

        // Joint defs share a base. Body ids are written as ids and remapped to the replay world on
        // read. userData and internalValue are not serialized.
        internal static void b2RecW_JointBase(ref B2RecBuffer buffer, B2JointDef value)
        {
            b2RecW_U64(ref buffer, 0); // userData
            b2RecW_BODYID(ref buffer, value.bodyIdA);
            b2RecW_BODYID(ref buffer, value.bodyIdB);
            b2RecW_XF(ref buffer, value.localFrameA);
            b2RecW_XF(ref buffer, value.localFrameB);
            b2RecW_F32(ref buffer, value.forceThreshold);
            b2RecW_F32(ref buffer, value.torqueThreshold);
            b2RecW_F32(ref buffer, value.constraintHertz);
            b2RecW_F32(ref buffer, value.constraintDampingRatio);
            b2RecW_F32(ref buffer, value.drawScale);
            b2RecW_BOOL(ref buffer, value.collideConnected);
        }

        internal static void b2RecW_DISTANCEJOINTDEF(ref B2RecBuffer buffer, B2DistanceJointDef value)
        {
            b2RecW_JointBase(ref buffer, value.@base);
            b2RecW_F32(ref buffer, value.length);
            b2RecW_BOOL(ref buffer, value.enableSpring);
            b2RecW_F32(ref buffer, value.lowerSpringForce);
            b2RecW_F32(ref buffer, value.upperSpringForce);
            b2RecW_F32(ref buffer, value.hertz);
            b2RecW_F32(ref buffer, value.dampingRatio);
            b2RecW_BOOL(ref buffer, value.enableLimit);
            b2RecW_F32(ref buffer, value.minLength);
            b2RecW_F32(ref buffer, value.maxLength);
            b2RecW_BOOL(ref buffer, value.enableMotor);
            b2RecW_F32(ref buffer, value.maxMotorForce);
            b2RecW_F32(ref buffer, value.motorSpeed);
        }

        internal static void b2RecW_MOTORJOINTDEF(ref B2RecBuffer buffer, B2MotorJointDef value)
        {
            b2RecW_JointBase(ref buffer, value.@base);
            b2RecW_VEC2(ref buffer, value.linearVelocity);
            b2RecW_F32(ref buffer, value.maxVelocityForce);
            b2RecW_F32(ref buffer, value.angularVelocity);
            b2RecW_F32(ref buffer, value.maxVelocityTorque);
            b2RecW_F32(ref buffer, value.linearHertz);
            b2RecW_F32(ref buffer, value.linearDampingRatio);
            b2RecW_F32(ref buffer, value.maxSpringForce);
            b2RecW_F32(ref buffer, value.angularHertz);
            b2RecW_F32(ref buffer, value.angularDampingRatio);
            b2RecW_F32(ref buffer, value.maxSpringTorque);
        }

        internal static void b2RecW_FILTERJOINTDEF(ref B2RecBuffer buf, B2FilterJointDef v)
        {
            b2RecW_JointBase(ref buf, v.@base);
        }

        internal static void b2RecW_PRISMATICJOINTDEF(ref B2RecBuffer buffer, B2PrismaticJointDef value)
        {
            b2RecW_JointBase(ref buffer, value.@base);
            b2RecW_BOOL(ref buffer, value.enableSpring);
            b2RecW_F32(ref buffer, value.hertz);
            b2RecW_F32(ref buffer, value.dampingRatio);
            b2RecW_F32(ref buffer, value.targetTranslation);
            b2RecW_BOOL(ref buffer, value.enableLimit);
            b2RecW_F32(ref buffer, value.lowerTranslation);
            b2RecW_F32(ref buffer, value.upperTranslation);
            b2RecW_BOOL(ref buffer, value.enableMotor);
            b2RecW_F32(ref buffer, value.maxMotorForce);
            b2RecW_F32(ref buffer, value.motorSpeed);
        }

        internal static void b2RecW_REVOLUTEJOINTDEF(ref B2RecBuffer buffer, B2RevoluteJointDef value)
        {
            b2RecW_JointBase(ref buffer, value.@base);
            b2RecW_F32(ref buffer, value.targetAngle);
            b2RecW_BOOL(ref buffer, value.enableSpring);
            b2RecW_F32(ref buffer, value.hertz);
            b2RecW_F32(ref buffer, value.dampingRatio);
            b2RecW_BOOL(ref buffer, value.enableLimit);
            b2RecW_F32(ref buffer, value.lowerAngle);
            b2RecW_F32(ref buffer, value.upperAngle);
            b2RecW_BOOL(ref buffer, value.enableMotor);
            b2RecW_F32(ref buffer, value.maxMotorTorque);
            b2RecW_F32(ref buffer, value.motorSpeed);
        }

        internal static void b2RecW_WELDJOINTDEF(ref B2RecBuffer buffer, B2WeldJointDef value)
        {
            b2RecW_JointBase(ref buffer, value.@base);
            b2RecW_F32(ref buffer, value.linearHertz);
            b2RecW_F32(ref buffer, value.angularHertz);
            b2RecW_F32(ref buffer, value.linearDampingRatio);
            b2RecW_F32(ref buffer, value.angularDampingRatio);
        }

        internal static void b2RecW_WHEELJOINTDEF(ref B2RecBuffer buffer, B2WheelJointDef value)
        {
            b2RecW_JointBase(ref buffer, value.@base);
            b2RecW_BOOL(ref buffer, value.enableSpring);
            b2RecW_F32(ref buffer, value.hertz);
            b2RecW_F32(ref buffer, value.dampingRatio);
            b2RecW_BOOL(ref buffer, value.enableLimit);
            b2RecW_F32(ref buffer, value.lowerTranslation);
            b2RecW_F32(ref buffer, value.upperTranslation);
            b2RecW_BOOL(ref buffer, value.enableMotor);
            b2RecW_F32(ref buffer, value.maxMotorTorque);
            b2RecW_F32(ref buffer, value.motorSpeed);
        }

        internal static void b2RecW_AABB(ref B2RecBuffer buffer, B2AABB value)
        {
            b2RecW_VEC2(ref buffer, value.lowerBound);
            b2RecW_VEC2(ref buffer, value.upperBound);
        }

        internal static void b2RecW_QUERYFILTER(ref B2RecBuffer buffer, B2QueryFilter value)
        {
            b2RecW_U64(ref buffer, value.categoryBits);
            b2RecW_U64(ref buffer, value.maskBits);
        }

        internal static void b2RecW_SHAPEPROXY(ref B2RecBuffer buf, B2ShapeProxy v)
        {
            int count = v.count;
            if (count < 0)
            {
                count = 0;
            }
            if (count > B2_MAX_POLYGON_VERTICES)
            {
                count = B2_MAX_POLYGON_VERTICES;
            }
            b2RecW_I32(ref buf, count);
            for (int i = 0; i < count; ++i)
            {
                b2RecW_VEC2(ref buf, v.points[i]);
            }
            b2RecW_F32(ref buf, v.radius);
        }

        internal static void b2RecW_RAYCASTINPUT(ref B2RecBuffer buffer, B2RayCastInput value)
        {
            b2RecW_VEC2(ref buffer, value.origin);
            b2RecW_VEC2(ref buffer, value.translation);
            b2RecW_F32(ref buffer, value.maxFraction);
        }

        internal static void b2RecW_CASTOUTPUT(ref B2RecBuffer buffer, B2CastOutput value)
        {
            b2RecW_VEC2(ref buffer, value.normal);
            b2RecW_VEC2(ref buffer, value.point);
            b2RecW_F32(ref buffer, value.fraction);
            b2RecW_I32(ref buffer, value.iterations);
            b2RecW_BOOL(ref buffer, value.hit);
        }

        internal static void b2RecW_RAYRESULT(ref B2RecBuffer buffer, B2RayResult value)
        {
            b2RecW_SHAPEID(ref buffer, value.shapeId);
            b2RecW_VEC2(ref buffer, value.point);
            b2RecW_VEC2(ref buffer, value.normal);
            b2RecW_F32(ref buffer, value.fraction);
            b2RecW_I32(ref buffer, value.nodeVisits);
            b2RecW_I32(ref buffer, value.leafVisits);
            b2RecW_BOOL(ref buffer, value.hit);
        }

        internal static void b2RecW_PLANERESULT(ref B2RecBuffer buffer, B2PlaneResult value)
        {
            b2RecW_VEC2(ref buffer, value.plane.normal);
            b2RecW_F32(ref buffer, value.plane.offset);
            b2RecW_VEC2(ref buffer, value.point);
            b2RecW_BOOL(ref buffer, value.hit);
        }

        internal static void b2RecW_TREESTATS(ref B2RecBuffer buffer, B2TreeStats value)
        {
            b2RecW_I32(ref buffer, value.nodeVisits);
            b2RecW_I32(ref buffer, value.leafVisits);
        }
    }
}
