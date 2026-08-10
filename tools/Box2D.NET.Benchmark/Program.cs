// SPDX-FileCopyrightText: 2024 Erin Catto
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Box2D.NET;
using Box2D.NET.Shared;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Timers;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.Shared.Benchmarks;

namespace Box2D.NET.PhysicsBenchmark
{
    public static class Program
    {
        private delegate object CreateFcn(B2WorldId worldId);
        private delegate float StepFcn(object state, B2WorldId worldId, int stepCount);

        private sealed class Benchmark
        {
            public Benchmark(string name, CreateFcn createFcn, StepFcn stepFcn, int totalStepCount)
            {
                Name = name;
                Create = createFcn;
                Step = stepFcn;
                TotalStepCount = totalStepCount;
            }

            public string Name { get; }
            public CreateFcn Create { get; }
            public StepFcn Step { get; }
            public int TotalStepCount { get; }
        }

        public static int Main(string[] args)
        {
            Benchmark[] benchmarks = CreateBenchmarks();

            int maxSteps = benchmarks[0].TotalStepCount;
            for (int i = 1; i < benchmarks.Length; ++i)
            {
                maxSteps = b2MaxInt(maxSteps, benchmarks[i].TotalStepCount);
            }

            B2Profile maxProfile = CreateMaxProfile();
            B2Profile[] profiles = new B2Profile[maxSteps];
            for (int i = 0; i < maxSteps; ++i)
            {
                profiles[i] = maxProfile;
            }

            float[] stepResults = new float[maxSteps];

            int maxThreadCount = b2MinInt(Environment.ProcessorCount, B2_MAX_WORKERS);
            int runCount = 4;
            int singleBenchmark = -1;
            int singleWorkerCount = -1;
            B2Counters counters = new B2Counters();
            bool enableContinuous = true;
            bool recordStepTimes = false;

            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i];
                if (arg.StartsWith("-t=", StringComparison.Ordinal))
                {
                    int threadCount = ParseInteger(arg.AsSpan(3));
                    maxThreadCount = b2ClampInt(threadCount, 1, maxThreadCount);
                }
                else if (arg.StartsWith("-b=", StringComparison.Ordinal))
                {
                    singleBenchmark = ParseInteger(arg.AsSpan(3));
                    singleBenchmark = b2ClampInt(singleBenchmark, 0, benchmarks.Length - 1);
                }
                else if (arg.StartsWith("-w=", StringComparison.Ordinal))
                {
                    singleWorkerCount = ParseInteger(arg.AsSpan(3));
                }
                else if (arg.StartsWith("-r=", StringComparison.Ordinal))
                {
                    runCount = b2ClampInt(ParseInteger(arg.AsSpan(3)), 1, 1000);
                }
                else if (arg.StartsWith("-nc", StringComparison.Ordinal))
                {
                    enableContinuous = false;
                    Console.WriteLine("Continuous disabled");
                }
                else if (string.Equals(arg, "-s", StringComparison.Ordinal))
                {
                    recordStepTimes = true;
                }
                else if (string.Equals(arg, "-h", StringComparison.Ordinal))
                {
                    PrintUsage();
                    return 0;
                }
            }

            if (singleWorkerCount != -1)
            {
                singleWorkerCount = b2ClampInt(singleWorkerCount, 1, maxThreadCount);
            }

            Console.WriteLine("Starting Box2D benchmarks");
            Console.WriteLine("======================================");

            for (int benchmarkIndex = 0; benchmarkIndex < benchmarks.Length; ++benchmarkIndex)
            {
                if (singleBenchmark != -1 && benchmarkIndex != singleBenchmark)
                {
                    continue;
                }

#if DEBUG
                int stepCount = 10;
#else
                int stepCount = benchmarks[benchmarkIndex].TotalStepCount;
#endif

                Benchmark benchmark = benchmarks[benchmarkIndex];
                bool countersAcquired = false;

                Console.WriteLine("benchmark: {0}, steps = {1}", benchmark.Name, stepCount);

                float[] minTime = new float[B2_MAX_WORKERS];

                for (int threadCount = 1; threadCount <= maxThreadCount; ++threadCount)
                {
                    if (singleWorkerCount != -1 && singleWorkerCount != threadCount)
                    {
                        continue;
                    }

                    Console.WriteLine("thread count: {0}", threadCount);

                    for (int runIndex = 0; runIndex < runCount; ++runIndex)
                    {
                        B2WorldDef worldDef = b2DefaultWorldDef();
                        worldDef.enableContinuous = enableContinuous;
                        worldDef.workerCount = threadCount;
                        B2WorldId worldId = b2CreateWorld(worldDef);

                        try
                        {
                            object state = benchmark.Create(worldId);

                            const float timeStep = 1.0f / 60.0f;
                            const int subStepCount = 4;

                            // The initial step can be expensive and skew the benchmark.
                            if (benchmark.Step != null)
                            {
                                stepResults[0] = benchmark.Step(state, worldId, 0);
                            }

                            b2World_Step(worldId, timeStep, subStepCount);

                            B2Profile profile = b2World_GetProfile(worldId);
                            MinProfile(ref profiles[0], profile);

                            ulong ticks = b2GetTicks();

                            for (int stepIndex = 1; stepIndex < stepCount; ++stepIndex)
                            {
                                if (benchmark.Step != null)
                                {
                                    stepResults[stepIndex] = benchmark.Step(state, worldId, stepIndex);
                                }

                                b2World_Step(worldId, timeStep, subStepCount);
                                profile = b2World_GetProfile(worldId);
                                MinProfile(ref profiles[stepIndex], profile);
                            }

                            float milliseconds = b2GetMilliseconds(ticks);
                            Console.WriteLine("run {0} : {1} (ms)", runIndex, FormatFloat(milliseconds));

                            if (runIndex == 0)
                            {
                                minTime[threadCount - 1] = milliseconds;
                            }
                            else
                            {
                                minTime[threadCount - 1] = b2MinFloat(minTime[threadCount - 1], milliseconds);
                            }

                            if (countersAcquired == false)
                            {
                                counters = b2World_GetCounters(worldId);
                                countersAcquired = true;
                            }

                            GC.KeepAlive(state);
                        }
                        finally
                        {
                            b2DestroyWorld(worldId);
                        }
                    }

                    if (recordStepTimes)
                    {
                        WriteStepProfiles(benchmark.Name, threadCount, profiles, stepCount);
                    }
                }

                Console.WriteLine(
                    "body {0} / shape {1} / contact {2} / joint {3} / stack {4}",
                    counters.bodyCount,
                    counters.shapeCount,
                    counters.contactCount,
                    counters.jointCount,
                    counters.stackUsed);
                Console.Write("color counts:");
                for (int colorIndex = 0; colorIndex < counters.colorCounts.Length; ++colorIndex)
                {
                    Console.Write(" {0}", counters.colorCounts[colorIndex]);
                }

                Console.WriteLine();
                Console.WriteLine();

                WriteThreadResults(benchmark.Name, minTime, maxThreadCount);
            }

            Console.WriteLine("======================================");
            Console.WriteLine("All Box2D benchmarks complete!");
            return 0;
        }

        private static Benchmark[] CreateBenchmarks()
        {
#if DEBUG
            const int smashRows = 10;
            const int smashColumns = 20;
#else
            const int smashRows = 80;
            const int smashColumns = 120;
#endif

            return new[]
            {
                new Benchmark("compounds", CreateStateless(CreateCompounds), null, 500),
                new Benchmark("joint_grid", CreateStateless(CreateJointGrid), null, 500),
                new Benchmark("junkyard", worldId => CreateJunkyard(worldId),
                    (state, worldId, stepCount) => StepJunkyard((JunkyardData)state, worldId, stepCount), 800),
                new Benchmark("large_pyramid", CreateStateless(CreateLargePyramid), null, 500),
                new Benchmark("many_pyramids", CreateStateless(CreateManyPyramids), null, 200),
                new Benchmark("rain", worldId => CreateRain(worldId),
                    (state, worldId, stepCount) => StepRain((RainData)state, worldId, stepCount), 1000),
                new Benchmark("smash", worldId =>
                {
                    var bodyIds = new List<B2BodyId>(smashRows * smashColumns + 1);
                    CreateSmash(worldId, smashRows, smashColumns, bodyIds);
                    return bodyIds;
                }, null, 300),
                new Benchmark("spinner", worldId => CreateSpinner(worldId),
                    (state, worldId, stepCount) => StepSpinner((SpinnerData)state, worldId, stepCount), 500),
                new Benchmark("tumbler", CreateStateless(CreateTumbler), null, 750),
                new Benchmark("washer", CreateStateless(CreateWasher), null, 500),
            };
        }

        private static CreateFcn CreateStateless(Action<B2WorldId> createFcn)
        {
            return worldId =>
            {
                createFcn(worldId);
                return null;
            };
        }

        private static B2Profile CreateMaxProfile()
        {
            return new B2Profile
            {
                step = float.MaxValue,
                pairs = float.MaxValue,
                collide = float.MaxValue,
                solve = float.MaxValue,
                solverSetup = float.MaxValue,
                constraints = float.MaxValue,
                prepareConstraints = float.MaxValue,
                integrateVelocities = float.MaxValue,
                warmStart = float.MaxValue,
                solveImpulses = float.MaxValue,
                integratePositions = float.MaxValue,
                relaxImpulses = float.MaxValue,
                applyRestitution = float.MaxValue,
                storeImpulses = float.MaxValue,
                splitIslands = float.MaxValue,
                transforms = float.MaxValue,
                refit = float.MaxValue,
                bullets = float.MaxValue,
                sleepIslands = float.MaxValue,
            };
        }

        private static void MinProfile(ref B2Profile first, in B2Profile second)
        {
            first.step = b2MinFloat(first.step, second.step);
            first.pairs = b2MinFloat(first.pairs, second.pairs);
            first.collide = b2MinFloat(first.collide, second.collide);
            first.constraints = b2MinFloat(first.constraints, second.constraints);
            first.transforms = b2MinFloat(first.transforms, second.transforms);
            first.refit = b2MinFloat(first.refit, second.refit);
            first.sleepIslands = b2MinFloat(first.sleepIslands, second.sleepIslands);
        }

        private static void WriteStepProfiles(string benchmarkName, int threadCount, B2Profile[] profiles, int stepCount)
        {
            string fileName = string.Format(CultureInfo.InvariantCulture, "{0}_t{1}.dat", benchmarkName, threadCount);

            try
            {
                using var writer = new StreamWriter(fileName, false);
                for (int stepIndex = 0; stepIndex < stepCount; ++stepIndex)
                {
                    B2Profile profile = profiles[stepIndex];
                    writer.Write(FormatFloat(profile.step));
                    writer.Write(' ');
                    writer.Write(FormatFloat(profile.pairs));
                    writer.Write(' ');
                    writer.Write(FormatFloat(profile.collide));
                    writer.Write(' ');
                    writer.Write(FormatFloat(profile.constraints));
                    writer.Write(' ');
                    writer.Write(FormatFloat(profile.transforms));
                    writer.Write(' ');
                    writer.Write(FormatFloat(profile.refit));
                    writer.Write(' ');
                    writer.WriteLine(FormatFloat(profile.sleepIslands));
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static void WriteThreadResults(string benchmarkName, float[] minTime, int maxThreadCount)
        {
            string fileName = benchmarkName + ".csv";

            try
            {
                using var writer = new StreamWriter(fileName, false);
                writer.WriteLine("threads,ms");
                for (int threadIndex = 1; threadIndex <= maxThreadCount; ++threadIndex)
                {
                    writer.Write(threadIndex.ToString(CultureInfo.InvariantCulture));
                    writer.Write(',');
                    writer.WriteLine(FormatFloat(minTime[threadIndex - 1]));
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static int ParseInteger(ReadOnlySpan<char> value)
        {
            int index = 0;
            while (index < value.Length && char.IsWhiteSpace(value[index]))
            {
                ++index;
            }

            bool negative = false;
            if (index < value.Length && (value[index] == '+' || value[index] == '-'))
            {
                negative = value[index] == '-';
                ++index;
            }

            long magnitude = 0;
            bool hasDigit = false;
            long limit = negative ? 2147483648L : int.MaxValue;

            while (index < value.Length)
            {
                int digit = value[index] - '0';
                if ((uint)digit > 9u)
                {
                    break;
                }

                hasDigit = true;
                if (magnitude > (limit - digit) / 10L)
                {
                    return negative ? int.MinValue : int.MaxValue;
                }

                magnitude = 10L * magnitude + digit;
                ++index;
            }

            if (hasDigit == false)
            {
                return 0;
            }

            return negative ? (int)-magnitude : (int)magnitude;
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("G6", CultureInfo.InvariantCulture);
        }

        private static void PrintUsage()
        {
            Console.WriteLine(
                "Usage\n" +
                "-t=<integer>: the maximum number of threads to use\n" +
                "-b=<integer>: run a single benchmark\n" +
                "-w=<integer>: run a single worker count\n" +
                "-r=<integer>: number of repeats (default is 4)\n" +
                "-s: record step times");
        }
    }
}
