// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using Box2D.NET.Test.Helpers;
using NUnit.Framework;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Ids;

namespace Box2D.NET.Test;

public class B2BodiesTest
{
    [Test]
    public void Test_b2LimitVelocity()
    {
        // Create a world
        using B2TestContext context = B2TestContext.CreateFor();
        var worldId = context.WorldId;
        var world = b2GetWorldFromId(worldId);

        // Create a dynamic body
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Vec2(0.0f, 0.0f);
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);
        Assert.That(b2Body_IsValid(bodyId));

        // Add a shape to the body
        B2Polygon box = b2MakeBox(1.0f, 1.0f);
        B2ShapeDef shapeDef = b2DefaultShapeDef();
        b2CreatePolygonShape(bodyId, ref shapeDef, ref box);

        // Get the body state
        B2Body body = b2GetBodyFullId(world, bodyId);
        B2BodyState state = b2GetBodyState(world, body);
        Assert.That(state, Is.Not.Null, "Body state should not be null");

        // Test case 1: Velocity within limits - should remain unchanged
        {
            state.linearVelocity = new B2Vec2(5.0f, 0.0f);
            b2LimitVelocity(state, 10.0f);
            Assert.That(state.linearVelocity.X, Is.EqualTo(5.0f), "Linear velocity should remain unchanged when within limits");
            Assert.That(state.linearVelocity.Y, Is.EqualTo(0.0f), "Linear velocity Y component should remain unchanged");
        }

        // Test case 2: Linear velocity exceeds limit - should be capped
        {
            state.linearVelocity = new B2Vec2(15.0f, 0.0f);
            b2LimitVelocity(state, 10.0f);
            Assert.That(state.linearVelocity.X, Is.EqualTo(10.0f), "Linear velocity should be limited to maxLinearSpeed");
            Assert.That(state.linearVelocity.Y, Is.EqualTo(0.0f), "Linear velocity Y component should remain unchanged");
        }

        // Test case 3: Diagonal velocity exceeds limit - magnitude should be capped while preserving direction
        {
            state.linearVelocity = new B2Vec2(8.0f, 8.0f);
            b2LimitVelocity(state, 10.0f);
            float expectedMagnitude = 10.0f;
            float actualMagnitude = b2Length(state.linearVelocity);
            Assert.That(actualMagnitude, Is.EqualTo(expectedMagnitude).Within(0.001f), "Diagonal velocity magnitude should be limited to maxLinearSpeed");
            Assert.That(state.linearVelocity.X, Is.EqualTo(state.linearVelocity.Y), "Direction should be preserved (X and Y components should be equal)");
        }
    }

    [Test]
    public void Test_b2GetBodyFullId()
    {
        // Create a world
        using B2TestContext context = B2TestContext.CreateFor();
        var worldId = context.WorldId;
        var world = b2GetWorldFromId(worldId);

        // Create a dynamic body
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Vec2(0.0f, 0.0f);
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);
        Assert.That(b2Body_IsValid(bodyId), "Body should be valid after creation");

        // Test case 1: Get body with valid ID
        {
            B2Body body = b2GetBodyFullId(world, bodyId);
            Assert.That(body, Is.Not.Null, "Body should not be null");
            Assert.That(body.id == bodyId.index1 - 1, "Body ID should match the requested ID");
            Assert.That(body.type == B2BodyType.b2_dynamicBody, "Body type should match the created type");
        }

        // Test case 2: Get body after destruction
        {
            b2DestroyBody(bodyId);
#if DEBUG
            Assert.Throws<InvalidOperationException>(() => b2GetBodyFullId(world, bodyId), "Destroyed body access should throw.");
#endif
        }

        // Test case 3: Get body with invalid ID
        {
            B2BodyId invalidId = b2_nullBodyId;
#if DEBUG
            Assert.Throws<InvalidOperationException>(() => b2GetBodyFullId(world, invalidId), "invalid body access should throw.");
#endif
        }
    }

    [Test]
    public void Test_b2GetBodyTransformQuick()
    {
        // Create a world
        using B2TestContext context = B2TestContext.CreateFor();
        var worldId = context.WorldId;
        var world = b2GetWorldFromId(worldId);

        // Create a dynamic body with initial position
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Vec2(1.0f, 2.0f);
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);
        B2Body body = b2GetBodyFullId(world, bodyId);

        // Test case 1: Get transform for a valid body and verify initial values
        {
            B2Transform transform = b2GetBodyTransformQuick(world, body);
            B2Transform transform2 = b2GetBodyTransform(world, body.id);

            //
            Assert.That(transform.p.X, Is.EqualTo(1.0f), "Initial position X should match body definition");
            Assert.That(transform.p.Y, Is.EqualTo(2.0f), "Initial position Y should match body definition");
            Assert.That(transform.q.c, Is.EqualTo(1.0f), "Initial rotation cosine should be 1.0 (no rotation)");
            Assert.That(transform.q.s, Is.EqualTo(0.0f), "Initial rotation sine should be 0.0 (no rotation)");

            //
            Assert.That(transform.p.X, Is.EqualTo(transform2.p.X), "Initial position X should match body definition");
            Assert.That(transform.p.Y, Is.EqualTo(transform2.p.Y), "Initial position Y should match body definition");
            Assert.That(transform.q.c, Is.EqualTo(transform2.q.c), "Initial rotation cosine should be 1.0 (no rotation)");
            Assert.That(transform.q.s, Is.EqualTo(transform2.q.s), "Initial rotation sine should be 0.0 (no rotation)");
        }

        // Test case 2: Get transform after body destruction
        {
            b2DestroyBody(bodyId);
            Assert.Throws<IndexOutOfRangeException>(() => b2GetBodyTransformQuick(world, body), "Getting transform of destroyed body should throw IndexOutOfRangeException");
        }
    }

    [Test]
    public void Test_b2MakeBodyId()
    {
        // Create a world
        using B2TestContext context = B2TestContext.CreateFor();
        var worldId = context.WorldId;
        var world = b2GetWorldFromId(worldId);

        // Create a dynamic body
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        B2BodyId createdBodyId = b2CreateBody(worldId, bodyDef);
        Assert.That(b2Body_IsValid(createdBodyId), "Body should be valid after creation");

        // Get the body
        B2Body body = b2GetBodyFullId(world, createdBodyId);
        Assert.That(body, Is.Not.Null, "Body should not be null");

        // Test case 1: Create body ID from valid body
        {
            B2BodyId bodyId = b2MakeBodyId(world, body.id);
            Assert.That(bodyId.index1, Is.EqualTo(body.id + 1), "Index should be body.id + 1");
            Assert.That(bodyId.world0, Is.EqualTo(world.worldId), "World ID should match");
            Assert.That(bodyId.generation, Is.EqualTo(body.generation), "Generation should match body's generation");
        }

        // Test case 2: Create body ID with invalid index
        {
            Assert.Throws<IndexOutOfRangeException>(() => b2MakeBodyId(world, -1), "Should throw IndexOutOfRangeException for negative index");
            Assert.Throws<IndexOutOfRangeException>(() => b2MakeBodyId(world, world.bodies.count), "Should throw IndexOutOfRangeException for index beyond array bounds");
        }
    }

    [Test]
    public void Test_b2GetBodySim()
    {
        // Create a world
        using B2TestContext context = B2TestContext.CreateFor();
        var worldId = context.WorldId;
        var world = b2GetWorldFromId(worldId);

        // Create a dynamic body with initial position and velocity
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Vec2(1.0f, 2.0f);
        bodyDef.linearVelocity = new B2Vec2(3.0f, 4.0f);
        bodyDef.angularVelocity = 5.0f;
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);
        Assert.That(b2Body_IsValid(bodyId), "Body should be valid after creation");

        // Get the body
        B2Body body = b2GetBodyFullId(world, bodyId);
        Assert.That(body, Is.Not.Null, "Body should not be null");

        // Test case 1: Get simulation state for valid body
        {
            B2BodySim sim = b2GetBodySim(world, body);
            Assert.That(sim, Is.Not.Null, "Simulation state should not be null");
            Assert.That(sim.transform.p.X, Is.EqualTo(1.0f), "Position X should match initial value");
            Assert.That(sim.transform.p.Y, Is.EqualTo(2.0f), "Position Y should match initial value");
            Assert.That(sim.center.X, Is.EqualTo(1.0f), "Center X should match initial position");
            Assert.That(sim.center.Y, Is.EqualTo(2.0f), "Center Y should match initial position");
            Assert.That(sim.bodyId, Is.EqualTo(body.id), "Body ID should match");
            Assert.That(sim.invMass, Is.EqualTo(0.0f), "Inverse mass should be 0 for new body");
            Assert.That(sim.invInertia, Is.EqualTo(0.0f), "Inverse inertia should be 0 for new body");
        }

        // Test case 2: Get simulation state after body destruction
        {
            b2DestroyBody(bodyId);
            Assert.Throws<IndexOutOfRangeException>(() => b2GetBodySim(world, body), "Getting simulation state of destroyed body should throw IndexOutOfRangeException");
        }
    }

    [Test]
    public void Test_b2GetBodyState()
    {
        // Create a world
        using B2TestContext context = B2TestContext.CreateFor();
        var worldId = context.WorldId;
        var world = b2GetWorldFromId(worldId);

        // Create a dynamic body with initial position and velocity
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Vec2(1.0f, 2.0f);
        bodyDef.linearVelocity = new B2Vec2(3.0f, 4.0f);
        bodyDef.angularVelocity = 5.0f;
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);
        Assert.That(b2Body_IsValid(bodyId), "Body should be valid after creation");

        // Get the body
        B2Body body = b2GetBodyFullId(world, bodyId);
        Assert.That(body, Is.Not.Null, "Body should not be null");

        {
            B2BodyState state = b2GetBodyState(world, body);
            Assert.That(state, Is.Not.Null, "Body state should not be null for awake body");
            
            b2Body_SetAwake(bodyId, false);
            state = b2GetBodyState(world, body);
            Assert.That(state, Is.Null, "Body state should be null for sleeping body");

            // Wake up the body
            b2Body_SetAwake(bodyId, true);
            state = b2GetBodyState(world, body);
            Assert.That(state, Is.Not.Null, "Body state should not be null after waking up");
        }
    }
}