// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using System.Text;
using Box2D.NET.Samples.Helpers;
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
using static Box2D.NET.Shared.RandomSupports;
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET.Samples.Samples;

public class Sample : IDisposable
{
    public const int k_maxContactPoints = 12 * 2048;
    public const int m_maxTasks = 64;
    public const int m_maxThreads = 64;

#if DEBUG
    public const bool m_isDebug = true;
#else
    public const bool m_isDebug = false;
#endif

    protected SampleContext m_context;
    protected Camera m_camera;
    protected Draw m_draw;

    protected TaskScheduler m_scheduler;
    protected SampleTask[] m_tasks;
    protected int m_taskCount;
    protected int m_threadCount;

    protected B2BodyId m_mouseBodyId;

    public B2WorldId m_worldId;
    protected B2JointId m_mouseJointId;
    protected B2Vec2 m_mousePoint;
    public int m_stepCount;
    protected B2Profile m_maxProfile;
    protected B2Profile m_totalProfile;

    private int m_textLine;
    private int m_textIncrement;

    public Sample(SampleContext context)
    {
        m_context = context;
        m_camera = context.camera;
        m_draw = context.draw;

        m_scheduler = new TaskScheduler();
        m_scheduler.Initialize(m_context.settings.workerCount);

        m_tasks = new SampleTask[m_maxTasks];
        for (int i = 0; i < m_maxTasks; ++i)
        {
            m_tasks[i] = new SampleTask();
        }

        m_taskCount = 0;

        m_threadCount = 1 + m_context.settings.workerCount;

        m_worldId = b2_nullWorldId;

        m_textLine = 30;
        m_textIncrement = 22;
        m_mouseJointId = b2_nullJointId;

        m_stepCount = 0;

        m_mouseBodyId = b2_nullBodyId;
        m_mousePoint = new B2Vec2();

        m_maxProfile = new B2Profile();
        m_totalProfile = new B2Profile();

        g_randomSeed = RAND_SEED;

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
        worldDef.workerCount = m_context.settings.workerCount;
        worldDef.enqueueTask = EnqueueTask;
        worldDef.finishTask = FinishTask;
        worldDef.userTaskContext = this;
        worldDef.enableSleep = m_context.settings.enableSleep;

        m_worldId = b2CreateWorld(ref worldDef);
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

    public virtual void UpdateGui()
    {
        if (m_context.settings.drawProfile)
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
                aveProfile.jointEvents = scale * m_totalProfile.jointEvents;
                aveProfile.hitEvents = scale * m_totalProfile.hitEvents;
                aveProfile.refit = scale * m_totalProfile.refit;
                aveProfile.bullets = scale * m_totalProfile.bullets;
                aveProfile.sleepIslands = scale * m_totalProfile.sleepIslands;
                aveProfile.sensors = scale * m_totalProfile.sensors;
            }

            DrawTextLine($"pairs [ave] (max) = {p.pairs,5:F2} [{aveProfile.pairs,6:F2}] ({m_maxProfile.pairs,6:F2})");
            DrawTextLine($"collide [ave] (max) = {p.collide,5:F2} [{aveProfile.collide,6:F2}] ({m_maxProfile.collide,6:F2})");
            DrawTextLine($"solve [ave] (max) = {p.solve,5:F2} [{aveProfile.solve,6:F2}] ({m_maxProfile.solve,6:F2})");
            DrawTextLine($"> prepare tasks [ave] (max) = {p.prepareStages,5:F2} [{aveProfile.prepareStages,6:F2}] ({m_maxProfile.prepareStages,6:F2})");
            DrawTextLine($"> solve constraints [ave] (max) = {p.solveConstraints,5:F2} [{aveProfile.solveConstraints,6:F2}] ({m_maxProfile.solveConstraints,6:F2})");
            DrawTextLine($">> prepare constraints [ave] (max) = {p.prepareConstraints,5:F2} [{aveProfile.prepareConstraints,6:F2}] ({m_maxProfile.prepareConstraints,6:F2})");
            DrawTextLine($">> integrate velocities [ave] (max) = {p.integrateVelocities,5:F2} [{aveProfile.integrateVelocities,6:F2}] ({m_maxProfile.integrateVelocities,6:F2})");
            DrawTextLine($">> warm start [ave] (max) = {p.warmStart,5:F2} [{aveProfile.warmStart,6:F2}] ({m_maxProfile.warmStart,6:F2})");
            DrawTextLine($">> solve impulses [ave] (max) = {p.solveImpulses,5:F2} [{aveProfile.solveImpulses,6:F2}] ({m_maxProfile.solveImpulses,6:F2})");
            DrawTextLine($">> integrate positions [ave] (max) = {p.integratePositions,5:F2} [{aveProfile.integratePositions,6:F2}] ({m_maxProfile.integratePositions,6:F2})");
            DrawTextLine($">> relax impulses [ave] (max) = {p.relaxImpulses,5:F2} [{aveProfile.relaxImpulses,6:F2}] ({m_maxProfile.relaxImpulses,6:F2})");
            DrawTextLine($">> apply restitution [ave] (max) = {p.applyRestitution,5:F2} [{aveProfile.applyRestitution,6:F2}] ({m_maxProfile.applyRestitution,6:F2})");
            DrawTextLine($">> store impulses [ave] (max) = {p.storeImpulses,5:F2} [{aveProfile.storeImpulses,6:F2}] ({m_maxProfile.storeImpulses,6:F2})");
            DrawTextLine($">> split islands [ave] (max) = {p.splitIslands,5:F2} [{aveProfile.splitIslands,6:F2}] ({m_maxProfile.splitIslands,6:F2})");
            DrawTextLine($"> update transforms [ave] (max) = {p.transforms,5:F2} [{aveProfile.transforms,6:F2}] ({m_maxProfile.transforms,6:F2})");
            DrawTextLine($"> joint events [ave] (max) = {p.jointEvents,5:F2} [{aveProfile.jointEvents,6:F2}] ({m_maxProfile.jointEvents})");
            DrawTextLine($"> hit events [ave] (max) = {p.hitEvents,5:F2} [{aveProfile.hitEvents,6:F2}] ({m_maxProfile.hitEvents,6:F2})");
            DrawTextLine($"> refit BVH [ave] (max) = {p.refit,5:F2} [{aveProfile.refit,6:F2}] ({m_maxProfile.refit,6:F2})");
            DrawTextLine($"> sleep islands [ave] (max) = {p.sleepIslands,5:F2} [{aveProfile.sleepIslands,6:F2}] ({m_maxProfile.sleepIslands,6:F2})");
            DrawTextLine($"> bullets [ave] (max) = {p.bullets,5:F2} [{aveProfile.bullets,6:F2}] ({m_maxProfile.bullets,6:F2})");
            DrawTextLine($"sensors [ave] (max) = {p.sensors,5:F2} [{aveProfile.sensors,6:F2}] ({m_maxProfile.sensors,6:F2})");
        }
    }


    private static object EnqueueTask(b2TaskCallback task, int itemCount, int minRange, object taskContext, object userContext)
    {
        Sample sample = userContext as Sample;
        if (sample.m_taskCount < m_maxTasks)
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
            B2_ASSERT(false);
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
        m_draw.DrawString(5, 5, title);
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

    public virtual void Keyboard(Keys a)
    {
    }

    public virtual void MouseDown(B2Vec2 p, MouseButton button, KeyModifiers mod)
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

            m_mousePoint = p;

            // Query the world for overlapping shapes.
            QueryContext queryContext = new QueryContext(p, b2_nullBodyId);
            b2World_OverlapAABB(m_worldId, box, b2DefaultQueryFilter(), QueryCallback, queryContext);

            if (B2_IS_NON_NULL(queryContext.bodyId))
            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_kinematicBody;
                bodyDef.position = p;
                bodyDef.enableSleep = false;
                m_mouseBodyId = b2CreateBody(m_worldId, ref bodyDef);

                B2MotorJointDef jointDef = b2DefaultMotorJointDef();
                jointDef.@base.bodyIdA = m_mouseBodyId;
                jointDef.@base.bodyIdB = queryContext.bodyId;
                jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(queryContext.bodyId, p);
                jointDef.linearHertz = 7.5f;
                jointDef.linearDampingRatio = 0.7f;

                B2MassData massData = b2Body_GetMassData(queryContext.bodyId);
                float g = b2Length(b2World_GetGravity(m_worldId));
                float mg = massData.mass * g;
                jointDef.maxSpringForce = 100.0f * mg;

                if (massData.mass > 0.0f)
                {
                    // This acts like angular friction
                    float lever = MathF.Sqrt(massData.rotationalInertia / massData.mass);
                    jointDef.maxVelocityTorque = 1.0f * lever * mg;
                }

                m_mouseJointId = b2CreateMotorJoint(m_worldId, ref jointDef);
            }
        }
    }

    public virtual void MouseUp(B2Vec2 p, MouseButton button)
    {
        if (B2_IS_NON_NULL(m_mouseJointId) && button == (int)MouseButton.Left)
        {
            b2DestroyJoint(m_mouseJointId);
            m_mouseJointId = b2_nullJointId;

            b2DestroyBody(m_mouseBodyId);
            m_mouseBodyId = b2_nullBodyId;
        }
    }

    public virtual void MouseMove(B2Vec2 p)
    {
        if (b2Joint_IsValid(m_mouseJointId) == false)
        {
            // The world or attached body was destroyed.
            m_mouseJointId = b2_nullJointId;
        }

        m_mousePoint = p;
    }

    public void DrawTextLine(string text)
    {
        ImGui.Begin("Overlay",
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize |
            ImGuiWindowFlags.NoScrollbar);
        ImGui.PushFont(m_context.draw.m_regularFont);
        ImGui.SetCursorPos(new Vector2(5.0f, (float)m_textLine));
        ImGui.TextColored(new Vector4(230, 153, 153, 255), text);
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

    public virtual void Step()
    {
        float timeStep = m_context.settings.hertz > 0.0f ? 1.0f / m_context.settings.hertz : 0.0f;

        if (m_context.settings.pause)
        {
            if (m_context.settings.singleStep)
            {
                m_context.settings.singleStep = false;
            }
            else
            {
                timeStep = 0.0f;
            }
        }

        if (B2_IS_NON_NULL(m_mouseJointId) && b2Joint_IsValid(m_mouseJointId) == false)
        {
            // The world or attached body was destroyed.
            m_mouseJointId = b2_nullJointId;

            if (B2_IS_NON_NULL(m_mouseBodyId))
            {
                b2DestroyBody(m_mouseBodyId);
                m_mouseBodyId = b2_nullBodyId;
            }
        }

        if (B2_IS_NON_NULL(m_mouseBodyId) && timeStep > 0.0f)
        {
            b2Body_SetTargetTransform(m_mouseBodyId, new B2Transform(m_mousePoint, b2Rot_identity), timeStep);
        }

        b2World_EnableSleeping(m_worldId, m_context.settings.enableSleep);
        b2World_EnableWarmStarting(m_worldId, m_context.settings.enableWarmStarting);
        b2World_EnableContinuous(m_worldId, m_context.settings.enableContinuous);

        for (int i = 0; i < 1; ++i)
        {
            b2World_Step(m_worldId, timeStep, m_context.settings.subStepCount);
            m_taskCount = 0;
        }

        if (timeStep > 0.0f)
        {
            ++m_stepCount;
        }

        // Track maximum profile times
        {
            B2Profile p = b2World_GetProfile(m_worldId);
            m_maxProfile.step = b2MaxFloat(m_maxProfile.step, p.step);
            m_maxProfile.pairs = b2MaxFloat(m_maxProfile.pairs, p.pairs);
            m_maxProfile.collide = b2MaxFloat(m_maxProfile.collide, p.collide);
            m_maxProfile.solve = b2MaxFloat(m_maxProfile.solve, p.solve);
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
            m_maxProfile.jointEvents = b2MaxFloat(m_maxProfile.jointEvents, p.jointEvents);
            m_maxProfile.hitEvents = b2MaxFloat(m_maxProfile.hitEvents, p.hitEvents);
            m_maxProfile.refit = b2MaxFloat(m_maxProfile.refit, p.refit);
            m_maxProfile.bullets = b2MaxFloat(m_maxProfile.bullets, p.bullets);
            m_maxProfile.sleepIslands = b2MaxFloat(m_maxProfile.sleepIslands, p.sleepIslands);
            m_maxProfile.sensors = b2MaxFloat(m_maxProfile.sensors, p.sensors);

            m_totalProfile.step += p.step;
            m_totalProfile.pairs += p.pairs;
            m_totalProfile.collide += p.collide;
            m_totalProfile.solve += p.solve;
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
            m_totalProfile.jointEvents += p.jointEvents;
            m_totalProfile.hitEvents += p.hitEvents;
            m_totalProfile.refit += p.refit;
            m_totalProfile.bullets += p.bullets;
            m_totalProfile.sleepIslands += p.sleepIslands;
            m_totalProfile.sensors += p.sensors;
        }
    }

    public virtual void Draw(Settings settings)
    {
        if (settings.pause)
        {
            if (m_context.draw.m_showUI)
            {
                DrawTextLine("****PAUSED****");
            }
        }

        m_context.draw.m_debugDraw.drawingBounds = m_camera.GetViewBounds();
        m_context.draw.m_debugDraw.drawShapes = settings.drawShapes;
        m_context.draw.m_debugDraw.drawJoints = settings.drawJoints;
        m_context.draw.m_debugDraw.drawJointExtras = settings.drawJointExtras;
        m_context.draw.m_debugDraw.drawBounds = settings.drawBounds;
        m_context.draw.m_debugDraw.drawMass = settings.drawMass;
        m_context.draw.m_debugDraw.drawBodyNames = settings.drawBodyNames;
        m_context.draw.m_debugDraw.drawContacts = settings.drawContactPoints;
        m_context.draw.m_debugDraw.drawGraphColors = settings.drawGraphColors;
        m_context.draw.m_debugDraw.drawContactNormals = settings.drawContactNormals;
        m_context.draw.m_debugDraw.drawContactImpulses = settings.drawContactImpulses;
        m_context.draw.m_debugDraw.drawContactFeatures = settings.drawContactFeatures;
        m_context.draw.m_debugDraw.drawFrictionImpulses = settings.drawFrictionImpulses;
        m_context.draw.m_debugDraw.drawIslands = settings.drawIslands;

        b2World_Draw(m_worldId, m_context.draw.m_debugDraw);

        if (settings.drawCounters)
        {
            B2Counters s = b2World_GetCounters(m_worldId);

            DrawTextLine($"bodies/shapes/contacts/joints = {s.bodyCount}/{s.shapeCount}/{s.contactCount}/{s.jointCount}");
            DrawTextLine($"islands/tasks = {s.islandCount}/{s.taskCount}");
            DrawTextLine($"tree height static/movable = {s.staticTreeHeight}/{s.treeHeight}");

            int totalCount = 0;
            var buffer = new StringBuilder();
            B2_ASSERT(s.colorCounts.Length == 12);

            // todo fix this
            buffer.Append("colors: ");
            for (int i = 0; i < s.colorCounts.Length; ++i)
            {
                buffer.Append($"{s.colorCounts[i]}/");
                totalCount += s.colorCounts[i];
            }

            buffer.Append($"[{totalCount}]");
            DrawTextLine(buffer.ToString());
            DrawTextLine($"stack allocator size = {s.stackUsed / 1024} K");
            DrawTextLine($"total allocation = {s.byteCount / 1024} K");
        }
    }

    public void ShiftOrigin(B2Vec2 newOrigin)
    {
        // m_world.ShiftOrigin(newOrigin);
    }


    protected InputAction GetKey(Keys key)
    {
        return GlfwHelpers.GetKey(m_context, key);
    }
}