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

public class SoftBody : Sample
{
    Donut m_donut;
    static int sampleDonut = RegisterSample("Joints", "Soft Body", Create);

    static Sample Create(Settings settings)
    {
        return new SoftBody(settings);
    }

    public SoftBody(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new B2Vec2(0.0f, 5.0f);
            Draw.g_camera.m_zoom = 25.0f * 0.25f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new B2Vec2(-20.0f, 0.0f), new B2Vec2(20.0f, 0.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        m_donut = new();
        m_donut.Spawn(m_worldId, new B2Vec2(0.0f, 10.0f), 2.0f, 0, null);
    }
}
