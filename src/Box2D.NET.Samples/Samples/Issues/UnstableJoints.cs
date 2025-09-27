// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Joints;

namespace Box2D.NET.Samples.Samples.Issues;

public class UnstableJoints : Sample
{
    private static readonly int SamplePrismaticJointCrash = SampleFactory.Shared.RegisterSample("Issues", "Unstable Joints", Create);

    private static Sample Create(SampleContext context)
    {
        return new UnstableJoints(context);
    }

    public UnstableJoints(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 1.75f);
            m_context.camera.m_zoom = 32.0f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new B2Vec2(-100.0f, 0.0f), new B2Vec2(100.0f, 0.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        B2BodyId centerId;
        {
            B2BodyDef bd = b2DefaultBodyDef();
            bd.type = B2BodyType.b2_dynamicBody;
            bd.position = new B2Vec2(0, 3);
            centerId = b2CreateBody(m_worldId, ref bd);

            B2ShapeDef sd = b2DefaultShapeDef();

            B2Circle circle;
            circle.center = new B2Vec2(0, 0);

            // Note: this will crash due to divergence (inf/nan) with a radius of 0.1
            //circle.radius = 0.1f;
            circle.radius = 0.5f;

            b2CreateCircleShape(centerId, ref sd, ref circle);
        }

        B2PrismaticJointDef jd = b2DefaultPrismaticJointDef();
        jd.enableSpring = true;
        jd.hertz = 10.0f;
        jd.dampingRatio = 2.0f;

        {
            B2BodyDef bd = b2DefaultBodyDef();
            bd.type = B2BodyType.b2_dynamicBody;
            bd.position = new B2Vec2(-3.5f, 3);

            B2BodyId leftId = b2CreateBody(m_worldId, ref bd);

            B2ShapeDef sd = b2DefaultShapeDef();

            B2Circle circle;
            circle.center = new B2Vec2(0, 0);
            circle.radius = 2.0f;
            b2CreateCircleShape(leftId, ref sd, ref circle);

            jd.@base.bodyIdA = centerId;
            jd.@base.bodyIdB = leftId;
            jd.targetTranslation = -3.0f;
            b2CreatePrismaticJoint(m_worldId, ref jd);
        }

        {
            B2BodyDef bd = b2DefaultBodyDef();
            bd.type = B2BodyType.b2_dynamicBody;
            bd.position = new B2Vec2(3.5f, 3);
            B2BodyId rightId = b2CreateBody(m_worldId, ref bd);

            B2ShapeDef sd = b2DefaultShapeDef();

            B2Circle circle;
            circle.center = new B2Vec2(0, 0);
            circle.radius = 2.0f;

            b2CreateCircleShape(rightId, ref sd, ref circle);

            jd.@base.bodyIdA = centerId;
            jd.@base.bodyIdB = rightId;
            jd.targetTranslation = 3.0f;
            b2CreatePrismaticJoint(m_worldId, ref jd);
        }
    }
}