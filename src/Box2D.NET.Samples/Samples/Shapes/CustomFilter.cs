// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Box2D.NET.Samples.Samples.Shapes;

// This shows how to use custom filtering
public class CustomFilter : Sample
{
    private static readonly int SampleCustomFilter = SampleFactory.Shared.RegisterSample("Shapes", "Custom Filter", Create);

    public const int e_count = 10;

    private B2BodyId[] m_bodyIds = new B2BodyId[e_count];
    private B2ShapeId[] m_shapeIds = new B2ShapeId[e_count];

    private static Sample Create(SampleContext context)
    {
        return new CustomFilter(context);
    }

    public CustomFilter(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 5.0f);
            m_context.camera.m_zoom = 10.0f;
        }

        // Register custom filter
        b2World_SetCustomFilterCallback(m_worldId, CustomFilterStatic, this);

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);
            B2Segment segment = new B2Segment(new B2Vec2(-40.0f, 0.0f), new B2Vec2(40.0f, 0.0f));

            B2ShapeDef shapeDef = b2DefaultShapeDef();

            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon box = b2MakeSquare(1.0f);
            float x = -e_count;

            for (int i = 0; i < e_count; ++i)
            {
                bodyDef.position = new B2Vec2(x, 5.0f);
                m_bodyIds[i] = b2CreateBody(m_worldId, ref bodyDef);

                shapeDef.userData = i + 1;
                m_shapeIds[i] = b2CreatePolygonShape(m_bodyIds[i], ref shapeDef, ref box);
                x += 2.0f;
            }
        }
    }

    public override void Step()
    {
        base.Step();
    }

    bool ShouldCollide(B2ShapeId shapeIdA, B2ShapeId shapeIdB)
    {
        object userDataA = b2Shape_GetUserData(shapeIdA);
        object userDataB = b2Shape_GetUserData(shapeIdB);

        if (userDataA == null || userDataB == null)
        {
            return true;
        }

        int indexA = (int)userDataA;
        int indexB = (int)userDataB;

        return ((indexA & 1) + (indexB & 1)) != 1;
    }

    static bool CustomFilterStatic(B2ShapeId shapeIdA, B2ShapeId shapeIdB, object context)
    {
        CustomFilter customFilter = context as CustomFilter;

        return customFilter.ShouldCollide(shapeIdA, shapeIdB);
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);

        DrawTextLine("Custom filter disables collision between odd and even shapes");
        

        for (int i = 0; i < e_count; ++i)
        {
            B2Vec2 p = b2Body_GetPosition(m_bodyIds[i]);
            m_context.draw.DrawString(new B2Vec2(p.X, p.Y), $"{i}");
        }
    }
}