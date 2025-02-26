// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Joints;

// This sample shows how to use a null joint to prevent collision between two bodies.
// This is more specific than filters. It also shows that sleeping is coupled by the null joint.
public class NullJoint : Sample
{
    static int sampleNullJoint = RegisterSample("Joints", "Null Joint", Create);

    static Sample Create(Settings settings)
    {
        return new NullJoint(settings);
    }

    public NullJoint(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new B2Vec2(0.0f, 7.0f);
            Draw.g_camera.m_zoom = 25.0f * 0.4f;
        }

        {
            B2BodyId groundId;
            B2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new B2Vec2(-20.0f, 0.0f), new B2Vec2(20.0f, 0.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(-4.0f, 2.0f);
            B2BodyId bodyId1 = b2CreateBody(m_worldId, bodyDef);

            B2Polygon box = b2MakeSquare(2.0f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(bodyId1, shapeDef, box);

            bodyDef.position = new B2Vec2(4.0f, 2.0f);
            B2BodyId bodyId2 = b2CreateBody(m_worldId, bodyDef);
            b2CreatePolygonShape(bodyId2, shapeDef, box);

            B2NullJointDef jointDef = b2DefaultNullJointDef();
            jointDef.bodyIdA = bodyId1;
            jointDef.bodyIdB = bodyId2;

            b2CreateNullJoint(m_worldId, jointDef);
        }
    }
}
