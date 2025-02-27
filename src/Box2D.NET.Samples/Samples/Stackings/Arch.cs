// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Stackings;

public class Arch : Sample
{
    static int sampleArch = RegisterSample("Stacking", "Arch", Create);

    static Sample Create(Settings settings)
    {
        return new Arch(settings);
    }

    public Arch(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            B2.g_camera.m_center = new B2Vec2(0.0f, 8.0f);
            B2.g_camera.m_zoom = 25.0f * 0.35f;
        }

        B2Vec2[] ps1 = new B2Vec2[]
        {
            new B2Vec2(16.0f, 0.0f),
            new B2Vec2(14.93803712795643f, 5.133601056842984f),
            new B2Vec2(13.79871746027416f, 10.24928069555078f),
            new B2Vec2(12.56252963284711f, 15.34107019122473f),
            new B2Vec2(11.20040987372525f, 20.39856541571217f),
            new B2Vec2(9.66521217819836f, 25.40369899225096f),
            new B2Vec2(7.87179930638133f, 30.3179337000085f),
            new B2Vec2(5.635199558196225f, 35.03820717801641f),
            new B2Vec2(2.405937953536585f, 39.09554102558315f),
        };

        B2Vec2[] ps2 = new B2Vec2[]
        {
            new B2Vec2(24.0f, 0.0f),
            new B2Vec2(22.33619528222415f, 6.02299846205841f),
            new B2Vec2(20.54936888969905f, 12.00964361211476f),
            new B2Vec2(18.60854610798073f, 17.9470321677465f),
            new B2Vec2(16.46769273811807f, 23.81367936585418f),
            new B2Vec2(14.05325025774858f, 29.57079353071012f),
            new B2Vec2(11.23551045834022f, 35.13775818285372f),
            new B2Vec2(7.752568160730571f, 40.30450679009583f),
            new B2Vec2(3.016931552701656f, 44.28891593799322f),
        };

        float scale = 0.25f;
        for (int i = 0; i < 9; ++i)
        {
            ps1[i] = b2MulSV(scale, ps1[i]);
            ps2[i] = b2MulSV(scale, ps2[i]);
        }

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.friction = 0.6f;

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);
            B2Segment segment = new B2Segment(new B2Vec2(-100.0f, 0.0f), new B2Vec2(100.0f, 0.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;

            for (int i = 0; i < 8; ++i)
            {
                B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
                B2Vec2[] ps = new B2Vec2[4] { ps1[i], ps2[i], ps2[i + 1], ps1[i + 1] };
                B2Hull hull = b2ComputeHull(ps, 4);
                B2Polygon polygon = b2MakePolygon(hull, 0.0f);
                b2CreatePolygonShape(bodyId, shapeDef, polygon);
            }

            for (int i = 0; i < 8; ++i)
            {
                B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
                B2Vec2[] ps = new B2Vec2[4]
                {
                    new B2Vec2(-ps2[i].x, ps2[i].y),
                    new B2Vec2(-ps1[i].x, ps1[i].y),
                    new B2Vec2(-ps1[i + 1].x, ps1[i + 1].y),
                    new B2Vec2(-ps2[i + 1].x, ps2[i + 1].y),
                };
                B2Hull hull = b2ComputeHull(ps, 4);
                B2Polygon polygon = b2MakePolygon(hull, 0.0f);
                b2CreatePolygonShape(bodyId, shapeDef, polygon);
            }

            {
                B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
                B2Vec2[] ps = new B2Vec2[4] { ps1[8], ps2[8], new B2Vec2(-ps2[8].x, ps2[8].y), new B2Vec2(-ps1[8].x, ps1[8].y) };
                B2Hull hull = b2ComputeHull(ps, 4);
                B2Polygon polygon = b2MakePolygon(hull, 0.0f);
                b2CreatePolygonShape(bodyId, shapeDef, polygon);
            }

            for (int i = 0; i < 4; ++i)
            {
                B2Polygon box = b2MakeBox(2.0f, 0.5f);
                bodyDef.position = new B2Vec2(0.0f, 0.5f + ps2[8].y + 1.0f * i);
                B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
                b2CreatePolygonShape(bodyId, shapeDef, box);
            }
        }
    }
}
