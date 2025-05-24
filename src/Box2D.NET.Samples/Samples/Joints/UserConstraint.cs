// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Joints;

// This shows how you can implement a constraint outside of Box2D
public class UserConstraint : Sample
{
    private static readonly int SampleUserConstraintIndex = SampleFactory.Shared.RegisterSample("Joints", "User Constraint", Create);

    private B2BodyId m_bodyId;
    private float[] m_impulses = new float[2];

    private static Sample Create(SampleContext context)
    {
        return new UserConstraint(context);
    }

    public UserConstraint(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_camera.m_center = new B2Vec2(3.0f, -1.0f);
            m_camera.m_zoom = 25.0f * 0.15f;
        }

        B2Polygon box = b2MakeBox(1.0f, 0.5f);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 20.0f;

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.gravityScale = 1.0f;
        bodyDef.angularDamping = 0.5f;
        bodyDef.linearDamping = 0.2f;
        m_bodyId = b2CreateBody(m_worldId, ref bodyDef);
        b2CreatePolygonShape(m_bodyId, ref shapeDef, ref box);

        m_impulses[0] = 0.0f;
        m_impulses[1] = 0.0f;
    }

    public override void Step()
    {
        base.Step();

        B2Transform axes = b2Transform_identity;
        m_draw.DrawTransform(axes);

        if (m_context.settings.pause)
        {
            return;
        }

        float timeStep = m_context.settings.hertz > 0.0f ? 1.0f / m_context.settings.hertz : 0.0f;
        if (timeStep == 0.0f)
        {
            return;
        }

        const float hertz = 3.0f;
        const float zeta = 0.7f;
        const float maxForce = 1000.0f;

        float omega = 2.0f * B2_PI * hertz;
        float sigma = 2.0f * zeta + timeStep * omega;
        float s = timeStep * omega * sigma;
        float impulseCoefficient = 1.0f / (1.0f + s);
        float massCoefficient = s * impulseCoefficient;
        float biasCoefficient = omega / sigma;

        B2Vec2[] localAnchors = new B2Vec2[2]
        {
            new B2Vec2(1.0f, -0.5f), new B2Vec2(1.0f, 0.5f)
        };
        float mass = b2Body_GetMass(m_bodyId);
        float invMass = mass < 0.0001f ? 0.0f : 1.0f / mass;
        float inertiaTensor = b2Body_GetRotationalInertia(m_bodyId);
        float invI = inertiaTensor < 0.0001f ? 0.0f : 1.0f / inertiaTensor;

        B2Vec2 vB = b2Body_GetLinearVelocity(m_bodyId);
        float omegaB = b2Body_GetAngularVelocity(m_bodyId);
        B2Vec2 pB = b2Body_GetWorldCenterOfMass(m_bodyId);

        for (int i = 0; i < 2; ++i)
        {
            B2Vec2 anchorA = new B2Vec2(3.0f, 0.0f);
            B2Vec2 anchorB = b2Body_GetWorldPoint(m_bodyId, localAnchors[i]);

            B2Vec2 deltaAnchor = b2Sub(anchorB, anchorA);

            float slackLength = 1.0f;
            float length = b2Length(deltaAnchor);
            float C = length - slackLength;
            if (C < 0.0f || length < 0.001f)
            {
                m_draw.DrawSegment(anchorA, anchorB, B2HexColor.b2_colorLightCyan);
                m_impulses[i] = 0.0f;
                continue;
            }

            m_draw.DrawSegment(anchorA, anchorB, B2HexColor.b2_colorViolet);
            B2Vec2 axis = b2Normalize(deltaAnchor);

            B2Vec2 rB = b2Sub(anchorB, pB);
            float Jb = b2Cross(rB, axis);
            float K = invMass + Jb * invI * Jb;
            float invK = K < 0.0001f ? 0.0f : 1.0f / K;

            float Cdot = b2Dot(vB, axis) + Jb * omegaB;
            float impulse = -massCoefficient * invK * (Cdot + biasCoefficient * C);
            float appliedImpulse = b2ClampFloat(impulse, -maxForce * timeStep, 0.0f);

            vB = b2MulAdd(vB, invMass * appliedImpulse, axis);
            omegaB += appliedImpulse * invI * Jb;

            m_impulses[i] = appliedImpulse;
        }

        b2Body_SetLinearVelocity(m_bodyId, vB);
        b2Body_SetAngularVelocity(m_bodyId, omegaB);
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);

        float invTimeStep = settings.hertz;
        DrawTextLine($"forces = {m_impulses[0] * invTimeStep:g}, {m_impulses[1] * invTimeStep:g}");
        
    }
}