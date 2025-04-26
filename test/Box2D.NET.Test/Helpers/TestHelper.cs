﻿using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Test.Helpers;

public static class TestHelper
{
    public static TestWorldHandle CreateWorld()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.workerCount = 1;
        worldDef.enqueueTask = EnqueueTask;
        worldDef.finishTask = FinishTask;
        worldDef.userTaskContext = null;
        worldDef.enableSleep = true;

        B2WorldId worldId = b2CreateWorld(ref worldDef);

        return new TestWorldHandle(worldId);
    }

    private static object EnqueueTask(b2TaskCallback task, int itemCount, int minRange, object taskContext, object userContext)
    {
        return null;
    }

    private static void FinishTask(object userTask, object userContext)
    {
    }

    public static B2ShapeId CreateCircle(B2WorldId worldId, B2Vec2 position, float radius)
    {
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = position;
        bodyDef.gravityScale = 0.0f;
        var bodyIdA = b2CreateBody(worldId, ref bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        shapeDef.material.friction = 0.5f;

        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), radius);
        B2ShapeId shapeId = b2CreateCircleShape(bodyIdA, ref shapeDef, ref circle);
        
        return shapeId;
    }
}