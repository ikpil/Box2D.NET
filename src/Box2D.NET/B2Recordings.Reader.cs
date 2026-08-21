// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;
using System.Text;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Cores;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2DistanceJoints;
using static Box2D.NET.B2MotorJoints;
using static Box2D.NET.B2PrismaticJoints;
using static Box2D.NET.B2RevoluteJoints;
using static Box2D.NET.B2WeldJoints;
using static Box2D.NET.B2WheelJoints;

namespace Box2D.NET
{
    public static partial class B2Recordings
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void b2RecRdrCheck(B2RecReader rdr, int size)
        {
            // 64-bit compare so a corrupt or oversized size can't overflow the cursor add
            if (size < 0 || (long)rdr.cursor + size > rdr.size)
            {
                rdr.ok = false;
            }
        }

        // Reserve rdr scratch for a count taken from an untrusted file. Every recorded element
        // consumes at least one byte, so a valid count can never exceed the bytes left in the file.
        // Reject anything larger (or negative, or that would overflow the byte size) by failing the read
        // rather than allocating wildly. Contents are not preserved; callers overwrite before use.
        internal static bool b2RecReserveScratch<T>(B2RecReader rdr, ref T[] data, ref int capacity, int count, int elementSize)
        {
            int remaining = rdr.size - rdr.cursor;
            if (count < 0 || remaining < 0 || count > remaining || count > int.MaxValue / elementSize)
            {
                rdr.ok = false;
                return false;
            }

            if (count <= capacity)
            {
                return true;
            }

            int newCapacity = count <= int.MaxValue / elementSize - 8 ? count + 8 : count;
            data = new T[newCapacity];
            capacity = newCapacity;
            return true;
        }

        // Read primitives

        internal static byte b2RecR_U8(B2RecReader rdr)
        {
            b2RecRdrCheck(rdr, 1);
            if (!rdr.ok)
            {
                return 0;
            }
            return rdr.data[rdr.cursor++];
        }

        internal static ushort b2RecR_U16(B2RecReader rdr)
        {
            b2RecRdrCheck(rdr, 2);
            if (!rdr.ok)
            {
                return 0;
            }
            ushort v = (ushort)(rdr.data[rdr.cursor] | rdr.data[rdr.cursor + 1] << 8);
            rdr.cursor += 2;
            return v;
        }

        internal static uint b2RecR_U24(B2RecReader rdr)
        {
            b2RecRdrCheck(rdr, 3);
            if (!rdr.ok)
            {
                return 0;
            }
            uint v = (uint)(rdr.data[rdr.cursor] | rdr.data[rdr.cursor + 1] << 8 | rdr.data[rdr.cursor + 2] << 16);
            rdr.cursor += 3;
            return v;
        }

        internal static uint b2RecR_U32(B2RecReader rdr)
        {
            b2RecRdrCheck(rdr, 4);
            if (!rdr.ok)
            {
                return 0;
            }
            uint v = (uint)(rdr.data[rdr.cursor] | rdr.data[rdr.cursor + 1] << 8 | rdr.data[rdr.cursor + 2] << 16 | rdr.data[rdr.cursor + 3] << 24);
            rdr.cursor += 4;
            return v;
        }

        internal static ulong b2RecR_U64(B2RecReader rdr)
        {
            b2RecRdrCheck(rdr, 8);
            if (!rdr.ok)
            {
                return 0;
            }
            ulong v = rdr.data[rdr.cursor] | (ulong)rdr.data[rdr.cursor + 1] << 8 | (ulong)rdr.data[rdr.cursor + 2] << 16 |
                      (ulong)rdr.data[rdr.cursor + 3] << 24 | (ulong)rdr.data[rdr.cursor + 4] << 32 |
                      (ulong)rdr.data[rdr.cursor + 5] << 40 | (ulong)rdr.data[rdr.cursor + 6] << 48 |
                      (ulong)rdr.data[rdr.cursor + 7] << 56;
            rdr.cursor += 8;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int b2RecR_I32(B2RecReader rdr)
        {
            return unchecked((int)b2RecR_U32(rdr));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float b2RecR_F32(B2RecReader rdr)
        {
            uint bits = b2RecR_U32(rdr);
            float v = BitConverter.Int32BitsToSingle(unchecked((int)bits));
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool b2RecR_BOOL(B2RecReader rdr)
        {
            return b2RecR_U8(rdr) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static B2Vec2 b2RecR_VEC2(B2RecReader rdr)
        {
            B2Vec2 v;
            v.X = b2RecR_F32(rdr);
            v.Y = b2RecR_F32(rdr);
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static B2Rot b2RecR_ROT(B2RecReader rdr)
        {
            B2Rot r;
            r.c = b2RecR_F32(rdr);
            r.s = b2RecR_F32(rdr);
            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static B2Transform b2RecR_XF(B2RecReader rdr)
        {
            B2Transform xf;
            xf.p = b2RecR_VEC2(rdr);
            xf.q = b2RecR_ROT(rdr);
            return xf;
        }

        // Read a pointer-free POD blob of the given size into out, advancing the cursor.
        // Zeroes out on overrun so a truncated file fails the read check rather than reading garbage.
        // Managed structured readers below read fields explicitly instead of relying on CLR layout and padding.
        private static void b2RecRdrBlob(B2RecReader rdr, Span<byte> output)
        {
            b2RecRdrCheck(rdr, output.Length);
            if (!rdr.ok)
            {
                output.Clear();
                return;
            }

            new ReadOnlySpan<byte>(rdr.data, rdr.cursor, output.Length).CopyTo(output);
            rdr.cursor += output.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static B2WorldId b2RecR_WORLDID(B2RecReader rdr)
        {
            return b2LoadWorldId(b2RecR_U32(rdr));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static B2BodyId b2RecR_BODYID(B2RecReader rdr)
        {
            return b2LoadBodyId(b2RecR_U64(rdr));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static B2ShapeId b2RecR_SHAPEID(B2RecReader rdr)
        {
            return b2LoadShapeId(b2RecR_U64(rdr));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static B2ChainId b2RecR_CHAINID(B2RecReader rdr)
        {
            return b2LoadChainId(b2RecR_U64(rdr));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static B2JointId b2RecR_JOINTID(B2RecReader rdr)
        {
            return b2LoadJointId(b2RecR_U64(rdr));
        }

        internal static B2Circle b2RecR_CIRCLE(B2RecReader rdr)
        {
            B2Circle c;
            c.center = b2RecR_VEC2(rdr);
            c.radius = b2RecR_F32(rdr);
            return c;
        }

        internal static B2Capsule b2RecR_CAPSULE(B2RecReader rdr)
        {
            B2Capsule c;
            c.center1 = b2RecR_VEC2(rdr);
            c.center2 = b2RecR_VEC2(rdr);
            c.radius = b2RecR_F32(rdr);
            return c;
        }

        internal static B2Segment b2RecR_SEGMENT(B2RecReader rdr)
        {
            B2Segment s;
            s.point1 = b2RecR_VEC2(rdr);
            s.point2 = b2RecR_VEC2(rdr);
            return s;
        }

        internal static B2Polygon b2RecR_POLYGON(B2RecReader rdr)
        {
            B2Polygon p = new B2Polygon();
            for (int i = 0; i < B2Constants.B2_MAX_POLYGON_VERTICES; ++i)
            {
                p.vertices[i] = b2RecR_VEC2(rdr);
            }
            for (int i = 0; i < B2Constants.B2_MAX_POLYGON_VERTICES; ++i)
            {
                p.normals[i] = b2RecR_VEC2(rdr);
            }
            p.centroid = b2RecR_VEC2(rdr);
            p.radius = b2RecR_F32(rdr);
            p.count = b2RecR_I32(rdr);
            return p;
        }

        internal static B2ChainSegment b2RecR_CHAINSEG(B2RecReader rdr)
        {
            B2ChainSegment cs;
            cs.ghost1 = b2RecR_VEC2(rdr);
            cs.segment = b2RecR_SEGMENT(rdr);
            cs.ghost2 = b2RecR_VEC2(rdr);
            cs.chainId = b2RecR_I32(rdr);
            return cs;
        }

        internal static B2Filter b2RecR_FILTER(B2RecReader rdr)
        {
            B2Filter f;
            f.categoryBits = b2RecR_U64(rdr);
            f.maskBits = b2RecR_U64(rdr);
            f.groupIndex = b2RecR_I32(rdr);
            return f;
        }

        internal static B2SurfaceMaterial b2RecR_MATERIAL(B2RecReader rdr)
        {
            B2SurfaceMaterial m = b2DefaultSurfaceMaterial();
            m.friction = b2RecR_F32(rdr);
            m.restitution = b2RecR_F32(rdr);
            m.rollingResistance = b2RecR_F32(rdr);
            m.tangentSpeed = b2RecR_F32(rdr);
            m.userMaterialId = b2RecR_U64(rdr);
            m.customColor = b2RecR_U32(rdr);
            return m;
        }

        internal static B2MassData b2RecR_MASSDATA(B2RecReader rdr)
        {
            B2MassData md;
            md.mass = b2RecR_F32(rdr);
            md.center = b2RecR_VEC2(rdr);
            md.rotationalInertia = b2RecR_F32(rdr);
            return md;
        }

        internal static B2MotionLocks b2RecR_LOCKS(B2RecReader rdr)
        {
            B2MotionLocks locks;
            locks.linearX = b2RecR_BOOL(rdr);
            locks.linearY = b2RecR_BOOL(rdr);
            locks.angularZ = b2RecR_BOOL(rdr);
            return locks;
        }

        // Returns a pointer into a rotating set of static buffers, valid until the next several
        // STR reads. Only used to pass a name straight into a create/setter call during dispatch.
        internal static string b2RecR_STR(B2RecReader rdr)
        {
            // C# returns an independently owned managed string instead.
            ushort len = b2RecR_U16(rdr);
            if (len == 0xFFFF)
            {
                return null;
            }

            int n = (int)len;
            if (n > B2Constants.B2_NAME_LENGTH)
            {
                n = B2Constants.B2_NAME_LENGTH;
            }
            b2RecRdrCheck(rdr, (int)len);
            string value = rdr.ok && n > 0 ? Encoding.UTF8.GetString(rdr.data, rdr.cursor, n) : string.Empty;

            // Skip the full recorded length even if it exceeds the clamp
            rdr.cursor += (int)len;
            return value;
        }

        // Def readers: start from b2Default*Def() to get cookie/internalValue, then overlay fields

        internal static B2WorldDef b2RecR_WORLDDEF(B2RecReader rdr)
        {
            B2WorldDef def = b2DefaultWorldDef();
            def.gravity = b2RecR_VEC2(rdr);
            def.restitutionThreshold = b2RecR_F32(rdr);
            def.hitEventThreshold = b2RecR_F32(rdr);
            def.contactHertz = b2RecR_F32(rdr);
            def.contactDampingRatio = b2RecR_F32(rdr);
            def.contactSpeed = b2RecR_F32(rdr);
            def.maximumLinearSpeed = b2RecR_F32(rdr);
            def.enableSleep = b2RecR_BOOL(rdr);
            def.enableContinuous = b2RecR_BOOL(rdr);
            def.enableContactSoftening = b2RecR_BOOL(rdr);
            def.workerCount = b2RecR_I32(rdr);
            b2RecR_U64(rdr); // userData (not preserved)
            def.capacity.staticShapeCount = b2RecR_I32(rdr);
            def.capacity.dynamicShapeCount = b2RecR_I32(rdr);
            def.capacity.staticBodyCount = b2RecR_I32(rdr);
            def.capacity.dynamicBodyCount = b2RecR_I32(rdr);
            def.capacity.contactCount = b2RecR_I32(rdr);

            // pointers/callbacks stay NULL from b2DefaultWorldDef()
            def.recordingPath = null;
            def.enqueueTask = null;
            def.finishTask = null;
            def.userTaskContext = null;
            def.frictionCallback = null;
            def.restitutionCallback = null;
            def.userData = default;
            return def;
        }

        internal static B2BodyDef b2RecR_BODYDEF(B2RecReader rdr)
        {
            B2BodyDef def = b2DefaultBodyDef();
            def.type = (B2BodyType)b2RecR_I32(rdr);
            def.position = b2RecR_VEC2(rdr);
            def.rotation = b2RecR_ROT(rdr);
            def.linearVelocity = b2RecR_VEC2(rdr);
            def.angularVelocity = b2RecR_F32(rdr);
            def.linearDamping = b2RecR_F32(rdr);
            def.angularDamping = b2RecR_F32(rdr);
            def.gravityScale = b2RecR_F32(rdr);
            def.sleepThreshold = b2RecR_F32(rdr);

            // b2RecR_STR handles the over-length clamp and skips the full recorded length, so the
            // cursor stays aligned even for names longer than B2_NAME_LENGTH. Valid until the create call.
            def.name = b2RecR_STR(rdr);

            b2RecR_U64(rdr); // userData (not preserved)
            def.motionLocks = b2RecR_LOCKS(rdr);
            def.enableSleep = b2RecR_BOOL(rdr);
            def.isAwake = b2RecR_BOOL(rdr);
            def.isBullet = b2RecR_BOOL(rdr);
            def.isEnabled = b2RecR_BOOL(rdr);
            def.allowFastRotation = b2RecR_BOOL(rdr);
            def.enableContactRecycling = b2RecR_BOOL(rdr);
            def.userData = default;
            return def;
        }

        internal static B2ShapeDef b2RecR_SHAPEDEF(B2RecReader rdr)
        {
            B2ShapeDef def = b2DefaultShapeDef();
            b2RecR_U64(rdr); // userData (not preserved)
            def.material = b2RecR_MATERIAL(rdr);
            def.density = b2RecR_F32(rdr);
            def.filter = b2RecR_FILTER(rdr);
            def.enableCustomFiltering = b2RecR_BOOL(rdr);
            def.isSensor = b2RecR_BOOL(rdr);
            def.enableSensorEvents = b2RecR_BOOL(rdr);
            def.enableContactEvents = b2RecR_BOOL(rdr);
            def.enableHitEvents = b2RecR_BOOL(rdr);
            def.enablePreSolveEvents = b2RecR_BOOL(rdr);
            def.invokeContactCreation = b2RecR_BOOL(rdr);
            def.updateBodyMass = b2RecR_BOOL(rdr);
            def.userData = default;
            return def;
        }

        internal static B2ChainDef b2RecR_CHAINDEF(B2RecReader rdr)
        {
            B2ChainDef def = b2DefaultChainDef();
            b2RecR_U64(rdr); // userData (not preserved)

            int count = b2RecR_I32(rdr);
            if (count < 0)
            {
                count = 0;
            }
            if (!b2RecReserveScratch(rdr, ref rdr.chainPoints, ref rdr.chainPointCap, count, 8))
            {
                count = 0; // corrupt count, the read has already failed
            }
            for (int i = 0; i < count; ++i)
            {
                rdr.chainPoints[i] = b2RecR_VEC2(rdr);
            }
            def.points = count > 0 ? rdr.chainPoints : null;
            def.count = count;

            int materialCount = b2RecR_I32(rdr);
            if (materialCount < 0)
            {
                materialCount = 0;
            }
            if (!b2RecReserveScratch(rdr, ref rdr.chainMaterials, ref rdr.chainMaterialCap, materialCount, 32))
            {
                materialCount = 0;
            }
            for (int i = 0; i < materialCount; ++i)
            {
                rdr.chainMaterials[i] = b2RecR_MATERIAL(rdr);
            }
            def.materials = materialCount > 0 ? rdr.chainMaterials : null;
            def.materialCount = materialCount;

            def.filter = b2RecR_FILTER(rdr);
            def.isLoop = b2RecR_BOOL(rdr);
            def.enableSensorEvents = b2RecR_BOOL(rdr);
            def.userData = default;
            return def;
        }

        internal static B2ExplosionDef b2RecR_EXPLOSIONDEF(B2RecReader rdr)
        {
            B2ExplosionDef def = b2DefaultExplosionDef();
            def.maskBits = b2RecR_U64(rdr);
            def.position = b2RecR_VEC2(rdr);
            def.radius = b2RecR_F32(rdr);
            def.falloff = b2RecR_F32(rdr);
            def.impulsePerLength = b2RecR_F32(rdr);
            return def;
        }

        // Body ids are read with their recorded world0; the create dispatcher remaps them.
        internal static void b2RecR_JointBase(B2RecReader rdr, ref B2JointDef @base)
        {
            b2RecR_U64(rdr); // userData
            @base.bodyIdA = b2RecR_BODYID(rdr);
            @base.bodyIdB = b2RecR_BODYID(rdr);
            @base.localFrameA = b2RecR_XF(rdr);
            @base.localFrameB = b2RecR_XF(rdr);
            @base.forceThreshold = b2RecR_F32(rdr);
            @base.torqueThreshold = b2RecR_F32(rdr);
            @base.constraintHertz = b2RecR_F32(rdr);
            @base.constraintDampingRatio = b2RecR_F32(rdr);
            @base.drawScale = b2RecR_F32(rdr);
            @base.collideConnected = b2RecR_BOOL(rdr);
            @base.userData = default;
        }

        internal static B2DistanceJointDef b2RecR_DISTANCEJOINTDEF(B2RecReader rdr)
        {
            B2DistanceJointDef def = b2DefaultDistanceJointDef();
            b2RecR_JointBase(rdr, ref def.@base);
            def.length = b2RecR_F32(rdr);
            def.enableSpring = b2RecR_BOOL(rdr);
            def.lowerSpringForce = b2RecR_F32(rdr);
            def.upperSpringForce = b2RecR_F32(rdr);
            def.hertz = b2RecR_F32(rdr);
            def.dampingRatio = b2RecR_F32(rdr);
            def.enableLimit = b2RecR_BOOL(rdr);
            def.minLength = b2RecR_F32(rdr);
            def.maxLength = b2RecR_F32(rdr);
            def.enableMotor = b2RecR_BOOL(rdr);
            def.maxMotorForce = b2RecR_F32(rdr);
            def.motorSpeed = b2RecR_F32(rdr);
            return def;
        }

        internal static B2MotorJointDef b2RecR_MOTORJOINTDEF(B2RecReader rdr)
        {
            B2MotorJointDef def = b2DefaultMotorJointDef();
            b2RecR_JointBase(rdr, ref def.@base);
            def.linearVelocity = b2RecR_VEC2(rdr);
            def.maxVelocityForce = b2RecR_F32(rdr);
            def.angularVelocity = b2RecR_F32(rdr);
            def.maxVelocityTorque = b2RecR_F32(rdr);
            def.linearHertz = b2RecR_F32(rdr);
            def.linearDampingRatio = b2RecR_F32(rdr);
            def.maxSpringForce = b2RecR_F32(rdr);
            def.angularHertz = b2RecR_F32(rdr);
            def.angularDampingRatio = b2RecR_F32(rdr);
            def.maxSpringTorque = b2RecR_F32(rdr);
            return def;
        }

        internal static B2FilterJointDef b2RecR_FILTERJOINTDEF(B2RecReader rdr)
        {
            B2FilterJointDef def = b2DefaultFilterJointDef();
            b2RecR_JointBase(rdr, ref def.@base);
            return def;
        }

        internal static B2PrismaticJointDef b2RecR_PRISMATICJOINTDEF(B2RecReader rdr)
        {
            B2PrismaticJointDef def = b2DefaultPrismaticJointDef();
            b2RecR_JointBase(rdr, ref def.@base);
            def.enableSpring = b2RecR_BOOL(rdr);
            def.hertz = b2RecR_F32(rdr);
            def.dampingRatio = b2RecR_F32(rdr);
            def.targetTranslation = b2RecR_F32(rdr);
            def.enableLimit = b2RecR_BOOL(rdr);
            def.lowerTranslation = b2RecR_F32(rdr);
            def.upperTranslation = b2RecR_F32(rdr);
            def.enableMotor = b2RecR_BOOL(rdr);
            def.maxMotorForce = b2RecR_F32(rdr);
            def.motorSpeed = b2RecR_F32(rdr);
            return def;
        }

        internal static B2RevoluteJointDef b2RecR_REVOLUTEJOINTDEF(B2RecReader rdr)
        {
            B2RevoluteJointDef def = b2DefaultRevoluteJointDef();
            b2RecR_JointBase(rdr, ref def.@base);
            def.targetAngle = b2RecR_F32(rdr);
            def.enableSpring = b2RecR_BOOL(rdr);
            def.hertz = b2RecR_F32(rdr);
            def.dampingRatio = b2RecR_F32(rdr);
            def.enableLimit = b2RecR_BOOL(rdr);
            def.lowerAngle = b2RecR_F32(rdr);
            def.upperAngle = b2RecR_F32(rdr);
            def.enableMotor = b2RecR_BOOL(rdr);
            def.maxMotorTorque = b2RecR_F32(rdr);
            def.motorSpeed = b2RecR_F32(rdr);
            return def;
        }

        internal static B2WeldJointDef b2RecR_WELDJOINTDEF(B2RecReader rdr)
        {
            B2WeldJointDef def = b2DefaultWeldJointDef();
            b2RecR_JointBase(rdr, ref def.@base);
            def.linearHertz = b2RecR_F32(rdr);
            def.angularHertz = b2RecR_F32(rdr);
            def.linearDampingRatio = b2RecR_F32(rdr);
            def.angularDampingRatio = b2RecR_F32(rdr);
            return def;
        }

        internal static B2WheelJointDef b2RecR_WHEELJOINTDEF(B2RecReader rdr)
        {
            B2WheelJointDef def = b2DefaultWheelJointDef();
            b2RecR_JointBase(rdr, ref def.@base);
            def.enableSpring = b2RecR_BOOL(rdr);
            def.hertz = b2RecR_F32(rdr);
            def.dampingRatio = b2RecR_F32(rdr);
            def.enableLimit = b2RecR_BOOL(rdr);
            def.lowerTranslation = b2RecR_F32(rdr);
            def.upperTranslation = b2RecR_F32(rdr);
            def.enableMotor = b2RecR_BOOL(rdr);
            def.maxMotorTorque = b2RecR_F32(rdr);
            def.motorSpeed = b2RecR_F32(rdr);
            return def;
        }

        internal static B2AABB b2RecR_AABB(B2RecReader rdr)
        {
            B2AABB v;
            v.lowerBound = b2RecR_VEC2(rdr);
            v.upperBound = b2RecR_VEC2(rdr);
            return v;
        }

        internal static B2QueryFilter b2RecR_QUERYFILTER(B2RecReader rdr)
        {
            B2QueryFilter f;
            f.categoryBits = b2RecR_U64(rdr);
            f.maskBits = b2RecR_U64(rdr);
            return f;
        }

        internal static B2ShapeProxy b2RecR_SHAPEPROXY(B2RecReader rdr)
        {
            B2ShapeProxy p = new B2ShapeProxy();
            int count = b2RecR_I32(rdr);
            if (count < 0)
            {
                count = 0;
            }
            if (count > B2Constants.B2_MAX_POLYGON_VERTICES)
            {
                count = B2Constants.B2_MAX_POLYGON_VERTICES;
            }
            p.count = count;
            for (int i = 0; i < count; ++i)
            {
                p.points[i] = b2RecR_VEC2(rdr);
            }
            p.radius = b2RecR_F32(rdr);
            return p;
        }

        internal static B2RayCastInput b2RecR_RAYCASTINPUT(B2RecReader rdr)
        {
            B2RayCastInput v;
            v.origin = b2RecR_VEC2(rdr);
            v.translation = b2RecR_VEC2(rdr);
            v.maxFraction = b2RecR_F32(rdr);
            return v;
        }

        internal static B2CastOutput b2RecR_CASTOUTPUT(B2RecReader rdr)
        {
            B2CastOutput v;
            v.normal = b2RecR_VEC2(rdr);
            v.point = b2RecR_VEC2(rdr);
            v.fraction = b2RecR_F32(rdr);
            v.iterations = b2RecR_I32(rdr);
            v.hit = b2RecR_BOOL(rdr);
            return v;
        }

        internal static B2RayResult b2RecR_RAYRESULT(B2RecReader rdr)
        {
            B2RayResult v = new B2RayResult();
            // shapeId keeps the recorded world0; b2RecMakeShapeId is applied at compare time
            v.shapeId = b2RecR_SHAPEID(rdr);
            v.point = b2RecR_VEC2(rdr);
            v.normal = b2RecR_VEC2(rdr);
            v.fraction = b2RecR_F32(rdr);
            v.nodeVisits = b2RecR_I32(rdr);
            v.leafVisits = b2RecR_I32(rdr);
            v.hit = b2RecR_BOOL(rdr);
            return v;
        }

        internal static B2PlaneResult b2RecR_PLANERESULT(B2RecReader rdr)
        {
            B2PlaneResult v = new B2PlaneResult();
            v.plane.normal = b2RecR_VEC2(rdr);
            v.plane.offset = b2RecR_F32(rdr);
            v.point = b2RecR_VEC2(rdr);
            v.hit = b2RecR_BOOL(rdr);
            return v;
        }

        internal static B2TreeStats b2RecR_TREESTATS(B2RecReader rdr)
        {
            B2TreeStats v;
            v.nodeVisits = b2RecR_I32(rdr);
            v.leafVisits = b2RecR_I32(rdr);
            return v;
        }

    }
}
