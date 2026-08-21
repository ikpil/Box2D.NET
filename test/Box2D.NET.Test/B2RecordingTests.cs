// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using NUnit.Framework;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2DistanceJoints;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2MotorJoints;
using static Box2D.NET.B2PrismaticJoints;
using static Box2D.NET.B2Recordings;
using static Box2D.NET.B2RevoluteJoints;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2WeldJoints;
using static Box2D.NET.B2WheelJoints;
using static Box2D.NET.B2Worlds;

namespace Box2D.NET.Test
{
    public class B2RecordingTests
    {
        // Query callbacks used by RecordingTest
        private static bool OverlapFcn(B2ShapeId id, object context)
        {
            return true;
        }

        private static float CastFcn(B2ShapeId id, B2Vec2 point, B2Vec2 normal, float fraction, ref QueryContext context)
        {
            return fraction;
        }

        private static float ReplacingCastFcn(B2ShapeId id, B2Vec2 point, B2Vec2 normal, float fraction, ref QueryContext context)
        {
            var generations = context.generations;
            generations.Add(context.generation);
            context = new QueryContext
            {
                generations = generations,
                generation = context.generation + 1,
            };
            return 1.0f;
        }

        private static bool PlaneFcn(B2ShapeId id, ref B2PlaneResult plane, object context)
        {
            return true;
        }

        // Issue all 9 spatial query types against worldId. groundShapeId and a known position
        // are used for the shape-level queries.
        private static void IssueAllQueries(B2WorldId worldId, B2ShapeId groundShapeId)
        {
            B2QueryFilter filter = b2DefaultQueryFilter();

            // OverlapAABB
            B2AABB aabb = new B2AABB(new B2Vec2(-5.0f, -15.0f), new B2Vec2(5.0f, 5.0f));
            b2World_OverlapAABB(worldId, aabb, filter, OverlapFcn, null);

            // OverlapShape (small box proxy)
            B2Vec2[] boxPoints =
            {
                new B2Vec2(-0.5f, -0.5f), new B2Vec2(0.5f, -0.5f),
                new B2Vec2(0.5f, 0.5f), new B2Vec2(-0.5f, 0.5f),
            };
            B2ShapeProxy proxy = b2MakeProxy(boxPoints, 4, 0.0f);
            b2World_OverlapShape(worldId, ref proxy, filter, OverlapFcn, null);

            // CastRay (all hits)
            B2Vec2 rayOrigin = new B2Vec2(0.0f, 10.0f);
            B2Vec2 rayTranslation = new B2Vec2(0.0f, -20.0f);
            b2World_CastRay(worldId, rayOrigin, rayTranslation, filter, CastFcn, new QueryContext());

            // CastRayClosest
            b2World_CastRayClosest(worldId, rayOrigin, rayTranslation, filter);

            // CastShape (circle proxy)
            B2ShapeProxy circleProxy = b2MakeProxy(new B2Vec2(), 1, 0.3f);
            b2World_CastShape(worldId, ref circleProxy, rayTranslation, filter, CastFcn, new QueryContext());

            // CollideMover (capsule with radius > 2*B2_LINEAR_SLOP)
            B2Capsule mover = new B2Capsule(new B2Vec2(-0.3f, 0.0f), new B2Vec2(0.3f, 0.0f), 0.5f);
            b2World_CollideMover(worldId, mover, filter, PlaneFcn, null);

            // CastMover
            b2World_CastMover(worldId, mover, new B2Vec2(0.0f, -5.0f), filter);

            // Shape_TestPoint: test inside (0,0 local, well inside the r=10 ground circle at y=-10)
            // and outside
            b2Shape_TestPoint(groundShapeId, new B2Vec2(0.0f, -10.0f)); // inside center of ground
            b2Shape_TestPoint(groundShapeId, new B2Vec2(0.0f, 100.0f)); // outside

            // Shape_RayCast against the ground shape
            B2RayCastInput input = new B2RayCastInput(new B2Vec2(0.0f, 5.0f), new B2Vec2(0.0f, -20.0f), 1.0f);
            b2Shape_RayCast(groundShapeId, input);
        }

        // No-op draw callbacks for the headless draw-path exercise
        private static void DrawLine(in B2Vec2 p1, in B2Vec2 p2, B2HexColor color, object context)
        {
            ((DrawContext)context).callCount += 1;
        }

        private static void DrawPoint(in B2Vec2 point, float size, B2HexColor color, object context)
        {
            ((DrawContext)context).callCount += 1;
        }

        private static void DrawPolygon(ReadOnlySpan<B2Vec2> vertices, int count, B2HexColor color, object context)
        {
            ((DrawContext)context).callCount += 1;
        }

        private static void DrawCapsule(in B2Vec2 p1, in B2Vec2 p2, float radius, B2HexColor color, object context)
        {
            ((DrawContext)context).callCount += 1;
        }

        [Test]
        public void RecordingQueryTypeValuesMatchUpstream()
        {
            Assert.That((int)B2RecQueryType.b2_recQueryOverlapAABB, Is.EqualTo(0));
            Assert.That((int)B2RecQueryType.b2_recQueryShapeRayCast, Is.EqualTo(8));
        }

        [Test]
        public void RecordingFunctionsKeepUpstreamNames()
        {
            string[] names = typeof(B2Recordings)
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Select(method => method.Name)
                .ToArray();

            string[] coreNames =
            {
                "b2RecBufAppend", "b2RecBufFree", "b2RecReserveU32", "b2RecPatchU32",
                "b2RecBeginRecord", "b2RecEndRecord", "b2RecCommitRecord", "b2FlushRecording",
                "b2RecQueryBegin", "b2RecQueryCommit", "b2RecOverlapTrampoline", "b2RecCastTrampoline",
                "b2RecPlaneTrampoline", "b2RecRdrCheck", "b2RecReserveScratch", "b2RecRdrBlob",
                "b2RecR_U32", "b2RecW_U32", "b2RecDispatchOne", "b2RecPumpToWorld", "b2RecScanFile",
                "b2StartRecording", "b2StopRecordingInternal", "b2HashWorldState",
            };
            foreach (string name in coreNames)
            {
                Assert.That(names, Does.Contain(name), name);
            }

            Assert.That(names.Count(name => name.StartsWith("b2RecWriteArgs_", StringComparison.Ordinal)), Is.EqualTo(146));
            Assert.That(names.Count(name => name.StartsWith("b2RecWrite_", StringComparison.Ordinal)), Is.EqualTo(146));
            Assert.That(names.Count(name => name.StartsWith("b2RecWriteRet_", StringComparison.Ordinal)), Is.EqualTo(14));
            Assert.That(names.Count(name => name.StartsWith("b2RecDispatch_", StringComparison.Ordinal)), Is.EqualTo(146));
            Assert.That(names, Does.Not.Contain("b2RecDispatch"));
            Assert.That(names, Does.Not.Contain("b2RecDispatchCreate"));
        }

        [Test]
        public void RecordingOperationsUseTypedArgumentStructs()
        {
            Assembly assembly = typeof(B2Recordings).Assembly;
            Type[] argumentTypes = assembly.GetTypes()
                .Where(type => type.Namespace == "Box2D.NET" && type.Name.StartsWith("B2RecArgs_", StringComparison.Ordinal))
                .ToArray();

            Assert.That(argumentTypes, Has.Length.EqualTo(146));
            Assert.That(argumentTypes.All(type => type.IsValueType), Is.True);
            Assert.That(assembly.GetType("Box2D.NET.B2RecTag"), Is.Null);
            Assert.That(assembly.GetType("Box2D.NET.B2RecOpDefinition"), Is.Null);
            Assert.That(assembly.GetType("Box2D.NET.B2RecordingOps"), Is.Null);

            MethodInfo[] methods = typeof(B2Recordings)
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo[] argumentWriters = methods
                .Where(method => method.Name.StartsWith("b2RecWriteArgs_", StringComparison.Ordinal))
                .ToArray();
            MethodInfo[] fullWriters = methods
                .Where(method => method.Name.StartsWith("b2RecWrite_", StringComparison.Ordinal))
                .ToArray();
            MethodInfo[] returnWriters = methods
                .Where(method => method.Name.StartsWith("b2RecWriteRet_", StringComparison.Ordinal))
                .ToArray();
            MethodInfo[] dispatchers = methods
                .Where(method => method.Name.StartsWith("b2RecDispatch_", StringComparison.Ordinal))
                .ToArray();

            Assert.That(argumentWriters, Has.Length.EqualTo(146));
            Assert.That(fullWriters, Has.Length.EqualTo(146));
            Assert.That(returnWriters, Has.Length.EqualTo(14));
            Assert.That(dispatchers, Has.Length.EqualTo(146));

            foreach (MethodInfo method in argumentWriters.Concat(fullWriters))
            {
                string operationName = method.Name.Substring(method.Name.LastIndexOf('_') + 1);
                ParameterInfo[] parameters = method.GetParameters();
                Assert.That(parameters, Has.Length.EqualTo(2), method.Name);
                Assert.That(parameters[0].ParameterType.Name, Is.EqualTo("B2Recording"), method.Name);
                Assert.That(parameters[1].ParameterType.IsByRef, Is.True, method.Name);
                Assert.That(parameters[1].ParameterType.GetElementType().Name,
                    Is.EqualTo("B2RecArgs_" + operationName), method.Name);
                Assert.That(parameters.Any(parameter => parameter.ParameterType == typeof(object[])), Is.False, method.Name);
            }

            foreach (MethodInfo method in returnWriters)
            {
                string operationName = method.Name.Substring(method.Name.LastIndexOf('_') + 1);
                ParameterInfo[] parameters = method.GetParameters();
                Assert.That(parameters, Has.Length.EqualTo(3), method.Name);
                Assert.That(parameters[1].ParameterType.IsByRef, Is.True, method.Name);
                Assert.That(parameters[1].ParameterType.GetElementType().Name,
                    Is.EqualTo("B2RecArgs_" + operationName), method.Name);
                Assert.That(parameters.All(parameter => parameter.ParameterType != typeof(object) && parameter.ParameterType != typeof(object[])),
                    Is.True, method.Name);
            }

            foreach (MethodInfo method in dispatchers)
            {
                string operationName = method.Name.Substring(method.Name.LastIndexOf('_') + 1);
                ParameterInfo[] parameters = method.GetParameters();
                Assert.That(parameters, Has.Length.EqualTo(2), method.Name);
                Assert.That(parameters[0].ParameterType.IsByRef, Is.True, method.Name);
                Assert.That(parameters[0].ParameterType.GetElementType().Name,
                    Is.EqualTo("B2RecArgs_" + operationName), method.Name);
                Assert.That(parameters[1].ParameterType.Name, Is.EqualTo("B2RecReader"), method.Name);
            }
        }

        [Test]
        public void RecordingBufferIsLazyAndPrimitiveWritersUseSpanScratchBuffers()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            Type bufferType = typeof(B2Recordings).Assembly.GetType("Box2D.NET.B2RecBuffer", true);
            object buffer = Activator.CreateInstance(bufferType);

            Assert.That(bufferType.GetField("data", flags).GetValue(buffer), Is.Null);
            Assert.That(bufferType.GetField("capacity", flags).GetValue(buffer), Is.Zero);
            Assert.That(bufferType.GetField("size", flags).GetValue(buffer), Is.Zero);

            MethodInfo append = typeof(B2Recordings).GetMethod("b2RecBufAppend", flags);
            Assert.That(append, Is.Not.Null);
            Assert.That(append.GetParameters()[0].ParameterType.IsByRef, Is.True);
            Assert.That(append.GetParameters()[1].ParameterType, Is.EqualTo(typeof(ReadOnlySpan<byte>)));

            string[] writerNames = { "b2RecW_U8", "b2RecW_U16", "b2RecW_U32", "b2RecW_U64" };
            object[] values = { (byte)0xA5, (ushort)0x1234, 0x89ABCDEFu, 0x0123456789ABCDEFul };
            for (int i = 0; i < writerNames.Length; ++i)
            {
                string writerName = writerNames[i];
                MethodInfo writer = typeof(B2Recordings).GetMethod(writerName, flags);
                Assert.That(writer, Is.Not.Null, writerName);
                Assert.That(writer.GetMethodBody().LocalVariables.Any(v => v.LocalType == typeof(byte[])), Is.False,
                    writerName);
                Assert.That(writer.GetMethodBody().LocalVariables.Any(v => v.LocalType == typeof(Span<byte>)), Is.True,
                    writerName);

                object[] arguments = { buffer, values[i] };
                writer.Invoke(null, arguments);
                buffer = arguments[0];
            }

            byte[] data = (byte[])bufferType.GetField("data", flags).GetValue(buffer);
            int capacity = (int)bufferType.GetField("capacity", flags).GetValue(buffer);
            int size = (int)bufferType.GetField("size", flags).GetValue(buffer);

            Assert.That(capacity, Is.EqualTo(65));
            Assert.That(size, Is.EqualTo(15));
            Assert.That(data.AsSpan(0, size).ToArray(), Is.EqualTo(new byte[]
            {
                0xA5,
                0x34, 0x12,
                0xEF, 0xCD, 0xAB, 0x89,
                0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01,
            }));

            MethodInfo free = typeof(B2Recordings).GetMethod("b2RecBufFree", flags);
            object[] freeArguments = { buffer };
            free.Invoke(null, freeArguments);
            buffer = freeArguments[0];

            Assert.That(bufferType.GetField("data", flags).GetValue(buffer), Is.Null);
            Assert.That(bufferType.GetField("capacity", flags).GetValue(buffer), Is.Zero);
            Assert.That(bufferType.GetField("size", flags).GetValue(buffer), Is.Zero);
        }

        [Test]
        public void RecordingReaderAdvancesPastTruncatedString()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            Type readerType = typeof(B2Recordings).Assembly.GetType("Box2D.NET.B2RecReader", true);
            object reader = Activator.CreateInstance(readerType);
            readerType.GetField("data", flags).SetValue(reader, new byte[] { 5, 0, (byte)'a' });
            readerType.GetField("size", flags).SetValue(reader, 3);
            readerType.GetField("cursor", flags).SetValue(reader, 0);
            readerType.GetField("ok", flags).SetValue(reader, true);

            MethodInfo readString = typeof(B2Recordings).GetMethod("b2RecR_STR", flags);
            object value = readString.Invoke(null, new[] { reader });

            Assert.That(value, Is.EqualTo(string.Empty));
            Assert.That(readerType.GetField("cursor", flags).GetValue(reader), Is.EqualTo(7));
            Assert.That(readerType.GetField("ok", flags).GetValue(reader), Is.False);
        }

        [Test]
        public void RecordingStateTypesDoNotOwnUpstreamOperations()
        {
            Assembly assembly = typeof(B2Recordings).Assembly;
            Type[] stateTypes =
            {
                assembly.GetType("Box2D.NET.B2Recording", true),
                assembly.GetType("Box2D.NET.B2RecBuffer", true),
                assembly.GetType("Box2D.NET.B2RecQueryWriter", true),
                assembly.GetType("Box2D.NET.B2RecReader", true),
                assembly.GetType("Box2D.NET.B2RecDrawQuery", true),
                assembly.GetType("Box2D.NET.B2RecReplayQueryCtx", true),
                typeof(B2RecPlayer),
            };

            foreach (Type stateType in stateTypes)
            {
                MethodInfo[] operations = stateType
                    .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(method => !method.IsSpecialName)
                    .ToArray();

                Assert.That(operations, Is.Empty, stateType.Name);
            }
        }

        [Test]
        public void RecordingQueryWriterMatchesUpstreamFields()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            Type writerType = typeof(B2Recordings).Assembly.GetType("Box2D.NET.B2RecQueryWriter", true);
            Type bufferType = typeof(B2Recordings).Assembly.GetType("Box2D.NET.B2RecBuffer", true);
            Type headerType = typeof(B2Recordings).Assembly.GetType("Box2D.NET.B2RecHeader", true);
            Type drawQueryType = typeof(B2Recordings).Assembly.GetType("Box2D.NET.B2RecDrawQuery", true);
            Type recordedHitType = typeof(B2Recordings).Assembly.GetType("Box2D.NET.B2RecRecordedHit", true);
            Type replayContextType = typeof(B2Recordings).Assembly.GetType("Box2D.NET.B2RecReplayQueryCtx", true);

            Assert.That(writerType.IsValueType, Is.True);
            Assert.That(bufferType.IsValueType, Is.True);
            Assert.That(bufferType.GetFields(flags).Select(field => field.Name), Is.EquivalentTo(new[] { "data", "capacity", "size" }));
            Assert.That(bufferType.GetProperties(flags), Is.Empty);
            Assert.That(drawQueryType.IsValueType, Is.True);
            Assert.That(recordedHitType.IsValueType, Is.True);
            Assert.That(replayContextType.IsValueType, Is.True);
            Assert.That(Marshal.SizeOf(headerType), Is.EqualTo(32));
            Assert.That(headerType.GetFields(flags).Select(field => field.Name), Is.EquivalentTo(new[]
            {
                "magic", "versionMajor", "versionMinor", "buildHash", "simdWidth", "pointerWidth", "bigEndian",
                "reserved0", "wallClockUnix", "reserved",
            }));
            Assert.That(replayContextType, Is.Not.Null);
            Assert.That(typeof(B2Recordings).Assembly.GetType("Box2D.NET.B2RecReplayQueryContext"), Is.Null);

            string[] fieldNames = writerType.GetFields(flags).Select(field => field.Name).ToArray();
            Assert.That(fieldNames, Is.EquivalentTo(new[] { "userFcn", "userContext", "buf", "countOffset", "hitCount" }));

            Type userFcnType = writerType.GetField("userFcn", flags).FieldType;
            Assert.That(userFcnType.Name, Is.EqualTo("B2RecQueryUserFcn"));
            string[] userFcnNames = userFcnType.GetProperties(flags).Select(property => property.Name).ToArray();
            Assert.That(userFcnNames, Is.EquivalentTo(new[] { "overlapFcn", "castFcn", "planeFcn" }));
            Assert.That(userFcnType.GetProperty("castFcn", flags).PropertyType, Is.EqualTo(typeof(Delegate)));

            string[] hitFieldNames = recordedHitType.GetFields(flags).Select(field => field.Name).ToArray();
            Assert.That(hitFieldNames, Is.EquivalentTo(new[]
            {
                "id", "point", "normal", "fraction", "plane", "userReturnF", "userReturnB",
            }));

            string[] drawQueryFieldNames = drawQueryType.GetFields(flags).Select(field => field.Name).ToArray();
            Assert.That(drawQueryFieldNames, Is.EquivalentTo(new[]
            {
                "kind", "filter", "aabb", "proxy", "mover", "origin", "translation", "boolResult",
                "castFraction", "castOut", "shape", "hitStart", "hitCount",
            }));
            Assert.That(drawQueryType.GetField("kind", flags).FieldType, Is.EqualTo(typeof(int)));

            Type readerType = typeof(B2Recordings).Assembly.GetType("Box2D.NET.B2RecReader", true);
            string[] readerFieldNames = readerType.GetFields(flags).Select(field => field.Name).ToArray();
            Assert.That(readerFieldNames, Is.EquivalentTo(new[]
            {
                "data", "size", "cursor", "replayWorldId", "workerCount", "ok", "diverged",
                "chainPoints", "chainPointCap", "chainMaterials", "chainMaterialCap", "hits", "hitCap", "owner",
            }));

            string[] playerFieldNames = typeof(B2RecPlayer).GetFields(flags).Select(field => field.Name).ToArray();
            Assert.That(playerFieldNames, Is.EquivalentTo(new[]
            {
                "data", "size", "headerEnd", "buildHash", "wallClock", "frame", "frameCount", "recordedWorkerCount",
                "recordedDt", "recordedSubStepCount", "divergeFrame", "atEnd", "rdr", "frameQueries", "frameQueryCount",
                "frameQueryCap", "frameHits", "frameHitCount", "frameHitCap", "bodyIds", "bodyIdCount", "bodyIdCap",
            }));

            Type queryKindType = typeof(B2Recordings).Assembly.GetType("Box2D.NET.B2RecQueryKind", true);
            Assert.That(Enum.GetNames(queryKindType), Is.EqualTo(new[]
            {
                "B2_RECQ_OVERLAP_AABB", "B2_RECQ_OVERLAP_SHAPE", "B2_RECQ_CAST_RAY", "B2_RECQ_CAST_SHAPE",
                "B2_RECQ_COLLIDE_MOVER", "B2_RECQ_CAST_RAY_CLOSEST", "B2_RECQ_CAST_MOVER", "B2_RECQ_SHAPE_TEST_POINT",
                "B2_RECQ_SHAPE_RAY_CAST",
            }));
        }

        [Test]
        public void RecordingCastTrampolineKeepsReassignedGenericContext()
        {
            string path = Path.Combine(Path.GetTempPath(), "box2d-net-recording-context-" + Guid.NewGuid().ToString("N") + ".b2rec");
            B2WorldId worldId = b2_nullWorldId;
            try
            {
                B2WorldDef worldDef = b2DefaultWorldDef();
                worldDef.recordingPath = path;
                worldId = b2CreateWorld(worldDef);

                B2ShapeDef shapeDef = b2DefaultShapeDef();
                B2Circle circle = new B2Circle(new B2Vec2(), 0.5f);
                for (int i = 0; i < 2; ++i)
                {
                    B2BodyDef bodyDef = b2DefaultBodyDef();
                    bodyDef.position = new B2Vec2(0.0f, -1.0f + 2.0f * i);
                    B2BodyId bodyId = b2CreateBody(worldId, bodyDef);
                    b2CreateCircleShape(bodyId, shapeDef, circle);
                }

                QueryContext context = new QueryContext();
                b2World_CastRay(worldId, new B2Vec2(0.0f, 4.0f), new B2Vec2(0.0f, -8.0f),
                    b2DefaultQueryFilter(), ReplacingCastFcn, context);

                Assert.That(context.generations, Is.EqualTo(new[] { 0, 1 }));
            }
            finally
            {
                if (b2World_IsValid(worldId))
                {
                    b2DestroyWorld(worldId);
                }
                File.Delete(path);
            }
        }

        [Test]
        public void RecordingRoundTripIsDeterministic()
        {
            string path = Path.Combine(Path.GetTempPath(), "box2d-net-recording-" + Guid.NewGuid().ToString("N") + ".b2rec");
            try
            {
                // Record a session
                B2WorldDef worldDef = b2DefaultWorldDef();
                worldDef.recordingPath = path;
                B2WorldId worldId = b2CreateWorld(worldDef);

                B2BodyDef groundDef = b2DefaultBodyDef();
                B2BodyId groundId = b2CreateBody(worldId, groundDef);
                B2ShapeDef shapeDef = b2DefaultShapeDef();
                B2Polygon groundBox = b2MakeBox(10.0f, 0.5f);
                b2CreatePolygonShape(groundId, shapeDef, groundBox);

                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                bodyDef.position = new B2Vec2(0.0f, 4.0f);
                B2BodyId bodyId = b2CreateBody(worldId, bodyDef);
                B2Circle circle = new B2Circle(new B2Vec2(), 0.5f);
                b2CreateCircleShape(bodyId, shapeDef, circle);

                for (int i = 0; i < 12; ++i)
                {
                    b2World_Step(worldId, 1.0f / 60.0f, 4);
                }

                b2World_StopRecording(worldId);
                b2DestroyWorld(worldId);

                // Verify both files are non-empty
                Assert.That(new FileInfo(path).Length, Is.GreaterThan(32));

                // Replay with recorded worker count
                Assert.That(b2ValidateReplayFile(path, 0), Is.True);

                B2RecPlayer player = b2RecPlayer_Create(path, 0);
                Assert.That(player, Is.Not.Null);
                Assert.That(b2RecPlayer_GetInfo(player).frameCount, Is.EqualTo(12));
                b2RecPlayer_SeekFrame(player, 6);
                Assert.That(b2RecPlayer_GetFrame(player), Is.EqualTo(6));
                Assert.That(b2RecPlayer_HasDiverged(player), Is.False);
                b2RecPlayer_Destroy(player);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void SaveRecordingCopiesAnActiveSession()
        {
            string source = Path.Combine(Path.GetTempPath(), "box2d-net-live-" + Guid.NewGuid().ToString("N") + ".b2rec");
            string snapshot = Path.Combine(Path.GetTempPath(), "box2d-net-snapshot-" + Guid.NewGuid().ToString("N") + ".b2rec");
            try
            {
                B2WorldDef worldDef = b2DefaultWorldDef();
                worldDef.recordingPath = source;
                B2WorldId worldId = b2CreateWorld(worldDef);
                b2World_Step(worldId, 1.0f / 60.0f, 4);
                b2World_SaveRecording(worldId, snapshot);
                b2World_StopRecording(worldId);
                b2DestroyWorld(worldId);

                Assert.That(b2ValidateReplayFile(snapshot, 0), Is.True);
            }
            finally
            {
                if (File.Exists(source)) File.Delete(source);
                if (File.Exists(snapshot)) File.Delete(snapshot);
            }
        }

        [Test]
        public void SpatialQueriesRoundTripAndDraw()
        {
            string path = Path.Combine(Path.GetTempPath(), "box2d-net-queries-" + Guid.NewGuid().ToString("N") + ".b2rec");
            try
            {
                B2WorldDef worldDef = b2DefaultWorldDef();
                worldDef.gravity = new B2Vec2(0.0f, -10.0f);
                worldDef.recordingPath = path;
                B2WorldId worldId = b2CreateWorld(worldDef);

                B2BodyDef groundDef = b2DefaultBodyDef();
                groundDef.position = new B2Vec2(0.0f, -10.0f);
                B2BodyId groundId = b2CreateBody(worldId, groundDef);
                B2ShapeDef shapeDef = b2DefaultShapeDef();
                B2ShapeId groundShapeId = b2CreateCircleShape(groundId, shapeDef, new B2Circle(new B2Vec2(), 10.0f));

                b2World_Step(worldId, 1.0f / 60.0f, 4);

                IssueAllQueries(worldId, groundShapeId);

                b2World_Step(worldId, 1.0f / 60.0f, 4);
                b2World_StopRecording(worldId);
                b2DestroyWorld(worldId);

                B2RecPlayer player = b2RecPlayer_Create(path, 0);
                Assert.That(player, Is.Not.Null);
                Assert.That(b2RecPlayer_StepFrame(player), Is.True);
                Assert.That(b2RecPlayer_GetFrameQueryCount(player), Is.EqualTo(10));

                for (int i = 0; i < b2RecPlayer_GetFrameQueryCount(player); ++i)
                {
                    B2RecQueryInfo query = b2RecPlayer_GetFrameQuery(player, i);
                    Assert.That((int)query.type, Is.InRange(0, 8));
                    if (query.type == B2RecQueryType.b2_recQueryShapeRayCast)
                    {
                        // The shape-local cast result is kept in the draw record, not exposed as a query hit.
                        Assert.That(query.hitCount, Is.Zero);
                    }
                    for (int j = 0; j < query.hitCount; ++j)
                    {
                        b2RecPlayer_GetFrameQueryHit(player, i, j);
                    }
                }

                DrawContext drawContext = new DrawContext();
                B2DebugDraw draw = b2DefaultDebugDraw();
                draw.DrawLineFcn = DrawLine;
                draw.DrawPointFcn = DrawPoint;
                draw.DrawPolygonFcn = DrawPolygon;
                draw.DrawSolidCapsuleFcn = DrawCapsule;
                draw.context = drawContext;
                b2RecPlayer_DrawFrameQueries(player, draw, -1);
                Assert.That(drawContext.callCount, Is.GreaterThan(0));
                Assert.That(b2RecPlayer_HasDiverged(player), Is.False);

                // Restart does not replay a frame, so inspection still reports the most recently replayed
                // frame until StepFrame resets the per-frame query store.
                b2RecPlayer_Restart(player);
                Assert.That(b2RecPlayer_GetFrameQueryCount(player), Is.EqualTo(10));
                Assert.That(b2RecPlayer_StepFrame(player), Is.True);
                Assert.That(b2RecPlayer_GetFrameQueryCount(player), Is.EqualTo(10));

                Assert.That(b2RecPlayer_StepFrame(player), Is.True);
                Assert.That(b2RecPlayer_IsAtEnd(player), Is.True);
                Assert.That(b2RecPlayer_HasDiverged(player), Is.False);

                // The trailing DestroyWorld is an end marker; the world stays valid so a viewer can keep
                // drawing the final step rather than blanking at the end
                Assert.That(b2World_IsValid(b2RecPlayer_GetWorldId(player)), Is.True);
                b2RecPlayer_Destroy(player);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void RecordedMutatorCoverageMatchesUpstream()
        {
            string path = Path.Combine(Path.GetTempPath(), "box2d-net-mutators-" + Guid.NewGuid().ToString("N") + ".b2rec");
            string snapshot = Path.Combine(Path.GetTempPath(), "box2d-net-mutators-saved-" + Guid.NewGuid().ToString("N") + ".b2rec");
            try
            {
                B2WorldDef worldDef = b2DefaultWorldDef();
                worldDef.gravity = new B2Vec2(0.0f, -10.0f);
                worldDef.workerCount = 1;
                worldDef.recordingPath = path;
                B2WorldId worldId = b2CreateWorld(worldDef);

                // Static ground body with a circle shape
                B2BodyDef groundDef = b2DefaultBodyDef();
                groundDef.position = new B2Vec2(0.0f, -10.0f);
                B2BodyId groundId = b2CreateBody(worldId, groundDef);
                B2ShapeDef groundShapeDef = b2DefaultShapeDef();
                B2ShapeId groundShapeId = b2CreateCircleShape(groundId, groundShapeDef,
                    new B2Circle(new B2Vec2(), 10.0f));

                // Dynamic body with a circle shape. The name is intentionally longer than B2_NAME_LENGTH and
                // the recording writer's stack buffer so replay exercises both over-length paths.
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                bodyDef.position = new B2Vec2(0.0f, 4.0f);
                bodyDef.name = new string('n', 300);
                B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

                B2ShapeDef shapeDef = b2DefaultShapeDef();
                shapeDef.density = 1.0f;
                B2ShapeId shapeId = b2CreateCircleShape(bodyId, shapeDef,
                    new B2Circle(new B2Vec2(), 0.5f));

                // Polygon shape on the dynamic body
                B2Polygon box = b2MakeBox(0.25f, 0.25f);
                B2ShapeDef boxDef = b2DefaultShapeDef();
                boxDef.density = 2.0f;
                B2ShapeId boxShapeId = b2CreatePolygonShape(bodyId, boxDef, box);

                // Capsule on a second dynamic body
                B2BodyDef capsuleBodyDef = b2DefaultBodyDef();
                capsuleBodyDef.type = B2BodyType.b2_dynamicBody;
                capsuleBodyDef.position = new B2Vec2(2.0f, 6.0f);
                B2BodyId capsuleBodyId = b2CreateBody(worldId, capsuleBodyDef);
                B2Capsule capsule = new B2Capsule(new B2Vec2(-0.5f, 0.0f), new B2Vec2(0.5f, 0.0f), 0.25f);
                B2ShapeDef capsuleDef = b2DefaultShapeDef();
                capsuleDef.density = 1.0f;
                B2ShapeId capsuleShapeId = b2CreateCapsuleShape(capsuleBodyId, capsuleDef, capsule);

                // Static segment extending the ground
                B2Segment segment = new B2Segment(new B2Vec2(-20.0f, 0.0f), new B2Vec2(20.0f, 0.0f));
                B2ShapeDef segmentDef = b2DefaultShapeDef();
                b2CreateSegmentShape(groundId, segmentDef, segment);

                // Chain segment shape on the ground
                B2ChainSegment chainSegment = new B2ChainSegment
                {
                    ghost1 = new B2Vec2(-2.0f, 1.0f),
                    segment = new B2Segment(new B2Vec2(-1.0f, 1.0f), new B2Vec2(1.0f, 1.0f)),
                    ghost2 = new B2Vec2(2.0f, 1.0f),
                    chainId = -1,
                };
                b2CreateChainSegmentShape(groundId, segmentDef, chainSegment);

                // Exercise the recorded shape mutators
                b2Shape_SetFriction(boxShapeId, 0.3f);
                b2Shape_SetRestitution(capsuleShapeId, 0.5f);
                b2Shape_SetDensity(boxShapeId, 3.0f, true);
                b2Shape_SetUserMaterial(boxShapeId, 0x1234u);
                B2SurfaceMaterial surface = b2DefaultSurfaceMaterial();
                surface.friction = 0.7f;
                surface.restitution = 0.1f;
                b2Shape_SetSurfaceMaterial(capsuleShapeId, surface);
                B2Filter shapeFilter = b2DefaultFilter();
                shapeFilter.categoryBits = 0x2;
                b2Shape_SetFilter(boxShapeId, shapeFilter);
                b2Shape_EnableContactEvents(capsuleShapeId, true);
                b2Shape_EnableSensorEvents(capsuleShapeId, true);
                b2Shape_EnableHitEvents(boxShapeId, true);
                b2Shape_EnablePreSolveEvents(boxShapeId, true);
                b2Shape_ApplyWind(capsuleShapeId, new B2Vec2(1.0f, 0.0f), 0.1f, 0.0f, true);

                // Change geometry in place
                B2Circle newCircle = new B2Circle(new B2Vec2(), 0.4f);
                b2Shape_SetCircle(shapeId, newCircle);
                B2Capsule newCapsule = new B2Capsule(new B2Vec2(-0.4f, 0.0f), new B2Vec2(0.4f, 0.0f), 0.2f);
                b2Shape_SetCapsule(capsuleShapeId, newCapsule);
                B2Segment newSegment = new B2Segment(new B2Vec2(-19.0f, 0.0f), new B2Vec2(19.0f, 0.0f));
                B2ShapeId mutableSegmentId = b2CreateSegmentShape(groundId, segmentDef, segment);
                b2Shape_SetSegment(mutableSegmentId, newSegment);
                B2Polygon newBox = b2MakeBox(0.3f, 0.2f);
                b2Shape_SetPolygon(boxShapeId, ref newBox);
                B2ShapeId mutableChainSegmentId = b2CreateChainSegmentShape(groundId, segmentDef, chainSegment);
                chainSegment.ghost1 = new B2Vec2(-2.5f, 1.0f);
                b2Shape_SetChainSegment(mutableChainSegmentId, chainSegment);

                // Throwaway shape to exercise DestroyShape
                B2ShapeId temporaryShapeId = b2CreateCircleShape(capsuleBodyId, capsuleDef,
                    new B2Circle(new B2Vec2(), 0.1f));
                b2DestroyShape(temporaryShapeId, true);

                // A kinematic body to exercise SetType and SetTargetTransform
                B2BodyDef kinematicDef = b2DefaultBodyDef();
                kinematicDef.type = B2BodyType.b2_kinematicBody;
                kinematicDef.position = new B2Vec2(-3.0f, 5.0f);
                B2BodyId kinematicId = b2CreateBody(worldId, kinematicDef);
                b2CreateCircleShape(kinematicId, b2DefaultShapeDef(), new B2Circle(new B2Vec2(), 0.3f));

                // A body to exercise Disable/Enable
                B2BodyDef disableDef = b2DefaultBodyDef();
                disableDef.type = B2BodyType.b2_dynamicBody;
                disableDef.position = new B2Vec2(5.0f, 5.0f);
                B2BodyId disableId = b2CreateBody(worldId, disableDef);
                b2CreateCircleShape(disableId, shapeDef, new B2Circle(new B2Vec2(), 0.3f));

                // Exercise the recorded body mutators
                b2Body_SetTransform(bodyId, new B2Vec2(1.0f, 5.0f), b2Rot_identity);
                b2Body_SetLinearVelocity(bodyId, new B2Vec2(0.5f, 0.0f));
                b2Body_SetAngularVelocity(bodyId, 0.25f);
                b2Body_SetName(bodyId, "renamedBody");
                b2Body_SetLinearDamping(bodyId, 0.1f);
                b2Body_SetAngularDamping(bodyId, 0.05f);
                b2Body_SetGravityScale(bodyId, 0.9f);
                b2Body_SetSleepThreshold(bodyId, 0.02f);
                b2Body_EnableSleep(bodyId, false);
                b2Body_SetBullet(bodyId, true);
                b2Body_EnableContactRecycling(bodyId, false);
                b2Body_EnableContactEvents(bodyId, true);
                b2Body_EnableHitEvents(bodyId, true);
                b2Body_SetMotionLocks(bodyId, new B2MotionLocks(false, false, true));
                b2Body_SetMassData(bodyId, new B2MassData(2.0f, new B2Vec2(), 0.5f));
                b2Body_ApplyMassFromShapes(bodyId);
                b2Body_SetType(capsuleBodyId, B2BodyType.b2_kinematicBody);
                b2Body_SetType(capsuleBodyId, B2BodyType.b2_dynamicBody);
                b2Body_SetTargetTransform(kinematicId,
                    new B2Transform(new B2Vec2(-2.0f, 5.0f), b2Rot_identity), 1.0f / 60.0f, true);
                b2Body_Disable(disableId);
                b2Body_Enable(disableId);
                b2Body_SetAwake(bodyId, true);
                b2Body_WakeTouching(bodyId);

                // Per-step forces and impulses applied before the first step
                b2Body_ApplyForce(bodyId, new B2Vec2(0.0f, 50.0f), new B2Vec2(1.0f, 5.0f), true);
                b2Body_ApplyForceToCenter(bodyId, new B2Vec2(5.0f, 0.0f), true);
                b2Body_ApplyTorque(bodyId, 1.0f, true);
                b2Body_ApplyLinearImpulse(bodyId, new B2Vec2(0.1f, 0.0f), new B2Vec2(1.0f, 5.0f), true);
                b2Body_ApplyLinearImpulseToCenter(bodyId, new B2Vec2(0.0f, 0.1f), true);
                b2Body_ApplyAngularImpulse(bodyId, 0.05f, true);

                // Chain shape on a static body, plus a material change and a throwaway chain destroyed
                B2BodyDef chainBodyDef = b2DefaultBodyDef();
                chainBodyDef.position = new B2Vec2(0.0f, -2.0f);
                B2BodyId chainBodyId = b2CreateBody(worldId, chainBodyDef);
                B2ChainDef chainDef = b2DefaultChainDef();
                chainDef.points = new[]
                {
                    new B2Vec2(-8.0f, 0.0f), new B2Vec2(-4.0f, 0.0f), new B2Vec2(0.0f, 0.0f),
                    new B2Vec2(4.0f, 0.0f), new B2Vec2(8.0f, 0.0f), new B2Vec2(8.0f, 4.0f),
                };
                chainDef.count = chainDef.points.Length;
                chainDef.materials = new[] { b2DefaultSurfaceMaterial() };
                chainDef.materialCount = 1;
                chainDef.isLoop = false;
                B2ChainId chainId = b2CreateChain(chainBodyId, chainDef);

                B2SurfaceMaterial chainSurface = b2DefaultSurfaceMaterial();
                chainSurface.friction = 0.4f;
                b2Chain_SetSurfaceMaterial(chainId, chainSurface, 0);
                B2ChainId temporaryChainId = b2CreateChain(chainBodyId, chainDef);
                b2DestroyChain(temporaryChainId);

                // Joints: a row of dynamic bodies connected by each joint type
                B2BodyId[] jointBodies = new B2BodyId[8];
                for (int i = 0; i < jointBodies.Length; ++i)
                {
                    B2BodyDef jointBodyDef = b2DefaultBodyDef();
                    jointBodyDef.type = B2BodyType.b2_dynamicBody;
                    jointBodyDef.position = new B2Vec2(-7.0f + i, 8.0f);
                    jointBodies[i] = b2CreateBody(worldId, jointBodyDef);
                    b2CreateCircleShape(jointBodies[i], shapeDef, new B2Circle(new B2Vec2(), 0.25f));
                }

                // Revolute joint with full setter coverage and the generic mutators
                B2RevoluteJointDef revoluteDef = b2DefaultRevoluteJointDef();
                revoluteDef.@base.bodyIdA = jointBodies[0];
                revoluteDef.@base.bodyIdB = jointBodies[1];
                revoluteDef.@base.localFrameA.p = new B2Vec2(0.5f, 0.0f);
                revoluteDef.@base.localFrameB.p = new B2Vec2(-0.5f, 0.0f);
                B2JointId revoluteId = b2CreateRevoluteJoint(worldId, revoluteDef);
                b2RevoluteJoint_EnableLimit(revoluteId, true);
                b2RevoluteJoint_SetLimits(revoluteId, -1.0f, 1.0f);
                b2RevoluteJoint_EnableMotor(revoluteId, true);
                b2RevoluteJoint_SetMotorSpeed(revoluteId, 0.5f);
                b2RevoluteJoint_SetMaxMotorTorque(revoluteId, 10.0f);
                b2RevoluteJoint_EnableSpring(revoluteId, true);
                b2RevoluteJoint_SetSpringHertz(revoluteId, 2.0f);
                b2RevoluteJoint_SetSpringDampingRatio(revoluteId, 0.5f);
                b2RevoluteJoint_SetTargetAngle(revoluteId, 0.25f);
                b2Joint_SetLocalFrameA(revoluteId,
                    new B2Transform(new B2Vec2(0.5f, 0.0f), b2Rot_identity));
                b2Joint_SetLocalFrameB(revoluteId,
                    new B2Transform(new B2Vec2(-0.5f, 0.0f), b2Rot_identity));
                b2Joint_SetConstraintTuning(revoluteId, 60.0f, 2.0f);
                b2Joint_SetForceThreshold(revoluteId, 100.0f);
                b2Joint_SetTorqueThreshold(revoluteId, 50.0f);
                b2Joint_SetCollideConnected(revoluteId, false);
                b2Joint_WakeBodies(revoluteId);

                // Distance joint
                B2DistanceJointDef distanceDef = b2DefaultDistanceJointDef();
                distanceDef.@base.bodyIdA = jointBodies[1];
                distanceDef.@base.bodyIdB = jointBodies[2];
                distanceDef.length = 1.0f;
                B2JointId distanceId = b2CreateDistanceJoint(worldId, distanceDef);
                b2DistanceJoint_SetLength(distanceId, 1.2f);
                b2DistanceJoint_EnableSpring(distanceId, true);
                b2DistanceJoint_SetSpringHertz(distanceId, 3.0f);
                b2DistanceJoint_SetSpringDampingRatio(distanceId, 0.4f);
                b2DistanceJoint_SetSpringForceRange(distanceId, -50.0f, 50.0f);
                b2DistanceJoint_EnableLimit(distanceId, true);
                b2DistanceJoint_SetLengthRange(distanceId, 0.5f, 2.0f);
                b2DistanceJoint_EnableMotor(distanceId, true);
                b2DistanceJoint_SetMotorSpeed(distanceId, 0.3f);
                b2DistanceJoint_SetMaxMotorForce(distanceId, 5.0f);

                // Prismatic joint
                B2PrismaticJointDef prismaticDef = b2DefaultPrismaticJointDef();
                prismaticDef.@base.bodyIdA = jointBodies[2];
                prismaticDef.@base.bodyIdB = jointBodies[3];
                B2JointId prismaticId = b2CreatePrismaticJoint(worldId, prismaticDef);
                b2PrismaticJoint_EnableSpring(prismaticId, true);
                b2PrismaticJoint_SetSpringHertz(prismaticId, 2.0f);
                b2PrismaticJoint_SetSpringDampingRatio(prismaticId, 0.5f);
                b2PrismaticJoint_SetTargetTranslation(prismaticId, 0.1f);
                b2PrismaticJoint_EnableLimit(prismaticId, true);
                b2PrismaticJoint_SetLimits(prismaticId, -1.0f, 1.0f);
                b2PrismaticJoint_EnableMotor(prismaticId, true);
                b2PrismaticJoint_SetMotorSpeed(prismaticId, 0.2f);
                b2PrismaticJoint_SetMaxMotorForce(prismaticId, 8.0f);

                // Wheel joint
                B2WheelJointDef wheelDef = b2DefaultWheelJointDef();
                wheelDef.@base.bodyIdA = jointBodies[3];
                wheelDef.@base.bodyIdB = jointBodies[4];
                B2JointId wheelId = b2CreateWheelJoint(worldId, wheelDef);
                b2WheelJoint_EnableSpring(wheelId, true);
                b2WheelJoint_SetSpringHertz(wheelId, 4.0f);
                b2WheelJoint_SetSpringDampingRatio(wheelId, 0.7f);
                b2WheelJoint_EnableLimit(wheelId, true);
                b2WheelJoint_SetLimits(wheelId, -0.5f, 0.5f);
                b2WheelJoint_EnableMotor(wheelId, true);
                b2WheelJoint_SetMotorSpeed(wheelId, 1.0f);
                b2WheelJoint_SetMaxMotorTorque(wheelId, 6.0f);

                // Weld joint
                B2WeldJointDef weldDef = b2DefaultWeldJointDef();
                weldDef.@base.bodyIdA = jointBodies[4];
                weldDef.@base.bodyIdB = jointBodies[5];
                B2JointId weldId = b2CreateWeldJoint(worldId, weldDef);
                b2WeldJoint_SetLinearHertz(weldId, 5.0f);
                b2WeldJoint_SetLinearDampingRatio(weldId, 0.6f);
                b2WeldJoint_SetAngularHertz(weldId, 5.0f);
                b2WeldJoint_SetAngularDampingRatio(weldId, 0.6f);

                // Motor joint
                B2MotorJointDef motorDef = b2DefaultMotorJointDef();
                motorDef.@base.bodyIdA = jointBodies[5];
                motorDef.@base.bodyIdB = jointBodies[6];
                B2JointId motorId = b2CreateMotorJoint(worldId, motorDef);
                b2MotorJoint_SetLinearVelocity(motorId, new B2Vec2(0.1f, 0.0f));
                b2MotorJoint_SetAngularVelocity(motorId, 0.2f);
                b2MotorJoint_SetMaxVelocityForce(motorId, 10.0f);
                b2MotorJoint_SetMaxVelocityTorque(motorId, 10.0f);
                b2MotorJoint_SetLinearHertz(motorId, 2.0f);
                b2MotorJoint_SetLinearDampingRatio(motorId, 0.5f);
                b2MotorJoint_SetAngularHertz(motorId, 2.0f);
                b2MotorJoint_SetAngularDampingRatio(motorId, 0.5f);
                b2MotorJoint_SetMaxSpringForce(motorId, 20.0f);
                b2MotorJoint_SetMaxSpringTorque(motorId, 20.0f);

                // Filter joint, plus a throwaway joint to exercise DestroyJoint
                B2FilterJointDef filterDef = b2DefaultFilterJointDef();
                filterDef.@base.bodyIdA = jointBodies[6];
                filterDef.@base.bodyIdB = jointBodies[7];
                b2CreateFilterJoint(worldId, filterDef);

                B2DistanceJointDef temporaryJointDef = b2DefaultDistanceJointDef();
                temporaryJointDef.@base.bodyIdA = jointBodies[0];
                temporaryJointDef.@base.bodyIdB = jointBodies[7];
                temporaryJointDef.length = 5.0f;
                B2JointId temporaryJointId = b2CreateDistanceJoint(worldId, temporaryJointDef);
                b2DestroyJoint(temporaryJointId, true);

                // Exercise world config mutators
                b2World_SetGravity(worldId, new B2Vec2(0.0f, -9.8f));
                b2World_EnableSleeping(worldId, true);
                b2World_EnableContinuous(worldId, true);
                b2World_EnableWarmStarting(worldId, true);
                b2World_EnableSpeculative(worldId, true);
                b2World_SetRestitutionThreshold(worldId, 1.5f);
                b2World_SetHitEventThreshold(worldId, 2.0f);
                b2World_SetContactTuning(worldId, 30.0f, 10.0f, 3.0f);
                b2World_SetContactRecycleDistance(worldId, 0.05f);
                b2World_SetMaximumLinearSpeed(worldId, 100.0f);
                b2World_RebuildStaticTree(worldId);
                B2ExplosionDef explosion = b2DefaultExplosionDef();
                explosion.position = new B2Vec2(0.0f, 4.0f);
                explosion.radius = 3.0f;
                explosion.falloff = 1.0f;
                explosion.impulsePerLength = 5.0f;
                b2World_Explode(worldId, explosion);

                // Issue all 9 query types before the first step (pre-step path)
                IssueAllQueries(worldId, groundShapeId);

                float timeStep = 1.0f / 60.0f;
                int subStepCount = 4;
                for (int i = 0; i < 60; ++i)
                {
                    // Inject mutators mid-simulation to exercise interleaving with steps
                    if (i == 30)
                    {
                        b2Body_ApplyLinearImpulseToCenter(capsuleBodyId, new B2Vec2(2.0f, 0.0f), true);
                        b2Body_ClearForces(bodyId);
                        b2Body_SetGravityScale(bodyId, 1.0f);
                    }

                    // Also issue queries mid-loop to exercise recording across steps
                    if (i == 15)
                    {
                        IssueAllQueries(worldId, groundShapeId);
                    }

                    b2World_Step(worldId, timeStep, subStepCount);
                }

                // Save a mid-recording snapshot, must replay deterministically too
                b2World_SaveRecording(worldId, snapshot);

                b2World_StopRecording(worldId);
                b2DestroyWorld(worldId);

                // Verify both files are non-empty
                Assert.That(new FileInfo(path).Length, Is.GreaterThan(0));
                Assert.That(new FileInfo(snapshot).Length, Is.GreaterThan(0));

                // Replay with recorded worker count
                Assert.That(b2ValidateReplayFile(path, 0), Is.True);

                // Replay with a different worker count to prove cross-thread determinism
                Assert.That(b2ValidateReplayFile(path, 4), Is.True);

                // The saved snapshot must replay deterministically at both worker counts
                Assert.That(b2ValidateReplayFile(snapshot, 0), Is.True);
                Assert.That(b2ValidateReplayFile(snapshot, 4), Is.True);

                // Drive the incremental player directly. It underpins the viewer and exercises
                // per-frame stepping, restart, and the getters beyond what b2ReplayFile covers.
                B2RecPlayer player = b2RecPlayer_Create(path, 0);
                Assert.That(player, Is.Not.Null);

                // Build a no-op b2DebugDraw to exercise the draw path headlessly
                DrawContext drawContext = new DrawContext();
                B2DebugDraw draw = b2DefaultDebugDraw();
                draw.DrawLineFcn = DrawLine;
                draw.DrawPointFcn = DrawPoint;
                draw.DrawPolygonFcn = DrawPolygon;
                draw.DrawSolidCapsuleFcn = DrawCapsule;
                draw.context = drawContext;

                int frames = 0;
                while (b2RecPlayer_StepFrame(player))
                {
                    // Exercise the draw path on every other frame
                    if (frames % 2 == 0)
                    {
                        b2RecPlayer_DrawFrameQueries(player, draw, -1);
                    }
                    frames += 1;
                }
                Assert.That(frames, Is.EqualTo(60));
                Assert.That(b2RecPlayer_GetFrame(player), Is.EqualTo(60));
                Assert.That(b2RecPlayer_IsAtEnd(player), Is.True);
                Assert.That(b2RecPlayer_HasDiverged(player), Is.False);

                // The trailing DestroyWorld is an end marker; the world stays valid so a viewer can keep
                // drawing the final step rather than blanking at the end
                Assert.That(b2World_IsValid(b2RecPlayer_GetWorldId(player)), Is.True);

                // Restart reproduces the same run without reloading the file
                b2RecPlayer_Restart(player);
                Assert.That(b2RecPlayer_GetFrame(player), Is.EqualTo(0));
                Assert.That(b2RecPlayer_IsAtEnd(player), Is.False);

                int frames2 = 0;
                while (b2RecPlayer_StepFrame(player))
                {
                    frames2 += 1;
                }
                Assert.That(frames2, Is.EqualTo(60));
                Assert.That(b2RecPlayer_HasDiverged(player), Is.False);

                b2RecPlayer_Destroy(player);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(snapshot)) File.Delete(snapshot);
            }
        }
    }
}
