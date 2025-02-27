// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;
using Box2D.NET.Primitives;
using Box2D.NET.Samples.Primitives;
using ImGuiNET;
using Silk.NET.GLFW;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2MouseJoints;
using static Box2D.NET.Shared.RandomSupports;

namespace Box2D.NET.Samples.Samples;

public class Sample : IDisposable
{
#if NDEBUG
    public const bool g_sampleDebug = false;
#else
    public const bool g_sampleDebug = true;
#endif
    public const int k_maxContactPoints = 12 * 2048;

    public const int MAX_SAMPLES = 256;
    public static SampleEntry[] g_sampleEntries = new SampleEntry[MAX_SAMPLES];
    public static int g_sampleCount = 0;

    public static int RegisterSample(string category, string name, SampleCreateFcn fcn)
    {
        int index = g_sampleCount;
        if (index < MAX_SAMPLES)
        {
            g_sampleEntries[index] = new SampleEntry(category, name, fcn);
            ++g_sampleCount;
            return index;
        }

        return -1;
    }

    // -----------------------------------------------------------------------------------------------------
    public const int m_maxTasks = 64;
    public const int m_maxThreads = 64;

    public Settings m_settings;
    public TaskScheduler m_scheduler;
    public SampleTask[] m_tasks;
    public int m_taskCount;
    public int m_threadCount;

    public B2BodyId m_groundBodyId;

    // DestructionListener m_destructionListener;
    public int m_textLine;
    public B2WorldId m_worldId;
    public B2JointId m_mouseJointId;
    public int m_stepCount;
    public int m_textIncrement;
    public B2Profile m_maxProfile;
    public B2Profile m_totalProfile;


    public Sample(Settings settings)
    {
        m_scheduler = new TaskScheduler();
        m_scheduler.Initialize(settings.workerCount);

        m_tasks = new SampleTask[m_maxTasks];
        m_taskCount = 0;

        m_threadCount = 1 + settings.workerCount;

        m_worldId = b2_nullWorldId;

        m_textLine = 30;
        m_textIncrement = 22;
        m_mouseJointId = b2_nullJointId;

        m_stepCount = 0;

        m_groundBodyId = b2_nullBodyId;

        m_maxProfile = new B2Profile();
        m_totalProfile = new B2Profile();

        g_seed = RAND_SEED;

        m_settings = settings;

        CreateWorld();
        TestMathCpp();
    }

    public virtual void Dispose()
    {
        // By deleting the world, we delete the bomb, mouse joint, etc.
        b2DestroyWorld(m_worldId);

        // delete m_scheduler;
        // delete[] m_tasks;
    }

    public void CreateWorld()
    {
        if (B2_IS_NON_NULL(m_worldId))
        {
            b2DestroyWorld(m_worldId);
            m_worldId = b2_nullWorldId;
        }

        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.workerCount = m_settings.workerCount;
        worldDef.enqueueTask = EnqueueTask;
        worldDef.finishTask = FinishTask;
        worldDef.userTaskContext = this;
        worldDef.enableSleep = m_settings.enableSleep;

        m_worldId = b2CreateWorld(worldDef);
    }

    public void TestMathCpp()
    {
        B2Vec2 a = new B2Vec2(1.0f, 2.0f);
        B2Vec2 b = new B2Vec2(3.0f, 4.0f);

        B2Vec2 c = a;
        c += b;
        c -= b;
        c *= 2.0f;
        c = -a;
        c = c + b;
        c = c - a;
        c = 2.0f * a;
        c = a * 2.0f;

        if (b == a)
        {
            c = a;
        }

        if (b != a)
        {
            c = b;
        }

        c += c;
    }

    public virtual void UpdateUI()
    {
    }


    private static object EnqueueTask(b2TaskCallback task, int itemCount, int minRange, object taskContext, object userContext)
    {
        Sample sample = userContext as Sample;
        if (sample.m_taskCount < Sample.m_maxTasks)
        {
            SampleTask sampleTask = sample.m_tasks[sample.m_taskCount];
            sampleTask.m_SetSize = itemCount;
            sampleTask.m_MinRange = minRange;
            sampleTask.m_task = task;
            sampleTask.m_taskContext = taskContext;
            sample.m_scheduler.AddTaskSetToPipe(sampleTask);
            ++sample.m_taskCount;
            return sampleTask;
        }
        else
        {
            // This is not fatal but the maxTasks should be increased
            Debug.Assert(false);
            task(0, itemCount, 0, taskContext);
            return null;
        }
    }

    private static void FinishTask(object taskPtr, object userContext)
    {
        if (taskPtr != null)
        {
            SampleTask sampleTask = taskPtr as SampleTask;
            Sample sample = userContext as Sample;
            sample.m_scheduler.WaitforTask(sampleTask);
        }
    }

    public void DrawTitle(string title)
    {
        Draw.g_draw.DrawString(5, 5, title);
        m_textLine = (int)26.0f;
    }


    public bool QueryCallback(B2ShapeId shapeId, object context)
    {
        QueryContext queryContext = context as QueryContext;

        B2BodyId bodyId = b2Shape_GetBody(shapeId);
        B2BodyType bodyType = b2Body_GetType(bodyId);
        if (bodyType != B2BodyType.b2_dynamicBody)
        {
            // continue query
            return true;
        }

        bool overlap = b2Shape_TestPoint(shapeId, queryContext.point);
        if (overlap)
        {
            // found shape
            queryContext.bodyId = bodyId;
            return false;
        }

        return true;
    }

    public virtual void Keyboard(int a)
    {
    }

    public virtual void MouseDown(B2Vec2 p, int button, int mod)
    {
        if (B2_IS_NON_NULL(m_mouseJointId))
        {
            return;
        }

        if (button == (int)MouseButton.Left)
        {
            // Make a small box.
            B2AABB box;
            B2Vec2 d = new B2Vec2(0.001f, 0.001f);
            box.lowerBound = b2Sub(p, d);
            box.upperBound = b2Add(p, d);

            // Query the world for overlapping shapes.
            QueryContext queryContext = new QueryContext(p, b2_nullBodyId);
            b2World_OverlapAABB(m_worldId, box, b2DefaultQueryFilter(), QueryCallback, queryContext);

            if (B2_IS_NON_NULL(queryContext.bodyId))
            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                m_groundBodyId = b2CreateBody(m_worldId, bodyDef);

                B2MouseJointDef mouseDef = b2DefaultMouseJointDef();
                mouseDef.bodyIdA = m_groundBodyId;
                mouseDef.bodyIdB = queryContext.bodyId;
                mouseDef.target = p;
                mouseDef.hertz = 5.0f;
                mouseDef.dampingRatio = 0.7f;
                mouseDef.maxForce = 1000.0f * b2Body_GetMass(queryContext.bodyId);
                m_mouseJointId = b2CreateMouseJoint(m_worldId, mouseDef);

                b2Body_SetAwake(queryContext.bodyId, true);
            }
        }
    }

    public virtual void MouseUp(B2Vec2 p, int button)
    {
        if (b2Joint_IsValid(m_mouseJointId) == false)
        {
            // The world or attached body was destroyed.
            m_mouseJointId = b2_nullJointId;
        }

        if (B2_IS_NON_NULL(m_mouseJointId) && button == (int)MouseButton.Left)
        {
            b2DestroyJoint(m_mouseJointId);
            m_mouseJointId = b2_nullJointId;

            b2DestroyBody(m_groundBodyId);
            m_groundBodyId = b2_nullBodyId;
        }
    }

    public virtual void MouseMove(B2Vec2 p)
    {
        if (b2Joint_IsValid(m_mouseJointId) == false)
        {
            // The world or attached body was destroyed.
            m_mouseJointId = b2_nullJointId;
        }

        if (B2_IS_NON_NULL(m_mouseJointId))
        {
            b2MouseJoint_SetTarget(m_mouseJointId, p);
            B2BodyId bodyIdB = b2Joint_GetBodyB(m_mouseJointId);
            b2Body_SetAwake(bodyIdB, true);
        }
    }

    public void DrawTextLine(string text, params object[] arg)
    {
        bool open = false;
        ImGui.Begin("Overlay", ref open,
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize |
            ImGuiWindowFlags.NoScrollbar);
        ImGui.PushFont(Draw.g_draw.m_regularFont);
        ImGui.SetCursorPos(new Vector2(5.0f, (float)m_textLine));
        ImGui.TextColored(new Vector4(230, 153, 153, 255), string.Format(text, arg));
        ImGui.PopFont();
        ImGui.End();

        m_textLine += m_textIncrement;
    }

    public void ResetProfile()
    {
        m_totalProfile = new B2Profile();
        m_maxProfile = new B2Profile();
        m_stepCount = 0;
    }

    public virtual void Step(Settings settings)
    {
        float timeStep = settings.hertz > 0.0f ? 1.0f / settings.hertz : 0.0f;

        if (settings.pause)
        {
            if (settings.singleStep)
            {
                settings.singleStep = false;
            }
            else
            {
                timeStep = 0.0f;
            }

            if (Draw.g_draw.m_showUI)
            {
                Draw.g_draw.DrawString(5, m_textLine, "****PAUSED****");
                m_textLine += m_textIncrement;
            }
        }

        Draw.g_draw.m_debugDraw.drawingBounds = Draw.g_camera.GetViewBounds();
        Draw.g_draw.m_debugDraw.useDrawingBounds = settings.useCameraBounds;

        // todo testing
        // b2Transform t1 = {Draw.g_draw.m_debugDraw.drawingBounds.lowerBound, b2Rot_identity};
        // b2Transform t2 = {Draw.g_draw.m_debugDraw.drawingBounds.upperBound, b2Rot_identity};
        // Draw.g_draw.DrawSolidCircle(ref t1, b2Vec2_zero, 1.0f, {1.0f, 0.0f, 0.0f, 1.0f});
        // Draw.g_draw.DrawSolidCircle(ref t2, b2Vec2_zero, 1.0f, {1.0f, 0.0f, 0.0f, 1.0f});

        Draw.g_draw.m_debugDraw.drawShapes = settings.drawShapes;
        Draw.g_draw.m_debugDraw.drawJoints = settings.drawJoints;
        Draw.g_draw.m_debugDraw.drawJointExtras = settings.drawJointExtras;
        Draw.g_draw.m_debugDraw.drawAABBs = settings.drawAABBs;
        Draw.g_draw.m_debugDraw.drawMass = settings.drawMass;
        Draw.g_draw.m_debugDraw.drawBodyNames = settings.drawBodyNames;
        Draw.g_draw.m_debugDraw.drawContacts = settings.drawContactPoints;
        Draw.g_draw.m_debugDraw.drawGraphColors = settings.drawGraphColors;
        Draw.g_draw.m_debugDraw.drawContactNormals = settings.drawContactNormals;
        Draw.g_draw.m_debugDraw.drawContactImpulses = settings.drawContactImpulses;
        Draw.g_draw.m_debugDraw.drawFrictionImpulses = settings.drawFrictionImpulses;

        b2World_EnableSleeping(m_worldId, settings.enableSleep);
        b2World_EnableWarmStarting(m_worldId, settings.enableWarmStarting);
        b2World_EnableContinuous(m_worldId, settings.enableContinuous);

        for (int i = 0; i < 1; ++i)
        {
            b2World_Step(m_worldId, timeStep, settings.subStepCount);
            m_taskCount = 0;
        }

        b2World_Draw(m_worldId, Draw.g_draw.m_debugDraw);

        if (timeStep > 0.0f)
        {
            ++m_stepCount;
        }

        if (settings.drawCounters)
        {
            B2Counters s = b2World_GetCounters(m_worldId);

            Draw.g_draw.DrawString(5, m_textLine, "bodies/shapes/contacts/joints = %d/%d/%d/%d", s.bodyCount, s.shapeCount,
                s.contactCount, s.jointCount);
            m_textLine += m_textIncrement;

            Draw.g_draw.DrawString(5, m_textLine, "islands/tasks = %d/%d", s.islandCount, s.taskCount);
            m_textLine += m_textIncrement;

            Draw.g_draw.DrawString(5, m_textLine, "tree height static/movable = %d/%d", s.staticTreeHeight, s.treeHeight);
            m_textLine += m_textIncrement;

            int totalCount = 0;
            var buffer = new StringBuilder();
            Debug.Assert(s.colorCounts.Length == 12);

            // todo fix this
            buffer.Append("colors: ");
            for (int i = 0; i < 12; ++i)
            {
                buffer.Append($"{s.colorCounts[i]}/");
                totalCount += s.colorCounts[i];
            }

            buffer.Append($"[{totalCount}]");
            Draw.g_draw.DrawString(5, m_textLine, buffer.ToString());
            m_textLine += m_textIncrement;

            Draw.g_draw.DrawString(5, m_textLine, "stack allocator size = %d K", s.stackUsed / 1024);
            m_textLine += m_textIncrement;

            Draw.g_draw.DrawString(5, m_textLine, "total allocation = %d K", s.byteCount / 1024);
            m_textLine += m_textIncrement;
        }

        // Track maximum profile times
        {
            B2Profile p = b2World_GetProfile(m_worldId);
            m_maxProfile.step = b2MaxFloat(m_maxProfile.step, p.step);
            m_maxProfile.pairs = b2MaxFloat(m_maxProfile.pairs, p.pairs);
            m_maxProfile.collide = b2MaxFloat(m_maxProfile.collide, p.collide);
            m_maxProfile.solve = b2MaxFloat(m_maxProfile.solve, p.solve);
            m_maxProfile.mergeIslands = b2MaxFloat(m_maxProfile.mergeIslands, p.mergeIslands);
            m_maxProfile.prepareStages = b2MaxFloat(m_maxProfile.prepareStages, p.prepareStages);
            m_maxProfile.solveConstraints = b2MaxFloat(m_maxProfile.solveConstraints, p.solveConstraints);
            m_maxProfile.prepareConstraints = b2MaxFloat(m_maxProfile.prepareConstraints, p.prepareConstraints);
            m_maxProfile.integrateVelocities = b2MaxFloat(m_maxProfile.integrateVelocities, p.integrateVelocities);
            m_maxProfile.warmStart = b2MaxFloat(m_maxProfile.warmStart, p.warmStart);
            m_maxProfile.solveImpulses = b2MaxFloat(m_maxProfile.solveImpulses, p.solveImpulses);
            m_maxProfile.integratePositions = b2MaxFloat(m_maxProfile.integratePositions, p.integratePositions);
            m_maxProfile.relaxImpulses = b2MaxFloat(m_maxProfile.relaxImpulses, p.relaxImpulses);
            m_maxProfile.applyRestitution = b2MaxFloat(m_maxProfile.applyRestitution, p.applyRestitution);
            m_maxProfile.storeImpulses = b2MaxFloat(m_maxProfile.storeImpulses, p.storeImpulses);
            m_maxProfile.transforms = b2MaxFloat(m_maxProfile.transforms, p.transforms);
            m_maxProfile.splitIslands = b2MaxFloat(m_maxProfile.splitIslands, p.splitIslands);
            m_maxProfile.hitEvents = b2MaxFloat(m_maxProfile.hitEvents, p.hitEvents);
            m_maxProfile.refit = b2MaxFloat(m_maxProfile.refit, p.refit);
            m_maxProfile.bullets = b2MaxFloat(m_maxProfile.bullets, p.bullets);
            m_maxProfile.sleepIslands = b2MaxFloat(m_maxProfile.sleepIslands, p.sleepIslands);
            m_maxProfile.sensors = b2MaxFloat(m_maxProfile.sensors, p.sensors);

            m_totalProfile.step += p.step;
            m_totalProfile.pairs += p.pairs;
            m_totalProfile.collide += p.collide;
            m_totalProfile.solve += p.solve;
            m_totalProfile.mergeIslands += p.mergeIslands;
            m_totalProfile.prepareStages += p.prepareStages;
            m_totalProfile.solveConstraints += p.solveConstraints;
            m_totalProfile.prepareConstraints += p.prepareConstraints;
            m_totalProfile.integrateVelocities += p.integrateVelocities;
            m_totalProfile.warmStart += p.warmStart;
            m_totalProfile.solveImpulses += p.solveImpulses;
            m_totalProfile.integratePositions += p.integratePositions;
            m_totalProfile.relaxImpulses += p.relaxImpulses;
            m_totalProfile.applyRestitution += p.applyRestitution;
            m_totalProfile.storeImpulses += p.storeImpulses;
            m_totalProfile.transforms += p.transforms;
            m_totalProfile.splitIslands += p.splitIslands;
            m_totalProfile.hitEvents += p.hitEvents;
            m_totalProfile.refit += p.refit;
            m_totalProfile.bullets += p.bullets;
            m_totalProfile.sleepIslands += p.sleepIslands;
            m_totalProfile.sensors += p.sensors;
        }

        if (settings.drawProfile)
        {
            B2Profile p = b2World_GetProfile(m_worldId);

            B2Profile aveProfile = new B2Profile();
            if (m_stepCount > 0)
            {
                float scale = 1.0f / m_stepCount;
                aveProfile.step = scale * m_totalProfile.step;
                aveProfile.pairs = scale * m_totalProfile.pairs;
                aveProfile.collide = scale * m_totalProfile.collide;
                aveProfile.solve = scale * m_totalProfile.solve;
                aveProfile.mergeIslands = scale * m_totalProfile.mergeIslands;
                aveProfile.prepareStages = scale * m_totalProfile.prepareStages;
                aveProfile.solveConstraints = scale * m_totalProfile.solveConstraints;
                aveProfile.prepareConstraints = scale * m_totalProfile.prepareConstraints;
                aveProfile.integrateVelocities = scale * m_totalProfile.integrateVelocities;
                aveProfile.warmStart = scale * m_totalProfile.warmStart;
                aveProfile.solveImpulses = scale * m_totalProfile.solveImpulses;
                aveProfile.integratePositions = scale * m_totalProfile.integratePositions;
                aveProfile.relaxImpulses = scale * m_totalProfile.relaxImpulses;
                aveProfile.applyRestitution = scale * m_totalProfile.applyRestitution;
                aveProfile.storeImpulses = scale * m_totalProfile.storeImpulses;
                aveProfile.transforms = scale * m_totalProfile.transforms;
                aveProfile.splitIslands = scale * m_totalProfile.splitIslands;
                aveProfile.hitEvents = scale * m_totalProfile.hitEvents;
                aveProfile.refit = scale * m_totalProfile.refit;
                aveProfile.bullets = scale * m_totalProfile.bullets;
                aveProfile.sleepIslands = scale * m_totalProfile.sleepIslands;
                aveProfile.sensors = scale * m_totalProfile.sensors;
            }

            DrawTextLine("step [ave] (max) = %5.2f [%6.2f] (%6.2f)", p.step, aveProfile.step, m_maxProfile.step);
            DrawTextLine("pairs [ave] (max) = %5.2f [%6.2f] (%6.2f)", p.pairs, aveProfile.pairs, m_maxProfile.pairs);
            DrawTextLine("collide [ave] (max) = %5.2f [%6.2f] (%6.2f)", p.collide, aveProfile.collide, m_maxProfile.collide);
            DrawTextLine("solve [ave] (max) = %5.2f [%6.2f] (%6.2f)", p.solve, aveProfile.solve, m_maxProfile.solve);
            DrawTextLine("> merge islands [ave] (max) = %5.2f [%6.2f] (%6.2f)", p.mergeIslands, aveProfile.mergeIslands,
                m_maxProfile.mergeIslands);
            DrawTextLine("> prepare tasks [ave] (max) = %5.2f [%6.2f] (%6.2f)", p.prepareStages, aveProfile.prepareStages,
                m_maxProfile.prepareStages);
            DrawTextLine("> solve constraints [ave] (max) = %5.2f [%6.2f] (%6.2f)", p.solveConstraints, aveProfile.solveConstraints,
                m_maxProfile.solveConstraints);
            DrawTextLine(">> prepare constraints [ave] (max) = %5.2f [%6.2f] (%6.2f)", p.prepareConstraints,
                aveProfile.prepareConstraints, m_maxProfile.prepareConstraints);
            DrawTextLine(">> integrate velocities [ave] (max) = %5.2f [%6.2f] (%6.2f)", p.integrateVelocities,
                aveProfile.integrateVelocities, m_maxProfile.integrateVelocities);
            DrawTextLine(">> warm start [ave] (max) = %5.2f [%6.2f] (%6.2f)", p.warmStart, aveProfile.warmStart,
                m_maxProfile.warmStart);
            DrawTextLine(">> solve impulses [ave] (max) = %5.2f [%6.2f] (%6.2f)", p.solveImpulses, aveProfile.solveImpulses,
                m_maxProfile.solveImpulses);
            DrawTextLine(">> integrate positions [ave] (max) = %5.2f [%6.2f] (%6.2f)", p.integratePositions,
                aveProfile.integratePositions, m_maxProfile.integratePositions);
            DrawTextLine(">> relax impulses [ave] (max) = %5.2f [%6.2f] (%6.2f)", p.relaxImpulses, aveProfile.relaxImpulses,
                m_maxProfile.relaxImpulses);
            DrawTextLine(">> apply restitution [ave] (max) = %5.2f [%6.2f] (%6.2f)", p.applyRestitution, aveProfile.applyRestitution,
                m_maxProfile.applyRestitution);
            DrawTextLine(">> store impulses [ave] (max) = %5.2f [%6.2f] (%6.2f)", p.storeImpulses, aveProfile.storeImpulses,
                m_maxProfile.storeImpulses);
            DrawTextLine(">> split islands [ave] (max) = %5.2f [%6.2f] (%6.2f)", p.splitIslands, aveProfile.splitIslands,
                m_maxProfile.splitIslands);
            DrawTextLine("> update transforms [ave] (max) = %5.2f [%6.2f] (%6.2f)", p.transforms, aveProfile.transforms,
                m_maxProfile.transforms);
            DrawTextLine("> hit events [ave] (max) = %5.2f [%6.2f] (%6.2f)", p.hitEvents, aveProfile.hitEvents,
                m_maxProfile.hitEvents);
            DrawTextLine("> refit BVH [ave] (max) = %5.2f [%6.2f] (%6.2f)", p.refit, aveProfile.refit, m_maxProfile.refit);
            DrawTextLine("> sleep islands [ave] (max) = %5.2f [%6.2f] (%6.2f)", p.sleepIslands, aveProfile.sleepIslands,
                m_maxProfile.sleepIslands);
            DrawTextLine("> bullets [ave] (max) = %5.2f [%6.2f] (%6.2f)", p.bullets, aveProfile.bullets, m_maxProfile.bullets);
            DrawTextLine("sensors [ave] (max) = %5.2f [%6.2f] (%6.2f)", p.sensors, aveProfile.sensors, m_maxProfile.sensors);
        }
    }

    public void ShiftOrigin(B2Vec2 newOrigin)
    {
        // m_world.ShiftOrigin(newOrigin);
    }

    // Parse an SVG path element with only straight lines. Example:
    // "M 47.625004,185.20833 H 161.39585 l 29.10417,-2.64583 26.45834,-7.9375 26.45833,-13.22917 23.81251,-21.16666 h "
    // "13.22916 v 44.97916 H 592.66669 V 0 h 21.16671 v 206.375 l -566.208398,-1e-5 z"
    public static int ParsePath(string svgPath, B2Vec2 offset, Span<B2Vec2> points, int capacity, float scale, bool reverseOrder)
    {
        int pointCount = 0;
        B2Vec2 currentPoint = new B2Vec2();
        int ptrIndex = 0;
        char command = svgPath[ptrIndex];

        while (ptrIndex < svgPath.Length)
        {
            if (!char.IsDigit(svgPath[ptrIndex]) && svgPath[ptrIndex] != '-')
            {
                // note: command can be implicitly repeated
                command = svgPath[ptrIndex];

                if (command == 'M' || command == 'L' || command == 'H' || command == 'V' || command == 'm' || command == 'l' ||
                    command == 'h' || command == 'v')
                {
                    ptrIndex += 2; // Skip the command character and space
                }

                if (command == 'z')
                {
                    break;
                }
            }

            Debug.Assert(!char.IsDigit(svgPath[ptrIndex]) || svgPath[ptrIndex] == '-');


            float x = 0.0f;
            float y = 0.0f;
            switch (command)
            {
                case 'M':
                case 'L':
                    if (Sscanf(svgPath, ref ptrIndex, out x, out y))
                    {
                        currentPoint.x = x;
                        currentPoint.y = y;
                    }
                    else
                    {
                        Debug.Assert(false);
                    }

                    break;
                case 'H':
                    if (Sscanf(svgPath, ref ptrIndex, out x))
                    {
                        currentPoint.x = x;
                    }
                    else
                    {
                        Debug.Assert(false);
                    }

                    break;
                case 'V':
                    if (Sscanf(svgPath, ref ptrIndex, out y))
                    {
                        currentPoint.y = y;
                    }
                    else
                    {
                        Debug.Assert(false);
                    }

                    break;
                case 'm':
                case 'l':
                    if (Sscanf(svgPath, ref ptrIndex, out x, out y))
                    {
                        currentPoint.x += x;
                        currentPoint.y += y;
                    }
                    else
                    {
                        Debug.Assert(false);
                    }

                    break;
                case 'h':
                    if (Sscanf(svgPath, ref ptrIndex, out x))
                    {
                        currentPoint.x += x;
                    }
                    else
                    {
                        Debug.Assert(false);
                    }

                    break;
                case 'v':
                    if (Sscanf(svgPath, ref ptrIndex, out y))
                    {
                        currentPoint.y += y;
                    }
                    else
                    {
                        Debug.Assert(false);
                    }

                    break;

                default:
                    Debug.Assert(false);
                    break;
            }

            points[pointCount] = new B2Vec2(scale * (currentPoint.x + offset.x), -scale * (currentPoint.y + offset.y));
            pointCount += 1;
            if (pointCount == capacity)
            {
                break;
            }

            // Move to the next space or end of string
            while (ptrIndex < svgPath.Length && !char.IsWhiteSpace(svgPath[ptrIndex]))
            {
                ptrIndex++;
            }

            // Skip contiguous spaces
            while (char.IsWhiteSpace(svgPath[ptrIndex]))
            {
                ptrIndex++;
            }

            ptrIndex += 0;
        }

        if (pointCount == 0)
        {
            return 0;
        }

        if (reverseOrder)
        {
        }

        return pointCount;
    }

    private static bool Sscanf(string svgPath, ref int ptrIndex, out float x, out float y)
    {
        // Parse the coordinates in the form "x,y"
        x = 0;
        y = 0;
        int startIdx = ptrIndex;
        while (ptrIndex < svgPath.Length && (char.IsDigit(svgPath[ptrIndex]) || svgPath[ptrIndex] == '.' || svgPath[ptrIndex] == '-' || svgPath[ptrIndex] == ','))
        {
            ptrIndex++;
        }

        var segment = svgPath.Substring(startIdx, ptrIndex - startIdx).Split(',');
        if (segment.Length == 2)
        {
            return float.TryParse(segment[0], CultureInfo.InvariantCulture, out x) && float.TryParse(segment[1], CultureInfo.InvariantCulture, out y);
        }

        return false;
    }

    private static bool Sscanf(string svgPath, ref int ptrIndex, out float value)
    {
        value = 0;
        int startIdx = ptrIndex;
        while (ptrIndex < svgPath.Length && (char.IsDigit(svgPath[ptrIndex]) || svgPath[ptrIndex] == '.' || svgPath[ptrIndex] == '-'))
        {
            ptrIndex++;
        }

        var segment = svgPath.Substring(startIdx, ptrIndex - startIdx);
        return float.TryParse(segment, CultureInfo.InvariantCulture, out value);
    }
}
