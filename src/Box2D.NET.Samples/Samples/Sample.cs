// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
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
using static Box2D.NET.B2Cores;
using static Box2D.NET.Samples.SampleText;

namespace Box2D.NET.Samples.Samples;

public class Sample : IDisposable
{
    public const int m_maxTasks = 512;
    public const int m_maxThreads = 64;
    public const int m_profileCapacity = 512;
    public const int m_maxHudLines = 64;
    private const int MAX_SAMPLES = 256;

    public struct HudLine
    {
        public B2HexColor color;
        public string text;
    }

    private readonly struct ProfileRowDef
    {
        public readonly string name;
        public readonly int indent;
        public readonly Vector4 color;

        public ProfileRowDef(string name, int indent, Vector4 color)
        {
            this.name = name;
            this.indent = indent;
            this.color = color;
        }
    }

    private struct ScoredSample
    {
        public int index;
        public int score;
    }

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

    public readonly HudLine[] m_hudLines;
    public int m_hudLineCount;
    private float m_screenTextY;
    
    //
    private readonly B2Profile[] m_profiles;
    private int m_currentProfileIndex;
    private ulong m_profileReadIndex;
    private ulong m_profileWriteIndex;
    
    //
    private static bool s_showProfilePlots;
    private static readonly bool[] s_profileRowOpen = new bool[22];
    private static bool s_showHelp;
    private static bool s_showAbout;
    private static string s_pickerQuery = string.Empty;
    private static string s_previousPickerQuery = string.Empty;
    private static int s_pickerHighlight;
    private static int s_previousPickerHighlight;
    private static readonly int[] s_filteredSamples = new int[MAX_SAMPLES];
    private static readonly ScoredSample[] s_scoredSamples = new ScoredSample[MAX_SAMPLES];
    private static int s_filteredCount;
    private static bool s_pickerJustOpened;
    private static bool s_forcePickerScroll;
    
    //
    private bool m_didStep;

    //
    private readonly float[] m_frameTimes;

    public Sample(SampleContext context)
    {
        m_context = context;
        m_camera = context.camera;
        m_draw = context.draw;

        m_worldId = b2_nullWorldId;

        m_mouseJointId = b2_nullJointId;

        m_stepCount = 0;
        m_didStep = false;
        m_hudLines = new HudLine[m_maxHudLines];
        m_hudLineCount = 0;
        m_screenTextY = 0.0f;

        m_mouseBodyId = b2_nullBodyId;
        m_mousePoint = new B2Vec2();
        m_mouseForceScale = 100.0f;

        m_frameTimes = new float[m_profileCapacity];

        m_profiles = new B2Profile[m_profileCapacity];
        m_currentProfileIndex = 0;
        m_profileReadIndex = 0;
        m_profileWriteIndex = 0;

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
        b2World_SetContactRecycleDistance(m_worldId, m_context.recycleDistance);
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

        if (m_context.showDiagnostics == false)
        {
            return;
        }

        float menuWidth = 14.0f * fontSize;
        float drawerHeight = 16.0f * fontSize;
        float drawerWidth = m_camera.width - menuWidth - 1.5f * fontSize;

        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.height - drawerHeight - 0.5f * fontSize));
        ImGui.SetNextWindowSize(new Vector2(drawerWidth, drawerHeight));

        ImGui.Begin("Diagnostics",
            ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoTitleBar);

        if (ImGui.BeginTabBar("DiagnosticsTabs", ImGuiTabBarFlags.None))
        {
            if (ImGui.BeginTabItem("Profile"))
            {
                int count = (int)(m_profileWriteIndex - m_profileReadIndex);

                const int kRowCount = 22;
                float[][] histories = new float[kRowCount][];
                float[] totals = new float[kRowCount];
                for (int i = 0; i < kRowCount; ++i)
                {
                    histories[i] = new float[m_profileCapacity];
                }

                for (int i = 0; i < count; ++i)
                {
                    int idx = (int)((m_profileReadIndex + (ulong)i) & (m_profileCapacity - 1));
                    ref readonly B2Profile p = ref m_profiles[idx];
                    histories[0][i] = p.step;
                    histories[1][i] = p.pairs;
                    histories[2][i] = p.collide;
                    histories[3][i] = p.solve;
                    histories[4][i] = p.solverSetup;
                    histories[5][i] = p.constraints;
                    histories[6][i] = p.prepareConstraints;
                    histories[7][i] = p.integrateVelocities;
                    histories[8][i] = p.warmStart;
                    histories[9][i] = p.solveImpulses;
                    histories[10][i] = p.integratePositions;
                    histories[11][i] = p.relaxImpulses;
                    histories[12][i] = p.applyRestitution;
                    histories[13][i] = p.storeImpulses;
                    histories[14][i] = p.splitIslands;
                    histories[15][i] = p.transforms;
                    histories[16][i] = p.jointEvents;
                    histories[17][i] = p.hitEvents;
                    histories[18][i] = p.refit;
                    histories[19][i] = p.sleepIslands;
                    histories[20][i] = p.bullets;
                    histories[21][i] = p.sensors;
                    for (int j = 0; j < kRowCount; ++j)
                    {
                        totals[j] += histories[j][i];
                    }
                }

                // "now" smoothed over the last few frames so bars don't jitter visibly.
                const int kNowWindow = 10;
                float[] now = new float[kRowCount];
                {
                    int n = count < kNowWindow ? count : kNowWindow;
                    if (n > 0)
                    {
                        float inv = 1.0f / n;
                        for (int r = 0; r < kRowCount; ++r)
                        {
                            float sum = 0.0f;
                            for (int i = count - n; i < count; ++i)
                            {
                                sum += histories[r][i];
                            }
                            now[r] = sum * inv;
                        }
                    }
                }

                float[] avg = new float[kRowCount];
                if (count > 0)
                {
                    float scale = 1.0f / count;
                    for (int i = 0; i < kRowCount; ++i)
                    {
                        avg[i] = scale * totals[i];
                    }
                }

                float[] rowMax = new float[kRowCount];
                for (int r = 0; r < kRowCount; ++r)
                {
                    for (int i = 0; i < count; ++i)
                    {
                        if (histories[r][i] > rowMax[r])
                        {
                            rowMax[r] = histories[r][i];
                        }
                    }
                }

                Vector4 colorStep = new Vector4(102.0f / 255.0f, 153.0f / 255.0f, 1.0f, 1.0f);
                Vector4 colorPairs = new Vector4(220.0f / 255.0f, 220.0f / 255.0f, 220.0f / 255.0f, 1.0f);
                Vector4 colorCollide = new Vector4(1.0f, 140.0f / 255.0f, 51.0f / 255.0f, 1.0f);
                Vector4 colorSolve = new Vector4(102.0f / 255.0f, 204.0f / 255.0f, 102.0f / 255.0f, 1.0f);
                Vector4 colorSensors = new Vector4(200.0f / 255.0f, 120.0f / 255.0f, 220.0f / 255.0f, 1.0f);
                Vector4 colorOther = new Vector4(90.0f / 255.0f, 90.0f / 255.0f, 90.0f / 255.0f, 1.0f);
                Vector4 colorDefault = new Vector4(220.0f / 255.0f, 220.0f / 255.0f, 220.0f / 255.0f, 1.0f);

                ProfileRowDef[] rows =
                [
                    new("step", 0, colorStep), new("pairs", 0, colorPairs), new("collide", 0, colorCollide),
                    new("solve", 0, colorSolve), new("setup", 1, colorDefault), new("constraints", 1, colorDefault),
                    new("prepare", 2, colorDefault), new("velocities", 2, colorDefault), new("warm start", 2, colorDefault),
                    new("bias", 2, colorDefault), new("positions", 2, colorDefault), new("relax", 2, colorDefault),
                    new("restitution", 2, colorDefault), new("store", 2, colorDefault), new("split islands", 2, colorDefault),
                    new("transforms", 1, colorDefault), new("joint events", 1, colorDefault), new("hit events", 1, colorDefault),
                    new("refit BVH", 1, colorDefault), new("sleep", 1, colorDefault), new("bullets", 1, colorDefault),
                    new("sensors", 0, colorSensors),
                ];

                int[] parents = new int[kRowCount];
                bool[] hasChildren = new bool[kRowCount];
                {
                    int[] stack = new int[8];
                    int stackSize = 0;
                    for (int i = 0; i < kRowCount; ++i)
                    {
                        while (stackSize > 0 && rows[stack[stackSize - 1]].indent >= rows[i].indent)
                        {
                            --stackSize;
                        }
                        parents[i] = stackSize > 0 ? stack[stackSize - 1] : -1;
                        stack[stackSize++] = i;
                        if (parents[i] >= 0)
                        {
                            hasChildren[parents[i]] = true;
                        }
                    }
                }

                float stepNow = b2MaxFloat(now[0], 0.001f);

                if (ImGui.Button("Reset"))
                {
                    ResetProfile();
                }
                ImGui.SameLine();
                ImGui.Checkbox("Show plots", ref s_showProfilePlots);
                ImGui.SameLine();
                ImGui.Text($"   step {now[0]:F2} ms");

                // Flame strip: step subdivided by top-level children.
                {
                    float pairsT = now[1];
                    float collideT = now[2];
                    float solveT = now[3];
                    float sensorsT = now[21];
                    float otherT = b2MaxFloat(stepNow - pairsT - collideT - solveT - sensorsT, 0.0f);

                    float availWidth = ImGui.GetContentRegionAvail().X;
                    float barHeight = 1.5f * fontSize;
                    ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                    Vector2 cursor = ImGui.GetCursorScreenPos();
                    float x = cursor.X;

                    void AddSegment(float t, Vector4 color)
                    {
                        float width = availWidth * (t / stepNow);
                        if (width > 0.0f)
                        {
                            drawList.AddRectFilled(new Vector2(x, cursor.Y), new Vector2(x + width, cursor.Y + barHeight), ImGui.ColorConvertFloat4ToU32(color));
                            x += width;
                        }
                    }

                    AddSegment(pairsT, colorPairs);
                    AddSegment(collideT, colorCollide);
                    AddSegment(solveT, colorSolve);
                    AddSegment(sensorsT, colorSensors);
                    AddSegment(otherT, colorOther);

                    ImGui.Dummy(new Vector2(availWidth, barHeight));
                }

                ImGuiTableFlags tableFlags = ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg |
                                              ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.ScrollY;

                int columnCount = s_showProfilePlots ? 6 : 5;
                Vector2 tableSize = ImGui.GetContentRegionAvail();
                if (ImGui.BeginTable("profile", columnCount, tableFlags, tableSize))
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

                    for (int r = 0; r < kRowCount; ++r)
                    {
                        bool visible = true;
                        for (int p = parents[r]; p >= 0; p = parents[p])
                        {
                            if (s_profileRowOpen[p] == false)
                            {
                                visible = false;
                                break;
                            }
                        }
                        if (visible == false)
                        {
                            continue;
                        }

                        // Hide leaf rows that are entirely zero; parents stay so structure reads.
                        if (hasChildren[r] == false && now[r] == 0.0f && avg[r] == 0.0f && rowMax[r] == 0.0f)
                        {
                            continue;
                        }

                        ProfileRowDef row = rows[r];
                        float[] history = histories[r];

                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        if (row.indent > 0)
                        {
                            ImGui.Indent(row.indent * fontSize);
                        }
                        if (hasChildren[r])
                        {
                            ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick |
                                                       ImGuiTreeNodeFlags.NoTreePushOnOpen;
                            ImGui.PushStyleColor(ImGuiCol.Text, row.color);
                            s_profileRowOpen[r] = ImGui.TreeNodeEx(row.name, flags);
                            ImGui.PopStyleColor();
                        }
                        else
                        {
                            float leafIndent = ImGui.GetTreeNodeToLabelSpacing();
                            ImGui.Indent(leafIndent);
                            ImGui.PushStyleColor(ImGuiCol.Text, row.color);
                            ImGui.TextUnformatted(row.name);
                            ImGui.PopStyleColor();
                            ImGui.Unindent(leafIndent);
                        }
                        if (row.indent > 0)
                        {
                            ImGui.Unindent(row.indent * fontSize);
                        }

                        ImGui.TableNextColumn();
                        ImGui.Text($"{now[r],6:F2}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{avg[r],6:F2}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{rowMax[r],6:F2}");

                        ImGui.TableNextColumn();
                        float fraction = b2ClampFloat(now[r] / stepNow, 0.0f, 1.0f);
                        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, row.color);
                        ImGui.ProgressBar(fraction, new Vector2(-float.Epsilon, 0.0f), "");
                        ImGui.PopStyleColor();

                        if (s_showProfilePlots)
                        {
                            ImGui.TableNextColumn();
                            if (count > 1)
                            {
                                ImGui.PushStyleColor(ImGuiCol.PlotLines, row.color);
                                ImGui.PlotLines($"##h{r}", ref history[0], count, 0, null, 0.0f, rowMax[r] * 1.05f + 0.001f,
                                    new Vector2(-float.Epsilon, rowHeight));
                                ImGui.PopStyleColor();
                            }
                        }
                    }
                    ImGui.EndTable();
                }

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Frame Time"))
            {
                float maxValue = 0.0f;
                int count = (int)(m_profileWriteIndex - m_profileReadIndex);
                for (int i = 0; i < count; ++i)
                {
                    int index = (int)((m_profileReadIndex + (ulong)i) & (m_profileCapacity - 1));
                    m_frameTimes[i] = i / 60.0f;
                    maxValue = b2MaxFloat(m_profiles[index].step, maxValue);
                }

                // This project does not depend on ImPlot, so draw the same profile data with ImGui's draw list.
                DrawProfilePlot("Profile", count, maxValue, m_profileCapacity / 60.0f, ImGui.GetContentRegionAvail());

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Counters"))
            {
                B2Counters counters = b2World_GetCounters(m_worldId);
                B2Capacity capacity = b2World_GetMaxCapacity(m_worldId);
                int colorCount = counters.colorCounts.Length;
                int overflowIndex = colorCount - 1;

                if (ImGui.BeginTable("counters_layout", 2, ImGuiTableFlags.SizingFixedFit))
                {
                    ImGui.TableSetupColumn("left", ImGuiTableColumnFlags.WidthFixed, 22.0f * fontSize);
                    ImGui.TableSetupColumn("right", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    ImGui.Text($"bodies   {counters.bodyCount} / {capacity.staticBodyCount + capacity.dynamicBodyCount}");
                    ImGui.Text($"shapes   {counters.shapeCount} / {capacity.staticShapeCount + capacity.dynamicShapeCount}");
                    ImGui.Text($"contacts {counters.contactCount} / {capacity.contactCount}");
                    ImGui.Text($"joints   {counters.jointCount}");
                    ImGui.Text($"islands/tasks {counters.islandCount} / {counters.taskCount}");
                    ImGui.Text($"tree height static/movable {counters.staticTreeHeight} / {counters.treeHeight}");
                    ImGui.Text($"alloc {counters.byteCount / 1024} K   stack {counters.stackUsed / 1024} K");

                    {
                        float fraction = counters.awakeContactCount > 0
                            ? b2ClampFloat((float)counters.recycledContactCount / counters.awakeContactCount, 0.0f, 1.0f)
                            : 0.0f;
                        ImGui.TextUnformatted("recycled");
                        ImGui.SameLine();
                        ImGui.ProgressBar(fraction, new Vector2(-float.Epsilon, 0.0f),
                            $"{counters.recycledContactCount} / {counters.awakeContactCount}");
                    }

                    ImGui.TableNextColumn();

                    int totalCount = 0;
                    int normalCount = 0;
                    for (int i = 0; i < colorCount; ++i)
                    {
                        totalCount += counters.colorCounts[i];
                        if (i != overflowIndex)
                        {
                            normalCount += counters.colorCounts[i];
                        }
                    }
                    int overflowCount = counters.colorCounts[overflowIndex];

                    ImGui.Text($"{totalCount} constraints across {colorCount - 1} colors");

                    float availableWidth = ImGui.GetContentRegionAvail().X;
                    float barHeight = 2.0f * fontSize;
                    ImDrawListPtr drawList = ImGui.GetWindowDrawList();

                    Vector2 cursor = ImGui.GetCursorScreenPos();
                    drawList.AddRectFilled(cursor, new Vector2(cursor.X + availableWidth, cursor.Y + barHeight),
                        ImGui.ColorConvertFloat4ToU32(new Vector4(40.0f / 255.0f, 40.0f / 255.0f, 40.0f / 255.0f, 1.0f)));
                    if (normalCount > 0)
                    {
                        float x = cursor.X;
                        float invTotal = 1.0f / normalCount;
                        for (int i = 0; i < overflowIndex; ++i)
                        {
                            int itemCount = counters.colorCounts[i];
                            if (itemCount == 0)
                            {
                                continue;
                            }
                            float segmentWidth = availableWidth * itemCount * invTotal;
                            uint color = ImGui.ColorConvertFloat4ToU32(HexToColor(b2GetGraphColor(i)));
                            drawList.AddRectFilled(new Vector2(x, cursor.Y), new Vector2(x + segmentWidth, cursor.Y + barHeight), color);
                            x += segmentWidth;
                        }
                    }
                    ImGui.Dummy(new Vector2(availableWidth, barHeight));

                    ImGui.Spacing();
                    float overflowFraction = totalCount > 0 ? (float)overflowCount / totalCount : 0.0f;
                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(220.0f / 255.0f, 60.0f / 255.0f, 60.0f / 255.0f, 1.0f));
                    ImGui.ProgressBar(overflowFraction, new Vector2(-float.Epsilon, 0.0f), $"overflow {overflowCount}");
                    ImGui.PopStyleColor();

                    ImGui.EndTable();
                }

                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.End();
    }

    private static Vector4 HexToColor(B2HexColor color)
    {
        uint hex = (uint)color;
        return new Vector4(((hex >> 16) & 0xFF) / 255.0f, ((hex >> 8) & 0xFF) / 255.0f, (hex & 0xFF) / 255.0f, 1.0f);
    }

    private void DrawProfilePlot(string label, int count, float maxValue, float maxTime, Vector2 size)
    {
        Vector2 canvasPos = ImGui.GetCursorScreenPos();
        Vector2 canvasSize = size;
        if (canvasSize.X < 0.0f)
        {
            canvasSize.X = ImGui.GetContentRegionAvail().X;
        }

        canvasSize.X = MathF.Max(canvasSize.X, 120.0f);
        canvasSize.Y = MathF.Max(canvasSize.Y, 120.0f);

        ImGui.InvisibleButton(label, canvasSize);

        Vector2 min = canvasPos + new Vector2(42.0f, 8.0f);
        Vector2 max = canvasPos + canvasSize - new Vector2(12.0f, 28.0f);
        Vector2 plotSize = max - min;
        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
        uint borderColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.35f, 0.35f, 0.35f, 1.0f));
        uint gridColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.20f, 0.20f, 0.20f, 1.0f));
        uint textColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.85f, 0.85f, 0.85f, 1.0f));
        Vector4 stepColor = new Vector4(0.20f, 0.70f, 1.00f, 1.0f);
        Vector4 collideColor = new Vector4(0.95f, 0.65f, 0.20f, 1.0f);
        Vector4 solveColor = new Vector4(0.30f, 0.85f, 0.45f, 1.0f);

        drawList.AddRect(min, max, borderColor);
        for (int i = 1; i < 4; ++i)
        {
            float y = min.Y + plotSize.Y * i / 4.0f;
            drawList.AddLine(new Vector2(min.X, y), new Vector2(max.X, y), gridColor);

            float x = min.X + plotSize.X * i / 4.0f;
            drawList.AddLine(new Vector2(x, min.Y), new Vector2(x, max.Y), gridColor);
        }

        float plotMaxValue = maxValue > 0.0f ? maxValue : 1.0f;
        DrawProfileSeries(drawList, min, plotSize, count, plotMaxValue, maxTime, p => p.step, stepColor);
        DrawProfileSeries(drawList, min, plotSize, count, plotMaxValue, maxTime, p => p.collide, collideColor);
        DrawProfileSeries(drawList, min, plotSize, count, plotMaxValue, maxTime, p => p.solve, solveColor);

        drawList.AddText(new Vector2(canvasPos.X + 4.0f, min.Y), textColor, "ms");
        drawList.AddText(new Vector2(canvasPos.X + 4.0f, min.Y + ImGui.GetFontSize()), textColor, FormatFloat(maxValue));
        drawList.AddText(new Vector2(min.X, max.Y + 4.0f), textColor, "0");
        drawList.AddText(new Vector2(max.X - 36.0f, max.Y + 4.0f), textColor, FormatFloat(maxTime));
        drawList.AddText(new Vector2(max.X + 2.0f, max.Y + 4.0f), textColor, "t");

        Vector2 legend = min + new Vector2(8.0f, 6.0f);
        legend = DrawProfileLegendItem(drawList, legend, "step", stepColor, textColor);
        legend = DrawProfileLegendItem(drawList, legend, "collide", collideColor, textColor);
        DrawProfileLegendItem(drawList, legend, "solve", solveColor, textColor);
    }

    private static Vector2 DrawProfileLegendItem(ImDrawListPtr drawList, Vector2 position, string name, Vector4 color, uint textColor)
    {
        uint lineColor = ImGui.ColorConvertFloat4ToU32(color);
        float centerY = position.Y + 0.5f * ImGui.GetFontSize();
        drawList.AddLine(new Vector2(position.X, centerY), new Vector2(position.X + 14.0f, centerY), lineColor, 2.0f);
        position.X += 18.0f;
        drawList.AddText(position, textColor, name);
        position.X += ImGui.CalcTextSize(name).X + 12.0f;
        return position;
    }

    private void DrawProfileSeries(ImDrawListPtr drawList, Vector2 origin, Vector2 size, int count, float maxValue, float maxTime,
        Func<B2Profile, float> selector, Vector4 color)
    {
        if (count < 2)
        {
            return;
        }

        uint lineColor = ImGui.ColorConvertFloat4ToU32(color);
        Vector2 previous = default;

        for (int i = 0; i < count; ++i)
        {
            int index = (int)((m_profileReadIndex + (ulong)i) & (m_profileCapacity - 1));
            float value = selector(m_profiles[index]);
            float x = origin.X + size.X * m_frameTimes[i] / maxTime;
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
        m_hudLineCount = 0;
        float fontSize = ImGui.GetFontSize();
        if (m_context.showUI)
        {
            m_screenTextY = ImGui.GetFrameHeight() + 1.5f * fontSize;
        }
        else
        {
            m_screenTextY = 3.0f * fontSize;
        }
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
        if (m_context.showUI == false || m_hudLineCount >= m_maxHudLines)
        {
            return;
        }

        m_hudLines[m_hudLineCount].color = color;
        m_hudLines[m_hudLineCount].text = TruncateText(text);
        m_hudLineCount += 1;
    }


    public void DrawTextLine(string text)
    {
        if (m_context.showUI == false || m_hudLineCount >= m_maxHudLines)
        {
            return;
        }

        m_hudLines[m_hudLineCount].color = B2HexColor.b2_colorWhite;
        m_hudLines[m_hudLineCount].text = TruncateText(text);
        m_hudLineCount += 1;
    }

    public void DrawScreenTextLine(string text)
    {
        DrawScreenString(m_draw, 5.0f, m_screenTextY, B2HexColor.b2_colorWhite, TruncateText(text));
        m_screenTextY += 1.5f * ImGui.GetFontSize();
    }

    public void DrawColoredScreenTextLine(B2HexColor color, string text)
    {
        DrawScreenString(m_draw, 5.0f, m_screenTextY, color, TruncateText(text));
        m_screenTextY += 1.5f * ImGui.GetFontSize();
    }

    private static string TruncateText(string text)
    {
        return text.Length > 255 ? text[..255] : text;
    }

    public void ResetProfile()
    {
        m_stepCount = 0;
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

            if (m_context.showUI)
            {
                DrawTextLine("****PAUSED****");
                DrawTextLine("");
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

        for (int i = 0; i < 1; ++i)
        {
            b2World_Step(m_worldId, timeStep, m_context.subStepCount);
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

    }

    public virtual void Draw()
    {
        m_context.debugDraw.drawingBounds = GetViewBounds(m_context.camera);

        b2World_Draw(m_worldId, m_context.debugDraw);

    }

    public virtual void BuildSamplePanel()
    {
    }

    public void ShiftOrigin(B2Vec2 newOrigin)
    {
        // m_world.ShiftOrigin(newOrigin);
    }


    protected InputAction GetKey(Keys key)
    {
        return GlfwHelpers.GetKey(m_context, key);
    }

    public static void SelectSample(SampleContext context, int selection, bool restart)
    {
        if (restart == false)
        {
            ResetView(context.camera);
            context.sampleIndex = selection;
            context.subStepCount = 4;
            context.debugDraw.drawJoints = true;
        }

        context.sample?.Dispose();
        context.sample = null;
        context.restart = restart;
        context.sample = SampleFactory.Shared.Create(context.sampleIndex, context);
        context.restart = false;
    }

    // Case-insensitive subsequence match. Returns >=0 score on match, -1 on no match.
    // Empty needle returns 0 so an empty query lets all samples through with a neutral score.
    private static int FuzzyScore(string needle, string haystack)
    {
        if (string.IsNullOrEmpty(needle))
        {
            return 0;
        }

        int score = 0;
        int hi = 0;
        int previousMatchHi = -2;

        for (int ni = 0; ni < needle.Length; ++ni)
        {
            char nc = char.ToLowerInvariant(needle[ni]);
            while (hi < haystack.Length && char.ToLowerInvariant(haystack[hi]) != nc)
            {
                ++hi;
            }
            if (hi == haystack.Length)
            {
                return -1;
            }

            int bonus = 1;
            if (hi == 0)
            {
                bonus += 10; // prefix match
            }
            else if (char.IsLetterOrDigit(haystack[hi - 1]) == false)
            {
                bonus += 5; // word-start (after _, space, etc.)
            }
            if (hi == previousMatchHi + 1)
            {
                bonus += 3; // contiguous run
            }

            score += bonus;
            previousMatchHi = hi;
            ++hi;
        }

        return score;
    }

    private static void RebuildSampleFilter(string query)
    {
        int sampleCount = Math.Min(SampleFactory.Shared.SampleCount, MAX_SAMPLES);
        int count = 0;
        for (int i = 0; i < sampleCount; ++i)
        {
            int nameScore = FuzzyScore(query, SampleFactory.Shared.GetName(i));
            int categoryScore = FuzzyScore(query, SampleFactory.Shared.GetCategory(i));
            int best = -1;
            if (nameScore >= 0)
            {
                best = nameScore * 2; // name matches outweigh category-only matches
            }
            if (categoryScore >= 0 && categoryScore > best)
            {
                best = categoryScore;
            }
            if (best < 0)
            {
                continue;
            }
            s_scoredSamples[count].index = i;
            s_scoredSamples[count].score = best;
            ++count;
        }

        // Stable insertion sort by score desc; equal scores keep registry order
        // (which main.cpp sorts by category then name).
        for (int i = 1; i < count; ++i)
        {
            ScoredSample temporary = s_scoredSamples[i];
            int j = i - 1;
            while (j >= 0 && s_scoredSamples[j].score < temporary.score)
            {
                s_scoredSamples[j + 1] = s_scoredSamples[j];
                --j;
            }
            s_scoredSamples[j + 1] = temporary;
        }
        for (int i = 0; i < count; ++i)
        {
            s_filteredSamples[i] = s_scoredSamples[i].index;
        }
        s_filteredCount = count;
    }

    private static void AddHelpRow(string key, string description)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextUnformatted(key);
        ImGui.TableSetColumnIndex(1);
        ImGui.TextUnformatted(description);
    }

    private static void TextLinkOpenURL(string label, string url)
    {
        // ImGui.NET 1.90 does not expose TextLinkOpenURL, so preserve the same appearance and click behavior here.
        Vector4 color = new Vector4(0.26f, 0.59f, 0.98f, 1.0f);
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(label);
        ImGui.PopStyleColor();
        if (ImGui.IsItemHovered())
        {
            Vector2 min = ImGui.GetItemRectMin();
            Vector2 max = ImGui.GetItemRectMax();
            uint underlineColor = ImGui.ColorConvertFloat4ToU32(color);
            ImGui.GetWindowDrawList().AddLine(new Vector2(min.X, max.Y), max, underlineColor);
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }

        if (ImGui.IsItemClicked())
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }

    public static void UpdateSampleUI(SampleContext context)
    {
        int maxWorkers = B2_MAX_WORKERS;

        float fontSize = ImGui.GetFontSize();
        float menuWidth = 14.0f * fontSize;

        if (context.showUI == false)
        {
            return;
        }

        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("Sim"))
            {
                ImGui.MenuItem("Pause", "P", ref context.pause);
                if (ImGui.MenuItem("Single Step", "O"))
                {
                    context.singleStep = true;
                }
                if (ImGui.MenuItem("Restart", "R"))
                {
                    SelectSample(context, context.sampleIndex, true);
                }
                ImGui.Separator();
                if (ImGui.MenuItem("Previous Sample", "["))
                {
                    int selection = context.sampleIndex - 1;
                    if (selection < 0)
                    {
                        selection = SampleFactory.Shared.SampleCount - 1;
                    }
                    SelectSample(context, selection, false);
                }
                if (ImGui.MenuItem("Next Sample", "]"))
                {
                    int selection = context.sampleIndex + 1;
                    if (selection == SampleFactory.Shared.SampleCount)
                    {
                        selection = 0;
                    }
                    SelectSample(context, selection, false);
                }
                ImGui.Separator();
                if (ImGui.MenuItem("Reset Profile"))
                {
                    context.sample.ResetProfile();
                }
                if (ImGui.MenuItem("Dump Mem Stats"))
                {
                    b2World_DumpMemoryStats(context.sample.m_worldId);
                }
                ImGui.Separator();
                if (ImGui.MenuItem("Quit", "Esc"))
                {
                    unsafe
                    {
                        context.glfw.SetWindowShouldClose(context.window, true);
                    }
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("View"))
            {
                if (ImGui.MenuItem("Hide UI", "Tab"))
                {
                    context.showUI = false;
                }
                if (ImGui.MenuItem("Reset Camera", "Home"))
                {
                    ResetView(context.camera);
                }
                ImGui.Separator();
                ImGui.MenuItem("Shapes", null, ref context.debugDraw.drawShapes);
                ImGui.MenuItem("Chain Normals", null, ref context.debugDraw.drawChainNormals);
                ImGui.MenuItem("Joints", null, ref context.debugDraw.drawJoints);
                ImGui.MenuItem("Joint Extras", null, ref context.debugDraw.drawJointExtras);
                ImGui.MenuItem("Bounds", null, ref context.debugDraw.drawBounds);
                ImGui.MenuItem("Mass", null, ref context.debugDraw.drawMass);
                ImGui.MenuItem("Body Names", null, ref context.debugDraw.drawBodyNames);
                ImGui.MenuItem("Graph Colors", null, ref context.debugDraw.drawGraphColors);
                ImGui.MenuItem("Islands", null, ref context.debugDraw.drawIslands);
                ImGui.Separator();
                ImGui.MenuItem("Contact Points", null, ref context.debugDraw.drawContacts);
                ImGui.MenuItem("Contact Normals", null, ref context.debugDraw.drawContactNormals);
                ImGui.MenuItem("Contact Features", null, ref context.debugDraw.drawContactFeatures);
                ImGui.MenuItem("Contact Forces", null, ref context.debugDraw.drawContactForces);
                ImGui.MenuItem("Friction Forces", null, ref context.debugDraw.drawFrictionForces);
                if (ImGui.BeginMenu("Anchor"))
                {
                    if (ImGui.MenuItem("Anchor A", null, context.debugDraw.drawAnchorA))
                    {
                        context.debugDraw.drawAnchorA = true;
                    }
                    if (ImGui.MenuItem("Anchor B", null, context.debugDraw.drawAnchorA == false))
                    {
                        context.debugDraw.drawAnchorA = false;
                    }
                    ImGui.EndMenu();
                }
                ImGui.Separator();
                ImGui.MenuItem("Diagnostics", "M", ref context.showDiagnostics);
                ImGui.Separator();
                if (ImGui.BeginMenu("Scale"))
                {
                    ImGui.PushItemWidth(6.0f * fontSize);
                    ImGui.InputFloat("Joint", ref context.debugDraw.jointScale);
                    ImGui.InputFloat("Force", ref context.debugDraw.forceScale);
                    ImGui.PopItemWidth();
                    ImGui.EndMenu();
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Samples"))
            {
                int i = 0;
                while (i < SampleFactory.Shared.SampleCount)
                {
                    string category = SampleFactory.Shared.GetCategory(i);
                    if (ImGui.BeginMenu(category))
                    {
                        while (i < SampleFactory.Shared.SampleCount && category == SampleFactory.Shared.GetCategory(i))
                        {
                            bool selected = i == context.sampleIndex;
                            if (ImGui.MenuItem(SampleFactory.Shared.GetName(i), null, selected))
                            {
                                SelectSample(context, i, false);
                            }
                            ++i;
                        }
                        ImGui.EndMenu();
                    }
                    else
                    {
                        while (i < SampleFactory.Shared.SampleCount && category == SampleFactory.Shared.GetCategory(i))
                        {
                            ++i;
                        }
                    }
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Help"))
            {
                ImGui.MenuItem("Controls", null, ref s_showHelp);
                ImGui.MenuItem("About", null, ref s_showAbout);
                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();

            {
                float menuBarBottom = ImGui.GetFrameHeight();
                uint borderColor = ImGui.GetColorU32(ImGuiCol.Border);
                Vector2 displaySize = ImGui.GetIO().DisplaySize;
                ImGui.GetForegroundDrawList().AddLine(new Vector2(0.0f, menuBarBottom),
                    new Vector2(displaySize.X, menuBarBottom), borderColor, 1.0f);
            }

            if (s_showHelp)
            {
                ImGui.SetNextWindowPos(new Vector2(context.camera.width * 0.5f, context.camera.height * 0.5f),
                    ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
                ImGui.SetNextWindowSize(new Vector2(24.0f * fontSize, 0.0f), ImGuiCond.Appearing);

                if (ImGui.Begin("Controls", ref s_showHelp,
                        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.SeparatorText("Keyboard");
                    if (ImGui.BeginTable("keys", 2, ImGuiTableFlags.SizingFixedFit))
                    {
                        AddHelpRow("Tab", "Show / hide UI");
                        AddHelpRow("M", "Show / hide diagnostics");
                        AddHelpRow("P", "Pause / resume");
                        AddHelpRow("O", "Single step");
                        AddHelpRow("R", "Restart sample");
                        AddHelpRow("[  ]", "Previous / next sample");
                        AddHelpRow("Ctrl+O", "Open sample picker");
                        AddHelpRow("Arrows", "Pan camera");
                        AddHelpRow("Ctrl+Arrows", "Shift origin");
                        AddHelpRow("Z  X", "Zoom out / in");
                        AddHelpRow("Home", "Reset camera");
                        AddHelpRow("Esc", "Quit");
                        ImGui.EndTable();
                    }

                    ImGui.SeparatorText("Mouse");
                    if (ImGui.BeginTable("mouse", 2, ImGuiTableFlags.SizingFixedFit))
                    {
                        AddHelpRow("Left drag", "Move bodies (mouse joint)");
                        AddHelpRow("Right drag", "Pan camera");
                        AddHelpRow("Scroll wheel", "Zoom");
                        ImGui.EndTable();
                    }
                }
                ImGui.End();
            }

            if (s_showAbout)
            {
                ImGui.SetNextWindowPos(new Vector2(context.camera.width * 0.5f, context.camera.height * 0.5f),
                    ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
                ImGui.SetNextWindowSize(new Vector2(22.0f * fontSize, 0.0f), ImGuiCond.Appearing);

                if (ImGui.Begin("About", ref s_showAbout,
                        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize))
                {
                    B2Version version = b2GetVersion();
                    ImGui.Text($"Box2D {version.major}.{version.minor}.{version.revision}");
                    ImGui.Spacing();
                    TextLinkOpenURL("box2d.org", "https://box2d.org/");
                    TextLinkOpenURL("github.com/ikpil/Box2D.NET", "https://github.com/ikpil/Box2D.NET");
                }
                ImGui.End();
            }
        }

        // Fuzzy sample picker (Ctrl+O). Opens a transient popup; type to filter by
        // name or category, Up/Down to navigate, Enter to select, Esc / click-outside to dismiss.
        {
            if (context.openSamplePicker)
            {
                ImGui.OpenPopup("##sample_picker");
                context.openSamplePicker = false;
                s_pickerQuery = string.Empty;
                s_previousPickerQuery = string.Empty;
                s_pickerHighlight = 0;
                s_previousPickerHighlight = 0;
                RebuildSampleFilter(s_pickerQuery);
                s_pickerJustOpened = true;
                s_forcePickerScroll = true;
            }

            ImGui.SetNextWindowPos(new Vector2(context.camera.width * 0.5f, context.camera.height * 0.35f),
                ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSize(new Vector2(32.0f * fontSize, 0.0f), ImGuiCond.Appearing);

            if (ImGui.BeginPopup("##sample_picker",
                    ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings))
            {
                if (s_pickerJustOpened)
                {
                    ImGui.SetKeyboardFocusHere();
                    s_pickerJustOpened = false;
                }

                ImGui.PushItemWidth(-1.0f);
                ImGui.InputTextWithHint("##q", "Search by name or category...", ref s_pickerQuery, 64);
                ImGui.PopItemWidth();

                if (s_pickerQuery != s_previousPickerQuery)
                {
                    RebuildSampleFilter(s_pickerQuery);
                    s_previousPickerQuery = s_pickerQuery;
                    s_pickerHighlight = 0;
                    s_forcePickerScroll = true;
                }

                if (s_filteredCount > 0)
                {
                    if (ImGui.IsKeyPressed(ImGuiKey.DownArrow, true))
                    {
                        s_pickerHighlight = (s_pickerHighlight + 1) % s_filteredCount;
                    }
                    if (ImGui.IsKeyPressed(ImGuiKey.UpArrow, true))
                    {
                        s_pickerHighlight = (s_pickerHighlight + s_filteredCount - 1) % s_filteredCount;
                    }
                }
                bool commit = ImGui.IsKeyPressed(ImGuiKey.Enter, false) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter, false);

                ImGui.BeginChild("##results", new Vector2(0.0f, 14.0f * fontSize), ImGuiChildFlags.Border);
                for (int row = 0; row < s_filteredCount; ++row)
                {
                    int i = s_filteredSamples[row];
                    string label = $"{SampleFactory.Shared.GetCategory(i)}  >  {SampleFactory.Shared.GetName(i)}";
                    bool selected = row == s_pickerHighlight;
                    if (ImGui.Selectable(label, selected))
                    {
                        s_pickerHighlight = row;
                        commit = true;
                    }
                    if (selected && (s_forcePickerScroll || s_pickerHighlight != s_previousPickerHighlight))
                    {
                        ImGui.SetScrollHereY();
                    }
                }
                ImGui.EndChild();
                s_previousPickerHighlight = s_pickerHighlight;
                s_forcePickerScroll = false;

                if (commit && s_filteredCount > 0)
                {
                    SelectSample(context, s_filteredSamples[s_pickerHighlight], false);
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }
        }

        float menuBarHeight = ImGui.GetFrameHeight();

        ImGui.SetNextWindowPos(new Vector2(context.camera.width - menuWidth - 0.5f * fontSize,
            menuBarHeight + 0.5f * fontSize));
        ImGui.SetNextWindowSize(new Vector2(menuWidth, context.camera.height - menuBarHeight - fontSize));

        ImGui.Begin("Info",
            ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoTitleBar);

        for (int i = 0; i < context.sample.m_hudLineCount; ++i)
        {
            HudLine line = context.sample.m_hudLines[i];
            if (string.IsNullOrEmpty(line.text))
            {
                ImGui.Separator();
                continue;
            }
            ImGui.PushStyleColor(ImGuiCol.Text, HexToColor(line.color));
            ImGui.TextUnformatted(line.text);
            ImGui.PopStyleColor();
        }

        if (context.sample.m_hudLineCount > 0)
        {
            ImGui.Separator();
        }

        context.sample.BuildSamplePanel();

        ImGui.Separator();

        if (ImGui.CollapsingHeader("Solver", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.PushItemWidth(6.0f * fontSize);
            ImGui.SliderInt("Sub-steps", ref context.subStepCount, 1, 32);
            ImGui.SliderFloat("Hertz", ref context.hertz, 5.0f, 240.0f, "%.0f hz");

            if (ImGui.SliderInt("Workers", ref context.workerCount, 1, maxWorkers))
            {
                context.workerCount = b2ClampInt(context.workerCount, 1, maxWorkers);
                SelectSample(context, context.sampleIndex, true);
            }

            float recyclingCentimeters = 100.0f * context.recycleDistance;
            if (ImGui.SliderFloat("Recycle", ref recyclingCentimeters, 0.0f, 10.0f, "%.1f cm"))
            {
                context.recycleDistance = 0.01f * recyclingCentimeters;
                b2World_SetContactRecycleDistance(context.sample.m_worldId, context.recycleDistance);
            }
            ImGui.PopItemWidth();

            ImGui.Checkbox("Sleep", ref context.enableSleep);
            ImGui.Checkbox("Warm Starting", ref context.enableWarmStarting);
            ImGui.Checkbox("Continuous", ref context.enableContinuous);
        }

        ImGui.End();

        context.sample.UpdateGui();
    }


}
