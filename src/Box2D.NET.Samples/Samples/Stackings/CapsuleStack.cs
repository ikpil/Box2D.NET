// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Stackings;

public class CapsuleStack : Sample
{
    private static readonly int SampleCapsuleStack = SampleFactory.Shared.RegisterSample("Stacking", "Capsule Stack", Create);

    struct Event
    {
        int indexA, indexB;
    }

    private static Sample Create(SampleContext context)
    {
        return new CapsuleStack(context);
    }

    public CapsuleStack(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.center = new B2Vec2(0.0f, 5.0f);
            m_camera.zoom = 6.0f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(0.0f, -1.0f);
            B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon polygon = b2MakeBox(10.0f, 1.0f);
            b2CreatePolygonShape(groundId, shapeDef, polygon);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;

            float a = 0.25f;
            B2Capsule capsule = new B2Capsule(new B2Vec2(-4.0f * a, 0.0f), new B2Vec2(4.0f * a, 0.0f), a);

            B2ShapeDef shapeDef = b2DefaultShapeDef();

            // rolling resistance increases stacking stability
            //shapeDef.rollingResistance = 0.2f;

            float y = 2.0f * a;

            for (int i = 0; i < 20; ++i)
            {
                bodyDef.position.Y = y;
                //bodyDef.position.x += ( i & 1 ) == 1 ? -0.5f * a : 0.5f * a;
                //bodyDef.linearVelocity = { 0.0f, -10.0f };
                B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

                b2CreateCapsuleShape(bodyId, shapeDef, capsule);

                y += 3.0f * a;
            }
        }
    }
}
