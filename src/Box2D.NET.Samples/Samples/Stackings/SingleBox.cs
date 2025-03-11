// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Stackings;

public class SingleBox : Sample
{
    private static readonly int SampleSingleBox = SampleFactory.Shared.RegisterSample("Stacking", "Single Box", Create);
    
    B2BodyId m_bodyId;

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new SingleBox(ctx, settings);
    }

    public SingleBox(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.g_camera.m_center = new B2Vec2(0.0f, 2.5f);
            m_context.g_camera.m_zoom = 3.5f;
        }

        float extent = 1.0f;

        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

        float groundWidth = 66.0f * extent;
        B2ShapeDef shapeDef = b2DefaultShapeDef();
        //shapeDef.friction = 0.5f;

        B2Segment segment = new B2Segment(new B2Vec2(-0.5f * 2.0f * groundWidth, 0.0f), new B2Vec2(0.5f * 2.0f * groundWidth, 0.0f));
        b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        bodyDef.type = B2BodyType.b2_dynamicBody;

        B2Polygon box = b2MakeBox(extent, extent);
        bodyDef.position = new B2Vec2(0.0f, 1.0f);
        bodyDef.linearVelocity = new B2Vec2(5.0f, 0.0f);
        m_bodyId = b2CreateBody(m_worldId, ref bodyDef);
        b2CreatePolygonShape(m_bodyId, ref shapeDef, ref box);
    }

    public override void Step(Settings settings)
    {
        base.Step(settings);

        // m_context.g_draw.DrawCircle({0.0f, 2.0f}, 1.0f, b2HexColor.b2_colorWhite);

        B2Vec2 position = b2Body_GetPosition(m_bodyId);
        DrawTextLine("(x, y) = (%.2g, %.2g)", position.x, position.y);
    }
}
