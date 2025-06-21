// SPDX-FileCopyrightText: 2023 Erin Catto
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

namespace Box2D.NET
{
    public static class B2ContactSolvers
    {
        // Overflow contacts don't fit into the constraint graph coloring
        // contact separation for sub-stepping
        // s = s0 + dot(cB + rB - cA - rA, normal)
        // normal is held constant
        // body positions c can translation and anchors r can rotate
        // s(t) = s0 + dot(cB(t) + rB(t) - cA(t) - rA(t), normal)
        // s(t) = s0 + dot(cB0 + dpB + rot(dqB, rB0) - cA0 - dpA - rot(dqA, rA0), normal)
        // s(t) = s0 + dot(cB0 - cA0, normal) + dot(dpB - dpA + rot(dqB, rB0) - rot(dqA, rA0), normal)
        // s_base = s0 + dot(cB0 - cA0, normal)
        public static void b2PrepareOverflowContacts(B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.prepare_overflow_contact, "Prepare Overflow Contact", B2HexColor.b2_colorYellow, true);

            B2World world = context.world;
            ref B2ConstraintGraph graph = ref context.graph;
            ref B2GraphColor color = ref graph.colors[B2_OVERFLOW_INDEX];
            Span<B2ContactConstraint> constraints = color.overflowConstraints;

            int contactCount = color.contactSims.count;
            B2ContactSim[] contacts = color.contactSims.data;
            B2BodyState[] awakeStates = context.states;

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

                int indexA = contactSim.bodySimIndexA;
                int indexB = contactSim.bodySimIndexB;

#if DEBUG
                B2Body bodyA = bodies[contactSim.bodyIdA];
                int validIndexA = bodyA.setIndex == (int)B2SetType.b2_awakeSet ? bodyA.localIndex : B2_NULL_INDEX;
                B2_ASSERT(indexA == validIndexA);

                B2Body bodyB = bodies[contactSim.bodyIdB];
                int validIndexB = bodyB.setIndex == (int)B2SetType.b2_awakeSet ? bodyB.localIndex : B2_NULL_INDEX;
                B2_ASSERT(indexB == validIndexB);
#endif

                ref B2ContactConstraint constraint = ref constraints[i];
                constraint.indexA = indexA;
                constraint.indexB = indexB;
                constraint.normal = manifold.normal;
                constraint.friction = contactSim.friction;
                constraint.restitution = contactSim.restitution;
                constraint.rollingResistance = contactSim.rollingResistance;
                constraint.rollingImpulse = warmStartScale * manifold.rollingImpulse;
                constraint.tangentSpeed = contactSim.tangentSpeed;
                constraint.pointCount = pointCount;

                B2Vec2 vA = b2Vec2_zero;
                float wA = 0.0f;
                float mA = contactSim.invMassA;
                float iA = contactSim.invIA;
                if (indexA != B2_NULL_INDEX)
                {
                    B2BodyState stateA = awakeStates[indexA];
                    vA = stateA.linearVelocity;
                    wA = stateA.angularVelocity;
                }

                B2Vec2 vB = b2Vec2_zero;
                float wB = 0.0f;
                float mB = contactSim.invMassB;
                float iB = contactSim.invIB;
                if (indexB != B2_NULL_INDEX)
                {
                    B2BodyState stateB = awakeStates[indexB];
                    vB = stateB.linearVelocity;
                    wB = stateB.angularVelocity;
                }

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

                    // Save relative velocity for restitution
                    B2Vec2 vrA = b2Add(vA, b2CrossSV(wA, rA));
                    B2Vec2 vrB = b2Add(vB, b2CrossSV(wB, rB));
                    cp.relativeVelocity = b2Dot(normal, b2Sub(vrB, vrA));
                }
            }

            b2TracyCZoneEnd(B2TracyCZone.prepare_overflow_contact);
        }

        public static void b2WarmStartOverflowContacts(B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.warmstart_overflow_contact, "WarmStart Overflow Contact", B2HexColor.b2_colorDarkOrange, true);

            ref B2ConstraintGraph graph = ref context.graph;
            ref B2GraphColor color = ref graph.colors[B2_OVERFLOW_INDEX];
            Span<B2ContactConstraint> constraints = color.overflowConstraints;
            int contactCount = color.contactSims.count;
            B2World world = context.world;
            B2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_awakeSet);
            B2BodyState[] states = awakeSet.bodyStates.data;

            // This is a dummy state to represent a static body because static bodies don't have a solver body.
            B2BodyState dummyState = B2BodyState.Create(b2_identityBodyState);

            for (int i = 0; i < contactCount; ++i)
            {
                ref B2ContactConstraint constraint = ref constraints[i];

                int indexA = constraint.indexA;
                int indexB = constraint.indexB;

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
                    wA -= iA * b2Cross(rA, P);
                    vA = b2MulAdd(vA, -mA, P);
                    wB += iB * b2Cross(rB, P);
                    vB = b2MulAdd(vB, mB, P);
                }

                wA -= iA * constraint.rollingImpulse;
                wB += iB * constraint.rollingImpulse;

                stateA.linearVelocity = vA;
                stateA.angularVelocity = wA;
                stateB.linearVelocity = vB;
                stateB.angularVelocity = wB;
            }

            b2TracyCZoneEnd(B2TracyCZone.warmstart_overflow_contact);
        }

        public static void b2SolveOverflowContacts(B2StepContext context, bool useBias)
        {
            b2TracyCZoneNC(B2TracyCZone.solve_contact, "Solve Contact", B2HexColor.b2_colorAliceBlue, true);

            ref B2ConstraintGraph graph = ref context.graph;
            ref B2GraphColor color = ref graph.colors[B2_OVERFLOW_INDEX];
            Span<B2ContactConstraint> constraints = color.overflowConstraints;
            int contactCount = color.contactSims.count;
            B2World world = context.world;
            B2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_awakeSet);
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

                B2BodyState stateA = constraint.indexA == B2_NULL_INDEX ? dummyState : states[constraint.indexA];
                B2Vec2 vA = stateA.linearVelocity;
                float wA = stateA.angularVelocity;
                B2Rot dqA = stateA.deltaRotation;

                B2BodyState stateB = constraint.indexB == B2_NULL_INDEX ? dummyState : states[constraint.indexB];
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

                // Non-penetration
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
                        // speculative bias
                        velocityBias = s * inv_h;
                    }
                    else if (useBias)
                    {
                        velocityBias = b2MaxFloat(softness.massScale * softness.biasRate * s, -contactSpeed);
                        massScale = softness.massScale;
                        impulseScale = softness.impulseScale;
                    }

                    // relative normal velocity at contact
                    B2Vec2 vrA = b2Add(vA, b2CrossSV(wA, rA));
                    B2Vec2 vrB = b2Add(vB, b2CrossSV(wB, rB));
                    float vn = b2Dot(b2Sub(vrB, vrA), normal);

                    // incremental normal impulse
                    float impulse = -cp.normalMass * (massScale * vn + velocityBias) - impulseScale * cp.normalImpulse;

                    // clamp the accumulated impulse
                    float newImpulse = b2MaxFloat(cp.normalImpulse + impulse, 0.0f);
                    impulse = newImpulse - cp.normalImpulse;
                    cp.normalImpulse = newImpulse;
                    cp.totalNormalImpulse += newImpulse;
                    totalNormalImpulse += newImpulse;

                    // apply normal impulse
                    B2Vec2 P = b2MulSV(impulse, normal);
                    vA = b2MulSub(vA, mA, P);
                    wA -= iA * b2Cross(rA, P);

                    vB = b2MulAdd(vB, mB, P);
                    wB += iB * b2Cross(rB, P);
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

                stateA.linearVelocity = vA;
                stateA.angularVelocity = wA;
                stateB.linearVelocity = vB;
                stateB.angularVelocity = wB;
            }

            b2TracyCZoneEnd(B2TracyCZone.solve_contact);
        }

        public static void b2ApplyOverflowRestitution(B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.overflow_resitution, "Overflow Restitution", B2HexColor.b2_colorViolet, true);

            ref B2ConstraintGraph graph = ref context.graph;
            ref B2GraphColor color = ref graph.colors[B2_OVERFLOW_INDEX];
            Span<B2ContactConstraint> constraints = color.overflowConstraints;
            int contactCount = color.contactSims.count;
            B2World world = context.world;
            B2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_awakeSet);
            B2BodyState[] states = awakeSet.bodyStates.data;

            float threshold = context.world.restitutionThreshold;

            // dummy state to represent a static body
            B2BodyState dummyState = B2BodyState.Create(b2_identityBodyState);

            for (int i = 0; i < contactCount; ++i)
            {
                ref B2ContactConstraint constraint = ref constraints[i];

                float restitution = constraint.restitution;
                if (restitution == 0.0f)
                {
                    continue;
                }

                float mA = constraint.invMassA;
                float iA = constraint.invIA;
                float mB = constraint.invMassB;
                float iB = constraint.invIB;

                B2BodyState stateA = constraint.indexA == B2_NULL_INDEX ? dummyState : states[constraint.indexA];
                B2Vec2 vA = stateA.linearVelocity;
                float wA = stateA.angularVelocity;

                B2BodyState stateB = constraint.indexB == B2_NULL_INDEX ? dummyState : states[constraint.indexB];
                B2Vec2 vB = stateB.linearVelocity;
                float wB = stateB.angularVelocity;

                B2Vec2 normal = constraint.normal;
                int pointCount = constraint.pointCount;

                // it is possible to get more accurate restitution by iterating
                // this only makes a difference if there are two contact points
                // for (int iter = 0; iter < 10; ++iter)
                {
                    for (int j = 0; j < pointCount; ++j)
                    {
                        ref B2ContactConstraintPoint cp = ref constraint.points[j];

                        // if the normal impulse is zero then there was no collision
                        // this skips speculative contact points that didn't generate an impulse
                        // The max normal impulse is used in case there was a collision that moved away within the sub-step process
                        if (cp.relativeVelocity > -threshold || cp.totalNormalImpulse == 0.0f)
                        {
                            continue;
                        }

                        // fixed anchor points
                        B2Vec2 rA = cp.anchorA;
                        B2Vec2 rB = cp.anchorB;

                        // relative normal velocity at contact
                        B2Vec2 vrB = b2Add(vB, b2CrossSV(wB, rB));
                        B2Vec2 vrA = b2Add(vA, b2CrossSV(wA, rA));
                        float vn = b2Dot(b2Sub(vrB, vrA), normal);

                        // compute normal impulse
                        float impulse = -cp.normalMass * (vn + restitution * cp.relativeVelocity);

                        // clamp the accumulated impulse
                        // todo should this be stored?
                        float newImpulse = b2MaxFloat(cp.normalImpulse + impulse, 0.0f);
                        impulse = newImpulse - cp.normalImpulse;
                        cp.normalImpulse = newImpulse;

                        // Add the incremental impulse rather than the full impulse because this is not a sub-step
                        cp.totalNormalImpulse += impulse;

                        // apply contact impulse
                        B2Vec2 P = b2MulSV(impulse, normal);
                        vA = b2MulSub(vA, mA, P);
                        wA -= iA * b2Cross(rA, P);
                        vB = b2MulAdd(vB, mB, P);
                        wB += iB * b2Cross(rB, P);
                    }
                }

                stateA.linearVelocity = vA;
                stateA.angularVelocity = wA;
                stateB.linearVelocity = vB;
                stateB.angularVelocity = wB;
            }

            b2TracyCZoneEnd(B2TracyCZone.overflow_resitution);
        }

        public static void b2StoreOverflowImpulses(B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.store_impulses, "Store", B2HexColor.b2_colorFireBrick, true);

            ref B2ConstraintGraph graph = ref context.graph;
            ref B2GraphColor color = ref graph.colors[B2_OVERFLOW_INDEX];
            Span<B2ContactConstraint> constraints = color.overflowConstraints;
            B2ContactSim[] contacts = color.contactSims.data;
            int contactCount = color.contactSims.count;

            for (int i = 0; i < contactCount; ++i)
            {
                ref B2ContactConstraint constraint = ref constraints[i];
                B2ContactSim contact = contacts[i];
                ref B2Manifold manifold = ref contact.manifold;
                int pointCount = manifold.pointCount;

                for (int j = 0; j < pointCount; ++j)
                {
                    manifold.points[j].normalImpulse = constraint.points[j].normalImpulse;
                    manifold.points[j].tangentImpulse = constraint.points[j].tangentImpulse;
                    manifold.points[j].totalNormalImpulse = constraint.points[j].totalNormalImpulse;
                    manifold.points[j].normalVelocity = constraint.points[j].relativeVelocity;
                }

                manifold.rollingImpulse = constraint.rollingImpulse;
            }

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

            // a 또는 b의 해당 요소가 0이 아니면 참인 마스크 생성
            Vector<int> zeroMask = aZeroMask | bZeroMask;

            // 마스크가 참이면 1.0f, 거짓이면 0.0f 선택
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
        public static B2FloatW b2AddW(B2FloatW a, B2FloatW b)
        {
            return new B2FloatW(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2SubW(B2FloatW a, B2FloatW b)
        {
            return new B2FloatW(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2MulW(B2FloatW a, B2FloatW b)
        {
            return new B2FloatW(a.X * b.X, a.Y * b.Y, a.Z * b.Z, a.W * b.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2MulAddW(B2FloatW a, B2FloatW b, B2FloatW c)
        {
            return new B2FloatW(a.X + b.X * c.X, a.Y + b.Y * c.Y, a.Z + b.Z * c.Z, a.W + b.W * c.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2MulSubW(B2FloatW a, B2FloatW b, B2FloatW c)
        {
            return new B2FloatW(a.X - b.X * c.X, a.Y - b.Y * c.Y, a.Z - b.Z * c.Z, a.W - b.W * c.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2MinW(B2FloatW a, B2FloatW b)
        {
            return new B2FloatW(
                a.X <= b.X ? a.X : b.X,
                a.Y <= b.Y ? a.Y : b.Y,
                a.Z <= b.Z ? a.Z : b.Z,
                a.W <= b.W ? a.W : b.W
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2MaxW(B2FloatW a, B2FloatW b)
        {
            return new B2FloatW(
                a.X >= b.X ? a.X : b.X,
                a.Y >= b.Y ? a.Y : b.Y,
                a.Z >= b.Z ? a.Z : b.Z,
                a.W >= b.W ? a.W : b.W
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2SymClampW(B2FloatW a, B2FloatW b)
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
        public static B2FloatW b2OrW(B2FloatW a, B2FloatW b)
        {
            return new B2FloatW(
                a.X != 0.0f || b.X != 0.0f ? 1.0f : 0.0f,
                a.Y != 0.0f || b.Y != 0.0f ? 1.0f : 0.0f,
                a.Z != 0.0f || b.Z != 0.0f ? 1.0f : 0.0f,
                a.W != 0.0f || b.W != 0.0f ? 1.0f : 0.0f
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2GreaterThanW(B2FloatW a, B2FloatW b)
        {
            return new B2FloatW(
                a.X > b.X ? 1.0f : 0.0f,
                a.Y > b.Y ? 1.0f : 0.0f,
                a.Z > b.Z ? 1.0f : 0.0f,
                a.W > b.W ? 1.0f : 0.0f
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2EqualsW(B2FloatW a, B2FloatW b)
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
        public static bool b2AllZeroW(B2FloatW a)
        {
            return a.X == 0.0f && a.Y == 0.0f && a.Z == 0.0f && a.W == 0.0f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2BlendW(B2FloatW a, B2FloatW b, B2FloatW mask)
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
        public static B2FloatW b2DotW(B2Vec2W a, B2Vec2W b)
        {
            return b2AddW(b2MulW(a.X, b.X), b2MulW(a.Y, b.Y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2FloatW b2CrossW(B2Vec2W a, B2Vec2W b)
        {
            return b2SubW(b2MulW(a.X, b.Y), b2MulW(a.Y, b.X));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2W b2RotateVectorW(B2RotW q, B2Vec2W v)
        {
            return new B2Vec2W(b2SubW(b2MulW(q.C, v.X), b2MulW(q.S, v.Y)), b2AddW(b2MulW(q.S, v.X), b2MulW(q.C, v.Y)));
        }


        public static int b2GetContactConstraintSIMDByteCount()
        {
            //return sizeof( b2ContactConstraintSIMD );
            return -1;
        }


// Custom gather/scatter for each SIMD type
#if B2_SIMD_AVX2
// This is a load and 8x8 transpose
static b2BodyStateW b2GatherBodies( const b2BodyState* states, int* indices )
{
	B2_ASSERT( sizeof( b2BodyState ) == 32, "b2BodyState not 32 bytes" );
	B2_ASSERT( ( (uintptr_t)states & 0x1F ) == 0 );
	// b2BodyState b2_identityBodyState = {{0.0f, 0.0f}, 0.0f, 0, {0.0f, 0.0f}, {1.0f, 0.0f}};
	b2FloatW identity = _mm256_setr_ps( 0.0f, 0.0f, 0.0f, 0, 0.0f, 0.0f, 1.0f, 0.0f );
	b2FloatW b0 = indices[0] == B2_NULL_INDEX ? identity : _mm256_load_ps( (float*)( states + indices[0] ) );
	b2FloatW b1 = indices[1] == B2_NULL_INDEX ? identity : _mm256_load_ps( (float*)( states + indices[1] ) );
	b2FloatW b2 = indices[2] == B2_NULL_INDEX ? identity : _mm256_load_ps( (float*)( states + indices[2] ) );
	b2FloatW b3 = indices[3] == B2_NULL_INDEX ? identity : _mm256_load_ps( (float*)( states + indices[3] ) );
	b2FloatW b4 = indices[4] == B2_NULL_INDEX ? identity : _mm256_load_ps( (float*)( states + indices[4] ) );
	b2FloatW b5 = indices[5] == B2_NULL_INDEX ? identity : _mm256_load_ps( (float*)( states + indices[5] ) );
	b2FloatW b6 = indices[6] == B2_NULL_INDEX ? identity : _mm256_load_ps( (float*)( states + indices[6] ) );
	b2FloatW b7 = indices[7] == B2_NULL_INDEX ? identity : _mm256_load_ps( (float*)( states + indices[7] ) );

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
	if ( indices[0] != B2_NULL_INDEX )
		_mm256_store_ps( (float*)( states + indices[0] ), _mm256_permute2f128_ps( tt0, tt4, 0x20 ) );
	if ( indices[1] != B2_NULL_INDEX )
		_mm256_store_ps( (float*)( states + indices[1] ), _mm256_permute2f128_ps( tt1, tt5, 0x20 ) );
	if ( indices[2] != B2_NULL_INDEX )
		_mm256_store_ps( (float*)( states + indices[2] ), _mm256_permute2f128_ps( tt2, tt6, 0x20 ) );
	if ( indices[3] != B2_NULL_INDEX )
		_mm256_store_ps( (float*)( states + indices[3] ), _mm256_permute2f128_ps( tt3, tt7, 0x20 ) );
	if ( indices[4] != B2_NULL_INDEX )
		_mm256_store_ps( (float*)( states + indices[4] ), _mm256_permute2f128_ps( tt0, tt4, 0x31 ) );
	if ( indices[5] != B2_NULL_INDEX )
		_mm256_store_ps( (float*)( states + indices[5] ), _mm256_permute2f128_ps( tt1, tt5, 0x31 ) );
	if ( indices[6] != B2_NULL_INDEX )
		_mm256_store_ps( (float*)( states + indices[6] ), _mm256_permute2f128_ps( tt2, tt6, 0x31 ) );
	if ( indices[7] != B2_NULL_INDEX )
		_mm256_store_ps( (float*)( states + indices[7] ), _mm256_permute2f128_ps( tt3, tt7, 0x31 ) );
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

	b2FloatW b1a = indices[0] == B2_NULL_INDEX ? identityA : b2LoadW( (float*)( states + indices[0] ) + 0 );
	b2FloatW b1b = indices[0] == B2_NULL_INDEX ? identityB : b2LoadW( (float*)( states + indices[0] ) + 4 );
	b2FloatW b2a = indices[1] == B2_NULL_INDEX ? identityA : b2LoadW( (float*)( states + indices[1] ) + 0 );
	b2FloatW b2b = indices[1] == B2_NULL_INDEX ? identityB : b2LoadW( (float*)( states + indices[1] ) + 4 );
	b2FloatW b3a = indices[2] == B2_NULL_INDEX ? identityA : b2LoadW( (float*)( states + indices[2] ) + 0 );
	b2FloatW b3b = indices[2] == B2_NULL_INDEX ? identityB : b2LoadW( (float*)( states + indices[2] ) + 4 );
	b2FloatW b4a = indices[3] == B2_NULL_INDEX ? identityA : b2LoadW( (float*)( states + indices[3] ) + 0 );
	b2FloatW b4b = indices[3] == B2_NULL_INDEX ? identityB : b2LoadW( (float*)( states + indices[3] ) + 4 );

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

	// I don't use any dummy body in the body array because this will lead to multithreaded sharing and the
	// associated cache flushing.
	if ( indices[0] != B2_NULL_INDEX )
	{
		float32x4_t body1 = vcombine_f32( vget_low_f32( r1.val[0] ), vget_low_f32( r2.val[0] ) );
		b2StoreW( (float*)( states + indices[0] ), body1 );
	}

	if ( indices[1] != B2_NULL_INDEX )
	{
		float32x4_t body2 = vcombine_f32( vget_low_f32( r1.val[1] ), vget_low_f32( r2.val[1] ) );
		b2StoreW( (float*)( states + indices[1] ), body2 );
	}

	if ( indices[2] != B2_NULL_INDEX )
	{
		float32x4_t body3 = vcombine_f32( vget_high_f32( r1.val[0] ), vget_high_f32( r2.val[0] ) );
		b2StoreW( (float*)( states + indices[2] ), body3 );
	}

	if ( indices[3] != B2_NULL_INDEX )
	{
		float32x4_t body4 = vcombine_f32( vget_high_f32( r1.val[1] ), vget_high_f32( r2.val[1] ) );
		b2StoreW( (float*)( states + indices[3] ), body4 );
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

	b2FloatW b1a = indices[0] == B2_NULL_INDEX ? identityA : b2LoadW( (float*)( states + indices[0] ) + 0 );
	b2FloatW b1b = indices[0] == B2_NULL_INDEX ? identityB : b2LoadW( (float*)( states + indices[0] ) + 4 );
	b2FloatW b2a = indices[1] == B2_NULL_INDEX ? identityA : b2LoadW( (float*)( states + indices[1] ) + 0 );
	b2FloatW b2b = indices[1] == B2_NULL_INDEX ? identityB : b2LoadW( (float*)( states + indices[1] ) + 4 );
	b2FloatW b3a = indices[2] == B2_NULL_INDEX ? identityA : b2LoadW( (float*)( states + indices[2] ) + 0 );
	b2FloatW b3b = indices[2] == B2_NULL_INDEX ? identityB : b2LoadW( (float*)( states + indices[2] ) + 4 );
	b2FloatW b4a = indices[3] == B2_NULL_INDEX ? identityA : b2LoadW( (float*)( states + indices[3] ) + 0 );
	b2FloatW b4b = indices[3] == B2_NULL_INDEX ? identityB : b2LoadW( (float*)( states + indices[3] ) + 4 );

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

	// [vx1 vy1 vx2 vy2]
	b2FloatW t1 = b2UnpackLoW( simdBody.v.X, simdBody.v.Y );
	// [vx3 vy3 vx4 vy4]
	b2FloatW t2 = b2UnpackHiW( simdBody.v.X, simdBody.v.Y );
	// [w1 f1 w2 f2]
	b2FloatW t3 = b2UnpackLoW( simdBody.w, simdBody.flags );
	// [w3 f3 w4 f4]
	b2FloatW t4 = b2UnpackHiW( simdBody.w, simdBody.flags );

	// I don't use any dummy body in the body array because this will lead to multithreaded sharing and the
	// associated cache flushing.
	if ( indices[0] != B2_NULL_INDEX )
	{
		// [t1.x t1.y t3.x t3.y]
		b2StoreW( (float*)( states + indices[0] ), _mm_shuffle_ps( t1, t3, _MM_SHUFFLE( 1, 0, 1, 0 ) ) );
	}

	if ( indices[1] != B2_NULL_INDEX )
	{
		// [t1.z t1.w t3.z t3.w]
		b2StoreW( (float*)( states + indices[1] ), _mm_shuffle_ps( t1, t3, _MM_SHUFFLE( 3, 2, 3, 2 ) ) );
	}

	if ( indices[2] != B2_NULL_INDEX )
	{
		// [t2.x t2.y t4.x t4.y]
		b2StoreW( (float*)( states + indices[2] ), _mm_shuffle_ps( t2, t4, _MM_SHUFFLE( 1, 0, 1, 0 ) ) );
	}

	if ( indices[3] != B2_NULL_INDEX )
	{
		// [t2.z t2.w t4.z t4.w]
		b2StoreW( (float*)( states + indices[3] ), _mm_shuffle_ps( t2, t4, _MM_SHUFFLE( 3, 2, 3, 2 ) ) );
	}
}

#else

        // This is a load and transpose
        public static B2BodyStateW b2GatherBodies(B2BodyState[] states, ReadOnlySpan<int> indices)
        {
            B2BodyState identity = b2_identityBodyState;

            B2BodyState s1 = indices[0] == B2_NULL_INDEX ? identity : states[indices[0]];
            B2BodyState s2 = indices[1] == B2_NULL_INDEX ? identity : states[indices[1]];
            B2BodyState s3 = indices[2] == B2_NULL_INDEX ? identity : states[indices[2]];
            B2BodyState s4 = indices[3] == B2_NULL_INDEX ? identity : states[indices[3]];

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
        public static void b2ScatterBodies(B2BodyState[] states, ReadOnlySpan<int> indices, ref B2BodyStateW simdBody)
        {
            // todo somehow skip writing to kinematic bodies

            if (indices[0] != B2_NULL_INDEX)
            {
                B2BodyState state = states[indices[0]];
                state.linearVelocity.X = simdBody.v.X.X;
                state.linearVelocity.Y = simdBody.v.Y.X;
                state.angularVelocity = simdBody.w.X;
            }

            if (indices[1] != B2_NULL_INDEX)
            {
                B2BodyState state = states[indices[1]];
                state.linearVelocity.X = simdBody.v.X.Y;
                state.linearVelocity.Y = simdBody.v.Y.Y;
                state.angularVelocity = simdBody.w.Y;
            }

            if (indices[2] != B2_NULL_INDEX)
            {
                B2BodyState state = states[indices[2]];
                state.linearVelocity.X = simdBody.v.X.Z;
                state.linearVelocity.Y = simdBody.v.Y.Z;
                state.angularVelocity = simdBody.w.Z;
            }

            if (indices[3] != B2_NULL_INDEX)
            {
                B2BodyState state = states[indices[3]];
                state.linearVelocity.X = simdBody.v.X.W;
                state.linearVelocity.Y = simdBody.v.Y.W;
                state.angularVelocity = simdBody.w.W;
            }
        }

#endif

        // Contacts that live within the constraint graph coloring
        public static void b2PrepareContactsTask(int startIndex, int endIndex, B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.prepare_contact, "Prepare Contact", B2HexColor.b2_colorYellow, true);
            B2World world = context.world;
            Span<B2ContactSim> contacts = context.contacts;
            Span<B2ContactConstraintSIMD> constraints = context.simdContactConstraints;
            B2BodyState[] awakeStates = context.states;
#if DEBUG
            B2Body[] bodies = world.bodies.data;
#endif

            // Stiffer for static contacts to avoid bodies getting pushed through the ground
            B2Softness contactSoftness = context.contactSoftness;
            B2Softness staticSoftness = context.staticSoftness;

            float warmStartScale = world.enableWarmStarting ? 1.0f : 0.0f;

            for (int i = startIndex; i < endIndex; ++i)
            {
                ref B2ContactConstraintSIMD constraint = ref constraints[i];

                for (int j = 0; j < B2_SIMD_WIDTH; ++j)
                {
                    B2ContactSim contactSim = contacts[B2_SIMD_WIDTH * i + j];

                    if (contactSim != null)
                    {
                        ref B2Manifold manifold = ref contactSim.manifold;

                        int indexA = contactSim.bodySimIndexA;
                        int indexB = contactSim.bodySimIndexB;

#if DEBUG
                        B2Body bodyA = bodies[contactSim.bodyIdA];
                        int validIndexA = bodyA.setIndex == (int)B2SetType.b2_awakeSet ? bodyA.localIndex : B2_NULL_INDEX;
                        B2Body bodyB = bodies[contactSim.bodyIdB];
                        int validIndexB = bodyB.setIndex == (int)B2SetType.b2_awakeSet ? bodyB.localIndex : B2_NULL_INDEX;

                        B2_ASSERT(indexA == validIndexA);
                        B2_ASSERT(indexB == validIndexB);
#endif
                        constraint.indexA[j] = indexA;
                        constraint.indexB[j] = indexB;

                        B2Vec2 vA = b2Vec2_zero;
                        float wA = 0.0f;
                        float mA = contactSim.invMassA;
                        float iA = contactSim.invIA;
                        if (indexA != B2_NULL_INDEX)
                        {
                            B2BodyState stateA = awakeStates[indexA];
                            vA = stateA.linearVelocity;
                            wA = stateA.angularVelocity;
                        }

                        B2Vec2 vB = b2Vec2_zero;
                        float wB = 0.0f;
                        float mB = contactSim.invMassB;
                        float iB = contactSim.invIB;
                        if (indexB != B2_NULL_INDEX)
                        {
                            B2BodyState stateB = awakeStates[indexB];
                            vB = stateB.linearVelocity;
                            wB = stateB.angularVelocity;
                        }

                        // TODO: @ikpil, check
                        constraint.invMassA[j] = mA;
                        constraint.invMassB[j] = mB;
                        constraint.invIA[j] = iA;
                        constraint.invIB[j] = iB;

                        {
                            float k = iA + iB;
                            constraint.rollingMass[j] = k > 0.0f ? 1.0f / k : 0.0f;
                        }

                        B2Softness soft = (indexA == B2_NULL_INDEX || indexB == B2_NULL_INDEX) ? staticSoftness : contactSoftness;

                        B2Vec2 normal = manifold.normal;
                        constraint.normal.X[j] = normal.X;
                        constraint.normal.Y[j] = normal.Y;

                        constraint.friction[j] = contactSim.friction;
                        constraint.tangentSpeed[j] = contactSim.tangentSpeed;
                        constraint.restitution[j] = contactSim.restitution;
                        constraint.rollingResistance[j] = contactSim.rollingResistance;
                        constraint.rollingImpulse[j] = warmStartScale * manifold.rollingImpulse;

                        constraint.biasRate[j] = soft.biasRate;
                        constraint.massScale[j] = soft.massScale;
                        constraint.impulseScale[j] = soft.impulseScale;

                        B2Vec2 tangent = b2RightPerp(normal);

                        {
                            ref B2ManifoldPoint mp = ref manifold.points[0];

                            B2Vec2 rA = mp.anchorA;
                            B2Vec2 rB = mp.anchorB;

                            constraint.anchorA1.X[j] = rA.X;
                            constraint.anchorA1.Y[j] = rA.Y;
                            constraint.anchorB1.X[j] = rB.X;
                            constraint.anchorB1.Y[j] = rB.Y;

                            constraint.baseSeparation1[j] = mp.separation - b2Dot(b2Sub(rB, rA), normal);

                            constraint.normalImpulse1[j] = warmStartScale * mp.normalImpulse;
                            constraint.tangentImpulse1[j] = warmStartScale * mp.tangentImpulse;
                            constraint.totalNormalImpulse1[j] = 0.0f;

                            float rnA = b2Cross(rA, normal);
                            float rnB = b2Cross(rB, normal);
                            float kNormal = mA + mB + iA * rnA * rnA + iB * rnB * rnB;
                            constraint.normalMass1[j] = kNormal > 0.0f ? 1.0f / kNormal : 0.0f;

                            float rtA = b2Cross(rA, tangent);
                            float rtB = b2Cross(rB, tangent);
                            float kTangent = mA + mB + iA * rtA * rtA + iB * rtB * rtB;
                            constraint.tangentMass1[j] = kTangent > 0.0f ? 1.0f / kTangent : 0.0f;

                            // relative velocity for restitution
                            B2Vec2 vrA = b2Add(vA, b2CrossSV(wA, rA));
                            B2Vec2 vrB = b2Add(vB, b2CrossSV(wB, rB));
                            constraint.relativeVelocity1[j] = b2Dot(normal, b2Sub(vrB, vrA));
                        }

                        int pointCount = manifold.pointCount;
                        B2_ASSERT(0 < pointCount && pointCount <= 2);

                        if (pointCount == 2)
                        {
                            ref B2ManifoldPoint mp = ref manifold.points[1];

                            B2Vec2 rA = mp.anchorA;
                            B2Vec2 rB = mp.anchorB;

                            constraint.anchorA2.X[j] = rA.X;
                            constraint.anchorA2.Y[j] = rA.Y;
                            constraint.anchorB2.X[j] = rB.X;
                            constraint.anchorB2.Y[j] = rB.Y;

                            constraint.baseSeparation2[j] = mp.separation - b2Dot(b2Sub(rB, rA), normal);

                            constraint.normalImpulse2[j] = warmStartScale * mp.normalImpulse;
                            constraint.tangentImpulse2[j] = warmStartScale * mp.tangentImpulse;
                            constraint.totalNormalImpulse2[j] = 0.0f;

                            float rnA = b2Cross(rA, normal);
                            float rnB = b2Cross(rB, normal);
                            float kNormal = mA + mB + iA * rnA * rnA + iB * rnB * rnB;
                            constraint.normalMass2[j] = kNormal > 0.0f ? 1.0f / kNormal : 0.0f;

                            float rtA = b2Cross(rA, tangent);
                            float rtB = b2Cross(rB, tangent);
                            float kTangent = mA + mB + iA * rtA * rtA + iB * rtB * rtB;
                            constraint.tangentMass2[j] = kTangent > 0.0f ? 1.0f / kTangent : 0.0f;

                            // relative velocity for restitution
                            B2Vec2 vrA = b2Add(vA, b2CrossSV(wA, rA));
                            B2Vec2 vrB = b2Add(vB, b2CrossSV(wB, rB));
                            constraint.relativeVelocity2[j] = b2Dot(normal, b2Sub(vrB, vrA));
                        }
                        else
                        {
                            // dummy data that has no effect
                            constraint.baseSeparation2[j] = 0.0f;
                            constraint.normalImpulse2[j] = 0.0f;
                            constraint.tangentImpulse2[j] = 0.0f;
                            constraint.totalNormalImpulse2[j] = 0.0f;
                            constraint.anchorA2.X[j] = 0.0f;
                            constraint.anchorA2.Y[j] = 0.0f;
                            constraint.anchorB2.X[j] = 0.0f;
                            constraint.anchorB2.Y[j] = 0.0f;
                            constraint.normalMass2[j] = 0.0f;
                            constraint.tangentMass2[j] = 0.0f;
                            constraint.relativeVelocity2[j] = 0.0f;
                        }
                    }
                    else
                    {
                        // SIMD remainder
                        constraint.indexA[j] = B2_NULL_INDEX;
                        constraint.indexB[j] = B2_NULL_INDEX;

                        constraint.invMassA[j] = 0.0f;
                        constraint.invMassB[j] = 0.0f;
                        constraint.invIA[j] = 0.0f;
                        constraint.invIB[j] = 0.0f;

                        constraint.normal.X[j] = 0.0f;
                        constraint.normal.Y[j] = 0.0f;
                        constraint.friction[j] = 0.0f;
                        constraint.tangentSpeed[j] = 0.0f;
                        constraint.rollingResistance[j] = 0.0f;
                        constraint.rollingMass[j] = 0.0f;
                        constraint.rollingImpulse[j] = 0.0f;
                        constraint.biasRate[j] = 0.0f;
                        constraint.massScale[j] = 0.0f;
                        constraint.impulseScale[j] = 0.0f;

                        constraint.anchorA1.X[j] = 0.0f;
                        constraint.anchorA1.Y[j] = 0.0f;
                        constraint.anchorB1.X[j] = 0.0f;
                        constraint.anchorB1.Y[j] = 0.0f;
                        constraint.baseSeparation1[j] = 0.0f;
                        constraint.normalImpulse1[j] = 0.0f;
                        constraint.tangentImpulse1[j] = 0.0f;
                        constraint.totalNormalImpulse1[j] = 0.0f;
                        constraint.normalMass1[j] = 0.0f;
                        constraint.tangentMass1[j] = 0.0f;

                        constraint.anchorA2.X[j] = 0.0f;
                        constraint.anchorA2.Y[j] = 0.0f;
                        constraint.anchorB2.X[j] = 0.0f;
                        constraint.anchorB2.Y[j] = 0.0f;
                        constraint.baseSeparation2[j] = 0.0f;
                        constraint.normalImpulse2[j] = 0.0f;
                        constraint.tangentImpulse2[j] = 0.0f;
                        constraint.totalNormalImpulse2[j] = 0.0f;
                        constraint.normalMass2[j] = 0.0f;
                        constraint.tangentMass2[j] = 0.0f;

                        constraint.restitution[j] = 0.0f;
                        constraint.relativeVelocity1[j] = 0.0f;
                        constraint.relativeVelocity2[j] = 0.0f;
                    }
                }
            }

            b2TracyCZoneEnd(B2TracyCZone.prepare_contact);
        }

        public static void b2WarmStartContactsTask(int startIndex, int endIndex, B2StepContext context, int colorIndex)
        {
            b2TracyCZoneNC(B2TracyCZone.warm_start_contact, "Warm Start", B2HexColor.b2_colorGreen, true);

            B2BodyState[] states = context.states;
            Span<B2ContactConstraintSIMD> constraints = context.graph.colors[colorIndex].simdConstraints;

            for (int i = startIndex; i < endIndex; ++i)
            {
                ref B2ContactConstraintSIMD c = ref constraints[i];
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
                }

                bA.w = b2MulSubW(bA.w, c.invIA, c.rollingImpulse);
                bB.w = b2MulAddW(bB.w, c.invIB, c.rollingImpulse);

                b2ScatterBodies(states, c.indexA.AsSpan(), ref bA);
                b2ScatterBodies(states, c.indexB.AsSpan(), ref bB);
            }

            b2TracyCZoneEnd(B2TracyCZone.warm_start_contact);
        }

        public static void b2SolveContactsTask(int startIndex, int endIndex, B2StepContext context, int colorIndex, bool useBias)
        {
            b2TracyCZoneNC(B2TracyCZone.solve_contact, "Solve Contact", B2HexColor.b2_colorAliceBlue, true);

            B2BodyState[] states = context.states;
            Span<B2ContactConstraintSIMD> constraints = context.graph.colors[colorIndex].simdConstraints;
            B2FloatW inv_h = b2SplatW(context.inv_h);
            B2FloatW contactSpeed = b2SplatW(-context.world.contactSpeed);
            B2FloatW oneW = b2SplatW(1.0f);

            for (int i = startIndex; i < endIndex; ++i)
            {
                ref B2ContactConstraintSIMD c = ref constraints[i];

                B2BodyStateW bA = b2GatherBodies(states, c.indexA.AsSpan());
                B2BodyStateW bB = b2GatherBodies(states, c.indexB.AsSpan());

                B2FloatW biasRate, massScale, impulseScale;
                if (useBias)
                {
                    biasRate = b2MulW(c.massScale, c.biasRate);
                    massScale = c.massScale;
                    impulseScale = c.impulseScale;
                }
                else
                {
                    biasRate = b2ZeroW();
                    massScale = oneW;
                    impulseScale = b2ZeroW();
                }

                B2FloatW totalNormalImpulse = b2ZeroW();

                B2Vec2W dp = new B2Vec2W(b2SubW(bB.dp.X, bA.dp.X), b2SubW(bB.dp.Y, bA.dp.Y));

                // point1 non-penetration constraint
                {
                    // fixed anchors for Jacobians
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
                    B2FloatW mask = b2GreaterThanW(s, b2ZeroW());
                    B2FloatW specBias = b2MulW(s, inv_h);
                    B2FloatW softBias = b2MaxW(b2MulW(biasRate, s), contactSpeed);

                    // todo try b2MaxW(softBias, specBias);
                    B2FloatW bias = b2BlendW(softBias, specBias, mask);

                    B2FloatW pointMassScale = b2BlendW(massScale, oneW, mask);
                    B2FloatW pointImpulseScale = b2BlendW(impulseScale, b2ZeroW(), mask);

                    // Relative velocity at contact
                    B2FloatW dvx = b2SubW(b2SubW(bB.v.X, b2MulW(bB.w, rB.Y)), b2SubW(bA.v.X, b2MulW(bA.w, rA.Y)));
                    B2FloatW dvy = b2SubW(b2AddW(bB.v.Y, b2MulW(bB.w, rB.X)), b2AddW(bA.v.Y, b2MulW(bA.w, rA.X)));
                    B2FloatW vn = b2AddW(b2MulW(dvx, c.normal.X), b2MulW(dvy, c.normal.Y));

                    // Compute normal impulse
                    B2FloatW negImpulse = b2AddW(b2MulW(c.normalMass1, b2AddW(b2MulW(pointMassScale, vn), bias)), b2MulW(pointImpulseScale, c.normalImpulse1));

                    // Clamp the accumulated impulse
                    B2FloatW newImpulse = b2MaxW(b2SubW(c.normalImpulse1, negImpulse), b2ZeroW());
                    B2FloatW impulse = b2SubW(newImpulse, c.normalImpulse1);
                    c.normalImpulse1 = newImpulse;
                    c.totalNormalImpulse1 = b2MaxW(c.totalNormalImpulse1, newImpulse);

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

                    B2FloatW mask = b2GreaterThanW(s, b2ZeroW());
                    B2FloatW specBias = b2MulW(s, inv_h);
                    B2FloatW softBias = b2MaxW(b2MulW(biasRate, s), contactSpeed);
                    B2FloatW bias = b2BlendW(softBias, specBias, mask);

                    B2FloatW pointMassScale = b2BlendW(massScale, oneW, mask);
                    B2FloatW pointImpulseScale = b2BlendW(impulseScale, b2ZeroW(), mask);

                    // fixed anchors for Jacobians
                    B2Vec2W rA = c.anchorA2;
                    B2Vec2W rB = c.anchorB2;

                    // Relative velocity at contact
                    B2FloatW dvx = b2SubW(b2SubW(bB.v.X, b2MulW(bB.w, rB.Y)), b2SubW(bA.v.X, b2MulW(bA.w, rA.Y)));
                    B2FloatW dvy = b2SubW(b2AddW(bB.v.Y, b2MulW(bB.w, rB.X)), b2AddW(bA.v.Y, b2MulW(bA.w, rA.X)));
                    B2FloatW vn = b2AddW(b2MulW(dvx, c.normal.X), b2MulW(dvy, c.normal.Y));

                    // Compute normal impulse
                    B2FloatW negImpulse = b2AddW(b2MulW(c.normalMass2, b2MulW(pointMassScale, b2AddW(vn, bias))), b2MulW(pointImpulseScale, c.normalImpulse2));

                    // Clamp the accumulated impulse
                    B2FloatW newImpulse = b2MaxW(b2SubW(c.normalImpulse2, negImpulse), b2ZeroW());
                    B2FloatW impulse = b2SubW(newImpulse, c.normalImpulse2);
                    c.normalImpulse2 = newImpulse;
                    c.totalNormalImpulse2 = b2MaxW(c.totalNormalImpulse2, newImpulse);

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

                B2FloatW tangentX = c.normal.Y;
                B2FloatW tangentY = b2SubW(b2ZeroW(), c.normal.X);

                // point 1 friction constraint
                {
                    // fixed anchors for Jacobians
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

                // Rolling resistance
                {
                    B2FloatW deltaLambda = b2MulW(c.rollingMass, b2SubW(bA.w, bB.w));
                    B2FloatW lambda = c.rollingImpulse;
                    B2FloatW maxLambda = b2MulW(c.rollingResistance, totalNormalImpulse);
                    c.rollingImpulse = b2SymClampW(b2AddW(lambda, deltaLambda), maxLambda);
                    deltaLambda = b2SubW(c.rollingImpulse, lambda);

                    bA.w = b2MulSubW(bA.w, c.invIA, deltaLambda);
                    bB.w = b2MulAddW(bB.w, c.invIB, deltaLambda);
                }

                b2ScatterBodies(states, c.indexA.AsSpan(), ref bA);
                b2ScatterBodies(states, c.indexB.AsSpan(), ref bB);
            }

            b2TracyCZoneEnd(B2TracyCZone.solve_contact);
        }

        public static void b2ApplyRestitutionTask(int startIndex, int endIndex, B2StepContext context, int colorIndex)
        {
            b2TracyCZoneNC(B2TracyCZone.restitution, "Restitution", B2HexColor.b2_colorDodgerBlue, true);

            B2BodyState[] states = context.states;
            Span<B2ContactConstraintSIMD> constraints = context.graph.colors[colorIndex].simdConstraints;
            B2FloatW threshold = b2SplatW(context.world.restitutionThreshold);
            B2FloatW zero = b2ZeroW();

            for (int i = startIndex; i < endIndex; ++i)
            {
                ref B2ContactConstraintSIMD c = ref constraints[i];

                if (b2AllZeroW(c.restitution))
                {
                    // No lanes have restitution. Common case.
                    continue;
                }

                // Create a mask based on restitution so that lanes with no restitution are not affected
                // by the calculations below.
                B2FloatW restitutionMask = b2EqualsW(c.restitution, zero);

                B2BodyStateW bA = b2GatherBodies(states, c.indexA.AsSpan());
                B2BodyStateW bB = b2GatherBodies(states, c.indexB.AsSpan());

                // first point non-penetration constraint
                {
                    // Set effective mass to zero if restitution should not be applied
                    B2FloatW mask1 = b2GreaterThanW(b2AddW(c.relativeVelocity1, threshold), zero);
                    B2FloatW mask2 = b2EqualsW(c.totalNormalImpulse1, zero);
                    B2FloatW mask = b2OrW(b2OrW(mask1, mask2), restitutionMask);
                    B2FloatW mass = b2BlendW(c.normalMass1, zero, mask);

                    // fixed anchors for Jacobians
                    B2Vec2W rA = c.anchorA1;
                    B2Vec2W rB = c.anchorB1;

                    // Relative velocity at contact
                    B2FloatW dvx = b2SubW(b2SubW(bB.v.X, b2MulW(bB.w, rB.Y)), b2SubW(bA.v.X, b2MulW(bA.w, rA.Y)));
                    B2FloatW dvy = b2SubW(b2AddW(bB.v.Y, b2MulW(bB.w, rB.X)), b2AddW(bA.v.Y, b2MulW(bA.w, rA.X)));
                    B2FloatW vn = b2AddW(b2MulW(dvx, c.normal.X), b2MulW(dvy, c.normal.Y));

                    // Compute normal impulse
                    B2FloatW negImpulse = b2MulW(mass, b2AddW(vn, b2MulW(c.restitution, c.relativeVelocity1)));

                    // Clamp the accumulated impulse
                    B2FloatW newImpulse = b2MaxW(b2SubW(c.normalImpulse1, negImpulse), b2ZeroW());
                    B2FloatW impulse = b2SubW(newImpulse, c.normalImpulse1);
                    c.normalImpulse1 = newImpulse;

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
                    // Set effective mass to zero if restitution should not be applied
                    B2FloatW mask1 = b2GreaterThanW(b2AddW(c.relativeVelocity2, threshold), zero);
                    B2FloatW mask2 = b2EqualsW(c.totalNormalImpulse2, zero);
                    B2FloatW mask = b2OrW(b2OrW(mask1, mask2), restitutionMask);
                    B2FloatW mass = b2BlendW(c.normalMass2, zero, mask);

                    // fixed anchors for Jacobians
                    B2Vec2W rA = c.anchorA2;
                    B2Vec2W rB = c.anchorB2;

                    // Relative velocity at contact
                    B2FloatW dvx = b2SubW(b2SubW(bB.v.X, b2MulW(bB.w, rB.Y)), b2SubW(bA.v.X, b2MulW(bA.w, rA.Y)));
                    B2FloatW dvy = b2SubW(b2AddW(bB.v.Y, b2MulW(bB.w, rB.X)), b2AddW(bA.v.Y, b2MulW(bA.w, rA.X)));
                    B2FloatW vn = b2AddW(b2MulW(dvx, c.normal.X), b2MulW(dvy, c.normal.Y));

                    // Compute normal impulse
                    B2FloatW negImpulse = b2MulW(mass, b2AddW(vn, b2MulW(c.restitution, c.relativeVelocity2)));

                    // Clamp the accumulated impulse
                    B2FloatW newImpulse = b2MaxW(b2SubW(c.normalImpulse2, negImpulse), b2ZeroW());
                    B2FloatW impulse = b2SubW(newImpulse, c.normalImpulse2);
                    c.normalImpulse2 = newImpulse;

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

            b2TracyCZoneEnd(B2TracyCZone.restitution);
        }

        public static void b2StoreImpulsesTask(int startIndex, int endIndex, B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.store_impulses, "Store", B2HexColor.b2_colorFireBrick, true);

            Span<B2ContactSim> contacts = context.contacts;
            Span<B2ContactConstraintSIMD> constraints = context.simdContactConstraints;

            B2Manifold dummy = new B2Manifold();

            for (int constraintIndex = startIndex; constraintIndex < endIndex; ++constraintIndex)
            {
                ref B2ContactConstraintSIMD c = ref constraints[constraintIndex];
                ref B2FloatW rollingImpulse = ref c.rollingImpulse;
                ref B2FloatW normalImpulse1 = ref c.normalImpulse1;
                ref B2FloatW normalImpulse2 = ref c.normalImpulse2;
                ref B2FloatW tangentImpulse1 = ref c.tangentImpulse1;
                ref B2FloatW tangentImpulse2 = ref c.tangentImpulse2;
                ref B2FloatW totalNormalImpulse1 = ref c.totalNormalImpulse1;
                ref B2FloatW totalNormalImpulse2 = ref c.totalNormalImpulse2;
                ref B2FloatW normalVelocity1 = ref c.relativeVelocity1;
                ref B2FloatW normalVelocity2 = ref c.relativeVelocity2;

                int baseIndex = B2_SIMD_WIDTH * constraintIndex;

                for (int laneIndex = 0; laneIndex < B2_SIMD_WIDTH; ++laneIndex)
                {
                    ref B2Manifold m = ref contacts[baseIndex + laneIndex] == null ? ref dummy : ref contacts[baseIndex + laneIndex].manifold;
                    m.rollingImpulse = rollingImpulse[laneIndex];

                    m.points[0].normalImpulse = normalImpulse1[laneIndex];
                    m.points[0].tangentImpulse = tangentImpulse1[laneIndex];
                    m.points[0].totalNormalImpulse = totalNormalImpulse1[laneIndex];
                    m.points[0].normalVelocity = normalVelocity1[laneIndex];

                    m.points[1].normalImpulse = normalImpulse2[laneIndex];
                    m.points[1].tangentImpulse = tangentImpulse2[laneIndex];
                    m.points[1].totalNormalImpulse = totalNormalImpulse2[laneIndex];
                    m.points[1].normalVelocity = normalVelocity2[laneIndex];
                }
            }

            b2TracyCZoneEnd(B2TracyCZone.store_impulses);
        }
    }
}