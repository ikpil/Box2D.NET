// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Shared;
using ImGuiNET;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Recordings;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.Shared.Determinism;

namespace Box2D.NET.Samples.Samples.Replays;

// Produces a recording file so the Replay File sample has something to load. Runs a small scene
// with recording enabled at world creation.
public class MakeRecording : Sample
{
    private static readonly int SampleMakeRecording = SampleFactory.Shared.RegisterSample("Replay", "Make Recording", Create);

    private FallingHingeData m_data;
    private bool m_done;

    private const string m_path = "recording.b2rec";

    private static Sample Create(SampleContext context)
    {
        return new MakeRecording(context);
    }

    public MakeRecording(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_context.camera.center = new B2Vec2(0.0f, 7.5f);
            m_context.camera.zoom = 10.0f;
        }

        // Recreate the base world with recording enabled
        if (B2_IS_NON_NULL(m_worldId))
        {
            b2DestroyWorld(m_worldId);
            m_worldId = b2_nullWorldId;
        }

        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.workerCount = m_context.workerCount;
        worldDef.enableSleep = m_context.enableSleep;
        worldDef.recordingPath = m_path;
        m_worldId = b2CreateWorld(worldDef);

        m_data = CreateFallingHinges(m_worldId);
        m_done = false;
    }

    public override void Dispose()
    {
        DestroyFallingHinges(ref m_data);
        base.Dispose();
    }

    private static bool OverlapCounter(B2ShapeId shapeId, object context)
    {
        return true;
    }

    private static float AllHitsCast(B2ShapeId shapeId, B2Vec2 point, B2Vec2 normal, float fraction, ref CastContext context)
    {
        return 1.0f;
    }

    public override void Step()
    {
        base.Step();

        if (m_context.pause == false && m_done == false)
        {
            m_done = UpdateFallingHinges(m_worldId, ref m_data);

            // Issue a few queries each step so the Replay viewer has something to draw
            B2QueryFilter filter = b2DefaultQueryFilter();
            B2AABB scanBox = new B2AABB(new B2Vec2(5.0f, 1.0f), new B2Vec2(7.0f, 2.5f));
            b2World_OverlapAABB(m_worldId, scanBox, filter, OverlapCounter, null);

            B2Vec2 origin = new B2Vec2(0.0f, 12.0f);
            B2Vec2 translation = new B2Vec2(0.0f, -14.0f);
            b2World_CastRayClosest(m_worldId, origin, translation, filter);

            origin = new B2Vec2(-10.0f, 2.0f);
            translation = new B2Vec2(20.0f, 0.0f);
            b2World_CastRay(m_worldId, origin, translation, filter, AllHitsCast, new CastContext());

            if (m_done)
            {
                System.Console.WriteLine($"sleep step = {m_data.sleepStep}, hash = 0x{m_data.hash:X8}");

                b2World_StopRecording(m_worldId);
            }
        }
        else
        {
            DrawScreenTextLine($"sleep step = {m_data.sleepStep}, hash = 0x{m_data.hash:X8}");
        }
    }

    public override bool DrawControls()
    {
        ImGui.TextWrapped($"Recording to \"{m_path}\".");
        return true;
    }

    // Block mouse interaction
    public override void MouseDown(B2Vec2 p, Silk.NET.GLFW.MouseButton button, Silk.NET.GLFW.KeyModifiers mod)
    {
    }

    public override void MouseUp(B2Vec2 p, Silk.NET.GLFW.MouseButton button)
    {
    }

    public override void MouseMove(B2Vec2 p)
    {
    }
}
