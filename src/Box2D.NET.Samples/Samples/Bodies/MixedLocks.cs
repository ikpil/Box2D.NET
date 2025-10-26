// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Bodies;

// Motion locking can be a bit squishy
public class MixedLocks : Sample
{
    private static readonly int SampleMixedLocks = SampleFactory.Shared.RegisterSample("Bodies", "Mixed Locks", Create);

    private static Sample Create(SampleContext context)
    {
        return new MixedLocks(context);
    }

    public MixedLocks(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_context.camera.center = new B2Vec2(0.0f, 2.5f);
            m_context.camera.zoom = 3.5f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new B2Vec2(-40.0f, 0.0f), new B2Vec2(40.0f, 0.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        {
            B2Polygon box = b2MakeSquare(0.5f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();

            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.position = new B2Vec2(2.0f, 1.0f);
                bodyDef.name = "static";

                B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
                b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
            }

            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                bodyDef.position = new B2Vec2(1.0f, 1.0f);
                bodyDef.name = "free";

                B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
                b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
            }

            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                bodyDef.position = new B2Vec2(1.0f, 3.0f);
                bodyDef.name = "free";

                B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
                b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
            }

            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                bodyDef.position = new B2Vec2(-1.0f, 1.0f);
                bodyDef.motionLocks.angularZ = true;
                bodyDef.name = "angular z";

                B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
                b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
            }

            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                bodyDef.position = new B2Vec2(-2.0f, 2.0f);
                bodyDef.motionLocks.linearX = true;
                bodyDef.name = "linear x";

                B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
                b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
            }

            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                bodyDef.position = new B2Vec2(-1.0f, 2.5f);
                bodyDef.motionLocks.linearY = true;
                bodyDef.motionLocks.angularZ = true;
                bodyDef.name = "lin y ang z";

                B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
                b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
            }

            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                bodyDef.position = new B2Vec2(0.0f, 1.0f);
                bodyDef.motionLocks.linearX = true;
                bodyDef.motionLocks.linearY = true;
                bodyDef.motionLocks.angularZ = true;
                bodyDef.name = "full";

                B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
                b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
            }
        }
    }
}