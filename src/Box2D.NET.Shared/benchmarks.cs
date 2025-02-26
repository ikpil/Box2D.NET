// SPDX-FileCopyrightText: 2022 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using Box2D.NET.Primitives;
using Box2D.NET.Shared.Primitives;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2RevoluteJoints;
using static Box2D.NET.Shared.human;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Cores;

namespace Box2D.NET.Shared
{
    public static class benchmarks
    {
#if NDEBUG
        private const bool BENCHMARK_DEBUG = false;
#else
        private const bool BENCHMARK_DEBUG = true;
#endif
        private const int SPINNER_POINT_COUNT = 360;


        public static void CreateJointGrid(B2WorldId worldId)
        {
            b2World_EnableSleeping(worldId, false);

            int N = BENCHMARK_DEBUG ? 10 : 100;

            // Allocate to avoid huge stack usage
            B2BodyId[] bodies = new B2BodyId[N * N];
            int index = 0;

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 1.0f;
            shapeDef.filter.categoryBits = 2;
            shapeDef.filter.maskBits = ~2u;

            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.4f);

            B2RevoluteJointDef jd = b2DefaultRevoluteJointDef();
            B2BodyDef bodyDef = b2DefaultBodyDef();

            for (int k = 0; k < N; ++k)
            {
                for (int i = 0; i < N; ++i)
                {
                    float fk = (float)k;
                    float fi = (float)i;

                    if (k >= N / 2 - 3 && k <= N / 2 + 3 && i == 0)
                    {
                        bodyDef.type = B2BodyType.b2_staticBody;
                    }
                    else
                    {
                        bodyDef.type = B2BodyType.b2_dynamicBody;
                    }

                    bodyDef.position = new B2Vec2(fk, -fi);

                    B2BodyId body = b2CreateBody(worldId, bodyDef);

                    b2CreateCircleShape(body, shapeDef, circle);

                    if (i > 0)
                    {
                        jd.bodyIdA = bodies[index - 1];
                        jd.bodyIdB = body;
                        jd.localAnchorA = new B2Vec2(0.0f, -0.5f);
                        jd.localAnchorB = new B2Vec2(0.0f, 0.5f);
                        b2CreateRevoluteJoint(worldId, jd);
                    }

                    if (k > 0)
                    {
                        jd.bodyIdA = bodies[index - N];
                        jd.bodyIdB = body;
                        jd.localAnchorA = new B2Vec2(0.5f, 0.0f);
                        jd.localAnchorB = new B2Vec2(-0.5f, 0.0f);
                        b2CreateRevoluteJoint(worldId, jd);
                    }

                    bodies[index++] = body;
                }
            }
        }

        public static void CreateLargePyramid(B2WorldId worldId)
        {
            b2World_EnableSleeping(worldId, false);

            int baseCount = BENCHMARK_DEBUG ? 20 : 100;

            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.position = new B2Vec2(0.0f, -1.0f);
                B2BodyId groundId = b2CreateBody(worldId, bodyDef);

                B2Polygon box = b2MakeBox(100.0f, 1.0f);
                B2ShapeDef shapeDef = b2DefaultShapeDef();
                b2CreatePolygonShape(groundId, shapeDef, box);
            }

            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                bodyDef.enableSleep = false;

                B2ShapeDef shapeDef = b2DefaultShapeDef();
                shapeDef.density = 1.0f;

                float h = 0.5f;
                B2Polygon box = b2MakeSquare(h);

                float shift = 1.0f * h;

                for (int i = 0; i < baseCount; ++i)
                {
                    float y = (2.0f * i + 1.0f) * shift;

                    for (int j = i; j < baseCount; ++j)
                    {
                        float x = (i + 1.0f) * shift + 2.0f * (j - i) * shift - h * baseCount;

                        bodyDef.position = new B2Vec2(x, y);
                        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);
                        b2CreatePolygonShape(bodyId, shapeDef, box);
                    }
                }
            }
        }

        public static void CreateSmallPyramid(B2WorldId worldId, int baseCount, float extent, float centerX, float baseY)
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;

            B2ShapeDef shapeDef = b2DefaultShapeDef();

            B2Polygon box = b2MakeSquare(extent);

            for (int i = 0; i < baseCount; ++i)
            {
                float y = (2.0f * i + 1.0f) * extent + baseY;

                for (int j = i; j < baseCount; ++j)
                {
                    float x = (i + 1.0f) * extent + 2.0f * (j - i) * extent + centerX - 0.5f;
                    bodyDef.position = new B2Vec2(x, y);

                    B2BodyId bodyId = b2CreateBody(worldId, bodyDef);
                    b2CreatePolygonShape(bodyId, shapeDef, box);
                }
            }
        }

        public static void CreateManyPyramids(B2WorldId worldId)
        {
            b2World_EnableSleeping(worldId, false);

            int baseCount = 10;
            float extent = 0.5f;
            int rowCount = BENCHMARK_DEBUG ? 5 : 20;
            int columnCount = BENCHMARK_DEBUG ? 5 : 20;

            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(worldId, bodyDef);

            float groundDeltaY = 2.0f * extent * (baseCount + 1.0f);
            float groundWidth = 2.0f * extent * columnCount * (baseCount + 1.0f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();

            float groundY = 0.0f;

            for (int i = 0; i < rowCount; ++i)
            {
                B2Segment segment = new B2Segment(new B2Vec2(-0.5f * 2.0f * groundWidth, groundY), new B2Vec2(0.5f * 2.0f * groundWidth, groundY));
                b2CreateSegmentShape(groundId, shapeDef, segment);
                groundY += groundDeltaY;
            }

            float baseWidth = 2.0f * extent * baseCount;
            float baseY = 0.0f;

            for (int i = 0; i < rowCount; ++i)
            {
                for (int j = 0; j < columnCount; ++j)
                {
                    float centerX = -0.5f * groundWidth + j * (baseWidth + 2.0f * extent) + extent;
                    CreateSmallPyramid(worldId, baseCount, extent, centerX, baseY);
                }

                baseY += groundDeltaY;
            }
        }


        public static RainData CreateRain(B2WorldId worldId)
        {
            var rainData = new RainData();
            for (int i = 0; i < rainData.groups.Length; ++i)
            {
                rainData.groups[i] = new Group();
                for (int ii = 0; ii < rainData.groups[i].humans.Length; ++ii)
                {
                    rainData.groups[i].humans[ii] = new Human();
                }
            }

            rainData.gridSize = 0.5f;
            rainData.gridCount = BENCHMARK_DEBUG ? 200 : 500;

            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                B2BodyId groundId = b2CreateBody(worldId, bodyDef);

                B2ShapeDef shapeDef = b2DefaultShapeDef();
                float y = 0.0f;
                float width = rainData.gridSize;
                float height = rainData.gridSize;

                for (int i = 0; i < (int)RainConstants.RAIN_ROW_COUNT; ++i)
                {
                    float x = -0.5f * rainData.gridCount * rainData.gridSize;
                    for (int j = 0; j <= rainData.gridCount; ++j)
                    {
                        B2Polygon box = b2MakeOffsetBox(0.5f * width, 0.5f * height, new B2Vec2(x, y), b2Rot_identity);
                        b2CreatePolygonShape(groundId, shapeDef, box);

                        //b2Segment segment = { { x - 0.5f * width, y }, { x + 0.5f * width, y } };
                        //b2CreateSegmentShape( groundId, &shapeDef, &segment );

                        x += rainData.gridSize;
                    }

                    y += 45.0f;
                }
            }

            rainData.columnCount = 0;
            rainData.columnIndex = 0;

            return rainData;
        }

        public static void CreateGroup(RainData rainData, B2WorldId worldId, int rowIndex, int columnIndex)
        {
            Debug.Assert(rowIndex < (int)RainConstants.RAIN_ROW_COUNT && columnIndex < (int)RainConstants.RAIN_COLUMN_COUNT);

            int groupIndex = rowIndex * (int)RainConstants.RAIN_COLUMN_COUNT + columnIndex;

            float span = rainData.gridCount * rainData.gridSize;
            float groupDistance = 1.0f * span / (int)RainConstants.RAIN_COLUMN_COUNT;

            B2Vec2 position;
            position.x = -0.5f * span + groupDistance * (columnIndex + 0.5f);
            position.y = 40.0f + 45.0f * rowIndex;

            float scale = 1.0f;
            float jointFriction = 0.05f;
            float jointHertz = 5.0f;
            float jointDamping = 0.5f;

            for (int i = 0; i < (int)RainConstants.RAIN_GROUP_SIZE; ++i)
            {
                Human human = rainData.groups[groupIndex].humans[i];
                CreateHuman(human, worldId, position, scale, jointFriction, jointHertz, jointDamping, i + 1, null, false);
                position.x += 0.5f;
            }
        }

        public static void DestroyGroup(RainData rainData, int rowIndex, int columnIndex)
        {
            Debug.Assert(rowIndex < (int)RainConstants.RAIN_ROW_COUNT && columnIndex < (int)RainConstants.RAIN_COLUMN_COUNT);

            int groupIndex = rowIndex * (int)RainConstants.RAIN_COLUMN_COUNT + columnIndex;

            for (int i = 0; i < (int)RainConstants.RAIN_GROUP_SIZE; ++i)
            {
                DestroyHuman(rainData.groups[groupIndex].humans[i]);
            }
        }

        public static float StepRain(RainData rainData, B2WorldId worldId, int stepCount)
        {
            int delay = BENCHMARK_DEBUG ? 0x1F : 0x7;

            if ((stepCount & delay) == 0)
            {
                if (rainData.columnCount < (int)RainConstants.RAIN_COLUMN_COUNT)
                {
                    for (int i = 0; i < (int)RainConstants.RAIN_ROW_COUNT; ++i)
                    {
                        CreateGroup(rainData, worldId, i, rainData.columnCount);
                    }

                    rainData.columnCount += 1;
                }
                else
                {
                    for (int i = 0; i < (int)RainConstants.RAIN_ROW_COUNT; ++i)
                    {
                        DestroyGroup(rainData, i, rainData.columnIndex);
                        CreateGroup(rainData, worldId, i, rainData.columnIndex);
                    }

                    rainData.columnIndex = (rainData.columnIndex + 1) % (int)RainConstants.RAIN_COLUMN_COUNT;
                }
            }

            return 0.0f;
        }


        public static SpinnerData CreateSpinner(B2WorldId worldId)
        {
            var spinnerData = new SpinnerData();
            B2BodyId groundId;

            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                groundId = b2CreateBody(worldId, bodyDef);

                B2Vec2[] points = new B2Vec2[SPINNER_POINT_COUNT];

                B2Rot q = b2MakeRot(-2.0f * B2_PI / SPINNER_POINT_COUNT);
                B2Vec2 p = new B2Vec2(40.0f, 0.0f);
                for (int i = 0; i < SPINNER_POINT_COUNT; ++i)
                {
                    points[i] = new B2Vec2(p.x, p.y + 32.0f);
                    p = b2RotateVector(q, p);
                }

                B2SurfaceMaterial material = new B2SurfaceMaterial();
                material.friction = 0.1f;

                B2ChainDef chainDef = b2DefaultChainDef();
                chainDef.points = points;
                chainDef.count = SPINNER_POINT_COUNT;
                chainDef.isLoop = true;
                chainDef.materials = new[] { material };
                chainDef.materialCount = 1;

                b2CreateChain(groundId, chainDef);
            }

            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                bodyDef.position = new B2Vec2(0.0f, 12.0f);
                bodyDef.enableSleep = false;

                B2BodyId spinnerId = b2CreateBody(worldId, bodyDef);

                B2Polygon box = b2MakeRoundedBox(0.4f, 20.0f, 0.2f);
                B2ShapeDef shapeDef = b2DefaultShapeDef();
                shapeDef.friction = 0.0f;
                b2CreatePolygonShape(spinnerId, shapeDef, box);

                float motorSpeed = 5.0f;
                float maxMotorTorque = 40000.0f;
                B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
                jointDef.bodyIdA = groundId;
                jointDef.bodyIdB = spinnerId;
                jointDef.localAnchorA = bodyDef.position;
                jointDef.enableMotor = true;
                jointDef.motorSpeed = motorSpeed;
                jointDef.maxMotorTorque = maxMotorTorque;

                spinnerData.spinnerId = b2CreateRevoluteJoint(worldId, jointDef);
            }

            {
                B2Capsule capsule = new B2Capsule(new B2Vec2(-0.25f, 0.0f), new B2Vec2(0.25f, 0.0f), 0.25f);
                B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.35f);
                B2Polygon square = b2MakeSquare(0.35f);

                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                B2ShapeDef shapeDef = b2DefaultShapeDef();
                shapeDef.friction = 0.1f;
                shapeDef.restitution = 0.1f;
                shapeDef.density = 0.25f;

                int bodyCount = BENCHMARK_DEBUG ? 499 : 3038;

                float x = -24.0f, y = 2.0f;
                for (int i = 0; i < bodyCount; ++i)
                {
                    bodyDef.position = new B2Vec2(x, y);
                    B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

                    int remainder = i % 3;
                    if (remainder == 0)
                    {
                        b2CreateCapsuleShape(bodyId, shapeDef, capsule);
                    }
                    else if (remainder == 1)
                    {
                        b2CreateCircleShape(bodyId, shapeDef, circle);
                    }
                    else if (remainder == 2)
                    {
                        b2CreatePolygonShape(bodyId, shapeDef, square);
                    }

                    x += 1.0f;

                    if (x > 24.0f)
                    {
                        x = -24.0f;
                        y += 1.0f;
                    }
                }
            }

            return spinnerData;
        }

        public static float StepSpinner(SpinnerData spinnerData, B2WorldId worldId, int stepCount)
        {
            B2_UNUSED(worldId);
            B2_UNUSED(stepCount);

            return b2RevoluteJoint_GetAngle(spinnerData.spinnerId);
        }

        public static void CreateSmash(B2WorldId worldId)
        {
            b2World_SetGravity(worldId, b2Vec2_zero);

            {
                B2Polygon box = b2MakeBox(4.0f, 4.0f);

                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                bodyDef.position = new B2Vec2(-20.0f, 0.0f);
                bodyDef.linearVelocity = new B2Vec2(40.0f, 0.0f);
                B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

                B2ShapeDef shapeDef = b2DefaultShapeDef();
                shapeDef.density = 8.0f;
                b2CreatePolygonShape(bodyId, shapeDef, box);
            }

            {
                float d = 0.4f;
                B2Polygon box = b2MakeSquare(0.5f * d);

                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                bodyDef.isAwake = false;

                B2ShapeDef shapeDef = b2DefaultShapeDef();

                int columns = BENCHMARK_DEBUG ? 20 : 120;
                int rows = BENCHMARK_DEBUG ? 10 : 80;

                for (int i = 0; i < columns; ++i)
                {
                    for (int j = 0; j < rows; ++j)
                    {
                        bodyDef.position.x = i * d + 30.0f;
                        bodyDef.position.y = (j - rows / 2.0f) * d;
                        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);
                        b2CreatePolygonShape(bodyId, shapeDef, box);
                    }
                }
            }
        }

        public static void CreateTumbler(B2WorldId worldId)
        {
            B2BodyId groundId;
            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                groundId = b2CreateBody(worldId, bodyDef);
            }

            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                bodyDef.position = new B2Vec2(0.0f, 10.0f);
                B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

                B2ShapeDef shapeDef = b2DefaultShapeDef();
                shapeDef.density = 50.0f;

                B2Polygon polygon;
                polygon = b2MakeOffsetBox(0.5f, 10.0f, new B2Vec2(10.0f, 0.0f), b2Rot_identity);
                b2CreatePolygonShape(bodyId, shapeDef, polygon);
                polygon = b2MakeOffsetBox(0.5f, 10.0f, new B2Vec2(-10.0f, 0.0f), b2Rot_identity);
                b2CreatePolygonShape(bodyId, shapeDef, polygon);
                polygon = b2MakeOffsetBox(10.0f, 0.5f, new B2Vec2(0.0f, 10.0f), b2Rot_identity);
                b2CreatePolygonShape(bodyId, shapeDef, polygon);
                polygon = b2MakeOffsetBox(10.0f, 0.5f, new B2Vec2(0.0f, -10.0f), b2Rot_identity);
                b2CreatePolygonShape(bodyId, shapeDef, polygon);

                float motorSpeed = 25.0f;

                B2RevoluteJointDef jd = b2DefaultRevoluteJointDef();
                jd.bodyIdA = groundId;
                jd.bodyIdB = bodyId;
                jd.localAnchorA = new B2Vec2(0.0f, 10.0f);
                jd.localAnchorB = new B2Vec2(0.0f, 0.0f);
                jd.referenceAngle = 0.0f;
                jd.motorSpeed = (B2_PI / 180.0f) * motorSpeed;
                jd.maxMotorTorque = 1e8f;
                jd.enableMotor = true;

                b2CreateRevoluteJoint(worldId, jd);
            }

            int gridCount = BENCHMARK_DEBUG ? 20 : 45;
            {
                B2Polygon polygon = b2MakeBox(0.125f, 0.125f);
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                B2ShapeDef shapeDef = b2DefaultShapeDef();

                float y = -0.2f * gridCount + 10.0f;
                for (int i = 0; i < gridCount; ++i)
                {
                    float x = -0.2f * gridCount;

                    for (int j = 0; j < gridCount; ++j)
                    {
                        bodyDef.position = new B2Vec2(x, y);
                        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

                        b2CreatePolygonShape(bodyId, shapeDef, polygon);

                        x += 0.4f;
                    }

                    y += 0.4f;
                }
            }
        }
    }
}
