// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Continuous;

// This shows that while Box2D uses speculative collision, it does not lead to speculative ghost collisions at small distances
public class SpeculativeGhost : Sample
{
    private static readonly int SampleSpeculativeGhost = SampleFactory.Shared.RegisterSample("Continuous", "Speculative Ghost", Create);

    private static Sample Create(SampleContext context)
    {
        return new SpeculativeGhost(context);
    }

    public SpeculativeGhost(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.center = new B2Vec2(0.0f, 1.75f);
            m_camera.zoom = 2.0f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new B2Vec2(-10.0f, 0.0f), new B2Vec2(10.0f, 0.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);

            B2Polygon box = b2MakeOffsetBox(1.0f, 0.1f, new B2Vec2(0.0f, 0.9f), b2Rot_identity);
            b2CreatePolygonShape(groundId, shapeDef, box);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;

            // The speculative distance is 0.02 meters, so this avoid it
            bodyDef.position = new B2Vec2(0.015f, 2.515f);
            bodyDef.linearVelocity = new B2Vec2(0.1f * 1.25f * m_context.hertz, -0.1f * 1.25f * m_context.hertz);
            bodyDef.gravityScale = 0.0f;
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon box = b2MakeSquare(0.25f);
            b2CreatePolygonShape(bodyId, shapeDef, box);
        }
    }
}
