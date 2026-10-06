// SPDX-License-Identifier: MIT

using System;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

namespace Box2D.NET.PhysicsBenchmark;

public static class DenseBenchmark
{
    public static int Run(string[] args)
    {

        const int count = 1536, warmup = 1600, samples = 1024;
        var definition = b2DefaultWorldDef();
        definition.gravity = new(0, 9.8f);
        definition.restitutionThreshold = 0;
        var workers = args.Length <= 1 ? 1 : int.Parse(args[1]);
        if (workers < 1 || workers > Box2D.NET.B2Constants.B2_MAX_WORKERS)
            throw new ArgumentOutOfRangeException(nameof(workers));
        definition.workerCount = workers;
        var world = b2CreateWorld(definition);
        try
        {
            var ground = b2CreateBody(world, b2DefaultBodyDef());
            var surface = b2DefaultShapeDef();
            surface.material.friction = .25f;
            surface.material.restitution = .35f;
            foreach (var (x, y, hx, hy) in new (float, float, float, float)[]
                { (5.76f, 6.38f, 5.4f, .09f), (.38f, 4.2f, .09f, 2.16f), (11.14f, 4.2f, .09f, 2.16f), (5.76f, 1.9f, 5.4f, .06f) })
            {
                var polygon = b2MakeOffsetBox(hx, hy, new B2Vec2(x, y), new B2Rot(1, 0));
                b2CreatePolygonShape(ground, surface, polygon);
            }
            var bodies = new B2BodyId[count];
            var shape = b2DefaultShapeDef();
            shape.density = .2f / (MathF.PI * .06f * .06f);
            shape.material.friction = .25f;
            shape.material.restitution = .35f;
            var circle = new B2Circle { radius = .06f };
            for (var i = 0; i < count; i++)
            {
                var body = b2DefaultBodyDef();
                body.type = B2BodyType.b2_dynamicBody;
                body.position = new(.65f + i % 50 * .2f, 2.16f + i / 50 * .125f);
                body.enableSleep = false;
                body.linearVelocity = new((i % 7 - 3) * .03f, 0);
                body.linearDamping = .05f;
                body.angularDamping = .1f;
                bodies[i] = b2CreateBody(world, body);
                b2CreateCircleShape(bodies[i], shape, circle);
            }
            for (var i = 0; i < warmup; i++)
                b2World_Step(world, 1f / 144, 4);
            var times = new double[samples];
            double collide = 0, solve = 0, pairs = 0, prepare = 0, impulses = 0, relax = 0, transforms = 0;
            var totalAllocated = GC.GetTotalAllocatedBytes(true);
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < samples; i++)
            {
                var start = Stopwatch.GetTimestamp();
                b2World_Step(world, 1f / 144, 4);
                times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                var p = b2World_GetProfile(world);
                collide += p.collide;
                solve += p.solve;
                pairs += p.pairs;
                prepare += p.prepareConstraints;
                impulses += p.solveImpulses;
                relax += p.relaxImpulses;
                transforms += p.transforms;
            }
            var bytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
            var allThreadsBytes = GC.GetTotalAllocatedBytes(true) - totalAllocated;
            var states = new float[count * 7];
            for (var i = 0; i < count; i++)
            {
                var t = b2Body_GetTransform(bodies[i]);
                var v = b2Body_GetLinearVelocity(bodies[i]);
                if (!float.IsFinite(t.p.X) || !float.IsFinite(t.p.Y) || t.p.Y > 6.4f || t.p.Y < 1.85f)
                    throw new Exception("Non-finite body transform.");
                states[i * 7] = t.p.X;
                states[i * 7 + 1] = t.p.Y;
                states[i * 7 + 2] = t.q.c;
                states[i * 7 + 3] = t.q.s;
                states[i * 7 + 4] = v.X;
                states[i * 7 + 5] = v.Y;
                states[i * 7 + 6] = b2Body_GetAngularVelocity(bodies[i]);
            }
            var counters = b2World_GetCounters(world);
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                runtime = RuntimeInformation.FrameworkDescription,
                hardwareSIMD = System.Runtime.Intrinsics.Vector256.IsHardwareAccelerated,
                lanes = Box2D.NET.B2Cores.B2_SIMD_WIDTH,
                bodies = count,
                contacts = counters.contactCount,
                touchingContacts = counters.colorCounts.AsSpan().ToArray().Sum(),
                workers = b2GetWorldFromId(world).workerCount,
                hz = 144,
                substeps = 4,
                warmup,
                samples,
                meanMS = times.Average(),
                p95MS = times.Order().ElementAt((int)(samples * .95)),
                p99MS = times.Order().ElementAt((int)(samples * .99)),
                bytes,
                allThreadsBytes,
                stateSHA256 = Convert.ToHexString(SHA256.HashData(MemoryMarshal.AsBytes(states.AsSpan()))),
                collideMS = collide / samples,
                solveMS = solve / samples,
                pairsMS = pairs / samples,
                prepareMS = prepare / samples,
                impulsesMS = impulses / samples,
                relaxMS = relax / samples,
                transformsMS = transforms / samples
            }));
        }
        finally { b2DestroyWorld(world); }

        return 0;
    }
}
