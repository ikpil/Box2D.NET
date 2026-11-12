// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Buffers;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Cores;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2MathFunction;

namespace Box2D.NET
{
    // The hash maps here are only used for lookups and never iterated, so Dictionary keeps the
    // output deterministic. It stands in for the verstable map used upstream.
    public static class B2ChainBuilders
    {
        private static ulong b2CellKey(int x, int y)
        {
            return (ulong)(uint)x << 32 | (ulong)(uint)y;
        }

        private static ulong b2EdgeKey(int vertex1, int vertex2)
        {
            return (ulong)(uint)vertex1 << 32 | (ulong)(uint)vertex2;
        }

        private static float b2DistanceSquaredToSegment(B2Vec2 p, B2Vec2 a, B2Vec2 b)
        {
            B2Vec2 d = b2Sub(b, a);
            float lengthSquared = b2Dot(d, d);
            if (lengthSquared == 0.0f)
            {
                return b2DistanceSquared(p, a);
            }

            float t = b2ClampFloat(b2Dot(b2Sub(p, a), d) / lengthSquared, 0.0f, 1.0f);
            return b2DistanceSquared(p, b2MulAdd(a, t, d));
        }

        private static void b2TestSplitVertex(ref B2SplitContext context, int vertex)
        {
            if (vertex == context.vertex1 || vertex == context.vertex2)
            {
                return;
            }

            B2Vec2 p = context.vertices[vertex];
            B2Vec2 a = context.point1;
            B2Vec2 d = b2Sub(context.point2, a);
            float dd = b2Dot(d, d);
            float fraction = b2Dot(b2Sub(p, a), d);
            if (fraction <= 0.0f || dd <= fraction)
            {
                return;
            }

            fraction /= dd;

            if (b2DistanceSquared(p, b2MulAdd(a, fraction, d)) > context.toleranceSquared)
            {
                return;
            }

            B2EdgeSplit split = new B2EdgeSplit
            {
                fraction = fraction,
                vertex = vertex,
            };

            b2Array_Push(ref context.splits, split);
        }

        private static int b2CellCoordinate(float x, float inverseCellSize)
        {
            return (int)MathF.Floor(x * inverseCellSize);
        }

        // Use a spatial hash to weld vertices. This is very similar to the mesh welder in Box3D.
        private static int b2WeldVertices(ReadOnlySpan<B2Polygon> polygons, int polygonCount, float tolerance,
            B2Vec2[] vertices, int[] remap, int[] cellNext)
        {
            Dictionary<ulong, int> cellMap = new Dictionary<ulong, int>();

            float cellSize = 2.0f * tolerance;
            float toleranceSquared = tolerance * tolerance;
            int vertexCount = 0;
            int inputIndex = 0;

            for (int i = 0; i < polygonCount; ++i)
            {
                ref readonly B2Polygon polygon = ref polygons[i];
                for (int j = 0; j < polygon.count; ++j)
                {
                    B2Vec2 p = polygon.vertices[j];
                    int cellX = (int)MathF.Floor(p.X / cellSize);
                    int cellY = (int)MathF.Floor(p.Y / cellSize);

                    int bestVertex = B2_NULL_INDEX;
                    float bestDistanceSquared = float.MaxValue;
                    for (int dx = -1; dx <= 1; ++dx)
                    {
                        for (int dy = -1; dy <= 1; ++dy)
                        {
                            if (cellMap.TryGetValue(b2CellKey(cellX + dx, cellY + dy), out int head) == false)
                            {
                                continue;
                            }

                            for (int k = head; k != B2_NULL_INDEX; k = cellNext[k])
                            {
                                float distanceSquared = b2DistanceSquared(vertices[k], p);
                                if (distanceSquared < bestDistanceSquared)
                                {
                                    bestVertex = k;
                                    bestDistanceSquared = distanceSquared;
                                }
                            }
                        }
                    }

                    if (bestVertex == B2_NULL_INDEX || bestDistanceSquared > toleranceSquared)
                    {
                        bestVertex = vertexCount++;
                        vertices[bestVertex] = p;

                        ulong key = b2CellKey(cellX, cellY);
                        cellNext[bestVertex] = cellMap.TryGetValue(key, out int head) ? head : B2_NULL_INDEX;
                        cellMap[key] = bestVertex;
                    }

                    remap[inputIndex++] = bestVertex;
                }
            }

            return vertexCount;
        }

        // Split t-junctions. This creates collinear edges, but they will get cancelled out in a later
        // stage.
        private static void b2SplitEdges(ReadOnlySpan<B2Polygon> polygons, int polygonCount, B2Vec2[] vertices,
            int vertexCount, int[] remap, float tolerance, ref B2Array<B2BuilderEdge> edges)
        {
            float lengthSum = 0.0f;
            int inputEdgeCount = 0;
            int @base = 0;

            // Count edges and compute total edge length, skipping vertices that got welded away.
            for (int i = 0; i < polygonCount; ++i)
            {
                int count = polygons[i].count;
                for (int j = 0; j < count; ++j)
                {
                    int vertex1 = remap[@base + j];
                    int vertex2 = remap[@base + (j + 1 < count ? j + 1 : 0)];
                    if (vertex1 != vertex2)
                    {
                        lengthSum += b2Distance(vertices[vertex1], vertices[vertex2]);
                        inputEdgeCount += 1;
                    }
                }

                @base += count;
            }

            if (inputEdgeCount == 0)
            {
                return;
            }

            // Base the hash grid cell size on the average edge length.
            float cellSize = b2MaxFloat(lengthSum / inputEdgeCount, 4.0f * tolerance);
            float inverseCellSize = 1.0f / cellSize;

            // Put all vertices in a spatial grid, using a linked list of vertices per cell.
            Dictionary<ulong, int> grid = new Dictionary<ulong, int>(vertexCount);
            int[] gridNext = b2Alloc<int>(vertexCount);
            for (int i = 0; i < vertexCount; ++i)
            {
                ulong key = b2CellKey(b2CellCoordinate(vertices[i].X, inverseCellSize),
                    b2CellCoordinate(vertices[i].Y, inverseCellSize));

                // Append linked list.
                gridNext[i] = grid.TryGetValue(key, out int head) ? head : B2_NULL_INDEX;

                // Overwrite linked list head.
                grid[key] = i;
            }

            B2SplitContext context = new B2SplitContext();
            context.vertices = vertices;
            context.toleranceSquared = tolerance * tolerance;
            context.splits = b2Array_Create<B2EdgeSplit>();

            // Walk edges for each polygon on the grid.
            @base = 0;
            for (int i = 0; i < polygonCount; ++i)
            {
                int count = polygons[i].count;
                for (int j = 0; j < count; ++j)
                {
                    int vertex1 = remap[@base + j];
                    int vertex2 = remap[@base + (j + 1 < count ? j + 1 : 0)];
                    if (vertex1 == vertex2)
                    {
                        continue;
                    }

                    B2Vec2 p1 = vertices[vertex1];
                    B2Vec2 p2 = vertices[vertex2];

                    context.point1 = p1;
                    context.point2 = p2;
                    context.vertex1 = vertex1;
                    context.vertex2 = vertex2;
                    context.splits.count = 0;

                    // Walk the grid. First the column range. Then for each row use the range of
                    // cells the segment visits.
                    float minX = b2MinFloat(p1.X, p2.X);
                    float maxX = b2MaxFloat(p1.X, p2.X);
                    float dx = p2.X - p1.X;
                    int firstColumn = b2CellCoordinate(minX - tolerance, inverseCellSize);
                    int lastColumn = b2CellCoordinate(maxX + tolerance, inverseCellSize);
                    for (int column = firstColumn; column <= lastColumn; ++column)
                    {
                        float xa = b2ClampFloat(column * cellSize - tolerance, minX, maxX);
                        float xb = b2ClampFloat((column + 1) * cellSize + tolerance, minX, maxX);
                        float ya = p1.Y;
                        float yb = p2.Y;
                        if (dx != 0.0f)
                        {
                            float ta = b2ClampFloat((xa - p1.X) / dx, 0.0f, 1.0f);
                            float tb = b2ClampFloat((xb - p1.X) / dx, 0.0f, 1.0f);
                            ya = p1.Y + ta * (p2.Y - p1.Y);
                            yb = p1.Y + tb * (p2.Y - p1.Y);
                        }

                        int firstRow = b2CellCoordinate(b2MinFloat(ya, yb) - tolerance, inverseCellSize);
                        int lastRow = b2CellCoordinate(b2MaxFloat(ya, yb) + tolerance, inverseCellSize);
                        for (int row = firstRow; row <= lastRow; ++row)
                        {
                            if (grid.TryGetValue(b2CellKey(column, row), out int head) == false)
                            {
                                continue;
                            }

                            // Walk the cell's linked list of vertices in this grid cell. Look for vertices
                            // from other edges that overlap this edge. Collected in the context.
                            for (int k = head; k != B2_NULL_INDEX; k = gridNext[k])
                            {
                                b2TestSplitVertex(ref context, k);
                            }
                        }
                    }

                    // Sort the splits by fraction using insertion sort. If two splits have the same
                    // fraction then the lowest vertex index goes first.
                    // https://en.wikipedia.org/wiki/Insertion_sort
                    B2EdgeSplit[] splits = context.splits.data;
                    int splitCount = context.splits.count;
                    for (int k = 1; k < splitCount; ++k)
                    {
                        B2EdgeSplit split = splits[k];
                        int m = k;
                        while (m > 0 && (splits[m - 1].fraction > split.fraction ||
                                         (splits[m - 1].fraction == split.fraction &&
                                          splits[m - 1].vertex > split.vertex)))
                        {
                            splits[m] = splits[m - 1];
                            m -= 1;
                        }

                        splits[m] = split;
                    }

                    // Build the sub-edges in order, between the original two end points.
                    int previous = vertex1;
                    for (int k = 0; k < splitCount; ++k)
                    {
                        b2Array_Push(ref edges, new B2BuilderEdge(previous, splits[k].vertex));

                        // This vertex already exists because it belongs to another edge.
                        previous = splits[k].vertex;
                    }

                    // The final segment.
                    b2Array_Push(ref edges, new B2BuilderEdge(previous, vertex2));
                }

                @base += count;
            }

            b2Array_Destroy(ref context.splits);
            b2Free(gridNext, vertexCount);
        }

        // Cancel edges by building a set based on the vertex indices. Then for each edge
        // look for a twin. If a twin is found, both edges will be culled. This will go further
        // and cancel duplicate pairs. So there could be 3 identical edges with 3 identical twins
        // and they will all cancel. 3/2 would leave one behind.
        private static bool b2CancelEdges(B2BuilderEdge[] edges, int edgeCount, bool[] alive)
        {
            bool hasDuplicate = false;

            // This is used to detect duplicates.
            int[] edgeNext = b2Alloc<int>(edgeCount);

            Dictionary<ulong, int> edgeMap = new Dictionary<ulong, int>(edgeCount);

            for (int i = 0; i < edgeCount; ++i)
            {
                alive[i] = true;

                B2BuilderEdge edge = edges[i];

                // Look for the reverse edge in the map.
                ulong reverseKey = b2EdgeKey(edge.vertex2, edge.vertex1);

                // Is the reverse in the map with a non-empty linked list?
                if (edgeMap.TryGetValue(reverseKey, out int other) && other != B2_NULL_INDEX)
                {
                    // Pop the head off the linked list. This can make it null.
                    edgeMap[reverseKey] = edgeNext[other];
                    alive[other] = false;
                    alive[i] = false;
                    continue;
                }

                // Look for existing edge in map.
                ulong key = b2EdgeKey(edge.vertex1, edge.vertex2);

                // If the exist already exists then append the duplicate list.
                edgeNext[i] = edgeMap.TryGetValue(key, out int head) ? head : B2_NULL_INDEX;

                // Insert or overwrite.
                edgeMap[key] = i;

                // Found an identical edge. Bad data.
                hasDuplicate = hasDuplicate || edgeNext[i] != B2_NULL_INDEX;
            }

            b2Free(edgeNext, edgeCount);
            return hasDuplicate;
        }

        private static int b2RemoveCollinearPoints(B2Vec2[] points, int count, float tolerance, B2Vec2[] output)
        {
            float toleranceSquared = tolerance * tolerance;

            int start = B2_NULL_INDEX;
            for (int i = 0; i < count; ++i)
            {
                B2Vec2 previous = points[i > 0 ? i - 1 : count - 1];
                B2Vec2 next = points[i + 1 < count ? i + 1 : 0];
                if (b2DistanceSquaredToSegment(points[i], previous, next) > toleranceSquared)
                {
                    start = i;
                    break;
                }
            }

            if (start == B2_NULL_INDEX)
            {
                return 0;
            }

            int outputCount = 0;
            output[outputCount++] = points[start];

            B2Vec2 anchor = points[start];
            bool hasCone = false;
            B2Vec2 left = b2Vec2_zero;
            B2Vec2 right = b2Vec2_zero;
            float reachSquared = 0.0f;

            for (int k = 1; k < count; ++k)
            {
                B2Vec2 point = points[(start + k) % count];
                B2Vec2 next = points[(start + k + 1) % count];

                B2Vec2 offset = b2Sub(point, anchor);
                float distanceSquared = b2Dot(offset, offset);

                bool coneValid = hasCone;
                B2Vec2 coneLeft = left;
                B2Vec2 coneRight = right;
                if (distanceSquared > toleranceSquared)
                {
                    float distance = MathF.Sqrt(distanceSquared);
                    B2Vec2 u = b2MulSV(1.0f / distance, offset);
                    float s = tolerance / distance;
                    float c = MathF.Sqrt(1.0f - s * s);
                    B2Vec2 pointLeft = new B2Vec2(c * u.X - s * u.Y, s * u.X + c * u.Y);
                    B2Vec2 pointRight = new B2Vec2(c * u.X + s * u.Y, c * u.Y - s * u.X);

                    if (coneValid == false)
                    {
                        coneLeft = pointLeft;
                        coneRight = pointRight;
                        coneValid = true;
                    }
                    else
                    {
                        if (b2Cross(pointLeft, coneLeft) > 0.0f)
                        {
                            coneLeft = pointLeft;
                        }

                        if (b2Cross(coneRight, pointRight) > 0.0f)
                        {
                            coneRight = pointRight;
                        }
                    }
                }

                B2Vec2 reach = b2Sub(next, anchor);
                float newReachSquared = b2MaxFloat(reachSquared, distanceSquared);
                bool collinear = b2Dot(reach, reach) >= newReachSquared;
                if (collinear && coneValid)
                {
                    collinear = b2Cross(coneRight, reach) >= 0.0f && b2Cross(reach, coneLeft) >= 0.0f;
                }

                if (collinear)
                {
                    hasCone = coneValid;
                    left = coneLeft;
                    right = coneRight;
                    reachSquared = newReachSquared;
                    continue;
                }

                if (b2DistanceSquared(point, output[outputCount - 1]) > toleranceSquared)
                {
                    output[outputCount++] = point;
                }

                anchor = point;
                hasCone = false;
                reachSquared = 0.0f;
            }

            while (outputCount > 1 &&
                   b2DistanceSquared(output[outputCount - 1], output[0]) <= toleranceSquared)
            {
                outputCount -= 1;
            }

            return outputCount;
        }

        /// Merge polygons into multiple chain loops.
        public static B2ChainResult b2ComputeChains(ReadOnlySpan<B2Polygon> polygons, int polygonCount, float weldTolerance)
        {
            B2ChainResult result = new B2ChainResult
            {
                chains = null,
                chainCount = 0,
                status = B2ChainStatus.b2_chainStatusSuccess,
            };

            if (polygons.IsEmpty || polygonCount <= 0)
            {
                return result;
            }

            if (b2IsValidFloat(weldTolerance) == false)
            {
                B2_ASSERT(false);
                return result;
            }

            int inputCount = 0;
            for (int i = 0; i < polygonCount; ++i)
            {
                B2_ASSERT(3 <= polygons[i].count && polygons[i].count <= B2_MAX_POLYGON_VERTICES);
                inputCount += polygons[i].count;
            }

            // Minimum weld tolerance is linear slop because that is what manifold code uses.
            float tolerance = b2MaxFloat(weldTolerance, B2_LINEAR_SLOP);

            // Weld vertices that overlap within tolerance.
            B2Vec2[] vertices = b2Alloc<B2Vec2>(inputCount);
            int[] vertexMap = b2Alloc<int>(inputCount);
            int[] cellNext = b2Alloc<int>(inputCount);

            int vertexCount = b2WeldVertices(polygons, polygonCount, tolerance, vertices, vertexMap, cellNext);

            // Split t-junctions. Reserve space for the edges that get built.
            B2Array<B2BuilderEdge> edgeArray = b2Array_Create<B2BuilderEdge>(inputCount);
            b2SplitEdges(polygons, polygonCount, vertices, vertexCount, vertexMap, tolerance, ref edgeArray);

            b2Free(cellNext, inputCount);
            b2Free(vertexMap, inputCount);

            // These edges now follow the winding for each polygon stacked together. Each polygon's segments
            // are split at the t-junctions.
            B2BuilderEdge[] edges = edgeArray.data;
            int edgeCount = edgeArray.count;

            // Cancel identical but antiparallel edges. This creates the outline.
            bool[] alive = new bool[edgeCount];
            bool hasDuplicate = b2CancelEdges(edges, edgeCount, alive);

            // Group the surviving edges by start vertex and a counting sort. This creates an adjacency
            // list. https://en.wikipedia.org/wiki/Counting_sort

            // The number of outgoing edges per vertex via counting sort.
            // For example: [0 3 4] means vertex0 has 3 outgoing edges, vertex1 has 1.
            int[] outStart = new int[vertexCount + 1];

            // Outgoing edge index for each vertex.
            // For example, using outStart [0 3 4]: [1 3 4 5] means vertex0 has edge 1,3,4 outgoing and
            // vertex1 has edge 5 outgoing
            int[] outEdges = new int[edgeCount];

            for (int i = 0; i < edgeCount; ++i)
            {
                // Not canceled?
                if (alive[i])
                {
                    outStart[edges[i].vertex1 + 1] += 1;
                }
            }

            // Prefix sum
            for (int i = 0; i < vertexCount; ++i)
            {
                outStart[i + 1] += outStart[i];
            }

            // Fill in the outgoing edge list, a contiguous array of sub-arrays for each vertex.
            // This array is a cursor for each vertex in the outgoing edge list.
            int[] fill = new int[vertexCount];
            Array.Copy(outStart, fill, vertexCount);
            for (int i = 0; i < edgeCount; ++i)
            {
                if (alive[i])
                {
                    // Outgoing edge by vertex.
                    outEdges[fill[edges[i].vertex1]] = i;

                    // Increment the cursor.
                    fill[edges[i].vertex1] += 1;
                }
            }

            // A vertex with multiple outgoing edges to different target vertices is a pinch. The chain
            // passes through it twice and the ghost vertices there are ambiguous. Outgoing edges to the
            // same target vertex only come from duplicate edges.
            bool hasPinch = false;
            for (int i = 0; i < vertexCount && hasPinch == false; ++i)
            {
                int outGoingCount = outStart[i + 1] - outStart[i];
                if (outGoingCount < 2)
                {
                    continue;
                }

                int target = edges[outEdges[outStart[i]]].vertex2;
                for (int j = outStart[i] + 1; j < outStart[i + 1]; ++j)
                {
                    if (edges[outEdges[j]].vertex2 != target)
                    {
                        hasPinch = true;
                        break;
                    }
                }
            }

            // Duplication is the more serious problem.
            if (hasDuplicate)
            {
                result.status = B2ChainStatus.b2_chainStatusOverlap;
            }
            else if (hasPinch)
            {
                result.status = B2ChainStatus.b2_chainStatusCornerTouch;
            }

            // Build loops using the outgoing edge list. Need to account for pinches by choosing the
            // right-most turn.
            bool[] used = new bool[edgeCount];

            B2Array<B2Vec2> loop = b2Array_Create<B2Vec2>();
            B2Array<B2Vec2> reduced = b2Array_Create<B2Vec2>();
            B2Array<B2ChainData> chains = b2Array_Create<B2ChainData>();

            for (int firstEdge = 0; firstEdge < edgeCount; ++firstEdge)
            {
                if (alive[firstEdge] == false || used[firstEdge])
                {
                    continue;
                }

                // Build a loop starting from the current edge.
                loop.count = 0;
                int edgeIndex = firstEdge;
                bool openPath = false;

                while (true)
                {
                    used[edgeIndex] = true;

                    B2BuilderEdge edge = edges[edgeIndex];
                    b2Array_Push(ref loop, vertices[edge.vertex1]);

                    // Edge direction.
                    B2Vec2 p1 = vertices[edge.vertex1];
                    B2Vec2 p2 = vertices[edge.vertex2];
                    B2Vec2 direction = b2Sub(p2, p1);

                    // Take the sharpest right turn. The minimum signed angle from the incoming direction.
                    int nextEdge = B2_NULL_INDEX;
                    float bestAngle = float.MaxValue;

                    // Consider each outgoing edge for vertex2.
                    for (int k = outStart[edge.vertex2]; k < outStart[edge.vertex2 + 1]; ++k)
                    {
                        int candidate = outEdges[k];
                        if (used[candidate] && candidate != firstEdge)
                        {
                            continue;
                        }

                        B2Vec2 p3 = vertices[edges[candidate].vertex2];
                        B2Vec2 candidateDirection = b2Sub(p3, p2);

                        // Signed angle
                        float angle = b2Atan2(b2Cross(direction, candidateDirection),
                            b2Dot(direction, candidateDirection));

                        if (angle < bestAngle)
                        {
                            nextEdge = candidate;
                            bestAngle = angle;
                        }
                    }

                    if (nextEdge == B2_NULL_INDEX)
                    {
                        // If the next edge is null then there is an algorithmic failure.
                        B2_ASSERT(false);
                        openPath = true;
                        break;
                    }

                    if (nextEdge == firstEdge)
                    {
                        // The loop is closed.
                        break;
                    }

                    edgeIndex = nextEdge;
                }

                // Failure
                if (openPath)
                {
                    continue;
                }

                // The loop is ready. Now remove collinear points.
                b2Array_Reserve(ref reduced, loop.count);
                int pointCount = b2RemoveCollinearPoints(loop.data, loop.count, B2_LINEAR_SLOP, reduced.data);

                if (pointCount < 3)
                {
                    continue;
                }

                // Publish the loop.
                B2ChainData chain = new B2ChainData();
                chain.points = b2Alloc<B2Vec2>(pointCount);
                chain.count = pointCount;
                Array.Copy(reduced.data, chain.points, pointCount);
                b2Array_Push(ref chains, chain);
            }

            b2Array_Destroy(ref edgeArray);
            b2Free(vertices, inputCount);
            b2Array_Destroy(ref loop);
            b2Array_Destroy(ref reduced);

            // Finalize the result.
            if (chains.count > 0)
            {
                result.chains = b2Alloc<B2ChainData>(chains.count);
                Array.Copy(chains.data, result.chains, chains.count);
                result.chainCount = chains.count;
            }

            b2Array_Destroy(ref chains);
            return result;
        }

        /// Free the chains.
        public static void b2FreeChains(ref B2ChainResult result)
        {
            if (result.chains == null)
            {
                return;
            }

            for (int i = 0; i < result.chainCount; ++i)
            {
                b2Free(result.chains[i].points, result.chains[i].count);
            }

            b2Free(result.chains, result.chainCount);
            result.chains = null;
            result.chainCount = 0;
        }
    }
}
