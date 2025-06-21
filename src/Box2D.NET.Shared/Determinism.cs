// SPDX-FileCopyrightText: 2022 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Timers;
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET.Shared
{
    public static class Determinism
    {
        public static FallingHingeData CreateFallingHinges(B2WorldId worldId)
        {
            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.position = new B2Vec2(0.0f, -1.0f);
                B2BodyId groundId = b2CreateBody(worldId, ref bodyDef);

                B2Polygon box = b2MakeBox(20.0f, 1.0f);
                B2ShapeDef shapeDef = b2DefaultShapeDef();
                b2CreatePolygonShape(groundId, ref shapeDef, ref box);
            }

            int columnCount = 4;
            int rowCount = 30;
            int bodyCount = rowCount * columnCount;

            B2BodyId[] bodyIds = new B2BodyId[bodyCount];

            {
                float h = 0.25f;
                float r = 0.1f * h;
                B2Polygon box = b2MakeRoundedBox(h - r, h - r, r);

                B2ShapeDef shapeDef = b2DefaultShapeDef();
                shapeDef.material.friction = 0.3f;

                float offset = 0.4f * h;
                float dx = 10.0f * h;
                float xroot = -0.5f * dx * (columnCount - 1.0f);

                B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
                jointDef.enableLimit = true;
                jointDef.lowerAngle = -0.1f * B2_PI;
                jointDef.upperAngle = 0.2f * B2_PI;
                jointDef.enableSpring = true;
                jointDef.hertz = 0.5f;
                jointDef.dampingRatio = 0.5f;
                jointDef.@base.localFrameA.p = new B2Vec2(h, h);
                jointDef.@base.localFrameB.p = new B2Vec2(offset, -h);
                jointDef.@base.drawScale = 0.1f;

                int bodyIndex = 0;

                for (int j = 0; j < columnCount; ++j)
                {
                    float x = xroot + j * dx;

                    B2BodyId prevBodyId = b2_nullBodyId;

                    for (int i = 0; i < rowCount; ++i)
                    {
                        B2BodyDef bodyDef = b2DefaultBodyDef();
                        bodyDef.type = B2BodyType.b2_dynamicBody;

                        bodyDef.position.X = x + offset * i;
                        bodyDef.position.Y = h + 2.0f * h * i;

                        // this tests the deterministic cosine and sine functions
                        bodyDef.rotation = b2MakeRot(0.1f * i - 1.0f);

                        B2BodyId bodyId = b2CreateBody(worldId, ref bodyDef);

                        if ((i & 1) == 0)
                        {
                            prevBodyId = bodyId;
                        }
                        else
                        {
                            jointDef.@base.bodyIdA = prevBodyId;
                            jointDef.@base.bodyIdB = bodyId;
                            b2CreateRevoluteJoint(worldId, ref jointDef);
                            prevBodyId = b2_nullBodyId;
                        }

                        b2CreatePolygonShape(bodyId, ref shapeDef, ref box);

                        B2_ASSERT(bodyIndex < bodyCount);
                        bodyIds[bodyIndex] = bodyId;

                        bodyIndex += 1;
                    }
                }

                B2_ASSERT(bodyIndex == bodyCount);
            }

            FallingHingeData data = new FallingHingeData();
            data.bodyIds = bodyIds;
            data.bodyCount = bodyCount;
            data.stepCount = 0;
            data.sleepStep = -1;
            data.hash = 0;
            return data;
        }

        public static bool UpdateFallingHinges(B2WorldId worldId, ref FallingHingeData data)
        {
            if (data.hash == 0)
            {
                B2BodyEvents bodyEvents = b2World_GetBodyEvents(worldId);

                if (bodyEvents.moveCount == 0)
                {
                    int awakeCount = b2World_GetAwakeBodyCount(worldId);
                    B2_ASSERT(awakeCount == 0);

                    data.hash = B2_HASH_INIT;
                    Span<byte> bxf = stackalloc byte[sizeof(float) * 4];
                    for (int i = 0; i < data.bodyCount; ++i)
                    {
                        B2Transform xf = b2Body_GetTransform(data.bodyIds[i]);
                        xf.TryWriteBytes(bxf);
                        data.hash = b2Hash(data.hash, bxf, bxf.Length);
                    }

                    data.sleepStep = data.stepCount;
                }
            }

            data.stepCount += 1;

            return data.hash != 0;
        }

        public static void DestroyFallingHinges(ref FallingHingeData data)
        {
            //free( data.bodyIds );
            data.bodyIds = null;
        }
    }
}