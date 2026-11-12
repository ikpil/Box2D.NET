// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using NUnit.Framework;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

namespace Box2D.NET.Test;

// Port of upstream test_restitution.c. Each upstream subtest is its own NUnit test; each prints
// all of its measurements before asserting.
public class B2RestitutionTest
{
    private const float TIME_STEP = 1.0f / 60.0f;
    private const int SUB_STEP_COUNT = 4;

    // Impact speed shared by every scenario that measures a coefficient. Well above the default
    // restitution threshold so the bounce is always armed.
    private const float IMPACT_SPEED = 5.0f;

    private static B2WorldId MakeWorld(float gravityY)
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.gravity = new B2Vec2(0.0f, gravityY);
        worldDef.enableSleep = false;
        return b2CreateWorld(worldDef);
    }

    // Ground with its top surface at y = 0
    private static void MakeGround(B2WorldId worldId, float restitution)
    {
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.position = new B2Pos(0.0f, -1.0f);
        B2BodyId groundId = b2CreateBody(worldId, bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.material.friction = 0.0f;
        shapeDef.material.restitution = restitution;
        B2Polygon box = b2MakeBox(40.0f, 1.0f);
        b2CreatePolygonShape(groundId, shapeDef, box);
    }

    private static B2BodyId MakeBall(B2WorldId worldId, float x, float y, float velocityY, float restitution)
    {
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Pos(x, y);
        bodyDef.linearVelocity = new B2Vec2(0.0f, velocityY);
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.material.friction = 0.0f;
        shapeDef.material.restitution = restitution;
        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
        b2CreateCircleShape(bodyId, shapeDef, circle);
        return bodyId;
    }

    // Two free circles closing head on with no gravity. The coefficient is the ratio of relative normal
    // speeds at the contact, which is the mass independent definition.
    private static float MeasureHeadOn(float restitution, float densityB, out float momentumError)
    {
        B2WorldId worldId = MakeWorld(0.0f);

        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        shapeDef.material.friction = 0.0f;
        shapeDef.material.restitution = restitution;

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Pos(-1.0f, 0.0f);
        bodyDef.linearVelocity = new B2Vec2(0.5f * IMPACT_SPEED, 0.0f);
        B2BodyId idA = b2CreateBody(worldId, bodyDef);
        b2CreateCircleShape(idA, shapeDef, circle);

        bodyDef.position = new B2Pos(1.0f, 0.0f);
        bodyDef.linearVelocity = new B2Vec2(-0.5f * IMPACT_SPEED, 0.0f);
        B2BodyId idB = b2CreateBody(worldId, bodyDef);
        shapeDef.density = densityB;
        b2CreateCircleShape(idB, shapeDef, circle);

        float massA = b2Body_GetMass(idA);
        float massB = b2Body_GetMass(idB);
        float momentum0 = massA * b2Body_GetLinearVelocity(idA).X + massB * b2Body_GetLinearVelocity(idB).X;

        for (int i = 0; i < 120; ++i)
        {
            b2World_Step(worldId, TIME_STEP, SUB_STEP_COUNT);
        }

        float vA = b2Body_GetLinearVelocity(idA).X;
        float vB = b2Body_GetLinearVelocity(idB).X;
        float momentum1 = massA * vA + massB * vB;

        b2DestroyWorld(worldId);

        momentumError = (momentum1 - momentum0) / (massA + massB);
        return (vB - vA) / IMPACT_SPEED;
    }

    // Ball driven into static ground with no gravity so the measurement carries no gravity bias. The
    // gap shifts where inside the time step the impact lands.
    private static float MeasureGroundBounce(float restitution, float gap)
    {
        B2WorldId worldId = MakeWorld(0.0f);
        MakeGround(worldId, 0.0f);

        B2BodyId ballId = MakeBall(worldId, 0.0f, 0.5f + gap, -IMPACT_SPEED, restitution);

        for (int i = 0; i < 60; ++i)
        {
            b2World_Step(worldId, TIME_STEP, SUB_STEP_COUNT);
        }

        float speed = b2Body_GetLinearVelocity(ballId).Y;
        b2DestroyWorld(worldId);
        return speed / IMPACT_SPEED;
    }

    // Impactor driven onto a column of resting balls pinned against the ground. The support balls are
    // dead so only the top contact can bounce. A supported target has infinite effective mass along the
    // normal, so the coefficient must not depend on the column height.
    private static float MeasureSupportedBounce(float restitution, int supportCount)
    {
        B2WorldId worldId = MakeWorld(0.0f);
        MakeGround(worldId, 0.0f);

        for (int i = 0; i < supportCount; ++i)
        {
            MakeBall(worldId, 0.0f, 0.5f + 1.0f * i, 0.0f, 0.0f);
        }

        float y = 0.5f + 1.0f * supportCount + 0.5f * IMPACT_SPEED * TIME_STEP;
        B2BodyId ballId = MakeBall(worldId, 0.0f, y, -IMPACT_SPEED, restitution);

        for (int i = 0; i < 60; ++i)
        {
            b2World_Step(worldId, TIME_STEP, SUB_STEP_COUNT);
        }

        float speed = b2Body_GetLinearVelocity(ballId).Y;
        b2DestroyWorld(worldId);
        return speed / IMPACT_SPEED;
    }

    // Flat box landing on both corners at once with no gravity
    private static float MeasureFlatBounce(float restitution, int subStepCount, out float spin)
    {
        B2WorldId worldId = MakeWorld(0.0f);
        MakeGround(worldId, 0.0f);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Pos(0.0f, 0.25f + 0.5f * IMPACT_SPEED * TIME_STEP);
        bodyDef.linearVelocity = new B2Vec2(0.0f, -IMPACT_SPEED);
        B2BodyId boxId = b2CreateBody(worldId, bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        shapeDef.material.friction = 0.0f;
        shapeDef.material.restitution = restitution;
        B2Polygon box = b2MakeBox(1.0f, 0.25f);
        b2CreatePolygonShape(boxId, shapeDef, box);

        for (int i = 0; i < 60; ++i)
        {
            b2World_Step(worldId, TIME_STEP, subStepCount);
        }

        float speed = b2Body_GetLinearVelocity(boxId).Y;
        spin = b2Body_GetAngularVelocity(boxId);
        b2DestroyWorld(worldId);
        return speed / IMPACT_SPEED;
    }

    // Perfectly elastic ball dropped onto a segment, the shape the Restitution sample uses
    private static int MeasureDrop(float dropHeight, float[] apexes, int capacity)
    {
        B2WorldId worldId = MakeWorld(-10.0f);

        B2BodyDef groundDef = b2DefaultBodyDef();
        B2BodyId groundId = b2CreateBody(worldId, groundDef);
        B2ShapeDef groundShape = b2DefaultShapeDef();
        B2Segment segment = new B2Segment(new B2Vec2(-60.0f, 0.0f), new B2Vec2(60.0f, 0.0f));
        b2CreateSegmentShape(groundId, groundShape, segment);

        B2BodyId ballId = MakeBall(worldId, 0.0f, dropHeight, 0.0f, 1.0f);

        int apexCount = 0;
        float previousSpeed = 0.0f;

        for (int i = 0; i < 4000 && apexCount < capacity; ++i)
        {
            b2World_Step(worldId, TIME_STEP, SUB_STEP_COUNT);

            float speed = b2Body_GetLinearVelocity(ballId).Y;
            if (previousSpeed > 0.0f && speed <= 0.0f)
            {
                apexes[apexCount] = b2Body_GetPosition(ballId).Y;
                apexCount += 1;
            }

            previousSpeed = speed;
        }

        b2DestroyWorld(worldId);
        return apexCount;
    }

    [Test]
    public void HeadOnTest()
    {
        float[] restitutions = { 0.0f, 0.25f, 0.5f, 0.75f, 1.0f };
        float[] densities = { 1.0f, 10.0f, 100.0f };
        const float tolerance = 0.02f;

        int failed = 0;

        for (int i = 0; i < densities.Length; ++i)
        {
            float worstError = 0.0f;
            float worstMomentum = 0.0f;

            for (int j = 0; j < restitutions.Length; ++j)
            {
                float measured = MeasureHeadOn(restitutions[j], densities[i], out float momentumError);

                float error = b2AbsFloat(measured - restitutions[j]);
                if (error > worstError)
                {
                    worstError = error;
                }

                if (b2AbsFloat(momentumError) > b2AbsFloat(worstMomentum))
                {
                    worstMomentum = momentumError;
                }

                if (error > tolerance)
                {
                    Console.WriteLine($"    head on density {densities[i]:F0} e {restitutions[j]:F2} -> {measured:F4}");
                    failed = 1;
                }
            }

            Console.WriteLine($"    head on density {densities[i],6:F1} worst error {worstError:F4} momentum drift {worstMomentum:E2}");

            if (b2AbsFloat(worstMomentum) > 1.0e-4f)
            {
                failed = 1;
            }
        }

        Assert.That(failed, Is.EqualTo(0));
    }

    [Test]
    public void PhaseTest()
    {
        float[] restitutions = { 0.25f, 0.5f, 0.9f };
        const int sampleCount = 16;
        const float tolerance = 0.05f;

        int failed = 0;

        for (int j = 0; j < restitutions.Length; ++j)
        {
            float minimum = float.MaxValue;
            float maximum = -float.MaxValue;
            float sum = 0.0f;

            for (int i = 0; i < sampleCount; ++i)
            {
                // Sweep the gap across one step of travel so the impact lands at every phase
                float gap = ((float)i / (float)sampleCount) * IMPACT_SPEED * TIME_STEP;
                float measured = MeasureGroundBounce(restitutions[j], gap);
                minimum = b2MinFloat(minimum, measured);
                maximum = b2MaxFloat(maximum, measured);
                sum += measured;
            }

            float mean = sum / sampleCount;
            float spread = maximum - minimum;
            Console.WriteLine($"    phase e {restitutions[j]:F2} -> mean {mean:F4} spread {spread:F4} [{minimum:F4}, {maximum:F4}]");

            if (spread > tolerance || b2AbsFloat(mean - restitutions[j]) > tolerance)
            {
                failed = 1;
            }
        }

        Assert.That(failed, Is.EqualTo(0));
    }

    // The bounce is solved as a constraint alongside the support contacts, so the column can supply the
    // reaction. A terminal restitution pass has nothing after it to do that and measured about 0.6 for
    // e = 0.9, which the tolerance is chosen to reject.
    [Test]
    public void SupportedTest()
    {
        float[] restitutions = { 0.5f, 0.9f };
        const float tolerance = 0.2f;

        int failed = 0;

        for (int j = 0; j < restitutions.Length; ++j)
        {
            for (int n = 0; n <= 3; ++n)
            {
                float measured = MeasureSupportedBounce(restitutions[j], n);
                Console.WriteLine($"    supported e {restitutions[j]:F2} supports {n} -> {measured:F4}");

                if (b2AbsFloat(measured - restitutions[j]) > tolerance)
                {
                    failed = 1;
                }

                // Rebounding faster than the impact is energy from nowhere, whatever the coefficient
                if (measured > 1.01f)
                {
                    failed = 1;
                }
            }
        }

        Assert.That(failed, Is.EqualTo(0));
    }

    // A symmetric two point landing. The two points are solved in sequence within a relax pass and the
    // bounce retires once both points separate, so a small residual spin is expected. The tolerance
    // admits that residual and rejects the gross asymmetry of one point taking the whole bounce.
    [Test]
    public void TwoPointTest()
    {
        float[] restitutions = { 0.5f, 0.9f };
        int[] subStepCounts = { 4, 8 };
        const float speedTolerance = 0.1f;
        const float spinTolerance = 0.25f;

        int failed = 0;

        for (int j = 0; j < restitutions.Length; ++j)
        {
            for (int k = 0; k < subStepCounts.Length; ++k)
            {
                float measured = MeasureFlatBounce(restitutions[j], subStepCounts[k], out float spin);
                Console.WriteLine($"    two point e {restitutions[j]:F2} substeps {subStepCounts[k]} -> {measured:F4} spin {spin:F4}");

                // Only the shipping sub step count is a gate. The wider count is reported so a
                // convergence problem can be told apart from a formulation problem.
                if (subStepCounts[k] != SUB_STEP_COUNT)
                {
                    continue;
                }

                if (b2AbsFloat(measured - restitutions[j]) > speedTolerance || b2AbsFloat(spin) > spinTolerance)
                {
                    failed = 1;
                }
            }
        }

        Assert.That(failed, Is.EqualTo(0));
    }

    // A flat box carrying spin. Both corners stay in contact and the constraints are linear in the body
    // velocity, so perfect restitution reverses linear and angular velocity. The single relax pass
    // leaves a residual well inside the tolerance.
    [Test]
    public void SpinTest()
    {
        float[] spins = { 0.0f, 0.5f, 1.0f, 2.0f };

        int failed = 0;

        for (int j = 0; j < spins.Length; ++j)
        {
            B2WorldId worldId = MakeWorld(0.0f);
            MakeGround(worldId, 0.0f);

            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Pos(0.0f, 0.25f + 0.5f * IMPACT_SPEED * TIME_STEP);
            bodyDef.linearVelocity = new B2Vec2(0.0f, -IMPACT_SPEED);
            bodyDef.angularVelocity = spins[j];
            B2BodyId boxId = b2CreateBody(worldId, bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 1.0f;
            shapeDef.material.friction = 0.0f;
            shapeDef.material.restitution = 1.0f;
            B2Polygon box = b2MakeBox(1.0f, 0.25f);
            b2CreatePolygonShape(boxId, shapeDef, box);

            for (int i = 0; i < 60; ++i)
            {
                b2World_Step(worldId, TIME_STEP, SUB_STEP_COUNT);
            }

            float speed = b2Body_GetLinearVelocity(boxId).Y;
            float spin = b2Body_GetAngularVelocity(boxId);
            b2DestroyWorld(worldId);

            float wantSpin = 0.0f - spins[j];
            Console.WriteLine($"    spin in {spins[j]:+0.00;-0.00} -> vy {speed:+0.0000;-0.0000} (want {IMPACT_SPEED:+0.0000;-0.0000})  w {spin:+0.0000;-0.0000} (want {wantSpin:+0.0000;-0.0000})");

            if (b2AbsFloat(speed - IMPACT_SPEED) > 0.25f || b2AbsFloat(spin + spins[j]) > 0.25f)
            {
                failed = 1;
            }
        }

        Assert.That(failed, Is.EqualTo(0));
    }

    // Perfectly elastic ball under gravity. Only heights where continuous collision engages are used.
    // Continuous collision lands the ball on the surface, so the bounce is armed from the true impact
    // speed and the apex holds. Slower drops resolve the impact inside the overlap and the penetration
    // recovery adds height, by design. See the restitution notes in the contact solver prepare stage.
    [Test]
    public void DropTest()
    {
        float[] heights = { 40.0f, 20.0f };

        int failed = 0;

        for (int j = 0; j < heights.Length; ++j)
        {
            float[] apexes = new float[6];
            int apexCount = MeasureDrop(heights[j], apexes, apexes.Length);

            string line = $"    drop {heights[j],5:F1} ->";
            for (int i = 0; i < apexCount; ++i)
            {
                line += $" {apexes[i],8:F3}";
            }

            Console.WriteLine(line);

            if (apexCount < apexes.Length)
            {
                failed = 1;
                continue;
            }

            float highest = apexes[0];
            for (int i = 1; i < apexCount; ++i)
            {
                highest = b2MaxFloat(highest, apexes[i]);
            }

            if (highest > 1.02f * apexes[0] || apexes[apexCount - 1] < 0.8f * apexes[0])
            {
                failed = 1;
            }
        }

        Assert.That(failed, Is.EqualTo(0));
    }

    // Mirrors the SingleRestitution sample: square box, perfectly elastic, dropped flat onto a segment
    // with an aggressive continuous safety factor so it lands square on the surface. Only the first
    // bounce is a gate. The residual spin from that landing tilts the box for the next one, and a corner
    // first landing under the per point impact law sheds energy into rotation, so later apexes fall off.
    [Test]
    public void SingleBoxTest()
    {
        B2WorldId worldId = MakeWorld(-10.0f);

        B2BodyDef groundDef = b2DefaultBodyDef();
        B2BodyId groundId = b2CreateBody(worldId, groundDef);
        B2ShapeDef groundShape = b2DefaultShapeDef();
        B2Segment segment = new B2Segment(new B2Vec2(-20.0f, 0.0f), new B2Vec2(20.0f, 0.0f));
        b2CreateSegmentShape(groundId, groundShape, segment);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        shapeDef.material.restitution = 1.0f;
        shapeDef.material.friction = 0.0f;
        B2Polygon box = b2MakeBox(0.5f, 0.5f);

        const float dropHeight = 10.0f;

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Pos(0.0f, dropHeight);
        bodyDef.safetyFactor = 0.01f;
        B2BodyId boxId = b2CreateBody(worldId, bodyDef);
        b2CreatePolygonShape(boxId, shapeDef, box);

        float firstSpin = 0.0f;
        float[] apexes = new float[4];
        int apexCount = 0;
        float previousSpeed = 0.0f;

        // A nearly elastic bounce from 10 m takes about 165 steps, so budget for four of them
        for (int i = 0; i < 1200 && apexCount < apexes.Length; ++i)
        {
            b2World_Step(worldId, TIME_STEP, SUB_STEP_COUNT);

            float speed = b2Body_GetLinearVelocity(boxId).Y;

            if (apexCount == 0 && previousSpeed <= 0.0f && speed > 0.0f)
            {
                firstSpin = b2Body_GetAngularVelocity(boxId);
            }

            if (previousSpeed > 0.0f && speed <= 0.0f)
            {
                apexes[apexCount] = b2Body_GetPosition(boxId).Y;
                apexCount += 1;
            }

            previousSpeed = speed;
        }

        b2DestroyWorld(worldId);

        string line = $"    single box first bounce spin {firstSpin:+0.0000;-0.0000} apexes";
        for (int i = 0; i < apexCount; ++i)
        {
            line += $" {apexes[i],7:F3}";
        }

        Console.WriteLine(line);

        Assert.That(apexCount, Is.EqualTo(apexes.Length));
        Assert.That(apexes[0], Is.GreaterThanOrEqualTo(0.97f * dropHeight));
        Assert.That(apexes[0], Is.LessThanOrEqualTo(1.02f * dropHeight));
        Assert.That(b2AbsFloat(firstSpin), Is.LessThanOrEqualTo(0.25f));
    }

    [Test]
    public void ThresholdTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        float threshold = worldDef.restitutionThreshold;

        float[] scales = { 0.5f, 0.9f, 1.5f, 4.0f };

        int failed = 0;

        for (int j = 0; j < scales.Length; ++j)
        {
            float speed = scales[j] * threshold;

            B2WorldId worldId = MakeWorld(0.0f);
            MakeGround(worldId, 0.0f);
            B2BodyId ballId = MakeBall(worldId, 0.0f, 0.5f + 0.5f * speed * TIME_STEP, -speed, 1.0f);

            for (int i = 0; i < 60; ++i)
            {
                b2World_Step(worldId, TIME_STEP, SUB_STEP_COUNT);
            }

            float ratio = b2Body_GetLinearVelocity(ballId).Y / speed;
            b2DestroyWorld(worldId);

            Console.WriteLine($"    threshold {scales[j]:F2}x -> {ratio:F4}");

            if (scales[j] < 1.0f && ratio > 0.05f)
            {
                failed = 1;
            }

            if (scales[j] > 1.0f && ratio < 0.9f)
            {
                failed = 1;
            }
        }

        Assert.That(failed, Is.EqualTo(0));
    }

    // High restitution must not wake a settled stack back up
    [Test]
    public void RestingTest()
    {
        B2WorldId worldId = MakeWorld(-10.0f);
        MakeGround(worldId, 0.9f);

        const int boxCount = 10;
        B2BodyId[] boxIds = new B2BodyId[boxCount];

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        shapeDef.material.friction = 0.6f;
        shapeDef.material.restitution = 0.9f;
        B2Polygon box = b2MakeBox(0.5f, 0.5f);

        for (int i = 0; i < boxCount; ++i)
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Pos(0.0f, 0.5f + 1.0f * i);
            boxIds[i] = b2CreateBody(worldId, bodyDef);
            b2CreatePolygonShape(boxIds[i], shapeDef, box);
        }

        for (int i = 0; i < 120; ++i)
        {
            b2World_Step(worldId, TIME_STEP, SUB_STEP_COUNT);
        }

        float settled = b2Body_GetPosition(boxIds[boxCount - 1]).Y;
        float drift = 0.0f;
        float peakSpeed = 0.0f;

        for (int i = 0; i < 300; ++i)
        {
            b2World_Step(worldId, TIME_STEP, SUB_STEP_COUNT);

            float y = b2Body_GetPosition(boxIds[boxCount - 1]).Y;
            drift = b2MaxFloat(drift, b2AbsFloat(y - settled));

            for (int j = 0; j < boxCount; ++j)
            {
                peakSpeed = b2MaxFloat(peakSpeed, b2Length(b2Body_GetLinearVelocity(boxIds[j])));
            }
        }

        b2DestroyWorld(worldId);

        Console.WriteLine($"    resting drift {drift:F5} peak speed {peakSpeed:F5}");

        Assert.That(drift, Is.LessThan(0.01f));
        Assert.That(peakSpeed, Is.LessThan(0.05f));
    }

    private static ulong RunWorkerScene(int workerCount)
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.gravity = new B2Vec2(0.0f, -10.0f);
        worldDef.enableSleep = false;
        worldDef.workerCount = workerCount;
        B2WorldId worldId = b2CreateWorld(worldDef);

        MakeGround(worldId, 0.6f);

        for (int i = 0; i < 20; ++i)
        {
            MakeBall(worldId, -10.0f + 1.05f * i, 3.0f + 0.13f * i, 0.0f, 0.6f);
        }

        for (int i = 0; i < 200; ++i)
        {
            b2World_Step(worldId, TIME_STEP, SUB_STEP_COUNT);
        }

        ulong hash = b2World_GetStateHash(worldId);
        b2DestroyWorld(worldId);
        return hash;
    }

    // The armed bounce is per manifold point state, so it must survive the split across workers
    [Test]
    public void WorkerParityTest()
    {
        ulong hash0 = RunWorkerScene(0);
        ulong hash1 = RunWorkerScene(1);
        ulong hash4 = RunWorkerScene(4);

        Console.WriteLine($"    worker hashes 0x{hash0:x16} 0x{hash1:x16} 0x{hash4:x16}");

        Assert.That(hash1, Is.EqualTo(hash0));
        Assert.That(hash4, Is.EqualTo(hash0));
    }
}
