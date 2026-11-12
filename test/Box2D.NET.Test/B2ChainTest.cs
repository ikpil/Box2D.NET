// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

using System;
using NUnit.Framework;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2ChainBuilders;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Manifolds;
using static Box2D.NET.B2MathFunction;

namespace Box2D.NET.Test;

// The point array is the collidable surface and the ghosts sit outside it, so segment i spans
// points i and i + 1 and there is one index space for everything: segmentCount is pointCount
// for a loop and pointCount - 1 otherwise, and material slot i is segment i.
public class B2ChainTest
{
    private static readonly B2Vec2[] loopTriangle =
    {
        new B2Vec2(-2.0f, 0.0f),
        new B2Vec2(2.0f, 0.0f),
        new B2Vec2(0.0f, 3.0f),
    };

    private static readonly B2Vec2[] loopSquare =
    {
        new B2Vec2(-2.0f, 0.0f),
        new B2Vec2(2.0f, 0.0f),
        new B2Vec2(2.0f, 4.0f),
        new B2Vec2(-2.0f, 4.0f),
    };

    private static readonly B2Vec2[] openLine =
    {
        new B2Vec2(0.0f, 0.0f), new B2Vec2(2.0f, 1.0f), new B2Vec2(4.0f, 1.0f), new B2Vec2(6.0f, 0.0f), new B2Vec2(8.0f, 0.0f), new B2Vec2(10.0f, 1.0f),
    };

    private static readonly B2Vec2[] openPair =
    {
        new B2Vec2(0.0f, 0.0f),
        new B2Vec2(4.0f, 0.0f),
    };

    private static readonly B2Vec2 ghostBegin = new B2Vec2(-2.0f, -1.0f);
    private static readonly B2Vec2 ghostEnd = new B2Vec2(12.0f, 2.0f);

    // Tag each material so a segment can be traced back to the slot it came from
    private static B2ChainId CreateTaggedChain(B2BodyId bodyId, B2Vec2[] points, int pointCount, int materialCount, bool isLoop)
    {
        B2SurfaceMaterial[] materials = new B2SurfaceMaterial[materialCount];
        for (int i = 0; i < materialCount; ++i)
        {
            materials[i] = b2DefaultSurfaceMaterial();
            materials[i].userMaterialId = (ulong)(i + 1);
        }

        B2ChainDef chainDef = b2DefaultChainDef();
        chainDef.points = points;
        chainDef.pointCount = pointCount;
        chainDef.materials = materials;
        chainDef.materialCount = materialCount;
        chainDef.isLoop = isLoop;
        chainDef.ghost1 = ghostBegin;
        chainDef.ghost2 = ghostEnd;

        return b2CreateChain(bodyId, chainDef);
    }

    private static void ReadSegmentTags(B2ChainId chainId, ulong[] tags, int count)
    {
        B2ShapeId[] segments = new B2ShapeId[8];
        b2Chain_GetSegments(chainId, segments, count);
        for (int i = 0; i < count; ++i)
        {
            tags[i] = b2Shape_GetSurfaceMaterial(segments[i]).userMaterialId;
        }
    }

    // Points are cloned into body local space with no transform applied, so an exact compare is
    // right here and a tolerance would only hide a wiring mistake
    private static bool SamePoint(B2Vec2 a, B2Vec2 b)
    {
        return a.X == b.X && a.Y == b.Y;
    }

    // A loop closes, so every point leads a segment. An open chain leaves the last point trailing.
    [Test]
    public void SegmentCountTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

        int triangle = b2Chain_GetSegmentCount(CreateTaggedChain(bodyId, loopTriangle, 3, 1, true));
        int square = b2Chain_GetSegmentCount(CreateTaggedChain(bodyId, loopSquare, 4, 1, true));
        int pair = b2Chain_GetSegmentCount(CreateTaggedChain(bodyId, openPair, 2, 1, false));
        int line = b2Chain_GetSegmentCount(CreateTaggedChain(bodyId, openLine, 6, 1, false));

        b2DestroyWorld(worldId);

        Assert.That(triangle, Is.EqualTo(3));
        Assert.That(square, Is.EqualTo(4));
        Assert.That(pair, Is.EqualTo(1));
        Assert.That(line, Is.EqualTo(5));
    }

    // Segment i spans points i and i + 1 and takes its ghosts from the neighbors. A loop wraps for
    // both ends. An open chain takes the def ghosts at the two ends and neighbors everywhere else.
    [Test]
    public void SegmentGeometryTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

        B2ChainId loopId = CreateTaggedChain(bodyId, loopSquare, 4, 1, true);
        B2ChainId openId = CreateTaggedChain(bodyId, openLine, 6, 1, false);

        B2ShapeId[] loopSegments = new B2ShapeId[4];
        B2ShapeId[] openSegments = new B2ShapeId[5];
        int loopCount = b2Chain_GetSegments(loopId, loopSegments, 4);
        int openCount = b2Chain_GetSegments(openId, openSegments, 5);

        B2ChainSegment[] loop = new B2ChainSegment[4];
        B2ChainSegment[] open = new B2ChainSegment[5];
        for (int i = 0; i < 4; ++i)
        {
            loop[i] = b2Shape_GetChainSegment(loopSegments[i]);
        }

        for (int i = 0; i < 5; ++i)
        {
            open[i] = b2Shape_GetChainSegment(openSegments[i]);
        }

        b2DestroyWorld(worldId);

        Assert.That(loopCount, Is.EqualTo(4));
        Assert.That(openCount, Is.EqualTo(5));

        for (int i = 0; i < 4; ++i)
        {
            int next = (i + 1) % 4;
            int after = (i + 2) % 4;
            int prev = (i + 3) % 4;
            Assert.That(SamePoint(loop[i].segment.point1, loopSquare[i]));
            Assert.That(SamePoint(loop[i].segment.point2, loopSquare[next]));
            Assert.That(SamePoint(loop[i].ghost1, loopSquare[prev]));
            Assert.That(SamePoint(loop[i].ghost2, loopSquare[after]));
        }

        for (int i = 0; i < 5; ++i)
        {
            Assert.That(SamePoint(open[i].segment.point1, openLine[i]));
            Assert.That(SamePoint(open[i].segment.point2, openLine[i + 1]));
        }

        // The def ghosts extend the chain past its ends without consuming a point or a segment
        Assert.That(SamePoint(open[0].ghost1, ghostBegin));
        Assert.That(SamePoint(open[4].ghost2, ghostEnd));

        for (int i = 1; i < 5; ++i)
        {
            Assert.That(SamePoint(open[i].ghost1, openLine[i - 1]));
        }

        for (int i = 0; i < 4; ++i)
        {
            Assert.That(SamePoint(open[i].ghost2, openLine[i + 2]));
        }
    }

    [Test]
    public void LoopMaterialTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);
        B2ChainId chainId = CreateTaggedChain(bodyId, loopSquare, 4, 4, true);

        ulong[] created = new ulong[4];
        ReadSegmentTags(chainId, created, 4);

        B2SurfaceMaterial material = b2DefaultSurfaceMaterial();
        material.userMaterialId = 99;

        // A tripped assert throws in a debug build
        Assert.DoesNotThrow(() => b2Chain_SetSurfaceMaterial(chainId, material, 1));

        ulong[] updated = new ulong[4];
        ulong[] reported = new ulong[4];
        ReadSegmentTags(chainId, updated, 4);
        for (int i = 0; i < 4; ++i)
        {
            reported[i] = b2Chain_GetSurfaceMaterial(chainId, i).userMaterialId;
        }

        b2DestroyWorld(worldId);

        for (int i = 0; i < 4; ++i)
        {
            ulong expected = i == 1 ? 99 : (ulong)(i + 1);
            Assert.That(created[i], Is.EqualTo((ulong)(i + 1)));
            Assert.That(updated[i], Is.EqualTo(expected));

            // The getter and the segment shape cannot disagree, there is one copy
            Assert.That(reported[i], Is.EqualTo(expected));
        }
    }

    // No ghost offset anywhere. Slot 0 and the last slot are ordinary segments, which is the part
    // that used to be inert on an open chain.
    [Test]
    public void OpenMaterialTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);
        B2ChainId chainId = CreateTaggedChain(bodyId, openLine, 6, 5, false);

        ulong[] created = new ulong[5];
        ReadSegmentTags(chainId, created, 5);

        B2SurfaceMaterial material = b2DefaultSurfaceMaterial();
        material.userMaterialId = 99;

        Assert.DoesNotThrow(() =>
        {
            b2Chain_SetSurfaceMaterial(chainId, material, 0);
            material.userMaterialId = 98;
            b2Chain_SetSurfaceMaterial(chainId, material, 4);
        });

        ulong[] updated = new ulong[5];
        ulong[] reported = new ulong[5];
        ReadSegmentTags(chainId, updated, 5);
        for (int i = 0; i < 5; ++i)
        {
            reported[i] = b2Chain_GetSurfaceMaterial(chainId, i).userMaterialId;
        }

        b2DestroyWorld(worldId);

        for (int i = 0; i < 5; ++i)
        {
            ulong expected = (ulong)(i + 1);
            if (i == 0)
            {
                expected = 99;
            }
            else if (i == 4)
            {
                expected = 98;
            }

            Assert.That(created[i], Is.EqualTo((ulong)(i + 1)));
            Assert.That(updated[i], Is.EqualTo(expected));
            Assert.That(reported[i], Is.EqualTo(expected));
        }
    }

    // One material at creation covers the chain and does not lock it that way. A broadcast and a
    // per segment set both work afterwards.
    [Test]
    public void SingleMaterialTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);
        B2ChainId chainId = CreateTaggedChain(bodyId, openLine, 6, 1, false);

        ulong[] created = new ulong[5];
        ReadSegmentTags(chainId, created, 5);

        B2SurfaceMaterial material = b2DefaultSurfaceMaterial();
        material.userMaterialId = 99;

        Assert.DoesNotThrow(() => b2Chain_SetAllSurfaceMaterials(chainId, material));

        ulong[] broadcast = new ulong[5];
        ReadSegmentTags(chainId, broadcast, 5);

        // One material at creation must not narrow the segment index, which is what the old count
        // bound would have rejected here
        material.userMaterialId = 98;
        Assert.DoesNotThrow(() => b2Chain_SetSurfaceMaterial(chainId, material, 2));

        ulong[] single = new ulong[5];
        ulong[] reported = new ulong[5];
        ReadSegmentTags(chainId, single, 5);
        for (int i = 0; i < 5; ++i)
        {
            reported[i] = b2Chain_GetSurfaceMaterial(chainId, i).userMaterialId;
        }

        b2DestroyWorld(worldId);

        for (int i = 0; i < 5; ++i)
        {
            ulong expected = i == 2 ? 98ul : 99ul;
            Assert.That(created[i], Is.EqualTo(1));
            Assert.That(broadcast[i], Is.EqualTo(99));
            Assert.That(single[i], Is.EqualTo(expected));
            Assert.That(reported[i], Is.EqualTo(expected));
        }
    }

    private static int s_assertCount;

    private static int CountingAssertFcn(string condition, string fileName, int lineNumber)
    {
        s_assertCount += 1;

        // Keep going so the subtest can report rather than break to the debugger
        return 0;
    }

    private static void BeginCountingAsserts()
    {
        s_assertCount = 0;
        b2SetAssertCallback(CountingAssertFcn);
    }

    // Always zero in a build without asserts, so a check on this is a debug signal only
    private static int EndCountingAsserts()
    {
        b2SetAssertCallback(b2DefaultAssertFcn);
        return s_assertCount;
    }

    // A ghost left on its endpoint means no neighbor, so both ends must act as convex corners: a
    // circle past either cap gets a contact, and a box corner just past either end gets the diagonal
    // vertex normal instead of being snapped up onto the segment normal as if the surface continued.
    private static void CollideEnds(B2ChainSegment segment, int[] circleCount, float[] boxNormalX)
    {
        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
        B2Polygon box = b2MakeSquare(0.25f);

        B2Transform xf1 = new B2Transform(new B2Vec2(4.3f, 0.2f), b2Rot_identity);
        B2Transform xf2 = new B2Transform(new B2Vec2(-0.3f, 0.2f), b2Rot_identity);
        circleCount[0] = b2CollideChainSegmentAndCircle(segment, circle, xf1).pointCount;
        circleCount[1] = b2CollideChainSegmentAndCircle(segment, circle, xf2).pointCount;

        B2SimplexCache cache = new B2SimplexCache();
        xf1.p = new B2Vec2(4.26f, 0.26f);
        B2LocalManifold m1 = b2CollideChainSegmentAndPolygon(segment, box, xf1, ref cache);

        cache = new B2SimplexCache();
        xf2.p = new B2Vec2(-0.26f, 0.26f);
        B2LocalManifold m2 = b2CollideChainSegmentAndPolygon(segment, box, xf2, ref cache);

        boxNormalX[0] = m1.pointCount > 0 ? m1.normal.X : 0.0f;
        boxNormalX[1] = m2.pointCount > 0 ? m2.normal.X : 0.0f;
    }

    [Test]
    public void DegenerateGhostTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

        // Right to left so the collision side faces up
        B2Vec2 p1 = new B2Vec2(4.0f, 0.0f);
        B2Vec2 p2 = new B2Vec2(0.0f, 0.0f);
        B2ChainSegment degenerate = new B2ChainSegment
        {
            ghost1 = p1,
            segment = new B2Segment(p1, p2),
            ghost2 = p2,
            chainId = B2_NULL_INDEX,
        };

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        B2ShapeId createdId = b2CreateChainSegmentShape(bodyId, shapeDef, degenerate);

        B2Segment plain = new B2Segment(new B2Vec2(0.0f, -1.0f), new B2Vec2(4.0f, -1.0f));
        B2ShapeId setId = b2CreateSegmentShape(bodyId, shapeDef, plain);
        b2Shape_SetChainSegment(setId, degenerate);

        B2ChainSegment created = b2Shape_GetChainSegment(createdId);
        B2ChainSegment set = b2Shape_GetChainSegment(setId);

        b2DestroyWorld(worldId);

        Assert.That(SamePoint(created.segment.point1, p1));
        Assert.That(SamePoint(created.segment.point2, p2));
        Assert.That(SamePoint(created.ghost1, set.ghost1));
        Assert.That(SamePoint(created.ghost2, set.ghost2));
        Assert.That(b2Distance(created.ghost1, p1), Is.GreaterThan(B2_LINEAR_SLOP));
        Assert.That(b2Distance(created.ghost2, p2), Is.GreaterThan(B2_LINEAR_SLOP));

        int[] circleCount = new int[2];
        float[] boxNormalX = new float[2];
        CollideEnds(created, circleCount, boxNormalX);

        Assert.That(circleCount[0], Is.EqualTo(1));
        Assert.That(circleCount[1], Is.EqualTo(1));
        Assert.That(boxNormalX[0], Is.GreaterThan(0.5f));
        Assert.That(boxNormalX[1], Is.LessThan(-0.5f));
    }

    private sealed class MoverPlanes
    {
        public int count;
        public B2Vec2 normal;
    }

    private static bool GatherPlane(B2ShapeId shapeId, ref B2PlaneResult plane, object context)
    {
        MoverPlanes planes = (MoverPlanes)context;
        planes.count += 1;
        planes.normal = plane.plane.normal;
        return true;
    }

    private static MoverPlanes CollideMoverAt(B2WorldId worldId, float y)
    {
        B2Capsule mover = new B2Capsule(new B2Vec2(1.5f, y), new B2Vec2(2.5f, y), 0.3f);
        MoverPlanes planes = new MoverPlanes();
        b2World_CollideMover(worldId, b2Pos_zero, mover, b2DefaultQueryFilter(), GatherPlane, planes);
        return planes;
    }

    // The mover must see a chain segment from the front only, matching contacts and the mover cast.
    // A plain segment beside it stays two-sided.
    [Test]
    public void MoverOneSidedTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

        // Right to left so the collision side faces up
        B2ChainSegment chainSegment = new B2ChainSegment
        {
            ghost1 = new B2Vec2(5.0f, 0.0f),
            segment = new B2Segment(new B2Vec2(4.0f, 0.0f), new B2Vec2(0.0f, 0.0f)),
            ghost2 = new B2Vec2(-1.0f, 0.0f),
            chainId = B2_NULL_INDEX,
        };
        B2ShapeDef shapeDef = b2DefaultShapeDef();
        b2CreateChainSegmentShape(bodyId, shapeDef, chainSegment);

        MoverPlanes front = CollideMoverAt(worldId, 0.2f);
        MoverPlanes behind = CollideMoverAt(worldId, -0.2f);

        B2Segment segment = new B2Segment(new B2Vec2(4.0f, 0.0f), new B2Vec2(0.0f, 0.0f));
        B2BodyDef plainDef = b2DefaultBodyDef();
        plainDef.position = new B2Pos(0.0f, 10.0f);
        B2BodyId plainId = b2CreateBody(worldId, plainDef);
        b2CreateSegmentShape(plainId, shapeDef, segment);

        MoverPlanes plainBehind = CollideMoverAt(worldId, 9.8f);

        b2DestroyWorld(worldId);

        Assert.That(front.count, Is.EqualTo(1));
        Assert.That(front.normal.Y, Is.GreaterThan(0.99f));
        Assert.That(behind.count, Is.EqualTo(0));
        Assert.That(plainBehind.count, Is.EqualTo(1));
        Assert.That(plainBehind.normal.Y, Is.LessThan(-0.99f));
    }

    private static float LoopArea(in B2ChainData chain)
    {
        float area = 0.0f;
        for (int i = 0; i < chain.count; ++i)
        {
            B2Vec2 p1 = chain.points[i];
            B2Vec2 p2 = chain.points[i + 1 < chain.count ? i + 1 : 0];
            area += 0.5f * b2Cross(p1, p2);
        }

        return area;
    }

    private static bool CreateChainsOnGround(in B2ChainResult result)
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(worldDef);
        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

        BeginCountingAsserts();
        bool valid = true;
        for (int i = 0; i < result.chainCount; ++i)
        {
            B2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = result.chains[i].points;
            chainDef.pointCount = result.chains[i].count;
            chainDef.isLoop = true;
            B2ChainId chainId = b2CreateChain(bodyId, chainDef);
            valid = valid && b2Chain_IsValid(chainId);
        }

        valid = valid && EndCountingAsserts() == 0;

        b2DestroyWorld(worldId);
        return valid;
    }

    private static void EnsureSmall(float value, float tolerance)
    {
        Assert.That(MathF.Abs(value), Is.LessThanOrEqualTo(tolerance));
    }

    // Tiles that share edges must merge into the outline of their union, with seams and T-junctions
    // gone and every loop winding counter-clockwise around solid.
    [Test]
    public void ComputeChainsTest()
    {
        // A row of boxes with float noise on the seams welds into one rectangle
        {
            B2Polygon[] boxes = new B2Polygon[3];
            for (int i = 0; i < 3; ++i)
            {
                float noise = 0.0001f * (float)(i - 1);
                boxes[i] = b2MakeOffsetBox(0.5f, 0.5f, new B2Vec2(1.0f * i + noise, noise), b2Rot_identity);
            }

            B2ChainResult result = b2ComputeChains(boxes, 3, B2_LINEAR_SLOP);
            Assert.That(result.status, Is.EqualTo(B2ChainStatus.b2_chainStatusSuccess));
            Assert.That(result.chainCount, Is.EqualTo(1));
            Assert.That(result.chains[0].count, Is.EqualTo(4));
            EnsureSmall(LoopArea(result.chains[0]) - 3.0f, 0.01f);
            Assert.That(CreateChainsOnGround(result));
            b2FreeChains(ref result);
            Assert.That(result.chains == null && result.chainCount == 0);
        }

        // Two boxes sitting on one long box meet it in a T-junction
        {
            B2Polygon[] boxes =
            {
                b2MakeOffsetBox(1.0f, 0.5f, new B2Vec2(1.0f, 0.5f), b2Rot_identity),
                b2MakeOffsetBox(0.5f, 0.5f, new B2Vec2(0.5f, 1.5f), b2Rot_identity),
                b2MakeOffsetBox(0.5f, 0.5f, new B2Vec2(1.5f, 1.5f), b2Rot_identity),
            };

            B2ChainResult result = b2ComputeChains(boxes, 3, B2_LINEAR_SLOP);
            Assert.That(result.status, Is.EqualTo(B2ChainStatus.b2_chainStatusSuccess));
            Assert.That(result.chainCount, Is.EqualTo(1));
            Assert.That(result.chains[0].count, Is.EqualTo(4));
            EnsureSmall(LoopArea(result.chains[0]) - 4.0f, 0.01f);
            Assert.That(CreateChainsOnGround(result));
            b2FreeChains(ref result);
        }

        // Boxes touching at one corner trace a single loop through the shared vertex, turning into
        // each empty quadrant so both corners there are concave. The shared vertex is ambiguous.
        {
            B2Polygon[] boxes =
            {
                b2MakeOffsetBox(0.5f, 0.5f, new B2Vec2(0.5f, 0.5f), b2Rot_identity),
                b2MakeOffsetBox(0.5f, 0.5f, new B2Vec2(1.5f, 1.5f), b2Rot_identity),
            };

            B2ChainResult result = b2ComputeChains(boxes, 2, B2_LINEAR_SLOP);
            Assert.That(result.status, Is.EqualTo(B2ChainStatus.b2_chainStatusCornerTouch));
            Assert.That(result.chainCount, Is.EqualTo(1));

            B2ChainData chain = result.chains[0];
            Assert.That(chain.count, Is.EqualTo(8));
            EnsureSmall(LoopArea(chain) - 2.0f, 0.01f);

            B2Vec2 shared = new B2Vec2(1.0f, 1.0f);
            int visits = 0;
            for (int i = 0; i < chain.count; ++i)
            {
                if (b2DistanceSquared(chain.points[i], shared) < 1.0e-6f)
                {
                    B2Vec2 previous = chain.points[i > 0 ? i - 1 : chain.count - 1];
                    B2Vec2 next = chain.points[i + 1 < chain.count ? i + 1 : 0];
                    B2Vec2 edge1 = b2Sub(shared, previous);
                    B2Vec2 edge2 = b2Sub(next, shared);
                    Assert.That(b2Cross(edge1, edge2), Is.LessThan(0.0f));
                    visits += 1;
                }
            }

            Assert.That(visits, Is.EqualTo(2));
            Assert.That(CreateChainsOnGround(result));
            b2FreeChains(ref result);
        }

        // A ring of boxes gives an outer loop and a clockwise hole
        {
            B2Polygon[] boxes = new B2Polygon[8];
            int count = 0;
            for (int i = 0; i < 3; ++i)
            {
                for (int j = 0; j < 3; ++j)
                {
                    if (i == 1 && j == 1)
                    {
                        continue;
                    }

                    boxes[count++] = b2MakeOffsetBox(0.5f, 0.5f, new B2Vec2(1.0f * i, 1.0f * j), b2Rot_identity);
                }
            }

            B2ChainResult result = b2ComputeChains(boxes, count, B2_LINEAR_SLOP);
            Assert.That(result.status, Is.EqualTo(B2ChainStatus.b2_chainStatusSuccess));
            Assert.That(result.chainCount, Is.EqualTo(2));

            float area0 = LoopArea(result.chains[0]);
            float area1 = LoopArea(result.chains[1]);
            float outer = b2MaxFloat(area0, area1);
            float inner = b2MinFloat(area0, area1);
            Assert.That(result.chains[0].count == 4 && result.chains[1].count == 4);
            EnsureSmall(outer - 9.0f, 0.01f);
            EnsureSmall(inner + 1.0f, 0.01f);
            Assert.That(CreateChainsOnGround(result));
            b2FreeChains(ref result);
        }

        // A gap narrower than the weld tolerance closes, a wider one keeps the tiles apart
        {
            B2Polygon[] boxes =
            {
                b2MakeOffsetBox(0.5f, 0.5f, new B2Vec2(0.0f, 0.0f), b2Rot_identity),
                b2MakeOffsetBox(0.5f, 0.5f, new B2Vec2(1.05f, 0.0f), b2Rot_identity),
            };

            B2ChainResult result = b2ComputeChains(boxes, 2, B2_LINEAR_SLOP);
            Assert.That(result.status, Is.EqualTo(B2ChainStatus.b2_chainStatusSuccess));
            Assert.That(result.chainCount, Is.EqualTo(2));
            b2FreeChains(ref result);

            result = b2ComputeChains(boxes, 2, 0.1f);
            Assert.That(result.status, Is.EqualTo(B2ChainStatus.b2_chainStatusSuccess));
            Assert.That(result.chainCount, Is.EqualTo(1));
            Assert.That(result.chains[0].count, Is.EqualTo(4));
            Assert.That(CreateChainsOnGround(result));
            b2FreeChains(ref result);
        }

        // Overlapping polygons are not supported, an exact duplicate shows up as duplicate edges and
        // overlap wins over the corner touches it also causes
        {
            B2Polygon[] boxes = { b2MakeBox(0.5f, 0.5f), b2MakeBox(0.5f, 0.5f) };

            B2ChainResult result = b2ComputeChains(boxes, 2, B2_LINEAR_SLOP);
            Assert.That(result.status, Is.EqualTo(B2ChainStatus.b2_chainStatusOverlap));
            b2FreeChains(ref result);
        }

        {
            B2ChainResult result = b2ComputeChains(null, 0, B2_LINEAR_SLOP);
            Assert.That(result.chains == null && result.chainCount == 0 && result.status == B2ChainStatus.b2_chainStatusSuccess);
            b2FreeChains(ref result);
        }
    }
}
