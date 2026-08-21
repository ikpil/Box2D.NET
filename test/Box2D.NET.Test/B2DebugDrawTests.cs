// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using NUnit.Framework;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

namespace Box2D.NET.Test;

public class B2DebugDrawTests
{

    [Test]
    public void ContactDrawingUsesSelectedBodyAnchor()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.gravity = new B2Vec2(0.0f, -10.0f);
        B2WorldId worldId = b2CreateWorld(worldDef);

        try
        {
            B2BodyDef groundDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(worldId, groundDef);
            B2ShapeDef groundShapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(groundId, groundShapeDef, b2MakeBox(2.0f, 0.5f));

            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(0.0f, 0.9f);
            B2BodyId bodyId = b2CreateBody(worldId, bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 1.0f;
            b2CreatePolygonShape(bodyId, shapeDef, b2MakeBox(0.5f, 0.5f));

            b2World_Step(worldId, 1.0f / 60.0f, 4);

            B2DebugDraw draw = b2DefaultDebugDraw();
            draw.drawShapes = false;
            draw.DrawPointFcn = CapturePoint;
            var capture = new PointCapture();
            draw.context = capture;

            b2World_Draw(worldId, draw);
            Assert.That(capture.Points, Is.Empty, "drawContacts=false must suppress contact points.");

            draw.drawContacts = true;
            draw.drawAnchorA = false;
            b2World_Draw(worldId, draw);
            B2Vec2[] anchorBPoints = capture.Points.ToArray();
            Assert.That(anchorBPoints, Is.Not.Empty);

            capture.Points.Clear();
            draw.drawAnchorA = true;
            b2World_Draw(worldId, draw);
            B2Vec2[] anchorAPoints = capture.Points.ToArray();

            Assert.That(anchorAPoints, Has.Length.EqualTo(anchorBPoints.Length));
            Assert.That(AnyPointDiffers(anchorAPoints, anchorBPoints), Is.True,
                "Anchor A and anchor B should be transformed from their respective body centers.");
        }
        finally
        {
            b2DestroyWorld(worldId);
        }
    }

    [Test]
    public void ContactDrawEnumIsRemovedFromThePublicApi()
    {
        B2DebugDraw draw = b2DefaultDebugDraw();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(draw.drawContacts, Is.False);
            Assert.That(draw.drawAnchorA, Is.False);
            Assert.That(typeof(B2DebugDraw).Assembly.GetType("Box2D.NET.B2ContactDrawType"), Is.Null);
        }));
    }

    private static void CapturePoint(in B2Vec2 point, float size, B2HexColor color, object context)
    {
        ((PointCapture)context).Points.Add(point);
    }

    private static bool AnyPointDiffers(B2Vec2[] pointsA, B2Vec2[] pointsB)
    {
        for (int i = 0; i < pointsA.Length; ++i)
        {
            if (pointsA[i] != pointsB[i])
            {
                return true;
            }
        }

        return false;
    }
}
