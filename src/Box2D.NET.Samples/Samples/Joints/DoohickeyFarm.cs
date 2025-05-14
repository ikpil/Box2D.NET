﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Joints;

public class DoohickeyFarm : Sample
{
    private static readonly int SampleDoohickey = SampleFactory.Shared.RegisterSample("Joints", "Doohickey", Create);

    private static Sample Create(SampleContext context)
    {
        return new DoohickeyFarm(context);
    }

    public DoohickeyFarm(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 5.0f);
            m_context.camera.m_zoom = 25.0f * 0.35f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new B2Vec2(-20.0f, 0.0f), new B2Vec2(20.0f, 0.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);

            B2Polygon box = b2MakeOffsetBox(1.0f, 1.0f, new B2Vec2(0.0f, 1.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);
        }

        float y = 4.0f;
        for (int i = 0; i < 4; ++i)
        {
            Doohickey doohickey = new Doohickey();
            doohickey.Spawn(m_worldId, new B2Vec2(0.0f, y), 0.5f);
            y += 2.0f;
        }
    }

}