// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Shapes;

public class ConveyorBelt : Sample
{
    private static readonly int SampleConveyorBelt = SampleFactory.Shared.RegisterSample("Shapes", "Conveyor Belt", Create);

    private static Sample Create(SampleContext context)
    {
        return new ConveyorBelt(context);
    }

    public ConveyorBelt(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_camera.m_center = new B2Vec2(2.0f, 7.5f);
            m_camera.m_zoom = 12.0f;
        }

        // Ground
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new B2Vec2(-20.0f, 0.0f), new B2Vec2(20.0f, 0.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        // Platform
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(-5.0f, 5.0f);
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2Polygon box = b2MakeRoundedBox(10.0f, 0.25f, 0.25f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.material.friction = 0.8f;
            shapeDef.material.tangentSpeed = 2.0f;

            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
        }

        // Boxes
        {
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon cube = b2MakeSquare(0.5f);
            for (int i = 0; i < 5; ++i)
            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                bodyDef.position = new B2Vec2(-10.0f + 2.0f * i, 7.0f);
                B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

                b2CreatePolygonShape(bodyId, ref shapeDef, ref cube);
            }
        }
    }
}
