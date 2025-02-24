﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using Box2D.NET.Samples;
using static Box2D.NET.id;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Stackings;

public class TiltedStack : Sample
{
    public const int e_columns = 10;
    public const int e_rows = 10;

    b2BodyId[] m_bodies = new b2BodyId[e_rows * e_columns];
    static int sampleTiltedStack = RegisterSample("Stacking", "Tilted Stack", Create);

    static Sample Create(Settings settings)
    {
        return new TiltedStack(settings);
    }


    public TiltedStack(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(7.5f, 7.5f);
            Draw.g_camera.m_zoom = 20.0f;
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new b2Vec2(0.0f, -1.0f);
            b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            b2Polygon box = b2MakeBox(1000.0f, 1.0f);
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(groundId, shapeDef, box);
        }

        for (int i = 0; i < e_rows * e_columns; ++i)
        {
            m_bodies[i] = b2_nullBodyId;
        }

        {
            b2Polygon box = b2MakeRoundedBox(0.45f, 0.45f, 0.05f);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 1.0f;
            shapeDef.friction = 0.3f;

            float offset = 0.2f;
            float dx = 5.0f;
            float xroot = -0.5f * dx * (e_columns - 1.0f);

            for (int j = 0; j < e_columns; ++j)
            {
                float x = xroot + j * dx;

                for (int i = 0; i < e_rows; ++i)
                {
                    b2BodyDef bodyDef = b2DefaultBodyDef();
                    bodyDef.type = b2BodyType.b2_dynamicBody;

                    int n = j * e_rows + i;

                    bodyDef.position = new b2Vec2(x + offset * i, 0.5f + 1.0f * i);
                    b2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

                    m_bodies[n] = bodyId;

                    b2CreatePolygonShape(bodyId, shapeDef, box);
                }
            }
        }
    }
}
