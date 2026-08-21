// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using NUnit.Framework;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2DynamicTrees;

namespace Box2D.NET.Test;

public class B2DynamicTreeContextTests
{

    private static bool QueryCallback(int proxyId, ulong userData, ref CallbackContext context)
    {
        context.Visits += 1;
        return true;
    }

    private static float RayCallback(in B2RayCastInput input, int proxyId, ulong userData, ref CallbackContext context)
    {
        context.Visits += 1;
        return 0.0f;
    }

    private static float ShapeCallback(in B2ShapeCastInput input, int proxyId, ulong userData, ref CallbackContext context)
    {
        context.Visits += 1;
        return 0.0f;
    }

    [Test]
    public void QueryRayAndShapeCast_AcceptReferenceTypeContext()
    {
        B2DynamicTree tree = b2DynamicTree_Create(1);
        try
        {
            var bounds = new B2AABB(new B2Vec2(-1.0f, -1.0f), new B2Vec2(1.0f, 1.0f));
            b2DynamicTree_CreateProxy(tree, bounds, ulong.MaxValue, 1);

            var context = new CallbackContext();
            b2DynamicTree_Query(tree, bounds, ulong.MaxValue, QueryCallback, ref context);
            Assert.That(context.Visits, Is.EqualTo(1));

            context.Visits = 0;
            var ray = new B2RayCastInput(new B2Vec2(-2.0f, 0.0f), new B2Vec2(4.0f, 0.0f), 1.0f);
            b2DynamicTree_RayCast(tree, ray, ulong.MaxValue, RayCallback, ref context);
            Assert.That(context.Visits, Is.EqualTo(1));

            context.Visits = 0;
            var point = new B2Vec2(-2.0f, 0.0f);
            var shapeCast = new B2ShapeCastInput
            {
                proxy = b2MakeProxy(point, 1, 0.0f),
                translation = new B2Vec2(4.0f, 0.0f),
                maxFraction = 1.0f,
            };
            b2DynamicTree_ShapeCast(tree, shapeCast, ulong.MaxValue, ShapeCallback, ref context);
            Assert.That(context.Visits, Is.EqualTo(1));
        }
        finally
        {
            b2DynamicTree_Destroy(tree);
        }
    }
}
