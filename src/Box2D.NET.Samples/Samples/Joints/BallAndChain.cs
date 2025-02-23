using System.Diagnostics;
using System.Numerics;
using Box2D.NET.Primitives;
using Box2D.NET.Samples;
using ImGuiNET;
using static Box2D.NET.joint;
using static Box2D.NET.id;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.revolute_joint;

namespace Box2D.NET.Samples.Samples.Joints;

public class BallAndChain : Sample
{
    public const int e_count = 30;

    b2JointId[] m_jointIds = new b2JointId[e_count + 1];
    float m_frictionTorque;

    static int sampleBallAndChainIndex = RegisterSample("Joints", "Ball & Chain", Create);

    static Sample Create(Settings settings)
    {
        return new BallAndChain(settings);
    }

    public BallAndChain(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(0.0f, -8.0f);
            Draw.g_camera.m_zoom = 27.5f;
        }

        b2BodyId groundId = b2_nullBodyId;
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, &bodyDef);
        }

        m_frictionTorque = 100.0f;

        {
            float hx = 0.5f;
            b2Capsule capsule = new b2Capsule(new b2Vec2(-hx, 0.0f), new b2Vec2(hx, 0.0f), 0.125f);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 20.0f;

            b2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();

            int jointIndex = 0;

            b2BodyId prevBodyId = groundId;
            for (int i = 0; i < e_count; ++i)
            {
                b2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = b2BodyType.b2_dynamicBody;
                bodyDef.position = new b2Vec2((1.0f + 2.0f * i) * hx, e_count * hx);
                b2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
                b2CreateCapsuleShape(bodyId, shapeDef, capsule);

                b2Vec2 pivot = new b2Vec2((2.0f * i) * hx, e_count * hx);
                jointDef.bodyIdA = prevBodyId;
                jointDef.bodyIdB = bodyId;
                jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
                jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
                // jointDef.enableMotor = true;
                jointDef.maxMotorTorque = m_frictionTorque;
                m_jointIds[jointIndex++] = b2CreateRevoluteJoint(m_worldId, jointDef);

                prevBodyId = bodyId;
            }

            {
                b2Circle circle = new b2Circle(new b2Vec2(0.0f, 0.0f), 4.0f);

                b2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = b2BodyType.b2_dynamicBody;
                bodyDef.position = new b2Vec2((1.0f + 2.0f * e_count) * hx + circle.radius - hx, e_count * hx);

                b2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
                b2CreateCircleShape(bodyId, shapeDef, circle);

                b2Vec2 pivot = new b2Vec2((2.0f * e_count) * hx, e_count * hx);
                jointDef.bodyIdA = prevBodyId;
                jointDef.bodyIdB = bodyId;
                jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
                jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
                jointDef.enableMotor = true;
                jointDef.maxMotorTorque = m_frictionTorque;
                m_jointIds[jointIndex++] = b2CreateRevoluteJoint(m_worldId, jointDef);
                Debug.Assert(jointIndex == e_count + 1);
            }
        }
    }

    public override void UpdateUI()
    {
        bool open = false;
        float height = 60.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, Draw.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Ball and Chain", ref open, ImGuiWindowFlags.NoResize);

        bool updateFriction = ImGui.SliderFloat("Joint Friction", ref m_frictionTorque, 0.0f, 1000.0f, "%2.f");
        if (updateFriction)
        {
            for (int i = 0; i <= e_count; ++i)
            {
                b2RevoluteJoint_SetMaxMotorTorque(m_jointIds[i], m_frictionTorque);
            }
        }

        ImGui.End();
    }
}