// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Shapes;

public class Friction : Sample
{
    static int sampleFriction = RegisterSample("Shapes", "Friction", Create);

    static Sample Create(Settings settings)
    {
        return new Friction(settings);
    }

    public Friction(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new B2Vec2(0.0f, 14.0f);
            Draw.g_camera.m_zoom = 25.0f * 0.6f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.2f;

            B2Segment segment = new B2Segment(new B2Vec2(-40.0f, 0.0f), new B2Vec2(40.0f, 0.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);

            B2Polygon box = b2MakeOffsetBox(13.0f, 0.25f, new B2Vec2(-4.0f, 22.0f), b2MakeRot(-0.25f));
            b2CreatePolygonShape(groundId, shapeDef, box);

            box = b2MakeOffsetBox(0.25f, 1.0f, new B2Vec2(10.5f, 19.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, shapeDef, box);

            box = b2MakeOffsetBox(13.0f, 0.25f, new B2Vec2(4.0f, 14.0f), b2MakeRot(0.25f));
            b2CreatePolygonShape(groundId, shapeDef, box);

            box = b2MakeOffsetBox(0.25f, 1.0f, new B2Vec2(-10.5f, 11.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, shapeDef, box);

            box = b2MakeOffsetBox(13.0f, 0.25f, new B2Vec2(-4.0f, 6.0f), b2MakeRot(-0.25f));
            b2CreatePolygonShape(groundId, shapeDef, box);
        }

        {
            B2Polygon box = b2MakeBox(0.5f, 0.5f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 25.0f;

            float[] friction = [0.75f, 0.5f, 0.35f, 0.1f, 0.0f];

            for (int i = 0; i < 5; ++i)
            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                bodyDef.position = new B2Vec2(-15.0f + 4.0f * i, 28.0f);
                B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

                shapeDef.friction = friction[i];
                b2CreatePolygonShape(bodyId, shapeDef, box);
            }
        }
    }
}
