// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using ImGuiNET;
using Silk.NET.GLFW;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET.Samples.Samples.Events;

// Shows how to make a rigid body character mover and use the pre-solve callback. In this
// case the platform should get the pre-solve event, not the player.
public class Platform : Sample
{
    private static readonly int SamplePlatformer = SampleFactory.Shared.RegisterSample("Events", "Platformer", Create);

    private bool m_jumping;
    private float m_radius;
    private float m_force;
    private float m_impulse;
    private float m_jumpDelay;
    private B2BodyId m_playerId;
    private B2ShapeId m_playerShapeId;
    private B2BodyId m_movingPlatformId;

    //
    private bool m_canJump;

    private static Sample Create(SampleContext context)
    {
        return new Platform(context);
    }

    public Platform(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.center = new B2Vec2(0.5f, 7.5f);
            m_camera.zoom = 25.0f * 0.4f;
        }

        b2World_SetPreSolveCallback(m_worldId, PreSolveStatic, this);

        // Ground
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new B2Vec2(-20.0f, 0.0f), new B2Vec2(20.0f, 0.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        // Static Platform
        // This tests pre-solve with continuous collision
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_staticBody;
            bodyDef.position = new B2Vec2(-6.0f, 6.0f);
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();

            // Need to turn this on to get the callback
            shapeDef.enablePreSolveEvents = true;

            B2Polygon box = b2MakeBox(2.0f, 0.5f);
            b2CreatePolygonShape(bodyId, shapeDef, box);
        }

        // Moving Platform
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_kinematicBody;
            bodyDef.position = new B2Vec2(0.0f, 6.0f);
            bodyDef.linearVelocity = new B2Vec2(2.0f, 0.0f);
            m_movingPlatformId = b2CreateBody(m_worldId, bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();

            // Need to turn this on to get the callback
            shapeDef.enablePreSolveEvents = true;

            B2Polygon box = b2MakeBox(3.0f, 0.5f);
            b2CreatePolygonShape(m_movingPlatformId, shapeDef, box);
        }

        // Player
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.motionLocks.angularZ = true;
            bodyDef.linearDamping = 0.5f;
            bodyDef.position = new B2Vec2(0.0f, 1.0f);
            m_playerId = b2CreateBody(m_worldId, bodyDef);

            m_radius = 0.5f;
            B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, 0.0f), new B2Vec2(0.0f, 1.0f), m_radius);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.material.friction = 0.1f;

            m_playerShapeId = b2CreateCapsuleShape(m_playerId, shapeDef, capsule);
        }

        m_force = 25.0f;
        m_impulse = 25.0f;
        m_jumpDelay = 0.25f;
        m_jumping = false;
    }

    private static bool PreSolveStatic(B2ShapeId shapeIdA, B2ShapeId shapeIdB, B2Vec2 point, B2Vec2 normal, object context)
    {
        Platform self = context as Platform;
        return self.PreSolve(shapeIdA, shapeIdB, point, normal);
    }

    // This callback must be thread-safe. It may be called multiple times simultaneously.
    // Notice how this method is constant and doesn't change any data. It also
    // does not try to access any values in the world that may be changing, such as contact data.
    public bool PreSolve(B2ShapeId shapeIdA, B2ShapeId shapeIdB, B2Vec2 point, B2Vec2 normal)
    {
        B2_ASSERT(b2Shape_IsValid(shapeIdA));
        B2_ASSERT(b2Shape_IsValid(shapeIdB));

        float sign = 0.0f;
        if (B2_ID_EQUALS(shapeIdA, m_playerShapeId))
        {
            sign = -1.0f;
        }
        else if (B2_ID_EQUALS(shapeIdB, m_playerShapeId))
        {
            sign = 1.0f;
        }
        else
        {
            // not colliding with the player, enable contact
            return true;
        }

        if (sign * normal.Y > 0.95f)
        {
            return true;
        }

        // normal points down, disable contact
        return false;
    }

    public override void UpdateGui()
    {
        base.UpdateGui();

        float fontSize = ImGui.GetFontSize();
        float height = 100.0f;
        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.height - height - 2.0f * fontSize), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("One-Sided Platform", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

        ImGui.SliderFloat("force", ref m_force, 0.0f, 50.0f, "%.1f");
        ImGui.SliderFloat("impulse", ref m_impulse, 0.0f, 50.0f, "%.1f");

        ImGui.End();
    }

    public override void Step()
    {
        m_canJump = false;
        B2Vec2 velocity = b2Body_GetLinearVelocity(m_playerId);
        if (m_jumpDelay == 0.0f && m_jumping == false && velocity.Y < 0.01f)
        {
            int capacity = b2Body_GetContactCapacity(m_playerId);
            capacity = b2MinInt(capacity, 4);
            Span<B2ContactData> contactData = stackalloc B2ContactData[capacity];
            int count = b2Body_GetContactData(m_playerId, contactData, capacity);
            for (int i = 0; i < count; ++i)
            {
                B2BodyId bodyIdA = b2Shape_GetBody(contactData[i].shapeIdA);
                float sign = 0.0f;
                if (B2_ID_EQUALS(bodyIdA, m_playerId))
                {
                    // normal points from A to B
                    sign = -1.0f;
                }
                else
                {
                    sign = 1.0f;
                }

                if (sign * contactData[i].manifold.normal.Y > 0.9f)
                {
                    m_canJump = true;
                    break;
                }
            }
        }

        // A kinematic body is moved by setting its velocity. This
        // ensure friction works correctly.
        B2Vec2 platformPosition = b2Body_GetPosition(m_movingPlatformId);
        if (platformPosition.X < -15.0f)
        {
            b2Body_SetLinearVelocity(m_movingPlatformId, new B2Vec2(2.0f, 0.0f));
        }
        else if (platformPosition.X > 15.0f)
        {
            b2Body_SetLinearVelocity(m_movingPlatformId, new B2Vec2(-2.0f, 0.0f));
        }

        if (GetKey(Keys.A) == InputAction.Press)
        {
            b2Body_ApplyForceToCenter(m_playerId, new B2Vec2(-m_force, 0.0f), true);
        }

        if (GetKey(Keys.D) == InputAction.Press)
        {
            b2Body_ApplyForceToCenter(m_playerId, new B2Vec2(m_force, 0.0f), true);
        }

        var keyState = GetKey(Keys.Space);
        if (keyState == InputAction.Press)
        {
            if (m_canJump)
            {
                b2Body_ApplyLinearImpulseToCenter(m_playerId, new B2Vec2(0.0f, m_impulse), true);
                m_jumpDelay = 0.5f;
                m_jumping = true;
            }
        }
        else
        {
            m_jumping = false;
        }

        base.Step();


        if (m_context.hertz > 0.0f)
        {
            m_jumpDelay = b2MaxFloat(0.0f, m_jumpDelay - 1.0f / m_context.hertz);
        }
    }

    public override void Draw()
    {
        base.Draw();

        {
            Span<B2ContactData> contactData = stackalloc B2ContactData[1];
            int contactCount = b2Body_GetContactData(m_movingPlatformId, contactData, contactData.Length);
            DrawTextLine($"Platform contact count = {contactCount}, point count = {contactData[0].manifold.pointCount}");
        }


        DrawTextLine("Movement: A/D/Space");


        DrawTextLine($"Can jump = {m_canJump}");
    }
}