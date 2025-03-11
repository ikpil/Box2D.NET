// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Box2D.NET.Samples.Extensions;
using Box2D.NET.Samples.Primitives;
using ImGuiNET;
using Silk.NET.GLFW;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.Shared.RandomSupports;

namespace Box2D.NET.Samples.Samples.Events;

public class ContactEvent : Sample
{
    private static readonly int SampleContact = SampleFactory.Shared.RegisterSample("Events", "Contact", Create);


    public const int e_count = 20;

    private B2BodyId m_playerId;
    private B2ShapeId m_coreShapeId;
    private B2BodyId[] m_debrisIds = new B2BodyId[e_count];
    private BodyUserData<int>[] m_bodyUserData = new BodyUserData<int>[e_count];
    private float m_force;
    private float m_wait;

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new ContactEvent(ctx, settings);
    }


    public ContactEvent(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 0.0f);
            m_context.camera.m_zoom = 25.0f * 1.75f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2Vec2[] points = new B2Vec2[] { new B2Vec2(40.0f, -40.0f), new B2Vec2(-40.0f, -40.0f), new B2Vec2(-40.0f, 40.0f), new B2Vec2(40.0f, 40.0f) };

            B2ChainDef chainDef = b2DefaultChainDef();
            chainDef.count = 4;
            chainDef.points = points;
            chainDef.isLoop = true;

            b2CreateChain(groundId, ref chainDef);
        }

        // Player
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.gravityScale = 0.0f;
            bodyDef.linearDamping = 0.5f;
            bodyDef.angularDamping = 0.5f;
            bodyDef.isBullet = true;
            m_playerId = b2CreateBody(m_worldId, ref bodyDef);

            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 1.0f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();

            // Enable contact events for the player shape
            shapeDef.enableContactEvents = true;

            m_coreShapeId = b2CreateCircleShape(m_playerId, ref shapeDef, ref circle);
        }

        for (int i = 0; i < e_count; ++i)
        {
            m_debrisIds[i] = b2_nullBodyId;
            m_bodyUserData[i] = BodyUserData.Create(i);
        }

        m_wait = 0.5f;
        m_force = 200.0f;
    }

    void SpawnDebris()
    {
        int index = -1;
        for (int i = 0; i < e_count; ++i)
        {
            if (B2_IS_NULL(m_debrisIds[i]))
            {
                index = i;
                break;
            }
        }

        if (index == -1)
        {
            return;
        }

        // Debris
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Vec2(RandomFloatRange(-38.0f, 38.0f), RandomFloatRange(-38.0f, 38.0f));
        bodyDef.rotation = b2MakeRot(RandomFloatRange(-B2_PI, B2_PI));
        bodyDef.linearVelocity = new B2Vec2(RandomFloatRange(-5.0f, 5.0f), RandomFloatRange(-5.0f, 5.0f));
        bodyDef.angularVelocity = RandomFloatRange(-1.0f, 1.0f);
        bodyDef.gravityScale = 0.0f;
        bodyDef.userData = m_bodyUserData[index];
        m_debrisIds[index] = b2CreateBody(m_worldId, ref bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.restitution = 0.8f;

        // No events when debris hits debris
        shapeDef.enableContactEvents = false;

        if ((index + 1) % 3 == 0)
        {
            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
            b2CreateCircleShape(m_debrisIds[index], ref shapeDef, ref circle);
        }
        else if ((index + 1) % 2 == 0)
        {
            B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.25f), new B2Vec2(0.0f, 0.25f), 0.25f);
            b2CreateCapsuleShape(m_debrisIds[index], ref shapeDef, ref capsule);
        }
        else
        {
            B2Polygon box = b2MakeBox(0.4f, 0.6f);
            b2CreatePolygonShape(m_debrisIds[index], ref shapeDef, ref box);
        }
    }

    public override void UpdateUI()
    {
        
        float height = 60.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_context.camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Contact Event", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

        ImGui.SliderFloat("force", ref m_force, 100.0f, 500.0f, "%.1f");

        ImGui.End();
    }

    public override void Step(Settings settings)
    {
        m_context.draw.DrawString(5, m_textLine, "move using WASD");
        m_textLine += m_textIncrement;

        B2Vec2 position = b2Body_GetPosition(m_playerId);

        if (GetKey(Keys.A) == InputAction.Press)
        {
            b2Body_ApplyForce(m_playerId, new B2Vec2(-m_force, 0.0f), position, true);
        }

        if (GetKey(Keys.D) == InputAction.Press)
        {
            b2Body_ApplyForce(m_playerId, new B2Vec2(m_force, 0.0f), position, true);
        }

        if (GetKey(Keys.W) == InputAction.Press)
        {
            b2Body_ApplyForce(m_playerId, new B2Vec2(0.0f, m_force), position, true);
        }

        if (GetKey(Keys.S) == InputAction.Press)
        {
            b2Body_ApplyForce(m_playerId, new B2Vec2(0.0f, -m_force), position, true);
        }

        base.Step(settings);

        // Discover rings that touch the bottom sensor
        int[] debrisToAttach = new int[e_count];
        B2ShapeId[] shapesToDestroy = new B2ShapeId[e_count];
        int attachCount = 0;
        int destroyCount = 0;

        List<B2ContactData> contactData = new List<B2ContactData>();

        // Process contact begin touch events.
        B2ContactEvents contactEvents = b2World_GetContactEvents(m_worldId);
        for (int i = 0; i < contactEvents.beginCount; ++i)
        {
            B2ContactBeginTouchEvent @event = contactEvents.beginEvents[i];
            B2BodyId bodyIdA = b2Shape_GetBody(@event.shapeIdA);
            B2BodyId bodyIdB = b2Shape_GetBody(@event.shapeIdB);

            // The begin touch events have the contact manifolds, but the impulses are zero. This is because the manifolds
            // are gathered before the contact solver is run.

            // We can get the final contact data from the shapes. The manifold is shared by the two shapes, so we just need the
            // contact data from one of the shapes. Choose the one with the smallest number of contacts.

            int capacityA = b2Shape_GetContactCapacity(@event.shapeIdA);
            int capacityB = b2Shape_GetContactCapacity(@event.shapeIdB);

            if (capacityA < capacityB)
            {
                contactData.Resize(capacityA);

                // The count may be less than the capacity
                int countA = b2Shape_GetContactData(@event.shapeIdA, CollectionsMarshal.AsSpan(contactData), capacityA);
                Debug.Assert(countA >= 1);

                for (int j = 0; j < countA; ++j)
                {
                    B2ShapeId idA = contactData[j].shapeIdA;
                    B2ShapeId idB = contactData[j].shapeIdB;
                    if (B2_ID_EQUALS(idA, @event.shapeIdB) || B2_ID_EQUALS(idB, @event.shapeIdB))
                    {
                        Debug.Assert(B2_ID_EQUALS(idA, @event.shapeIdA) || B2_ID_EQUALS(idB, @event.shapeIdA));

                        B2Manifold manifold = contactData[j].manifold;
                        B2Vec2 normal = manifold.normal;
                        Debug.Assert(b2AbsFloat(b2Length(normal) - 1.0f) < 4.0f * FLT_EPSILON);

                        for (int k = 0; k < manifold.pointCount; ++k)
                        {
                            B2ManifoldPoint point = manifold.points[k];
                            m_context.draw.DrawSegment(point.point, point.point + point.maxNormalImpulse * normal, B2HexColor.b2_colorBlueViolet);
                            m_context.draw.DrawPoint(point.point, 10.0f, B2HexColor.b2_colorWhite);
                        }
                    }
                }
            }
            else
            {
                contactData.Resize(capacityB);

                // The count may be less than the capacity
                int countB = b2Shape_GetContactData(@event.shapeIdB, CollectionsMarshal.AsSpan(contactData), capacityB);
                Debug.Assert(countB >= 1);

                for (int j = 0; j < countB; ++j)
                {
                    B2ShapeId idA = contactData[j].shapeIdA;
                    B2ShapeId idB = contactData[j].shapeIdB;

                    if (B2_ID_EQUALS(idA, @event.shapeIdA) || B2_ID_EQUALS(idB, @event.shapeIdA))
                    {
                        Debug.Assert(B2_ID_EQUALS(idA, @event.shapeIdB) || B2_ID_EQUALS(idB, @event.shapeIdB));

                        B2Manifold manifold = contactData[j].manifold;
                        B2Vec2 normal = manifold.normal;
                        Debug.Assert(b2AbsFloat(b2Length(normal) - 1.0f) < 4.0f * FLT_EPSILON);

                        for (int k = 0; k < manifold.pointCount; ++k)
                        {
                            B2ManifoldPoint point = manifold.points[k];
                            m_context.draw.DrawSegment(point.point, point.point + point.maxNormalImpulse * normal, B2HexColor.b2_colorYellowGreen);
                            m_context.draw.DrawPoint(point.point, 10.0f, B2HexColor.b2_colorWhite);
                        }
                    }
                }
            }

            if (B2_ID_EQUALS(bodyIdA, m_playerId))
            {
                BodyUserData<int> userDataB = b2Body_GetUserData(bodyIdB) as BodyUserData<int>;
                if (userDataB == null)
                {
                    if (B2_ID_EQUALS(@event.shapeIdA, m_coreShapeId) == false && destroyCount < e_count)
                    {
                        // player non-core shape hit the wall

                        bool found = false;
                        for (int j = 0; j < destroyCount; ++j)
                        {
                            if (B2_ID_EQUALS(@event.shapeIdA, shapesToDestroy[j]))
                            {
                                found = true;
                                break;
                            }
                        }

                        // avoid double deletion
                        if (found == false)
                        {
                            shapesToDestroy[destroyCount] = @event.shapeIdA;
                            destroyCount += 1;
                        }
                    }
                }
                else if (attachCount < e_count)
                {
                    debrisToAttach[attachCount] = userDataB.Value;
                    attachCount += 1;
                }
            }
            else
            {
                // Only expect events for the player
                Debug.Assert(B2_ID_EQUALS(bodyIdB, m_playerId));
                BodyUserData<int> userDataA = b2Body_GetUserData(bodyIdA) as BodyUserData<int>;
                if (userDataA == null)
                {
                    if (B2_ID_EQUALS(@event.shapeIdB, m_coreShapeId) == false && destroyCount < e_count)
                    {
                        // player non-core shape hit the wall

                        bool found = false;
                        for (int j = 0; j < destroyCount; ++j)
                        {
                            if (B2_ID_EQUALS(@event.shapeIdB, shapesToDestroy[j]))
                            {
                                found = true;
                                break;
                            }
                        }

                        // avoid double deletion
                        if (found == false)
                        {
                            shapesToDestroy[destroyCount] = @event.shapeIdB;
                            destroyCount += 1;
                        }
                    }
                }
                else if (attachCount < e_count)
                {
                    debrisToAttach[attachCount] = userDataA.Value;
                    attachCount += 1;
                }
            }
        }

        // Attach debris to player body
        for (int i = 0; i < attachCount; ++i)
        {
            int index = debrisToAttach[i];
            B2BodyId debrisId = m_debrisIds[index];
            if (B2_IS_NULL(debrisId))
            {
                continue;
            }

            B2Transform playerTransform = b2Body_GetTransform(m_playerId);
            B2Transform debrisTransform = b2Body_GetTransform(debrisId);
            B2Transform relativeTransform = b2InvMulTransforms(playerTransform, debrisTransform);

            int shapeCount = b2Body_GetShapeCount(debrisId);
            if (shapeCount == 0)
            {
                continue;
            }

            B2ShapeId[] shapeId = new B2ShapeId[1];
            b2Body_GetShapes(debrisId, shapeId, 1);

            B2ShapeType type = b2Shape_GetType(shapeId[0]);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.enableContactEvents = true;

            switch (type)
            {
                case B2ShapeType.b2_circleShape:
                {
                    B2Circle circle = b2Shape_GetCircle(shapeId[0]);
                    circle.center = b2TransformPoint(ref relativeTransform, circle.center);

                    b2CreateCircleShape(m_playerId, ref shapeDef, ref circle);
                }
                    break;

                case B2ShapeType.b2_capsuleShape:
                {
                    B2Capsule capsule = b2Shape_GetCapsule(shapeId[0]);
                    capsule.center1 = b2TransformPoint(ref relativeTransform, capsule.center1);
                    capsule.center2 = b2TransformPoint(ref relativeTransform, capsule.center2);

                    b2CreateCapsuleShape(m_playerId, ref shapeDef, ref capsule);
                }
                    break;

                case B2ShapeType.b2_polygonShape:
                {
                    B2Polygon originalPolygon = b2Shape_GetPolygon(shapeId[0]);
                    B2Polygon polygon = b2TransformPolygon(relativeTransform, originalPolygon);

                    b2CreatePolygonShape(m_playerId, ref shapeDef, ref polygon);
                }
                    break;

                default:
                    Debug.Assert(false);
                    break;
            }

            b2DestroyBody(debrisId);
            m_debrisIds[index] = b2_nullBodyId;
        }

        for (int i = 0; i < destroyCount; ++i)
        {
            bool updateMass = false;
            b2DestroyShape(shapesToDestroy[i], updateMass);
        }

        if (destroyCount > 0)
        {
            // Update mass just once
            b2Body_ApplyMassFromShapes(m_playerId);
        }

        if (settings.hertz > 0.0f && settings.pause == false)
        {
            m_wait -= 1.0f / settings.hertz;
            if (m_wait < 0.0f)
            {
                SpawnDebris();
                m_wait += 0.5f;
            }
        }
    }
}