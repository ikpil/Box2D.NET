// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Test.Helpers;

public static class B2TestHelper
{
    public static B2ShapeId Circle(B2WorldId worldId, B2Vec2 position, float radius, B2BodyType bodyType)
    {
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = bodyType;
        bodyDef.position = position;
        bodyDef.gravityScale = 0.0f;
        bodyDef.enableSleep = true;
        var bodyId = b2CreateBody(worldId, bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = bodyType == B2BodyType.b2_staticBody ? 0.0f : 1.0f;
        shapeDef.material.friction = 0.5f;

        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), radius);
        B2ShapeId shapeId = b2CreateCircleShape(bodyId, ref shapeDef, ref circle);

        return shapeId;
    }
}