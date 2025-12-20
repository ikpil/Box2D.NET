// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Worlds;

public class WorkbenchWorld : Sample
{
    private static readonly int SampleLargeWorld = SampleFactory.Shared.RegisterSample("World", "Workbench World", Create);

    private static Sample Create(SampleContext context)
    {
        return new WorkbenchWorld(context);
    }

    public WorkbenchWorld(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.center = new B2Vec2(0.0f, 5.0f);
            m_camera.zoom = 25.0f * 0.5f;
        }

        B2ShapeId shapeIdA = CreateCircle(m_worldId, new B2Vec2(0.0f, 0.0f), 1.0f);
        B2ShapeId shapeIdB = CreateCircle(m_worldId, new B2Vec2(1.0f, 1.0f), 1.0f);
    }
    
    public static B2ShapeId CreateCircle(B2WorldId worldId, B2Vec2 position, float radius)
    {
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = position;
        bodyDef.gravityScale = 0.0f;
        var bodyIdA = b2CreateBody(worldId, bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        shapeDef.material.friction = 0.5f;

        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), radius);
        B2ShapeId shapeId = b2CreateCircleShape(bodyIdA, ref shapeDef, ref circle);
        
        return shapeId;
    }

}
