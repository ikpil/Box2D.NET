// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2MathFunction;

namespace Box2D.NET
{
    public static class B2Movers
    {
        /// Solves the position of a mover that satisfies the given collision planes.
        /// @param targetDelta the desired movement from the position used to generate the collision planes
        /// @param planes the collision planes
        /// @param count the number of collision planes
        public static B2PlaneSolverResult b2SolvePlanes(B2Vec2 targetDelta, Span<B2CollisionPlane> planes, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                planes[i].push = 0.0f;
            }

            B2Vec2 delta = targetDelta;
            float tolerance = B2_LINEAR_SLOP;

            int iteration;
            for (iteration = 0; iteration < 20; ++iteration)
            {
                float totalPush = 0.0f;
                for (int planeIndex = 0; planeIndex < count; ++planeIndex)
                {
                    ref B2CollisionPlane plane = ref planes[planeIndex];

                    // Add slop to prevent jitter
                    float separation = b2PlaneSeparation(plane.plane, delta) + B2_LINEAR_SLOP;
                    // if (separation > 0.0f)
                    //{
                    //	continue;
                    // }

                    float push = -separation;

                    // Clamp accumulated push
                    float accumulatedPush = plane.push;
                    plane.push = b2ClampFloat(plane.push + push, 0.0f, plane.pushLimit);
                    push = plane.push - accumulatedPush;
                    delta = b2MulAdd(delta, push, plane.plane.normal);

                    // Track maximum push for convergence
                    totalPush += b2AbsFloat(push);
                }

                if (totalPush < tolerance)
                {
                    break;
                }
            }

            return new B2PlaneSolverResult(delta, iteration);
        }

        /// Clips the velocity against the given collision planes. Planes with zero push or clipVelocity
        /// set to false are skipped.
        public static B2Vec2 b2ClipVector(B2Vec2 vector, Span<B2CollisionPlane> planes, int count)
        {
            B2Vec2 v = vector;

            for (int planeIndex = 0; planeIndex < count; ++planeIndex)
            {
                ref readonly B2CollisionPlane plane = ref planes[planeIndex];
                if (plane.push == 0.0f || plane.clipVelocity == false)
                {
                    continue;
                }

                v = b2MulSub(v, b2MinFloat(0.0f, b2Dot(v, plane.plane.normal)), plane.plane.normal);
            }

            return v;
        }
    }
}