// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using ImGuiNET;
using Silk.NET.GLFW;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Cores;
using static Box2D.NET.B2DistanceJoints;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2PrismaticJoints;
using static Box2D.NET.B2Recordings;
using static Box2D.NET.B2RevoluteJoints;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.Samples.Graphics.Cameras;
using static Box2D.NET.Samples.Graphics.Draws;

namespace Box2D.NET.Samples.Samples.Replays;

// Plays back a recording by re-running the engine one step at a time and drawing the
// replayed world. Stepping is driven by the recorded inputs, not by b2World_Step, so the motion
// reproduces the original session exactly. Pause, single step, and restart use the shared sample
// controls. Mouse picking is disabled because dragging a body would mutate the replayed world
// and diverge it from the recording.
public class ReplayFile : Sample
{
    private static readonly int SampleReplayFile = SampleFactory.Shared.RegisterSample("Replay", "Replay File", Create);

    private enum SelKind
    {
        SelNone,
        SelBody,
        SelShape,
        SelJoint,
        SelQuery,
    }

    private B2RecPlayer m_player;
    private string m_path = "recording.b2rec";
    private string m_status = string.Empty;
    private uint m_recHash;
    private uint m_runHash;
    private bool m_buildMismatch;

    private B2RecPlayerInfo m_info;
    private float m_speed = 1.0f;
    private float m_frameAccum;
    private int m_replayWorkers;
    private bool m_loop;
    private bool m_selectTimelineTab = true;
    private readonly bool m_prevShowMetrics;

    // Inspector selection, keyed by stable creation ordinals so it survives a backward scrub. Resolved
    // to live ids each frame from the player's body tracking; out of range means "not at this frame".
    private SelKind m_selKind = SelKind.SelNone;
    private int m_selBodyOrdinal = -1; // index into the player's tracked body list
    private int m_selSlot = -1;        // shape or joint slot within that body
    private int m_selQuery = -1;       // query index, only meaningful for the current frame
    private bool m_revealSelection;    // one-shot request to expand and scroll the tree to a viewport pick

    private static Sample Create(SampleContext context)
    {
        return new ReplayFile(context);
    }

    // The player owns the world we draw, so skip the base world. m_worldId stays null until
    // OpenPlayer adopts the player's world.
    public ReplayFile(SampleContext context) : base(context, false)
    {
        if (m_context.restart == false)
        {
            m_context.camera.center = new B2Vec2(0.0f, 7.5f);
            m_context.camera.zoom = 10.0f;
        }

        // The timeline scrubber lives in the diagnostics drawer, so open it for the replay
        m_prevShowMetrics = m_context.showMetrics;
        m_context.showMetrics = true;
        m_selectTimelineTab = true;

        OpenPlayer();
    }

    public override void Dispose()
    {
        ClosePlayer();

        m_context.showMetrics = m_prevShowMetrics;
        m_worldId = b2_nullWorldId;
        base.Dispose();
    }

    private void ClosePlayer()
    {
        if (m_player != null)
        {
            b2RecPlayer_Destroy(m_player);
            m_player = null;
        }
        m_worldId = b2_nullWorldId;
        m_buildMismatch = false;
        m_selKind = SelKind.SelNone;
        m_selBodyOrdinal = -1;
        m_selSlot = -1;
        m_selQuery = -1;
    }

    private void OpenPlayer()
    {
        ClosePlayer();

        // Replay workers of 0 uses the recorded count, otherwise force a different count
        // to spot-check cross-thread determinism.
        m_player = b2RecPlayer_Create(m_path, m_replayWorkers);
        m_frameAccum = 0.0f;
        if (m_player != null)
        {
            m_worldId = b2RecPlayer_GetWorldId(m_player);
            m_info = b2RecPlayer_GetInfo(m_player);

            // Flag a file made by a different engine build. 0 on either side is unstamped.
            m_recHash = b2RecPlayer_GetBuildHash(m_player);
            m_runHash = b2GetBuildHash();
            m_buildMismatch = m_recHash != 0 && m_runHash != 0 && m_recHash != m_runHash;
            m_status = $"loaded (build {m_recHash:x8})";
        }
        else
        {
            m_info = new B2RecPlayerInfo();
            m_status = "failed to open file";
        }
    }

    // Advance one recorded step and keep the world pointer current
    private void AdvanceOne()
    {
        b2RecPlayer_StepFrame(m_player);
        m_worldId = b2RecPlayer_GetWorldId(m_player);
    }

    public override void Step()
    {
        if (m_player == null)
        {
            DrawScreenTextLine(m_status);
            return;
        }

        if (m_context.pause && m_context.singleStep)
        {
            m_context.singleStep = false;
            if (b2RecPlayer_IsAtEnd(m_player) == false)
            {
                AdvanceOne();
            }
            m_frameAccum = 0.0f;
        }
        else if (m_context.pause == false)
        {
            // Speed scales how many recorded steps pass per display frame. Below 1 advances
            // only every few frames, above 1 advances several.
            m_frameAccum += m_speed;
            while (m_frameAccum >= 1.0f)
            {
                m_frameAccum -= 1.0f;
                if (b2RecPlayer_IsAtEnd(m_player))
                {
                    if (m_loop)
                    {
                        b2RecPlayer_Restart(m_player);
                        m_worldId = b2RecPlayer_GetWorldId(m_player);
                    }
                    else
                    {
                        m_frameAccum = 0.0f;
                        break;
                    }
                }
                AdvanceOne();
            }
        }

        // Keep the base panel "step N" line tracking the replay frame
        m_stepCount = b2RecPlayer_GetFrame(m_player);

        DrawScreenTextLine($"frame {b2RecPlayer_GetFrame(m_player)} / {m_info.frameCount}" +
                           (b2RecPlayer_IsAtEnd(m_player) ? "  (end)" : string.Empty));

        if (b2RecPlayer_HasDiverged(m_player))
        {
            DrawScreenTextLine("****DIVERGED****");
        }

        if (m_buildMismatch)
        {
            DrawScreenTextLine($"build mismatch: file {m_recHash:x8}, engine {m_runHash:x8}");
        }

        if (m_context.pause)
        {
            DrawScreenTextLine("****PAUSED****");
        }
    }

    public override void Draw()
    {
        m_context.debugDraw.drawingBounds = GetViewBounds(m_context.camera);
        if (B2_IS_NON_NULL(m_worldId))
        {
            b2World_Draw(m_worldId, m_context.debugDraw);
            if (m_selKind == SelKind.SelQuery)
            {
                b2RecPlayer_DrawFrameQueries(m_player, m_context.debugDraw, m_selQuery);
            }
            DrawSelectionHighlight();
        }

        DrawInspectorPanel();
    }

    // Shared transport row used by both the right panel and the timeline tab
    private void DrawTransport()
    {
        if (m_player == null)
        {
            return;
        }

        int frame = b2RecPlayer_GetFrame(m_player);

        if (ImGui.Button("|<"))
        {
            b2RecPlayer_SeekFrame(m_player, 0);
            m_worldId = b2RecPlayer_GetWorldId(m_player);
            m_frameAccum = 0.0f;
        }
        ImGui.SameLine();
        if (ImGui.Button("<"))
        {
            b2RecPlayer_SeekFrame(m_player, frame - 1);
            m_worldId = b2RecPlayer_GetWorldId(m_player);
            m_frameAccum = 0.0f;
            m_context.pause = true;
        }
        ImGui.SameLine();
        if (ImGui.Button(m_context.pause ? "Play " : "Pause"))
        {
            m_context.pause = !m_context.pause;
        }
        ImGui.SameLine();
        if (ImGui.Button(">"))
        {
            b2RecPlayer_SeekFrame(m_player, frame + 1);
            m_worldId = b2RecPlayer_GetWorldId(m_player);
            m_frameAccum = 0.0f;
            m_context.pause = true;
        }
        ImGui.SameLine();
        if (ImGui.Button(">|"))
        {
            b2RecPlayer_SeekFrame(m_player, m_info.frameCount);
            m_worldId = b2RecPlayer_GetWorldId(m_player);
            m_frameAccum = 0.0f;
        }
    }

    // A replay re-runs recorded inputs, so the live solver sliders would do nothing
    public override bool HasSolverControls()
    {
        return false;
    }

    // The inspector lives in the wide left panel. This right-panel control just reopens the
    // diagnostics drawer and jumps to the timeline if it was closed.
    public override bool DrawControls()
    {
        if (ImGui.Button("Show Timeline"))
        {
            m_context.showMetrics = true;
            m_selectTimelineTab = true;
        }
        return false;
    }

    // Selection resolution. The selection is stored as creation ordinals so it survives a backward
    // scrub that rebuilds the world. Each frame the ordinal is mapped back to a live id, or to null
    // when that object does not exist at the current frame.
    private B2BodyId SelectedBody()
    {
        if (m_selBodyOrdinal < 0)
        {
            return b2_nullBodyId;
        }
        return b2RecPlayer_GetBodyId(m_player, m_selBodyOrdinal);
    }

    private B2ShapeId SelectedShape()
    {
        B2BodyId body = SelectedBody();
        if (m_selKind != SelKind.SelShape || b2Body_IsValid(body) == false)
        {
            return b2_nullShapeId;
        }
        B2ShapeId[] shapes = new B2ShapeId[32];
        int count = b2Body_GetShapes(body, shapes, 32);
        return m_selSlot >= 0 && m_selSlot < count ? shapes[m_selSlot] : b2_nullShapeId;
    }

    private B2JointId SelectedJoint()
    {
        B2BodyId body = SelectedBody();
        if (m_selKind != SelKind.SelJoint || b2Body_IsValid(body) == false)
        {
            return b2_nullJointId;
        }
        B2JointId[] joints = new B2JointId[16];
        int count = b2Body_GetJoints(body, joints, 16);
        return m_selSlot >= 0 && m_selSlot < count ? joints[m_selSlot] : b2_nullJointId;
    }

    private int FindBodyOrdinal(B2BodyId body)
    {
        int count = b2RecPlayer_GetBodyCount(m_player);
        for (int i = 0; i < count; ++i)
        {
            if (B2_ID_EQUALS(b2RecPlayer_GetBodyId(m_player, i), body))
            {
                return i;
            }
        }
        return -1;
    }

    // Map a picked shape back to its body ordinal and shape slot. A null shape clears the selection.
    private void SelectShape(B2ShapeId shape)
    {
        if (B2_IS_NULL(shape))
        {
            m_selKind = SelKind.SelNone;
            return;
        }
        B2BodyId body = b2Shape_GetBody(shape);
        int ordinal = FindBodyOrdinal(body);
        if (ordinal < 0)
        {
            m_selKind = SelKind.SelNone;
            return;
        }
        B2ShapeId[] shapes = new B2ShapeId[32];
        int count = b2Body_GetShapes(body, shapes, 32);
        int slot = -1;
        for (int i = 0; i < count; ++i)
        {
            if (B2_ID_EQUALS(shapes[i], shape))
            {
                slot = i;
                break;
            }
        }
        m_selKind = SelKind.SelShape;
        m_selBodyOrdinal = ordinal;
        m_selSlot = slot;
        m_revealSelection = true; // expand and scroll the tree to the picked shape next draw
    }

    // Draw a body's live contact points and normals, the most useful solver readout
    private void DrawBodyContacts(B2BodyId body)
    {
        B2ContactData[] contacts = new B2ContactData[64];
        int capacity = b2Body_GetContactCapacity(body);
        if (capacity > 64)
        {
            capacity = 64;
        }
        int count = b2Body_GetContactData(body, contacts, capacity);
        for (int i = 0; i < count; ++i)
        {
            B2Vec2 originA = b2Body_GetPosition(b2Shape_GetBody(contacts[i].shapeIdA));
            B2Manifold manifold = contacts[i].manifold;
            for (int j = 0; j < manifold.pointCount; ++j)
            {
                B2Vec2 point = b2Add(originA, manifold.points[j].anchorA);
                DrawPoint(m_draw, point, 6.0f, B2HexColor.b2_colorOrange);
                DrawLine(m_draw, point, b2MulAdd(point, 0.3f, manifold.normal), B2HexColor.b2_colorOrange);
            }
        }
    }

    // Highlight the current selection without touching the world. Queries are already drawn by
    // b2RecPlayer_DrawFrameQueries, so they need nothing here.
    private void DrawSelectionHighlight()
    {
        if (m_selKind == SelKind.SelShape)
        {
            B2ShapeId shape = SelectedShape();
            if (b2Shape_IsValid(shape) == false)
            {
                return;
            }
            B2BodyId body = b2Shape_GetBody(shape);
            DrawBounds(m_draw, b2Shape_GetAABB(shape), B2HexColor.b2_colorYellow);
            DrawTransform(m_draw, b2Body_GetTransform(body), 0.5f);
            DrawPoint(m_draw, b2Body_GetWorldCenterOfMass(body), 8.0f, B2HexColor.b2_colorYellow);
            DrawBodyContacts(body);
        }
        else if (m_selKind == SelKind.SelBody)
        {
            B2BodyId body = SelectedBody();
            if (b2Body_IsValid(body) == false)
            {
                return;
            }
            DrawBounds(m_draw, b2Body_ComputeAABB(body), B2HexColor.b2_colorYellow);
            DrawTransform(m_draw, b2Body_GetTransform(body), 0.5f);
            DrawPoint(m_draw, b2Body_GetWorldCenterOfMass(body), 8.0f, B2HexColor.b2_colorYellow);
            DrawBodyContacts(body);
        }
        else if (m_selKind == SelKind.SelJoint)
        {
            B2JointId joint = SelectedJoint();
            if (b2Joint_IsValid(joint) == false)
            {
                return;
            }
            B2BodyId bodyA = b2Joint_GetBodyA(joint);
            B2BodyId bodyB = b2Joint_GetBodyB(joint);
            if (b2Body_IsValid(bodyA))
            {
                DrawPoint(m_draw, b2Body_GetWorldCenterOfMass(bodyA), 8.0f, B2HexColor.b2_colorMagenta);
            }
            if (b2Body_IsValid(bodyB))
            {
                DrawPoint(m_draw, b2Body_GetWorldCenterOfMass(bodyB), 8.0f, B2HexColor.b2_colorMagenta);
            }
        }
    }

    // Pick the first shape whose area contains the click point
    private static bool ReplayPickCallback(B2ShapeId shapeId, object context)
    {
        ReplayPickContext pick = (ReplayPickContext)context;
        if (b2Shape_TestPoint(shapeId, pick.point))
        {
            pick.shape = shapeId;
            return false;
        }
        return true;
    }

    // Left click selects a shape to inspect. Picking only reads the world, it never creates the drag
    // joint the base sample does, so the replay is not mutated. Dragging stays disabled.
    public override void MouseDown(B2Vec2 p, MouseButton button, KeyModifiers mod)
    {
        if (button != MouseButton.Left || B2_IS_NULL(m_worldId))
        {
            return;
        }

        B2Vec2 d = new B2Vec2(0.001f, 0.001f);
        B2AABB box = new B2AABB(b2Sub(p, d), b2Add(p, d));
        ReplayPickContext pick = new ReplayPickContext { point = p, shape = b2_nullShapeId };
        b2World_OverlapAABB(m_worldId, box, b2DefaultQueryFilter(), ReplayPickCallback, pick);

        // A miss clears the selection
        SelectShape(pick.shape);
    }

    public override void MouseUp(B2Vec2 p, MouseButton button)
    {
    }

    public override void MouseMove(B2Vec2 p)
    {
    }

    // Wide left panel: an outliner tree of the scene on top, the selected item's full detail below.
    // Its own window, so it is not bound by the fixed-width right Info panel. Opened from Step, which
    // runs inside the imgui frame.
    // The managed sample invokes this from Draw, the corresponding ImGui phase.
    private void DrawInspectorPanel()
    {
        if (m_player == null)
        {
            return;
        }

        float fontSize = ImGui.GetFontSize();
        float menuBarHeight = ImGui.GetFrameHeight();
        float drawerHeight = 16.0f * fontSize; // matches the diagnostics drawer in Sample.cs
        float top = menuBarHeight + 0.5f * fontSize;
        // Stop above the timeline drawer, which this sample keeps open
        float bottom = m_context.showMetrics ? m_context.camera.height - drawerHeight - fontSize
                                             : m_context.camera.height - 0.5f * fontSize;

        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, top));
        ImGui.SetNextWindowSize(new Vector2(22.0f * fontSize, bottom - top));
        ImGui.Begin("Inspector",
            ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoTitleBar);

        ImGui.TextColored(new Vector4(0.9f, 0.6f, 0.2f, 1.0f), "Outline");
        float available = ImGui.GetContentRegionAvail().Y;
        ImGui.BeginChild("tree", new Vector2(0.0f, 0.55f * available), ImGuiChildFlags.None);
        DrawOutlineTree();
        ImGui.EndChild();

        ImGui.Separator();
        ImGui.TextColored(new Vector4(0.9f, 0.6f, 0.2f, 1.0f), "Detail");
        ImGui.BeginChild("detail", Vector2.Zero, ImGuiChildFlags.None);
        DrawDetail();
        ImGui.EndChild();

        ImGui.End();
    }

    // The scene tree: bodies (creation order), each expandable to its shapes and joints, plus the
    // current frame's queries. Clicking a row selects it; clicking a body arrow expands it.
    private void DrawOutlineTree()
    {
        // A viewport pick asks the tree to reveal its target once: expand the owning body and scroll to
        // the row. Consumed at the end so it never fights the user's own expand/collapse.
        bool reveal = m_revealSelection;

        int count = b2RecPlayer_GetBodyCount(m_player);
        for (int ordinal = 0; ordinal < count; ++ordinal)
        {
            B2BodyId body = b2RecPlayer_GetBodyId(m_player, ordinal);
            if (B2_IS_NULL(body) || b2Body_IsValid(body) == false)
            {
                continue;
            }

            bool ownsSelection = m_selBodyOrdinal == ordinal &&
                                 (m_selKind == SelKind.SelBody || m_selKind == SelKind.SelShape || m_selKind == SelKind.SelJoint);

            string name = b2Body_GetName(body);
            string label = $"Body {ordinal}  {(string.IsNullOrEmpty(name) ? ReplayBodyTypeName(b2Body_GetType(body)) : name)}###b{ordinal}";

            ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;
            if (m_selKind == SelKind.SelBody && m_selBodyOrdinal == ordinal)
            {
                flags |= ImGuiTreeNodeFlags.Selected;
            }
            // Reveal a picked shape or joint by expanding its body
            if (reveal && ownsSelection && m_selKind != SelKind.SelBody)
            {
                ImGui.SetNextItemOpen(true);
            }
            bool open = ImGui.TreeNodeEx(label, flags);
            if (reveal && ownsSelection && m_selKind == SelKind.SelBody)
            {
                ImGui.SetScrollHereY(0.5f);
            }
            if (ImGui.IsItemClicked() && ImGui.IsItemToggledOpen() == false)
            {
                m_selKind = SelKind.SelBody;
                m_selBodyOrdinal = ordinal;
                m_selSlot = -1;
            }
            if (open == false)
            {
                continue;
            }

            B2ShapeId[] shapes = new B2ShapeId[32];
            int shapeCount = b2Body_GetShapes(body, shapes, 32);
            for (int shapeIndex = 0; shapeIndex < shapeCount; ++shapeIndex)
            {
                string shapeLabel = $"Shape {shapeIndex}  {ReplayShapeTypeName(b2Shape_GetType(shapes[shapeIndex]))}###b{ordinal}s{shapeIndex}";
                ImGuiTreeNodeFlags leafFlags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAvailWidth |
                                               ImGuiTreeNodeFlags.NoTreePushOnOpen;
                if (m_selKind == SelKind.SelShape && m_selBodyOrdinal == ordinal && m_selSlot == shapeIndex)
                {
                    leafFlags |= ImGuiTreeNodeFlags.Selected;
                }
                ImGui.TreeNodeEx(shapeLabel, leafFlags);
                if (reveal && m_selKind == SelKind.SelShape && m_selBodyOrdinal == ordinal && m_selSlot == shapeIndex)
                {
                    ImGui.SetScrollHereY(0.5f);
                }
                if (ImGui.IsItemClicked())
                {
                    m_selKind = SelKind.SelShape;
                    m_selBodyOrdinal = ordinal;
                    m_selSlot = shapeIndex;
                }
            }

            B2JointId[] joints = new B2JointId[16];
            int jointCount = b2Body_GetJoints(body, joints, 16);
            for (int jointIndex = 0; jointIndex < jointCount; ++jointIndex)
            {
                string jointLabel = $"{ReplayJointTypeName(b2Joint_GetType(joints[jointIndex]))} joint###b{ordinal}j{jointIndex}";
                ImGuiTreeNodeFlags leafFlags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAvailWidth |
                                               ImGuiTreeNodeFlags.NoTreePushOnOpen;
                if (m_selKind == SelKind.SelJoint && m_selBodyOrdinal == ordinal && m_selSlot == jointIndex)
                {
                    leafFlags |= ImGuiTreeNodeFlags.Selected;
                }
                ImGui.TreeNodeEx(jointLabel, leafFlags);
                if (ImGui.IsItemClicked())
                {
                    m_selKind = SelKind.SelJoint;
                    m_selBodyOrdinal = ordinal;
                    m_selSlot = jointIndex;
                }
            }

            ImGui.TreePop();
        }

        int queryCount = b2RecPlayer_GetFrameQueryCount(m_player);
        if (ImGui.TreeNodeEx($"Queries ({queryCount})###queries", ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            for (int i = 0; i < queryCount; ++i)
            {
                B2RecQueryInfo query = b2RecPlayer_GetFrameQuery(m_player, i);
                ImGuiTreeNodeFlags leafFlags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAvailWidth |
                                               ImGuiTreeNodeFlags.NoTreePushOnOpen;
                if (m_selKind == SelKind.SelQuery && m_selQuery == i)
                {
                    leafFlags |= ImGuiTreeNodeFlags.Selected;
                }
                ImGui.TreeNodeEx($"{ReplayQueryTypeName(query.type)}  ({query.hitCount})###q{i}", leafFlags);
                if (ImGui.IsItemClicked())
                {
                    m_selKind = SelKind.SelQuery;
                    m_selQuery = i;
                }
            }
            ImGui.TreePop();
        }

        m_revealSelection = false;
    }

    // Detail pane for the current selection. Full width, so 64-bit hex fits without clipping.
    private void DrawDetail()
    {
        if (m_selKind == SelKind.SelNone)
        {
            ImGui.TextWrapped("Click a node, or a shape in the view.");
            if (B2_IS_NON_NULL(m_worldId))
            {
                B2Vec2 gravity = b2World_GetGravity(m_worldId);
                B2Counters counters = b2World_GetCounters(m_worldId);
                ImGui.Text($"gravity ({gravity.X:F2}, {gravity.Y:F2})");
                ImGui.Text($"bodies {counters.bodyCount}  shapes {counters.shapeCount}");
                ImGui.Text($"contacts {counters.contactCount}  joints {counters.jointCount}");
            }
            return;
        }

        if (m_selKind == SelKind.SelQuery)
        {
            DrawQueryDetail();
            return;
        }

        B2BodyId body = SelectedBody();
        if (b2Body_IsValid(body) == false)
        {
            ImGui.TextDisabled("Not present at this frame.");
            return;
        }

        DrawBodyDetail(body);
        if (m_selKind == SelKind.SelShape)
        {
            B2ShapeId shape = SelectedShape();
            if (b2Shape_IsValid(shape))
            {
                DrawShapeDetail(shape);
            }
        }
        else if (m_selKind == SelKind.SelJoint)
        {
            B2JointId joint = SelectedJoint();
            if (b2Joint_IsValid(joint))
            {
                DrawJointDetail(joint);
            }
        }
        DrawContactDetail(body);
    }

    private void DrawBodyDetail(B2BodyId body)
    {
        if (ImGui.CollapsingHeader("Body", ImGuiTreeNodeFlags.DefaultOpen) == false)
        {
            return;
        }

        string name = b2Body_GetName(body);
        B2Transform transform = b2Body_GetTransform(body);
        B2Vec2 velocity = b2Body_GetLinearVelocity(body);

        ImGui.Text($"id      {body.index1}");
        ImGui.Text($"name    {(string.IsNullOrEmpty(name) ? "(none)" : name)}");
        ImGui.Text($"type    {ReplayBodyTypeName(b2Body_GetType(body))}");
        ImGui.Text($"pos     ({transform.p.X:F3}, {transform.p.Y:F3})");
        ImGui.Text($"angle   {b2Rot_GetAngle(transform.q) * 57.2957795f:F1} deg");
        ImGui.Text($"vel     ({velocity.X:F3}, {velocity.Y:F3})");
        ImGui.Text($"omega   {b2Body_GetAngularVelocity(body):F3} rad/s");
        ImGui.Text($"mass    {b2Body_GetMass(body):G4} kg");
        ImGui.Text($"inertia {b2Body_GetRotationalInertia(body):G4}");
        ImGui.Text($"awake   {(b2Body_IsAwake(body) ? "yes" : "no")}");
        ImGui.Text($"enabled {(b2Body_IsEnabled(body) ? "yes" : "no")}");
        ImGui.Text($"bullet  {(b2Body_IsBullet(body) ? "yes" : "no")}");
        ImGui.Text($"gravity scale {b2Body_GetGravityScale(body):F2}");
        ImGui.Text($"shapes {b2Body_GetShapeCount(body)}  joints {b2Body_GetJointCount(body)}");
    }

    private void DrawShapeDetail(B2ShapeId shape)
    {
        if (ImGui.CollapsingHeader("Shape", ImGuiTreeNodeFlags.DefaultOpen) == false)
        {
            return;
        }

        ImGui.Text($"type     {ReplayShapeTypeName(b2Shape_GetType(shape))}");
        B2Filter filter = b2Shape_GetFilter(shape);
        ImGui.Text($"category 0x{filter.categoryBits:x16}");
        ImGui.Text($"mask     0x{filter.maskBits:x16}");
        ImGui.Text($"group    {filter.groupIndex}");
        ImGui.Text($"density  {b2Shape_GetDensity(shape):G3}");
        ImGui.Text($"friction {b2Shape_GetFriction(shape):G3}");
        ImGui.Text($"restitution {b2Shape_GetRestitution(shape):G3}");
        ImGui.Text($"sensor   {(b2Shape_IsSensor(shape) ? "yes" : "no")}");
        B2SurfaceMaterial material = b2Shape_GetSurfaceMaterial(shape);
        ImGui.Text($"custom color 0x{(uint)material.customColor:x6}");
        B2AABB aabb = b2Shape_GetAABB(shape);
        ImGui.Text($"aabb ({aabb.lowerBound.X:F2}, {aabb.lowerBound.Y:F2})-({aabb.upperBound.X:F2}, {aabb.upperBound.Y:F2})");
    }

    private void DrawContactDetail(B2BodyId body)
    {
        B2ContactData[] contacts = new B2ContactData[64];
        int capacity = b2Body_GetContactCapacity(body);
        if (capacity > 64)
        {
            capacity = 64;
        }
        int count = b2Body_GetContactData(body, contacts, capacity);

        if (ImGui.CollapsingHeader($"Contacts ({count})###contacts") == false)
        {
            return;
        }

        for (int i = 0; i < count; ++i)
        {
            B2Manifold manifold = contacts[i].manifold;
            ImGui.Text($"shapes {contacts[i].shapeIdA.index1} / {contacts[i].shapeIdB.index1}   " +
                       $"normal ({manifold.normal.X:F2}, {manifold.normal.Y:F2})   points {manifold.pointCount}");
            for (int j = 0; j < manifold.pointCount; ++j)
            {
                B2ManifoldPoint point = manifold.points[j];
                ImGui.Text($"  sep {point.separation:F4}  Pn {point.normalImpulse:G3}  Pt {point.tangentImpulse:G3}");
            }
        }
    }

    private void DrawJointDetail(B2JointId joint)
    {
        if (ImGui.CollapsingHeader("Joint", ImGuiTreeNodeFlags.DefaultOpen) == false)
        {
            return;
        }

        B2JointType type = b2Joint_GetType(joint);
        ImGui.Text($"type     {ReplayJointTypeName(type)}");
        ImGui.Text($"body A   {b2Joint_GetBodyA(joint).index1}");
        ImGui.Text($"body B   {b2Joint_GetBodyB(joint).index1}");
        ImGui.Text($"collide  {(b2Joint_GetCollideConnected(joint) ? "yes" : "no")}");
        ImGui.Text($"force    {b2Length(b2Joint_GetConstraintForce(joint)):G3}");
        ImGui.Text($"torque   {b2Joint_GetConstraintTorque(joint):G3}");

        switch (type)
        {
            case B2JointType.b2_revoluteJoint:
                ImGui.Text($"angle    {b2RevoluteJoint_GetAngle(joint) * 57.2957795f:F1} deg");
                break;
            case B2JointType.b2_prismaticJoint:
                ImGui.Text($"translation {b2PrismaticJoint_GetTranslation(joint):F3}");
                break;
            case B2JointType.b2_distanceJoint:
                ImGui.Text($"length   {b2DistanceJoint_GetCurrentLength(joint):F3}");
                break;
        }
    }

    private void DrawQueryDetail()
    {
        int count = b2RecPlayer_GetFrameQueryCount(m_player);
        if (m_selQuery < 0 || m_selQuery >= count)
        {
            ImGui.TextDisabled("Query not present at this frame.");
            return;
        }

        B2RecQueryInfo query = b2RecPlayer_GetFrameQuery(m_player, m_selQuery);
        if (ImGui.CollapsingHeader("Query", ImGuiTreeNodeFlags.DefaultOpen) == false)
        {
            return;
        }

        ImGui.Text($"type     {ReplayQueryTypeName(query.type)}");
        bool shapeLocal = query.type == B2RecQueryType.b2_recQueryShapeTestPoint ||
                          query.type == B2RecQueryType.b2_recQueryShapeRayCast;
        if (shapeLocal == false)
        {
            ImGui.Text($"category 0x{query.filter.categoryBits:x16}");
            ImGui.Text($"mask     0x{query.filter.maskBits:x16}");
        }
        else
        {
            ImGui.Text($"shape    {query.shape.index1}");
        }
        ImGui.Text($"hits     {query.hitCount}");

        // Hits as one wrapped id list, so a 50-hit query stays compact
        if (query.hitCount > 0)
        {
            System.Text.StringBuilder line = new System.Text.StringBuilder(256);
            for (int hitIndex = 0; hitIndex < query.hitCount && line.Length < 244; ++hitIndex)
            {
                B2RecQueryHit hit = b2RecPlayer_GetFrameQueryHit(m_player, m_selQuery, hitIndex);
                line.Append(hit.shape.index1).Append(' ');
            }
            ImGui.TextWrapped($"hit shapes: {line}");
        }
    }

    // All replay controls live in the diagnostics drawer tab.
    public override void DrawMetricsTab()
    {
        ImGuiTabItemFlags tabFlags = ImGuiTabItemFlags.None;
        if (m_selectTimelineTab)
        {
            tabFlags |= ImGuiTabItemFlags.SetSelected;
            m_selectTimelineTab = false;
        }

        if (BeginTabItem("Timeline", tabFlags) == false)
        {
            return;
        }

        float fontSize = ImGui.GetFontSize();

        // File row, always available so a recording can be loaded even when none is open
        ImGui.PushItemWidth(18.0f * fontSize);
        ImGui.InputText("File", ref m_path, 256);
        ImGui.PopItemWidth();
        ImGui.SameLine();
        if (ImGui.Button("Load"))
        {
            OpenPlayer();
        }
        ImGui.SameLine();
        if (ImGui.Button("Restart") && m_player != null)
        {
            b2RecPlayer_Restart(m_player);
            m_worldId = b2RecPlayer_GetWorldId(m_player);
            m_frameAccum = 0.0f;
        }
        ImGui.SameLine();
        ImGui.TextUnformatted(m_status);

        if (m_player == null)
        {
            ImGui.EndTabItem();
            return;
        }

        // Transport row: buttons, speed, loop, replay worker count
        DrawTransport();
        ImGui.SameLine();

        string[] speedNames = { "0.25x", "0.5x", "1x", "2x", "4x" };
        float[] speedValues = { 0.25f, 0.5f, 1.0f, 2.0f, 4.0f };
        int speedIndex = 2;
        for (int i = 0; i < 5; ++i)
        {
            if (m_speed == speedValues[i])
            {
                speedIndex = i;
            }
        }
        ImGui.PushItemWidth(5.0f * fontSize);
        if (ImGui.Combo("Speed", ref speedIndex, speedNames, 5))
        {
            m_speed = speedValues[speedIndex];
        }
        ImGui.PopItemWidth();
        ImGui.SameLine();
        ImGui.Checkbox("Loop", ref m_loop);
        ImGui.SameLine();

        // Replaying at a different worker count is a visual cross-thread determinism check.
        // 0 means use the recorded count. Re-open on release so the player is not rebuilt mid-drag.
        ImGui.PushItemWidth(6.0f * fontSize);
        ImGui.SliderInt("Workers", ref m_replayWorkers, 0, B2Constants.B2_MAX_WORKERS);
        ImGui.PopItemWidth();
        bool reopen = ImGui.IsItemDeactivatedAfterEdit();
        ImGui.SameLine();
        ImGui.TextDisabled($"(rec {m_info.workerCount})");

        // Scrubber: full width, seeks both directions
        int scrub = b2RecPlayer_GetFrame(m_player);
        ImGui.PushItemWidth(-1.0f);
        if (ImGui.SliderInt("##frame", ref scrub, 0, m_info.frameCount))
        {
            b2RecPlayer_SeekFrame(m_player, scrub);
            m_worldId = b2RecPlayer_GetWorldId(m_player);
            m_frameAccum = 0.0f;
            m_context.pause = true;
        }
        ImGui.PopItemWidth();

        // Mark where the replay first diverged on the scrubber track
        int divergeFrame = b2RecPlayer_GetDivergeFrame(m_player);
        if (divergeFrame >= 0 && m_info.frameCount > 0)
        {
            Vector2 lower = ImGui.GetItemRectMin();
            Vector2 upper = ImGui.GetItemRectMax();
            float fraction = (float)divergeFrame / m_info.frameCount;
            float x = lower.X + fraction * (upper.X - lower.X);
            ImGui.GetWindowDrawList().AddLine(new Vector2(x, lower.Y), new Vector2(x, upper.Y),
                ImGui.ColorConvertFloat4ToU32(new Vector4(220.0f / 255.0f, 60.0f / 255.0f, 60.0f / 255.0f, 1.0f)), 2.0f);
        }

        // Info row: recording metadata, live counts, divergence
        ImGui.Text($"frames {m_info.frameCount}");
        if (m_info.timeStep > 0.0f)
        {
            ImGui.SameLine();
            ImGui.Text($"   {1.0f / m_info.timeStep:F0} hz, {m_info.subStepCount} sub-steps");
        }
        if (B2_IS_NON_NULL(m_worldId))
        {
            B2Counters counters = b2World_GetCounters(m_worldId);
            ImGui.SameLine();
            ImGui.Text($"   bodies {counters.bodyCount}  shapes {counters.shapeCount}  " +
                       $"contacts {counters.contactCount}  joints {counters.jointCount}");
        }
        if (divergeFrame >= 0)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.85f, 0.30f, 0.30f, 1.0f), $"   diverged at frame {divergeFrame}");
        }

        // Re-open last so the player is not torn down mid-draw
        if (reopen)
        {
            OpenPlayer();
        }

        ImGui.EndTabItem();
    }

    // ImGui.NET does not expose the flags overload without a writable open flag, so call the native
    // entry point directly to preserve the upstream nullptr (and avoid adding a close button).
    private static unsafe bool BeginTabItem(string label, ImGuiTabItemFlags flags)
    {
        int byteCount = System.Text.Encoding.UTF8.GetByteCount(label);
        Span<byte> utf8 = stackalloc byte[byteCount + 1];
        int written = System.Text.Encoding.UTF8.GetBytes(label.AsSpan(), utf8);
        utf8[written] = 0;
        fixed (byte* labelPtr = utf8)
        {
            return ImGuiNative.igBeginTabItem(labelPtr, null, flags) != 0;
        }
    }

    // Names for the inspector readouts
    private static string ReplayBodyTypeName(B2BodyType type)
    {
        return type switch
        {
            B2BodyType.b2_staticBody => "static",
            B2BodyType.b2_kinematicBody => "kinematic",
            B2BodyType.b2_dynamicBody => "dynamic",
            _ => "?",
        };
    }

    private static string ReplayShapeTypeName(B2ShapeType type)
    {
        return type switch
        {
            B2ShapeType.b2_circleShape => "circle",
            B2ShapeType.b2_capsuleShape => "capsule",
            B2ShapeType.b2_segmentShape => "segment",
            B2ShapeType.b2_polygonShape => "polygon",
            B2ShapeType.b2_chainSegmentShape => "chain segment",
            _ => "?",
        };
    }

    private static string ReplayJointTypeName(B2JointType type)
    {
        return type switch
        {
            B2JointType.b2_distanceJoint => "distance",
            B2JointType.b2_filterJoint => "filter",
            B2JointType.b2_motorJoint => "motor",
            B2JointType.b2_prismaticJoint => "prismatic",
            B2JointType.b2_revoluteJoint => "revolute",
            B2JointType.b2_weldJoint => "weld",
            B2JointType.b2_wheelJoint => "wheel",
            _ => "?",
        };
    }

    private static string ReplayQueryTypeName(B2RecQueryType type)
    {
        return type switch
        {
            B2RecQueryType.b2_recQueryOverlapAABB => "overlap AABB",
            B2RecQueryType.b2_recQueryOverlapShape => "overlap shape",
            B2RecQueryType.b2_recQueryCastRay => "cast ray",
            B2RecQueryType.b2_recQueryCastShape => "cast shape",
            B2RecQueryType.b2_recQueryCollideMover => "collide mover",
            B2RecQueryType.b2_recQueryCastRayClosest => "cast ray closest",
            B2RecQueryType.b2_recQueryCastMover => "cast mover",
            B2RecQueryType.b2_recQueryShapeTestPoint => "shape test point",
            B2RecQueryType.b2_recQueryShapeRayCast => "shape ray cast",
            _ => "?",
        };
    }
}
