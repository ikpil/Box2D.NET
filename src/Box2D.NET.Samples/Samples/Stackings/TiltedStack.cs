// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Samples;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Stackings;

public class TiltedStack : Sample
{
    private static readonly int SampleTiltedStack = SampleFactory.Shared.RegisterSample("Stacking", "Tilted Stack", Create);
    
    public const int e_columns = 10;
    public const int e_rows = 10;

    B2BodyId[] m_bodies = new B2BodyId[e_rows * e_columns];

    private static Sample Create(Settings settings)
    {
        return new TiltedStack(settings);
    }


    public TiltedStack(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            B2.g_camera.m_center = new B2Vec2(7.5f, 7.5f);
            B2.g_camera.m_zoom = 20.0f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(0.0f, -1.0f);
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2Polygon box = b2MakeBox(1000.0f, 1.0f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(groundId, ref shapeDef, box);
        }

        for (int i = 0; i < e_rows * e_columns; ++i)
        {
            m_bodies[i] = b2_nullBodyId;
        }

        {
            B2Polygon box = b2MakeRoundedBox(0.45f, 0.45f, 0.05f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
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
                    B2BodyDef bodyDef = b2DefaultBodyDef();
                    bodyDef.type = B2BodyType.b2_dynamicBody;

                    int n = j * e_rows + i;

                    bodyDef.position = new B2Vec2(x + offset * i, 0.5f + 1.0f * i);
                    B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

                    m_bodies[n] = bodyId;

                    b2CreatePolygonShape(bodyId, ref shapeDef, box);
                }
            }
        }
    }
}
