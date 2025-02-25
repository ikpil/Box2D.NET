// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Joints;

public class DoohickeyFarm : Sample
{
    static int sampleDoohickey = RegisterSample("Joints", "Doohickey", Create);

    static Sample Create(Settings settings)
    {
        return new DoohickeyFarm(settings);
    }

    public DoohickeyFarm(Settings settings)
        : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(0.0f, 5.0f);
            Draw.g_camera.m_zoom = 25.0f * 0.35f;
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2Segment segment = new b2Segment(new b2Vec2(-20.0f, 0.0f), new b2Vec2(20.0f, 0.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);

            b2Polygon box = b2MakeOffsetBox(1.0f, 1.0f, new b2Vec2(0.0f, 1.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, shapeDef, box);
        }

        float y = 4.0f;
        for (int i = 0; i < 4; ++i)
        {
            Doohickey doohickey = new Doohickey();
            doohickey.Spawn(m_worldId, new b2Vec2(0.0f, y), 0.5f);
            y += 2.0f;
        }
    }

    public override void Step(Settings settings)
    {
        base.Step(settings);
    }
}
