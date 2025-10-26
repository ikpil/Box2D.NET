// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;


namespace Box2D.NET.Samples.Samples.Bodies;

public class SetVelocity : Sample
{
    private static int SampleSetVelocity = SampleFactory.Shared.RegisterSample("Bodies", "Set Velocity", Create);

    private static Sample Create(SampleContext context)
    {
        return new SetVelocity(context);
    }

    private B2BodyId m_bodyId;

    private SetVelocity(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_context.camera.center = new B2Vec2(0.0f, 2.5f);
            m_context.camera.zoom = 3.5f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(0.0f, -0.25f);
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon box = b2MakeBox(20.0f, 0.25f);
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon box = b2MakeSquare(0.5f);
            bodyDef.position = new B2Vec2(0.0f, 0.5f);
            m_bodyId = b2CreateBody(m_worldId, ref bodyDef);
            b2CreatePolygonShape(m_bodyId, ref shapeDef, ref box);
        }
    }

    public override void Step()
    {
        base.Step();
        b2Body_SetLinearVelocity(m_bodyId, new B2Vec2(0.0f, -20.0f));
    }

    public override void Draw()
    {
        base.Draw();
        B2Vec2 position = b2Body_GetPosition(m_bodyId);
        DrawTextLine($"(x, y) = {position.X:G}, {position.Y:G})");
    }
}