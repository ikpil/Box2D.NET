// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Cores;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Profiling;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2ConstraintGraphs;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Solvers;
using static Box2D.NET.B2BitSets;

namespace Box2D.NET
{
    public static class B2ContactSolvers
    {
        // contact separation for sub-stepping
        // s = s0 + dot(cB + rB - cA - rA, normal)
        // normal is held constant
        // body positions c can translation and anchors r can rotate
        // s(t) = s0 + dot(cB(t) + rB(t) - cA(t) - rA(t), normal)
        // s(t) = s0 + dot(cB0 + dpB + rot(dqB, rB0) - cA0 - dpA - rot(dqA, rA0), normal)
        // s(t) = s0 + dot(cB0 - cA0, normal) + dot(dpB - dpA + rot(dqB, rB0) - rot(dqA, rA0), normal)
        // s_base = s0 + dot(cB0 - cA0, normal)
        internal static void b2PrepareContacts_Overflow(B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.prepare_overflow_contact, "Prepare Overflow Contact", B2HexColor.b2_colorYellow, true);

            B2World world = context.world;
            ref B2ConstraintGraph graph = ref context.graph;
            ref B2GraphColor color = ref graph.colors[B2_OVERFLOW_INDEX];
            Span<B2ContactConstraint> constraints = color.overflowConstraints;

            int contactCount = color.contactSims.count;
            B2ContactSim[] contacts = color.contactSims.data;

#if DEBUG
            B2Body[] bodies = world.bodies.data;
#endif

            // Stiffer for static contacts to avoid bodies getting pushed through the ground
            B2Softness contactSoftness = context.contactSoftness;
            B2Softness staticSoftness = context.staticSoftness;

            float warmStartScale = world.enableWarmStarting ? 1.0f : 0.0f;

            for (int i = 0; i < contactCount; ++i)
            {
                B2ContactSim contactSim = contacts[i];

                ref B2Manifold manifold = ref contactSim.manifold;
                int pointCount = manifold.pointCount;

                B2_ASSERT(0 < pointCount && pointCount <= 2);

                int indexA = b2DecodeAwakeIndex(contactSim.encodedBodySimA);
                int indexB = b2DecodeAwakeIndex(contactSim.encodedBodySimB);

#if DEBUG
                B2Body bodyA = bodies[contactSim.bodyIdA];
                B2_ASSERT(contactSim.encodedBodySimA == b2EncodeBodySimIndex(bodyA));

                B2Body bodyB = bodies[contactSim.bodyIdB];
                B2_ASSERT(contactSim.encodedBodySimB == b2EncodeBodySimIndex(bodyB));
#endif

                ref B2ContactConstraint constraint = ref constraints[i];

                // 0 is null
                constraint.indexA = indexA + 1;
                constraint.indexB = indexB + 1;
                constraint.normal = manifold.normal;
                constraint.friction = contactSim.friction;
                constraint.rollingResistance = contactSim.rollingResistance;
                constraint.rollingImpulse = warmStartScale * manifold.rollingImpulse;
                constraint.tangentSpeed = contactSim.tangentSpeed;
                constraint.pointCount = pointCount;

                float mA = contactSim.invMassA;
                float iA = contactSim.invIA;
                float mB = contactSim.invMassB;
                float iB = contactSim.invIB;

                if (indexA == B2_NULL_INDEX || indexB == B2_NULL_INDEX)
                {
                    constraint.softness = staticSoftness;
                }
                else
                {
                    constraint.softness = contactSoftness;
                }

                // copy mass into constraint to avoid cache misses during sub-stepping
                constraint.invMassA = mA;
                constraint.invIA = iA;
                constraint.invMassB = mB;
                constraint.invIB = iB;

                {
                    float k = iA + iB;
                    constraint.rollingMass = k > 0.0f ? 1.0f / k : 0.0f;
                }

                B2Vec2 normal = constraint.normal;
                B2Vec2 tangent = b2RightPerp(constraint.normal);

                for (int j = 0; j < pointCount; ++j)
                {
                    ref B2ManifoldPoint mp = ref manifold.points[j];
                    ref B2ContactConstraintPoint cp = ref constraint.points[j];

                    cp.normalImpulse = warmStartScale * mp.normalImpulse;
                    cp.tangentImpulse = warmStartScale * mp.tangentImpulse;
                    cp.totalNormalImpulse = 0.0f;

                    B2Vec2 rA = mp.anchorA;
                    B2Vec2 rB = mp.anchorB;

                    cp.anchorA = rA;
                    cp.anchorB = rB;
                    cp.baseSeparation = mp.separation - b2Dot(b2Sub(rB, rA), normal);

                    float rnA = b2Cross(rA, normal);
                    float rnB = b2Cross(rB, normal);
                    float kNormal = mA + mB + iA * rnA * rnA + iB * rnB * rnB;
                    cp.normalMass = kNormal > 0.0f ? 1.0f / kNormal : 0.0f;

                    float rtA = b2Cross(rA, tangent);
                    float rtB = b2Cross(rB, tangent);
                    float kTangent = mA + mB + iA * rtA * rtA + iB * rtB * rtB;
                    cp.tangentMass = kTangent > 0.0f ? 1.0f / kTangent : 0.0f;

                    cp.restitutionVelocity = mp.restitutionVelocity;
                }
            }

            b2TracyCZoneEnd(B2TracyCZone.prepare_overflow_contact);
        }

        internal static void b2WarmStartContacts_Overflow(B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.warmstart_overflow_contact, "WarmStart Overflow Contact", B2HexColor.b2_colorDarkOrange, true);

            ref B2ConstraintGraph graph = ref context.graph;
            ref B2GraphColor color = ref graph.colors[B2_OVERFLOW_INDEX];
            Span<B2ContactConstraint> constraints = color.overflowConstraints;
            int contactCount = color.contactSims.count;
            B2World world = context.world;
            B2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)B2SolverSetType.b2_awakeSet);
            B2BodyState[] states = awakeSet.bodyStates.data;

            // This is a dummy state to represent a static body because static bodies don't have a solver body.
            B2BodyState dummyState = B2BodyState.Create(b2_identityBodyState);

            for (int i = 0; i < contactCount; ++i)
            {
                ref B2ContactConstraint constraint = ref constraints[i];

                int indexA = constraint.indexA - 1;
                int indexB = constraint.indexB - 1;

                B2BodyState stateA = indexA == B2_NULL_INDEX ? dummyState : states[indexA];
                B2BodyState stateB = indexB == B2_NULL_INDEX ? dummyState : states[indexB];

                B2Vec2 vA = stateA.linearVelocity;
                float wA = stateA.angularVelocity;
                B2Vec2 vB = stateB.linearVelocity;
                float wB = stateB.angularVelocity;

                float mA = constraint.invMassA;
                float iA = constraint.invIA;
                float mB = constraint.invMassB;
                float iB = constraint.invIB;

                // Stiffer for static contacts to avoid bodies getting pushed through the ground
                B2Vec2 normal = constraint.normal;
                B2Vec2 tangent = b2RightPerp(constraint.normal);
                int pointCount = constraint.pointCount;

                for (int j = 0; j < pointCount; ++j)
                {
                    ref B2ContactConstraintPoint cp = ref constraint.points[j];

                    // fixed anchors
                    B2Vec2 rA = cp.anchorA;
                    B2Vec2 rB = cp.anchorB;

                    B2Vec2 P = b2Add(b2MulSV(cp.normalImpulse, normal), b2MulSV(cp.tangentImpulse, tangent));

                    cp.totalNormalImpulse += cp.normalImpulse;

                    wA -= iA * b2Cross(rA, P);
                    vA = b2MulAdd(vA, -mA, P);
                    wB += iB * b2Cross(rB, P);
                    vB = b2MulAdd(vB, mB, P);
                }

                wA -= iA * constraint.rollingImpulse;
                wB += iB * constraint.rollingImpulse;

                if (0 != (stateA.flags & (uint)B2BodyFlags.b2_dynamicFlag))
                {
                    stateA.linearVelocity = vA;
                    stateA.angularVelocity = wA;
                }

                if (0 != (stateB.flags & (uint)B2BodyFlags.b2_dynamicFlag))
                {
                    stateB.linearVelocity = vB;
                    stateB.angularVelocity = wB;
                }
            }

            b2TracyCZoneEnd(B2TracyCZone.warmstart_overflow_contact);
        }

        internal static void b2SolveContacts_Overflow(B2StepContext context, bool useBias)
        {
            b2TracyCZoneNC(B2TracyCZone.solve_contact, "Solve Overflow Contact", B2HexColor.b2_colorAliceBlue, true);

            ref B2ConstraintGraph graph = ref context.graph;
            ref B2GraphColor color = ref graph.colors[B2_OVERFLOW_INDEX];
            Span<B2ContactConstraint> constraints = color.overflowConstraints;
            int contactCount = color.contactSims.count;
            B2World world = context.world;
            B2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)B2SolverSetType.b2_awakeSet);
            B2BodyState[] states = awakeSet.bodyStates.data;

            float inv_h = context.inv_h;
            float contactSpeed = context.world.contactSpeed;

            // This is a dummy body to represent a static body since static bodies don't have a solver body.
            B2BodyState dummyState = B2BodyState.Create(b2_identityBodyState);

            for (int i = 0; i < contactCount; ++i)
            {
                ref B2ContactConstraint constraint = ref constraints[i];
                float mA = constraint.invMassA;
                float iA = constraint.invIA;
                float mB = constraint.invMassB;
                float iB = constraint.invIB;

                int indexA = constraint.indexA - 1;
                int indexB = constraint.indexB - 1;

                B2BodyState stateA = indexA == B2_NULL_INDEX ? dummyState : states[indexA];
                B2Vec2 vA = stateA.linearVelocity;
                float wA = stateA.angularVelocity;
                B2Rot dqA = stateA.deltaRotation;

                B2BodyState stateB = indexB == B2_NULL_INDEX ? dummyState : states[indexB];
                B2Vec2 vB = stateB.linearVelocity;
                float wB = stateB.angularVelocity;
                B2Rot dqB = stateB.deltaRotation;

                B2Vec2 dp = b2Sub(stateB.deltaPosition, stateA.deltaPosition);

                B2Vec2 normal = constraint.normal;
                B2Vec2 tangent = b2RightPerp(normal);
                float friction = constraint.friction;
                B2Softness softness = constraint.softness;

                int pointCount = constraint.pointCount;
                float totalNormalImpulse = 0.0f;
                bool stopRestitution = true;

                for (int j = 0; j < pointCount; ++j)
                {
                    ref B2ContactConstraintPoint cp = ref constraint.points[j];

                    // fixed anchor points
                    B2Vec2 rA = cp.anchorA;
                    B2Vec2 rB = cp.anchorB;

                    // compute current separation
                    // this is subject to round-off error if the anchor is far from the body center of mass
                    B2Vec2 ds = b2Add(dp, b2Sub(b2RotateVector(dqB, rB), b2RotateVector(dqA, rA)));
                    float s = cp.baseSeparation + b2Dot(ds, normal);

                    float velocityBias = 0.0f;
                    float massScale = 1.0f;
                    float impulseScale = 0.0f;
                    if (s > 0.0f)
                    {
                        // speculative bias - positive
                        velocityBias = s * inv_h;
                    }
                    else if (useBias)
                    {
                        // overlap bias - negative
                        velocityBias = b2MaxFloat(softness.massScale * softness.biasRate * s, -contactSpeed);
                        massScale = softness.massScale;
                        impulseScale = softness.impulseScale;
                    }

                    // relative normal velocity at contact
                    B2Vec2 vrA = b2Add(vA, b2CrossSV(wA, rA));
                    B2Vec2 vrB = b2Add(vB, b2CrossSV(wB, rB));
                    float vn = b2Dot(b2Sub(vrB, vrA), normal);

                    // Restitution in the relax phase only.
                    if (useBias == false && cp.restitutionVelocity > 0.0f)
                    {
                        velocityBias = b2MinFloat(velocityBias, -cp.restitutionVelocity);
                        stopRestitution = stopRestitution && s > 0.0f;
                    }

                    // incremental normal impulse
                    float impulse = -cp.normalMass * (massScale * vn + velocityBias) - impulseScale * cp.normalImpulse;

                    // clamp the accumulated impulse
                    float newImpulse = b2MaxFloat(cp.normalImpulse + impulse, 0.0f);
                    impulse = newImpulse - cp.normalImpulse;
                    cp.normalImpulse = newImpulse;
                    cp.totalNormalImpulse += impulse;

                    // b2Log( "vn %g impulse %g bias %g", vn, newImpulse, velocityBias );

                    // This is used for rolling resistance
                    totalNormalImpulse += newImpulse;

                    // apply normal impulse
                    B2Vec2 P = b2MulSV(impulse, normal);
                    vA = b2MulSub(vA, mA, P);
                    wA -= iA * b2Cross(rA, P);

                    vB = b2MulAdd(vB, mB, P);
                    wB += iB * b2Cross(rB, P);
                }

                if (useBias == false)
                {
                    // Stop applying restitution when all points are separated.
                    if (stopRestitution)
                    {
                        constraint.points[0].restitutionVelocity = 0.0f;
                        constraint.points[1].restitutionVelocity = 0.0f;
                    }

                    // Rolling resistance
                    {
                        float deltaLambda = -constraint.rollingMass * (wB - wA);
                        float lambda = constraint.rollingImpulse;
                        float maxLambda = constraint.rollingResistance * totalNormalImpulse;
                        constraint.rollingImpulse = b2ClampFloat(lambda + deltaLambda, -maxLambda, maxLambda);
                        deltaLambda = constraint.rollingImpulse - lambda;

                        wA -= iA * deltaLambda;
                        wB += iB * deltaLambda;
                    }

                    // Friction
                    for (int j = 0; j < pointCount; ++j)
                    {
                        ref B2ContactConstraintPoint cp = ref constraint.points[j];

                        // fixed anchor points
                        B2Vec2 rA = cp.anchorA;
                        B2Vec2 rB = cp.anchorB;

                        // relative tangent velocity at contact
                        B2Vec2 vrB = b2Add(vB, b2CrossSV(wB, rB));
                        B2Vec2 vrA = b2Add(vA, b2CrossSV(wA, rA));

                        // vt = dot(vrB - sB * tangent - (vrA + sA * tangent), tangent)
                        //    = dot(vrB - vrA, tangent) - (sA + sB)

                        float vt = b2Dot(b2Sub(vrB, vrA), tangent) - constraint.tangentSpeed;

                        // incremental tangent impulse
                        float impulse = cp.tangentMass * (-vt);

                        // clamp the accumulated force
                        float maxFriction = friction * cp.normalImpulse;
                        float newImpulse = b2ClampFloat(cp.tangentImpulse + impulse, -maxFriction, maxFriction);
                        impulse = newImpulse - cp.tangentImpulse;
                        cp.tangentImpulse = newImpulse;

                        // apply tangent impulse
                        B2Vec2 P = b2MulSV(impulse, tangent);
                        vA = b2MulSub(vA, mA, P);
                        wA -= iA * b2Cross(rA, P);
                        vB = b2MulAdd(vB, mB, P);
                        wB += iB * b2Cross(rB, P);
                    }

                }

                if (0 != (stateA.flags & (uint)B2BodyFlags.b2_dynamicFlag))
                {
                    stateA.linearVelocity = vA;
                    stateA.angularVelocity = wA;
                }

                if (0 != (stateB.flags & (uint)B2BodyFlags.b2_dynamicFlag))
                {
                    stateB.linearVelocity = vB;
                    stateB.angularVelocity = wB;
                }
            }

            b2TracyCZoneEnd(B2TracyCZone.solve_contact);
        }

        internal static void b2StoreImpulses_Overflow(B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.store_impulses, "Store", B2HexColor.b2_colorFireBrick, true);

            B2World world = context.world;
            ref B2ConstraintGraph graph = ref context.graph;
            ref B2GraphColor color = ref graph.colors[B2_OVERFLOW_INDEX];
            Span<B2ContactConstraint> constraints = color.overflowConstraints;
            int contactCount = color.contactSims.count;
            B2ContactSim[] contactSims = color.contactSims.data;
            B2TaskContext taskContext = world.taskContexts.data[0];
            ref B2BitSet hitEventBitSet = ref taskContext.hitEventBitSet;

            float negHitThreshold = -world.hitEventThreshold;
            bool hasHitEvents = taskContext.hasHitEvents;

            for (int i = 0; i < contactCount; ++i)
            {
                ref readonly B2ContactConstraint constraint = ref constraints[i];
                B2ContactSim contactSim = contactSims[i];
                ref B2Manifold manifold = ref contactSim.manifold;
                int pointCount = manifold.pointCount;

                for (int j = 0; j < pointCount; ++j)
                {
                    ref B2ManifoldPoint mp = ref manifold.points[j];
                    mp.normalImpulse = constraint.points[j].normalImpulse;
                    mp.tangentImpulse = constraint.points[j].tangentImpulse;
                    mp.totalNormalImpulse = constraint.points[j].totalNormalImpulse;
                }

                if ((contactSim.simFlags & (uint)B2ContactFlags.b2_simEnableHitEvent) != 0)
                {
                    for (int j = 0; j < contactSim.manifold.pointCount; ++j)
                    {
                        ref B2ManifoldPoint mp = ref manifold.points[j];

                        // Need to check total impulse because the point may be speculative and not colliding
                        if (mp.normalVelocity < negHitThreshold && mp.totalNormalImpulse > 0.0f)
                        {
                            b2SetBit(ref hitEventBitSet, contactSim.contactId);
                            hasHitEvents = true;
                            break;
                        }
                    }
                }

                manifold.rollingImpulse = constraint.rollingImpulse;
            }

            taskContext.hasHitEvents = hasHitEvents;

            b2TracyCZoneEnd(B2TracyCZone.store_impulses);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> b2AddW(Vector<float> a, Vector<float> b)
        {
            return a + b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> b2SubW(Vector<float> a, Vector<float> b)
        {
            return a - b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> b2MulW(Vector<float> a, Vector<float> b)
        {
            return a * b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> b2MulAddW(Vector<float> a, Vector<float> b, Vector<float> c)
        {
            return (b * c) + a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> b2MulSubW(Vector<float> a, Vector<float> b, Vector<float> c)
        {
            return a - (b * c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> b2MinW(Vector<float> a, Vector<float> b)
        {
            return Vector.Min(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> b2MaxW(Vector<float> a, Vector<float> b)
        {
            return Vector.Max(a, b);
        }

        // a = clamp(a, -b, b)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> b2SymClampW(Vector<float> a, Vector<float> b)
        {
            // a = clamp(a, -b, b)
            Vector<float> min = Vector.Min(a, b);
            return Vector.Max(-b, min);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> b2OrW(Vector<float> a, Vector<float> b)
        {
            Vector<int> aZeroMask = Vector.Equals(a, Vector<float>.Zero);
            Vector<int> bZeroMask = Vector.Equals(b, Vector<float>.Zero);

            // Zero only where both lanes are zero, matching a != 0 || b != 0
            Vector<int> zeroMask = aZeroMask & bZeroMask;

            return Vector.ConditionalSelect(zeroMask, Vector<float>.Zero, Vector<float>.One);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> b2GreaterThanW(Vector<float> a, Vector<float> b)
        {
            var mask = Vector.GreaterThan(a, b);
            return Vector.ConditionalSelect(mask, Vector<float>.One, Vector<float>.Zero);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> b2EqualsW(Vector<float> a, Vector<float> b)
        {
            var mask = Vector.Equals(a, b);
            return Vector.ConditionalSelect(mask, Vector<float>.One, Vector<float>.Zero);
        }

        // component-wise returns mask ? b : a
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> b2BlendW(Vector<float> a, Vector<float> b, Vector<float> mask)
        {
            // component-wise returns mask ? b : a
            var mask2 = Vector.Equals(mask, Vector<float>.Zero);
            return Vector.ConditionalSelect(mask2, a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2AllZeroW(Vector<float> a)
        {
            return Vector.EqualsAll(a, Vector<float>.Zero);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2ZeroW()
        {
            return new B2FloatW(0.0f, 0.0f, 0.0f, 0.0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2SplatW(float scalar)
        {
            return new B2FloatW(scalar, scalar, scalar, scalar);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2SetW(float a, float b, float c, float d)
        {
            return new B2FloatW(a, b, c, d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2NegW(in B2FloatW a)
        {
            return new B2FloatW(-a.X, -a.Y, -a.Z, -a.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2AddW(in B2FloatW a, in B2FloatW b)
        {
            return new B2FloatW(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2SubW(in B2FloatW a, in B2FloatW b)
        {
            return new B2FloatW(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2MulW(in B2FloatW a, in B2FloatW b)
        {
            return new B2FloatW(a.X * b.X, a.Y * b.Y, a.Z * b.Z, a.W * b.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2DivW(in B2FloatW a, in B2FloatW b)
        {
            return new B2FloatW(a.X / b.X, a.Y / b.Y, a.Z / b.Z, a.W / b.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2MulAddW(in B2FloatW a, in B2FloatW b, in B2FloatW c)
        {
            return new B2FloatW(a.X + b.X * c.X, a.Y + b.Y * c.Y, a.Z + b.Z * c.Z, a.W + b.W * c.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2MulSubW(in B2FloatW a, in B2FloatW b, in B2FloatW c)
        {
            return new B2FloatW(a.X - b.X * c.X, a.Y - b.Y * c.Y, a.Z - b.Z * c.Z, a.W - b.W * c.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2MinW(in B2FloatW a, in B2FloatW b)
        {
            return new B2FloatW(
                a.X <= b.X ? a.X : b.X,
                a.Y <= b.Y ? a.Y : b.Y,
                a.Z <= b.Z ? a.Z : b.Z,
                a.W <= b.W ? a.W : b.W
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2MaxW(in B2FloatW a, in B2FloatW b)
        {
            return new B2FloatW(
                a.X >= b.X ? a.X : b.X,
                a.Y >= b.Y ? a.Y : b.Y,
                a.Z >= b.Z ? a.Z : b.Z,
                a.W >= b.W ? a.W : b.W
            );
        }

        // a = clamp(a, -b, b)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2SymClampW(in B2FloatW a, in B2FloatW b)
        {
            // a = clamp(a, -b, b)
            return new B2FloatW(
                b2ClampFloat(a.X, -b.X, b.X),
                b2ClampFloat(a.Y, -b.Y, b.Y),
                b2ClampFloat(a.Z, -b.Z, b.Z),
                b2ClampFloat(a.W, -b.W, b.W)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2OrW(in B2FloatW a, in B2FloatW b)
        {
            return new B2FloatW(
                a.X != 0.0f || b.X != 0.0f ? 1.0f : 0.0f,
                a.Y != 0.0f || b.Y != 0.0f ? 1.0f : 0.0f,
                a.Z != 0.0f || b.Z != 0.0f ? 1.0f : 0.0f,
                a.W != 0.0f || b.W != 0.0f ? 1.0f : 0.0f
            );
        }

        // a & ~b
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2AndNotW(in B2FloatW a, in B2FloatW b)
        {
            return new B2FloatW(
                a.X != 0.0f && b.X == 0.0f ? 1.0f : 0.0f,
                a.Y != 0.0f && b.Y == 0.0f ? 1.0f : 0.0f,
                a.Z != 0.0f && b.Z == 0.0f ? 1.0f : 0.0f,
                a.W != 0.0f && b.W == 0.0f ? 1.0f : 0.0f
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2GreaterThanW(in B2FloatW a, in B2FloatW b)
        {
            return new B2FloatW(
                a.X > b.X ? 1.0f : 0.0f,
                a.Y > b.Y ? 1.0f : 0.0f,
                a.Z > b.Z ? 1.0f : 0.0f,
                a.W > b.W ? 1.0f : 0.0f
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2EqualsW(in B2FloatW a, in B2FloatW b)
        {
            // TODO: @ikpil check float equal
            return new B2FloatW(
                a.X == b.X ? 1.0f : 0.0f,
                a.Y == b.Y ? 1.0f : 0.0f,
                a.Z == b.Z ? 1.0f : 0.0f,
                a.W == b.W ? 1.0f : 0.0f
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2AllZeroW(in B2FloatW a)
        {
            return a.X == 0.0f && a.Y == 0.0f && a.Z == 0.0f && a.W == 0.0f;
        }

        // component-wise returns mask ? b : a
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2BlendW(in B2FloatW a, in B2FloatW b, in B2FloatW mask)
        {
            // component-wise returns mask ? b : a
            return new B2FloatW()
            {
                X = mask.X != 0.0f ? b.X : a.X,
                Y = mask.Y != 0.0f ? b.Y : a.Y,
                Z = mask.Z != 0.0f ? b.Z : a.Z,
                W = mask.W != 0.0f ? b.W : a.W,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2SoftMaskW(ReadOnlySpan<int> indexA, ReadOnlySpan<int> indexB)
        {
            return new B2FloatW(
                indexA[0] == 0 || indexB[0] == 0 ? 1.0f : 0.0f,
                indexA[1] == 0 || indexB[1] == 0 ? 1.0f : 0.0f,
                indexA[2] == 0 || indexB[2] == 0 ? 1.0f : 0.0f,
                indexA[3] == 0 || indexB[3] == 0 ? 1.0f : 0.0f
            );
        }

        // a - b
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2W b2SubVW(in B2Vec2W a, in B2Vec2W b)
        {
            return new B2Vec2W(b2SubW(a.X, b.X), b2SubW(a.Y, b.Y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2W b2RightPerpW(in B2Vec2W a)
        {
            return new B2Vec2W(a.Y, b2NegW(a.X));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2DotW(in B2Vec2W a, in B2Vec2W b)
        {
            return b2AddW(b2MulW(a.X, b.X), b2MulW(a.Y, b.Y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2CrossW(in B2Vec2W a, in B2Vec2W b)
        {
            return b2SubW(b2MulW(a.X, b.Y), b2MulW(a.Y, b.X));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2W b2RotateVectorW(in B2RotW q, in B2Vec2W v)
        {
            return new B2Vec2W(b2SubW(b2MulW(q.C, v.X), b2MulW(q.S, v.Y)), b2AddW(b2MulW(q.S, v.X), b2MulW(q.C, v.Y)));
        }


        // This function allows hiding SIMD intrinsics in the source file to improve compilation performance.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int b2GetWideContactConstraintByteCount()
        {
            return B2SizeOf<B2ContactConstraintWide>.Size;
        }


// Custom gather/scatter for each SIMD type
#if B2_SIMD_AVX2
// This is a load and 8x8 transpose
static b2BodyStateW b2GatherBodies( const b2BodyState* states, int* indices )
{
	B2_ASSERT( sizeof( b2BodyState ) == 32, "b2BodyState not 32 bytes" );
	B2_ASSERT( ( (uintptr_t)states & 0x1F ) == 0 );

	// zero means null
	int i1 = indices[0] - 1;
	int i2 = indices[1] - 1;
	int i3 = indices[2] - 1;
	int i4 = indices[3] - 1;
	int i5 = indices[4] - 1;
	int i6 = indices[5] - 1;
	int i7 = indices[6] - 1;
	int i8 = indices[7] - 1;

	// b2BodyState b2_identityBodyState = {{0.0f, 0.0f}, 0.0f, 0, {0.0f, 0.0f}, {1.0f, 0.0f}};
	b2FloatW identity = _mm256_setr_ps( 0.0f, 0.0f, 0.0f, 0, 0.0f, 0.0f, 1.0f, 0.0f );
	b2FloatW b0 = i1 == B2_NULL_INDEX ? identity : _mm256_load_ps( (float*)( states + i1 ) );
	b2FloatW b1 = i2 == B2_NULL_INDEX ? identity : _mm256_load_ps( (float*)( states + i2 ) );
	b2FloatW b2 = i3 == B2_NULL_INDEX ? identity : _mm256_load_ps( (float*)( states + i3 ) );
	b2FloatW b3 = i4 == B2_NULL_INDEX ? identity : _mm256_load_ps( (float*)( states + i4 ) );
	b2FloatW b4 = i5 == B2_NULL_INDEX ? identity : _mm256_load_ps( (float*)( states + i5 ) );
	b2FloatW b5 = i6 == B2_NULL_INDEX ? identity : _mm256_load_ps( (float*)( states + i6 ) );
	b2FloatW b6 = i7 == B2_NULL_INDEX ? identity : _mm256_load_ps( (float*)( states + i7 ) );
	b2FloatW b7 = i8 == B2_NULL_INDEX ? identity : _mm256_load_ps( (float*)( states + i8 ) );

	b2FloatW t0 = _mm256_unpacklo_ps( b0, b1 );
	b2FloatW t1 = _mm256_unpackhi_ps( b0, b1 );
	b2FloatW t2 = _mm256_unpacklo_ps( b2, b3 );
	b2FloatW t3 = _mm256_unpackhi_ps( b2, b3 );
	b2FloatW t4 = _mm256_unpacklo_ps( b4, b5 );
	b2FloatW t5 = _mm256_unpackhi_ps( b4, b5 );
	b2FloatW t6 = _mm256_unpacklo_ps( b6, b7 );
	b2FloatW t7 = _mm256_unpackhi_ps( b6, b7 );
	b2FloatW tt0 = _mm256_shuffle_ps( t0, t2, _MM_SHUFFLE( 1, 0, 1, 0 ) );
	b2FloatW tt1 = _mm256_shuffle_ps( t0, t2, _MM_SHUFFLE( 3, 2, 3, 2 ) );
	b2FloatW tt2 = _mm256_shuffle_ps( t1, t3, _MM_SHUFFLE( 1, 0, 1, 0 ) );
	b2FloatW tt3 = _mm256_shuffle_ps( t1, t3, _MM_SHUFFLE( 3, 2, 3, 2 ) );
	b2FloatW tt4 = _mm256_shuffle_ps( t4, t6, _MM_SHUFFLE( 1, 0, 1, 0 ) );
	b2FloatW tt5 = _mm256_shuffle_ps( t4, t6, _MM_SHUFFLE( 3, 2, 3, 2 ) );
	b2FloatW tt6 = _mm256_shuffle_ps( t5, t7, _MM_SHUFFLE( 1, 0, 1, 0 ) );
	b2FloatW tt7 = _mm256_shuffle_ps( t5, t7, _MM_SHUFFLE( 3, 2, 3, 2 ) );

	b2BodyStateW simdBody;
	simdBody.v.X = _mm256_permute2f128_ps( tt0, tt4, 0x20 );
	simdBody.v.Y = _mm256_permute2f128_ps( tt1, tt5, 0x20 );
	simdBody.w = _mm256_permute2f128_ps( tt2, tt6, 0x20 );
	simdBody.flags = _mm256_permute2f128_ps( tt3, tt7, 0x20 );
	simdBody.dp.X = _mm256_permute2f128_ps( tt0, tt4, 0x31 );
	simdBody.dp.Y = _mm256_permute2f128_ps( tt1, tt5, 0x31 );
	simdBody.dq.C = _mm256_permute2f128_ps( tt2, tt6, 0x31 );
	simdBody.dq.S = _mm256_permute2f128_ps( tt3, tt7, 0x31 );
	return simdBody;
}

// This writes everything back to the solver bodies but only the velocities change
static void b2ScatterBodies( b2BodyState* states, int* indices, const b2BodyStateW* simdBody )
{
	B2_ASSERT( sizeof( b2BodyState ) == 32, "b2BodyState not 32 bytes" );
	B2_ASSERT( ( (uintptr_t)states & 0x1F ) == 0 );
	b2FloatW t0 = _mm256_unpacklo_ps( simdBody.v.X, simdBody.v.Y );
	b2FloatW t1 = _mm256_unpackhi_ps( simdBody.v.X, simdBody.v.Y );
	b2FloatW t2 = _mm256_unpacklo_ps( simdBody.w, simdBody.flags );
	b2FloatW t3 = _mm256_unpackhi_ps( simdBody.w, simdBody.flags );
	b2FloatW t4 = _mm256_unpacklo_ps( simdBody.dp.X, simdBody.dp.Y );
	b2FloatW t5 = _mm256_unpackhi_ps( simdBody.dp.X, simdBody.dp.Y );
	b2FloatW t6 = _mm256_unpacklo_ps( simdBody.dq.C, simdBody.dq.S );
	b2FloatW t7 = _mm256_unpackhi_ps( simdBody.dq.C, simdBody.dq.S );
	b2FloatW tt0 = _mm256_shuffle_ps( t0, t2, _MM_SHUFFLE( 1, 0, 1, 0 ) );
	b2FloatW tt1 = _mm256_shuffle_ps( t0, t2, _MM_SHUFFLE( 3, 2, 3, 2 ) );
	b2FloatW tt2 = _mm256_shuffle_ps( t1, t3, _MM_SHUFFLE( 1, 0, 1, 0 ) );
	b2FloatW tt3 = _mm256_shuffle_ps( t1, t3, _MM_SHUFFLE( 3, 2, 3, 2 ) );
	b2FloatW tt4 = _mm256_shuffle_ps( t4, t6, _MM_SHUFFLE( 1, 0, 1, 0 ) );
	b2FloatW tt5 = _mm256_shuffle_ps( t4, t6, _MM_SHUFFLE( 3, 2, 3, 2 ) );
	b2FloatW tt6 = _mm256_shuffle_ps( t5, t7, _MM_SHUFFLE( 1, 0, 1, 0 ) );
	b2FloatW tt7 = _mm256_shuffle_ps( t5, t7, _MM_SHUFFLE( 3, 2, 3, 2 ) );

	// I don't use any dummy body in the body array because this will lead to multithreaded sharing and the
	// associated cache flushing.

	// zero means null
	int i1 = indices[0] - 1;
	int i2 = indices[1] - 1;
	int i3 = indices[2] - 1;
	int i4 = indices[3] - 1;
	int i5 = indices[4] - 1;
	int i6 = indices[5] - 1;
	int i7 = indices[6] - 1;
	int i8 = indices[7] - 1;

	if ( i1 != B2_NULL_INDEX && ( states[i1].flags & b2_dynamicFlag ) != 0 )
		_mm256_store_ps( (float*)( states + i1 ), _mm256_permute2f128_ps( tt0, tt4, 0x20 ) );
	if ( i2 != B2_NULL_INDEX && ( states[i2].flags & b2_dynamicFlag ) != 0 )
		_mm256_store_ps( (float*)( states + i2 ), _mm256_permute2f128_ps( tt1, tt5, 0x20 ) );
	if ( i3 != B2_NULL_INDEX && ( states[i3].flags & b2_dynamicFlag ) != 0 )
		_mm256_store_ps( (float*)( states + i3 ), _mm256_permute2f128_ps( tt2, tt6, 0x20 ) );
	if ( i4 != B2_NULL_INDEX && ( states[i4].flags & b2_dynamicFlag ) != 0 )
		_mm256_store_ps( (float*)( states + i4 ), _mm256_permute2f128_ps( tt3, tt7, 0x20 ) );
	if ( i5 != B2_NULL_INDEX && ( states[i5].flags & b2_dynamicFlag ) != 0 )
		_mm256_store_ps( (float*)( states + i5 ), _mm256_permute2f128_ps( tt0, tt4, 0x31 ) );
	if ( i6 != B2_NULL_INDEX && ( states[i6].flags & b2_dynamicFlag ) != 0 )
		_mm256_store_ps( (float*)( states + i6 ), _mm256_permute2f128_ps( tt1, tt5, 0x31 ) );
	if ( i7 != B2_NULL_INDEX && ( states[i7].flags & b2_dynamicFlag ) != 0 )
		_mm256_store_ps( (float*)( states + i7 ), _mm256_permute2f128_ps( tt2, tt6, 0x31 ) );
	if ( i8 != B2_NULL_INDEX && ( states[i8].flags & b2_dynamicFlag ) != 0 )
		_mm256_store_ps( (float*)( states + i8 ), _mm256_permute2f128_ps( tt3, tt7, 0x31 ) );
}

#elif B2_SIMD_NEON
// This is a load and transpose
static b2BodyStateW b2GatherBodies( const b2BodyState* states, int* indices )
{
	B2_ASSERT( sizeof( b2BodyState ) == 32, "b2BodyState not 32 bytes" );
	B2_ASSERT( ( (uintptr_t)states & 0x1F ) == 0 );

	// [vx vy w flags]
	b2FloatW identityA = b2ZeroW();

	// [dpx dpy dqc dqs]

	b2FloatW identityB = b2SetW( 0.0f, 0.0f, 1.0f, 0.0f );

	// zero means null
	int i1 = indices[0] - 1;
	int i2 = indices[1] - 1;
	int i3 = indices[2] - 1;
	int i4 = indices[3] - 1;

	b2FloatW b1a = i1 == B2_NULL_INDEX ? identityA : b2LoadW( (float*)( states + i1 ) + 0 );
	b2FloatW b1b = i1 == B2_NULL_INDEX ? identityB : b2LoadW( (float*)( states + i1 ) + 4 );
	b2FloatW b2a = i2 == B2_NULL_INDEX ? identityA : b2LoadW( (float*)( states + i2 ) + 0 );
	b2FloatW b2b = i2 == B2_NULL_INDEX ? identityB : b2LoadW( (float*)( states + i2 ) + 4 );
	b2FloatW b3a = i3 == B2_NULL_INDEX ? identityA : b2LoadW( (float*)( states + i3 ) + 0 );
	b2FloatW b3b = i3 == B2_NULL_INDEX ? identityB : b2LoadW( (float*)( states + i3 ) + 4 );
	b2FloatW b4a = i4 == B2_NULL_INDEX ? identityA : b2LoadW( (float*)( states + i4 ) + 0 );
	b2FloatW b4b = i4 == B2_NULL_INDEX ? identityB : b2LoadW( (float*)( states + i4 ) + 4 );

	// [vx1 vx3 vy1 vy3]
	b2FloatW t1a = b2UnpackLoW( b1a, b3a );

	// [vx2 vx4 vy2 vy4]
	b2FloatW t2a = b2UnpackLoW( b2a, b4a );

	// [w1 w3 f1 f3]
	b2FloatW t3a = b2UnpackHiW( b1a, b3a );

	// [w2 w4 f2 f4]
	b2FloatW t4a = b2UnpackHiW( b2a, b4a );

	b2BodyStateW simdBody;
	simdBody.v.X = b2UnpackLoW( t1a, t2a );
	simdBody.v.Y = b2UnpackHiW( t1a, t2a );
	simdBody.w = b2UnpackLoW( t3a, t4a );
	simdBody.flags = b2UnpackHiW( t3a, t4a );

	b2FloatW t1b = b2UnpackLoW( b1b, b3b );
	b2FloatW t2b = b2UnpackLoW( b2b, b4b );
	b2FloatW t3b = b2UnpackHiW( b1b, b3b );
	b2FloatW t4b = b2UnpackHiW( b2b, b4b );

	simdBody.dp.X = b2UnpackLoW( t1b, t2b );
	simdBody.dp.Y = b2UnpackHiW( t1b, t2b );
	simdBody.dq.C = b2UnpackLoW( t3b, t4b );
	simdBody.dq.S = b2UnpackHiW( t3b, t4b );

	return simdBody;
}

// This writes only the velocities back to the solver bodies
// https://developer.arm.com/documentation/102107a/0100/Floating-point-4x4-matrix-transposition
static void b2ScatterBodies( b2BodyState* states, int* indices, const b2BodyStateW* simdBody )
{
	B2_ASSERT( sizeof( b2BodyState ) == 32, "b2BodyState not 32 bytes" );
	B2_ASSERT( ( (uintptr_t)states & 0x1F ) == 0 );

	//	b2FloatW x = b2SetW(0.0f, 1.0f, 2.0f, 3.0f);
	//	b2FloatW y = b2SetW(4.0f, 5.0f, 6.0f, 7.0f);
	//	b2FloatW z = b2SetW(8.0f, 9.0f, 10.0f, 11.0f);
	//	b2FloatW w = b2SetW(12.0f, 13.0f, 14.0f, 15.0f);
	//
	//	float32x4x2_t rr1 = vtrnq_f32( x, y );
	//	float32x4x2_t rr2 = vtrnq_f32( z, w );
	//
	//	float32x4_t b1 = vcombine_f32(vget_low_f32(rr1.val[0]), vget_low_f32(rr2.val[0]));
	//	float32x4_t b2 = vcombine_f32(vget_low_f32(rr1.val[1]), vget_low_f32(rr2.val[1]));
	//	float32x4_t b3 = vcombine_f32(vget_high_f32(rr1.val[0]), vget_high_f32(rr2.val[0]));
	//	float32x4_t b4 = vcombine_f32(vget_high_f32(rr1.val[1]), vget_high_f32(rr2.val[1]));

	// transpose
	float32x4x2_t r1 = vtrnq_f32( simdBody.v.X, simdBody.v.Y );
	float32x4x2_t r2 = vtrnq_f32( simdBody.w, simdBody.flags );

	// zero means null
	int i1 = indices[0] - 1;
	int i2 = indices[1] - 1;
	int i3 = indices[2] - 1;
	int i4 = indices[3] - 1;

	// I don't use any dummy body in the body array because this will lead to multithreaded sharing and the
	// associated cache flushing.
	if ( i1 != B2_NULL_INDEX && ( states[i1].flags & b2_dynamicFlag ) != 0 )
	{
		float32x4_t body1 = vcombine_f32( vget_low_f32( r1.val[0] ), vget_low_f32( r2.val[0] ) );
		b2StoreW( (float*)( states + i1 ), body1 );
	}

	if ( i2 != B2_NULL_INDEX && ( states[i2].flags & b2_dynamicFlag ) != 0 )
	{
		float32x4_t body2 = vcombine_f32( vget_low_f32( r1.val[1] ), vget_low_f32( r2.val[1] ) );
		b2StoreW( (float*)( states + i2 ), body2 );
	}

	if ( i3 != B2_NULL_INDEX && ( states[i3].flags & b2_dynamicFlag ) != 0 )
	{
		float32x4_t body3 = vcombine_f32( vget_high_f32( r1.val[0] ), vget_high_f32( r2.val[0] ) );
		b2StoreW( (float*)( states + i3 ), body3 );
	}

	if ( i4 != B2_NULL_INDEX && ( states[i4].flags & b2_dynamicFlag ) != 0 )
	{
		float32x4_t body4 = vcombine_f32( vget_high_f32( r1.val[1] ), vget_high_f32( r2.val[1] ) );
		b2StoreW( (float*)( states + i4 ), body4 );
	}
}

#elif B2_SIMD_SSE2
// This is a load and transpose
static b2BodyStateW b2GatherBodies( const b2BodyState* states, int* indices )
{
	B2_ASSERT( sizeof( b2BodyState ) == 32, "b2BodyState not 32 bytes" );
	B2_ASSERT( ( (uintptr_t)states & 0x1F ) == 0 );

	// [vx vy w flags]
	b2FloatW identityA = b2ZeroW();

	// [dpx dpy dqc dqs]
	b2FloatW identityB = b2SetW( 0.0f, 0.0f, 1.0f, 0.0f );

	// zero means null
	int i1 = indices[0] - 1;
	int i2 = indices[1] - 1;
	int i3 = indices[2] - 1;
	int i4 = indices[3] - 1;

	b2FloatW b1a = i1 == B2_NULL_INDEX ? identityA : b2LoadW( (float*)( states + i1 ) + 0 );
	b2FloatW b1b = i1 == B2_NULL_INDEX ? identityB : b2LoadW( (float*)( states + i1 ) + 4 );
	b2FloatW b2a = i2 == B2_NULL_INDEX ? identityA : b2LoadW( (float*)( states + i2 ) + 0 );
	b2FloatW b2b = i2 == B2_NULL_INDEX ? identityB : b2LoadW( (float*)( states + i2 ) + 4 );
	b2FloatW b3a = i3 == B2_NULL_INDEX ? identityA : b2LoadW( (float*)( states + i3 ) + 0 );
	b2FloatW b3b = i3 == B2_NULL_INDEX ? identityB : b2LoadW( (float*)( states + i3 ) + 4 );
	b2FloatW b4a = i4 == B2_NULL_INDEX ? identityA : b2LoadW( (float*)( states + i4 ) + 0 );
	b2FloatW b4b = i4 == B2_NULL_INDEX ? identityB : b2LoadW( (float*)( states + i4 ) + 4 );

	// [vx1 vx3 vy1 vy3]
	b2FloatW t1a = b2UnpackLoW( b1a, b3a );

	// [vx2 vx4 vy2 vy4]
	b2FloatW t2a = b2UnpackLoW( b2a, b4a );

	// [w1 w3 f1 f3]
	b2FloatW t3a = b2UnpackHiW( b1a, b3a );

	// [w2 w4 f2 f4]
	b2FloatW t4a = b2UnpackHiW( b2a, b4a );

	b2BodyStateW simdBody;
	simdBody.v.X = b2UnpackLoW( t1a, t2a );
	simdBody.v.Y = b2UnpackHiW( t1a, t2a );
	simdBody.w = b2UnpackLoW( t3a, t4a );
	simdBody.flags = b2UnpackHiW( t3a, t4a );

	b2FloatW t1b = b2UnpackLoW( b1b, b3b );
	b2FloatW t2b = b2UnpackLoW( b2b, b4b );
	b2FloatW t3b = b2UnpackHiW( b1b, b3b );
	b2FloatW t4b = b2UnpackHiW( b2b, b4b );

	simdBody.dp.X = b2UnpackLoW( t1b, t2b );
	simdBody.dp.Y = b2UnpackHiW( t1b, t2b );
	simdBody.dq.C = b2UnpackLoW( t3b, t4b );
	simdBody.dq.S = b2UnpackHiW( t3b, t4b );

	return simdBody;
}

// This writes only the velocities back to the solver bodies
static void b2ScatterBodies( b2BodyState* states, int* indices, const b2BodyStateW* simdBody )
{
	B2_ASSERT( sizeof( b2BodyState ) == 32, "b2BodyState not 32 bytes" );
	B2_ASSERT( ( (uintptr_t)states & 0x1F ) == 0 );
	B2_VALIDATE( indices[0] >= 0 && indices[1] >= 0 && indices[2] >= 0 && indices[3] >= 0 );

	// [vx1 vy1 vx2 vy2]
	b2FloatW t1 = b2UnpackLoW( simdBody.v.X, simdBody.v.Y );
	// [vx3 vy3 vx4 vy4]
	b2FloatW t2 = b2UnpackHiW( simdBody.v.X, simdBody.v.Y );
	// [w1 f1 w2 f2]
	b2FloatW t3 = b2UnpackLoW( simdBody.w, simdBody.flags );
	// [w3 f3 w4 f4]
	b2FloatW t4 = b2UnpackHiW( simdBody.w, simdBody.flags );

	// zero means null
	int i1 = indices[0] - 1;
	int i2 = indices[1] - 1;
	int i3 = indices[2] - 1;
	int i4 = indices[3] - 1;

#if ENABLED
	// I don't use any dummy body in the body array because this will lead to multithreaded cache coherence problems.
	if ( i1 != B2_NULL_INDEX && ( states[i1].flags & b2_dynamicFlag ) != 0 )
	{
		// [t1.x t1.y t3.x t3.y]
		b2StoreW( (float*)( states + i1 ), _mm_shuffle_ps( t1, t3, _MM_SHUFFLE( 1, 0, 1, 0 ) ) );
	}

	if ( i2 != B2_NULL_INDEX && ( states[i2].flags & b2_dynamicFlag ) != 0 )
	{
		// [t1.z t1.w t3.z t3.w]
		b2StoreW( (float*)( states + i2 ), _mm_shuffle_ps( t1, t3, _MM_SHUFFLE( 3, 2, 3, 2 ) ) );
	}

	if ( i3 != B2_NULL_INDEX && ( states[i3].flags & b2_dynamicFlag ) != 0 )
	{
		// [t2.x t2.y t4.x t4.y]
		b2StoreW( (float*)( states + i3 ), _mm_shuffle_ps( t2, t4, _MM_SHUFFLE( 1, 0, 1, 0 ) ) );
	}

	if ( i4 != B2_NULL_INDEX && ( states[i4].flags & b2_dynamicFlag ) != 0 )
	{
		// [t2.z t2.w t4.z t4.w]
		b2StoreW( (float*)( states + i4 ), _mm_shuffle_ps( t2, t4, _MM_SHUFFLE( 3, 2, 3, 2 ) ) );
	}

#else

	// todo_testing this is here to test the impact of unsafe writes

	if ( i1 != B2_NULL_INDEX )
	{
		// [t1.x t1.y t3.x t3.y]
		b2StoreW( (float*)( states + i1 ), _mm_shuffle_ps( t1, t3, _MM_SHUFFLE( 1, 0, 1, 0 ) ) );
	}

	if ( i2 != B2_NULL_INDEX )
	{
		// [t1.z t1.w t3.z t3.w]
		b2StoreW( (float*)( states + i2 ), _mm_shuffle_ps( t1, t3, _MM_SHUFFLE( 3, 2, 3, 2 ) ) );
	}

	if ( i3 != B2_NULL_INDEX )
	{
		// [t2.x t2.y t4.x t4.y]
		b2StoreW( (float*)( states + i3 ), _mm_shuffle_ps( t2, t4, _MM_SHUFFLE( 1, 0, 1, 0 ) ) );
	}

	if ( i4 != B2_NULL_INDEX )
	{
		// [t2.z t2.w t4.z t4.w]
		b2StoreW( (float*)( states + i4 ), _mm_shuffle_ps( t2, t4, _MM_SHUFFLE( 3, 2, 3, 2 ) ) );
	}
#endif
}

#else

        // This is a load and transpose
        internal static B2BodyStateW b2GatherBodies(ReadOnlySpan<B2BodyState> states, ReadOnlySpan<int> indices)
        {
            B2_VALIDATE(indices[0] >= 0 && indices[1] >= 0 && indices[2] >= 0 && indices[3] >= 0);

            // Read-only here, so alias the shared instance instead of allocating a copy per call.
            B2BodyState identity = b2_identityBodyState;

            // zero means null
            int i1 = indices[0] - 1;
            int i2 = indices[1] - 1;
            int i3 = indices[2] - 1;
            int i4 = indices[3] - 1;

            B2BodyState s1 = i1 == B2_NULL_INDEX ? identity : states[i1];
            B2BodyState s2 = i2 == B2_NULL_INDEX ? identity : states[i2];
            B2BodyState s3 = i3 == B2_NULL_INDEX ? identity : states[i3];
            B2BodyState s4 = i4 == B2_NULL_INDEX ? identity : states[i4];

            B2BodyStateW simdBody = new B2BodyStateW();
            simdBody.v.X = new B2FloatW(s1.linearVelocity.X, s2.linearVelocity.X, s3.linearVelocity.X, s4.linearVelocity.X);
            simdBody.v.Y = new B2FloatW(s1.linearVelocity.Y, s2.linearVelocity.Y, s3.linearVelocity.Y, s4.linearVelocity.Y);
            simdBody.w = new B2FloatW(s1.angularVelocity, s2.angularVelocity, s3.angularVelocity, s4.angularVelocity);
            simdBody.flags = new B2FloatW((float)s1.flags, (float)s2.flags, (float)s3.flags, (float)s4.flags);
            simdBody.dp.X = new B2FloatW(s1.deltaPosition.X, s2.deltaPosition.X, s3.deltaPosition.X, s4.deltaPosition.X);
            simdBody.dp.Y = new B2FloatW(s1.deltaPosition.Y, s2.deltaPosition.Y, s3.deltaPosition.Y, s4.deltaPosition.Y);
            simdBody.dq.C = new B2FloatW(s1.deltaRotation.c, s2.deltaRotation.c, s3.deltaRotation.c, s4.deltaRotation.c);
            simdBody.dq.S = new B2FloatW(s1.deltaRotation.s, s2.deltaRotation.s, s3.deltaRotation.s, s4.deltaRotation.s);

            return simdBody;
        }

        // This writes only the velocities back to the solver bodies
        internal static void b2ScatterBodies(ReadOnlySpan<B2BodyState> states, ReadOnlySpan<int> indices, ref B2BodyStateW simdBody)
        {
            B2_VALIDATE(indices[0] >= 0 && indices[1] >= 0 && indices[2] >= 0 && indices[3] >= 0);

            // zero means null
            int i1 = indices[0] - 1;
            int i2 = indices[1] - 1;
            int i3 = indices[2] - 1;
            int i4 = indices[3] - 1;

            if (i1 != B2_NULL_INDEX && (states[i1].flags & (uint)B2BodyFlags.b2_dynamicFlag) != 0)
            {
                B2BodyState state = states[i1];
                state.linearVelocity.X = simdBody.v.X.X;
                state.linearVelocity.Y = simdBody.v.Y.X;
                state.angularVelocity = simdBody.w.X;
            }

            if (i2 != B2_NULL_INDEX && (states[i2].flags & (uint)B2BodyFlags.b2_dynamicFlag) != 0)
            {
                B2BodyState state = states[i2];
                state.linearVelocity.X = simdBody.v.X.Y;
                state.linearVelocity.Y = simdBody.v.Y.Y;
                state.angularVelocity = simdBody.w.Y;
            }

            if (i3 != B2_NULL_INDEX && (states[i3].flags & (uint)B2BodyFlags.b2_dynamicFlag) != 0)
            {
                B2BodyState state = states[i3];
                state.linearVelocity.X = simdBody.v.X.Z;
                state.linearVelocity.Y = simdBody.v.Y.Z;
                state.angularVelocity = simdBody.w.Z;
            }

            if (i4 != B2_NULL_INDEX && (states[i4].flags & (uint)B2BodyFlags.b2_dynamicFlag) != 0)
            {
                B2BodyState state = states[i4];
                state.linearVelocity.X = simdBody.v.X.W;
                state.linearVelocity.Y = simdBody.v.Y.W;
                state.angularVelocity = simdBody.w.W;
            }
        }

#endif

        private static readonly B2ContactSim b2_zeroContactSim = new B2ContactSim();

        // Prepare wide contact constraints.
        // Potential optimization: for SSE2 load 4 manifold points and transpose, like the body gather/scatter.
        // I would need to reorder the manifold point so that constraint data comes first.
        internal static void b2PrepareContacts_Wide(B2SolverBlock block, B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.prepare_contact, "Prepare Contact", B2HexColor.b2_colorYellow, true);
            B2World world = context.world;
            B2ContactPrepareSpan[] spans = context.contactPrepareSpans;
            Span<B2ContactConstraintWide> wideBase = context.wideContactConstraints;

#if DEBUG
            B2Body[] bodies = world.bodies.data;
#endif

            B2FloatW oneW = b2SplatW(1.0f);
            B2FloatW warmStartScale = world.enableWarmStarting ? oneW : b2ZeroW();

            int wideIndex = block.startIndex;
            int endWideIndex = block.startIndex + block.count;

            // Find color for start index. Linear search but fast.
            int colorIndex = 0;
            while (spans[colorIndex + 1].start <= wideIndex)
            {
                colorIndex += 1;
            }

            // Loop over block
            while (wideIndex < endWideIndex)
            {
                int colorWideStart = spans[colorIndex].start;
                int colorWideEndIndex = b2MinInt(spans[colorIndex + 1].start, endWideIndex);
                int colorContactCount = spans[colorIndex].count;
                B2ContactSim[] contactSims = spans[colorIndex].contacts;

#if DEBUG
                int expectedWide = colorContactCount > 0 ? ((colorContactCount - 1) >> B2_SIMD_SHIFT) + 1 : 0;
                B2_ASSERT(spans[colorIndex + 1].start - spans[colorIndex].start == expectedWide);
#endif

                // Loop over color
                for (; wideIndex < colorWideEndIndex; ++wideIndex)
                {
                    ref B2ContactConstraintWide cw = ref wideBase[wideIndex];
                    int localWideIndex = wideIndex - colorWideStart;

                    for (int laneIndex = 0; laneIndex < B2_SIMD_WIDTH; ++laneIndex)
                    {
                        int contactIndex = B2_SIMD_WIDTH * localWideIndex + laneIndex;
                        if (contactIndex < colorContactCount)
                        {
                            B2ContactSim c = contactSims[contactIndex];

                            // index base-1
                            cw.indexA[laneIndex] = b2DecodeAwakeIndex(c.encodedBodySimA) + 1;
                            cw.indexB[laneIndex] = b2DecodeAwakeIndex(c.encodedBodySimB) + 1;

#if DEBUG
                            B2Body bodyA = bodies[c.bodyIdA];
                            B2Body bodyB = bodies[c.bodyIdB];
                            B2_ASSERT(c.encodedBodySimA == b2EncodeBodySimIndex(bodyA));
                            B2_ASSERT(c.encodedBodySimB == b2EncodeBodySimIndex(bodyB));
#endif
                        }
                    }

                    int laneBase = B2_SIMD_WIDTH * localWideIndex;
                    B2ContactSim l0 = laneBase + 0 < colorContactCount ? contactSims[laneBase + 0] : b2_zeroContactSim;
                    B2ContactSim l1 = laneBase + 1 < colorContactCount ? contactSims[laneBase + 1] : b2_zeroContactSim;
                    B2ContactSim l2 = laneBase + 2 < colorContactCount ? contactSims[laneBase + 2] : b2_zeroContactSim;
                    B2ContactSim l3 = laneBase + 3 < colorContactCount ? contactSims[laneBase + 3] : b2_zeroContactSim;

                    cw.invMassA = b2SetW(l0.invMassA, l1.invMassA, l2.invMassA, l3.invMassA);
                    cw.invMassB = b2SetW(l0.invMassB, l1.invMassB, l2.invMassB, l3.invMassB);
                    cw.invIA = b2SetW(l0.invIA, l1.invIA, l2.invIA, l3.invIA);
                    cw.invIB = b2SetW(l0.invIB, l1.invIB, l2.invIB, l3.invIB);
                    cw.normal.X = b2SetW(l0.manifold.normal.X, l1.manifold.normal.X, l2.manifold.normal.X, l3.manifold.normal.X);
                    cw.normal.Y = b2SetW(l0.manifold.normal.Y, l1.manifold.normal.Y, l2.manifold.normal.Y, l3.manifold.normal.Y);
                    cw.friction = b2SetW(l0.friction, l1.friction, l2.friction, l3.friction);
                    cw.tangentSpeed = b2SetW(l0.tangentSpeed, l1.tangentSpeed, l2.tangentSpeed, l3.tangentSpeed);
                    cw.rollingResistance = b2SetW(l0.rollingResistance, l1.rollingResistance, l2.rollingResistance, l3.rollingResistance);
                    cw.rollingImpulse = b2SetW(l0.manifold.rollingImpulse, l1.manifold.rollingImpulse, l2.manifold.rollingImpulse, l3.manifold.rollingImpulse);
                    cw.rollingImpulse = b2MulW(warmStartScale, cw.rollingImpulse);

                    B2Vec2W tangent = b2RightPerpW(cw.normal);

                    {
                        // Transpose gather of the first manifold point. Matches the upstream layout:
                        // anchorA, anchorB, separation, normalImpulse, tangentImpulse, restitutionVelocity
                        ref B2ManifoldPoint m1 = ref l0.manifold.points[0];
                        ref B2ManifoldPoint m2 = ref l1.manifold.points[0];
                        ref B2ManifoldPoint m3 = ref l2.manifold.points[0];
                        ref B2ManifoldPoint m4 = ref l3.manifold.points[0];

                        cw.anchorA1.X = b2SetW(m1.anchorA.X, m2.anchorA.X, m3.anchorA.X, m4.anchorA.X);
                        cw.anchorA1.Y = b2SetW(m1.anchorA.Y, m2.anchorA.Y, m3.anchorA.Y, m4.anchorA.Y);
                        cw.anchorB1.X = b2SetW(m1.anchorB.X, m2.anchorB.X, m3.anchorB.X, m4.anchorB.X);
                        cw.anchorB1.Y = b2SetW(m1.anchorB.Y, m2.anchorB.Y, m3.anchorB.Y, m4.anchorB.Y);
                        cw.baseSeparation1 = b2SetW(m1.separation, m2.separation, m3.separation, m4.separation);
                        cw.normalImpulse1 = b2SetW(m1.normalImpulse, m2.normalImpulse, m3.normalImpulse, m4.normalImpulse);
                        cw.tangentImpulse1 = b2SetW(m1.tangentImpulse, m2.tangentImpulse, m3.tangentImpulse, m4.tangentImpulse);
                        cw.negRestitutionVelocity1 = b2SetW(m1.restitutionVelocity, m2.restitutionVelocity, m3.restitutionVelocity, m4.restitutionVelocity);

                        B2FloatW offset = b2DotW(b2SubVW(cw.anchorB1, cw.anchorA1), cw.normal);
                        cw.baseSeparation1 = b2SubW(cw.baseSeparation1, offset);
                        cw.negRestitutionVelocity1 = b2NegW(cw.negRestitutionVelocity1);
                        cw.normalImpulse1 = b2MulW(warmStartScale, cw.normalImpulse1);
                        cw.tangentImpulse1 = b2MulW(warmStartScale, cw.tangentImpulse1);
                        cw.totalNormalImpulse1 = b2ZeroW();

                        // Effective mass along normal.
                        {
                            B2FloatW rnA = b2CrossW(cw.anchorA1, cw.normal);
                            B2FloatW rnB = b2CrossW(cw.anchorB1, cw.normal);
                            B2FloatW k = b2AddW(cw.invMassA, cw.invMassB);
                            k = b2MulAddW(k, cw.invIA, b2MulW(rnA, rnA));
                            k = b2MulAddW(k, cw.invIB, b2MulW(rnB, rnB));
                            B2FloatW test = b2GreaterThanW(k, b2ZeroW());
                            cw.normalMass1 = b2BlendW(b2ZeroW(), b2DivW(oneW, k), test);
                        }

                        // Effective mass along tangent.
                        {
                            B2FloatW rtA = b2CrossW(cw.anchorA1, tangent);
                            B2FloatW rtB = b2CrossW(cw.anchorB1, tangent);
                            B2FloatW k = b2AddW(cw.invMassA, cw.invMassB);
                            k = b2MulAddW(k, cw.invIA, b2MulW(rtA, rtA));
                            k = b2MulAddW(k, cw.invIB, b2MulW(rtB, rtB));
                            B2FloatW test = b2GreaterThanW(k, b2ZeroW());
                            cw.tangentMass1 = b2BlendW(b2ZeroW(), b2DivW(oneW, k), test);
                        }
                    }

                    {
                        ref B2ManifoldPoint m1 = ref l0.manifold.points[1];
                        ref B2ManifoldPoint m2 = ref l1.manifold.points[1];
                        ref B2ManifoldPoint m3 = ref l2.manifold.points[1];
                        ref B2ManifoldPoint m4 = ref l3.manifold.points[1];

                        cw.anchorA2.X = b2SetW(m1.anchorA.X, m2.anchorA.X, m3.anchorA.X, m4.anchorA.X);
                        cw.anchorA2.Y = b2SetW(m1.anchorA.Y, m2.anchorA.Y, m3.anchorA.Y, m4.anchorA.Y);
                        cw.anchorB2.X = b2SetW(m1.anchorB.X, m2.anchorB.X, m3.anchorB.X, m4.anchorB.X);
                        cw.anchorB2.Y = b2SetW(m1.anchorB.Y, m2.anchorB.Y, m3.anchorB.Y, m4.anchorB.Y);
                        cw.baseSeparation2 = b2SetW(m1.separation, m2.separation, m3.separation, m4.separation);
                        cw.normalImpulse2 = b2SetW(m1.normalImpulse, m2.normalImpulse, m3.normalImpulse, m4.normalImpulse);
                        cw.tangentImpulse2 = b2SetW(m1.tangentImpulse, m2.tangentImpulse, m3.tangentImpulse, m4.tangentImpulse);
                        cw.negRestitutionVelocity2 = b2SetW(m1.restitutionVelocity, m2.restitutionVelocity, m3.restitutionVelocity, m4.restitutionVelocity);

                        B2FloatW offset = b2DotW(b2SubVW(cw.anchorB2, cw.anchorA2), cw.normal);
                        cw.baseSeparation2 = b2SubW(cw.baseSeparation2, offset);
                        cw.negRestitutionVelocity2 = b2NegW(cw.negRestitutionVelocity2);
                        cw.normalImpulse2 = b2MulW(warmStartScale, cw.normalImpulse2);
                        cw.tangentImpulse2 = b2MulW(warmStartScale, cw.tangentImpulse2);
                        cw.totalNormalImpulse2 = b2ZeroW();

                        // Effective mass along normal.
                        {
                            B2FloatW rnA = b2CrossW(cw.anchorA2, cw.normal);
                            B2FloatW rnB = b2CrossW(cw.anchorB2, cw.normal);
                            B2FloatW k = b2AddW(cw.invMassA, cw.invMassB);
                            k = b2MulAddW(k, cw.invIA, b2MulW(rnA, rnA));
                            k = b2MulAddW(k, cw.invIB, b2MulW(rnB, rnB));
                            B2FloatW test = b2GreaterThanW(k, b2ZeroW());
                            cw.normalMass2 = b2BlendW(b2ZeroW(), b2DivW(oneW, k), test);
                        }

                        // Effective mass along tangent.
                        {
                            B2FloatW rtA = b2CrossW(cw.anchorA2, tangent);
                            B2FloatW rtB = b2CrossW(cw.anchorB2, tangent);
                            B2FloatW k = b2AddW(cw.invMassA, cw.invMassB);
                            k = b2MulAddW(k, cw.invIA, b2MulW(rtA, rtA));
                            k = b2MulAddW(k, cw.invIB, b2MulW(rtB, rtB));
                            B2FloatW test = b2GreaterThanW(k, b2ZeroW());
                            cw.tangentMass2 = b2BlendW(b2ZeroW(), b2DivW(oneW, k), test);
                        }

                        // Zero effective mass on lanes that only have one point.
                        B2FloatW massScale = b2SetW(
                            l0.manifold.pointCount > 1 ? 1.0f : 0.0f,
                            l1.manifold.pointCount > 1 ? 1.0f : 0.0f,
                            l2.manifold.pointCount > 1 ? 1.0f : 0.0f,
                            l3.manifold.pointCount > 1 ? 1.0f : 0.0f);
                        cw.normalMass2 = b2MulW(massScale, cw.normalMass2);
                        cw.tangentMass2 = b2MulW(massScale, cw.tangentMass2);
                    }
                }

                // Advance to next color
                colorIndex += 1;
            }

            b2TracyCZoneEnd(B2TracyCZone.prepare_contact);
        }

        internal static void b2WarmStartContacts_Wide(B2SolverBlock block, B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.warm_start_contact, "Warm Start", B2HexColor.b2_colorGreen, true);

            B2BodyState[] states = context.states;
            int colorIndex = block.colorIndex;
            Span<B2ContactConstraintWide> constraints = context.graph.colors[colorIndex].wideConstraints;

            for (int i = block.startIndex; i < block.startIndex + block.count; ++i)
            {
                ref B2ContactConstraintWide c = ref constraints[i];
                B2BodyStateW bA = b2GatherBodies(states, c.indexA.AsSpan());
                B2BodyStateW bB = b2GatherBodies(states, c.indexB.AsSpan());

                B2FloatW tangentX = c.normal.Y;
                B2FloatW tangentY = b2SubW(b2ZeroW(), c.normal.X);

                {
                    // fixed anchors
                    B2Vec2W rA = c.anchorA1;
                    B2Vec2W rB = c.anchorB1;

                    B2Vec2W P;
                    P.X = b2AddW(b2MulW(c.normalImpulse1, c.normal.X), b2MulW(c.tangentImpulse1, tangentX));
                    P.Y = b2AddW(b2MulW(c.normalImpulse1, c.normal.Y), b2MulW(c.tangentImpulse1, tangentY));
                    bA.w = b2MulSubW(bA.w, c.invIA, b2CrossW(rA, P));
                    bA.v.X = b2MulSubW(bA.v.X, c.invMassA, P.X);
                    bA.v.Y = b2MulSubW(bA.v.Y, c.invMassA, P.Y);
                    bB.w = b2MulAddW(bB.w, c.invIB, b2CrossW(rB, P));
                    bB.v.X = b2MulAddW(bB.v.X, c.invMassB, P.X);
                    bB.v.Y = b2MulAddW(bB.v.Y, c.invMassB, P.Y);

                    c.totalNormalImpulse1 = b2AddW(c.totalNormalImpulse1, c.normalImpulse1);
                }

                {
                    // fixed anchors
                    B2Vec2W rA = c.anchorA2;
                    B2Vec2W rB = c.anchorB2;

                    B2Vec2W P;
                    P.X = b2AddW(b2MulW(c.normalImpulse2, c.normal.X), b2MulW(c.tangentImpulse2, tangentX));
                    P.Y = b2AddW(b2MulW(c.normalImpulse2, c.normal.Y), b2MulW(c.tangentImpulse2, tangentY));
                    bA.w = b2MulSubW(bA.w, c.invIA, b2CrossW(rA, P));
                    bA.v.X = b2MulSubW(bA.v.X, c.invMassA, P.X);
                    bA.v.Y = b2MulSubW(bA.v.Y, c.invMassA, P.Y);
                    bB.w = b2MulAddW(bB.w, c.invIB, b2CrossW(rB, P));
                    bB.v.X = b2MulAddW(bB.v.X, c.invMassB, P.X);
                    bB.v.Y = b2MulAddW(bB.v.Y, c.invMassB, P.Y);

                    c.totalNormalImpulse2 = b2AddW(c.totalNormalImpulse2, c.normalImpulse2);
                }

                bA.w = b2MulSubW(bA.w, c.invIA, c.rollingImpulse);
                bB.w = b2MulAddW(bB.w, c.invIB, c.rollingImpulse);

                b2ScatterBodies(states, c.indexA.AsSpan(), ref bA);
                b2ScatterBodies(states, c.indexB.AsSpan(), ref bB);
            }

            b2TracyCZoneEnd(B2TracyCZone.warm_start_contact);
        }

        internal static void b2PushContacts_Wide(B2SolverBlock block, B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.solve_contact, "Solve Contact", B2HexColor.b2_colorAliceBlue, true);

            B2BodyState[] states = context.states;
            int colorIndex = block.colorIndex;
            Span<B2ContactConstraintWide> constraints = context.graph.colors[colorIndex].wideConstraints;
            B2FloatW inv_h = b2SplatW(context.inv_h);
            B2FloatW contactSpeed = b2SplatW(-context.world.contactSpeed);
            B2FloatW oneW = b2SplatW(1.0f);

            B2FloatW dynamicBiasRate = b2SplatW(context.contactSoftness.massScale * context.contactSoftness.biasRate);
            B2FloatW dynamicMassScale = b2SplatW(context.contactSoftness.massScale);
            B2FloatW dynamicImpulseScale = b2SplatW(context.contactSoftness.impulseScale);
            B2FloatW staticBiasRate = b2SplatW(context.staticSoftness.massScale * context.staticSoftness.biasRate);
            B2FloatW staticMassScale = b2SplatW(context.staticSoftness.massScale);
            B2FloatW staticImpulseScale = b2SplatW(context.staticSoftness.impulseScale);

            for (int wideIndex = block.startIndex; wideIndex < block.startIndex + block.count; ++wideIndex)
            {
                ref B2ContactConstraintWide c = ref constraints[wideIndex];

                B2BodyStateW bA = b2GatherBodies(states, c.indexA.AsSpan());
                B2BodyStateW bB = b2GatherBodies(states, c.indexB.AsSpan());

                B2FloatW softMask = b2SoftMaskW(c.indexA.AsSpan(), c.indexB.AsSpan());
                B2FloatW biasRate = b2BlendW(dynamicBiasRate, staticBiasRate, softMask);
                B2FloatW massScale = b2BlendW(dynamicMassScale, staticMassScale, softMask);
                B2FloatW impulseScale = b2BlendW(dynamicImpulseScale, staticImpulseScale, softMask);

                B2Vec2W dp = new B2Vec2W(b2SubW(bB.dp.X, bA.dp.X), b2SubW(bB.dp.Y, bA.dp.Y));

                // point1 non-penetration constraint
                {
                    // Fixed anchors for impulses
                    B2Vec2W rA = c.anchorA1;
                    B2Vec2W rB = c.anchorB1;

                    // Moving anchors for current separation
                    B2Vec2W rsA = b2RotateVectorW(bA.dq, rA);
                    B2Vec2W rsB = b2RotateVectorW(bB.dq, rB);

                    // compute current separation
                    // this is subject to round-off error if the anchor is far from the body center of mass
                    B2Vec2W ds = new B2Vec2W(b2AddW(dp.X, b2SubW(rsB.X, rsA.X)), b2AddW(dp.Y, b2SubW(rsB.Y, rsA.Y)));
                    B2FloatW s = b2AddW(b2DotW(c.normal, ds), c.baseSeparation1);

                    // Apply speculative bias if separation is greater than zero, otherwise apply soft constraint bias
                    // The contactSpeed is meant to limit stiffness, not increase it.
                    B2FloatW separated = b2GreaterThanW(s, b2ZeroW());

                    // Speculative bias - positive
                    B2FloatW specBias = b2MulW(s, inv_h);

                    // Overlap bias - negative when overlapped
                    B2FloatW overlapBias = b2MaxW(b2MulW(biasRate, s), contactSpeed);

                    // velocity bias = s > 0 ? spec : overlap
                    B2FloatW velocityBias = b2BlendW(overlapBias, specBias, separated);

                    B2FloatW pointMassScale = b2BlendW(massScale, oneW, separated);
                    B2FloatW pointImpulseScale = b2BlendW(impulseScale, b2ZeroW(), separated);

                    // Relative velocity at contact
                    B2FloatW dvx = b2SubW(b2SubW(bB.v.X, b2MulW(bB.w, rB.Y)), b2SubW(bA.v.X, b2MulW(bA.w, rA.Y)));
                    B2FloatW dvy = b2SubW(b2AddW(bB.v.Y, b2MulW(bB.w, rB.X)), b2AddW(bA.v.Y, b2MulW(bA.w, rA.X)));
                    B2FloatW vn = b2AddW(b2MulW(dvx, c.normal.X), b2MulW(dvy, c.normal.Y));

                    // Compute normal impulse
                    B2FloatW negImpulse = b2AddW(b2MulW(c.normalMass1, b2AddW(b2MulW(pointMassScale, vn), velocityBias)),
                        b2MulW(pointImpulseScale, c.normalImpulse1));

                    // Clamp the accumulated impulse
                    B2FloatW newImpulse = b2MaxW(b2SubW(c.normalImpulse1, negImpulse), b2ZeroW());
                    B2FloatW impulse = b2SubW(newImpulse, c.normalImpulse1);
                    c.normalImpulse1 = newImpulse;
                    c.totalNormalImpulse1 = b2AddW(c.totalNormalImpulse1, impulse);

                    // Apply contact impulse
                    B2FloatW Px = b2MulW(impulse, c.normal.X);
                    B2FloatW Py = b2MulW(impulse, c.normal.Y);

                    bA.v.X = b2MulSubW(bA.v.X, c.invMassA, Px);
                    bA.v.Y = b2MulSubW(bA.v.Y, c.invMassA, Py);
                    bA.w = b2MulSubW(bA.w, c.invIA, b2SubW(b2MulW(rA.X, Py), b2MulW(rA.Y, Px)));

                    bB.v.X = b2MulAddW(bB.v.X, c.invMassB, Px);
                    bB.v.Y = b2MulAddW(bB.v.Y, c.invMassB, Py);
                    bB.w = b2MulAddW(bB.w, c.invIB, b2SubW(b2MulW(rB.X, Py), b2MulW(rB.Y, Px)));
                }

                // second point non-penetration constraint
                {
                    // moving anchors for current separation
                    B2Vec2W rsA = b2RotateVectorW(bA.dq, c.anchorA2);
                    B2Vec2W rsB = b2RotateVectorW(bB.dq, c.anchorB2);

                    // compute current separation
                    B2Vec2W ds = new B2Vec2W(b2AddW(dp.X, b2SubW(rsB.X, rsA.X)), b2AddW(dp.Y, b2SubW(rsB.Y, rsA.Y)));
                    B2FloatW s = b2AddW(b2DotW(c.normal, ds), c.baseSeparation2);

                    B2FloatW separated = b2GreaterThanW(s, b2ZeroW());

                    B2FloatW specBias = b2MulW(s, inv_h);
                    B2FloatW overlapBias = b2MaxW(b2MulW(biasRate, s), contactSpeed);
                    B2FloatW velocityBias = b2BlendW(overlapBias, specBias, separated);

                    B2FloatW pointMassScale = b2BlendW(massScale, oneW, separated);
                    B2FloatW pointImpulseScale = b2BlendW(impulseScale, b2ZeroW(), separated);

                    // fixed anchors for Jacobians
                    B2Vec2W rA = c.anchorA2;
                    B2Vec2W rB = c.anchorB2;

                    // Relative velocity at contact
                    B2FloatW dvx = b2SubW(b2SubW(bB.v.X, b2MulW(bB.w, rB.Y)), b2SubW(bA.v.X, b2MulW(bA.w, rA.Y)));
                    B2FloatW dvy = b2SubW(b2AddW(bB.v.Y, b2MulW(bB.w, rB.X)), b2AddW(bA.v.Y, b2MulW(bA.w, rA.X)));
                    B2FloatW vn = b2AddW(b2MulW(dvx, c.normal.X), b2MulW(dvy, c.normal.Y));

                    // Compute normal impulse
                    B2FloatW negImpulse = b2AddW(b2MulW(c.normalMass2, b2AddW(b2MulW(pointMassScale, vn), velocityBias)),
                        b2MulW(pointImpulseScale, c.normalImpulse2));

                    // Clamp the accumulated impulse
                    B2FloatW newImpulse = b2MaxW(b2SubW(c.normalImpulse2, negImpulse), b2ZeroW());
                    B2FloatW impulse = b2SubW(newImpulse, c.normalImpulse2);
                    c.normalImpulse2 = newImpulse;
                    c.totalNormalImpulse2 = b2AddW(c.totalNormalImpulse2, impulse);

                    // Apply contact impulse
                    B2FloatW Px = b2MulW(impulse, c.normal.X);
                    B2FloatW Py = b2MulW(impulse, c.normal.Y);

                    bA.v.X = b2MulSubW(bA.v.X, c.invMassA, Px);
                    bA.v.Y = b2MulSubW(bA.v.Y, c.invMassA, Py);
                    bA.w = b2MulSubW(bA.w, c.invIA, b2SubW(b2MulW(rA.X, Py), b2MulW(rA.Y, Px)));

                    bB.v.X = b2MulAddW(bB.v.X, c.invMassB, Px);
                    bB.v.Y = b2MulAddW(bB.v.Y, c.invMassB, Py);
                    bB.w = b2MulAddW(bB.w, c.invIB, b2SubW(b2MulW(rB.X, Py), b2MulW(rB.Y, Px)));
                }

                b2ScatterBodies(states, c.indexA.AsSpan(), ref bA);
                b2ScatterBodies(states, c.indexB.AsSpan(), ref bB);
            }

            b2TracyCZoneEnd(B2TracyCZone.solve_contact);
        }

        internal static void b2SolveContacts_Wide(B2SolverBlock block, B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.solve_contact, "Solve Contact", B2HexColor.b2_colorAliceBlue, true);

            B2BodyState[] states = context.states;
            int colorIndex = block.colorIndex;
            Span<B2ContactConstraintWide> constraints = context.graph.colors[colorIndex].wideConstraints;
            B2FloatW inv_h = b2SplatW(context.inv_h);

            for (int wideIndex = block.startIndex; wideIndex < block.startIndex + block.count; ++wideIndex)
            {
                ref B2ContactConstraintWide c = ref constraints[wideIndex];

                B2BodyStateW bA = b2GatherBodies(states, c.indexA.AsSpan());
                B2BodyStateW bB = b2GatherBodies(states, c.indexB.AsSpan());

                B2FloatW restitutionMask1 = b2GreaterThanW(b2ZeroW(), c.negRestitutionVelocity1);
                B2FloatW restitutionMask2 = b2GreaterThanW(b2ZeroW(), c.negRestitutionVelocity2);
                bool haveRestitution = b2AllZeroW(b2OrW(restitutionMask1, restitutionMask2)) == false;

                B2FloatW keepRestitution = b2ZeroW();
                B2FloatW totalNormalImpulse = b2ZeroW();

                B2Vec2W dp = new B2Vec2W(b2SubW(bB.dp.X, bA.dp.X), b2SubW(bB.dp.Y, bA.dp.Y));

                // point1 non-penetration constraint
                {
                    // Fixed anchors for impulses
                    B2Vec2W rA = c.anchorA1;
                    B2Vec2W rB = c.anchorB1;

                    // Moving anchors for current separation
                    B2Vec2W rsA = b2RotateVectorW(bA.dq, rA);
                    B2Vec2W rsB = b2RotateVectorW(bB.dq, rB);

                    // compute current separation
                    // this is subject to round-off error if the anchor is far from the body center of mass
                    B2Vec2W ds = new B2Vec2W(b2AddW(dp.X, b2SubW(rsB.X, rsA.X)), b2AddW(dp.Y, b2SubW(rsB.Y, rsA.Y)));
                    B2FloatW s = b2AddW(b2DotW(c.normal, ds), c.baseSeparation1);

                    // Speculative bias - positive if separated
                    B2FloatW specBias = b2MulW(s, inv_h);

                    // This will be zero if separation <= 0
                    B2FloatW velocityBias = b2MaxW(b2ZeroW(), specBias);

                    if (haveRestitution)
                    {
                        B2FloatW separated = b2GreaterThanW(s, b2ZeroW());
                        velocityBias = b2BlendW(velocityBias, c.negRestitutionVelocity1, restitutionMask1);
                        keepRestitution = b2OrW(keepRestitution, b2AndNotW(restitutionMask1, separated));
                    }

                    // Relative velocity at contact
                    B2FloatW dvx = b2SubW(b2SubW(bB.v.X, b2MulW(bB.w, rB.Y)), b2SubW(bA.v.X, b2MulW(bA.w, rA.Y)));
                    B2FloatW dvy = b2SubW(b2AddW(bB.v.Y, b2MulW(bB.w, rB.X)), b2AddW(bA.v.Y, b2MulW(bA.w, rA.X)));
                    B2FloatW vn = b2AddW(b2MulW(dvx, c.normal.X), b2MulW(dvy, c.normal.Y));

                    // Compute normal impulse
                    B2FloatW negImpulse = b2MulW(c.normalMass1, b2AddW(vn, velocityBias));

                    // Clamp the accumulated impulse
                    B2FloatW newImpulse = b2MaxW(b2SubW(c.normalImpulse1, negImpulse), b2ZeroW());
                    B2FloatW impulse = b2SubW(newImpulse, c.normalImpulse1);
                    c.normalImpulse1 = newImpulse;
                    c.totalNormalImpulse1 = b2AddW(c.totalNormalImpulse1, impulse);

                    totalNormalImpulse = b2AddW(totalNormalImpulse, newImpulse);

                    // Apply contact impulse
                    B2FloatW Px = b2MulW(impulse, c.normal.X);
                    B2FloatW Py = b2MulW(impulse, c.normal.Y);

                    bA.v.X = b2MulSubW(bA.v.X, c.invMassA, Px);
                    bA.v.Y = b2MulSubW(bA.v.Y, c.invMassA, Py);
                    bA.w = b2MulSubW(bA.w, c.invIA, b2SubW(b2MulW(rA.X, Py), b2MulW(rA.Y, Px)));

                    bB.v.X = b2MulAddW(bB.v.X, c.invMassB, Px);
                    bB.v.Y = b2MulAddW(bB.v.Y, c.invMassB, Py);
                    bB.w = b2MulAddW(bB.w, c.invIB, b2SubW(b2MulW(rB.X, Py), b2MulW(rB.Y, Px)));
                }

                // second point non-penetration constraint
                {
                    // moving anchors for current separation
                    B2Vec2W rsA = b2RotateVectorW(bA.dq, c.anchorA2);
                    B2Vec2W rsB = b2RotateVectorW(bB.dq, c.anchorB2);

                    // compute current separation
                    B2Vec2W ds = new B2Vec2W(b2AddW(dp.X, b2SubW(rsB.X, rsA.X)), b2AddW(dp.Y, b2SubW(rsB.Y, rsA.Y)));
                    B2FloatW s = b2AddW(b2DotW(c.normal, ds), c.baseSeparation2);

                    // Speculative bias - positive if separated
                    B2FloatW specBias = b2MulW(s, inv_h);

                    // This will be zero if separation <= 0
                    B2FloatW velocityBias = b2MaxW(b2ZeroW(), specBias);

                    if (haveRestitution)
                    {
                        B2FloatW separated = b2GreaterThanW(s, b2ZeroW());
                        velocityBias = b2BlendW(velocityBias, c.negRestitutionVelocity2, restitutionMask2);
                        keepRestitution = b2OrW(keepRestitution, b2AndNotW(restitutionMask2, separated));
                    }

                    // fixed anchors for Jacobians
                    B2Vec2W rA = c.anchorA2;
                    B2Vec2W rB = c.anchorB2;

                    // Relative velocity at contact
                    B2FloatW dvx = b2SubW(b2SubW(bB.v.X, b2MulW(bB.w, rB.Y)), b2SubW(bA.v.X, b2MulW(bA.w, rA.Y)));
                    B2FloatW dvy = b2SubW(b2AddW(bB.v.Y, b2MulW(bB.w, rB.X)), b2AddW(bA.v.Y, b2MulW(bA.w, rA.X)));
                    B2FloatW vn = b2AddW(b2MulW(dvx, c.normal.X), b2MulW(dvy, c.normal.Y));

                    // Compute normal impulse
                    B2FloatW negImpulse = b2MulW(c.normalMass2, b2AddW(vn, velocityBias));

                    // Clamp the accumulated impulse
                    B2FloatW newImpulse = b2MaxW(b2SubW(c.normalImpulse2, negImpulse), b2ZeroW());
                    B2FloatW impulse = b2SubW(newImpulse, c.normalImpulse2);
                    c.normalImpulse2 = newImpulse;
                    c.totalNormalImpulse2 = b2AddW(c.totalNormalImpulse2, impulse);

                    totalNormalImpulse = b2AddW(totalNormalImpulse, newImpulse);

                    // Apply contact impulse
                    B2FloatW Px = b2MulW(impulse, c.normal.X);
                    B2FloatW Py = b2MulW(impulse, c.normal.Y);

                    bA.v.X = b2MulSubW(bA.v.X, c.invMassA, Px);
                    bA.v.Y = b2MulSubW(bA.v.Y, c.invMassA, Py);
                    bA.w = b2MulSubW(bA.w, c.invIA, b2SubW(b2MulW(rA.X, Py), b2MulW(rA.Y, Px)));

                    bB.v.X = b2MulAddW(bB.v.X, c.invMassB, Px);
                    bB.v.Y = b2MulAddW(bB.v.Y, c.invMassB, Py);
                    bB.w = b2MulAddW(bB.w, c.invIB, b2SubW(b2MulW(rB.X, Py), b2MulW(rB.Y, Px)));
                }

                // Turn off restitution when all points become separated on a lane.
                c.negRestitutionVelocity1 = b2BlendW(b2ZeroW(), c.negRestitutionVelocity1, keepRestitution);
                c.negRestitutionVelocity2 = b2BlendW(b2ZeroW(), c.negRestitutionVelocity2, keepRestitution);

                // Rolling resistance
                if (b2AllZeroW(c.rollingResistance) == false)
                {
                    B2FloatW k = b2AddW(c.invIA, c.invIB);
                    B2FloatW mask = b2GreaterThanW(k, b2ZeroW());
                    B2FloatW deltaLambda = b2BlendW(b2ZeroW(), b2DivW(b2SubW(bA.w, bB.w), k), mask);
                    B2FloatW lambda = c.rollingImpulse;
                    B2FloatW maxLambda = b2MulW(c.rollingResistance, totalNormalImpulse);
                    c.rollingImpulse = b2SymClampW(b2AddW(lambda, deltaLambda), maxLambda);
                    deltaLambda = b2SubW(c.rollingImpulse, lambda);

                    bA.w = b2MulSubW(bA.w, c.invIA, deltaLambda);
                    bB.w = b2MulAddW(bB.w, c.invIB, deltaLambda);
                }

                B2FloatW tangentX = c.normal.Y;
                B2FloatW tangentY = b2SubW(b2ZeroW(), c.normal.X);

                // point 1 friction constraint
                {
                    // Fixed anchor points for applying impulses
                    B2Vec2W rA = c.anchorA1;
                    B2Vec2W rB = c.anchorB1;

                    // Relative velocity at contact
                    B2FloatW dvx = b2SubW(b2SubW(bB.v.X, b2MulW(bB.w, rB.Y)), b2SubW(bA.v.X, b2MulW(bA.w, rA.Y)));
                    B2FloatW dvy = b2SubW(b2AddW(bB.v.Y, b2MulW(bB.w, rB.X)), b2AddW(bA.v.Y, b2MulW(bA.w, rA.X)));
                    B2FloatW vt = b2AddW(b2MulW(dvx, tangentX), b2MulW(dvy, tangentY));

                    // Tangent speed (conveyor belt)
                    vt = b2SubW(vt, c.tangentSpeed);

                    // Compute tangent force
                    B2FloatW negImpulse = b2MulW(c.tangentMass1, vt);

                    // Clamp the accumulated force
                    B2FloatW maxFriction = b2MulW(c.friction, c.normalImpulse1);
                    B2FloatW newImpulse = b2SubW(c.tangentImpulse1, negImpulse);
                    newImpulse = b2MaxW(b2SubW(b2ZeroW(), maxFriction), b2MinW(newImpulse, maxFriction));
                    B2FloatW impulse = b2SubW(newImpulse, c.tangentImpulse1);
                    c.tangentImpulse1 = newImpulse;

                    // Apply contact impulse
                    B2FloatW Px = b2MulW(impulse, tangentX);
                    B2FloatW Py = b2MulW(impulse, tangentY);

                    bA.v.X = b2MulSubW(bA.v.X, c.invMassA, Px);
                    bA.v.Y = b2MulSubW(bA.v.Y, c.invMassA, Py);
                    bA.w = b2MulSubW(bA.w, c.invIA, b2SubW(b2MulW(rA.X, Py), b2MulW(rA.Y, Px)));

                    bB.v.X = b2MulAddW(bB.v.X, c.invMassB, Px);
                    bB.v.Y = b2MulAddW(bB.v.Y, c.invMassB, Py);
                    bB.w = b2MulAddW(bB.w, c.invIB, b2SubW(b2MulW(rB.X, Py), b2MulW(rB.Y, Px)));
                }

                // second point friction constraint
                {
                    // fixed anchors for Jacobians
                    B2Vec2W rA = c.anchorA2;
                    B2Vec2W rB = c.anchorB2;

                    // Relative velocity at contact
                    B2FloatW dvx = b2SubW(b2SubW(bB.v.X, b2MulW(bB.w, rB.Y)), b2SubW(bA.v.X, b2MulW(bA.w, rA.Y)));
                    B2FloatW dvy = b2SubW(b2AddW(bB.v.Y, b2MulW(bB.w, rB.X)), b2AddW(bA.v.Y, b2MulW(bA.w, rA.X)));
                    B2FloatW vt = b2AddW(b2MulW(dvx, tangentX), b2MulW(dvy, tangentY));

                    // Tangent speed (conveyor belt)
                    vt = b2SubW(vt, c.tangentSpeed);

                    // Compute tangent force
                    B2FloatW negImpulse = b2MulW(c.tangentMass2, vt);

                    // Clamp the accumulated force
                    B2FloatW maxFriction = b2MulW(c.friction, c.normalImpulse2);
                    B2FloatW newImpulse = b2SubW(c.tangentImpulse2, negImpulse);
                    newImpulse = b2MaxW(b2SubW(b2ZeroW(), maxFriction), b2MinW(newImpulse, maxFriction));
                    B2FloatW impulse = b2SubW(newImpulse, c.tangentImpulse2);
                    c.tangentImpulse2 = newImpulse;

                    // Apply contact impulse
                    B2FloatW Px = b2MulW(impulse, tangentX);
                    B2FloatW Py = b2MulW(impulse, tangentY);

                    bA.v.X = b2MulSubW(bA.v.X, c.invMassA, Px);
                    bA.v.Y = b2MulSubW(bA.v.Y, c.invMassA, Py);
                    bA.w = b2MulSubW(bA.w, c.invIA, b2SubW(b2MulW(rA.X, Py), b2MulW(rA.Y, Px)));

                    bB.v.X = b2MulAddW(bB.v.X, c.invMassB, Px);
                    bB.v.Y = b2MulAddW(bB.v.Y, c.invMassB, Py);
                    bB.w = b2MulAddW(bB.w, c.invIB, b2SubW(b2MulW(rB.X, Py), b2MulW(rB.Y, Px)));
                }

                b2ScatterBodies(states, c.indexA.AsSpan(), ref bA);
                b2ScatterBodies(states, c.indexB.AsSpan(), ref bB);
            }

            b2TracyCZoneEnd(B2TracyCZone.solve_contact);
        }

        // I tried adding this to the last relax iterations but it was slower.
        //
        // Runs as a flat parallel-for over the whole wide constraint range. Per-color
        // contact sims are looked up through the prepareSpans cursor rather than the
        // block's colorIndex, matching the layout of b2PrepareContacts_Wide.
        //
        // I could store the manifold pointer in the b2ContactConstraintWide to simplify
        // this. But it is important to keep the constraint small.
        internal static void b2StoreImpulses_Wide(B2SolverBlock block, B2StepContext context, int workerIndex)
        {
            b2TracyCZoneNC(B2TracyCZone.store_impulses, "Store", B2HexColor.b2_colorFireBrick, true);

            B2World world = context.world;
            ReadOnlySpan<B2ContactPrepareSpan> spans = context.contactPrepareSpans;
            ReadOnlySpan<B2ContactConstraintWide> wideBase = context.wideContactConstraints;
            B2TaskContext taskContext = world.taskContexts.data[workerIndex];
            ref B2BitSet hitEventBitSet = ref taskContext.hitEventBitSet;
            bool hasHitEvents = taskContext.hasHitEvents;
            float negHitThreshold = -world.hitEventThreshold;

            int wideIndex = block.startIndex;
            int endWideIndex = block.startIndex + block.count;

            // Find color for start index
            int colorIndex = 0;
            while (spans[colorIndex + 1].start <= wideIndex)
            {
                colorIndex += 1;
            }

            while (wideIndex < endWideIndex)
            {
                int colorWideEndIndex = b2MinInt(spans[colorIndex + 1].start, endWideIndex);
                int colorWideStart = spans[colorIndex].start;
                int colorContactCount = spans[colorIndex].count;
                Span<B2ContactSim> contactSims = spans[colorIndex].contacts;

                for (; wideIndex < colorWideEndIndex; ++wideIndex)
                {
                    ref readonly B2ContactConstraintWide c = ref wideBase[wideIndex];
                    ref readonly B2FloatW rollingImpulse = ref c.rollingImpulse;
                    ref readonly B2FloatW normalImpulse1 = ref c.normalImpulse1;
                    ref readonly B2FloatW normalImpulse2 = ref c.normalImpulse2;
                    ref readonly B2FloatW tangentImpulse1 = ref c.tangentImpulse1;
                    ref readonly B2FloatW tangentImpulse2 = ref c.tangentImpulse2;
                    ref readonly B2FloatW totalNormalImpulse1 = ref c.totalNormalImpulse1;
                    ref readonly B2FloatW totalNormalImpulse2 = ref c.totalNormalImpulse2;

                    int localWideIndex = wideIndex - colorWideStart;
                    int baseIndex = B2_SIMD_WIDTH * localWideIndex;

                    for (int laneIndex = 0; laneIndex < B2_SIMD_WIDTH; ++laneIndex)
                    {
                        int contactIndex = baseIndex + laneIndex;
                        if (contactIndex >= colorContactCount)
                        {
                            break;
                        }

                        B2ContactSim contactSim = contactSims[contactIndex];
                        ref B2Manifold m = ref contactSim.manifold;
                        m.rollingImpulse = rollingImpulse[laneIndex];

                        m.points[0].normalImpulse = normalImpulse1[laneIndex];
                        m.points[0].tangentImpulse = tangentImpulse1[laneIndex];
                        m.points[0].totalNormalImpulse = totalNormalImpulse1[laneIndex];

                        m.points[1].normalImpulse = normalImpulse2[laneIndex];
                        m.points[1].tangentImpulse = tangentImpulse2[laneIndex];
                        m.points[1].totalNormalImpulse = totalNormalImpulse2[laneIndex];

                        // Check for hit events to speed up serial processing later in the step
                        if ((contactSim.simFlags & (uint)B2ContactFlags.b2_simEnableHitEvent) != 0)
                        {
                            for (int k = 0; k < contactSim.manifold.pointCount; ++k)
                            {
                                ref B2ManifoldPoint mp = ref m.points[k];

                                // Need to check total impulse because the point may be speculative and not colliding
                                if (mp.normalVelocity < negHitThreshold && mp.totalNormalImpulse > 0.0f)
                                {
                                    b2SetBit(ref hitEventBitSet, contactSim.contactId);
                                    hasHitEvents = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                // Advance to next color
                colorIndex += 1;
            }

            taskContext.hasHitEvents = hasHitEvents;

            b2TracyCZoneEnd(B2TracyCZone.store_impulses);
        }
    }
}
