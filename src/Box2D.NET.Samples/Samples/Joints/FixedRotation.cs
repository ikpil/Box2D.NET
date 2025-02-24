using System.Diagnostics;
using System.Numerics;
using Box2D.NET.Primitives;
using Box2D.NET.Samples;
using ImGuiNET;
using static Box2D.NET.joint;
using static Box2D.NET.id;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Joints;

// This test ensures joints work correctly with bodies that have fixed rotation
public class FixedRotation : Sample
{
    public const int e_count = 6;

    b2BodyId m_groundId;
    b2BodyId[] m_bodyIds = new b2BodyId[e_count];
    b2JointId[] m_jointIds = new b2JointId[e_count];
    bool m_fixedRotation;

    static int sampleFixedRotation = RegisterSample("Joints", "Fixed Rotation", Create);

    static Sample Create(Settings settings)
    {
        return new FixedRotation(settings);
    }

    public FixedRotation(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(0.0f, 8.0f);
            Draw.g_camera.m_zoom = 25.0f * 0.7f;
        }

        b2BodyDef bodyDef = b2DefaultBodyDef();
        m_groundId = b2CreateBody(m_worldId, bodyDef);
        m_fixedRotation = true;

        for (int i = 0; i < e_count; ++i)
        {
            m_bodyIds[i] = b2_nullBodyId;
            m_jointIds[i] = b2_nullJointId;
        }

        CreateScene();
    }

    void CreateScene()
    {
        for (int i = 0; i < e_count; ++i)
        {
            if (B2_IS_NON_NULL(m_jointIds[i]))
            {
                b2DestroyJoint(m_jointIds[i]);
                m_jointIds[i] = b2_nullJointId;
            }

            if (B2_IS_NON_NULL(m_bodyIds[i]))
            {
                b2DestroyBody(m_bodyIds[i]);
                m_bodyIds[i] = b2_nullBodyId;
            }
        }

        b2Vec2 position = new b2Vec2(-12.5f, 10.0f);
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.fixedRotation = m_fixedRotation;

        b2Polygon box = b2MakeBox(1.0f, 1.0f);

        int index = 0;

        // distance joint
        {
            Debug.Assert(index < e_count);

            bodyDef.position = position;
            m_bodyIds[index] = b2CreateBody(m_worldId, bodyDef);
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(m_bodyIds[index], shapeDef, box);

            float length = 2.0f;
            b2Vec2 pivot1 = new b2Vec2(position.x, position.y + 1.0f + length);
            b2Vec2 pivot2 = new b2Vec2(position.x, position.y + 1.0f);
            b2DistanceJointDef jointDef = b2DefaultDistanceJointDef();
            jointDef.bodyIdA = m_groundId;
            jointDef.bodyIdB = m_bodyIds[index];
            jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot1);
            jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot2);
            jointDef.length = length;
            m_jointIds[index] = b2CreateDistanceJoint(m_worldId, jointDef);
        }

        position.x += 5.0f;
        ++index;

        // motor joint
        {
            Debug.Assert(index < e_count);

            bodyDef.position = position;
            m_bodyIds[index] = b2CreateBody(m_worldId, bodyDef);
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(m_bodyIds[index], shapeDef, box);

            b2MotorJointDef jointDef = b2DefaultMotorJointDef();
            jointDef.bodyIdA = m_groundId;
            jointDef.bodyIdB = m_bodyIds[index];
            jointDef.linearOffset = position;
            jointDef.maxForce = 200.0f;
            jointDef.maxTorque = 20.0f;
            m_jointIds[index] = b2CreateMotorJoint(m_worldId, jointDef);
        }

        position.x += 5.0f;
        ++index;

        // prismatic joint
        {
            Debug.Assert(index < e_count);

            bodyDef.position = position;
            m_bodyIds[index] = b2CreateBody(m_worldId, bodyDef);
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(m_bodyIds[index], shapeDef, box);

            b2Vec2 pivot = new b2Vec2(position.x - 1.0f, position.y);
            b2PrismaticJointDef jointDef = b2DefaultPrismaticJointDef();
            jointDef.bodyIdA = m_groundId;
            jointDef.bodyIdB = m_bodyIds[index];
            jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
            jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
            jointDef.localAxisA = b2Body_GetLocalVector(jointDef.bodyIdA, new b2Vec2(1.0f, 0.0f));
            m_jointIds[index] = b2CreatePrismaticJoint(m_worldId, jointDef);
        }

        position.x += 5.0f;
        ++index;

        // revolute joint
        {
            Debug.Assert(index < e_count);

            bodyDef.position = position;
            m_bodyIds[index] = b2CreateBody(m_worldId, bodyDef);
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(m_bodyIds[index], shapeDef, box);

            b2Vec2 pivot = new b2Vec2(position.x - 1.0f, position.y);
            b2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
            jointDef.bodyIdA = m_groundId;
            jointDef.bodyIdB = m_bodyIds[index];
            jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
            jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
            m_jointIds[index] = b2CreateRevoluteJoint(m_worldId, jointDef);
        }

        position.x += 5.0f;
        ++index;

        // weld joint
        {
            Debug.Assert(index < e_count);

            bodyDef.position = position;
            m_bodyIds[index] = b2CreateBody(m_worldId, bodyDef);
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(m_bodyIds[index], shapeDef, box);

            b2Vec2 pivot = new b2Vec2(position.x - 1.0f, position.y);
            b2WeldJointDef jointDef = b2DefaultWeldJointDef();
            jointDef.bodyIdA = m_groundId;
            jointDef.bodyIdB = m_bodyIds[index];
            jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
            jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
            jointDef.angularHertz = 1.0f;
            jointDef.angularDampingRatio = 0.5f;
            jointDef.linearHertz = 1.0f;
            jointDef.linearDampingRatio = 0.5f;
            m_jointIds[index] = b2CreateWeldJoint(m_worldId, jointDef);
        }

        position.x += 5.0f;
        ++index;

        // wheel joint
        {
            Debug.Assert(index < e_count);

            bodyDef.position = position;
            m_bodyIds[index] = b2CreateBody(m_worldId, bodyDef);
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(m_bodyIds[index], shapeDef, box);

            b2Vec2 pivot = new b2Vec2(position.x - 1.0f, position.y);
            b2WheelJointDef jointDef = b2DefaultWheelJointDef();
            jointDef.bodyIdA = m_groundId;
            jointDef.bodyIdB = m_bodyIds[index];
            jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
            jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
            jointDef.localAxisA = b2Body_GetLocalVector(jointDef.bodyIdA, new b2Vec2(1.0f, 0.0f));
            jointDef.hertz = 1.0f;
            jointDef.dampingRatio = 0.7f;
            jointDef.lowerTranslation = -1.0f;
            jointDef.upperTranslation = 1.0f;
            jointDef.enableLimit = true;
            jointDef.enableMotor = true;
            jointDef.maxMotorTorque = 10.0f;
            jointDef.motorSpeed = 1.0f;
            m_jointIds[index] = b2CreateWheelJoint(m_worldId, jointDef);
        }

        position.x += 5.0f;
        ++index;
    }

    public override void UpdateUI()
    {
        bool open = false;
        float height = 60.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, Draw.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(180.0f, height));

        ImGui.Begin("Fixed Rotation", ref open, ImGuiWindowFlags.NoResize);

        if (ImGui.Checkbox("Fixed Rotation", ref m_fixedRotation))
        {
            for (int i = 0; i < e_count; ++i)
            {
                b2Body_SetFixedRotation(m_bodyIds[i], m_fixedRotation);
            }
        }

        ImGui.End();
    }
}