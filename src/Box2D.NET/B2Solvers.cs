// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

// Compare to SDL_CPUPauseInstruction

using System;
using System.Threading.Tasks;
using static Box2D.NET.B2Tables;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Atomics;
using static Box2D.NET.B2DynamicTrees;
using static Box2D.NET.B2Cores;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Profiling;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Contacts;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2BitSets;
using static Box2D.NET.B2ContactSolvers;
using static Box2D.NET.B2Timers;
using static Box2D.NET.B2Islands;
using static Box2D.NET.B2BoardPhases;
using static Box2D.NET.B2ArenaAllocators;
using static Box2D.NET.B2ConstraintGraphs;
using static Box2D.NET.B2CTZs;
using static Box2D.NET.B2SolverSets;
using static Box2D.NET.B2IdPools;

namespace Box2D.NET
{
    public static class B2Solvers
    {
        // todo testing
        public const int ITERATIONS = 1;
        public const int RELAX_ITERATIONS = 1;

        // TODO: @ikpil. check SIMD
        public static readonly int B2_SIMD_SHIFT = b2SIMDShift();

        public static int b2SIMDShift()
        {
            if (8 == B2_SIMD_WIDTH)
            {
                return 3;
            }
            else if (4 == B2_SIMD_WIDTH)
            {
                return 2;
            }
            else
            {
                return 0;
            }
        }


        public static B2Softness b2MakeSoft(float hertz, float zeta, float h)
        {
            if (hertz == 0.0f)
            {
                return new B2Softness(0.0f, 0.0f, 0.0f);
            }

            float omega = 2.0f * B2_PI * hertz;
            float a1 = 2.0f * zeta + h * omega;
            float a2 = h * omega * a1;
            float a3 = 1.0f / (1.0f + a2);

            // bias = w / (2 * z + hw)
            // massScale = hw * (2 * z + hw) / (1 + hw * (2 * z + hw))
            // impulseScale = 1 / (1 + hw * (2 * z + hw))

            // If z == 0
            // bias = 1/h
            // massScale = hw^2 / (1 + hw^2)
            // impulseScale = 1 / (1 + hw^2)

            // w -> inf
            // bias = 1/h
            // massScale = 1
            // impulseScale = 0

            // if w = pi / 4  * inv_h
            // massScale = (pi/4)^2 / (1 + (pi/4)^2) = pi^2 / (16 + pi^2) ~= 0.38
            // impulseScale = 1 / (1 + (pi/4)^2) = 16 / (16 + pi^2) ~= 0.62

            // In all cases:
            // massScale + impulseScale == 1

            return new B2Softness(omega / a1, a2 * a3, a3);
        }


        public static void b2Pause()
        {
            // TODO: @ikpil, check sleep or yield
            Task.Yield();
        }


        // Integrate velocities and apply damping
        public static void b2IntegrateVelocitiesTask(int startIndex, int endIndex, B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.integrate_velocity, "IntVel", B2HexColor.b2_colorDeepPink, true);

            B2BodyState[] states = context.states;
            B2BodySim[] sims = context.sims;

            B2Vec2 gravity = context.world.gravity;
            float h = context.h;
            float maxLinearSpeed = context.maxLinearVelocity;
            float maxAngularSpeed = B2_MAX_ROTATION * context.inv_dt;
            float maxLinearSpeedSquared = maxLinearSpeed * maxLinearSpeed;
            float maxAngularSpeedSquared = maxAngularSpeed * maxAngularSpeed;

            for (int i = startIndex; i < endIndex; ++i)
            {
                B2BodySim sim = sims[i];
                B2BodyState state = states[i];

                B2Vec2 v = state.linearVelocity;
                float w = state.angularVelocity;

                // Apply forces, torque, gravity, and damping
                // Apply damping.
                // Differential equation: dv/dt + c * v = 0
                // Solution: v(t) = v0 * exp(-c * t)
                // Time step: v(t + dt) = v0 * exp(-c * (t + dt)) = v0 * exp(-c * t) * exp(-c * dt) = v(t) * exp(-c * dt)
                // v2 = exp(-c * dt) * v1
                // Pade approximation:
                // v2 = v1 * 1 / (1 + c * dt)
                float linearDamping = 1.0f / (1.0f + h * sim.linearDamping);
                float angularDamping = 1.0f / (1.0f + h * sim.angularDamping);

                // Gravity scale will be zero for kinematic bodies
                float gravityScale = sim.invMass > 0.0f ? sim.gravityScale : 0.0f;

                // lvd = h * im * f + h * g
                B2Vec2 linearVelocityDelta = b2Add(b2MulSV(h * sim.invMass, sim.force), b2MulSV(h * gravityScale, gravity));
                float angularVelocityDelta = h * sim.invInertia * sim.torque;

                v = b2MulAdd(linearVelocityDelta, linearDamping, v);
                w = angularVelocityDelta + angularDamping * w;

                // Clamp to max linear speed
                if (b2Dot(v, v) > maxLinearSpeedSquared)
                {
                    float ratio = maxLinearSpeed / b2Length(v);
                    v = b2MulSV(ratio, v);
                    sim.flags |= (int)B2BodyFlags.b2_isSpeedCapped;
                }

                // Clamp to max angular speed
                if (w * w > maxAngularSpeedSquared && (sim.flags & (uint)B2BodyFlags.b2_allowFastRotation) == 0)
                {
                    float ratio = maxAngularSpeed / b2AbsFloat(w);
                    w *= ratio;
                    sim.flags |= (uint)B2BodyFlags.b2_isSpeedCapped;
                }

                if (0 != (state.flags & (uint)B2BodyFlags.b2_lockLinearX))
                {
                    v.X = 0.0f;
                }

                if (0 != (state.flags & (uint)B2BodyFlags.b2_lockLinearY))
                {
                    v.Y = 0.0f;
                }

                if (0 != (state.flags & (uint)B2BodyFlags.b2_lockAngularZ))
                {
                    w = 0.0f;
                }

                state.linearVelocity = v;
                state.angularVelocity = w;
            }

            b2TracyCZoneEnd(B2TracyCZone.integrate_velocity);
        }

        public static void b2PrepareJointsTask(int startIndex, int endIndex, B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.prepare_joints, "PrepJoints", B2HexColor.b2_colorOldLace, true);

            ArraySegment<B2JointSim> joints = context.joints;

            for (int i = startIndex; i < endIndex; ++i)
            {
                B2JointSim joint = joints[i];
                b2PrepareJoint(joint, context);
            }

            b2TracyCZoneEnd(B2TracyCZone.prepare_joints);
        }

        public static void b2WarmStartJointsTask(int startIndex, int endIndex, B2StepContext context, int colorIndex)
        {
            b2TracyCZoneNC(B2TracyCZone.warm_joints, "WarmJoints", B2HexColor.b2_colorGold, true);

            ref B2GraphColor color = ref context.graph.colors[colorIndex];
            B2JointSim[] joints = color.jointSims.data;
            B2_ASSERT(0 <= startIndex && startIndex < color.jointSims.count);
            B2_ASSERT(startIndex <= endIndex && endIndex <= color.jointSims.count);

            for (int i = startIndex; i < endIndex; ++i)
            {
                B2JointSim joint = joints[i];
                b2WarmStartJoint(joint, context);
            }

            b2TracyCZoneEnd(B2TracyCZone.warm_joints);
        }

        static void b2SolveJointsTask(int startIndex, int endIndex, B2StepContext context, int colorIndex, bool useBias,
            int workerIndex)
        {
            b2TracyCZoneNC(B2TracyCZone.solve_joints, "SolveJoints", B2HexColor.b2_colorLemonChiffon, true);

            ref B2GraphColor color = ref context.graph.colors[colorIndex];
            B2JointSim[] joints = color.jointSims.data;
            B2_ASSERT(0 <= startIndex && startIndex < color.jointSims.count);
            B2_ASSERT(startIndex <= endIndex && endIndex <= color.jointSims.count);

            ref B2BitSet jointStateBitSet = ref context.world.taskContexts.data[workerIndex].jointStateBitSet;

            for (int i = startIndex; i < endIndex; ++i)
            {
                B2JointSim joint = joints[i];
                b2SolveJoint(joint, context, useBias);

                if (useBias &&
                    (joint.forceThreshold < float.MaxValue || joint.torqueThreshold < float.MaxValue) &&
                    b2GetBit(ref jointStateBitSet, joint.jointId) == false)
                {
                    float force, torque;
                    b2GetJointReaction(joint, context.inv_h, out force, out torque);

                    // Check thresholds. A zero threshold means all awake joints get reported.
                    if (force >= joint.forceThreshold || torque >= joint.torqueThreshold)
                    {
                        // Flag this joint for processing.
                        b2SetBit(ref jointStateBitSet, joint.jointId);
                    }
                }
            }

            b2TracyCZoneEnd(B2TracyCZone.solve_joints);
        }

        public static void b2IntegratePositionsTask(int startIndex, int endIndex, B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.integrate_positions, "IntPos", B2HexColor.b2_colorDarkSeaGreen, true);

            B2BodyState[] states = context.states;
            float h = context.h;

            B2_ASSERT(startIndex <= endIndex);

            for (int i = startIndex; i < endIndex; ++i)
            {
                B2BodyState state = states[i];

                if (0 != (state.flags & (uint)B2BodyFlags.b2_lockLinearX))
                {
                    state.linearVelocity.X = 0.0f;
                }

                if (0 != (state.flags & (uint)B2BodyFlags.b2_lockLinearY))
                {
                    state.linearVelocity.Y = 0.0f;
                }

                if (0 != (state.flags & (uint)B2BodyFlags.b2_lockAngularZ))
                {
                    state.angularVelocity = 0.0f;
                }

                state.deltaPosition = b2MulAdd(state.deltaPosition, h, state.linearVelocity);
                state.deltaRotation = b2IntegrateRotation(state.deltaRotation, h * state.angularVelocity);
            }

            b2TracyCZoneEnd(B2TracyCZone.integrate_positions);
        }


        // This is called from b2DynamicTree_Query for continuous collision
        public static bool b2ContinuousQueryCallback(int proxyId, ulong userData, ref B2ContinuousContext context)
        {
            B2_UNUSED(proxyId);

            int shapeId = (int)userData;

            ref B2ContinuousContext continuousContext = ref context;
            B2Shape fastShape = continuousContext.fastShape;
            B2BodySim fastBodySim = continuousContext.fastBodySim;

            B2_ASSERT(fastShape.sensorIndex == B2_NULL_INDEX);

            // Skip same shape
            if (shapeId == fastShape.id)
            {
                return true;
            }

            B2World world = continuousContext.world;

            B2Shape shape = b2Array_Get(ref world.shapes, shapeId);

            // Skip same body
            if (shape.bodyId == fastShape.bodyId)
            {
                return true;
            }

            bool isSensor = shape.sensorIndex != B2_NULL_INDEX;

            // Skip sensors unless the shapes want sensor events
            if (isSensor && (shape.enableSensorEvents == false || fastShape.enableSensorEvents == false))
            {
                return true;
            }


            // Skip filtered shapes
            bool canCollide = b2ShouldShapesCollide(fastShape.filter, shape.filter);
            if (canCollide == false)
            {
                return true;
            }

            B2Body body = b2Array_Get(ref world.bodies, shape.bodyId);

            B2BodySim bodySim = b2GetBodySim(world, body);
            B2_ASSERT(body.type == B2BodyType.b2_staticBody || 0 != (fastBodySim.flags & (uint)B2BodyFlags.b2_isBullet));

            // Skip bullets
            if (0 != (bodySim.flags & (uint)B2BodyFlags.b2_isBullet))
            {
                return true;
            }

            // Skip filtered bodies
            B2Body fastBody = b2Array_Get(ref world.bodies, fastBodySim.bodyId);
            canCollide = b2ShouldBodiesCollide(world, fastBody, body);
            if (canCollide == false)
            {
                return true;
            }

            // Custom user filtering
            b2CustomFilterFcn customFilterFcn = world.customFilterFcn;
            if (customFilterFcn != null)
            {
                B2ShapeId idA = new B2ShapeId(shape.id + 1, world.worldId, shape.generation);
                B2ShapeId idB = new B2ShapeId(fastShape.id + 1, world.worldId, fastShape.generation);
                canCollide = customFilterFcn(idA, idB, world.customFilterContext);
                if (canCollide == false)
                {
                    return true;
                }
            }

            // Early out on fast parallel movement over a chain shape.
            if (shape.type == B2ShapeType.b2_chainSegmentShape)
            {
                B2Transform transform = bodySim.transform;
                B2Vec2 p1 = b2TransformPoint(ref transform, shape.us.chainSegment.segment.point1);
                B2Vec2 p2 = b2TransformPoint(ref transform, shape.us.chainSegment.segment.point2);
                B2Vec2 e = b2Sub(p2, p1);
                float length = 0;
                e = b2GetLengthAndNormalize(ref length, e);
                if (length > B2_LINEAR_SLOP)
                {
                    B2Vec2 c1 = continuousContext.centroid1;
                    float offset1 = b2Cross(b2Sub(c1, p1), e);
                    B2Vec2 c2 = continuousContext.centroid2;
                    float offset2 = b2Cross(b2Sub(c2, p1), e);

                    // todo this should use the min extent of the fast shape, not the body
                    const float allowedFraction = 0.25f;
                    if (offset1 < 0.0f || offset1 - offset2 < allowedFraction * fastBodySim.minExtent)
                    {
                        // Minimal clipping
                        return true;
                    }
                }
            }

            // todo_erin testing early out for segments
#if FALSE
	if ( shape.type == b2ShapeType.b2_segmentShape )
	{
		b2Transform transform = bodySim.transform;
		B2Vec2 p1 = b2TransformPoint( transform, shape.segment.point1 );
		B2Vec2 p2 = b2TransformPoint( transform, shape.segment.point2 );
		B2Vec2 e = b2Sub( p2, p1 );
		B2Vec2 c1 = continuousContext.centroid1;
		B2Vec2 c2 = continuousContext.centroid2;
		float offset1 = b2Cross( b2Sub( c1, p1 ), e );
		float offset2 = b2Cross( b2Sub( c2, p1 ), e );

		if ( offset1 > 0.0f && offset2 > 0.0f )
		{
			// Started behind or finished in front
			return true;
		}

		if ( offset1 < 0.0f && offset2 < 0.0f )
		{
			// Started behind or finished in front
			return true;
		}
	}
#endif

            B2TOIInput input = new B2TOIInput();
            input.proxyA = b2MakeShapeDistanceProxy(shape);
            input.proxyB = b2MakeShapeDistanceProxy(fastShape);
            input.sweepA = b2MakeSweep(bodySim);
            input.sweepB = continuousContext.sweep;
            input.maxFraction = continuousContext.fraction;

            B2TOIOutput output = b2TimeOfImpact(ref input);
            if (isSensor)
            {
                // Only accept a sensor hit that is sooner than the current solid hit.
                if (output.fraction <= continuousContext.fraction && continuousContext.sensorCount < B2_MAX_CONTINUOUS_SENSOR_HITS)
                {
                    int index = continuousContext.sensorCount;
                    // The hit shape is a sensor
                    B2SensorHit sensorHit = new B2SensorHit()
                    {
                        sensorId = shape.id,
                        visitorId = fastShape.id,
                    };
                    continuousContext.sensorHits[index] = sensorHit;
                    continuousContext.sensorFractions[index] = output.fraction;
                    continuousContext.sensorCount += 1;
                }
            }
            else
            {
                float hitFraction = continuousContext.fraction;
                bool didHit = false;

                if (0.0f < output.fraction && output.fraction < continuousContext.fraction)
                {
                    hitFraction = output.fraction;
                    didHit = true;
                }
                else if (0.0f == output.fraction)
                {
                    // fallback to TOI of a small circle around the fast shape centroid
                    B2Vec2 centroid = b2GetShapeCentroid(fastShape);
                    B2ShapeExtent extent = b2ComputeShapeExtent(fastShape, centroid);
                    float radius = 0.25f * extent.minExtent;
                    input.proxyB = b2MakeProxy(centroid, 1, radius);
                    output = b2TimeOfImpact(ref input);
                    if (0.0f < output.fraction && output.fraction < continuousContext.fraction)
                    {
                        hitFraction = output.fraction;
                        didHit = true;
                    }
                }

                if (didHit && (shape.enablePreSolveEvents || fastShape.enablePreSolveEvents) && world.preSolveFcn != null)
                {
                    // Pre-solve is expensive because I need to compute a temporary manifold
                    B2ShapeId shapeIdA = new B2ShapeId(shape.id + 1, world.worldId, shape.generation);
                    B2ShapeId shapeIdB = new B2ShapeId(fastShape.id + 1, world.worldId, fastShape.generation);
                    didHit = world.preSolveFcn(shapeIdA, shapeIdB, output.point, output.normal, world.preSolveContext);
                }

                if (didHit)
                {
                    continuousContext.fraction = hitFraction;
                }
            }

            // Continue query
            return true;
        }

        public static void b2SolveContinuous(B2World world, int bodySimIndex, B2TaskContext taskContext)
        {
            b2TracyCZoneNC(B2TracyCZone.ccd, "CCD", B2HexColor.b2_colorDarkGoldenRod, true);

            B2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_awakeSet);
            B2BodySim fastBodySim = b2Array_Get(ref awakeSet.bodySims, bodySimIndex);
            B2_ASSERT(0 != (fastBodySim.flags & (uint)B2BodyFlags.b2_isFast));

            B2Sweep sweep = b2MakeSweep(fastBodySim);

            B2Transform xf1;
            xf1.q = sweep.q1;
            xf1.p = b2Sub(sweep.c1, b2RotateVector(sweep.q1, sweep.localCenter));

            B2Transform xf2;
            xf2.q = sweep.q2;
            xf2.p = b2Sub(sweep.c2, b2RotateVector(sweep.q2, sweep.localCenter));

            B2DynamicTree staticTree = world.broadPhase.trees[(int)B2BodyType.b2_staticBody];
            B2DynamicTree kinematicTree = world.broadPhase.trees[(int)B2BodyType.b2_kinematicBody];
            B2DynamicTree dynamicTree = world.broadPhase.trees[(int)B2BodyType.b2_dynamicBody];
            B2Body fastBody = b2Array_Get(ref world.bodies, fastBodySim.bodyId);

            B2ContinuousContext context = new B2ContinuousContext();
            context.world = world;
            context.sweep = sweep;
            context.fastBodySim = fastBodySim;
            context.fraction = 1.0f;

            bool isBullet = (fastBodySim.flags & (uint)B2BodyFlags.b2_isBullet) != 0;

            int shapeId = fastBody.headShapeId;
            while (shapeId != B2_NULL_INDEX)
            {
                B2Shape fastShape = b2Array_Get(ref world.shapes, shapeId);
                shapeId = fastShape.nextShapeId;

                context.fastShape = fastShape;
                context.centroid1 = b2TransformPoint(ref xf1, fastShape.localCentroid);
                context.centroid2 = b2TransformPoint(ref xf2, fastShape.localCentroid);

                B2AABB box1 = fastShape.aabb;
                B2AABB box2 = b2ComputeShapeAABB(fastShape, xf2);

                // Store this to avoid double computation in the case there is no impact event
                fastShape.aabb = box2;

                // No continuous collision for sensors (but still need the updated bounds)
                if (fastShape.sensorIndex != B2_NULL_INDEX)
                {
                    continue;
                }

                B2AABB sweptBox = b2AABB_Union(box1, box2);
                b2DynamicTree_Query(staticTree, sweptBox, B2_DEFAULT_MASK_BITS, b2ContinuousQueryCallback, ref context);

                if (isBullet)
                {
                    b2DynamicTree_Query(kinematicTree, sweptBox, B2_DEFAULT_MASK_BITS, b2ContinuousQueryCallback, ref context);
                    b2DynamicTree_Query(dynamicTree, sweptBox, B2_DEFAULT_MASK_BITS, b2ContinuousQueryCallback, ref context);
                }
            }

            float speculativeDistance = B2_SPECULATIVE_DISTANCE;
            float aabbMargin = B2_AABB_MARGIN;

            if (context.fraction < 1.0f)
            {
                // Handle time of impact event
                B2Rot q = b2NLerp(sweep.q1, sweep.q2, context.fraction);
                B2Vec2 c = b2Lerp(sweep.c1, sweep.c2, context.fraction);
                B2Vec2 origin = b2Sub(c, b2RotateVector(q, sweep.localCenter));

                // Advance body
                B2Transform transform = new B2Transform(origin, q);
                fastBodySim.transform = transform;
                fastBodySim.center = c;
                fastBodySim.rotation0 = q;
                fastBodySim.center0 = c;

                // Update body move event
                ref B2BodyMoveEvent @event = ref b2Array_Get(ref world.bodyMoveEvents, bodySimIndex);
                @event.transform = transform;

                // Prepare AABBs for broad-phase.
                // Even though a body is fast, it may not move much. So the AABB may not need enlargement.

                shapeId = fastBody.headShapeId;
                while (shapeId != B2_NULL_INDEX)
                {
                    B2Shape shape = b2Array_Get(ref world.shapes, shapeId);

                    // Must recompute aabb at the interpolated transform
                    B2AABB aabb = b2ComputeShapeAABB(shape, transform);
                    aabb.lowerBound.X -= speculativeDistance;
                    aabb.lowerBound.Y -= speculativeDistance;
                    aabb.upperBound.X += speculativeDistance;
                    aabb.upperBound.Y += speculativeDistance;
                    shape.aabb = aabb;

                    if (b2AABB_Contains(shape.fatAABB, aabb) == false)
                    {
                        B2AABB fatAABB;
                        fatAABB.lowerBound.X = aabb.lowerBound.X - aabbMargin;
                        fatAABB.lowerBound.Y = aabb.lowerBound.Y - aabbMargin;
                        fatAABB.upperBound.X = aabb.upperBound.X + aabbMargin;
                        fatAABB.upperBound.Y = aabb.upperBound.Y + aabbMargin;
                        shape.fatAABB = fatAABB;

                        shape.enlargedAABB = true;
                        fastBodySim.flags |= (uint)B2BodyFlags.b2_enlargeBounds;
                    }

                    shapeId = shape.nextShapeId;
                }
            }
            else
            {
                // No time of impact event

                // Advance body
                fastBodySim.rotation0 = fastBodySim.transform.q;
                fastBodySim.center0 = fastBodySim.center;

                // Prepare AABBs for broad-phase
                shapeId = fastBody.headShapeId;
                while (shapeId != B2_NULL_INDEX)
                {
                    B2Shape shape = b2Array_Get(ref world.shapes, shapeId);

                    // shape.aabb is still valid from above

                    if (b2AABB_Contains(shape.fatAABB, shape.aabb) == false)
                    {
                        B2AABB fatAABB;
                        fatAABB.lowerBound.X = shape.aabb.lowerBound.X - aabbMargin;
                        fatAABB.lowerBound.Y = shape.aabb.lowerBound.Y - aabbMargin;
                        fatAABB.upperBound.X = shape.aabb.upperBound.X + aabbMargin;
                        fatAABB.upperBound.Y = shape.aabb.upperBound.Y + aabbMargin;
                        shape.fatAABB = fatAABB;

                        shape.enlargedAABB = true;
                        fastBodySim.flags |= (uint)B2BodyFlags.b2_enlargeBounds;
                    }

                    shapeId = shape.nextShapeId;
                }
            }

            // Push sensor hits on the the task context for serial processing.
            for (int i = 0; i < context.sensorCount; ++i)
            {
                // Skip any sensor hits that occurred after a solid hit
                if (context.sensorFractions[i] < context.fraction)
                {
                    b2Array_Push(ref taskContext.sensorHits, context.sensorHits[i]);
                }
            }

            b2TracyCZoneEnd(B2TracyCZone.ccd);
        }

        public static void b2FinalizeBodiesTask(int startIndex, int endIndex, uint threadIndex, object context)
        {
            b2TracyCZoneNC(B2TracyCZone.finalize_transfprms, "Transforms", B2HexColor.b2_colorMediumSeaGreen, true);

            B2StepContext stepContext = context as B2StepContext;
            B2World world = stepContext.world;
            bool enableSleep = world.enableSleep;
            B2BodyState[] states = stepContext.states;
            B2BodySim[] sims = stepContext.sims;
            B2Body[] bodies = world.bodies.data;
            float timeStep = stepContext.dt;
            float invTimeStep = stepContext.inv_dt;

            ushort worldId = world.worldId;

            // The body move event array should already have the correct size
            B2_ASSERT(endIndex <= world.bodyMoveEvents.count);
            B2BodyMoveEvent[] moveEvents = world.bodyMoveEvents.data;

            ref B2BitSet enlargedSimBitSet = ref world.taskContexts.data[threadIndex].enlargedSimBitSet;
            ref B2BitSet awakeIslandBitSet = ref world.taskContexts.data[threadIndex].awakeIslandBitSet;
            B2TaskContext taskContext = world.taskContexts.data[threadIndex];

            bool enableContinuous = world.enableContinuous;

            float speculativeDistance = B2_SPECULATIVE_DISTANCE;
            float aabbMargin = B2_AABB_MARGIN;

            B2_ASSERT(startIndex <= endIndex);

            for (int simIndex = startIndex; simIndex < endIndex; ++simIndex)
            {
                B2BodyState state = states[simIndex];
                B2BodySim sim = sims[simIndex];

                if (0 != (state.flags & (uint)B2BodyFlags.b2_lockLinearX))
                {
                    state.linearVelocity.X = 0.0f;
                }

                if (0 != (state.flags & (uint)B2BodyFlags.b2_lockLinearY))
                {
                    state.linearVelocity.Y = 0.0f;
                }

                if (0 != (state.flags & (uint)B2BodyFlags.b2_lockAngularZ))
                {
                    state.angularVelocity = 0.0f;
                }

                B2Vec2 v = state.linearVelocity;
                float w = state.angularVelocity;

                B2_ASSERT(b2IsValidVec2(v));
                B2_ASSERT(b2IsValidFloat(w));

                sim.center = b2Add(sim.center, state.deltaPosition);
                sim.transform.q = b2NormalizeRot(b2MulRot(state.deltaRotation, sim.transform.q));

                // Use the velocity of the farthest point on the body to account for rotation.
                float maxVelocity = b2Length(v) + b2AbsFloat(w) * sim.maxExtent;

                // Sleep needs to observe position correction as well as true velocity.
                float maxDeltaPosition = b2Length(state.deltaPosition) + b2AbsFloat(state.deltaRotation.s) * sim.maxExtent;

                // Position correction is not as important for sleep as true velocity.
                float positionSleepFactor = 0.5f;

                float sleepVelocity = b2MaxFloat(maxVelocity, positionSleepFactor * invTimeStep * maxDeltaPosition);

                // reset state deltas
                state.deltaPosition = b2Vec2_zero;
                state.deltaRotation = b2Rot_identity;

                sim.transform.p = b2Sub(sim.center, b2RotateVector(sim.transform.q, sim.localCenter));

                // cache miss here, however I need the shape list below
                B2Body body = bodies[sim.bodyId];
                body.bodyMoveIndex = simIndex;
                moveEvents[simIndex].transform = sim.transform;
                moveEvents[simIndex].bodyId = new B2BodyId(sim.bodyId + 1, worldId, body.generation);
                moveEvents[simIndex].userData = body.userData;
                moveEvents[simIndex].fellAsleep = false;

                // reset applied force and torque
                sim.force = b2Vec2_zero;
                sim.torque = 0.0f;

                body.isSpeedCapped = (sim.flags & (uint)B2BodyFlags.b2_isSpeedCapped) != 0;
                sim.flags &= ~((uint)B2BodyFlags.b2_isFast | (uint)B2BodyFlags.b2_isSpeedCapped);

                if (enableSleep == false || body.enableSleep == false || sleepVelocity > body.sleepThreshold)
                {
                    // Body is not sleepy
                    body.sleepTime = 0.0f;

                    if (body.type == B2BodyType.b2_dynamicBody && enableContinuous && maxVelocity * timeStep > 0.5f * sim.minExtent)
                    {
                        // This flag is only retained for debug draw
                        sim.flags |= (uint)B2BodyFlags.b2_isFast;

                        // Store in fast array for the continuous collision stage
                        // This is deterministic because the order of TOI sweeps doesn't matter
                        if (0 != (sim.flags & (uint)B2BodyFlags.b2_isBullet))
                        {
                            int bulletIndex = b2AtomicFetchAddInt(ref stepContext.bulletBodyCount, 1);
                            stepContext.bulletBodies[bulletIndex] = simIndex;
                        }
                        else
                        {
                            b2SolveContinuous(world, simIndex, taskContext);
                        }
                    }
                    else
                    {
                        // Body is safe to advance
                        sim.center0 = sim.center;
                        sim.rotation0 = sim.transform.q;
                    }
                }
                else
                {
                    // Body is safe to advance and is falling asleep
                    sim.center0 = sim.center;
                    sim.rotation0 = sim.transform.q;
                    body.sleepTime += timeStep;
                }

                // Any single body in an island can keep it awake
                B2Island island = b2Array_Get(ref world.islands, body.islandId);
                if (body.sleepTime < B2_TIME_TO_SLEEP)
                {
                    // keep island awake
                    int islandIndex = island.localIndex;
                    b2SetBit(ref awakeIslandBitSet, islandIndex);
                }
                else if (island.constraintRemoveCount > 0)
                {
                    // body wants to sleep but its island needs splitting first
                    if (body.sleepTime > taskContext.splitSleepTime)
                    {
                        // pick the sleepiest candidate
                        taskContext.splitIslandId = body.islandId;
                        taskContext.splitSleepTime = body.sleepTime;
                    }
                }

                // Update shapes AABBs
                B2Transform transform = sim.transform;
                bool isFast = (sim.flags & (uint)B2BodyFlags.b2_isFast) != 0;
                int shapeId = body.headShapeId;
                while (shapeId != B2_NULL_INDEX)
                {
                    B2Shape shape = b2Array_Get(ref world.shapes, shapeId);

                    if (isFast)
                    {
                        // For fast non-bullet bodies the AABB has already been updated in b2SolveContinuous
                        // For fast bullet bodies the AABB will be updated at a later stage

                        // Add to enlarged shapes regardless of AABB changes.
                        // Bit-set to keep the move array sorted
                        b2SetBit(ref enlargedSimBitSet, simIndex);
                    }
                    else
                    {
                        B2AABB aabb = b2ComputeShapeAABB(shape, transform);
                        aabb.lowerBound.X -= speculativeDistance;
                        aabb.lowerBound.Y -= speculativeDistance;
                        aabb.upperBound.X += speculativeDistance;
                        aabb.upperBound.Y += speculativeDistance;
                        shape.aabb = aabb;

                        B2_ASSERT(shape.enlargedAABB == false);

                        if (b2AABB_Contains(shape.fatAABB, aabb) == false)
                        {
                            B2AABB fatAABB;
                            fatAABB.lowerBound.X = aabb.lowerBound.X - aabbMargin;
                            fatAABB.lowerBound.Y = aabb.lowerBound.Y - aabbMargin;
                            fatAABB.upperBound.X = aabb.upperBound.X + aabbMargin;
                            fatAABB.upperBound.Y = aabb.upperBound.Y + aabbMargin;
                            shape.fatAABB = fatAABB;

                            shape.enlargedAABB = true;

                            // Bit-set to keep the move array sorted
                            b2SetBit(ref enlargedSimBitSet, simIndex);
                        }
                    }

                    shapeId = shape.nextShapeId;
                }
            }

            b2TracyCZoneEnd(B2TracyCZone.finalize_transfprms);
        }

/*
 public enum b2SolverStageType
{
    b2_stagePrepareJoints,
    b2_stagePrepareContacts,
    b2_stageIntegrateVelocities,
    b2_stageWarmStart,
    b2_stageSolve,
    b2_stageIntegratePositions,
    b2_stageRelax,
    b2_stageRestitution,
    b2_stageStoreImpulses
} b2SolverStageType;

public enum b2SolverBlockType
{
    b2_bodyBlock,
    b2_jointBlock,
    b2_contactBlock,
    b2_graphJointBlock,
    b2_graphContactBlock
} b2SolverBlockType;
*/

        public static void b2ExecuteBlock(B2SolverStage stage, B2StepContext context, B2SolverBlock block, int workerIndex)
        {
            B2SolverStageType stageType = stage.type;
            B2SolverBlockType blockType = (B2SolverBlockType)block.blockType;
            int startIndex = block.startIndex;
            int endIndex = startIndex + block.count;

            switch (stageType)
            {
                case B2SolverStageType.b2_stagePrepareJoints:
                    b2PrepareJointsTask(startIndex, endIndex, context);
                    break;

                case B2SolverStageType.b2_stagePrepareContacts:
                    b2PrepareContactsTask(startIndex, endIndex, context);
                    break;

                case B2SolverStageType.b2_stageIntegrateVelocities:
                    b2IntegrateVelocitiesTask(startIndex, endIndex, context);
                    break;

                case B2SolverStageType.b2_stageWarmStart:
                    if (blockType == B2SolverBlockType.b2_graphContactBlock)
                    {
                        b2WarmStartContactsTask(startIndex, endIndex, context, stage.colorIndex);
                    }
                    else if (blockType == B2SolverBlockType.b2_graphJointBlock)
                    {
                        b2WarmStartJointsTask(startIndex, endIndex, context, stage.colorIndex);
                    }

                    break;

                case B2SolverStageType.b2_stageSolve:
                    if (blockType == B2SolverBlockType.b2_graphContactBlock)
                    {
                        b2SolveContactsTask(startIndex, endIndex, context, stage.colorIndex, true);
                    }
                    else if (blockType == B2SolverBlockType.b2_graphJointBlock)
                    {
                        b2SolveJointsTask(startIndex, endIndex, context, stage.colorIndex, true, workerIndex);
                    }

                    break;

                case B2SolverStageType.b2_stageIntegratePositions:
                    b2IntegratePositionsTask(startIndex, endIndex, context);
                    break;

                case B2SolverStageType.b2_stageRelax:
                    if (blockType == B2SolverBlockType.b2_graphContactBlock)
                    {
                        b2SolveContactsTask(startIndex, endIndex, context, stage.colorIndex, false);
                    }
                    else if (blockType == B2SolverBlockType.b2_graphJointBlock)
                    {
                        b2SolveJointsTask(startIndex, endIndex, context, stage.colorIndex, false, workerIndex);
                    }

                    break;

                case B2SolverStageType.b2_stageRestitution:
                    if (blockType == B2SolverBlockType.b2_graphContactBlock)
                    {
                        b2ApplyRestitutionTask(startIndex, endIndex, context, stage.colorIndex);
                    }

                    break;

                case B2SolverStageType.b2_stageStoreImpulses:
                    b2StoreImpulsesTask(startIndex, endIndex, context);
                    break;
            }
        }

        public static int GetWorkerStartIndex(int workerIndex, int blockCount, int workerCount)
        {
            if (blockCount <= workerCount)
            {
                return workerIndex < blockCount ? workerIndex : B2_NULL_INDEX;
            }

            int blocksPerWorker = blockCount / workerCount;
            int remainder = blockCount - blocksPerWorker * workerCount;
            return blocksPerWorker * workerIndex + b2MinInt(remainder, workerIndex);
        }

        public static void b2ExecuteStage(B2SolverStage stage, B2StepContext context, int previousSyncIndex, int syncIndex, int workerIndex)
        {
            int completedCount = 0;
            ArraySegment<B2SolverBlock> blocks = stage.blocks;
            int blockCount = stage.blockCount;

            int expectedSyncIndex = previousSyncIndex;

            int startIndex = GetWorkerStartIndex(workerIndex, blockCount, context.workerCount);
            if (startIndex == B2_NULL_INDEX)
            {
                return;
            }

            B2_ASSERT(0 <= startIndex && startIndex < blockCount);

            int blockIndex = startIndex;

            while (b2AtomicCompareExchangeInt(ref blocks[blockIndex].syncIndex, expectedSyncIndex, syncIndex) == true)
            {
                B2_ASSERT(stage.type != B2SolverStageType.b2_stagePrepareContacts || syncIndex < 2);

                B2_ASSERT(completedCount < blockCount);

                b2ExecuteBlock(stage, context, blocks[blockIndex], workerIndex);

                completedCount += 1;
                blockIndex += 1;
                if (blockIndex >= blockCount)
                {
                    // Keep looking for work
                    blockIndex = 0;
                }

                expectedSyncIndex = previousSyncIndex;
            }

            // Search backwards for blocks
            blockIndex = startIndex - 1;
            while (true)
            {
                if (blockIndex < 0)
                {
                    blockIndex = blockCount - 1;
                }

                expectedSyncIndex = previousSyncIndex;

                if (b2AtomicCompareExchangeInt(ref blocks[blockIndex].syncIndex, expectedSyncIndex, syncIndex) == false)
                {
                    break;
                }

                b2ExecuteBlock(stage, context, blocks[blockIndex], workerIndex);
                completedCount += 1;
                blockIndex -= 1;
            }

            b2AtomicFetchAddInt(ref stage.completionCount, completedCount);
        }

        public static void b2ExecuteMainStage(B2SolverStage stage, B2StepContext context, uint syncBits)
        {
            int blockCount = stage.blockCount;
            if (blockCount == 0)
            {
                return;
            }

            int workerIndex = 0;

            if (blockCount == 1)
            {
                b2ExecuteBlock(stage, context, stage.blocks[0], workerIndex);
            }
            else
            {
                b2AtomicStoreU32(ref context.atomicSyncBits, syncBits);

                int syncIndex = (int)((syncBits >> 16) & 0xFFFF);
                B2_ASSERT(syncIndex > 0);
                int previousSyncIndex = syncIndex - 1;

                b2ExecuteStage(stage, context, previousSyncIndex, syncIndex, workerIndex);

                // todo consider using the cycle counter as well
                while (b2AtomicLoadInt(ref stage.completionCount) != blockCount)
                {
                    b2Pause();
                }

                b2AtomicStoreInt(ref stage.completionCount, 0);
            }
        }

        // This should not use the thread index because thread 0 can be called twice by enkiTS.
        public static void b2SolverTask(int startIndex, int endIndex, uint threadIndexIgnore, object taskContext)
        {
            B2_UNUSED(startIndex, endIndex, threadIndexIgnore);

            B2WorkerContext workerContext = taskContext as B2WorkerContext;
            int workerIndex = workerContext.workerIndex;
            B2StepContext context = workerContext.context;
            int activeColorCount = context.activeColorCount;
            ArraySegment<B2SolverStage> stages = context.stages;
            ref B2Profile profile = ref context.world.profile;

            if (workerIndex == 0)
            {
                // Main thread synchronizes the workers and does work itself.
                //
                // Stages are re-used by loops so that I don't need more stages for large iteration counts.
                // The sync indices grow monotonically for the body/graph/constraint groupings because they share solver blocks.
                // The stage index and sync indices are combined in to sync bits for atomic synchronization.
                // The workers need to compute the previous sync index for a given stage so that CAS works correctly. This
                // setup makes this easy to do.

                /*
                b2_stagePrepareJoints,
                b2_stagePrepareContacts,
                b2_stageIntegrateVelocities,
                b2_stageWarmStart,
                b2_stageSolve,
                b2_stageIntegratePositions,
                b2_stageRelax,
                b2_stageRestitution,
                b2_stageStoreImpulses
                */

                ulong ticks = b2GetTicks();

                int bodySyncIndex = 1;
                int stageIndex = 0;

                // This stage loops over all awake joints
                uint jointSyncIndex = 1;
                uint syncBits = (jointSyncIndex << 16) | (uint)stageIndex;
                B2_ASSERT(stages[stageIndex].type == B2SolverStageType.b2_stagePrepareJoints);
                b2ExecuteMainStage(stages[stageIndex], context, syncBits);
                stageIndex += 1;
                jointSyncIndex += 1;

                // This stage loops over all contact constraints
                uint contactSyncIndex = 1;
                syncBits = (contactSyncIndex << 16) | (uint)stageIndex;
                B2_ASSERT(stages[stageIndex].type == B2SolverStageType.b2_stagePrepareContacts);
                b2ExecuteMainStage(stages[stageIndex], context, syncBits);
                stageIndex += 1;
                contactSyncIndex += 1;

                int graphSyncIndex = 1;

                // Single-threaded overflow work. These constraints don't fit in the graph coloring.
                b2PrepareOverflowJoints(context);
                b2PrepareOverflowContacts(context);

                profile.prepareConstraints += b2GetMillisecondsAndReset(ref ticks);

                int subStepCount = context.subStepCount;
                for (int i = 0; i < subStepCount; ++i)
                {
                    // stage index restarted each iteration
                    // syncBits still increases monotonically because the upper bits increase each iteration
                    int iterStageIndex = stageIndex;

                    // integrate velocities
                    syncBits = (uint)((bodySyncIndex << 16) | iterStageIndex);
                    B2_ASSERT(stages[iterStageIndex].type == B2SolverStageType.b2_stageIntegrateVelocities);
                    b2ExecuteMainStage(stages[iterStageIndex], context, syncBits);
                    iterStageIndex += 1;
                    bodySyncIndex += 1;

                    profile.integrateVelocities += b2GetMillisecondsAndReset(ref ticks);

                    // warm start constraints
                    b2WarmStartOverflowJoints(context);
                    b2WarmStartOverflowContacts(context);

                    for (int colorIndex = 0; colorIndex < activeColorCount; ++colorIndex)
                    {
                        syncBits = (uint)((graphSyncIndex << 16) | iterStageIndex);
                        B2_ASSERT(stages[iterStageIndex].type == B2SolverStageType.b2_stageWarmStart);
                        b2ExecuteMainStage(stages[iterStageIndex], context, syncBits);
                        iterStageIndex += 1;
                    }

                    graphSyncIndex += 1;

                    profile.warmStart += b2GetMillisecondsAndReset(ref ticks);

                    // solve constraints
                    bool useBias = true;

                    for (int j = 0; j < ITERATIONS; ++j)
                    {
                        b2SolveOverflowJoints(context, useBias);
                        b2SolveOverflowContacts(context, useBias);

                        for (int colorIndex = 0; colorIndex < activeColorCount; ++colorIndex)
                        {
                            syncBits = (uint)((graphSyncIndex << 16) | iterStageIndex);
                            B2_ASSERT(stages[iterStageIndex].type == B2SolverStageType.b2_stageSolve);
                            b2ExecuteMainStage(stages[iterStageIndex], context, syncBits);
                            iterStageIndex += 1;
                        }

                        graphSyncIndex += 1;
                    }

                    profile.solveImpulses += b2GetMillisecondsAndReset(ref ticks);

                    // integrate positions
                    B2_ASSERT(stages[iterStageIndex].type == B2SolverStageType.b2_stageIntegratePositions);
                    syncBits = (uint)((bodySyncIndex << 16) | iterStageIndex);
                    b2ExecuteMainStage(stages[iterStageIndex], context, syncBits);
                    iterStageIndex += 1;
                    bodySyncIndex += 1;

                    profile.integratePositions += b2GetMillisecondsAndReset(ref ticks);

                    // relax constraints
                    useBias = false;
                    for (int j = 0; j < RELAX_ITERATIONS; ++j)
                    {
                        b2SolveOverflowJoints(context, useBias);
                        b2SolveOverflowContacts(context, useBias);

                        for (int colorIndex = 0; colorIndex < activeColorCount; ++colorIndex)
                        {
                            syncBits = (uint)((graphSyncIndex << 16) | iterStageIndex);
                            B2_ASSERT(stages[iterStageIndex].type == B2SolverStageType.b2_stageRelax);
                            b2ExecuteMainStage(stages[iterStageIndex], context, syncBits);
                            iterStageIndex += 1;
                        }

                        graphSyncIndex += 1;
                    }

                    profile.relaxImpulses += b2GetMillisecondsAndReset(ref ticks);
                }

                // advance the stage according to the sub-stepping tasks just completed
                // integrate velocities / warm start / solve / integrate positions / relax
                stageIndex += 1 + activeColorCount + ITERATIONS * activeColorCount + 1 + RELAX_ITERATIONS * activeColorCount;

                // Restitution
                {
                    b2ApplyOverflowRestitution(context);

                    int iterStageIndex = stageIndex;
                    for (int colorIndex = 0; colorIndex < activeColorCount; ++colorIndex)
                    {
                        syncBits = (uint)((graphSyncIndex << 16) | iterStageIndex);
                        B2_ASSERT(stages[iterStageIndex].type == B2SolverStageType.b2_stageRestitution);
                        b2ExecuteMainStage(stages[iterStageIndex], context, syncBits);
                        iterStageIndex += 1;
                    }

                    // graphSyncIndex += 1;
                    stageIndex += activeColorCount;
                }

                profile.applyRestitution += b2GetMillisecondsAndReset(ref ticks);

                b2StoreOverflowImpulses(context);

                syncBits = (contactSyncIndex << 16) | (uint)stageIndex;
                B2_ASSERT(stages[stageIndex].type == B2SolverStageType.b2_stageStoreImpulses);
                b2ExecuteMainStage(stages[stageIndex], context, syncBits);

                profile.storeImpulses += b2GetMillisecondsAndReset(ref ticks);

                // Signal workers to finish
                b2AtomicStoreU32(ref context.atomicSyncBits, uint.MaxValue);

                B2_ASSERT(stageIndex + 1 == context.stageCount);
                return;
            }

            // Worker spins and waits for work
            uint lastSyncBits = 0;
            // ulong maxSpinTime = 10;
            while (true)
            {
                // Spin until main thread bumps changes the sync bits. This can waste significant time overall, but it is necessary for
                // parallel simulation with graph coloring.
                uint syncBits;
                int spinCount = 0;
                while ((syncBits = b2AtomicLoadU32(ref context.atomicSyncBits)) == lastSyncBits)
                {
                    if (spinCount > 5)
                    {
                        b2Yield();
                        spinCount = 0;
                    }
                    else
                    {
                        // Using the cycle counter helps to account for variation in mm_pause timing across different
                        // CPUs. However, this is X64 only.
                        // ulong prev = __rdtsc();
                        // do
                        //{
                        //	b2Pause();
                        //}
                        // while ((__rdtsc() - prev) < maxSpinTime);
                        // maxSpinTime += 10;
                        b2Pause();
                        b2Pause();
                        spinCount += 1;
                    }
                }

                if (syncBits == uint.MaxValue)
                {
                    // sentinel hit
                    break;
                }

                int stageIndex = (int)(syncBits & 0xFFFF);
                B2_ASSERT(stageIndex < context.stageCount);

                int syncIndex = (int)((syncBits >> 16) & 0xFFFF);
                B2_ASSERT(syncIndex > 0);

                int previousSyncIndex = syncIndex - 1;

                B2SolverStage stage = stages[stageIndex];
                b2ExecuteStage(stage, context, previousSyncIndex, syncIndex, workerIndex);

                lastSyncBits = syncBits;
            }
        }

        public static void b2BulletBodyTask(int startIndex, int endIndex, uint threadIndex, object context)
        {
            B2_UNUSED(threadIndex);

            b2TracyCZoneNC(B2TracyCZone.bullet_body_task, "Bullet", B2HexColor.b2_colorLightSkyBlue, true);

            B2StepContext stepContext = context as B2StepContext;
            B2TaskContext taskContext = b2Array_Get(ref stepContext.world.taskContexts, (int)threadIndex);

            B2_ASSERT(startIndex <= endIndex);

            for (int i = startIndex; i < endIndex; ++i)
            {
                int simIndex = stepContext.bulletBodies[i];
                b2SolveContinuous(stepContext.world, simIndex, taskContext);
            }

            b2TracyCZoneEnd(B2TracyCZone.bullet_body_task);
        }


        // Solve with graph coloring
        public static void b2Solve(B2World world, B2StepContext stepContext)
        {
            world.stepIndex += 1;

            // Merge islands
            {
                b2TracyCZoneNC(B2TracyCZone.merge, "Merge", B2HexColor.b2_colorLightGoldenRodYellow, true);
                ulong mergeTicks = b2GetTicks();

                b2MergeAwakeIslands(world);

                world.profile.mergeIslands = b2GetMilliseconds(mergeTicks);
                b2TracyCZoneEnd(B2TracyCZone.merge);
            }

            // Are there any awake bodies? This scenario should not be important for profiling.
            B2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_awakeSet);
            int awakeBodyCount = awakeSet.bodySims.count;
            if (awakeBodyCount == 0)
            {
                // Nothing to simulate, however the tree rebuild must be finished.
                if (world.userTreeTask != null)
                {
                    world.finishTaskFcn(world.userTreeTask, world.userTaskContext);
                    world.userTreeTask = null;
                    world.activeTaskCount -= 1;
                }

                b2ValidateNoEnlarged(world.broadPhase);
                return;
            }

            // Solve constraints using graph coloring
            {
                // Prepare buffers for bullets
                b2AtomicStoreInt(ref stepContext.bulletBodyCount, 0);
                stepContext.bulletBodies = b2AllocateArenaItem<int>(world.arena, awakeBodyCount, "bullet bodies");

                b2TracyCZoneNC(B2TracyCZone.prepare_stages, "Prepare Stages", B2HexColor.b2_colorDarkOrange, true);
                ulong prepareTicks = b2GetTicks();

                ref B2ConstraintGraph graph = ref world.constraintGraph;
                B2GraphColor[] colors = graph.colors;

                stepContext.sims = awakeSet.bodySims.data;
                stepContext.states = awakeSet.bodyStates.data;

                // count contacts, joints, and colors
                int awakeJointCount = 0;
                int activeColorCount = 0;
                for (int i = 0; i < B2_GRAPH_COLOR_COUNT - 1; ++i)
                {
                    int perColorContactCount = colors[i].contactSims.count;
                    int perColorJointCount = colors[i].jointSims.count;
                    int occupancyCount = perColorContactCount + perColorJointCount;
                    activeColorCount += occupancyCount > 0 ? 1 : 0;
                    awakeJointCount += perColorJointCount;
                }

                // prepare for move events
                b2Array_Resize(ref world.bodyMoveEvents, awakeBodyCount);

                // Each worker receives at most M blocks of work. The workers may receive less blocks if there is not sufficient work.
                // Each block of work has a minimum number of elements (block size). This in turn may limit the number of blocks.
                // If there are many elements then the block size is increased so there are still at most M blocks of work per worker.
                // M is a tunable number that has two goals:
                // 1. keep M small to reduce overhead
                // 2. keep M large enough for other workers to be able to steal work
                // The block size is a power of two to make math efficient.

                int workerCount = world.workerCount;
                const int blocksPerWorker = 4;
                int maxBlockCount = blocksPerWorker * workerCount;

                // Configure blocks for tasks that parallel-for bodies
                int bodyBlockSize = 1 << 5;
                int bodyBlockCount;
                if (awakeBodyCount > bodyBlockSize * maxBlockCount)
                {
                    // Too many blocks, increase block size
                    bodyBlockSize = awakeBodyCount / maxBlockCount;
                    bodyBlockCount = maxBlockCount;
                }
                else
                {
                    bodyBlockCount = ((awakeBodyCount - 1) >> 5) + 1;
                }

                // Configure blocks for tasks parallel-for each active graph color
                // The blocks are a mix of SIMD contact blocks and joint blocks
                B2_ASSERT(B2FixedArray12<int>.Size == B2_GRAPH_COLOR_COUNT);
                B2FixedArray12<int> arrayActiveColorIndices = new B2FixedArray12<int>();

                B2FixedArray12<int> arrayColorContactCounts = new B2FixedArray12<int>();
                B2FixedArray12<int> arrayColorContactBlockSizes = new B2FixedArray12<int>();
                B2FixedArray12<int> arrayColorContactBlockCounts = new B2FixedArray12<int>();

                B2FixedArray12<int> arrayColorJointCounts = new B2FixedArray12<int>();
                B2FixedArray12<int> arrayColorJointBlockSizes = new B2FixedArray12<int>();
                B2FixedArray12<int> arrayColorJointBlockCounts = new B2FixedArray12<int>();

                Span<int> activeColorIndices = arrayActiveColorIndices.AsSpan();

                Span<int> colorContactCounts = arrayColorContactCounts.AsSpan();
                Span<int> colorContactBlockSizes = arrayColorContactBlockSizes.AsSpan();
                Span<int> colorContactBlockCounts = arrayColorContactBlockCounts.AsSpan();

                Span<int> colorJointCounts = arrayColorJointCounts.AsSpan();
                Span<int> colorJointBlockSizes = arrayColorJointBlockSizes.AsSpan();
                Span<int> colorJointBlockCounts = arrayColorJointBlockCounts.AsSpan();

                int graphBlockCount = 0;

                // c is the active color index
                int simdContactCount = 0;
                int c = 0;
                for (int i = 0; i < B2_GRAPH_COLOR_COUNT - 1; ++i)
                {
                    int colorContactCount = colors[i].contactSims.count;
                    int colorJointCount = colors[i].jointSims.count;

                    if (colorContactCount + colorJointCount > 0)
                    {
                        activeColorIndices[c] = i;

                        // 4/8-way SIMD
                        int colorContactCountSIMD = colorContactCount > 0 ? ((colorContactCount - 1) >> B2_SIMD_SHIFT) + 1 : 0;

                        colorContactCounts[c] = colorContactCountSIMD;

                        // determine the number of contact work blocks for this color
                        if (colorContactCountSIMD > blocksPerWorker * maxBlockCount)
                        {
                            // too many contact blocks
                            colorContactBlockSizes[c] = colorContactCountSIMD / maxBlockCount;
                            colorContactBlockCounts[c] = maxBlockCount;
                        }
                        else if (colorContactCountSIMD > 0)
                        {
                            // dividing by blocksPerWorker (4)
                            colorContactBlockSizes[c] = blocksPerWorker;
                            colorContactBlockCounts[c] = ((colorContactCountSIMD - 1) >> 2) + 1;
                        }
                        else
                        {
                            // no contacts in this color
                            colorContactBlockSizes[c] = 0;
                            colorContactBlockCounts[c] = 0;
                        }

                        colorJointCounts[c] = colorJointCount;

                        // determine number of joint work blocks for this color
                        if (colorJointCount > blocksPerWorker * maxBlockCount)
                        {
                            // too many joint blocks
                            colorJointBlockSizes[c] = colorJointCount / maxBlockCount;
                            colorJointBlockCounts[c] = maxBlockCount;
                        }
                        else if (colorJointCount > 0)
                        {
                            // dividing by blocksPerWorker (4)
                            colorJointBlockSizes[c] = blocksPerWorker;
                            colorJointBlockCounts[c] = ((colorJointCount - 1) >> 2) + 1;
                        }
                        else
                        {
                            colorJointBlockSizes[c] = 0;
                            colorJointBlockCounts[c] = 0;
                        }

                        graphBlockCount += colorContactBlockCounts[c] + colorJointBlockCounts[c];
                        simdContactCount += colorContactCountSIMD;
                        c += 1;
                    }
                }

                activeColorCount = c;

                // Gather contact pointers for easy parallel-for traversal. Some may be NULL due to SIMD remainders.
                ArraySegment<B2ContactSim> contacts = b2AllocateArenaItem<B2ContactSim>(
                    world.arena, B2_SIMD_WIDTH * simdContactCount, "contact pointers");

                // Gather joint pointers for easy parallel-for traversal.
                ArraySegment<B2JointSim> joints =
                    b2AllocateArenaItem<B2JointSim>(world.arena, awakeJointCount, "joint pointers");

                B2_ASSERT(B2FixedArray4<B2ContactConstraintSIMD>.Size == B2_SIMD_WIDTH);
                int simdConstraintSize = b2GetContactConstraintSIMDByteCount();
                ArraySegment<B2ContactConstraintSIMD> simdContactConstraints =
                    b2AllocateArenaItem<B2ContactConstraintSIMD>(world.arena, simdContactCount /** simdConstraintSize */, "contact constraint");

                int overflowContactCount = colors[B2_OVERFLOW_INDEX].contactSims.count;
                ArraySegment<B2ContactConstraint> overflowContactConstraints = b2AllocateArenaItem<B2ContactConstraint>(
                    world.arena, overflowContactCount, "overflow contact constraint");

                graph.colors[B2_OVERFLOW_INDEX].overflowConstraints = overflowContactConstraints;

                // Distribute transient constraints to each graph color and build flat arrays of contact and joint pointers
                {
                    int contactBase = 0;
                    int jointBase = 0;
                    for (int i = 0; i < activeColorCount; ++i)
                    {
                        int j = activeColorIndices[i];
                        ref B2GraphColor color = ref colors[j];

                        int colorContactCount = color.contactSims.count;

                        if (colorContactCount == 0)
                        {
                            color.simdConstraints = null;
                        }
                        else
                        {
                            //color.simdConstraints = (b2ContactConstraintSIMD*)( (byte*)simdContactConstraints + contactBase * simdConstraintSize );
                            color.simdConstraints = simdContactConstraints.Slice(contactBase);

                            for (int k = 0; k < colorContactCount; ++k)
                            {
                                contacts[B2_SIMD_WIDTH * contactBase + k] = color.contactSims.data[k];
                            }

                            // remainder
                            int colorContactCountSIMD = ((colorContactCount - 1) >> B2_SIMD_SHIFT) + 1;
                            for (int k = colorContactCount; k < B2_SIMD_WIDTH * colorContactCountSIMD; ++k)
                            {
                                contacts[B2_SIMD_WIDTH * contactBase + k] = null;
                            }

                            contactBase += colorContactCountSIMD;
                        }

                        int colorJointCount = color.jointSims.count;
                        for (int k = 0; k < colorJointCount; ++k)
                        {
                            joints[jointBase + k] = color.jointSims.data[k];
                        }

                        jointBase += colorJointCount;
                    }

                    B2_ASSERT(contactBase == simdContactCount);
                    B2_ASSERT(jointBase == awakeJointCount);
                }

                // Define work blocks for preparing contacts and storing contact impulses
                int contactBlockSize = blocksPerWorker;
                int contactBlockCount = simdContactCount > 0 ? ((simdContactCount - 1) >> 2) + 1 : 0;
                if (simdContactCount > contactBlockSize * maxBlockCount)
                {
                    // Too many blocks, increase block size
                    contactBlockSize = simdContactCount / maxBlockCount;
                    contactBlockCount = maxBlockCount;
                }

                // Define work blocks for preparing joints
                int jointBlockSize = blocksPerWorker;
                int jointBlockCount = awakeJointCount > 0 ? ((awakeJointCount - 1) >> 2) + 1 : 0;
                if (awakeJointCount > jointBlockSize * maxBlockCount)
                {
                    // Too many blocks, increase block size
                    jointBlockSize = awakeJointCount / maxBlockCount;
                    jointBlockCount = maxBlockCount;
                }

                int stageCount = 0;

                // b2_stagePrepareJoints
                stageCount += 1;
                // b2_stagePrepareContacts
                stageCount += 1;
                // b2_stageIntegrateVelocities
                stageCount += 1;
                // b2_stageWarmStart
                stageCount += activeColorCount;
                // b2_stageSolve
                stageCount += ITERATIONS * activeColorCount;
                // b2_stageIntegratePositions
                stageCount += 1;
                // b2_stageRelax
                stageCount += RELAX_ITERATIONS * activeColorCount;
                // b2_stageRestitution
                stageCount += activeColorCount;
                // b2_stageStoreImpulses
                stageCount += 1;

                ArraySegment<B2SolverStage> stages = b2AllocateArenaItem<B2SolverStage>(world.arena, stageCount, "stages");
                ArraySegment<B2SolverBlock> bodyBlocks = b2AllocateArenaItem<B2SolverBlock>(world.arena, bodyBlockCount, "body blocks");
                ArraySegment<B2SolverBlock> contactBlocks = b2AllocateArenaItem<B2SolverBlock>(world.arena, contactBlockCount, "contact blocks");
                ArraySegment<B2SolverBlock> jointBlocks = b2AllocateArenaItem<B2SolverBlock>(world.arena, jointBlockCount, "joint blocks");
                ArraySegment<B2SolverBlock> graphBlocks = b2AllocateArenaItem<B2SolverBlock>(world.arena, graphBlockCount, "graph blocks");

                // Split an awake island. This modifies:
                // - stack allocator
                // - world island array and solver set
                // - island indices on bodies, contacts, and joints
                // I'm squeezing this task in here because it may be expensive and this is a safe place to put it.
                // Note: cannot split islands in parallel with FinalizeBodies
                object splitIslandTask = null;
                if (world.splitIslandId != B2_NULL_INDEX)
                {
                    splitIslandTask = world.enqueueTaskFcn(b2SplitIslandTask, 1, 1, world, world.userTaskContext);
                    world.taskCount += 1;
                    world.activeTaskCount += splitIslandTask == null ? 0 : 1;
                }

                // Prepare body work blocks
                for (int i = 0; i < bodyBlockCount; ++i)
                {
                    B2SolverBlock block = bodyBlocks[i];
                    block.startIndex = i * bodyBlockSize;
                    block.count = (short)bodyBlockSize;
                    block.blockType = (short)B2SolverBlockType.b2_bodyBlock;
                    b2AtomicStoreInt(ref block.syncIndex, 0);
                }

                bodyBlocks[bodyBlockCount - 1].count = (short)(awakeBodyCount - (bodyBlockCount - 1) * bodyBlockSize);

                // Prepare joint work blocks
                for (int i = 0; i < jointBlockCount; ++i)
                {
                    B2SolverBlock block = jointBlocks[i];
                    block.startIndex = i * jointBlockSize;
                    block.count = (short)jointBlockSize;
                    block.blockType = (int)B2SolverBlockType.b2_jointBlock;
                    b2AtomicStoreInt(ref block.syncIndex, 0);
                }

                if (jointBlockCount > 0)
                {
                    jointBlocks[jointBlockCount - 1].count = (short)(awakeJointCount - (jointBlockCount - 1) * jointBlockSize);
                }

                // Prepare contact work blocks
                for (int i = 0; i < contactBlockCount; ++i)
                {
                    B2SolverBlock block = contactBlocks[i];
                    block.startIndex = i * contactBlockSize;
                    block.count = (short)contactBlockSize;
                    block.blockType = (int)B2SolverBlockType.b2_contactBlock;
                    b2AtomicStoreInt(ref block.syncIndex, 0);
                }

                if (contactBlockCount > 0)
                {
                    contactBlocks[contactBlockCount - 1].count =
                        (short)(simdContactCount - (contactBlockCount - 1) * contactBlockSize);
                }

                // Prepare graph work blocks
                ArraySegment<B2SolverBlock>[] graphColorBlocks = new ArraySegment<B2SolverBlock>[B2_GRAPH_COLOR_COUNT];
                ArraySegment<B2SolverBlock> baseGraphBlock = graphBlocks;

                for (int i = 0; i < activeColorCount; ++i)
                {
                    graphColorBlocks[i] = baseGraphBlock;

                    int colorJointBlockCount = colorJointBlockCounts[i];
                    int colorJointBlockSize = colorJointBlockSizes[i];
                    for (int j = 0; j < colorJointBlockCount; ++j)
                    {
                        B2SolverBlock block = baseGraphBlock[j];
                        block.startIndex = j * colorJointBlockSize;
                        block.count = (short)colorJointBlockSize;
                        block.blockType = (short)B2SolverBlockType.b2_graphJointBlock;
                        b2AtomicStoreInt(ref block.syncIndex, 0);
                    }

                    if (colorJointBlockCount > 0)
                    {
                        baseGraphBlock[colorJointBlockCount - 1].count =
                            (short)(colorJointCounts[i] - (colorJointBlockCount - 1) * colorJointBlockSize);
                        baseGraphBlock = baseGraphBlock.Slice(colorJointBlockCount);
                    }

                    int colorContactBlockCount = colorContactBlockCounts[i];
                    int colorContactBlockSize = colorContactBlockSizes[i];
                    for (int j = 0; j < colorContactBlockCount; ++j)
                    {
                        B2SolverBlock block = baseGraphBlock[j];
                        block.startIndex = j * colorContactBlockSize;
                        block.count = (short)colorContactBlockSize;
                        block.blockType = (short)B2SolverBlockType.b2_graphContactBlock;
                        b2AtomicStoreInt(ref block.syncIndex, 0);
                    }

                    if (colorContactBlockCount > 0)
                    {
                        baseGraphBlock[colorContactBlockCount - 1].count =
                            (short)(colorContactCounts[i] - (colorContactBlockCount - 1) * colorContactBlockSize);
                        baseGraphBlock = baseGraphBlock.Slice(colorContactBlockCount);
                    }
                }

                // TODO: @ikpil check!
                B2_ASSERT((baseGraphBlock.Offset - graphBlocks.Offset) == graphBlockCount);

                int stageIdx = 0;
                B2SolverStage stage = stages[stageIdx];

                // Prepare joints
                stage.type = B2SolverStageType.b2_stagePrepareJoints;
                stage.blocks = jointBlocks;
                stage.blockCount = jointBlockCount;
                stage.colorIndex = -1;
                b2AtomicStoreInt(ref stage.completionCount, 0);
                stage = stages[++stageIdx];

                // Prepare contacts
                stage.type = B2SolverStageType.b2_stagePrepareContacts;
                stage.blocks = contactBlocks;
                stage.blockCount = contactBlockCount;
                stage.colorIndex = -1;
                b2AtomicStoreInt(ref stage.completionCount, 0);
                stage = stages[++stageIdx];

                // Integrate velocities
                stage.type = B2SolverStageType.b2_stageIntegrateVelocities;
                stage.blocks = bodyBlocks;
                stage.blockCount = bodyBlockCount;
                stage.colorIndex = -1;
                b2AtomicStoreInt(ref stage.completionCount, 0);
                stage = stages[++stageIdx];

                // Warm start
                for (int i = 0; i < activeColorCount; ++i)
                {
                    stage.type = B2SolverStageType.b2_stageWarmStart;
                    stage.blocks = graphColorBlocks[i];
                    stage.blockCount = colorJointBlockCounts[i] + colorContactBlockCounts[i];
                    stage.colorIndex = activeColorIndices[i];
                    b2AtomicStoreInt(ref stage.completionCount, 0);
                    stage = stages[++stageIdx];
                }

                // Solve graph
                for (int j = 0; j < ITERATIONS; ++j)
                {
                    for (int i = 0; i < activeColorCount; ++i)
                    {
                        stage.type = B2SolverStageType.b2_stageSolve;
                        stage.blocks = graphColorBlocks[i];
                        stage.blockCount = colorJointBlockCounts[i] + colorContactBlockCounts[i];
                        stage.colorIndex = activeColorIndices[i];
                        b2AtomicStoreInt(ref stage.completionCount, 0);
                        stage = stages[++stageIdx];
                    }
                }

                // Integrate positions
                stage.type = B2SolverStageType.b2_stageIntegratePositions;
                stage.blocks = bodyBlocks;
                stage.blockCount = bodyBlockCount;
                stage.colorIndex = -1;
                b2AtomicStoreInt(ref stage.completionCount, 0);
                stage = stages[++stageIdx];

                // Relax constraints
                for (int j = 0; j < RELAX_ITERATIONS; ++j)
                {
                    for (int i = 0; i < activeColorCount; ++i)
                    {
                        stage.type = B2SolverStageType.b2_stageRelax;
                        stage.blocks = graphColorBlocks[i];
                        stage.blockCount = colorJointBlockCounts[i] + colorContactBlockCounts[i];
                        stage.colorIndex = activeColorIndices[i];
                        b2AtomicStoreInt(ref stage.completionCount, 0);
                        stage = stages[++stageIdx];
                    }
                }

                // Restitution
                // Note: joint blocks mixed in, could have joint limit restitution
                for (int i = 0; i < activeColorCount; ++i)
                {
                    stage.type = B2SolverStageType.b2_stageRestitution;
                    stage.blocks = graphColorBlocks[i];
                    stage.blockCount = colorJointBlockCounts[i] + colorContactBlockCounts[i];
                    stage.colorIndex = activeColorIndices[i];
                    b2AtomicStoreInt(ref stage.completionCount, 0);
                    stage = stages[++stageIdx];
                }

                // Store impulses
                stage.type = B2SolverStageType.b2_stageStoreImpulses;
                stage.blocks = contactBlocks;
                stage.blockCount = contactBlockCount;
                stage.colorIndex = -1;
                b2AtomicStoreInt(ref stage.completionCount, 0);
                stage = stages[++stageIdx];

                //B2_ASSERT( (int)( stage - stages ) == stageCount );
                B2_ASSERT((int)(stageIdx) == stageCount);

                B2_ASSERT(workerCount <= B2_MAX_WORKERS);
                B2_ASSERT(world.tempWorkerContext.Length <= B2_MAX_WORKERS);
                //b2WorkerContext[] workerContext = new b2WorkerContext[B2_MAX_WORKERS];
                Span<B2WorkerContext> workerContext = world.tempWorkerContext;
                for (int i = 0; i < workerContext.Length; ++i)
                {
                    workerContext[i].Clear();
                }

                stepContext.graph = graph;
                stepContext.joints = joints;
                stepContext.contacts = contacts;
                stepContext.simdContactConstraints = simdContactConstraints;
                stepContext.activeColorCount = activeColorCount;
                stepContext.workerCount = workerCount;
                stepContext.stageCount = stageCount;
                stepContext.stages = stages;
                b2AtomicStoreU32(ref stepContext.atomicSyncBits, 0);

                world.profile.prepareStages = b2GetMillisecondsAndReset(ref prepareTicks);
                b2TracyCZoneEnd(B2TracyCZone.prepare_stages);

                b2TracyCZoneNC(B2TracyCZone.solve_constraints, "Solve Constraints", B2HexColor.b2_colorIndigo, true);
                ulong constraintTicks = b2GetTicks();

                // Must use worker index because thread 0 can be assigned multiple tasks by enkiTS
                int jointIdCapacity = b2GetIdCapacity(world.jointIdPool);
                for (int i = 0; i < workerCount; ++i)
                {
                    B2TaskContext taskContext = b2Array_Get(ref world.taskContexts, i);
                    b2SetBitCountAndClear(ref taskContext.jointStateBitSet, jointIdCapacity);

                    workerContext[i].context = stepContext;
                    workerContext[i].workerIndex = i;
                    workerContext[i].userTask = world.enqueueTaskFcn(b2SolverTask, 1, 1, workerContext[i], world.userTaskContext);
                    world.taskCount += 1;
                    world.activeTaskCount += workerContext[i].userTask == null ? 0 : 1;
                }

                // Finish island split
                if (splitIslandTask != null)
                {
                    world.finishTaskFcn(splitIslandTask, world.userTaskContext);
                    world.activeTaskCount -= 1;
                }

                world.splitIslandId = B2_NULL_INDEX;

                // Finish constraint solve
                for (int i = 0; i < workerCount; ++i)
                {
                    if (workerContext[i].userTask != null)
                    {
                        world.finishTaskFcn(workerContext[i].userTask, world.userTaskContext);
                        world.activeTaskCount -= 1;
                    }
                }

                world.profile.solveConstraints = b2GetMillisecondsAndReset(ref constraintTicks);
                b2TracyCZoneEnd(B2TracyCZone.solve_constraints);

                b2TracyCZoneNC(B2TracyCZone.update_transforms, "Update Transforms", B2HexColor.b2_colorMediumSeaGreen, true);
                ulong transformTicks = b2GetTicks();

                // Prepare contact, enlarged body, and island bit sets used in body finalization.
                int awakeIslandCount = awakeSet.islandSims.count;
                for (int i = 0; i < world.workerCount; ++i)
                {
                    B2TaskContext taskContext = world.taskContexts.data[i];
                    b2Array_Clear(ref taskContext.sensorHits);
                    b2SetBitCountAndClear(ref taskContext.enlargedSimBitSet, awakeBodyCount);
                    b2SetBitCountAndClear(ref taskContext.awakeIslandBitSet, awakeIslandCount);
                    taskContext.splitIslandId = B2_NULL_INDEX;
                    taskContext.splitSleepTime = 0.0f;
                }

                // Finalize bodies. Must happen after the constraint solver and after island splitting.
                object finalizeBodiesTask =
                    world.enqueueTaskFcn(b2FinalizeBodiesTask, awakeBodyCount, 64, stepContext, world.userTaskContext);
                world.taskCount += 1;
                if (finalizeBodiesTask != null)
                {
                    world.finishTaskFcn(finalizeBodiesTask, world.userTaskContext);
                }

                b2FreeArenaItem(world.arena, graphBlocks);
                b2FreeArenaItem(world.arena, jointBlocks);
                b2FreeArenaItem(world.arena, contactBlocks);
                b2FreeArenaItem(world.arena, bodyBlocks);
                b2FreeArenaItem(world.arena, stages);
                b2FreeArenaItem(world.arena, overflowContactConstraints);
                b2FreeArenaItem(world.arena, simdContactConstraints);
                b2FreeArenaItem(world.arena, joints);
                b2FreeArenaItem(world.arena, contacts);

                world.profile.transforms = b2GetMilliseconds(transformTicks);
                b2TracyCZoneEnd(B2TracyCZone.update_transforms);
            }

            // Report joint events
            {
                b2TracyCZoneNC(B2TracyCZone.joint_events, "Joint Events", B2HexColor.b2_colorPeru, true);
                ulong jointEventTicks = b2GetTicks();

                // Gather bits for all joints that have force/torque events
                ref B2BitSet jointStateBitSet = ref world.taskContexts.data[0].jointStateBitSet;
                for (int i = 1; i < world.workerCount; ++i)
                {
                    b2InPlaceUnion(ref jointStateBitSet, ref world.taskContexts.data[i].jointStateBitSet);
                }

                {
                    uint wordCount = (uint)jointStateBitSet.blockCount;
                    ulong[] bits = jointStateBitSet.bits;

                    B2Joint[] jointArray = world.joints.data;
                    int jointCapacity = world.joints.capacity;
                    ushort worldIndex0 = world.worldId;

                    for (uint k = 0; k < wordCount; ++k)
                    {
                        ulong word = bits[k];
                        while (word != 0)
                        {
                            uint ctz = b2CTZ64(word);
                            int jointId = (int)(64 * k + ctz);

                            B2_ASSERT(jointId < jointCapacity);

                            B2Joint joint = jointArray[jointId];

                            B2_ASSERT(joint.setIndex == (int)B2SetType.b2_awakeSet);

                            B2JointEvent @event = new B2JointEvent();
                            @event.jointId = new B2JointId(jointId + 1, worldIndex0, joint.generation);
                            @event.userData = joint.userData;

                            b2Array_Push(ref world.jointEvents, @event);

                            // Clear the smallest set bit
                            word = word & (word - 1);
                        }
                    }
                }

                world.profile.jointEvents = b2GetMilliseconds(jointEventTicks);
                b2TracyCZoneEnd(B2TracyCZone.joint_events);
            }

            // Report hit events
            // todo_erin perhaps optimize this with a bitset
            // todo_erin perhaps do this in parallel with other work below
            {
                b2TracyCZoneNC(B2TracyCZone.hit_events, "Hit Events", B2HexColor.b2_colorRosyBrown, true);
                ulong hitTicks = b2GetTicks();

                B2_ASSERT(world.contactHitEvents.count == 0);

                float threshold = world.hitEventThreshold;
                B2GraphColor[] colors = world.constraintGraph.colors;
                for (int i = 0; i < B2_GRAPH_COLOR_COUNT; ++i)
                {
                    ref B2GraphColor color = ref colors[i];
                    int contactCount = color.contactSims.count;
                    B2ContactSim[] contactSims = color.contactSims.data;
                    for (int j = 0; j < contactCount; ++j)
                    {
                        B2ContactSim contactSim = contactSims[j];
                        if ((contactSim.simFlags & (uint)B2ContactSimFlags.b2_simEnableHitEvent) == 0)
                        {
                            continue;
                        }

                        B2ContactHitEvent @event = new B2ContactHitEvent();
                        @event.approachSpeed = threshold;

                        bool hit = false;
                        int pointCount = contactSim.manifold.pointCount;
                        for (int k = 0; k < pointCount; ++k)
                        {
                            ref B2ManifoldPoint mp = ref contactSim.manifold.points[k];
                            float approachSpeed = -mp.normalVelocity;

                            // Need to check total impulse because the point may be speculative and not colliding
                            if (approachSpeed > @event.approachSpeed && mp.totalNormalImpulse > 0.0f)
                            {
                                @event.approachSpeed = approachSpeed;
                                @event.point = mp.point;
                                hit = true;
                            }
                        }

                        if (hit == true)
                        {
                            @event.normal = contactSim.manifold.normal;

                            B2Shape shapeA = b2Array_Get(ref world.shapes, contactSim.shapeIdA);
                            B2Shape shapeB = b2Array_Get(ref world.shapes, contactSim.shapeIdB);

                            @event.shapeIdA = new B2ShapeId(shapeA.id + 1, world.worldId, shapeA.generation);
                            @event.shapeIdB = new B2ShapeId(shapeB.id + 1, world.worldId, shapeB.generation);

                            b2Array_Push(ref world.contactHitEvents, @event);
                        }
                    }
                }

                world.profile.hitEvents = b2GetMilliseconds(hitTicks);
                b2TracyCZoneEnd(B2TracyCZone.hit_events);
            }

            {
                b2TracyCZoneNC(B2TracyCZone.refit_bvh, "Refit BVH", B2HexColor.b2_colorFireBrick, true);
                ulong refitTicks = b2GetTicks();

                // Finish the user tree task that was queued earlier in the time step. This must be complete before touching the
                // broad-phase.
                if (world.userTreeTask != null)
                {
                    world.finishTaskFcn(world.userTreeTask, world.userTaskContext);
                    world.userTreeTask = null;
                    world.activeTaskCount -= 1;
                }

                b2ValidateNoEnlarged(world.broadPhase);

                // Gather bits for all sim bodies that have enlarged AABBs
                ref B2BitSet enlargedBodyBitSet = ref world.taskContexts.data[0].enlargedSimBitSet;
                for (int i = 1; i < world.workerCount; ++i)
                {
                    b2InPlaceUnion(ref enlargedBodyBitSet, ref world.taskContexts.data[i].enlargedSimBitSet);
                }

                // Enlarge broad-phase proxies and build move array
                // Apply shape AABB changes to broad-phase. This also create the move array which must be
                // in deterministic order. I'm tracking sim bodies because the number of shape ids can be huge.
                // This has to happen before bullets are processed.
                {
                    B2BroadPhase broadPhase = world.broadPhase;
                    uint wordCount = (uint)enlargedBodyBitSet.blockCount;
                    ulong[] bits = enlargedBodyBitSet.bits;

                    // Fast array access is important here
                    B2Body[] bodyArray = world.bodies.data;
                    B2BodySim[] bodySimArray = awakeSet.bodySims.data;
                    B2Shape[] shapeArray = world.shapes.data;

                    for (uint k = 0; k < wordCount; ++k)
                    {
                        ulong word = bits[k];
                        while (word != 0)
                        {
                            uint ctz = b2CTZ64(word);
                            uint bodySimIndex = 64 * k + ctz;

                            B2BodySim bodySim = bodySimArray[bodySimIndex];

                            B2Body body = bodyArray[bodySim.bodyId];

                            int shapeId = body.headShapeId;
                            if ((bodySim.flags & ((uint)B2BodyFlags.b2_isBullet | (uint)B2BodyFlags.b2_isFast)) == ((uint)B2BodyFlags.b2_isBullet | (uint)B2BodyFlags.b2_isFast))
                            {
                                // Fast bullet bodies don't have their final AABB yet
                                while (shapeId != B2_NULL_INDEX)
                                {
                                    B2Shape shape = shapeArray[shapeId];

                                    // Shape is fast. It's aabb will be enlarged in continuous collision.
                                    // Update the move array here for determinism because bullets are processed
                                    // below in non-deterministic order.
                                    b2BufferMove(broadPhase, shape.proxyKey);

                                    shapeId = shape.nextShapeId;
                                }
                            }
                            else
                            {
                                while (shapeId != B2_NULL_INDEX)
                                {
                                    B2Shape shape = shapeArray[shapeId];

                                    // The AABB may not have been enlarged, despite the body being flagged as enlarged.
                                    // For example, a body with multiple shapes may have not have all shapes enlarged.
                                    // A fast body may have been flagged as enlarged despite having no shapes enlarged.
                                    if (shape.enlargedAABB)
                                    {
                                        b2BroadPhase_EnlargeProxy(broadPhase, shape.proxyKey, shape.fatAABB);
                                        shape.enlargedAABB = false;
                                    }

                                    shapeId = shape.nextShapeId;
                                }
                            }

                            // Clear the smallest set bit
                            word = word & (word - 1);
                        }
                    }
                }

                b2ValidateBroadphase(world.broadPhase);

                world.profile.refit = b2GetMilliseconds(refitTicks);
                b2TracyCZoneEnd(B2TracyCZone.refit_bvh);
            }

            int bulletBodyCount = b2AtomicLoadInt(ref stepContext.bulletBodyCount);
            if (bulletBodyCount > 0)
            {
                b2TracyCZoneNC(B2TracyCZone.bullets, "Bullets", B2HexColor.b2_colorLightYellow, true);
                ulong bulletTicks = b2GetTicks();

                // Fast bullet bodies
                // Note: a bullet body may be moving slow
                int minRange = 8;
                object userBulletBodyTask = world.enqueueTaskFcn(b2BulletBodyTask, bulletBodyCount, minRange, stepContext,
                    world.userTaskContext);
                world.taskCount += 1;
                if (userBulletBodyTask != null)
                {
                    world.finishTaskFcn(userBulletBodyTask, world.userTaskContext);
                }

                // Serially enlarge broad-phase proxies for bullet shapes
                B2BroadPhase broadPhase = world.broadPhase;
                B2DynamicTree dynamicTree = broadPhase.trees[(int)B2BodyType.b2_dynamicBody];

                // Fast array access is important here
                B2Body[] bodyArray = world.bodies.data;
                B2BodySim[] bodySimArray = awakeSet.bodySims.data;
                B2Shape[] shapeArray = world.shapes.data;

                // Serially enlarge broad-phase proxies for bullet shapes
                ArraySegment<int> bulletBodySimIndices = stepContext.bulletBodies;

                // This loop has non-deterministic order but it shouldn't affect the result
                for (int i = 0; i < bulletBodyCount; ++i)
                {
                    B2BodySim bulletBodySim = bodySimArray[bulletBodySimIndices[i]];
                    if ((bulletBodySim.flags & (uint)B2BodyFlags.b2_enlargeBounds) == 0)
                    {
                        continue;
                    }

                    // Clear flag
                    bulletBodySim.flags &= ~(uint)B2BodyFlags.b2_enlargeBounds;

                    int bodyId = bulletBodySim.bodyId;
                    B2_ASSERT(0 <= bodyId && bodyId < world.bodies.count);
                    B2Body bulletBody = bodyArray[bodyId];

                    int shapeId = bulletBody.headShapeId;
                    while (shapeId != B2_NULL_INDEX)
                    {
                        B2Shape shape = shapeArray[shapeId];
                        if (shape.enlargedAABB == false)
                        {
                            shapeId = shape.nextShapeId;
                            continue;
                        }

                        // Clear flag
                        shape.enlargedAABB = false;

                        int proxyKey = shape.proxyKey;
                        int proxyId = B2_PROXY_ID(proxyKey);
                        B2_ASSERT(B2_PROXY_TYPE(proxyKey) == B2BodyType.b2_dynamicBody);

                        // all fast bullet shapes should already be in the move buffer
                        B2_ASSERT(b2ContainsKey(ref broadPhase.moveSet, (ulong)(proxyKey + 1)));

                        b2DynamicTree_EnlargeProxy(dynamicTree, proxyId, shape.fatAABB);

                        shapeId = shape.nextShapeId;
                    }
                }

                world.profile.bullets = b2GetMilliseconds(bulletTicks);
                b2TracyCZoneEnd(B2TracyCZone.bullets);
            }

            // Need to free this even if no bullets got processed.
            b2FreeArenaItem(world.arena, stepContext.bulletBodies);
            stepContext.bulletBodies = null;
            b2AtomicStoreInt(ref stepContext.bulletBodyCount, 0);

            // Report sensor hits. This may include bullets sensor hits.
            {
                b2TracyCZoneNC(B2TracyCZone.sensor_hits, "Sensor Hits", B2HexColor.b2_colorPowderBlue, true);
                ulong sensorHitTicks = b2GetTicks();

                int workerCount = world.workerCount;
                B2_ASSERT(workerCount == world.taskContexts.count);

                for (int i = 0; i < workerCount; ++i)
                {
                    B2TaskContext taskContext = world.taskContexts.data[i];
                    int hitCount = taskContext.sensorHits.count;
                    Span<B2SensorHit> hits = taskContext.sensorHits.data;

                    for (int j = 0; j < hitCount; ++j)
                    {
                        B2SensorHit hit = hits[j];
                        B2Shape sensorShape = b2Array_Get(ref world.shapes, hit.sensorId);
                        B2Shape visitor = b2Array_Get(ref world.shapes, hit.visitorId);

                        B2Sensor sensor = b2Array_Get(ref world.sensors, sensorShape.sensorIndex);
                        B2Visitor shapeRef = new B2Visitor()
                        {
                            shapeId = hit.visitorId,
                            generation = visitor.generation,
                        };
                        b2Array_Push(ref sensor.hits, shapeRef);
                    }
                }

                world.profile.sensorHits = b2GetMilliseconds(sensorHitTicks);
                b2TracyCZoneEnd(B2TracyCZone.sensor_hits);
            }

            // Island sleeping
            // This must be done last because putting islands to sleep invalidates the enlarged body bits.
            // todo_erin figure out how to do this in parallel with tree refit
            if (world.enableSleep == true)
            {
                b2TracyCZoneNC(B2TracyCZone.sleep_islands, "Island Sleep", B2HexColor.b2_colorLightSlateGray, true);
                ulong sleepTicks = b2GetTicks();

                // Collect split island candidate for the next time step. No need to split if sleeping is disabled.
                B2_ASSERT(world.splitIslandId == B2_NULL_INDEX);
                float splitSleepTimer = 0.0f;
                for (int i = 0; i < world.workerCount; ++i)
                {
                    B2TaskContext taskContext = world.taskContexts.data[i];
                    if (taskContext.splitIslandId != B2_NULL_INDEX && taskContext.splitSleepTime >= splitSleepTimer)
                    {
                        B2_ASSERT(taskContext.splitSleepTime > 0.0f);

                        // Tie breaking for determinism. Largest island id wins. Needed due to work stealing.
                        if (taskContext.splitSleepTime == splitSleepTimer && taskContext.splitIslandId < world.splitIslandId)
                        {
                            continue;
                        }

                        world.splitIslandId = taskContext.splitIslandId;
                        splitSleepTimer = taskContext.splitSleepTime;
                    }
                }

                ref B2BitSet awakeIslandBitSet = ref world.taskContexts.data[0].awakeIslandBitSet;
                for (int i = 1; i < world.workerCount; ++i)
                {
                    b2InPlaceUnion(ref awakeIslandBitSet, ref world.taskContexts.data[i].awakeIslandBitSet);
                }

                // Need to process in reverse because this moves islands to sleeping solver sets.
                B2IslandSim[] islands = awakeSet.islandSims.data;
                int count = awakeSet.islandSims.count;
                for (int islandIndex = count - 1; islandIndex >= 0; islandIndex -= 1)
                {
                    if (b2GetBit(ref awakeIslandBitSet, islandIndex) == true)
                    {
                        // this island is still awake
                        continue;
                    }

                    B2IslandSim island = islands[islandIndex];
                    int islandId = island.islandId;

                    b2TrySleepIsland(world, islandId);
                }

                b2ValidateSolverSets(world);

                world.profile.sleepIslands = b2GetMilliseconds(sleepTicks);
                b2TracyCZoneEnd(B2TracyCZone.sleep_islands);
            }
        }
    }
}