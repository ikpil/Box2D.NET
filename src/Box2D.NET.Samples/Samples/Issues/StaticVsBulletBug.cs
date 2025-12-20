// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Hulls;

namespace Box2D.NET.Samples.Samples.Issues;

public class StaticVsBulletBug : Sample
{
    private static int staticVsBulletBug = SampleFactory.Shared.RegisterSample("Issues", "StaticVsBulletBug", Create);

    private static Sample Create(SampleContext context)
    {
        return new StaticVsBulletBug(context);
    }

    private StaticVsBulletBug(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_context.camera.center = new B2Vec2(48.8525391f, 68.1518555f);
            m_context.camera.zoom = 100.0f * 0.5f;
        }

        {
            B2BodyDef bd = b2DefaultBodyDef();
            bd.type = B2BodyType.b2_dynamicBody; // NOTE(bug): Changing this to b2_staticBody fixes the issue
            B2BodyId staticBodyId = b2CreateBody(m_worldId, bd);

            B2Vec2[] verts = new B2Vec2[]
            {
                new B2Vec2(48.8525391f, 68.1518555f), new B2Vec2(49.1821289f, 68.1152344f), new B2Vec2(68.8476562f, 68.1152344f),
                new B2Vec2(68.8476562f, 70.2392578f), new B2Vec2(48.8525391f, 70.2392578f),
            };

            B2Hull hull = b2ComputeHull(verts, verts.Length);
            B2Polygon poly = b2MakePolygon(ref hull, 0.0f);

            B2ShapeDef sd = b2DefaultShapeDef();
            sd.density = 1.0f;
            sd.material.friction = 0.5f;
            sd.material.restitution = 0.1f;

            b2CreatePolygonShape(staticBodyId, ref sd, ref poly);
            b2Body_SetType(staticBodyId, B2BodyType.b2_staticBody);
        }

        {
            B2BodyDef bd = b2DefaultBodyDef();
            bd.position = new B2Vec2(58.9243050f, 77.5401459f);
            bd.type = B2BodyType.b2_dynamicBody;
            bd.motionLocks.angularZ = true;
            bd.linearVelocity = new B2Vec2(104.868881f, -281.073883f);
            bd.isBullet = true;

            B2BodyId ballBodyId = b2CreateBody(m_worldId, bd);
            B2Circle ball = new B2Circle(center: new B2Vec2(), radius: 0.3f);

            B2ShapeDef ballShape = b2DefaultShapeDef();
            ballShape.density = 3.0f;
            ballShape.material.friction = 0.2f;
            ballShape.material.restitution = 0.9f;

            b2CreateCircleShape(ballBodyId, ballShape, ball);
        }
    }
}