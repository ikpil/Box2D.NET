// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Geometries;

namespace Box2D.NET.Samples.Samples.Robustness;

// Ensure prismatic joint stability when highly distorted
public class MultiplePrismatic : Sample
{
    private static readonly int SampleMultiplePrismatic = SampleFactory.Shared.RegisterSample("Robustness", "Multiple Prismatic", Create);

    static Sample Create(SampleContext context)
    {
        return new MultiplePrismatic(context);
    }

    public MultiplePrismatic(SampleContext context)
        : base(context)
    {
        if (m_context.restart == false)
        {
            m_context.camera.center = new B2Vec2(0.0f, 8.0f);
            m_context.camera.zoom = 25.0f * 0.5f;
        }

        B2BodyId groundId;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, bodyDef);
        }

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        B2Polygon box = b2MakeBox(0.5f, 0.5f);
        B2PrismaticJointDef jointDef = b2DefaultPrismaticJointDef();
        jointDef.@base.bodyIdA = groundId;
        jointDef.@base.localFrameA.p = new B2Vec2(0.0f, 0.0f);
        jointDef.@base.localFrameB.p = new B2Vec2(0.0f, -0.6f);
        jointDef.@base.drawScale = 1.0f;
        jointDef.@base.constraintHertz = 240.0f;
        jointDef.lowerTranslation = -6.0f;
        jointDef.upperTranslation = 6.0f;
        jointDef.enableLimit = true;

        for (int i = 0; i < 6; ++i)
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(0.0f, 0.6f + 1.2f * i);
            bodyDef.type = B2BodyType.b2_dynamicBody;
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

            b2CreatePolygonShape(bodyId, shapeDef, box);

            jointDef.@base.bodyIdB = bodyId;
            b2CreatePrismaticJoint(m_worldId, jointDef);

            jointDef.@base.bodyIdA = bodyId;
            jointDef.@base.localFrameA.p = new B2Vec2(0.0f, 0.6f);
        }

        // Increase the mouse force
        m_mouseForceScale = 100000.0f;
    }
}