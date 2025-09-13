// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2MathFunction;

namespace Box2D.NET.Samples.Samples.Joints;

public class MultiplePrismatic : Sample
{
    private static readonly int SampleMultiplePrismatic = SampleFactory.Shared.RegisterSample("Joints", "Multiple Prismatic", Create);

    static Sample Create(SampleContext context)
    {
        return new MultiplePrismatic(context);
    }

    public MultiplePrismatic(SampleContext context)
        : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 8.0f);
            m_context.camera.m_zoom = 25.0f * 0.5f;
        }

        B2BodyId groundId;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon box = b2MakeOffsetBox(20.0f, 1.0f, new B2Vec2(0.0f, -1.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);

            box = b2MakeOffsetBox(1.0f, 5.0f, new B2Vec2(19.0f, 5.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);

            box = b2MakeOffsetBox(1.0f, 5.0f, new B2Vec2(-19.0f, 5.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);
        }

        {
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon box = b2MakeBox(3.0f, 0.5f);
            B2PrismaticJointDef jointDef = b2DefaultPrismaticJointDef();
            jointDef.@base.bodyIdA = groundId;
            jointDef.@base.localFrameA.p = new B2Vec2(0.0f, 0.0f);
            jointDef.@base.localFrameB.p = new B2Vec2(0.0f, -0.6f);
            jointDef.@base.drawScale = 1.0f;
            jointDef.motorSpeed = 0.0f;
            jointDef.maxMotorForce = 25.0f;
            jointDef.enableMotor = true;
            jointDef.lowerTranslation = -3.0f;
            jointDef.upperTranslation = 3.0f;
            jointDef.enableLimit = true;
            jointDef.hertz = 1.0f;
            jointDef.dampingRatio = 0.5f;
            jointDef.enableSpring = true;

            for (int i = 0; i < 3; ++i)
            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.position = new B2Vec2(0.0f, 0.6f + 1.2f * i);
                bodyDef.type = B2BodyType.b2_dynamicBody;
                B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
                b2CreatePolygonShape(bodyId, ref shapeDef, ref box);

                jointDef.@base.bodyIdB = bodyId;
                b2CreatePrismaticJoint(m_worldId, ref jointDef);

                jointDef.@base.bodyIdA = bodyId;
                jointDef.@base.localFrameA.p = new B2Vec2(0.0f, 0.6f);
                jointDef.@base.localFrameB.p = new B2Vec2(0.0f, -0.6f);
            }
        }
    }
}