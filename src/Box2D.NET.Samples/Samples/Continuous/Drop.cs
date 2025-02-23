using System.Collections.Generic;
using Box2D.NET.Primitives;
using Box2D.NET.Shared.Primitives;
using Silk.NET.GLFW;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.world;
using static Box2D.NET.Shared.human;

namespace Box2D.NET.Samples.Samples.Continuous;

public class Drop : Sample
{
    List<b2BodyId> m_groundIds;
    List<b2BodyId> m_bodyIds;
    Human m_human;
    int m_frameSkip;
    int m_frameCount;
    bool m_continuous;
    bool m_speculative;

    static int sampleDrop = RegisterSample("Continuous", "Drop", Create);

    static Sample Create(Settings settings)
    {
        return new Drop(settings);
    }

    public Drop(Settings settings)
        : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(0.0f, 1.5f);
            Draw.g_camera.m_zoom = 3.0f;
            settings.enableSleep = false;
            settings.drawJoints = false;
        }

#if FALSE
    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

        b2ShapeDef shapeDef = b2DefaultShapeDef();

        float w = 0.25f;
        int count = 40;

        float x = -0.5f * count * w;
        float h = 0.05f;
        for ( int j = 0; j <= count; ++j )
        {
            b2Polygon box = b2MakeOffsetBox( w, h, { x, -h }, b2Rot_identity );
            b2CreatePolygonShape( groundId, &shapeDef, &box );
            x += w;
        }
    }
#endif

        m_human = new Human();
        m_frameSkip = 0;
        m_frameCount = 0;
        m_continuous = true;
        m_speculative = true;

        Scene1();
    }

    void Clear()
    {
        for (int i = 0; i < m_bodyIds.Count; ++i)
        {
            b2DestroyBody(m_bodyIds[i]);
        }

        m_bodyIds.Clear();

        if (m_human.isSpawned)
        {
            DestroyHuman(m_human);
        }
    }

    void CreateGround1()
    {
        for (int i = 0; i < m_groundIds.Count; ++i)
        {
            b2DestroyBody(m_groundIds[i]);
        }

        m_groundIds.Clear();

        b2BodyDef bodyDef = b2DefaultBodyDef();
        b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

        b2ShapeDef shapeDef = b2DefaultShapeDef();

        float w = 0.25f;
        int count = 40;
        b2Segment segment = new b2Segment(new b2Vec2(-0.5f * count * w, 0.0f), new b2Vec2(0.5f * count * w, 0.0f));
        b2CreateSegmentShape(groundId, shapeDef, segment);

        m_groundIds.Add(groundId);
    }

    void CreateGround2()
    {
        for (int i = 0; i < m_groundIds.Count; ++i)
        {
            b2DestroyBody(m_groundIds[i]);
        }

        m_groundIds.Clear();

        b2BodyDef bodyDef = b2DefaultBodyDef();
        b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

        b2ShapeDef shapeDef = b2DefaultShapeDef();

        float w = 0.25f;
        int count = 40;

        float x = -0.5f * count * w;
        float h = 0.05f;
        for (int j = 0; j <= count; ++j)
        {
            b2Polygon box = b2MakeOffsetBox(0.5f * w, h, new b2Vec2(x, 0.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, shapeDef, box);
            x += w;
        }

        m_groundIds.Add(groundId);
    }

    void CreateGround3()
    {
        for (int i = 0; i < m_groundIds.Count; ++i)
        {
            b2DestroyBody(m_groundIds[i]);
        }

        m_groundIds.Clear();

        b2BodyDef bodyDef = b2DefaultBodyDef();
        b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

        b2ShapeDef shapeDef = b2DefaultShapeDef();

        float w = 0.25f;
        int count = 40;
        b2Segment segment = new b2Segment(new b2Vec2(-0.5f * count * w, 0.0f), new b2Vec2(0.5f * count * w, 0.0f));
        b2CreateSegmentShape(groundId, shapeDef, segment);
        segment = new b2Segment(new b2Vec2(3.0f, 0.0f), new b2Vec2(3.0f, 8.0f));
        b2CreateSegmentShape(groundId, shapeDef, segment);

        m_groundIds.Add(groundId);
    }

// ball
    void Scene1()
    {
        Clear();
        CreateGround2();

        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = new b2Vec2(0.0f, 4.0f);
        bodyDef.linearVelocity = new b2Vec2(0.0f, -100.0f);

        b2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        b2Circle circle = new b2Circle(new b2Vec2(0.0f, 0.0f), 0.125f);
        b2CreateCircleShape(bodyId, shapeDef, circle);

        m_bodyIds.Add(bodyId);
        m_frameCount = 1;
    }

// ruler
    void Scene2()
    {
        Clear();
        CreateGround1();

        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = new b2Vec2(0.0f, 4.0f);
        bodyDef.rotation = b2MakeRot(0.5f * B2_PI);
        bodyDef.linearVelocity = new b2Vec2(0.0f, 0.0f);
        bodyDef.angularVelocity = -0.5f;

        b2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        b2Polygon box = b2MakeBox(0.75f, 0.01f);
        b2CreatePolygonShape(bodyId, shapeDef, box);

        m_bodyIds.Add(bodyId);
        m_frameCount = 1;
    }

// ragdoll
    void Scene3()
    {
        Clear();
        CreateGround2();

        float jointFrictionTorque = 0.03f;
        float jointHertz = 1.0f;
        float jointDampingRatio = 0.5f;

        CreateHuman(m_human, m_worldId, new b2Vec2(0.0f, 40.0f), 1.0f, jointFrictionTorque, jointHertz, jointDampingRatio, 1, null, true);

        m_frameCount = 1;
    }

    void Scene4()
    {
        Clear();
        CreateGround3();

        float a = 0.25f;
        b2Polygon box = b2MakeSquare(a);

        b2ShapeDef shapeDef = b2DefaultShapeDef();

        float offset = 0.01f;

        for (int i = 0; i < 5; ++i)
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_dynamicBody;

            float shift = (i % 2 == 0 ? -offset : offset);
            bodyDef.position = new b2Vec2(2.5f + shift, a + 2.0f * a * i);
            b2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

            m_bodyIds.Add(bodyId);
            b2CreatePolygonShape(bodyId, shapeDef, box);
        }

        b2Circle circle = new b2Circle(new b2Vec2(0.0f, 0.0f), 0.125f);
        shapeDef.density = 4.0f;

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_dynamicBody;
            bodyDef.position = new b2Vec2(-7.7f, 1.9f);
            bodyDef.linearVelocity = new b2Vec2(200.0f, 0.0f);
            bodyDef.isBullet = true;

            b2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
            b2CreateCircleShape(bodyId, shapeDef, circle);
            m_bodyIds.Add(bodyId);
        }

        m_frameCount = 1;
    }

    public override void Keyboard(int key)
    {
        switch ((Keys)key)
        {
            case Keys.Keypad1:
                Scene1();
                break;

            case Keys.Keypad2:
                Scene2();
                break;

            case Keys.Keypad3:
                Scene3();
                break;

            case Keys.Keypad4:
                Scene4();
                break;

            case Keys.Keypad5:
                Clear();
                m_continuous = !m_continuous;
                break;

            case Keys.V:
                Clear();
                m_speculative = !m_speculative;
                b2World_EnableSpeculative(m_worldId, m_speculative);
                break;

            case Keys.S:
                m_frameSkip = m_frameSkip > 0 ? 0 : 60;
                break;

            default:
                base.Keyboard(key);
                break;
        }
    }

    public override void Step(Settings settings)
    {
#if FALSE
    ImGui.SetNextWindowPos( new Vector2( 0.0f, 0.0f ) );
    ImGui.SetNextWindowSize( new Vector2( float( Draw.g_camera.m_width ), float( Draw.g_camera.m_height ) ) );
    ImGui.SetNextWindowBgAlpha( 0.0f );
    ImGui.Begin( "DropBackground", nullptr,
        ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize |
        ImGuiWindowFlags.NoScrollbar );

    ImDrawList* drawList = ImGui.GetWindowDrawList();

    const char* ContinuousText = m_continuous && m_speculative ? "Continuous ON" : "Continuous OFF";
    drawList->AddText( Draw.g_draw.m_largeFont, Draw.g_draw.m_largeFont->FontSize, { 40.0f, 40.0f }, IM_COL32_WHITE, ContinuousText );

    if ( m_frameSkip > 0 )
    {
        drawList->AddText( Draw.g_draw.m_mediumFont, Draw.g_draw.m_mediumFont->FontSize, { 40.0f, 40.0f + 64.0f + 20.0f },
        IM_COL32( 200, 200, 200, 255 ), "Slow Time" );
    }

    ImGui.End();
#endif

        //if (m_frameCount == 165)
        //{
        //	settings.pause = true;
        //	m_frameSkip = 30;
        //}

        settings.enableContinuous = m_continuous;

        if ((m_frameSkip == 0 || m_frameCount % m_frameSkip == 0) && settings.pause == false)
        {
            base.Step(settings);
        }
        else
        {
            bool pause = settings.pause;
            settings.pause = true;
            base.Step(settings);
            settings.pause = pause;
        }

        m_frameCount += 1;
    }
}