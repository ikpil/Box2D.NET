// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Joints;

namespace Box2D.NET.Samples.Samples.Issues;

public class UnstableWindmill : Sample
{
    private static readonly int SampleUnstableWindmill = SampleFactory.Shared.RegisterSample("Issues", "Unstable Windmill", Create);

    private static Sample Create(SampleContext context)
    {
        return new UnstableWindmill(context);
    }

    public UnstableWindmill(SampleContext context)
        : base(context)
    {
        if (m_context.restart == false)
        {
            m_context.camera.center = new B2Vec2(0.0f, 1.75f);
            m_context.camera.zoom = 32.0f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new B2Vec2(-100.0f, -10.0f), new B2Vec2(100.0f, -10.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        B2BodyDef bdef = b2DefaultBodyDef();
        bdef.gravityScale = 0.0f;
        bdef.type = B2BodyType.b2_dynamicBody;
        B2ShapeDef sdef = b2DefaultShapeDef();
        sdef.material = b2DefaultSurfaceMaterial();
        sdef.material.friction = 0.1f;

        // center
        bdef.position = new B2Vec2(10, 10);
        B2BodyId center = b2CreateBody(m_worldId, bdef);
        B2Circle circle = new B2Circle(new B2Vec2(0, 0), 5);
        b2CreateCircleShape(center, sdef, circle);

        // rotors
        B2WeldJointDef wjdef = b2DefaultWeldJointDef();

        // This simulation can be stabilized by using a lower constraint stiffness
        wjdef.@base.constraintHertz = 30.0f;
        wjdef.@base.bodyIdA = center;

        B2Polygon polygon;

        bdef.position = new B2Vec2(10, 0);
        B2BodyId body = b2CreateBody(m_worldId, bdef);
        polygon = b2MakeBox(4, 5);
        b2CreatePolygonShape(body, sdef, polygon);
        wjdef.@base.localFrameA = new B2Transform(new B2Vec2(0, -5), b2Rot_identity);
        wjdef.@base.bodyIdB = body;
        wjdef.@base.localFrameB = new B2Transform(new B2Vec2(0, 5), b2Rot_identity);
        b2CreateWeldJoint(m_worldId, ref wjdef);

        bdef.position = new B2Vec2(20, 10);
        body = b2CreateBody(m_worldId, bdef);
        polygon = b2MakeBox(5, 4);
        b2CreatePolygonShape(body, ref sdef, ref polygon);
        wjdef.@base.localFrameA = new B2Transform(new B2Vec2(5, 0), b2Rot_identity);
        wjdef.@base.bodyIdB = body;
        wjdef.@base.localFrameB = new B2Transform(new B2Vec2(-5, 0), b2Rot_identity);
        b2CreateWeldJoint(m_worldId, ref wjdef);

        bdef.position = new B2Vec2(10, 20);
        body = b2CreateBody(m_worldId, bdef);
        polygon = b2MakeBox(4, 5);
        b2CreatePolygonShape(body, ref sdef, ref polygon);
        wjdef.@base.localFrameA = new B2Transform(new B2Vec2(0, 5), b2Rot_identity);
        wjdef.@base.bodyIdB = body;
        wjdef.@base.localFrameB = new B2Transform(new B2Vec2(0, -5), b2Rot_identity);
        b2CreateWeldJoint(m_worldId, ref wjdef);

        bdef.position = new B2Vec2(0, 10);
        body = b2CreateBody(m_worldId, bdef);
        polygon = b2MakeBox(5, 4);
        b2CreatePolygonShape(body, ref sdef, ref polygon);
        wjdef.@base.localFrameA = new B2Transform(new B2Vec2(-5, 0), b2Rot_identity);
        wjdef.@base.bodyIdB = body;
        wjdef.@base.localFrameB = new B2Transform(new B2Vec2(5, 0), b2Rot_identity);
        b2CreateWeldJoint(m_worldId, ref wjdef);
    }
}