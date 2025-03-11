// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Shapes;

public class OffsetShapes : Sample
{
    private static readonly int SampleOffsetShapes = SampleFactory.Shared.RegisterSample("Shapes", "Offset", Create);

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new OffsetShapes(ctx, settings);
    }

    public OffsetShapes(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.camera.m_zoom = 25.0f * 0.55f;
            m_context.camera.m_center = new B2Vec2(2.0f, 8.0f);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(-1.0f, 1.0f);
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon box = b2MakeOffsetBox(1.0f, 1.0f, new B2Vec2(10.0f, -2.0f), b2MakeRot(0.5f * B2_PI));
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);
        }

        {
            B2Capsule capsule = new B2Capsule(new B2Vec2(-5.0f, 1.0f), new B2Vec2(-4.0f, 1.0f), 0.25f);
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(13.5f, -0.75f);
            bodyDef.type = B2BodyType.b2_dynamicBody;
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreateCapsuleShape(bodyId, ref shapeDef, ref capsule);
        }

        {
            B2Polygon box = b2MakeOffsetBox(0.75f, 0.5f, new B2Vec2(9.0f, 2.0f), b2MakeRot(0.5f * B2_PI));
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(0.0f, 0.0f);
            bodyDef.type = B2BodyType.b2_dynamicBody;
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
        }
    }

    public override void Step(Settings settings)
    {
        base.Step(settings);

        m_context.g_draw.DrawTransform(b2Transform_identity);
    }
}
