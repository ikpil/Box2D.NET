// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Shapes;

// This sample tests a static shape being recreated every step.
public class RecreateStatic : Sample
{
    B2BodyId m_groundId;
    private static readonly int SampleSingleBox = SampleFactory.Shared.RegisterSample("Shapes", "Recreate Static", Create);

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new RecreateStatic(ctx, settings);
    }

    public RecreateStatic(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.g_camera.m_center = new B2Vec2(0.0f, 2.5f);
            m_context.g_camera.m_zoom = 3.5f;
        }

        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2ShapeDef shapeDef = b2DefaultShapeDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Vec2(0.0f, 1.0f);
        B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

        B2Polygon box = b2MakeBox(1.0f, 1.0f);
        b2CreatePolygonShape(bodyId, ref shapeDef, ref box);

        m_groundId = new B2BodyId();
    }

    public override void Step(Settings settings)
    {
        if (B2_IS_NON_NULL(m_groundId))
        {
            b2DestroyBody(m_groundId);
            m_groundId = new B2BodyId();
        }

        B2BodyDef bodyDef = b2DefaultBodyDef();
        m_groundId = b2CreateBody(m_worldId, ref bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();

        // Invoke contact creation so that contact points are created immediately
        // on a static body.
        shapeDef.invokeContactCreation = true;

        B2Segment segment = new B2Segment(new B2Vec2(-10.0f, 0.0f), new B2Vec2(10.0f, 0.0f));
        b2CreateSegmentShape(m_groundId, ref shapeDef, ref segment);

        base.Step(settings);
    }
}
