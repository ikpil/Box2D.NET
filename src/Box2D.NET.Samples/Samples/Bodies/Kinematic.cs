// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.Samples.Graphics.Draws;

namespace Box2D.NET.Samples.Samples.Bodies;

// This shows how to drive a kinematic body to reach a target
public class Kinematic : Sample
{
    private static readonly int SampleKinematic = SampleFactory.Shared.RegisterSample("Bodies", "Kinematic", Create);

    private B2BodyId m_bodyId;
    private float m_amplitude;
    private float m_time;
    private float m_timeStep;

    private static Sample Create(SampleContext context)
    {
        return new Kinematic(context);
    }

    public Kinematic(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.center = new B2Vec2(0.0f, 0.0f);
            m_camera.zoom = 4.0f;
        }

        m_amplitude = 2.0f;

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_kinematicBody;
            bodyDef.position.X = 2.0f * m_amplitude;

            m_bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2Polygon box = b2MakeBox(0.1f, 1.0f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(m_bodyId, ref shapeDef, ref box);
        }

        m_time = 0.0f;
    }


    public override void Step()
    {
        m_timeStep = m_context.hertz > 0.0f ? 1.0f / m_context.hertz : 0.0f;
        if (m_context.pause && m_context.singleStep == false)
        {
            m_timeStep = 0.0f;
        }


        base.Step();

        m_time += m_timeStep;
    }

    public override void Draw()
    {
        base.Draw();

        if (m_timeStep > 0.0f)
        {
            B2Vec2 point;
            point.X = 2.0f * m_amplitude * MathF.Cos(m_time);
            point.Y = m_amplitude * MathF.Sin(2.0f * m_time);
            B2Rot rotation = b2MakeRot(2.0f * m_time);

            B2Vec2 axis = b2RotateVector(rotation, new B2Vec2(0.0f, 1.0f));
            DrawLine(m_draw, point - 0.5f * axis, point + 0.5f * axis, B2HexColor.b2_colorPlum);
            DrawPoint(m_draw, point, 10.0f, B2HexColor.b2_colorPlum);

            b2Body_SetTargetTransform(m_bodyId, new B2Transform(point, rotation), m_timeStep);
        }
    }
}
