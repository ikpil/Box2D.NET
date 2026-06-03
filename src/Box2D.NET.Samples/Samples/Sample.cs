// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using Box2D.NET.Samples.Graphics;
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
using static Box2D.NET.B2ConstraintGraphs;
using static Box2D.NET.Shared.RandomSupports;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.Samples.Graphics.Draws;
using static Box2D.NET.Samples.Graphics.Cameras;
using static Box2D.NET.B2Constants;

namespace Box2D.NET.Samples.Samples;

public class Sample : IDisposable
{
    public const int m_maxTasks = 512;
    public const int m_maxThreads = 64;
    public const int m_profileCapacity = 512;

#if DEBUG
    public const bool m_isDebug = true;
#else
    public const bool m_isDebug = false;
#endif

    protected SampleContext m_context;
    protected Camera m_camera;
    protected Draw m_draw;

    private B2BodyId m_mouseBodyId;

    //
    public B2WorldId m_worldId;
    private B2JointId m_mouseJointId;
    private B2Vec2 m_mousePoint;
    protected float m_mouseForceScale;
    public int m_stepCount;
    private int m_textLine;
    private int m_textIncrement;
    
    //
    private readonly B2Profile[] m_profiles;
    private int m_currentProfileIndex;
    private ulong m_profileReadIndex;
    private ulong m_profileWriteIndex;
    
    //
    private B2Profile m_totalProfile;
    private static bool s_showProfilePlots;
    private static readonly bool[] s_profileRowOpen = new bool[22];
    
    //
    private bool m_didStep;

    private readonly float[] m_frameTimes;

    public Sample(SampleContext context)
    {
        m_context = context;
        m_camera = context.camera;
        m_draw = context.draw;

        m_worldId = b2_nullWorldId;

        m_textIncrement = 26;
        m_textLine = m_textIncrement;
        m_mouseJointId = b2_nullJointId;

        m_stepCount = 0;
        m_didStep = false;

        m_mouseBodyId = b2_nullBodyId;
        m_mousePoint = new B2Vec2();
        m_mouseForceScale = 100.0f;

        m_frameTimes = new float[m_profileCapacity];

        m_profiles = new B2Profile[m_profileCapacity];
        m_currentProfileIndex = 0;
        m_profileReadIndex = 0;
        m_profileWriteIndex = 0;

        m_totalProfile = new B2Profile();

        g_randomSeed = RAND_SEED;

        CreateWorld();
        TestMathCpp();
    }

    public virtual void Dispose()
    {
        // By deleting the world, we delete the bomb, mouse joint, etc.
        b2DestroyWorld(m_worldId);
        
    }

    public void CreateWorld()
    {
        if (B2_IS_NON_NULL(m_worldId))
        {
            b2DestroyWorld(m_worldId);
            m_worldId = b2_nullWorldId;
        }

        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.workerCount = m_context.workerCount;
        worldDef.userTaskContext = this;
        worldDef.enableSleep = m_context.enableSleep;
        worldDef.capacity = m_context.capacity;

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

    public virtual void UpdateGui()
    {
        float fontSize = ImGui.GetFontSize();

        if (m_context.drawProfile)
        {
            ImGui.SetNextWindowPos(new Vector2(fontSize, 8.0f * fontSize), ImGuiCond.FirstUseEver);
            ImGui.Begin("Profile (ms)", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize);

            int count = (int)(m_profileWriteIndex - m_profileReadIndex);

            const int rowCount = 22;
            float[][] histories = new float[rowCount][];
            for (int row = 0; row < rowCount; ++row)
            {
                histories[row] = new float[m_profileCapacity];
            }

            for (int i = 0; i < count; ++i)
            {
                int index = (int)((m_profileReadIndex + (ulong)i) & (m_profileCapacity - 1));
                B2Profile profile = m_profiles[index];
                for (int row = 0; row < rowCount; ++row)
                {
                    histories[row][i] = GetProfileValue(profile, row);
                }
            }

            float[] avg = new float[rowCount];
            if (m_stepCount > 0)
            {
                float scale = 1.0f / m_stepCount;
                for (int row = 0; row < rowCount; ++row)
                {
                    avg[row] = scale * GetProfileValue(m_totalProfile, row);
                }
            }

            ref readonly B2Profile current = ref m_profiles[m_currentProfileIndex];
            string[] names =
            [
                "step", "pairs", "collide", "solve", "setup", "constraints", "prepare",
                "velocities", "warm start", "bias", "positions", "relax",
                "restitution", "store", "split islands", "transforms", "joint events",
                "hit events", "refit BVH", "sleep", "bullets", "sensors"
            ];
            int[] indents = [0, 0, 0, 0, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 0];
            int[] parents = new int[rowCount];
            bool[] hasChildren = new bool[rowCount];
            int[] stack = new int[8];
            int stackSize = 0;
            for (int i = 0; i < rowCount; ++i)
            {
                while (stackSize > 0 && indents[stack[stackSize - 1]] >= indents[i])
                {
                    stackSize -= 1;
                }

                parents[i] = stackSize > 0 ? stack[stackSize - 1] : -1;
                stack[stackSize] = i;
                stackSize += 1;

                if (parents[i] >= 0)
                {
                    hasChildren[parents[i]] = true;
                }
            }

            Vector4 colorStep = new Vector4(102.0f / 255.0f, 153.0f / 255.0f, 1.0f, 1.0f);
            Vector4 colorCollide = new Vector4(1.0f, 140.0f / 255.0f, 51.0f / 255.0f, 1.0f);
            Vector4 colorSolve = new Vector4(102.0f / 255.0f, 204.0f / 255.0f, 102.0f / 255.0f, 1.0f);
            Vector4 colorDefault = new Vector4(220.0f / 255.0f, 220.0f / 255.0f, 220.0f / 255.0f, 1.0f);

            Vector4[] colors =
            [
                colorStep, colorDefault, colorCollide, colorSolve, colorDefault, colorDefault,
                colorDefault, colorDefault, colorDefault, colorDefault, colorDefault, colorDefault,
                colorDefault, colorDefault, colorDefault, colorDefault, colorDefault, colorDefault,
                colorDefault, colorDefault, colorDefault, colorDefault
            ];
            float[] now =
            [
                current.step, current.pairs, current.collide, current.solve, current.solverSetup,
                current.constraints, current.prepareConstraints, current.integrateVelocities, current.warmStart,
                current.solveImpulses, current.integratePositions, current.relaxImpulses, current.applyRestitution,
                current.storeImpulses, current.splitIslands, current.transforms, current.jointEvents,
                current.hitEvents, current.refit, current.sleepIslands, current.bullets, current.sensors
            ];

            if (ImGui.Button("Reset"))
            {
                ResetProfile();
            }

            ImGui.SameLine();
            ImGui.Checkbox("Show plots", ref s_showProfilePlots);

            ImGuiTableFlags tableFlags = ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit;
            int columnCount = s_showProfilePlots ? 6 : 5;
            if (ImGui.BeginTable("profile", columnCount, tableFlags))
            {
                ImGui.TableSetupColumn("section", ImGuiTableColumnFlags.WidthFixed, 8.0f * fontSize);
                ImGui.TableSetupColumn("now", ImGuiTableColumnFlags.WidthFixed, 3.0f * fontSize);
                ImGui.TableSetupColumn("avg", ImGuiTableColumnFlags.WidthFixed, 3.0f * fontSize);
                ImGui.TableSetupColumn("max", ImGuiTableColumnFlags.WidthFixed, 3.0f * fontSize);
                ImGui.TableSetupColumn("% step", ImGuiTableColumnFlags.WidthFixed, 8.0f * fontSize);
                if (s_showProfilePlots)
                {
                    ImGui.TableSetupColumn("history", ImGuiTableColumnFlags.WidthFixed, 16.0f * fontSize);
                }
                ImGui.TableHeadersRow();

                float rowHeight = 1.5f * fontSize;
                float stepNow = b2MaxFloat(current.step, 0.001f);

                for (int row = 0; row < rowCount; ++row)
                {
                    bool visible = true;
                    for (int parent = parents[row]; parent >= 0; parent = parents[parent])
                    {
                        if (s_profileRowOpen[parent] == false)
                        {
                            visible = false;
                            break;
                        }
                    }

                    if (visible == false)
                    {
                        continue;
                    }

                    float[] history = histories[row];
                    float rollingMax = 0.0f;
                    for (int i = 0; i < count; ++i)
                    {
                        rollingMax = b2MaxFloat(rollingMax, history[i]);
                    }

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    if (indents[row] > 0)
                    {
                        ImGui.Indent(indents[row] * fontSize);
                    }
                    if (hasChildren[row])
                    {
                        ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick |
                                                   ImGuiTreeNodeFlags.NoTreePushOnOpen;
                        ImGui.PushStyleColor(ImGuiCol.Text, colors[row]);
                        s_profileRowOpen[row] = ImGui.TreeNodeEx(names[row], flags);
                        ImGui.PopStyleColor();
                    }
                    else
                    {
                        float leafIndent = ImGui.GetTreeNodeToLabelSpacing();
                        ImGui.Indent(leafIndent);
                        ImGui.PushStyleColor(ImGuiCol.Text, colors[row]);
                        ImGui.TextUnformatted(names[row]);
                        ImGui.PopStyleColor();
                        ImGui.Unindent(leafIndent);
                    }
                    if (indents[row] > 0)
                    {
                        ImGui.Unindent(indents[row] * fontSize);
                    }

                    ImGui.TableNextColumn();
                    ImGui.Text($"{now[row],6:F2}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{avg[row],6:F2}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{rollingMax,6:F2}");

                    ImGui.TableNextColumn();
                    float frac = b2ClampFloat(now[row] / stepNow, 0.0f, 1.0f);
                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, colors[row]);
                    ImGui.ProgressBar(frac, new Vector2(-float.Epsilon, 0.0f), "");
                    ImGui.PopStyleColor();

                    if (s_showProfilePlots)
                    {
                        ImGui.TableNextColumn();
                        if (count > 1)
                        {
                            ImGui.PushStyleColor(ImGuiCol.PlotLines, colors[row]);
                            ImGui.PlotLines($"##h{row}", ref history[0], count, 0, null, 0.0f, rollingMax * 1.05f + 0.001f, new Vector2(-float.Epsilon, rowHeight));
                            ImGui.PopStyleColor();
                        }
                    }
                }

                ImGui.EndTable();
            }

            ImGui.End();
        }

        if (m_context.drawCounters)
        {
            B2Counters s = b2World_GetCounters(m_worldId);
            int colorCount = s.colorCounts.Length;
            int overflowIndex = colorCount - 1;
            int totalCount = 0;
            int maxCount = 1;
            for (int i = 0; i < colorCount; ++i)
            {
                totalCount += s.colorCounts[i];
                if (i != overflowIndex && s.colorCounts[i] > maxCount)
                {
                    maxCount = s.colorCounts[i];
                }
            }

            ImGui.SetNextWindowPos(new Vector2(fontSize, 8.0f * fontSize), ImGuiCond.FirstUseEver);
            ImGui.Begin("Counters", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize);
            ImGui.Text($"bodies/shapes/contacts/joints = {s.bodyCount}/{s.shapeCount}/{s.contactCount}/{s.jointCount}");

            float recycledFraction = s.awakeContactCount > 0
                ? b2ClampFloat((float)s.recycledContactCount / s.awakeContactCount, 0.0f, 1.0f)
                : 0.0f;
            ImGui.TextUnformatted("recycled contacts");
            ImGui.SameLine();
            ImGui.ProgressBar(recycledFraction, new Vector2(-float.Epsilon, 0.0f), $"{s.recycledContactCount} / {s.awakeContactCount}");

            ImGui.Text($"islands/tasks = {s.islandCount}/{s.taskCount}");
            ImGui.Text($"tree height static/movable = {s.staticTreeHeight}/{s.treeHeight}");
            ImGui.Text($"stack allocator size = {s.stackUsed / 1024} K");
            ImGui.Text($"total allocation = {s.byteCount / 1024} K");

            ImGui.Separator();
            B2Capacity capacity = b2World_GetMaxCapacity(m_worldId);
            ImGui.TextUnformatted("max capacities");
            ImGui.BulletText($"static shapes/bodies = {capacity.staticShapeCount}/{capacity.staticBodyCount}");
            ImGui.BulletText($"dynamic shapes/bodies = {capacity.dynamicShapeCount}/{capacity.dynamicBodyCount}");
            ImGui.BulletText($"contacts = {capacity.contactCount}");

            ImGui.Separator();
            ImGui.Text($"{totalCount} constraints across {colorCount} colors");

            ImGuiTableFlags tableFlags = ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit;
            if (ImGui.BeginTable("graphColors", 3, tableFlags))
            {
                ImGui.TableSetupColumn("color", ImGuiTableColumnFlags.WidthFixed, 3.5f * fontSize);
                ImGui.TableSetupColumn("count", ImGuiTableColumnFlags.WidthFixed, 5.0f * fontSize);
                ImGui.TableSetupColumn("share", ImGuiTableColumnFlags.WidthFixed, 16.0f * fontSize);
                ImGui.TableHeadersRow();

                float invMax = 1.0f / maxCount;
                for (int i = 0; i < colorCount; ++i)
                {
                    int count = s.colorCounts[i];
                    bool isOverflow = i == overflowIndex;
                    if (count == 0 && isOverflow == false)
                    {
                        continue;
                    }

                    Vector4 color = isOverflow ? new Vector4(0.86f, 0.24f, 0.24f, 1.0f) : HexToColor(b2GetGraphColor(i));

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.PushStyleColor(ImGuiCol.Text, color);
                    ImGui.TextUnformatted(isOverflow ? "over" : i.ToString());
                    ImGui.PopStyleColor();

                    ImGui.TableNextColumn();
                    ImGui.Text(count.ToString());

                    ImGui.TableNextColumn();
                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, color);
                    ImGui.ProgressBar(b2ClampFloat(count * invMax, 0.0f, 1.0f), new Vector2(-float.Epsilon, 0.0f), "");
                    ImGui.PopStyleColor();
                }

                ImGui.EndTable();
            }

            ImGui.End();
        }

        if (m_context.frameTime)
        {
            UpdateFrameTimeGui(fontSize);
        }
    }

    private static float GetProfileValue(in B2Profile profile, int row)
    {
        return row switch
        {
            0 => profile.step,
            1 => profile.pairs,
            2 => profile.collide,
            3 => profile.solve,
            4 => profile.solverSetup,
            5 => profile.constraints,
            6 => profile.prepareConstraints,
            7 => profile.integrateVelocities,
            8 => profile.warmStart,
            9 => profile.solveImpulses,
            10 => profile.integratePositions,
            11 => profile.relaxImpulses,
            12 => profile.applyRestitution,
            13 => profile.storeImpulses,
            14 => profile.splitIslands,
            15 => profile.transforms,
            16 => profile.jointEvents,
            17 => profile.hitEvents,
            18 => profile.refit,
            19 => profile.sleepIslands,
            20 => profile.bullets,
            21 => profile.sensors,
            _ => 0.0f,
        };
    }

    private static Vector4 HexToColor(B2HexColor color)
    {
        uint hex = (uint)color;
        return new Vector4(((hex >> 16) & 0xFF) / 255.0f, ((hex >> 8) & 0xFF) / 255.0f, (hex & 0xFF) / 255.0f, 1.0f);
    }

    private void UpdateFrameTimeGui(float fontSize)
    {
        float frameTimeHeight = 30.0f * fontSize;
        float frameTimeWidth = 50.0f * fontSize;

        ImGui.SetNextWindowPos(new Vector2(3.0f * fontSize, 3.0f * fontSize), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(frameTimeWidth, frameTimeHeight), ImGuiCond.FirstUseEver);

        ImGui.Begin("Frame Time", ref m_context.frameTime, ImGuiWindowFlags.NoCollapse);
        ImGui.PushItemWidth(ImGui.GetWindowWidth() - 2.0f * fontSize);

        int count = (int)(m_profileWriteIndex - m_profileReadIndex);
        float maxValue = 0.0f;
        for (int i = 0; i < count; ++i)
        {
            int index = (int)((m_profileReadIndex + (ulong)i) & (m_profileCapacity - 1));
            m_frameTimes[i] = i / 60.0f;
            maxValue = b2MaxFloat(maxValue, m_profiles[index].step);
        }

        // This is the pixel size, not the range.
        Vector2 plotSize = new Vector2(-1.0f, 22.0f * fontSize);
        DrawProfilePlot("Profile", count, maxValue, plotSize);

        ImGui.PopItemWidth();
        ImGui.End();
    }

    private void DrawProfilePlot(string label, int count, float maxValue, Vector2 size)
    {
        if (count == 0)
        {
            ImGui.TextUnformatted("No frame data");
            return;
        }

        maxValue = b2MaxFloat(maxValue, 0.001f);
        Vector2 canvasPos = ImGui.GetCursorScreenPos();
        Vector2 canvasSize = size;
        if (canvasSize.X < 0.0f)
        {
            canvasSize.X = ImGui.GetContentRegionAvail().X;
        }

        canvasSize.X = MathF.Max(canvasSize.X, 120.0f);
        canvasSize.Y = MathF.Max(canvasSize.Y, 120.0f);

        ImGui.InvisibleButton(label, canvasSize);

        Vector2 min = canvasPos;
        Vector2 max = canvasPos + canvasSize;
        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
        uint borderColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.35f, 0.35f, 0.35f, 1.0f));
        uint gridColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.20f, 0.20f, 0.20f, 1.0f));
        uint textColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.85f, 0.85f, 0.85f, 1.0f));

        drawList.AddRect(min, max, borderColor);
        for (int i = 1; i < 4; ++i)
        {
            float y = min.Y + canvasSize.Y * i / 4.0f;
            drawList.AddLine(new Vector2(min.X, y), new Vector2(max.X, y), gridColor);
        }

        DrawProfileSeries(drawList, min, canvasSize, count, maxValue, p => p.step, new Vector4(0.20f, 0.70f, 1.00f, 1.0f));
        DrawProfileSeries(drawList, min, canvasSize, count, maxValue, p => p.collide, new Vector4(0.95f, 0.65f, 0.20f, 1.0f));
        DrawProfileSeries(drawList, min, canvasSize, count, maxValue, p => p.solve, new Vector4(0.30f, 0.85f, 0.45f, 1.0f));

        drawList.AddText(min + new Vector2(8.0f, 6.0f), textColor, $"step / collide / solve    max {maxValue:F2} ms");
        drawList.AddText(new Vector2(min.X + 8.0f, max.Y - 22.0f), textColor, $"0s .. {m_frameTimes[count - 1]:F1}s");
    }

    private void DrawProfileSeries(ImDrawListPtr drawList, Vector2 origin, Vector2 size, int count, float maxValue, Func<B2Profile, float> selector, Vector4 color)
    {
        if (count < 2)
        {
            return;
        }

        uint lineColor = ImGui.ColorConvertFloat4ToU32(color);
        Vector2 previous = default;
        float invCount = 1.0f / (count - 1);

        for (int i = 0; i < count; ++i)
        {
            int index = (int)((m_profileReadIndex + (ulong)i) & (m_profileCapacity - 1));
            float value = selector(m_profiles[index]);
            float x = origin.X + size.X * i * invCount;
            float y = origin.Y + size.Y * (1.0f - b2ClampFloat(value / maxValue, 0.0f, 1.0f));
            Vector2 current = new Vector2(x, y);

            if (i > 0)
            {
                drawList.AddLine(previous, current, lineColor, 1.5f);
            }

            previous = current;
        }
    }
    public void ResetText()
    {
        m_textLine = m_textIncrement;
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
                bodyDef.position = m_mousePoint;
                bodyDef.enableSleep = false;
                m_mouseBodyId = b2CreateBody(m_worldId, bodyDef);

                B2MotorJointDef jointDef = b2DefaultMotorJointDef();
                jointDef.@base.bodyIdA = m_mouseBodyId;
                jointDef.@base.bodyIdB = queryContext.bodyId;
                jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(queryContext.bodyId, p);
                jointDef.linearHertz = 7.5f;
                jointDef.linearDampingRatio = 1.0f;

                B2MassData massData = b2Body_GetMassData(queryContext.bodyId);
                float g = b2Length(b2World_GetGravity(m_worldId));
                float mg = massData.mass * g;
                jointDef.maxSpringForce = m_mouseForceScale * mg;

                if (massData.mass > 0.0f)
                {
                    // This acts like angular friction
                    float lever = MathF.Sqrt(massData.rotationalInertia / massData.mass);
                    jointDef.maxVelocityTorque = 0.25f * lever * mg;
                }

                m_mouseJointId = b2CreateMotorJoint(m_worldId, jointDef);
            }
        }
    }

    public virtual void MouseUp(B2Vec2 p, MouseButton button)
    {
        if (B2_IS_NON_NULL(m_mouseJointId) && button == (int)MouseButton.Left)
        {
            b2DestroyJoint(m_mouseJointId, true);
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

    public void DrawColoredTextLine(B2HexColor color, string text)
    {
        if (m_context.showUI == false)
        {
            return;
        }

        DrawScreenString(m_draw, 5, m_textLine, color, text);
        m_textLine += m_textIncrement;
    }


    public void DrawTextLine(string text)
    {
        if (m_context.showUI == false)
        {
            return;
        }

        DrawScreenString(m_draw, 5, m_textLine, B2HexColor.b2_colorWhite, text);
        m_textLine += m_textIncrement;
    }

    public void ResetProfile()
    {
        m_totalProfile = new B2Profile();
        m_stepCount = 0;
        m_currentProfileIndex = 0;
        m_profileReadIndex = 0;
        m_profileWriteIndex = 0;
    }

    public virtual void Step()
    {
        m_didStep = false;

        float timeStep = m_context.hertz > 0.0f ? 1.0f / m_context.hertz : 0.0f;

        if (m_context.pause)
        {
            if (m_context.singleStep)
            {
                m_context.singleStep = false;
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
            bool wake = true;
            b2Body_SetTargetTransform(m_mouseBodyId, new B2Transform(m_mousePoint, b2Rot_identity), timeStep, wake);
        }

        b2World_EnableSleeping(m_worldId, m_context.enableSleep);
        b2World_EnableWarmStarting(m_worldId, m_context.enableWarmStarting);
        b2World_EnableContinuous(m_worldId, m_context.enableContinuous);

        if (m_context.enableRecycling)
        {
            b2World_SetContactRecycleDistance(m_worldId, B2_CONTACT_RECYCLE_DISTANCE);
        }
        else
        {
            b2World_SetContactRecycleDistance(m_worldId, 0.0f);
        }

        for (int i = 0; i < 1; ++i)
        {
            b2World_Step(m_worldId, timeStep, m_context.subStepCount);
            // m_taskCount = 0;
        }

        if (timeStep > 0.0f)
        {
            m_stepCount += 1;
            m_didStep = true;

            if (m_profileWriteIndex == m_profileCapacity + m_profileReadIndex)
            {
                m_profileReadIndex += 1;
            }

            m_currentProfileIndex = (int)(m_profileWriteIndex & (m_profileCapacity - 1));
            m_profiles[m_currentProfileIndex] = b2World_GetProfile(m_worldId);
            m_profileWriteIndex += 1;
        }

        // Accumulate profile averages
        if (m_didStep)
        {
            B2Profile p = m_profiles[m_currentProfileIndex];
            m_totalProfile.step += p.step;
            m_totalProfile.pairs += p.pairs;
            m_totalProfile.collide += p.collide;
            m_totalProfile.solve += p.solve;
            m_totalProfile.solverSetup += p.solverSetup;
            m_totalProfile.constraints += p.constraints;
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

    public virtual void Draw()
    {
        if (m_context.pause)
        {
            if (m_context.showUI)
            {
                DrawTextLine("****PAUSED****");
            }
        }

        m_context.debugDraw.drawingBounds = GetViewBounds(m_context.camera);

        b2World_Draw(m_worldId, m_context.debugDraw);

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
