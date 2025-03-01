// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Timers;
using static Box2D.NET.B2Constants;

namespace Box2D.NET.Samples.Samples.Determinisms;

// This sample provides a visual representation of the cross platform determinism unit test.
// The scenario is designed to produce a chaotic result engaging:
// - continuous collision
// - joint limits (approximate atan2)
// - b2MakeRot (approximate sin/cos)
// Once all the bodies go to sleep the step counter and transform hash is emitted which
// can then be transferred to the unit test and tested in GitHub build actions.
// See CrossPlatformTest in the unit tests.

public class FallingHinges : Sample
{
    public const int e_columns = 4;
    public const int e_rows = 30;

    B2BodyId[] m_bodies = new B2BodyId[e_rows * e_columns];
    uint m_hash;
    int m_sleepStep;
    private static readonly int SampleFallingHinges = SampleFactory.Shared.RegisterSample("Determinism", "Falling Hinges", Create);

    private static Sample Create(Settings settings)
    {
        return new FallingHinges(settings);
    }


    public FallingHinges(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            B2.g_camera.m_center = new B2Vec2(0.0f, 7.5f);
            B2.g_camera.m_zoom = 10.0f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(0.0f, -1.0f);
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2Polygon box = b2MakeBox(20.0f, 1.0f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(groundId, ref shapeDef, box);
        }

        for (int i = 0; i < e_rows * e_columns; ++i)
        {
            m_bodies[i] = b2_nullBodyId;
        }

        {
            float h = 0.25f;
            float r = 0.1f * h;
            B2Polygon box = b2MakeRoundedBox(h - r, h - r, r);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.3f;

            float offset = 0.4f * h;
            float dx = 10.0f * h;
            float xroot = -0.5f * dx * (e_columns - 1.0f);

            B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
            jointDef.enableLimit = true;
            jointDef.lowerAngle = -0.1f * B2_PI;
            jointDef.upperAngle = 0.2f * B2_PI;
            jointDef.enableSpring = true;
            jointDef.hertz = 0.5f;
            jointDef.dampingRatio = 0.5f;
            jointDef.localAnchorA = new B2Vec2(h, h);
            jointDef.localAnchorB = new B2Vec2(offset, -h);
            jointDef.drawSize = 0.1f;

            int bodyIndex = 0;
            int bodyCount = e_rows * e_columns;

            for (int j = 0; j < e_columns; ++j)
            {
                float x = xroot + j * dx;

                B2BodyId prevBodyId = b2_nullBodyId;

                for (int i = 0; i < e_rows; ++i)
                {
                    B2BodyDef bodyDef = b2DefaultBodyDef();
                    bodyDef.type = B2BodyType.b2_dynamicBody;

                    bodyDef.position.x = x + offset * i;
                    bodyDef.position.y = h + 2.0f * h * i;

                    // this tests the deterministic cosine and sine functions
                    bodyDef.rotation = b2MakeRot(0.1f * i - 1.0f);

                    B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

                    if ((i & 1) == 0)
                    {
                        prevBodyId = bodyId;
                    }
                    else
                    {
                        jointDef.bodyIdA = prevBodyId;
                        jointDef.bodyIdB = bodyId;
                        b2CreateRevoluteJoint(m_worldId, ref jointDef);
                        prevBodyId = b2_nullBodyId;
                    }

                    b2CreatePolygonShape(bodyId, ref shapeDef, box);

                    Debug.Assert(bodyIndex < bodyCount);
                    m_bodies[bodyIndex] = bodyId;

                    bodyIndex += 1;
                }
            }
        }

        m_hash = 0;
        m_sleepStep = -1;

        //PrintTransforms();
    }

    void PrintTransforms()
    {
        uint hash = B2_HASH_INIT;
        int bodyCount = e_rows * e_columns;
        Span<byte> bxf = stackalloc byte[sizeof(float) * 4];
        for (int i = 0; i < bodyCount; ++i)
        {
            B2Transform xf = b2Body_GetTransform(m_bodies[i]);
            //Console.WriteLine("%d %.9f %.9f %.9f %.9f\n", i, xf.p.x, xf.p.y, xf.q.c, xf.q.s);
            Console.WriteLine($"{i} {xf.p.x:F9} {xf.p.y:F9} {xf.q.c:F9} {xf.q.s:F9}");
            xf.TryWriteBytes(bxf);
            hash = b2Hash(hash, bxf, bxf.Length);
        }

        //Console.WriteLine("hash = 0x%08x\n", hash);
        Console.WriteLine($"hash = 0x{hash:X8}");
    }

    public override void Step(Settings settings)
    {
        base.Step(settings);

        if (m_hash == 0)
        {
            B2BodyEvents bodyEvents = b2World_GetBodyEvents(m_worldId);

            if (bodyEvents.moveCount == 0)
            {
                Span<byte> bxf = stackalloc byte[sizeof(float) * 4];
                uint hash = B2_HASH_INIT;
                int bodyCount = e_rows * e_columns;
                for (int i = 0; i < bodyCount; ++i)
                {
                    B2Transform xf = b2Body_GetTransform(m_bodies[i]);
                    //Console.WriteLine( "%d %.9f %.9f %.9f %.9f\n", i, xf.p.x, xf.p.y, xf.q.c, xf.q.s );
                    xf.TryWriteBytes(bxf);
                    hash = b2Hash(hash, bxf, bxf.Length);
                }

                m_sleepStep = m_stepCount - 1;
                m_hash = hash;
                //Console.WriteLine("sleep step = %d, hash = 0x%08x\n", m_sleepStep, m_hash);
                Console.WriteLine($"sleep step = {m_sleepStep}, hash = 0x{m_hash:X8}");
            }
        }

        B2.g_draw.DrawString(5, m_textLine, "sleep step = %d, hash = 0x%08x", m_sleepStep, m_hash);
        m_textLine += m_textIncrement;
    }
}
