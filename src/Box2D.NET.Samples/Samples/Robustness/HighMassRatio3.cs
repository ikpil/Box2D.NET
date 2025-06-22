﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Robustness;

// Big box on small triangles
public class HighMassRatio3 : Sample
{
    private static readonly int SampleHighMassRatio3 = SampleFactory.Shared.RegisterSample("Robustness", "HighMassRatio3", Create);

    private static Sample Create(SampleContext context)
    {
        return new HighMassRatio3(context);
    }

    public HighMassRatio3(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_camera.m_center = new B2Vec2(0.0f, 16.5f);
            m_camera.m_zoom = 25.0f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon box = b2MakeOffsetBox(50.0f, 1.0f, new B2Vec2(0.0f, -1.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            B2ShapeDef shapeDef = b2DefaultShapeDef();

            float extent = 1.0f;
            B2Vec2[] points = new B2Vec2[3] { new B2Vec2(-0.5f * extent, 0.0f), new B2Vec2(0.5f * extent, 0.0f), new B2Vec2(0.0f, 1.0f * extent) };
            B2Hull hull = b2ComputeHull(points, 3);
            B2Polygon smallTriangle = b2MakePolygon(ref hull, 0.0f);
            B2Polygon bigBox = b2MakeBox(10.0f * extent, 10.0f * extent);

            {
                bodyDef.position = new B2Vec2(-9.0f * extent, 0.5f * extent);
                B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
                b2CreatePolygonShape(bodyId, ref shapeDef, ref smallTriangle);
            }

            {
                bodyDef.position = new B2Vec2(9.0f * extent, 0.5f * extent);
                B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
                b2CreatePolygonShape(bodyId, ref shapeDef, ref smallTriangle);
            }

            {
                bodyDef.position = new B2Vec2(0.0f, (10.0f + 4.0f) * extent);
                B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
                b2CreatePolygonShape(bodyId, ref shapeDef, ref bigBox);
            }
        }
    }
}