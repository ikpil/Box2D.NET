// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Stackings;

public class SingleBox : Sample
{
    b2BodyId m_bodyId;
    static int sampleSingleBox = RegisterSample("Stacking", "Single Box", Create);

    static Sample Create(Settings settings)
    {
        return new SingleBox(settings);
    }

    public SingleBox(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(0.0f, 2.5f);
            Draw.g_camera.m_zoom = 3.5f;
        }

        float extent = 1.0f;

        b2BodyDef bodyDef = b2DefaultBodyDef();
        b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

        float groundWidth = 66.0f * extent;
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        //shapeDef.friction = 0.5f;

        b2Segment segment = new b2Segment(new b2Vec2(-0.5f * 2.0f * groundWidth, 0.0f), new b2Vec2(0.5f * 2.0f * groundWidth, 0.0f));
        b2CreateSegmentShape(groundId, shapeDef, segment);
        bodyDef.type = b2BodyType.b2_dynamicBody;

        b2Polygon box = b2MakeBox(extent, extent);
        bodyDef.position = new b2Vec2(0.0f, 1.0f);
        bodyDef.linearVelocity = new b2Vec2(5.0f, 0.0f);
        m_bodyId = b2CreateBody(m_worldId, bodyDef);
        b2CreatePolygonShape(m_bodyId, shapeDef, box);
    }

    public override void Step(Settings settings)
    {
        base.Step(settings);

        // Draw.g_draw.DrawCircle({0.0f, 2.0f}, 1.0f, b2HexColor.b2_colorWhite);

        b2Vec2 position = b2Body_GetPosition(m_bodyId);
        DrawTextLine("(x, y) = (%.2g, %.2g)", position.x, position.y);
    }
}
