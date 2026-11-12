// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

using NUnit.Framework;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

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
}
