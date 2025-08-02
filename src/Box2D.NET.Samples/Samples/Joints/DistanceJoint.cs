// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2DistanceJoints;

namespace Box2D.NET.Samples.Samples.Joints;

// Test the distance joint and all options
public class DistanceJoint : Sample
{
    private static readonly int SampleDistanceJoint = SampleFactory.Shared.RegisterSample("Joints", "Distance Joint", Create);

    public const int e_maxCount = 10;

    private B2BodyId m_groundId;
    private B2BodyId[] m_bodyIds = new B2BodyId[e_maxCount];
    private B2JointId[] m_jointIds = new B2JointId[e_maxCount];
    private int m_count;
    private float m_hertz;
    private float m_dampingRatio;
    private float m_length;
    private float m_tensionForce;
    private float m_compressionForce;
    private float m_minLength;
    private float m_maxLength;
    private bool m_enableSpring;
    private bool m_enableLimit;

    private static Sample Create(SampleContext context)
    {
        return new DistanceJoint(context);
    }

    public DistanceJoint(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_camera.m_center = new B2Vec2(0.0f, 12.0f);
            m_camera.m_zoom = 25.0f * 0.35f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            m_groundId = b2CreateBody(m_worldId, ref bodyDef);
        }

        m_count = 0;
        m_hertz = 5.0f;
        m_dampingRatio = 0.5f;
        m_length = 1.0f;
        m_minLength = m_length;
        m_maxLength = m_length;
        m_tensionForce = 2000.0f;
        m_compressionForce = 100.0f;        
        m_enableSpring = false;
        m_enableLimit = false;

        for (int i = 0; i < e_maxCount; ++i)
        {
            m_bodyIds[i] = b2_nullBodyId;
            m_jointIds[i] = b2_nullJointId;
        }

        CreateScene(1);
    }

    void CreateScene(int newCount)
    {
        // Must destroy joints before bodies
        for (int i = 0; i < m_count; ++i)
        {
            b2DestroyJoint(m_jointIds[i]);
            m_jointIds[i] = b2_nullJointId;
        }

        for (int i = 0; i < m_count; ++i)
        {
            b2DestroyBody(m_bodyIds[i]);
            m_bodyIds[i] = b2_nullBodyId;
        }

        m_count = newCount;

        float radius = 0.25f;
        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), radius);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 20.0f;

        float yOffset = 20.0f;

        B2DistanceJointDef jointDef = b2DefaultDistanceJointDef();
        jointDef.hertz = m_hertz;
        jointDef.dampingRatio = m_dampingRatio;
        jointDef.length = m_length;
        jointDef.lowerSpringForce = -m_tensionForce;
        jointDef.upperSpringForce = m_compressionForce;
        jointDef.minLength = m_minLength;
        jointDef.maxLength = m_maxLength;
        jointDef.enableSpring = m_enableSpring;
        jointDef.enableLimit = m_enableLimit;

        B2BodyId prevBodyId = m_groundId;
        for (int i = 0; i < m_count; ++i)
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.angularDamping = 1.0f;
            bodyDef.position = new B2Vec2(m_length * (i + 1.0f), yOffset);
            m_bodyIds[i] = b2CreateBody(m_worldId, ref bodyDef);
            b2CreateCircleShape(m_bodyIds[i], ref shapeDef, ref circle);

            B2Vec2 pivotA = new B2Vec2(m_length * i, yOffset);
            B2Vec2 pivotB = new B2Vec2(m_length * (i + 1.0f), yOffset);
            jointDef.@base.bodyIdA = prevBodyId;
            jointDef.@base.bodyIdB = m_bodyIds[i];
            jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivotA);
            jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivotB);
            m_jointIds[i] = b2CreateDistanceJoint(m_worldId, ref jointDef);

            prevBodyId = m_bodyIds[i];
        }
    }

    public override void UpdateGui()
    {
        base.UpdateGui();

        float fontSize = ImGui.GetFontSize();
        float height = 20.0f * fontSize;
        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.m_height - height - 2.0f * fontSize), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(18.0f * fontSize, height));

        ImGui.Begin("Distance Joint", ImGuiWindowFlags.NoResize);
        ImGui.PushItemWidth(10.0f * fontSize);

        if (ImGui.SliderFloat("Length", ref m_length, 0.1f, 4.0f, "%3.1f"))
        {
            for (int i = 0; i < m_count; ++i)
            {
                b2DistanceJoint_SetLength(m_jointIds[i], m_length);
                b2Joint_WakeBodies(m_jointIds[i]);
            }
        }

        if (ImGui.Checkbox("Spring", ref m_enableSpring))
        {
            for (int i = 0; i < m_count; ++i)
            {
                b2DistanceJoint_EnableSpring(m_jointIds[i], m_enableSpring);
                b2Joint_WakeBodies(m_jointIds[i]);
            }
        }

        if (m_enableSpring)
        {
            
            if ( ImGui.SliderFloat( "Tension", ref m_tensionForce, 0.0f, 4000.0f ) )
            {
                for ( int i = 0; i < m_count; ++i )
                {
                    b2DistanceJoint_SetSpringForceRange( m_jointIds[i], -m_tensionForce, m_compressionForce );
                    b2Joint_WakeBodies( m_jointIds[i] );
                }
            }

            if ( ImGui.SliderFloat( "Compression", ref m_compressionForce, 0.0f, 200.0f ) )
            {
                for ( int i = 0; i < m_count; ++i )
                {
                    b2DistanceJoint_SetSpringForceRange( m_jointIds[i], -m_tensionForce, m_compressionForce );
                    b2Joint_WakeBodies( m_jointIds[i] );
                }
            }
            
            if (ImGui.SliderFloat("Hertz", ref m_hertz, 0.0f, 15.0f, "%3.1f"))
            {
                for (int i = 0; i < m_count; ++i)
                {
                    b2DistanceJoint_SetSpringHertz(m_jointIds[i], m_hertz);
                    b2Joint_WakeBodies(m_jointIds[i]);
                }
            }

            if (ImGui.SliderFloat("Damping", ref m_dampingRatio, 0.0f, 4.0f, "%3.1f"))
            {
                for (int i = 0; i < m_count; ++i)
                {
                    b2DistanceJoint_SetSpringDampingRatio(m_jointIds[i], m_dampingRatio);
                    b2Joint_WakeBodies(m_jointIds[i]);
                }
            }
        }

        if (ImGui.Checkbox("Limit", ref m_enableLimit))
        {
            for (int i = 0; i < m_count; ++i)
            {
                b2DistanceJoint_EnableLimit(m_jointIds[i], m_enableLimit);
                b2Joint_WakeBodies(m_jointIds[i]);
            }
        }

        if (m_enableLimit)
        {
            if (ImGui.SliderFloat("Min Length", ref m_minLength, 0.1f, 4.0f, "%3.1f"))
            {
                for (int i = 0; i < m_count; ++i)
                {
                    b2DistanceJoint_SetLengthRange(m_jointIds[i], m_minLength, m_maxLength);
                    b2Joint_WakeBodies(m_jointIds[i]);
                }
            }

            if (ImGui.SliderFloat("Max Length", ref m_maxLength, 0.1f, 4.0f, "%3.1f"))
            {
                for (int i = 0; i < m_count; ++i)
                {
                    b2DistanceJoint_SetLengthRange(m_jointIds[i], m_minLength, m_maxLength);
                    b2Joint_WakeBodies(m_jointIds[i]);
                }
            }
        }

        int count = m_count;
        if (ImGui.SliderInt("Count", ref count, 1, e_maxCount))
        {
            CreateScene(count);
        }

        ImGui.PopItemWidth();
        ImGui.End();
    }
}