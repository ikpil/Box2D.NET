// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using static Box2D.NET.joint;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;

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
            Draw.g_camera.m_center = new b2Vec2(0.0f, 7.0f);
            Draw.g_camera.m_zoom = 25.0f * 0.4f;
        }

        {
            b2BodyId groundId;
            b2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, bodyDef);
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2Segment segment = new b2Segment(new b2Vec2(-20.0f, 0.0f), new b2Vec2(20.0f, 0.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_dynamicBody;
            bodyDef.position = new b2Vec2(-4.0f, 2.0f);
            b2BodyId bodyId1 = b2CreateBody(m_worldId, bodyDef);

            b2Polygon box = b2MakeSquare(2.0f);
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(bodyId1, shapeDef, box);

            bodyDef.position = new b2Vec2(4.0f, 2.0f);
            b2BodyId bodyId2 = b2CreateBody(m_worldId, bodyDef);
            b2CreatePolygonShape(bodyId2, shapeDef, box);

            b2NullJointDef jointDef = b2DefaultNullJointDef();
            jointDef.bodyIdA = bodyId1;
            jointDef.bodyIdB = bodyId2;

            b2CreateNullJoint(m_worldId, jointDef);
        }
    }
}
