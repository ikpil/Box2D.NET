// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using NUnit.Framework;
using System;
using static Box2D.NET.B2DynamicTrees;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2AABBs;
using static Box2D.NET.B2Cores;

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

        B2DynamicTree tree = new B2DynamicTree();
        b2CreateDynamicTreeInternal(tree, 16);
        b2CreateTreeProxyInternal(tree, a, 1, 0, false);

        Assert.That(tree.nodeEnd == 2);
        Assert.That(tree.proxyCount == 1);

        b2DestroyDynamicTreeInternal(tree);

        Assert.That(tree.nodeEnd == 0);
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
        B2DynamicTree tree = b2CreateDynamicTree(16);
        int proxyId = b2CreateTreeProxyInternal(tree, a, 1, 0, false);

        B2RayCastInput input = new B2RayCastInput();
        input.maxFraction = 1.0f;

        // Test 1: Ray hits AABB from left side
        {
            B2Vec2 p1 = new B2Vec2(-3.0f, 0.0f);
            B2Vec2 p2 = new B2Vec2(3.0f, 0.0f);

            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_CastRay(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == proxyId);
        }

        // Test 2: Ray hits AABB from right side
        {
            B2Vec2 p1 = new B2Vec2(3.0f, 0.0f);
            B2Vec2 p2 = new B2Vec2(-3.0f, 0.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_CastRay(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == proxyId);
        }

        // Test 3: Ray hits AABB from bottom
        {
            B2Vec2 p1 = new B2Vec2(0.0f, -3.0f);
            B2Vec2 p2 = new B2Vec2(0.0f, 3.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_CastRay(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == proxyId);
        }

        // Test 4: Ray hits AABB from top
        {
            B2Vec2 p1 = new B2Vec2(0.0f, 3.0f);
            B2Vec2 p2 = new B2Vec2(0.0f, -3.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_CastRay(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == proxyId);
        }

        // Test 5: Ray misses AABB completely (parallel to x-axis)
        {
            B2Vec2 p1 = new B2Vec2(-3.0f, 2.0f);
            B2Vec2 p2 = new B2Vec2(3.0f, 2.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_CastRay(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == -1);
        }

        // Test 6: Ray misses AABB completely (parallel to y-axis)
        {
            B2Vec2 p1 = new B2Vec2(2.0f, -3.0f);
            B2Vec2 p2 = new B2Vec2(2.0f, 3.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_CastRay(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == -1);
        }

        // Test 7: Ray starts inside AABB
        {
            B2Vec2 p1 = new B2Vec2(0.0f, 0.0f);
            B2Vec2 p2 = new B2Vec2(2.0f, 0.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_CastRay(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == proxyId);
        }

        // Test 8: Ray hits corner of AABB (diagonal ray)
        {
            B2Vec2 p1 = new B2Vec2(-2.0f, -2.0f);
            B2Vec2 p2 = new B2Vec2(2.0f, 2.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_CastRay(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == proxyId);
        }

        // Test 9: Ray parallel to AABB edge but outside
        {
            B2Vec2 p1 = new B2Vec2(-2.0f, 1.5f);
            B2Vec2 p2 = new B2Vec2(2.0f, 1.5f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_CastRay(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == -1);
        }

        // Test 10: Ray parallel to AABB edge and exactly on boundary
        {
            B2Vec2 p1 = new B2Vec2(-2.0f, 1.0f);
            B2Vec2 p2 = new B2Vec2(2.0f, 1.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_CastRay(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == proxyId);
        }

        // Test 11: Very short ray that doesn't reach AABB
        {
            B2Vec2 p1 = new B2Vec2(-3.0f, 0.0f);
            B2Vec2 p2 = new B2Vec2(-2.5f, 0.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_CastRay(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == -1);
        }

        // Test 12: Zero-length ray (degenerate case)
        {
            B2Vec2 p1 = new B2Vec2(0.0f, 0.0f);
            B2Vec2 p2 = new B2Vec2(0.0f, 0.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_CastRay(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == proxyId);
        }

        // Test 13: Ray hits AABB at exact boundary condition (t = 1.0)
        {
            B2Vec2 p1 = new B2Vec2(-2.0f, 0.0f);
            B2Vec2 p2 = new B2Vec2(-1.0f, 0.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_CastRay(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == proxyId);
        }

        // An off center box catches an axis aligned ray tested against the wrong axis, which the origin
        // centered box above cannot. Two leaves also give the root a child pair.
        B2AABB b = new B2AABB(lowerBound: new B2Vec2(10.0f, 4.0f), upperBound: new B2Vec2(11.0f, 6.0f));
        int proxyIdB = b2CreateTreeProxyInternal(tree, b, 1, 1, false);

        // Test 14: Horizontal ray through the off center box
        {
            B2Vec2 p1 = new B2Vec2(0.0f, 5.0f);
            B2Vec2 p2 = new B2Vec2(21.0f, 5.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_CastRay(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == proxyIdB);
        }

        // Test 15: Vertical ray through the off center box
        {
            B2Vec2 p1 = new B2Vec2(10.5f, 0.0f);
            B2Vec2 p2 = new B2Vec2(10.5f, 21.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_CastRay(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == proxyIdB);
        }

        // Test 16: Horizontal ray passing above the off center box
        {
            B2Vec2 p1 = new B2Vec2(0.0f, 7.0f);
            B2Vec2 p2 = new B2Vec2(21.0f, 7.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_CastRay(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == -1);
        }

        // Test 17: Vertical ray passing beside the off center box
        {
            B2Vec2 p1 = new B2Vec2(12.0f, 0.0f);
            B2Vec2 p2 = new B2Vec2(12.0f, 21.0f);
            input.origin = p1;
            input.translation = b2Sub(p2, p1);

            int proxyHit = -1;
            b2DynamicTree_CastRay(tree, input, 1, RayCastCallbackFcn, ref proxyHit);

            Assert.That(proxyHit == -1);
        }

        b2DestroyDynamicTree(tree);
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
        B2DynamicTree tree = b2CreateDynamicTree(16);

        B2AABB a1 = new B2AABB(lowerBound: new B2Vec2(-5.0f, -1.0f), upperBound: new B2Vec2(-3.0f, 1.0f));
        B2AABB a2 = new B2AABB(lowerBound: new B2Vec2(-1.0f, -1.0f), upperBound: new B2Vec2(1.0f, 1.0f));
        B2AABB a3 = new B2AABB(lowerBound: new B2Vec2(3.0f, -1.0f), upperBound: new B2Vec2(5.0f, 1.0f));

        int id1 = b2CreateTreeProxyInternal(tree, a1, 0x1ul, 42, false);
        int id2 = b2CreateTreeProxyInternal(tree, a2, 0x2ul, 43, false);
        int id3 = b2CreateTreeProxyInternal(tree, a3, 0x4ul, 44, false);

        Assert.That(b2DynamicTree_GetProxyCount(tree) == 3);

        Assert.That(b2DynamicTree_GetUserData(tree, id1) == 42);
        Assert.That(b2DynamicTree_GetUserData(tree, id2) == 43);
        Assert.That(b2DynamicTree_GetUserData(tree, id3) == 44);

        Assert.That(b2DynamicTree_GetCategoryBits(tree, id1) == 0x1ul);
        Assert.That(b2DynamicTree_GetCategoryBits(tree, id2) == 0x2ul);
        Assert.That(b2DynamicTree_GetCategoryBits(tree, id3) == 0x4ul);

        b2DestroyDynamicTree(tree);
    }

    [Test]
    public void TreeQueryTest()
    {
        B2DynamicTree tree = b2CreateDynamicTree(16);

        B2AABB a1 = new B2AABB(lowerBound: new B2Vec2(-5.0f, -1.0f), upperBound: new B2Vec2(-3.0f, 1.0f));
        B2AABB a2 = new B2AABB(lowerBound: new B2Vec2(-1.0f, -1.0f), upperBound: new B2Vec2(1.0f, 1.0f));
        B2AABB a3 = new B2AABB(lowerBound: new B2Vec2(3.0f, -1.0f), upperBound: new B2Vec2(5.0f, 1.0f));

        int id1 = b2CreateTreeProxyInternal(tree, a1, 0xFFul, 0, false);
        int id2 = b2CreateTreeProxyInternal(tree, a2, 0xFFul, 0, false);
        int id3 = b2CreateTreeProxyInternal(tree, a3, 0xFFul, 0, false);

        B2AABB queryA = new B2AABB(lowerBound: new B2Vec2(-2.0f, -2.0f), upperBound: new B2Vec2(2.0f, 2.0f));

        B2FixedArray32<int> foundFlags = new B2FixedArray32<int>();
        B2TreeStats stats = b2DynamicTree_OverlapAABB(tree, queryA, 0xFFFFFFFFul, QueryCollectCallback, ref foundFlags);

        // We expect at least the middle proxy to be visited.
        Assert.That(foundFlags[id2] == 1);
        Assert.That(stats.leafVisits >= 1);

        // Test QueryAll using list collector
        int[] list = new int[16]; // list[0] holds count, following entries are ids
        B2TreeStats allStats = b2DynamicTree_OverlapAABBAll(tree, queryA, QueryCollectListCallback, ref list);
        Assert.That(list[0] >= 1); // at least one proxy should be collected
        Assert.That(allStats.leafVisits >= 1);

        b2DestroyDynamicTree(tree);
        //(void)id1; (void)id3;
    }

    [Test]
    public void TreeMoveAndEnlargeTest()
    {
        B2DynamicTree tree = b2CreateDynamicTree(16);

        B2AABB a = new B2AABB(lowerBound: new B2Vec2(0.0f, 0.0f), upperBound: new B2Vec2(1.0f, 1.0f));
        int id = b2CreateTreeProxyInternal(tree, a, 0x1ul, 100, false);

        // Move proxy to a new place
        B2AABB moved = new B2AABB(lowerBound: new B2Vec2(10.0f, 10.0f), upperBound: new B2Vec2(11.0f, 11.0f));
        b2DynamicTree_MoveProxyInternal(tree, id, moved, false);

        B2AABB got = b2DynamicTree_GetProxyBounds(tree, id);
        Assert.That(got.lowerBound.X == moved.lowerBound.X);
        Assert.That(got.lowerBound.Y == moved.lowerBound.Y);
        Assert.That(got.upperBound.X == moved.upperBound.X);
        Assert.That(got.upperBound.Y == moved.upperBound.Y);

        // Now enlarge the proxy
        B2AABB enlarge = new B2AABB(lowerBound: new B2Vec2(9.5f, 9.5f), upperBound: new B2Vec2(11.5f, 11.5f));
        b2DynamicTree_EnlargeProxy(tree, id, enlarge);

        B2AABB got2 = b2DynamicTree_GetProxyBounds(tree, id);
        Assert.That(got2.lowerBound.X <= enlarge.lowerBound.X + 1e-6f);
        Assert.That(got2.upperBound.X >= enlarge.upperBound.X - 1e-6f);

        b2DestroyDynamicTree(tree);
    }

    [Test]
    public void TreeRebuildAndValidateTest()
    {
        B2DynamicTree tree = b2CreateDynamicTree(16);

        // Create a number of proxies to make rebuild meaningful
        for (int i = 0; i < 12; ++i)
        {
            float x = (float)i * 2.0f;
            B2AABB a = new B2AABB(lowerBound: new B2Vec2(x - 0.5f, -0.5f), upperBound: new B2Vec2(x + 0.5f, 0.5f));
            b2CreateTreeProxyInternal(tree, a, 0xFFul, (ulong)i, false);
        }

        int sorted = b2DynamicTree_Rebuild(tree, true);

        Assert.That(sorted, Is.GreaterThanOrEqualTo(0));
        Assert.That(b2DynamicTree_GetByteCount(tree), Is.GreaterThan(0));
        Assert.That(b2DynamicTree_GetHeight(tree), Is.GreaterThan(0));

        b2DestroyDynamicTree(tree);
    }

    [Test]
    public void TreeRowHeightTest()
    {
        B2DynamicTree tree = b2CreateDynamicTree(16);

        int columnCount = 200;
        for (int i = 0; i < columnCount; ++i)
        {
            float x = 1.0f * i;
            B2AABB a = new B2AABB(lowerBound: new B2Vec2(x, 0.0f), upperBound: new B2Vec2(x + 1.0f, 1.0f));
            b2CreateTreeProxyInternal(tree, a, 1, (ulong)i, false);
        }

        float minHeight = MathF.Log2((float)columnCount);

        b2DynamicTree_Validate(tree);
        Assert.That(b2DynamicTree_GetHeight(tree) < 2.0f * minHeight);

        b2DestroyDynamicTree(tree);
    }

    [Test]
    public void TreeGridHeightTest()
    {
        B2DynamicTree tree = b2CreateDynamicTree(16);

        int columnCount = 20;
        int rowCount = 20;
        for (int i = 0; i < columnCount; ++i)
        {
            float x = 1.0f * i;
            for (int j = 0; j < rowCount; ++j)
            {
                float y = 1.0f * j;
                B2AABB a = new B2AABB(lowerBound: new B2Vec2(x, y), upperBound: new B2Vec2(x + 1.0f, y + 1.0f));
                b2CreateTreeProxyInternal(tree, a, 1, (ulong)i, false);
            }
        }

        float minHeight = MathF.Log2((float)(rowCount * columnCount));

        b2DynamicTree_Validate(tree);
        Assert.That(b2DynamicTree_GetHeight(tree) < 2.0f * minHeight);

        b2DestroyDynamicTree(tree);
    }

    [Test]
    public void TreeGridMovementTest()
    {
        B2DynamicTree tree = b2CreateDynamicTree(16);

        int[] proxyIds = new int[GRID_COUNT * GRID_COUNT];
        int index = 0;
        for (int i = 0; i < GRID_COUNT; ++i)
        {
            float x = 1.0f * i;
            for (int j = 0; j < GRID_COUNT; ++j)
            {
                float y = 1.0f * j;
                B2AABB a = new B2AABB(lowerBound: new B2Vec2(x, y), upperBound: new B2Vec2(x + 1.0f, y + 1.0f));
                proxyIds[index] = b2CreateTreeProxyInternal(tree, a, 1, (ulong)i, false);
                index += 1;
            }
        }

        Assert.That(index == GRID_COUNT * GRID_COUNT);

        float minHeight = MathF.Log2((float)(GRID_COUNT * GRID_COUNT));

        b2DynamicTree_Validate(tree);
        int height1 = b2DynamicTree_GetHeight(tree);
        Assert.That(height1 < 2.0f * minHeight);

        B2Vec2 offset = new B2Vec2(10.0f, 20.0f);
        index = 0;
        for (int i = 0; i < GRID_COUNT; ++i)
        {
            for (int j = 0; j < GRID_COUNT; ++j)
            {
                B2AABB a = b2DynamicTree_GetProxyBounds(tree, proxyIds[index]);
                a.lowerBound = b2Add(a.lowerBound, offset);
                a.upperBound = b2Add(a.upperBound, offset);
                b2DynamicTree_MoveProxyInternal(tree, proxyIds[index], a, false);
                index += 1;
            }
        }

        b2DynamicTree_Validate(tree);
        int height2 = b2DynamicTree_GetHeight(tree);
        Assert.That(height2 < 3.0f * minHeight);

        b2DynamicTree_Rebuild(tree, true);

        b2DynamicTree_Validate(tree);
        int height3 = b2DynamicTree_GetHeight(tree);
        Assert.That(height3 < 2.0f * minHeight);

        b2DestroyDynamicTree(tree);
    }

    private const int TREE_TEST_PROXY_COUNT = 200;

    // Small generator so the random tests repeat run to run
    private uint treeRandomState = 12345u;

    private float RandomTreeFloat(float lower, float upper)
    {
        treeRandomState = 1664525u * treeRandomState + 1013904223u;
        float unit = (float)(treeRandomState >> 8) * (1.0f / 16777216.0f);
        return lower + (upper - lower) * unit;
    }

    private B2AABB RandomTreeBox(float maxHalfExtent)
    {
        float x = RandomTreeFloat(-50.0f, 50.0f);
        float y = RandomTreeFloat(-50.0f, 50.0f);
        float hx = RandomTreeFloat(0.25f, maxHalfExtent);
        float hy = RandomTreeFloat(0.25f, maxHalfExtent);
        return new B2AABB(new B2Vec2(x - hx, y - hy), new B2Vec2(x + hx, y + hy));
    }

    private static bool BoxEquals(B2AABB a, B2AABB b)
    {
        return a.lowerBound.X == b.lowerBound.X && a.lowerBound.Y == b.lowerBound.Y && a.upperBound.X == b.upperBound.X &&
               a.upperBound.Y == b.upperBound.Y;
    }

    private static B2AABB EnlargeTreeBox(B2AABB a, float margin)
    {
        return new B2AABB(new B2Vec2(a.lowerBound.X - margin, a.lowerBound.Y - margin), new B2Vec2(a.upperBound.X + margin, a.upperBound.Y + margin));
    }

    private class TreeHitSet
    {
        public bool[] hit = new bool[TREE_TEST_PROXY_COUNT];
        public int count;
        public bool bad;
    }

    private static bool CollectHitCallback(int proxyId, ulong userData, ref TreeHitSet hits)
    {
        if (proxyId < 0 || TREE_TEST_PROXY_COUNT <= proxyId || hits.hit[proxyId])
        {
            // Out of range or reported twice
            hits.bad = true;
            return true;
        }

        hits.hit[proxyId] = true;
        hits.count += 1;
        return true;
    }

    // Query the tree and compare against every live proxy box
    private static void CompareQuery(B2DynamicTree tree, int[] proxyIds, int proxyCount, B2AABB box)
    {
        TreeHitSet hits = new TreeHitSet();
        b2DynamicTree_OverlapAABB(tree, box, B2_DEFAULT_MASK_BITS, CollectHitCallback, ref hits);
        Assert.That(hits.bad, Is.False);

        int expectedCount = 0;
        for (int i = 0; i < proxyCount; ++i)
        {
            int proxyId = proxyIds[i];
            bool overlaps = b2AABB_Overlaps(b2DynamicTree_GetProxyBounds(tree, proxyId), box);
            Assert.That(hits.hit[proxyId], Is.EqualTo(overlaps));
            expectedCount += overlaps ? 1 : 0;
        }

        Assert.That(hits.count, Is.EqualTo(expectedCount));
    }

    private class TreeCastHit
    {
        public B2DynamicTree tree;
        public int proxyId;
        public float fraction;

        public TreeCastHit(B2DynamicTree tree, int proxyId, float fraction)
        {
            this.tree = tree;
            this.proxyId = proxyId;
            this.fraction = fraction;
        }
    }

    // Entry fraction along the full translation, negative for a miss
    private static float RayBoxFraction(B2AABB box, B2Vec2 origin, B2Vec2 translation)
    {
        B2CastOutput output = b2AABB_RayCast(box, origin, b2Add(origin, translation));
        return output.hit ? output.fraction : -1.0f;
    }

    // A box sweep against a box is a ray against the box grown by the swept extents
    private static float BoxCastFraction(B2AABB box, in B2BoxCastInput input)
    {
        B2Vec2 extents = b2AABB_Extents(input.box);
        B2AABB grown = new B2AABB(b2Sub(box.lowerBound, extents), b2Add(box.upperBound, extents));
        return RayBoxFraction(grown, b2AABB_Center(input.box), input.translation);
    }

    // The fraction is computed against the full translation rather than the clipped input so it
    // matches the brute force bit for bit
    private static float ClosestRayCallback(in B2RayCastInput input, int proxyId, ulong userData, ref TreeCastHit hit)
    {
        float fraction = RayBoxFraction(b2DynamicTree_GetProxyBounds(hit.tree, proxyId), input.origin, input.translation);
        if (fraction < 0.0f)
        {
            return -1.0f;
        }

        if (fraction < hit.fraction)
        {
            hit.proxyId = proxyId;
            hit.fraction = fraction;
        }

        return fraction;
    }

    private static float ClosestBoxCallback(in B2BoxCastInput input, int proxyId, ulong userData, ref TreeCastHit hit)
    {
        float fraction = BoxCastFraction(b2DynamicTree_GetProxyBounds(hit.tree, proxyId), input);
        if (fraction < 0.0f)
        {
            return -1.0f;
        }

        if (fraction < hit.fraction)
        {
            hit.proxyId = proxyId;
            hit.fraction = fraction;
        }

        return fraction;
    }

    // Closest ray hit from the tree against the closest over every live proxy
    private static void CompareRayCast(B2DynamicTree tree, int[] proxyIds, int proxyCount, B2Vec2 origin, B2Vec2 translation)
    {
        TreeCastHit hit = new TreeCastHit(tree, B2_NULL_INDEX, 1.0f);
        B2RayCastInput input = new B2RayCastInput(origin, translation, 1.0f);
        b2DynamicTree_CastRay(tree, input, B2_DEFAULT_MASK_BITS, ClosestRayCallback, ref hit);

        int bestId = B2_NULL_INDEX;
        float bestFraction = 1.0f;
        bool unique = true;
        for (int i = 0; i < proxyCount; ++i)
        {
            int proxyId = proxyIds[i];
            float fraction = RayBoxFraction(b2DynamicTree_GetProxyBounds(tree, proxyId), origin, translation);
            if (fraction < 0.0f)
            {
                continue;
            }

            if (fraction < bestFraction)
            {
                bestId = proxyId;
                bestFraction = fraction;
                unique = true;
            }
            else if (fraction == bestFraction)
            {
                unique = false;
            }
        }

        Assert.That(hit.fraction, Is.EqualTo(bestFraction));
        if (unique)
        {
            Assert.That(hit.proxyId, Is.EqualTo(bestId));
        }
    }

    private static void CompareBoxCast(B2DynamicTree tree, int[] proxyIds, int proxyCount, B2AABB box, B2Vec2 translation)
    {
        TreeCastHit hit = new TreeCastHit(tree, B2_NULL_INDEX, 1.0f);
        B2BoxCastInput input = new B2BoxCastInput();
        input.box = box;
        input.translation = translation;
        input.maxFraction = 1.0f;
        b2DynamicTree_CastBox(tree, input, B2_DEFAULT_MASK_BITS, ClosestBoxCallback, ref hit);

        int bestId = B2_NULL_INDEX;
        float bestFraction = 1.0f;
        bool unique = true;
        for (int i = 0; i < proxyCount; ++i)
        {
            int proxyId = proxyIds[i];
            float fraction = BoxCastFraction(b2DynamicTree_GetProxyBounds(tree, proxyId), input);
            if (fraction < 0.0f)
            {
                continue;
            }

            if (fraction < bestFraction)
            {
                bestId = proxyId;
                bestFraction = fraction;
                unique = true;
            }
            else if (fraction == bestFraction)
            {
                unique = false;
            }
        }

        Assert.That(hit.fraction, Is.EqualTo(bestFraction));
        if (unique)
        {
            Assert.That(hit.proxyId, Is.EqualTo(bestId));
        }
    }

    // A node is marked exactly when a leaf below it is marked. Leaves are checked against the
    // set the test marked itself.
    private static void CheckMarks(B2DynamicTree tree, int nodeIndex, bool[] markedProxies, out bool markedBelow)
    {
        ref readonly B2TreeNode node = ref tree.nodes[nodeIndex];
        bool marked = b2IsNodeMoved(node);
        if (b2IsLeaf(node))
        {
            Assert.That(marked, Is.EqualTo(markedProxies[b2GetProxyId(node)]));
        }
        else
        {
            int pair = b2GetLeftChild(node);
            CheckMarks(tree, pair, markedProxies, out bool below1);
            CheckMarks(tree, pair + 1, markedProxies, out bool below2);
            Assert.That(marked, Is.EqualTo(below1 || below2));
        }

        markedBelow = marked;
    }

    // Every internal node holds exactly the union of its children
    private static void CheckBounds(B2DynamicTree tree, int nodeIndex, out B2AABB bounds)
    {
        ref readonly B2TreeNode node = ref tree.nodes[nodeIndex];
        if (b2IsLeaf(node) == false)
        {
            int pair = b2GetLeftChild(node);
            CheckBounds(tree, pair, out B2AABB below1);
            CheckBounds(tree, pair + 1, out B2AABB below2);
            Assert.That(BoxEquals(b2AABB_Union(below1, below2), node.aabb));
        }

        bounds = node.aabb;
    }

    private static bool RootMarked(B2DynamicTree tree)
    {
        return b2HasTreeMoved(tree);
    }

    [Test]
    public void TreeOneProxyTest()
    {
        B2DynamicTree tree = b2CreateDynamicTree(1);
        B2AABB box = new B2AABB(new B2Vec2(0.0f, 0.0f), new B2Vec2(1.0f, 1.0f));
        int proxyId = b2CreateTreeProxyInternal(tree, box, 1, 7, false);
        int[] proxyIds = { proxyId };
        b2DynamicTree_Validate(tree);

        // One proxy is a leaf at the root beside the empty node
        Assert.That(tree.nodeEnd, Is.EqualTo(2));
        Assert.That(tree.proxyCount, Is.EqualTo(1));
        Assert.That(b2IsLeaf(tree.nodes[B2_ROOT_NODE]));
        Assert.That(b2DynamicTree_GetHeight(tree), Is.EqualTo(0));
        Assert.That(b2DynamicTree_GetUserData(tree, proxyId), Is.EqualTo(7));
        Assert.That(b2DynamicTree_GetCategoryBits(tree, proxyId), Is.EqualTo(1));
        Assert.That(BoxEquals(b2DynamicTree_GetProxyBounds(tree, proxyId), box));
        Assert.That(BoxEquals(b2DynamicTree_GetRootBounds(tree), box));

        B2AABB inside = new B2AABB(new B2Vec2(0.4f, 0.4f), new B2Vec2(0.6f, 0.6f));
        B2AABB outside = new B2AABB(new B2Vec2(5.0f, 5.0f), new B2Vec2(6.0f, 6.0f));
        CompareQuery(tree, proxyIds, 1, inside);
        CompareQuery(tree, proxyIds, 1, outside);

        B2Vec2 origin = new B2Vec2(-1.0f, 0.5f);
        B2Vec2 translation = new B2Vec2(3.0f, 0.0f);
        TreeCastHit hit = new TreeCastHit(tree, B2_NULL_INDEX, 1.0f);
        B2RayCastInput input = new B2RayCastInput(origin, translation, 1.0f);
        b2DynamicTree_CastRay(tree, input, B2_DEFAULT_MASK_BITS, ClosestRayCallback, ref hit);
        Assert.That(hit.proxyId, Is.EqualTo(proxyId));
        CompareRayCast(tree, proxyIds, 1, origin, translation);

        B2Vec2 missOrigin = new B2Vec2(-1.0f, 5.0f);
        CompareRayCast(tree, proxyIds, 1, missOrigin, translation);

        B2AABB castBox = new B2AABB(new B2Vec2(-3.0f, 0.25f), new B2Vec2(-2.0f, 0.75f));
        B2Vec2 castTranslation = new B2Vec2(10.0f, 0.0f);
        CompareBoxCast(tree, proxyIds, 1, castBox, castTranslation);

        // A full build keeps the leaf at the root
        Assert.That(b2DynamicTree_Rebuild(tree, true), Is.EqualTo(1));
        b2DynamicTree_Validate(tree);
        Assert.That(tree.nodeEnd, Is.EqualTo(2));
        Assert.That(tree.dfsOrdered);
        CompareQuery(tree, proxyIds, 1, inside);

        // The leaf is the root, so a mark and refit show up in the root bounds directly
        B2AABB enlarged = EnlargeTreeBox(box, 0.5f);
        b2DynamicTree_MarkProxyMoved(tree, proxyId, enlarged);
        Assert.That(RootMarked(tree));
        b2DynamicTree_Refit(tree);
        b2DynamicTree_Validate(tree);
        Assert.That(BoxEquals(b2DynamicTree_GetProxyBounds(tree, proxyId), enlarged));
        Assert.That(BoxEquals(b2DynamicTree_GetRootBounds(tree), enlarged));
        Assert.That(b2DynamicTree_Rebuild(tree, false), Is.EqualTo(1));
        Assert.That(RootMarked(tree), Is.False);
        b2DynamicTree_ValidateNoMoved(tree);

        b2DestroyTreeProxy(tree, proxyId);
        b2DynamicTree_Validate(tree);
        Assert.That(tree.proxyCount, Is.EqualTo(0));
        Assert.That(tree.nodeEnd, Is.EqualTo(2));
        Assert.That(b2IsEmptyNode(tree.nodes[B2_ROOT_NODE]));
        Assert.That(b2DynamicTree_GetHeight(tree), Is.EqualTo(0));
        CompareQuery(tree, proxyIds, 0, inside);

        b2DestroyDynamicTree(tree);
    }

    // Removing down to one proxy puts the survivor at the root as a leaf, the next insert makes a
    // pair for the two of them from the hole, and a full build compacts the holes.
    [Test]
    public void TreeLeafRootTest()
    {
        B2DynamicTree tree = b2CreateDynamicTree(16);
        B2AABB boxA = new B2AABB(new B2Vec2(0.0f, 0.0f), new B2Vec2(1.0f, 1.0f));
        B2AABB boxB = new B2AABB(new B2Vec2(10.0f, 0.0f), new B2Vec2(11.0f, 1.0f));
        B2AABB boxC = new B2AABB(new B2Vec2(20.0f, 0.0f), new B2Vec2(21.0f, 1.0f));
        B2AABB boxD = new B2AABB(new B2Vec2(30.0f, 0.0f), new B2Vec2(31.0f, 1.0f));
        B2AABB all = new B2AABB(new B2Vec2(-1.0f, -1.0f), new B2Vec2(40.0f, 2.0f));

        int idA = b2CreateTreeProxyInternal(tree, boxA, 1, 0, false);
        int idB = b2CreateTreeProxyInternal(tree, boxB, 1, 1, false);
        b2DynamicTree_Validate(tree);
        Assert.That(tree.nodeEnd, Is.EqualTo(4));
        Assert.That(tree.proxyCount, Is.EqualTo(2));
        Assert.That(b2IsLeaf(tree.nodes[B2_ROOT_NODE]), Is.False);
        Assert.That(b2DynamicTree_GetHeight(tree), Is.EqualTo(1));

        b2DestroyTreeProxy(tree, idA);
        b2DynamicTree_Validate(tree);
        Assert.That(b2IsLeaf(tree.nodes[B2_ROOT_NODE]));
        Assert.That(tree.nodeEnd, Is.EqualTo(4));
        Assert.That(tree.pairFreeList, Is.EqualTo(2));
        Assert.That(tree.proxyCount, Is.EqualTo(1));
        int[] idsB = { idB };
        CompareQuery(tree, idsB, 1, all);
        CompareQuery(tree, idsB, 1, boxA);

        // The freed pair is reused
        int idC = b2CreateTreeProxyInternal(tree, boxC, 1, 2, false);
        b2DynamicTree_Validate(tree);
        Assert.That(tree.nodeEnd, Is.EqualTo(4));
        Assert.That(tree.pairFreeList, Is.EqualTo(B2_NULL_INDEX));
        Assert.That(tree.proxyCount, Is.EqualTo(2));
        int[] idsBC = { idB, idC };
        CompareQuery(tree, idsBC, 2, all);

        // A third proxy extends the array
        int idD = b2CreateTreeProxyInternal(tree, boxD, 1, 3, false);
        b2DynamicTree_Validate(tree);
        Assert.That(tree.nodeEnd, Is.EqualTo(6));
        Assert.That(tree.proxyCount, Is.EqualTo(3));
        int[] idsBCD = { idB, idC, idD };
        CompareQuery(tree, idsBCD, 3, all);
        CompareQuery(tree, idsBCD, 3, boxC);

        // A remove leaves a hole that the full build compacts
        b2DestroyTreeProxy(tree, idB);
        b2DynamicTree_Validate(tree);
        Assert.That(tree.nodeEnd, Is.EqualTo(6));
        Assert.That(tree.pairFreeList, Is.Not.EqualTo(B2_NULL_INDEX));
        int[] idsCD = { idC, idD };
        CompareQuery(tree, idsCD, 2, all);
        Assert.That(b2DynamicTree_Rebuild(tree, true), Is.EqualTo(2));
        b2DynamicTree_Validate(tree);
        Assert.That(tree.nodeEnd, Is.EqualTo(4));
        Assert.That(tree.pairFreeList, Is.EqualTo(B2_NULL_INDEX));
        CompareQuery(tree, idsCD, 2, all);

        b2DestroyTreeProxy(tree, idC);
        b2DynamicTree_Validate(tree);
        Assert.That(b2IsLeaf(tree.nodes[B2_ROOT_NODE]));

        b2DestroyTreeProxy(tree, idD);
        b2DynamicTree_Validate(tree);
        Assert.That(tree.proxyCount, Is.EqualTo(0));
        Assert.That(b2IsEmptyNode(tree.nodes[B2_ROOT_NODE]));
        Assert.That(b2DynamicTree_GetHeight(tree), Is.EqualTo(0));
        CompareQuery(tree, idsBCD, 0, all);

        b2DestroyDynamicTree(tree);
    }

    [Test]
    public void TreeQueryBruteForceTest()
    {
        B2DynamicTree tree = b2CreateDynamicTree(TREE_TEST_PROXY_COUNT);
        int[] proxyIds = new int[TREE_TEST_PROXY_COUNT];
        for (int i = 0; i < TREE_TEST_PROXY_COUNT; ++i)
        {
            proxyIds[i] = b2CreateTreeProxyInternal(tree, RandomTreeBox(2.0f), 1, (ulong)i, false);
        }

        b2DynamicTree_Validate(tree);
        for (int i = 0; i < 32; ++i)
        {
            CompareQuery(tree, proxyIds, TREE_TEST_PROXY_COUNT, RandomTreeBox(8.0f));
        }

        Assert.That(b2DynamicTree_Rebuild(tree, true), Is.EqualTo(TREE_TEST_PROXY_COUNT));
        b2DynamicTree_Validate(tree);
        for (int i = 0; i < 32; ++i)
        {
            CompareQuery(tree, proxyIds, TREE_TEST_PROXY_COUNT, RandomTreeBox(8.0f));
        }

        // Moves leave the built order behind
        for (int i = 0; i < TREE_TEST_PROXY_COUNT; i += 2)
        {
            b2DynamicTree_MoveProxyInternal(tree, proxyIds[i], RandomTreeBox(2.0f), false);
        }

        b2DynamicTree_Validate(tree);
        for (int i = 0; i < 32; ++i)
        {
            CompareQuery(tree, proxyIds, TREE_TEST_PROXY_COUNT, RandomTreeBox(8.0f));
        }

        b2DestroyDynamicTree(tree);
    }

    [Test]
    public void TreeCastBruteForceTest()
    {
        B2DynamicTree tree = b2CreateDynamicTree(TREE_TEST_PROXY_COUNT);
        int[] proxyIds = new int[TREE_TEST_PROXY_COUNT];
        for (int i = 0; i < TREE_TEST_PROXY_COUNT; ++i)
        {
            proxyIds[i] = b2CreateTreeProxyInternal(tree, RandomTreeBox(2.0f), 1, (ulong)i, false);
        }

        Assert.That(b2DynamicTree_Rebuild(tree, true), Is.EqualTo(TREE_TEST_PROXY_COUNT));
        b2DynamicTree_Validate(tree);

        for (int i = 0; i < 64; ++i)
        {
            B2Vec2 origin = new B2Vec2(RandomTreeFloat(-60.0f, 60.0f), RandomTreeFloat(-60.0f, 60.0f));
            float angle = RandomTreeFloat(-B2_PI, B2_PI);
            float length = RandomTreeFloat(40.0f, 120.0f);
            B2Vec2 translation = new B2Vec2(length * MathF.Cos(angle), length * MathF.Sin(angle));
            CompareRayCast(tree, proxyIds, TREE_TEST_PROXY_COUNT, origin, translation);

            B2Vec2 halfExtent = new B2Vec2(RandomTreeFloat(0.25f, 2.0f), RandomTreeFloat(0.25f, 2.0f));
            B2AABB box = new B2AABB(b2Sub(origin, halfExtent), b2Add(origin, halfExtent));
            CompareBoxCast(tree, proxyIds, TREE_TEST_PROXY_COUNT, box, translation);
        }

        b2DestroyDynamicTree(tree);
    }

    // Marks reach the root from any leaf, the sweep refit writes exact unions, and the rebuild clears
    [Test]
    public void TreeMarkAndRefitTest()
    {
        B2DynamicTree tree = b2CreateDynamicTree(TREE_TEST_PROXY_COUNT);
        int[] proxyIds = new int[TREE_TEST_PROXY_COUNT];
        for (int i = 0; i < TREE_TEST_PROXY_COUNT; ++i)
        {
            proxyIds[i] = b2CreateTreeProxyInternal(tree, RandomTreeBox(2.0f), 1, (ulong)i, false);
        }

        Assert.That(b2DynamicTree_Rebuild(tree, true), Is.EqualTo(TREE_TEST_PROXY_COUNT));
        Assert.That(tree.dfsOrdered);
        Assert.That(tree.nodeEnd, Is.EqualTo(2 * TREE_TEST_PROXY_COUNT));
        Assert.That(RootMarked(tree), Is.False);

        bool[] marked = new bool[TREE_TEST_PROXY_COUNT];
        B2AABB[] enlarged = new B2AABB[TREE_TEST_PROXY_COUNT];
        for (int i = 0; i < TREE_TEST_PROXY_COUNT; i += 3)
        {
            int proxyId = proxyIds[i];
            enlarged[proxyId] = EnlargeTreeBox(b2DynamicTree_GetProxyBounds(tree, proxyId), 0.5f);
            b2DynamicTree_MarkProxyMoved(tree, proxyId, enlarged[proxyId]);
            marked[proxyId] = true;
        }

        CheckMarks(tree, B2_ROOT_NODE, marked, out bool markedBelow);
        Assert.That(markedBelow);
        Assert.That(RootMarked(tree));

        // The leaf records took the fat boxes before the refit
        for (int i = 0; i < TREE_TEST_PROXY_COUNT; ++i)
        {
            if (marked[i])
            {
                Assert.That(BoxEquals(b2DynamicTree_GetProxyBounds(tree, i), enlarged[i]));
            }
        }

        b2DynamicTree_Refit(tree);
        b2DynamicTree_Validate(tree);
        CheckBounds(tree, B2_ROOT_NODE, out B2AABB bounds);
        Assert.That(BoxEquals(bounds, b2DynamicTree_GetRootBounds(tree)));

        // Marks survive the refit
        CheckMarks(tree, B2_ROOT_NODE, marked, out markedBelow);
        Assert.That(markedBelow);
        for (int i = 0; i < 16; ++i)
        {
            CompareQuery(tree, proxyIds, TREE_TEST_PROXY_COUNT, RandomTreeBox(8.0f));
        }

        // The rebuild clears every mark and restores the order
        Assert.That(b2DynamicTree_Rebuild(tree, false), Is.GreaterThan(0));
        Assert.That(RootMarked(tree), Is.False);
        Array.Clear(marked, 0, marked.Length);
        CheckMarks(tree, B2_ROOT_NODE, marked, out markedBelow);
        Assert.That(markedBelow, Is.False);
        Assert.That(tree.dfsOrdered);
        CheckBounds(tree, B2_ROOT_NODE, out bounds);
        for (int i = 0; i < 16; ++i)
        {
            CompareQuery(tree, proxyIds, TREE_TEST_PROXY_COUNT, RandomTreeBox(8.0f));
        }

        // A single deep leaf still marks the whole path to the root
        int loneId = proxyIds[TREE_TEST_PROXY_COUNT / 2];
        enlarged[loneId] = EnlargeTreeBox(b2DynamicTree_GetProxyBounds(tree, loneId), 0.5f);
        b2DynamicTree_MarkProxyMoved(tree, loneId, enlarged[loneId]);
        marked[loneId] = true;
        CheckMarks(tree, B2_ROOT_NODE, marked, out markedBelow);
        Assert.That(markedBelow);
        b2DynamicTree_Refit(tree);
        CheckBounds(tree, B2_ROOT_NODE, out bounds);
        Assert.That(b2DynamicTree_Rebuild(tree, false), Is.GreaterThan(0));
        b2DynamicTree_ValidateNoMoved(tree);

        b2DestroyDynamicTree(tree);
    }

    // The rebuild orders the array, a remove keeps the order, an insert that moves an internal node
    // below its children loses it, and the rebuild runs on an unordered array even when nothing moved
    [Test]
    public void TreeDfsOrderTest()
    {
        int proxyCount = 50;
        B2DynamicTree tree = b2CreateDynamicTree(TREE_TEST_PROXY_COUNT);
        int[] proxyIds = new int[TREE_TEST_PROXY_COUNT];
        for (int i = 0; i < proxyCount; ++i)
        {
            proxyIds[i] = b2CreateTreeProxyInternal(tree, RandomTreeBox(2.0f), 1, (ulong)i, false);
        }

        Assert.That(b2DynamicTree_Rebuild(tree, true), Is.EqualTo(proxyCount));
        Assert.That(tree.dfsOrdered);
        Assert.That(tree.nodeEnd, Is.EqualTo(2 * proxyCount));
        Assert.That(tree.pairFreeList, Is.EqualTo(B2_NULL_INDEX));

        // A fresh pair lands above everything, so the order survives when the sibling is a leaf.
        // Validate checks the flag against the array either way.
        proxyIds[proxyCount] = b2CreateTreeProxyInternal(tree, RandomTreeBox(2.0f), 1, (ulong)proxyCount, false);
        proxyCount += 1;
        b2DynamicTree_Validate(tree);

        Assert.That(b2DynamicTree_Rebuild(tree, true), Is.EqualTo(proxyCount));
        Assert.That(tree.dfsOrdered);

        b2DynamicTree_MoveProxyInternal(tree, proxyIds[7], RandomTreeBox(2.0f), false);
        b2DynamicTree_Validate(tree);

        Assert.That(b2DynamicTree_Rebuild(tree, true), Is.EqualTo(proxyCount));
        Assert.That(tree.dfsOrdered);
        int orderedEnd = tree.nodeEnd;

        // Removes leave holes but every survivor stays above its parent
        b2DestroyTreeProxy(tree, proxyIds[3]);
        b2DestroyTreeProxy(tree, proxyIds[20]);
        proxyIds[3] = proxyIds[proxyCount - 1];
        proxyIds[20] = proxyIds[proxyCount - 2];
        proxyCount -= 2;
        Assert.That(tree.dfsOrdered);
        Assert.That(tree.nodeEnd, Is.EqualTo(orderedEnd));
        Assert.That(tree.pairFreeList, Is.Not.EqualTo(B2_NULL_INDEX));
        b2DynamicTree_Validate(tree);

        B2AABB enlarged = EnlargeTreeBox(b2DynamicTree_GetProxyBounds(tree, proxyIds[10]), 0.5f);
        b2DynamicTree_MarkProxyMoved(tree, proxyIds[10], enlarged);
        Assert.That(RootMarked(tree));
        b2DynamicTree_Refit(tree);
        b2DynamicTree_Validate(tree);
        CheckBounds(tree, B2_ROOT_NODE, out B2AABB bounds);
        Assert.That(BoxEquals(bounds, b2DynamicTree_GetRootBounds(tree)));
        for (int i = 0; i < 16; ++i)
        {
            CompareQuery(tree, proxyIds, proxyCount, RandomTreeBox(8.0f));
        }

        Assert.That(b2DynamicTree_Rebuild(tree, false), Is.GreaterThan(0));
        Assert.That(tree.dfsOrdered);
        Assert.That(tree.nodeEnd, Is.EqualTo(2 * proxyCount));
        Assert.That(tree.pairFreeList, Is.EqualTo(B2_NULL_INDEX));
        b2DynamicTree_Validate(tree);

        // A box around everything makes the root the best sibling, so the root's subtree moves into
        // the new pair below its own children and the order is lost with nothing marked
        B2AABB huge = new B2AABB(new B2Vec2(-100.0f, -100.0f), new B2Vec2(100.0f, 100.0f));
        proxyIds[proxyCount] = b2CreateTreeProxyInternal(tree, huge, 1, (ulong)proxyCount, false);
        proxyCount += 1;
        Assert.That(tree.dfsOrdered, Is.False);
        Assert.That(RootMarked(tree), Is.False);
        b2DynamicTree_Validate(tree);

        // The refit falls back to a descent on an unordered array
        enlarged = EnlargeTreeBox(b2DynamicTree_GetProxyBounds(tree, proxyIds[10]), 0.5f);
        b2DynamicTree_MarkProxyMoved(tree, proxyIds[10], enlarged);
        b2DynamicTree_Refit(tree);
        b2DynamicTree_Validate(tree);
        CheckBounds(tree, B2_ROOT_NODE, out bounds);
        b2DynamicTree_ClearMoved(tree);
        Assert.That(RootMarked(tree), Is.False);

        // The rebuild runs for the order alone
        Assert.That(b2DynamicTree_Rebuild(tree, false), Is.GreaterThan(0));
        Assert.That(tree.dfsOrdered);
        Assert.That(tree.nodeEnd, Is.EqualTo(2 * proxyCount));
        b2DynamicTree_Validate(tree);
        for (int i = 0; i < 16; ++i)
        {
            CompareQuery(tree, proxyIds, proxyCount, RandomTreeBox(8.0f));
        }

        b2DestroyDynamicTree(tree);
    }

    // A create and destroy between steps leaves no marks. The rebuild has to run anyway when the
    // array lost its order, or the next enlarge and refit leave the ancestors stale.
    [Test]
    public void TreeStaleAfterInsertRemoveTest()
    {
        B2DynamicTree tree = b2CreateDynamicTree(16);
        int[] ids = new int[16];
        for (int i = 0; i < 16; ++i)
        {
            B2AABB box = new B2AABB(new B2Vec2(2.0f * i, 0.0f), new B2Vec2(2.0f * i + 1.0f, 1.0f));
            ids[i] = b2CreateTreeProxyInternal(tree, box, 1, (ulong)i, true);
        }

        Assert.That(b2DynamicTree_Rebuild(tree, false), Is.EqualTo(16));

        // Control: enlarge and refit on the ordered tree
        B2AABB big = new B2AABB(new B2Vec2(0.0f, 0.0f), new B2Vec2(40.0f, 40.0f));
        b2DynamicTree_MarkProxyMoved(tree, ids[0], big);
        b2DynamicTree_Refit(tree);
        Assert.That(b2AABB_Contains(b2DynamicTree_GetRootBounds(tree), big));
        Assert.That(b2DynamicTree_Rebuild(tree, false), Is.GreaterThan(0));

        // Insert and remove between steps leaves no marks
        B2AABB extra = new B2AABB(new B2Vec2(100.0f, 100.0f), new B2Vec2(101.0f, 101.0f));
        int extraId = b2CreateTreeProxyInternal(tree, extra, 1, 99, true);
        b2DestroyTreeProxy(tree, extraId);
        Assert.That(b2HasTreeMoved(tree), Is.False);

        // The world's rebuild call, then a proxy enlarges during the step
        b2DynamicTree_Rebuild(tree, false);
        B2AABB bigger = new B2AABB(new B2Vec2(-10.0f, -10.0f), new B2Vec2(60.0f, 60.0f));
        b2DynamicTree_MarkProxyMoved(tree, ids[0], bigger);
        b2DynamicTree_Refit(tree);
        Assert.That(b2AABB_Contains(b2DynamicTree_GetRootBounds(tree), bigger));
        b2DynamicTree_Validate(tree);

        b2DestroyDynamicTree(tree);
    }
}
