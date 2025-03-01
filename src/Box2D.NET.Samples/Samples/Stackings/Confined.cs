﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Stackings;

public class Confined : Sample
{
    private static readonly int SampleConfined = SampleFactory.Shared.RegisterSample("Stacking", "Confined", Create);
    
    public const int e_gridCount = 25;
    public const int e_maxCount = e_gridCount * e_gridCount;

    int m_row;
    int m_column;
    int m_count;


    private static Sample Create(Settings settings)
    {
        return new Confined(settings);
    }


    public Confined(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            B2.g_camera.m_center = new B2Vec2(0.0f, 10.0f);
            B2.g_camera.m_zoom = 25.0f * 0.5f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Capsule capsule;
            capsule = new B2Capsule(new B2Vec2(-10.5f, 0.0f), new B2Vec2(10.5f, 0.0f), 0.5f);
            b2CreateCapsuleShape(groundId, shapeDef, capsule);
            capsule = new B2Capsule(new B2Vec2(-10.5f, 0.0f), new B2Vec2(-10.5f, 20.5f), 0.5f);
            b2CreateCapsuleShape(groundId, shapeDef, capsule);
            capsule = new B2Capsule(new B2Vec2(10.5f, 0.0f), new B2Vec2(10.5f, 20.5f), 0.5f);
            b2CreateCapsuleShape(groundId, shapeDef, capsule);
            capsule = new B2Capsule(new B2Vec2(-10.5f, 20.5f), new B2Vec2(10.5f, 20.5f), 0.5f);
            b2CreateCapsuleShape(groundId, shapeDef, capsule);
        }

        m_row = 0;
        m_column = 0;
        m_count = 0;

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.gravityScale = 0.0f;

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);

            while (m_count < e_maxCount)
            {
                m_row = 0;
                for (int i = 0; i < e_gridCount; ++i)
                {
                    float x = -8.75f + m_column * 18.0f / e_gridCount;
                    float y = 1.5f + m_row * 18.0f / e_gridCount;

                    bodyDef.position = new B2Vec2(x, y);
                    B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
                    b2CreateCircleShape(bodyId, shapeDef, circle);

                    m_count += 1;
                    m_row += 1;
                }

                m_column += 1;
            }
        }
    }
}
