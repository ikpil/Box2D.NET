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

    // A two point landing converges its Gauss-Seidel coupling in four stage iterations and the default
    // is two, so the tests that read the coefficient or the spin of a flat landing pin the count
    private const int RESTITUTION_ITERATIONS = 4;

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

    private static B2WorldId MakeWorldIterations(float gravityY, int restitutionIterations)
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.gravity = new B2Vec2(0.0f, gravityY);
        worldDef.enableSleep = false;
        worldDef.restitutionIterations = restitutionIterations;
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
    private static float MeasureSupportedBounce(float restitution, int supportCount, bool propagate)
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.gravity = new B2Vec2(0.0f, 0.0f);
        worldDef.enableSleep = false;
        worldDef.enableRestitutionPropagation = propagate;
        worldDef.restitutionIterations = RESTITUTION_ITERATIONS;
        B2WorldId worldId = b2CreateWorld(worldDef);
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
        B2WorldId worldId = MakeWorldIterations(0.0f, RESTITUTION_ITERATIONS);
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

    // By default the restitution stage sweeps only the contacts that carry restitution, so the ball
    // pushes the column down and the ground under it gets no say until the next step: e = 0.9 gives
    // about 0.52 on one support. With propagation enabled the stage sweeps every contact, the ground
    // contact supplies the reaction within the stage and e = 0.9 gives 0.85 on one support and 0.73
    // on three at the pinned four iterations (0.71 and 0.65 at the default two), decaying with depth
    // because the column needs an iteration per level. Propagation costs a normal pass over every
    // contact per iteration, hence the option. Both modes are gated: never above the free coefficient,
    // which would be energy from nowhere, and never collapsed.
    [Test]
    public void SupportedTest()
    {
        float[] restitutions = { 0.5f, 0.9f };

        int failed = 0;

        for (int propagate = 0; propagate <= 1; ++propagate)
        {
            float floor = propagate != 0 ? 0.75f : 0.5f;

            for (int j = 0; j < restitutions.Length; ++j)
            {
                for (int n = 0; n <= 3; ++n)
                {
                    float measured = MeasureSupportedBounce(restitutions[j], n, propagate == 1);
                    Console.WriteLine($"    supported e {restitutions[j]:F2} supports {n} propagation {propagate} -> {measured:F4}");

                    if (measured > restitutions[j] + 0.01f || measured < floor * restitutions[j])
                    {
                        failed = 1;
                    }
                }
            }
        }

        Assert.That(failed, Is.EqualTo(0));
    }

    // A symmetric two point landing. The stage solves the two points in sequence, so a single sweep
    // leaves the box spinning: the first impulse tilts it and the second point sees a larger closing
    // speed. Four stage iterations converge the coupling and leave a small residual, two leave the box
    // with 0.3 rad/s. The tolerance admits that residual and rejects the single sweep behavior.
    [Test]
    public void TwoPointTest()
    {
        float[] restitutions = { 0.5f, 0.9f };
        int[] subStepCounts = { 4, 8 };
        const float speedTolerance = 0.01f;
        const float spinTolerance = 0.03f;

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
    // velocity, so perfect restitution reverses linear and angular velocity. Four stage iterations
    // bring both within a few hundredths. One left a residual an order of magnitude larger.
    [Test]
    public void SpinTest()
    {
        float[] spins = { 0.0f, 0.5f, 1.0f, 2.0f };

        int failed = 0;

        for (int j = 0; j < spins.Length; ++j)
        {
            B2WorldId worldId = MakeWorldIterations(0.0f, RESTITUTION_ITERATIONS);
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

            if (b2AbsFloat(speed - IMPACT_SPEED) > 0.05f || b2AbsFloat(spin + spins[j]) > 0.05f)
            {
                failed = 1;
            }
        }

        Assert.That(failed, Is.EqualTo(0));
    }

    // Perfectly elastic ball under gravity. Only heights where continuous collision engages are used.
    // Continuous collision lands the ball on the surface, so the bounce is armed from the true impact
    // speed and the apex holds. Slower drops resolve the impact inside the overlap and the penetration
    // recovery adds height, by design. The 10 m case is the long horizon read: the per manifold energy
    // budget crept up 0.05% a bounce there while 20 m and 40 m looked clean.
    [Test]
    public void DropTest()
    {
        float[] heights = { 40.0f, 20.0f, 10.0f };

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

            // A later apex above the first, or a first apex above the drop, is energy from nowhere
            if (highest > 1.001f * apexes[0] || apexes[0] > 1.001f * heights[j] || apexes[apexCount - 1] < 0.8f * apexes[0])
            {
                failed = 1;
            }
        }

        Assert.That(failed, Is.EqualTo(0));
    }

    // Mirrors the SingleBoxRestitution sample: square box, perfectly elastic, dropped flat onto a
    // segment with an aggressive continuous safety factor so it lands square on the surface. A flat
    // landing solves two coplanar points in sequence, and one sweep over-delivers: the first impulse
    // tilts the box, the second point sees a larger closing speed and the sum overshoots the rigid body
    // answer, leaving spin that tilts the box for the next landing. Four stage iterations converge the
    // coupling, at the default two the plank leaves with 0.13 rad/s and tumbles on its second bounce.
    // The bounds below reject the single sweep behavior and any apex above the drop.
    [Test]
    public void SingleBoxTest()
    {
        B2WorldId worldId = MakeWorldIterations(-10.0f, RESTITUTION_ITERATIONS);

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
        float[] spins = new float[4];
        int apexCount = 0;
        float previousSpeed = 0.0f;

        // A nearly elastic bounce from 10 m takes about 165 steps, so budget for four of them
        for (int i = 0; i < 1200 && apexCount < apexes.Length; ++i)
        {
            b2World_Step(worldId, TIME_STEP, SUB_STEP_COUNT);

            float speed = b2Body_GetLinearVelocity(boxId).Y;

            if (apexCount == 0 && previousSpeed <= 0.0f && speed > 0.0f)
            {
                firstSpin = b2AbsFloat(b2Body_GetAngularVelocity(boxId));
            }

            if (previousSpeed > 0.0f && speed <= 0.0f)
            {
                apexes[apexCount] = b2Body_GetPosition(boxId).Y;
                spins[apexCount] = b2AbsFloat(b2Body_GetAngularVelocity(boxId));
                apexCount += 1;
            }

            previousSpeed = speed;
        }

        b2DestroyWorld(worldId);

        string line = $"    single box first bounce spin {firstSpin:F4} apexes";
        for (int i = 0; i < apexCount; ++i)
        {
            line += $" {apexes[i],7:F3} (spin {spins[i],5:F2})";
        }

        Console.WriteLine(line);

        Assert.That(apexCount, Is.EqualTo(apexes.Length));

        int failed = 0;

        if (firstSpin > 0.01f)
        {
            failed = 1;
        }

        for (int i = 0; i < apexCount; ++i)
        {
            if (apexes[i] < 0.95f * dropHeight || apexes[i] > 1.001f * dropHeight)
            {
                failed = 1;
            }
        }

        Assert.That(failed, Is.EqualTo(0));
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

    // The manifold point normal velocity is published only for contacts that enabled hit events, and it
    // is not a stale value otherwise, it is zero, so a reader can tell "not measured" from "measured
    // zero" by whether the shape enables the events. Restitution no longer reads it at all, so this is
    // the only thing keeping the field alive.
    private static float MeasureFirstTouchNormalVelocity(bool enableHitEvents)
    {
        B2WorldId worldId = MakeWorld(0.0f);
        MakeGround(worldId, 0.0f);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Pos(0.0f, 0.6f);
        B2BodyId ballId = b2CreateBody(worldId, bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.material.friction = 0.0f;
        shapeDef.material.restitution = 0.0f;
        shapeDef.enableHitEvents = enableHitEvents;
        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
        b2CreateCircleShape(ballId, shapeDef, circle);

        float normalVelocity = float.MaxValue;

        // Hold the closing speed so the sampled value does not depend on which step the pair is created
        B2ContactData[] contactData = new B2ContactData[1];
        for (int i = 0; i < 20; ++i)
        {
            b2Body_SetLinearVelocity(ballId, new B2Vec2(0.0f, -IMPACT_SPEED));
            b2World_Step(worldId, TIME_STEP, SUB_STEP_COUNT);

            if (b2Body_GetContactData(ballId, contactData, 1) == 1 && contactData[0].manifold.pointCount > 0)
            {
                normalVelocity = contactData[0].manifold.points[0].normalVelocity;
                break;
            }
        }

        b2DestroyWorld(worldId);
        return normalVelocity;
    }

    [Test]
    public void NormalVelocityTest()
    {
        float quiet = MeasureFirstTouchNormalVelocity(false);
        float published = MeasureFirstTouchNormalVelocity(true);

        Console.WriteLine($"    normal velocity hit events off {quiet:F4} on {published:F4} (closing at {-IMPACT_SPEED:F4})");

        Assert.That(quiet, Is.EqualTo(0.0f));
        Assert.That(published, Is.LessThanOrEqualTo(-0.8f * IMPACT_SPEED));
    }

    // Mechanical energy of a body as symplectic Euler conserves it. The integrator updates velocity
    // before position, so the quantity that is exactly constant in free flight is not kinetic plus
    // potential but that plus half a sub-step of the gravity power, m * dot(v, g) * h / 2. Plain
    // kinetic plus potential sampled at step boundaries jumps by about g * v * h at every bounce and
    // drifts down by g^2 h^2 / 2 each sub-step, which would hide a real gain or fake one. With this
    // measure a contact that never does positive work leaves the energy flat to round-off.
    private static float MeasureEnergy(B2BodyId bodyId, B2WorldId worldId)
    {
        B2MassData massData = b2Body_GetMassData(bodyId);
        B2Vec2 v = b2Body_GetLinearVelocity(bodyId);
        float w = b2Body_GetAngularVelocity(bodyId);
        B2Vec2 gravity = b2MulSV(b2Body_GetGravityScale(bodyId), b2World_GetGravity(worldId));
        float h = TIME_STEP / SUB_STEP_COUNT;

        float kinetic = 0.5f * massData.mass * b2Dot(v, v) + 0.5f * massData.rotationalInertia * w * w;
        float potential = -massData.mass * b2Dot(gravity, b2ToVec2(b2Body_GetWorldCenter(bodyId)));
        float phase = 0.5f * h * massData.mass * b2Dot(v, gravity);

        return kinetic + potential + phase;
    }

    private struct OvershootResult
    {
        public float firstApex;
        public float firstEnergyRatio;
        public float peakEnergyRatio;
        public float speedRatio;
        public int bounceCount;
    }

    // A unit box dropped flat on a floor narrower than itself, perfectly elastic, so the two contact
    // points sit inboard of the box corners. A perfectly elastic bounce cannot come back higher than it
    // was dropped from and cannot gain energy.
    private static OvershootResult MeasureOvershoot(bool continuous)
    {
        B2WorldId worldId = MakeWorldIterations(-10.0f, RESTITUTION_ITERATIONS);

        B2BodyDef floorDef = b2DefaultBodyDef();
        floorDef.position = new B2Pos(0.0f, -0.25f);
        B2BodyId floorId = b2CreateBody(worldId, floorDef);

        B2ShapeDef floorShape = b2DefaultShapeDef();
        B2Polygon floor = b2MakeBox(0.375f, 0.25f);
        b2CreatePolygonShape(floorId, floorShape, floor);

        const float dropHeight = 10.0f;

        B2BodyDef boxDef = b2DefaultBodyDef();
        boxDef.type = B2BodyType.b2_dynamicBody;
        boxDef.position = new B2Pos(0.0f, dropHeight);
        if (continuous)
        {
            boxDef.safetyFactor = 0.1f;
        }

        B2BodyId boxId = b2CreateBody(worldId, boxDef);

        B2ShapeDef boxShape = b2DefaultShapeDef();
        boxShape.material.restitution = 1.0f;
        B2Polygon box = b2MakeBox(0.5f, 0.5f);
        b2CreatePolygonShape(boxId, boxShape, box);

        OvershootResult result = new OvershootResult();

        float startEnergy = MeasureEnergy(boxId, worldId);
        float impactSpeed = 0.0f;
        float reboundSpeed = 0.0f;
        float bounceHeight = 0.0f;
        float previousSpeed = 0.0f;

        for (int i = 0; i < 600 && result.bounceCount < 2; ++i)
        {
            float before = b2Body_GetLinearVelocity(boxId).Y;
            float heightBefore = b2Body_GetPosition(boxId).Y;
            b2World_Step(worldId, TIME_STEP, SUB_STEP_COUNT);
            float speed = b2Body_GetLinearVelocity(boxId).Y;

            if (reboundSpeed == 0.0f && before < 0.0f && speed > 0.0f)
            {
                impactSpeed = -before;
                reboundSpeed = speed;
                bounceHeight = heightBefore;
            }

            if (reboundSpeed == 0.0f)
            {
                continue;
            }

            // Every step, not just at the apexes, because the impact step itself is where any gain appears
            float ratio = MeasureEnergy(boxId, worldId) / startEnergy;
            if (ratio > result.peakEnergyRatio)
            {
                result.peakEnergyRatio = ratio;
            }

            if (previousSpeed > 0.0f && speed <= 0.0f)
            {
                if (result.bounceCount == 0)
                {
                    result.firstApex = b2Body_GetPosition(boxId).Y;
                    result.firstEnergyRatio = ratio;
                }

                result.bounceCount += 1;
            }

            previousSpeed = speed;
        }

        b2DestroyWorld(worldId);

        result.speedRatio = reboundSpeed / impactSpeed;

        Console.WriteLine($"    overshoot continuous {(continuous ? 1 : 0)} apex {result.firstApex:F4} of {dropHeight:F4} at y {bounceHeight:F4}, " +
                          $"speed ratio {result.speedRatio:F4}, energy {result.firstEnergyRatio:F4} then {result.peakEnergyRatio:F4}");

        return result;
    }

    // Two runs, because two unrelated effects were tangled together here.
    //
    // The impulse must never return more speed than it received. That is the restitution invariant and it
    // is checked in both runs. The unconverged multi point solve failed it.
    //
    // Everything else depends on whether continuous collision engages. Without it the box moves 0.23 m
    // in the step before contact, just under the 0.25 m trigger, so the first manifold appears with the
    // box already deep. It bounces from down there and climbs back through that depth for free, which is
    // worth a fraction of a metre of apex and a little energy, and none of it is restitution. With
    // continuous collision the box lands on the surface and both the apex and the energy come in under
    // where they started. The safety factor here is the supported answer for a body whose elastic
    // accuracy matters.
    //
    // The discrete run is only gated on its first bounce. Later bounces there are a genuine blow-up, not
    // a tolerance question: the free height tilts the box and the next landing is a deep corner impact.
    // Lowering the safety factor is the fix; the discrete run is kept to pin what happens when it is
    // left alone, and because its speed ratio is what caught the real bug.
    [Test]
    public void OvershootTest()
    {
        const float dropHeight = 10.0f;

        OvershootResult discrete = MeasureOvershoot(false);
        OvershootResult continuous = MeasureOvershoot(true);

        // Perfectly elastic returns exactly the speed it received, so any excess is round-off. The
        // overflow and colored solvers round differently and either can land an ulp over one.
        const float speedTolerance = 1.0e-5f;

        int failed = 0;

        if (discrete.speedRatio > 1.0f + speedTolerance || continuous.speedRatio > 1.0f + speedTolerance)
        {
            failed = 1;
        }

        if (continuous.firstApex > dropHeight || continuous.peakEnergyRatio > 1.001f)
        {
            failed = 1;
        }

        if (discrete.firstApex > dropHeight + 0.15f || discrete.firstEnergyRatio > 1.02f)
        {
            failed = 1;
        }

        if (discrete.bounceCount < 2 || continuous.bounceCount < 2)
        {
            failed = 1;
        }

        Assert.That(failed, Is.EqualTo(0));
    }

    private struct ImpulseResult
    {
        public float worstError;
        public float approachSpeed;
        public float firstSeparation;
        public float bounceImpulse;
        public int contactSteps;
        public int toiSteps;
        public int toiImpulseSteps;
    }

    // Ball dropped under gravity with the contact impulse read back every step. Nothing else touches the
    // ball, so once gravity is taken out the change in momentum over a step is the impulse the contact
    // applied, and that is what the total normal impulse must report. A time of impact step is left out
    // of the balance: no contact was solved on it, and the sweep hands back the gravity of the time it
    // cut short.
    private static ImpulseResult MeasureDropImpulse(float restitution, float dropHeight)
    {
        B2WorldId worldId = MakeWorld(-10.0f);
        MakeGround(worldId, 0.0f);

        B2BodyId ballId = MakeBall(worldId, 0.0f, 0.5f + dropHeight, 0.0f, restitution);
        B2World world = b2GetWorldFromId(worldId);

        float mass = b2Body_GetMass(ballId);
        float gravityY = b2World_GetGravity(worldId).Y;

        ImpulseResult result = new ImpulseResult();
        bool touched = false;
        bool bouncing = false;

        B2ContactData[] contactData = new B2ContactData[4];

        // Enough for the longest fall and the bounce that follows it
        for (int i = 0; i < 240; ++i)
        {
            float speedBefore = b2Body_GetLinearVelocity(ballId).Y;
            b2World_Step(worldId, TIME_STEP, SUB_STEP_COUNT);
            float speedAfter = b2Body_GetLinearVelocity(ballId).Y;

            float measured = 0.0f;
            float separation = 0.0f;
            int contactCount = b2Body_GetContactData(ballId, contactData, contactData.Length);
            for (int c = 0; c < contactCount; ++c)
            {
                ref readonly B2Manifold manifold = ref contactData[c].manifold;
                for (int p = 0; p < manifold.pointCount; ++p)
                {
                    measured += manifold.points[p].totalNormalImpulse;
                    separation = manifold.points[p].separation;
                }
            }

            // Transient body flags are cleared and rewritten every step
            B2Body ball = b2GetBodyFullId(world, ballId);
            if (0 != (ball.flags & (uint)B2BodyFlags.b2_hadTimeOfImpact))
            {
                result.toiSteps += 1;
                if (measured != 0.0f)
                {
                    result.toiImpulseSteps += 1;
                }

                continue;
            }

            float expected = mass * (speedAfter - speedBefore) - mass * gravityY * TIME_STEP;
            result.worstError = b2MaxFloat(result.worstError, b2AbsFloat(measured - expected));

            if (contactCount > 0)
            {
                if (touched == false)
                {
                    touched = true;
                    bouncing = true;
                    result.approachSpeed = -speedBefore;
                    result.firstSeparation = separation;
                }

                if (bouncing)
                {
                    result.bounceImpulse += measured;
                    result.contactSteps += 1;
                }
            }
            else
            {
                bouncing = false;
            }
        }

        b2DestroyWorld(worldId);
        return result;
    }

    // Heights on both sides of the continuous collision threshold, so the impulse is checked for a ball
    // that lands inside the overlap and for one the sweep sets down on the surface. The threshold is
    // derived from the body so the split survives a change to the default safety factor.
    //
    // The step balance is the real gate. The bounce total is bracketed as well so the restitution sweep
    // means something: the contact has to reverse the approach at the coefficient and may carry the
    // weight for at most as long as it lasted. The stage runs after the sub-steps, so the reversal
    // lands a step late and the ball can come in up to a step of gravity under it.
    [Test]
    public void ImpulseTest()
    {
        float[] restitutions = { 0.0f, 0.25f, 0.5f, 0.75f, 1.0f };
        float[] heights = { 1.0f, 5.0f, 20.0f, 45.0f };

        float mass;
        float fastSpeed;
        {
            B2WorldId worldId = MakeWorld(-10.0f);
            B2BodyId ballId = MakeBall(worldId, 0.0f, 0.5f, 0.0f, 0.0f);
            mass = b2Body_GetMass(ballId);
            fastSpeed = b2Body_GetSafetyFactor(ballId) * b2Body_GetMinExtent(ballId) / TIME_STEP;
            b2DestroyWorld(worldId);
        }

        float weightImpulse = mass * 10.0f * TIME_STEP;

        int failed = 0;

        for (int j = 0; j < heights.Length; ++j)
        {
            float impactSpeed = MathF.Sqrt(20.0f * heights[j]);
            bool expectToi = impactSpeed > fastSpeed;

            // Float noise in the solver velocities scales with the impact speed, and the ball is heavy
            float tolerance = 1e-5f * mass * (10.0f + impactSpeed);

            for (int k = 0; k < restitutions.Length; ++k)
            {
                ImpulseResult result = MeasureDropImpulse(restitutions[k], heights[j]);

                float reversal = (1.0f + restitutions[k]) * mass * result.approachSpeed;
                float lower = reversal - weightImpulse;
                float upper = reversal + weightImpulse * result.contactSteps;

                Console.WriteLine($"    impulse drop {heights[j],4:F1} e {restitutions[k]:F2} toi {result.toiSteps} at {result.firstSeparation:+0.0000;-0.0000} -> " +
                                  $"worst step error {result.worstError:0.0e+00}, bounce {result.bounceImpulse:F1} in [{lower:F1}, {upper:F1}] over {result.contactSteps} steps");

                if (result.worstError > tolerance)
                {
                    failed = 1;
                }

                if (result.contactSteps == 0 || result.toiImpulseSteps > 0)
                {
                    failed = 1;
                }

                float slack = result.contactSteps * tolerance;
                if (result.bounceImpulse < lower - slack || result.bounceImpulse > upper + slack)
                {
                    failed = 1;
                }

                if ((result.toiSteps > 0) != expectToi)
                {
                    Console.WriteLine($"    continuous collision {(expectToi ? "expected" : "unexpected")} at {impactSpeed:F1} m/s (threshold {fastSpeed:F1} m/s)");
                    failed = 1;
                }
            }
        }

        Assert.That(failed, Is.EqualTo(0));
    }

    private readonly struct EnergyScene
    {
        public readonly string name;
        public readonly float safetyFactor;
        public readonly float friction;
        public readonly float angle;
        public readonly float spin;
        public readonly float height;
        public readonly float velocityY;
        public readonly float halfWidth;
        public readonly float halfHeight;
        public readonly bool bullet;
        public readonly bool stack;
        public readonly int stepCount;

        public EnergyScene(string name, float safetyFactor, float friction, float angle, float spin, float height, float velocityY,
            float halfWidth, float halfHeight, bool bullet, bool stack, int stepCount)
        {
            this.name = name;
            this.safetyFactor = safetyFactor;
            this.friction = friction;
            this.angle = angle;
            this.spin = spin;
            this.height = height;
            this.velocityY = velocityY;
            this.halfWidth = halfWidth;
            this.halfHeight = halfHeight;
            this.bullet = bullet;
            this.stack = stack;
            this.stepCount = stepCount;
        }
    }

    // Peak energy over the run relative to the start, for every dynamic body in the scene. Perfectly
    // elastic and the energy may still only go down: friction and the soft contact position correction
    // dissipate, and a bounce may return at most what the contact absorbed.
    private static float MeasurePeakEnergy(in EnergyScene scene)
    {
        B2WorldId worldId = MakeWorld(-10.0f);

        B2BodyDef groundDef = b2DefaultBodyDef();
        B2BodyId groundId = b2CreateBody(worldId, groundDef);
        B2ShapeDef groundShape = b2DefaultShapeDef();
        groundShape.material.friction = scene.friction;
        B2Segment segment = new B2Segment(new B2Vec2(-40.0f, 0.0f), new B2Vec2(40.0f, 0.0f));
        b2CreateSegmentShape(groundId, groundShape, segment);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        shapeDef.material.friction = scene.friction;
        shapeDef.material.restitution = 1.0f;
        B2Polygon box = b2MakeBox(scene.halfWidth, scene.halfHeight);

        B2BodyId[] bodyIds = new B2BodyId[4];
        int bodyCount = 0;

        if (scene.stack)
        {
            // Resting box under the dropped one, like the TwoBoxRestitution sample
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Pos(0.0f, scene.halfHeight);
            bodyDef.safetyFactor = scene.safetyFactor;
            bodyIds[bodyCount] = b2CreateBody(worldId, bodyDef);
            b2CreatePolygonShape(bodyIds[bodyCount], shapeDef, box);
            bodyCount += 1;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Pos(0.0f, scene.height);
            bodyDef.rotation = b2MakeRot(scene.angle);
            bodyDef.angularVelocity = scene.spin;
            bodyDef.linearVelocity = new B2Vec2(0.0f, scene.velocityY);
            bodyDef.safetyFactor = scene.safetyFactor;
            bodyDef.isBullet = scene.bullet;
            bodyIds[bodyCount] = b2CreateBody(worldId, bodyDef);
            b2CreatePolygonShape(bodyIds[bodyCount], shapeDef, box);
            bodyCount += 1;
        }

        float start = 0.0f;
        for (int k = 0; k < bodyCount; ++k)
        {
            start += MeasureEnergy(bodyIds[k], worldId);
        }

        float peak = start;
        int peakStep = -1;

        for (int i = 0; i < scene.stepCount; ++i)
        {
            b2World_Step(worldId, TIME_STEP, SUB_STEP_COUNT);

            float energy = 0.0f;
            for (int k = 0; k < bodyCount; ++k)
            {
                energy += MeasureEnergy(bodyIds[k], worldId);
            }

            if (energy > peak)
            {
                peak = energy;
                peakStep = i;
            }
        }

        b2DestroyWorld(worldId);

        float ratio = peak / start;
        Console.WriteLine($"    energy {scene.name,-24} peak {ratio:F5} at step {peakStep}");
        return ratio;
    }

    // Perfectly elastic material with friction, dropped every way that used to gain energy: a spinning
    // corner landing, tumbling, a flat landing with spin, a box falling on a resting box, and a tall box
    // landing flat on its narrow end. Energy must never rise above the start. This is the load bearing
    // property of restitution. Accuracy of the coefficient and freedom from spin are secondary.
    //
    // The tall boxes guard the stage iteration count. Their contact points sit close together while the
    // inertia is long, so the off diagonal of the two point normal mass is large and one sweep
    // overshoots: the narrowest gains 1.7% frictionless and 1.0% with friction at a single sweep and is
    // flat from two on.
    [Test]
    public void EnergyTest()
    {
        EnergyScene[] scenes =
        {
            new EnergyScene("spinning corner", 0.01f, 0.6f, -1.43f, 7.9f, 0.7f, -1.9f, 0.5f, 0.5f, false, false, 300),
            new EnergyScene("spinning corner fast", 0.01f, 0.6f, -0.8f, 12.0f, 3.0f, -8.0f, 0.5f, 0.5f, false, false, 300),
            new EnergyScene("tumble", 0.01f, 0.6f, -0.3f, 3.0f, 8.0f, 0.0f, 0.5f, 0.5f, false, false, 600),
            new EnergyScene("tumble discrete", 0.5f, 0.6f, -0.3f, 3.0f, 8.0f, 0.0f, 0.5f, 0.5f, false, false, 600),
            new EnergyScene("flat with spin", 0.01f, 0.6f, 0.0f, 2.0f, 2.0f, -5.0f, 0.5f, 0.5f, false, false, 300),
            new EnergyScene("flat frictionless", 0.01f, 0.0f, 0.0f, 0.0f, 10.0f, 0.0f, 0.5f, 0.5f, false, false, 1200),
            new EnergyScene("corner", 0.01f, 0.6f, -0.2f, 0.0f, 5.0f, 0.0f, 0.5f, 0.5f, false, false, 600),
            new EnergyScene("two box", 0.02f, 0.6f, 0.0f, 0.0f, 4.0f, 0.0f, 0.5f, 0.5f, true, true, 1500),
            new EnergyScene("two box discrete", 0.5f, 0.6f, 0.0f, 0.0f, 4.0f, 0.0f, 0.5f, 0.5f, false, true, 1500),
            new EnergyScene("tall flat frictionless", 0.01f, 0.0f, 0.0f, 0.0f, 10.0f, 0.0f, 0.5f, 1.0f, false, false, 1200),
            new EnergyScene("tall flat friction", 0.01f, 0.6f, 0.0f, 0.0f, 10.0f, 0.0f, 0.5f, 1.0f, false, false, 1200),
            new EnergyScene("narrow flat frictionless", 0.01f, 0.0f, 0.0f, 0.0f, 10.0f, 0.0f, 0.25f, 1.0f, false, false, 1200),
            new EnergyScene("narrow flat friction", 0.01f, 0.6f, 0.0f, 0.0f, 10.0f, 0.0f, 0.25f, 1.0f, false, false, 1200),
        };

        // The soft contact position correction lifts a body that lands inside the ground, which is
        // potential energy from nowhere at any restitution. Continuous collision keeps landings on the
        // surface, so the tolerance is round-off. The discrete scenes land deep and are allowed the lift.
        const float tolerance = 1.0e-3f;
        const float discreteTolerance = 2.0e-2f;

        int failed = 0;

        for (int i = 0; i < scenes.Length; ++i)
        {
            float ratio = MeasurePeakEnergy(scenes[i]);
            float limit = scenes[i].safetyFactor < 0.1f ? tolerance : discreteTolerance;
            if (ratio > 1.0f + limit)
            {
                failed = 1;
            }
        }

        Assert.That(failed, Is.EqualTo(0));
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

    // The sampled approach speed and the Poisson accumulator are per manifold point state, so they must
    // survive the split across workers
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
