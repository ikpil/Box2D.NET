// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using NUnit.Framework;
using System;
using static Box2D.NET.B2DynamicTrees;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET.Test;

public class B2DynamicTreeTest
{
    private const int GRID_COUNT = 20;

    [Test]
    public void TreeCreateDestroy()
    {
        B2AABB a = new B2AABB(
            lowerBound: new B2Vec2(-1.0f, -1.0f),
            upperBound: new B2Vec2(2.0f, 2.0f)
        );

        B2DynamicTree tree = b2DynamicTree_Create();
        b2DynamicTree_CreateProxy(tree, a, 1, 0);

        Assert.That(tree.nodeCount > 0);
        Assert.That(tree.proxyCount == 1);

        b2DynamicTree_Destroy(tree);

        Assert.That(tree.nodeCount == 0);
        Assert.That(tree.proxyCount == 0);
    }

    private static float RayCastCallbackFcn(in B2RayCastInput input, int proxyId, ulong userData, ref int context)
    {
        // (void)input;
        // (void)userData;

        ref int proxyHit = ref context;
        proxyHit = proxyId;
        return 0.0f;
    }

    [Test]
    public void TreeRayCastTest()
    {
        // Test AABB centered at origin with bounds [-1, -1] to [1, 1]
        B2AABB a = new B2AABB(lowerBound: new B2Vec2(-1.0f, -1.0f), upperBound: new B2Vec2(1.0f, 1.0f));
        B2DynamicTree tree = b2DynamicTree_Create();
        int proxyId = b2DynamicTree_CreateProxy(tree, a, 1, 0);

        B2RayCastInput input = new B2RayCastInput();
        input.maxFraction = 1.0f;

        // Test 1: Ray hits AABB from left side
        {
            B2Vec2 p1 = new B2Vec2(-3.0f, 0.0f);
            B2Vec2 p2 = new B2Vec2(3.0f, 0.0f);

            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_RayCast(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == proxyId);
        }

        // Test 2: Ray hits AABB from right side
        {
            B2Vec2 p1 = new B2Vec2(3.0f, 0.0f);
            B2Vec2 p2 = new B2Vec2(-3.0f, 0.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_RayCast(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == proxyId);
        }

        // Test 3: Ray hits AABB from bottom
        {
            B2Vec2 p1 = new B2Vec2(0.0f, -3.0f);
            B2Vec2 p2 = new B2Vec2(0.0f, 3.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_RayCast(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == proxyId);
        }

        // Test 4: Ray hits AABB from top
        {
            B2Vec2 p1 = new B2Vec2(0.0f, 3.0f);
            B2Vec2 p2 = new B2Vec2(0.0f, -3.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_RayCast(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == proxyId);
        }

        // Test 5: Ray misses AABB completely (parallel to x-axis)
        {
            B2Vec2 p1 = new B2Vec2(-3.0f, 2.0f);
            B2Vec2 p2 = new B2Vec2(3.0f, 2.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_RayCast(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == -1);
        }

        // Test 6: Ray misses AABB completely (parallel to y-axis)
        {
            B2Vec2 p1 = new B2Vec2(2.0f, -3.0f);
            B2Vec2 p2 = new B2Vec2(2.0f, 3.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_RayCast(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == -1);
        }

        // Test 7: Ray starts inside AABB
        {
            B2Vec2 p1 = new B2Vec2(0.0f, 0.0f);
            B2Vec2 p2 = new B2Vec2(2.0f, 0.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_RayCast(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == proxyId);
        }

        // Test 8: Ray hits corner of AABB (diagonal ray)
        {
            B2Vec2 p1 = new B2Vec2(-2.0f, -2.0f);
            B2Vec2 p2 = new B2Vec2(2.0f, 2.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_RayCast(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == proxyId);
        }

        // Test 9: Ray parallel to AABB edge but outside
        {
            B2Vec2 p1 = new B2Vec2(-2.0f, 1.5f);
            B2Vec2 p2 = new B2Vec2(2.0f, 1.5f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_RayCast(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == -1);
        }

        // Test 10: Ray parallel to AABB edge and exactly on boundary
        {
            B2Vec2 p1 = new B2Vec2(-2.0f, 1.0f);
            B2Vec2 p2 = new B2Vec2(2.0f, 1.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_RayCast(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == proxyId);
        }

        // Test 11: Very short ray that doesn't reach AABB
        {
            B2Vec2 p1 = new B2Vec2(-3.0f, 0.0f);
            B2Vec2 p2 = new B2Vec2(-2.5f, 0.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_RayCast(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == -1);
        }

        // Test 12: Zero-length ray (degenerate case)
        {
            B2Vec2 p1 = new B2Vec2(0.0f, 0.0f);
            B2Vec2 p2 = new B2Vec2(0.0f, 0.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_RayCast(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == proxyId);
        }

        // Test 13: Ray hits AABB at exact boundary condition (t = 1.0)
        {
            B2Vec2 p1 = new B2Vec2(-2.0f, 0.0f);
            B2Vec2 p2 = new B2Vec2(-1.0f, 0.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_RayCast(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == proxyId);
        }

        b2DynamicTree_Destroy(tree);
    }

    private static bool QueryCollectCallback(int proxyId, ulong userData, ref B2FixedArray32<int> context)
    {
        B2_UNUSED(userData);
        Span<int> @out = context.AsSpan();
        @out[proxyId] = 1;
        return true; // continue the query
    }

    private static bool QueryCollectListCallback(int proxyId, ulong userData, ref int[] context)
    {
        B2_UNUSED(userData);
        Span<int> list = context;
        int count = list[0];
        list[count + 1] = proxyId;
        list[0] = count + 1;
        return true;
    }

    [Test]
    public void TreeMultipleProxiesTest()
    {
        B2DynamicTree tree = b2DynamicTree_Create();

        B2AABB a1 = new B2AABB(lowerBound: new B2Vec2(-5.0f, -1.0f), upperBound: new B2Vec2(-3.0f, 1.0f));
        B2AABB a2 = new B2AABB(lowerBound: new B2Vec2(-1.0f, -1.0f), upperBound: new B2Vec2(1.0f, 1.0f));
        B2AABB a3 = new B2AABB(lowerBound: new B2Vec2(3.0f, -1.0f), upperBound: new B2Vec2(5.0f, 1.0f));

        int id1 = b2DynamicTree_CreateProxy(tree, a1, 0x1ul, 42);
        int id2 = b2DynamicTree_CreateProxy(tree, a2, 0x2ul, 43);
        int id3 = b2DynamicTree_CreateProxy(tree, a3, 0x4ul, 44);

        Assert.That(b2DynamicTree_GetProxyCount(tree) == 3);

        Assert.That(b2DynamicTree_GetUserData(tree, id1) == 42);
        Assert.That(b2DynamicTree_GetUserData(tree, id2) == 43);
        Assert.That(b2DynamicTree_GetUserData(tree, id3) == 44);

        Assert.That(b2DynamicTree_GetCategoryBits(tree, id1) == 0x1ul);
        Assert.That(b2DynamicTree_GetCategoryBits(tree, id2) == 0x2ul);
        Assert.That(b2DynamicTree_GetCategoryBits(tree, id3) == 0x4ul);

        b2DynamicTree_Destroy(tree);
    }

    [Test]
    public void TreeQueryTest()
    {
        B2DynamicTree tree = b2DynamicTree_Create();

        B2AABB a1 = new B2AABB(lowerBound: new B2Vec2(-5.0f, -1.0f), upperBound: new B2Vec2(-3.0f, 1.0f));
        B2AABB a2 = new B2AABB(lowerBound: new B2Vec2(-1.0f, -1.0f), upperBound: new B2Vec2(1.0f, 1.0f));
        B2AABB a3 = new B2AABB(lowerBound: new B2Vec2(3.0f, -1.0f), upperBound: new B2Vec2(5.0f, 1.0f));

        int id1 = b2DynamicTree_CreateProxy(tree, a1, 0xFFul, 0);
        int id2 = b2DynamicTree_CreateProxy(tree, a2, 0xFFul, 0);
        int id3 = b2DynamicTree_CreateProxy(tree, a3, 0xFFul, 0);

        B2AABB queryA = new B2AABB(lowerBound: new B2Vec2(-2.0f, -2.0f), upperBound: new B2Vec2(2.0f, 2.0f));

        B2FixedArray32<int> foundFlags = new B2FixedArray32<int>();
        B2TreeStats stats = b2DynamicTree_Query(tree, queryA, 0xFFFFFFFFul, QueryCollectCallback, ref foundFlags);

        // We expect at least the middle proxy to be visited.
        Assert.That(foundFlags[id2] == 1);
        Assert.That(stats.leafVisits >= 1);

        // Test QueryAll using list collector
        int[] list = new int[16]; // list[0] holds count, following entries are ids
        B2TreeStats allStats = b2DynamicTree_QueryAll(tree, queryA, QueryCollectListCallback, ref list);
        Assert.That(list[0] >= 1); // at least one proxy should be collected
        Assert.That(allStats.leafVisits >= 1);

        b2DynamicTree_Destroy(tree);
        //(void)id1; (void)id3;
    }

    [Test]
    public void TreeMoveAndEnlargeTest()
    {
        B2DynamicTree tree = b2DynamicTree_Create();

        B2AABB a = new B2AABB(lowerBound: new B2Vec2(0.0f, 0.0f), upperBound: new B2Vec2(1.0f, 1.0f));
        int id = b2DynamicTree_CreateProxy(tree, a, 0x1ul, 100);

        // Move proxy to a new place
        B2AABB moved = new B2AABB(lowerBound: new B2Vec2(10.0f, 10.0f), upperBound: new B2Vec2(11.0f, 11.0f));
        b2DynamicTree_MoveProxy(tree, id, moved);

        B2AABB got = b2DynamicTree_GetAABB(tree, id);
        Assert.That(got.lowerBound.X == moved.lowerBound.X);
        Assert.That(got.lowerBound.Y == moved.lowerBound.Y);
        Assert.That(got.upperBound.X == moved.upperBound.X);
        Assert.That(got.upperBound.Y == moved.upperBound.Y);

        // Now enlarge the proxy
        B2AABB enlarge = new B2AABB(lowerBound: new B2Vec2(9.5f, 9.5f), upperBound: new B2Vec2(11.5f, 11.5f));
        b2DynamicTree_EnlargeProxy(tree, id, enlarge);

        B2AABB got2 = b2DynamicTree_GetAABB(tree, id);
        Assert.That(got2.lowerBound.X <= enlarge.lowerBound.X + 1e-6f);
        Assert.That(got2.upperBound.X >= enlarge.upperBound.X - 1e-6f);

        b2DynamicTree_Destroy(tree);
    }

    [Test]
    public void TreeRebuildAndValidateTest()
    {
        B2DynamicTree tree = b2DynamicTree_Create();

        // Create a number of proxies to make rebuild meaningful
        for (int i = 0; i < 12; ++i)
        {
            float x = (float)i * 2.0f;
            B2AABB a = new B2AABB(lowerBound: new B2Vec2(x - 0.5f, -0.5f), upperBound: new B2Vec2(x + 0.5f, 0.5f));
            b2DynamicTree_CreateProxy(tree, a, 0xFFul, (ulong)i);
        }

        int sorted = b2DynamicTree_Rebuild(tree, true);

        Assert.That(sorted, Is.GreaterThanOrEqualTo(0));
        Assert.That(b2DynamicTree_GetByteCount(tree), Is.GreaterThan(0));
        Assert.That(b2DynamicTree_GetHeight(tree), Is.GreaterThan(0));

        b2DynamicTree_Destroy(tree);
    }

    [Test]
    public void TreeRowHeightTest()
    {
        B2DynamicTree tree = b2DynamicTree_Create();

        int columnCount = 200;
        for (int i = 0; i < columnCount; ++i)
        {
            float x = 1.0f * i;
            B2AABB a = new B2AABB(lowerBound: new B2Vec2(x, 0.0f), upperBound: new B2Vec2(x + 1.0f, 1.0f));
            b2DynamicTree_CreateProxy(tree, a, 1, (ulong)i);
        }

        float minHeight = MathF.Log2((float)columnCount);

        Assert.That(b2DynamicTree_GetHeight(tree) < 2.0f * minHeight);

        b2DynamicTree_Destroy(tree);
    }

    [Test]
    public void TreeGridHeightTest()
    {
        B2DynamicTree tree = b2DynamicTree_Create();

        int columnCount = 20;
        int rowCount = 20;
        for (int i = 0; i < columnCount; ++i)
        {
            float x = 1.0f * i;
            for (int j = 0; j < rowCount; ++j)
            {
                float y = 1.0f * j;
                B2AABB a = new B2AABB(lowerBound: new B2Vec2(x, y), upperBound: new B2Vec2(x + 1.0f, y + 1.0f));
                b2DynamicTree_CreateProxy(tree, a, 1, (ulong)i);
            }
        }

        float minHeight = MathF.Log2((float)(rowCount * columnCount));

        Assert.That(b2DynamicTree_GetHeight(tree) < 2.0f * minHeight);

        b2DynamicTree_Destroy(tree);
    }

    [Test]
    public void TreeGridMovementTest()
    {
        B2DynamicTree tree = b2DynamicTree_Create();

        int[] proxyIds = new int[GRID_COUNT * GRID_COUNT];
        int index = 0;
        for (int i = 0; i < GRID_COUNT; ++i)
        {
            float x = 1.0f * i;
            for (int j = 0; j < GRID_COUNT; ++j)
            {
                float y = 1.0f * j;
                B2AABB a = new B2AABB(lowerBound: new B2Vec2(x, y), upperBound: new B2Vec2(x + 1.0f, y + 1.0f));
                proxyIds[index] = b2DynamicTree_CreateProxy(tree, a, 1, (ulong)i);
                index += 1;
            }
        }

        Assert.That(index == GRID_COUNT * GRID_COUNT);

        float minHeight = MathF.Log2((float)(GRID_COUNT * GRID_COUNT));

        int height1 = b2DynamicTree_GetHeight(tree);
        Assert.That(height1 < 2.0f * minHeight);

        B2Vec2 offset = new B2Vec2(10.0f, 20.0f);
        index = 0;
        for (int i = 0; i < GRID_COUNT; ++i)
        {
            for (int j = 0; j < GRID_COUNT; ++j)
            {
                B2AABB a = b2DynamicTree_GetAABB(tree, proxyIds[index]);
                a.lowerBound = b2Add(a.lowerBound, offset);
                a.upperBound = b2Add(a.upperBound, offset);
                b2DynamicTree_MoveProxy(tree, proxyIds[index], a);
                index += 1;
            }
        }

        int height2 = b2DynamicTree_GetHeight(tree);
        Assert.That(height2 < 3.0f * minHeight);

        b2DynamicTree_Rebuild(tree, true);

        int height3 = b2DynamicTree_GetHeight(tree);
        Assert.That(height3 < 2.0f * minHeight);

        b2DynamicTree_Destroy(tree);
    }
}