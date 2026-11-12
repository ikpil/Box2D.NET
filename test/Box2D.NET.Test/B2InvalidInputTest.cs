// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using NUnit.Framework;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2RevoluteJoints;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

namespace Box2D.NET.Test;

// Port of upstream test_invalid_input.c (without the record/replay subtest).
public class B2InvalidInputTest
{
    // The guard logs once per rejected call in every build config, while the assert only fires in
    // debug. So the log count is the portable signal that bad input was turned away.
    private static int _logCount;

    private static void CountingLogFcn(in string message)
    {
        _logCount += 1;
    }

    private static int SilentAssertFcn(string condition, string fileName, int lineNumber)
    {
        // Keep going so the subtest can report rather than throw
        return 0;
    }

    private static void BeginRejectCount()
    {
        _logCount = 0;
        b2SetLogCallback(CountingLogFcn);
        b2SetAssertCallback(SilentAssertFcn);
    }

    private static void EndRejectCount()
    {
        b2SetAssertCallback(b2DefaultAssertFcn);
        b2SetLogCallback(b2DefaultLogFcn);
    }

    private static void Rejects(Action call, string name)
    {
        int countBefore = _logCount;
        call();
        Assert.That(_logCount, Is.EqualTo(countBefore + 1), $"not rejected: {name}");
    }

    private static void Accepts(Action call, string name)
    {
        int countBefore = _logCount;
        call();
        Assert.That(_logCount, Is.EqualTo(countBefore), $"wrongly rejected: {name}");
    }

    private static readonly float[] BadValues = { float.NaN, float.PositiveInfinity, float.NegativeInfinity };

    private static bool IsBodyFinite(B2BodyId bodyId)
    {
        return b2IsValidPos(b2Body_GetPosition(bodyId)) && b2IsValidRot(b2Body_GetRotation(bodyId)) &&
               b2IsValidVec2(b2Body_GetLinearVelocity(bodyId)) && b2IsValidFloat(b2Body_GetAngularVelocity(bodyId));
    }

    [TearDown]
    public void TearDown()
    {
        // Restore the default hooks even when an assertion failed mid-test
        EndRejectCount();
    }

    [Test]
    public void WorldInputTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2Vec2 gravity = b2World_GetGravity(worldId);
        float restitutionThreshold = b2World_GetRestitutionThreshold(worldId);
        float hitEventThreshold = b2World_GetHitEventThreshold(worldId);
        float recycleDistance = b2World_GetContactRecycleDistance(worldId);
        float maximumLinearSpeed = b2World_GetMaxLinearSpeed(worldId);
        int restitutionIterations = b2World_GetRestitutionIterations(worldId);

        BeginRejectCount();

        foreach (float bad in BadValues)
        {
            Rejects(() => b2World_SetGravity(worldId, new B2Vec2(bad, 0.0f)), "b2World_SetGravity x");
            Rejects(() => b2World_SetGravity(worldId, new B2Vec2(0.0f, bad)), "b2World_SetGravity y");
            Rejects(() => b2World_SetRestitutionThreshold(worldId, bad), "b2World_SetRestitutionThreshold");
            Rejects(() => b2World_SetHitEventThreshold(worldId, bad), "b2World_SetHitEventThreshold");
            Rejects(() => b2World_SetContactRecycleDistance(worldId, bad), "b2World_SetContactRecycleDistance");
            Rejects(() => b2World_SetMaxLinearSpeed(worldId, bad), "b2World_SetMaximumLinearSpeed");
            Rejects(() => b2World_SetContactTuning(worldId, bad, 10.0f, 3.0f), "b2World_SetContactTuning hertz");
            Rejects(() => b2World_SetContactTuning(worldId, 30.0f, bad, 3.0f), "b2World_SetContactTuning damping");
            Rejects(() => b2World_SetContactTuning(worldId, 30.0f, 10.0f, bad), "b2World_SetContactTuning speed");
            Rejects(() => b2World_Step(worldId, bad, 4), "b2World_Step");
        }

        Rejects(() => b2World_SetRestitutionIterations(worldId, -1), "b2World_SetRestitutionIterations");

        Accepts(() => b2World_Step(worldId, 1.0f / 60.0f, 4), "b2World_Step");

        EndRejectCount();

        Assert.That(b2World_GetGravity(worldId).X == gravity.X && b2World_GetGravity(worldId).Y == gravity.Y);
        Assert.That(b2World_GetRestitutionThreshold(worldId), Is.EqualTo(restitutionThreshold));
        Assert.That(b2World_GetHitEventThreshold(worldId), Is.EqualTo(hitEventThreshold));
        Assert.That(b2World_GetContactRecycleDistance(worldId), Is.EqualTo(recycleDistance));
        Assert.That(b2World_GetMaxLinearSpeed(worldId), Is.EqualTo(maximumLinearSpeed));
        Assert.That(b2World_GetRestitutionIterations(worldId), Is.EqualTo(restitutionIterations));

        b2DestroyWorld(worldId);
    }

    [Test]
    public void BodyInputTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        B2Polygon box = b2MakeBox(0.5f, 0.5f);
        b2CreatePolygonShape(bodyId, shapeDef, box);

        BeginRejectCount();

        foreach (float bad in BadValues)
        {
            B2Vec2 badVec = new B2Vec2(bad, 0.0f);
            B2Pos badPos = new B2Pos(bad, 0.0f);
            B2Rot badRot = new B2Rot(bad, 0.0f);
            B2WorldTransform badTarget = new B2WorldTransform(badPos, b2Rot_identity);

            Rejects(() => b2Body_SetLinearVelocity(bodyId, badVec), "b2Body_SetLinearVelocity");
            Rejects(() => b2Body_SetAngularVelocity(bodyId, bad), "b2Body_SetAngularVelocity");
            Rejects(() => b2Body_SetTransform(bodyId, badPos, b2Rot_identity), "b2Body_SetTransform position");
            Rejects(() => b2Body_SetTransform(bodyId, b2Pos_zero, badRot), "b2Body_SetTransform rotation");
            Rejects(() => b2Body_SetTargetTransform(bodyId, badTarget, 1.0f / 60.0f, true), "b2Body_SetTargetTransform target");
            Rejects(() => b2Body_SetTargetTransform(bodyId, b2WorldTransform_identity, bad, true), "b2Body_SetTargetTransform timeStep");
            Rejects(() => b2Body_ApplyForce(bodyId, badVec, b2Pos_zero, true), "b2Body_ApplyForce force");
            Rejects(() => b2Body_ApplyForce(bodyId, b2Vec2_zero, badPos, true), "b2Body_ApplyForce point");
            Rejects(() => b2Body_ApplyForceToCenter(bodyId, badVec, true), "b2Body_ApplyForceToCenter");
            Rejects(() => b2Body_ApplyTorque(bodyId, bad, true), "b2Body_ApplyTorque");
            Rejects(() => b2Body_ApplyLinearImpulse(bodyId, badVec, b2Pos_zero, true), "b2Body_ApplyLinearImpulse impulse");
            Rejects(() => b2Body_ApplyLinearImpulse(bodyId, b2Vec2_zero, badPos, true), "b2Body_ApplyLinearImpulse point");
            Rejects(() => b2Body_ApplyLinearImpulseToCenter(bodyId, badVec, true), "b2Body_ApplyLinearImpulseToCenter");
            Rejects(() => b2Body_ApplyAngularImpulse(bodyId, bad, true), "b2Body_ApplyAngularImpulse");
            Rejects(() => b2Body_SetMassData(bodyId, new B2MassData(bad, b2Vec2_zero, 1.0f)), "b2Body_SetMassData mass");
            Rejects(() => b2Body_SetMassData(bodyId, new B2MassData(1.0f, badVec, 1.0f)), "b2Body_SetMassData center");
            Rejects(() => b2Body_SetMassData(bodyId, new B2MassData(1.0f, b2Vec2_zero, bad)), "b2Body_SetMassData inertia");
            Rejects(() => b2Body_SetLinearDamping(bodyId, bad), "b2Body_SetLinearDamping");
            Rejects(() => b2Body_SetAngularDamping(bodyId, bad), "b2Body_SetAngularDamping");
            Rejects(() => b2Body_SetGravityScale(bodyId, bad), "b2Body_SetGravityScale");
            Rejects(() => b2Body_SetSleepThreshold(bodyId, bad), "b2Body_SetSleepThreshold");
            Rejects(() => b2Body_SetSafetyFactor(bodyId, bad), "b2Body_SetSafetyFactor");
        }

        // A negative value on a quantity the API documents as non-negative is turned away too,
        // since negative damping grows velocity without bound.
        Rejects(() => b2Body_SetLinearDamping(bodyId, -1.0f), "b2Body_SetLinearDamping negative");
        Rejects(() => b2Body_SetAngularDamping(bodyId, -1.0f), "b2Body_SetAngularDamping negative");

        Accepts(() => b2Body_SetLinearVelocity(bodyId, new B2Vec2(1.0f, 2.0f)), "b2Body_SetLinearVelocity");
        Accepts(() => b2Body_ApplyForceToCenter(bodyId, new B2Vec2(0.0f, 10.0f), true), "b2Body_ApplyForceToCenter");

        for (int i = 0; i < 30; ++i)
        {
            b2World_Step(worldId, 1.0f / 60.0f, 4);
        }

        EndRejectCount();

        Assert.That(IsBodyFinite(bodyId));

        b2DestroyWorld(worldId);
    }

    [Test]
    public void ShapeInputTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        B2Polygon box = b2MakeBox(0.5f, 0.5f);
        B2ShapeId shapeId = b2CreatePolygonShape(bodyId, shapeDef, box);

        float density = b2Shape_GetDensity(shapeId);
        float restitution = b2Shape_GetRestitution(shapeId);

        BeginRejectCount();

        foreach (float bad in BadValues)
        {
            B2Vec2 badVec = new B2Vec2(bad, 0.0f);

            B2Circle badCircle = new B2Circle(badVec, 1.0f);
            B2Capsule badCapsule = new B2Capsule(new B2Vec2(-0.5f, 0.0f), new B2Vec2(0.5f, 0.0f), bad);
            B2Segment badSegment = new B2Segment(badVec, new B2Vec2(1.0f, 0.0f));
            B2ChainSegment badChainSegment = new B2ChainSegment
            {
                ghost1 = badVec,
                segment = new B2Segment(new B2Vec2(0.0f, 0.0f), new B2Vec2(1.0f, 0.0f)),
                ghost2 = new B2Vec2(2.0f, 0.0f),
                chainId = -1,
            };

            B2Polygon badPolygon = box;
            badPolygon.radius = bad;

            B2Polygon badVertexPolygon = box;
            badVertexPolygon.vertices[0] = badVec;

            B2SurfaceMaterial badMaterial = b2DefaultSurfaceMaterial();
            badMaterial.friction = bad;

            Rejects(() => b2Shape_SetDensity(shapeId, bad, true), "b2Shape_SetDensity");
            Rejects(() => b2Shape_SetFriction(shapeId, bad), "b2Shape_SetFriction");
            Rejects(() => b2Shape_SetRestitution(shapeId, bad), "b2Shape_SetRestitution");
            Rejects(() => b2Shape_SetSurfaceMaterial(shapeId, badMaterial), "b2Shape_SetSurfaceMaterial");
            Rejects(() => b2Shape_ApplyWind(shapeId, badVec, 1.0f, 1.0f, true), "b2Shape_ApplyWind wind");
            Rejects(() => b2Shape_ApplyWind(shapeId, b2Vec2_zero, bad, 1.0f, true), "b2Shape_ApplyWind drag");
            Rejects(() => b2Shape_ApplyWind(shapeId, b2Vec2_zero, 1.0f, bad, true), "b2Shape_ApplyWind lift");
            Rejects(() => b2Shape_SetCircle(shapeId, badCircle), "b2Shape_SetCircle");
            Rejects(() => b2Shape_SetCapsule(shapeId, badCapsule), "b2Shape_SetCapsule");
            Rejects(() => b2Shape_SetSegment(shapeId, badSegment), "b2Shape_SetSegment");
            Rejects(() => b2Shape_SetChainSegment(shapeId, badChainSegment), "b2Shape_SetChainSegment");
            Rejects(() => b2Shape_SetPolygon(shapeId, ref badPolygon), "b2Shape_SetPolygon radius");
            Rejects(() => b2Shape_SetPolygon(shapeId, ref badVertexPolygon), "b2Shape_SetPolygon vertex");
        }

        Accepts(() => b2Shape_SetFriction(shapeId, 0.4f), "b2Shape_SetFriction");

        for (int i = 0; i < 10; ++i)
        {
            b2World_Step(worldId, 1.0f / 60.0f, 4);
        }

        EndRejectCount();

        // The rejected geometry never reached the shape, so the proxy is still the original box
        Assert.That(b2Shape_GetType(shapeId), Is.EqualTo(B2ShapeType.b2_polygonShape));
        Assert.That(b2IsValidAABB(b2Shape_GetBounds(shapeId)));
        Assert.That(b2IsValidAABB(b2World_GetBounds(worldId)));
        Assert.That(b2Shape_GetDensity(shapeId), Is.EqualTo(density));
        Assert.That(b2Shape_GetFriction(shapeId), Is.EqualTo(0.4f));
        Assert.That(b2Shape_GetRestitution(shapeId), Is.EqualTo(restitution));
        Assert.That(IsBodyFinite(bodyId));

        b2DestroyWorld(worldId);
    }

    [Test]
    public void JointInputTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2BodyId groundId = b2CreateBody(worldId, bodyDef);

        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Pos(0.0f, -1.0f);
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        B2Polygon box = b2MakeBox(0.5f, 0.5f);
        b2CreatePolygonShape(bodyId, shapeDef, box);

        B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
        jointDef.@base.bodyIdA = groundId;
        jointDef.@base.bodyIdB = bodyId;
        jointDef.@base.localFrameB.p = new B2Vec2(0.0f, 1.0f);
        B2JointId jointId = b2CreateRevoluteJoint(worldId, jointDef);

        BeginRejectCount();

        foreach (float bad in BadValues)
        {
            B2Transform badFrame = new B2Transform(new B2Vec2(bad, 0.0f), b2Rot_identity);

            Rejects(() => b2Joint_SetLocalFrameA(jointId, badFrame), "b2Joint_SetLocalFrameA");
            Rejects(() => b2Joint_SetLocalFrameB(jointId, badFrame), "b2Joint_SetLocalFrameB");
            Rejects(() => b2Joint_SetConstraintTuning(jointId, bad, 1.0f), "b2Joint_SetConstraintTuning hertz");
            Rejects(() => b2Joint_SetConstraintTuning(jointId, 60.0f, bad), "b2Joint_SetConstraintTuning damping");
            Rejects(() => b2Joint_SetForceThreshold(jointId, bad), "b2Joint_SetForceThreshold");
            Rejects(() => b2Joint_SetTorqueThreshold(jointId, bad), "b2Joint_SetTorqueThreshold");
            Rejects(() => b2RevoluteJoint_SetSpringHertz(jointId, bad), "b2RevoluteJoint_SetSpringHertz");
            Rejects(() => b2RevoluteJoint_SetSpringDampingRatio(jointId, bad), "b2RevoluteJoint_SetSpringDampingRatio");
            Rejects(() => b2RevoluteJoint_SetTargetAngle(jointId, bad), "b2RevoluteJoint_SetTargetAngle");
            Rejects(() => b2RevoluteJoint_SetLimits(jointId, bad, 1.0f), "b2RevoluteJoint_SetLimits lower");
            Rejects(() => b2RevoluteJoint_SetLimits(jointId, -1.0f, bad), "b2RevoluteJoint_SetLimits upper");
            Rejects(() => b2RevoluteJoint_SetMotorSpeed(jointId, bad), "b2RevoluteJoint_SetMotorSpeed");
            Rejects(() => b2RevoluteJoint_SetMaxMotorTorque(jointId, bad), "b2RevoluteJoint_SetMaxMotorTorque");
        }

        Accepts(() => b2RevoluteJoint_SetMotorSpeed(jointId, 2.0f), "b2RevoluteJoint_SetMotorSpeed");

        for (int i = 0; i < 30; ++i)
        {
            b2World_Step(worldId, 1.0f / 60.0f, 4);
        }

        EndRejectCount();

        Assert.That(IsBodyFinite(bodyId));
        Assert.That(b2IsValidVec2(b2Joint_GetConstraintForce(jointId)));
        Assert.That(b2IsValidFloat(b2Joint_GetConstraintTorque(jointId)));

        b2DestroyWorld(worldId);
    }

    [Test]
    public void CreateInputTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        B2Polygon box = b2MakeBox(0.5f, 0.5f);
        b2CreatePolygonShape(bodyId, shapeDef, box);

        BeginRejectCount();

        foreach (float bad in BadValues)
        {
            B2Vec2 badVec = new B2Vec2(bad, 0.0f);

            B2WorldDef badWorldDef = b2DefaultWorldDef();
            badWorldDef.gravity = badVec;
            B2WorldId badWorldId = b2CreateWorld(badWorldDef);
            Assert.That(b2World_IsValid(badWorldId), Is.False);

            B2BodyDef badBodyDef = b2DefaultBodyDef();
            badBodyDef.position = new B2Pos(bad, 0.0f);
            Assert.That(b2Body_IsValid(b2CreateBody(worldId, badBodyDef)), Is.False);

            B2ShapeDef badShapeDef = b2DefaultShapeDef();
            badShapeDef.density = bad;
            Assert.That(b2Shape_IsValid(b2CreatePolygonShape(bodyId, badShapeDef, box)), Is.False);

            B2Circle badCircle = new B2Circle(badVec, 1.0f);
            Assert.That(b2Shape_IsValid(b2CreateCircleShape(bodyId, shapeDef, badCircle)), Is.False);

            B2Capsule badCapsule = new B2Capsule(new B2Vec2(-0.5f, 0.0f), new B2Vec2(0.5f, 0.0f), bad);
            Assert.That(b2Shape_IsValid(b2CreateCapsuleShape(bodyId, shapeDef, badCapsule)), Is.False);

            B2Segment badSegment = new B2Segment(badVec, new B2Vec2(1.0f, 0.0f));
            Assert.That(b2Shape_IsValid(b2CreateSegmentShape(bodyId, shapeDef, badSegment)), Is.False);

            B2Polygon badPolygon = box;
            badPolygon.radius = bad;
            Assert.That(b2Shape_IsValid(b2CreatePolygonShape(bodyId, shapeDef, badPolygon)), Is.False);

            B2Vec2[] chainPoints = { new B2Vec2(-4.0f, 0.0f), new B2Vec2(0.0f, 0.0f), new B2Vec2(4.0f, 0.0f), new B2Vec2(8.0f, 0.0f) };
            chainPoints[2] = badVec;
            B2SurfaceMaterial material = b2DefaultSurfaceMaterial();
            B2ChainDef badChainDef = b2DefaultChainDef();
            badChainDef.points = chainPoints;
            badChainDef.pointCount = 4;
            badChainDef.materials = new[] { material };
            badChainDef.materialCount = 1;
            badChainDef.isLoop = true;
            Assert.That(b2Chain_IsValid(b2CreateChain(bodyId, badChainDef)), Is.False);

            B2RevoluteJointDef badJointDef = b2DefaultRevoluteJointDef();
            badJointDef.@base.bodyIdA = bodyId;
            badJointDef.@base.bodyIdB = b2CreateBody(worldId, bodyDef);
            badJointDef.hertz = bad;
            Assert.That(b2Joint_IsValid(b2CreateRevoluteJoint(worldId, badJointDef)), Is.False);

            B2ExplosionDef badExplosionDef = b2DefaultExplosionDef();
            badExplosionDef.radius = bad;
            Rejects(() => b2World_Explode(worldId, badExplosionDef), "b2World_Explode");
        }

        for (int i = 0; i < 10; ++i)
        {
            b2World_Step(worldId, 1.0f / 60.0f, 4);
        }

        EndRejectCount();

        Assert.That(IsBodyFinite(bodyId));
        Assert.That(b2IsValidAABB(b2World_GetBounds(worldId)));

        b2DestroyWorld(worldId);
    }
}
