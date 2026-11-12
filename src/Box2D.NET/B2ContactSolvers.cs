// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
#if NET8_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Cores;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Profiling;
using static Box2D.NET.B2Atomics;
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
            B2BodyState[] states = context.states;

#if DEBUG
            B2Body[] bodies = world.bodies.data;
#endif

            // Stiffer for static contacts to avoid bodies getting pushed through the ground
            B2Softness contactSoftness = context.contactSoftness;
            B2Softness staticSoftness = context.staticSoftness;

            float warmStartScale = world.enableWarmStarting ? 1.0f : 0.0f;
            bool anyRestitution = false;

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
                constraint.restitution = contactSim.restitution;
                constraint.rollingResistance = contactSim.rollingResistance;
                constraint.rollingImpulse = warmStartScale * manifold.rollingImpulse;
                constraint.tangentSpeed = contactSim.tangentSpeed;
                constraint.pointCount = pointCount;

                bool haveRestitution = contactSim.restitution > 0.0f;
                bool hitEvents = (contactSim.simFlags & (uint)B2ContactFlags.b2_simEnableHitEvent) != 0;
                bool sampleVelocity = haveRestitution || hitEvents;
                anyRestitution = anyRestitution || haveRestitution;

                B2Vec2 vA = b2Vec2_zero;
                float wA = 0.0f;
                B2Vec2 vB = b2Vec2_zero;
                float wB = 0.0f;

                if (sampleVelocity)
                {
                    if (indexA != B2_NULL_INDEX)
                    {
                        vA = states[indexA].linearVelocity;
                        wA = states[indexA].angularVelocity;
                    }

                    if (indexB != B2_NULL_INDEX)
                    {
                        vB = states[indexB].linearVelocity;
                        wB = states[indexB].angularVelocity;
                    }
                }

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
                    cp.restitutionImpulse = 0.0f;

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

                    // Only compute the normal velocity terms if needed.
                    if (sampleVelocity)
                    {
                        B2Vec2 vrA = b2Add(vA, b2CrossSV(wA, rA));
                        B2Vec2 vrB = b2Add(vB, b2CrossSV(wB, rB));
                        float vn = b2Dot(normal, b2Sub(vrB, vrA));

                        cp.relativeVelocity = vn;
                        mp.normalVelocity = vn;
                    }
                    else
                    {
                        cp.relativeVelocity = 0.0f;
                        mp.normalVelocity = 0.0f;
                    }
                }
            }

            if (anyRestitution)
            {
                // Check if set first? Store per task context and OR?
                b2AtomicStoreInt(ref context.anyRestitution, 1);
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
        internal static void b2PushContacts_Overflow(B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.push_contact, "Push Overflow Contact", B2HexColor.b2_colorAliceBlue, true);

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

            Span<float> rnA = stackalloc float[2];
            Span<float> rnB = stackalloc float[2];
            Span<float> impulses = stackalloc float[2];

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
                B2Softness softness = constraint.softness;
                int pointCount = constraint.pointCount;

                float vnc = b2Dot(b2Sub(vB, vA), normal);

                impulses[0] = 0.0f;
                impulses[1] = 0.0f;
                float normalImpulse = 0.0f;
                float angularA = 0.0f;
                float angularB = 0.0f;

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

                    float velocityBias;
                    float massScale;
                    float impulseScale;
                    if (s > 0.0f)
                    {
                        // speculative bias is positive
                        velocityBias = s * inv_h;
                        massScale = 1.0f;
                        impulseScale = 0.0f;
                    }
                    else
                    {
                        // overlap bias is negative
                        velocityBias = b2MaxFloat(softness.massScale * softness.biasRate * s, -contactSpeed);
                        massScale = softness.massScale;
                        impulseScale = softness.impulseScale;
                    }

                    rnA[j] = b2Cross(rA, normal);
                    rnB[j] = b2Cross(rB, normal);

                    // Relative normal velocity at contact.
                    float vn = vnc + wB * rnB[j] - wA * rnA[j];
                    for (int k = 0; k < j; ++k)
                    {
                        // Point 2 only depends on the impulse from point 1.
                        vn += (mA + mB + iA * rnA[k] * rnA[j] + iB * rnB[k] * rnB[j]) * impulses[k];
                    }

                    float impulse = b2MaxFloat(-cp.normalMass * (massScale * vn + velocityBias) - impulseScale * cp.normalImpulse,
                        -cp.normalImpulse);
                    cp.normalImpulse += impulse;
                    cp.totalNormalImpulse += impulse;

                    impulses[j] = impulse;

                    // These accumulators are not used until the loop is done.
                    normalImpulse += impulse;
                    angularA += impulse * rnA[j];
                    angularB += impulse * rnB[j];
                }

                B2Vec2 P = b2MulSV(normalImpulse, normal);
                vA = b2MulSub(vA, mA, P);
                wA -= iA * angularA;
                vB = b2MulAdd(vB, mB, P);
                wB += iB * angularB;

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

            b2TracyCZoneEnd(B2TracyCZone.push_contact);
        }

        // Solve contacts: normal, friction and rolling resistance.
        internal static void b2SolveContacts_Overflow(B2StepContext context)
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

            // This is a dummy body to represent a static body since static bodies don't have a solver body.
            B2BodyState dummyState = B2BodyState.Create(b2_identityBodyState);

            Span<float> normalImpulses = stackalloc float[2];
            Span<float> tangentImpulses = stackalloc float[2];
            Span<float> rnA = stackalloc float[2];
            Span<float> rnB = stackalloc float[2];
            Span<float> rtA = stackalloc float[2];
            Span<float> rtB = stackalloc float[2];

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

                int pointCount = constraint.pointCount;
                float totalNormalImpulse = 0.0f;

                B2Vec2 dv = b2Sub(vB, vA);
                float vnc = b2Dot(dv, normal);

                // vt = dot(vrB - sB * tangent - (vrA + sA * tangent), tangent)
                //    = dot(vrB - vrA, tangent) - (sA + sB)
                float vtc = b2Dot(dv, tangent) - constraint.tangentSpeed;

                normalImpulses[0] = 0.0f;
                normalImpulses[1] = 0.0f;
                tangentImpulses[0] = 0.0f;
                tangentImpulses[1] = 0.0f;

                // Cache cross products
                for (int j = 0; j < pointCount; ++j)
                {
                    ref B2ContactConstraintPoint cp = ref constraint.points[j];
                    rnA[j] = b2Cross(cp.anchorA, normal);
                    rnB[j] = b2Cross(cp.anchorB, normal);
                    rtA[j] = b2Cross(cp.anchorA, tangent);
                    rtB[j] = b2Cross(cp.anchorB, tangent);
                }

                for (int j = 0; j < pointCount; ++j)
                {
                    ref B2ContactConstraintPoint cp = ref constraint.points[j];

                    // Fixed anchor points.
                    B2Vec2 rA = cp.anchorA;
                    B2Vec2 rB = cp.anchorB;

                    // Compute current separation.
                    // This is subject to round-off error if the anchor is far from the body center of mass.
                    B2Vec2 ds = b2Add(dp, b2Sub(b2RotateVector(dqB, rB), b2RotateVector(dqA, rA)));
                    float s = cp.baseSeparation + b2Dot(ds, normal);

                    // Speculative bias, zero when overlapped.
                    float velocityBias = s > 0.0f ? s * inv_h : 0.0f;

                    // Relative normal velocity at contact.
                    float vn = vnc + wB * rnB[j] - wA * rnA[j];
                    for (int k = 0; k < j; ++k)
                    {
                        // This allows getting point 2 normal velocity with only one dependency.
                        vn += (mA + mB + iA * rnA[k] * rnA[j] + iB * rnB[k] * rnB[j]) * normalImpulses[k];
                    }

                    float impulse = b2MaxFloat(-cp.normalMass * (vn + velocityBias), -cp.normalImpulse);
                    cp.normalImpulse += impulse;
                    cp.totalNormalImpulse += impulse;
                    normalImpulses[j] = impulse;

                    // This is used for rolling resistance
                    totalNormalImpulse += cp.normalImpulse;
                }

                // Rolling resistance
                float rollingImpulse;
                {
                    float Cdot = wB - wA;
                    for (int j = 0; j < pointCount; ++j)
                    {
                        Cdot += (iA * rnA[j] + iB * rnB[j]) * normalImpulses[j];
                    }

                    float lambda = constraint.rollingImpulse;
                    float maxLambda = constraint.rollingResistance * totalNormalImpulse;
                    constraint.rollingImpulse = b2ClampFloat(lambda - constraint.rollingMass * Cdot, -maxLambda, maxLambda);
                    rollingImpulse = constraint.rollingImpulse - lambda;
                }

                // Friction
                for (int j = 0; j < pointCount; ++j)
                {
                    ref B2ContactConstraintPoint cp = ref constraint.points[j];

                    // relative tangent velocity at contact
                    float vt = vtc + wB * rtB[j] - wA * rtA[j] + (iA * rtA[j] + iB * rtB[j]) * rollingImpulse;
                    for (int k = 0; k < pointCount; ++k)
                    {
                        vt += (iA * rtA[j] * rnA[k] + iB * rtB[j] * rnB[k]) * normalImpulses[k];
                    }

                    for (int k = 0; k < j; ++k)
                    {
                        vt += (mA + mB + iA * rtA[k] * rtA[j] + iB * rtB[k] * rtB[j]) * tangentImpulses[k];
                    }

                    // clamp the accumulated force
                    float maxFriction = friction * cp.normalImpulse;
                    float impulse = b2ClampFloat(-cp.tangentMass * vt, -maxFriction - cp.tangentImpulse, maxFriction - cp.tangentImpulse);
                    cp.tangentImpulse += impulse;
                    tangentImpulses[j] = impulse;
                }

                {
                    float normalImpulse = 0.0f;
                    float tangentImpulse = 0.0f;
                    float angularA = rollingImpulse;
                    float angularB = rollingImpulse;
                    for (int j = 0; j < pointCount; ++j)
                    {
                        normalImpulse += normalImpulses[j];
                        tangentImpulse += tangentImpulses[j];
                        angularA += normalImpulses[j] * rnA[j] + tangentImpulses[j] * rtA[j];
                        angularB += normalImpulses[j] * rnB[j] + tangentImpulses[j] * rtB[j];
                    }

                    B2Vec2 P = b2Add(b2MulSV(normalImpulse, normal), b2MulSV(tangentImpulse, tangent));
                    vA = b2MulSub(vA, mA, P);
                    wA -= iA * angularA;
                    vB = b2MulAdd(vB, mB, P);
                    wB += iB * angularB;
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

        internal static void b2ApplyRestitution_Overflow(B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.overflow_resitution, "Restitution Overflow", B2HexColor.b2_colorViolet, true);

            ref B2ConstraintGraph graph = ref context.graph;
            ref B2GraphColor color = ref graph.colors[B2_OVERFLOW_INDEX];
            Span<B2ContactConstraint> constraints = color.overflowConstraints;
            int contactCount = color.contactSims.count;
            B2World world = context.world;
            B2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)B2SolverSetType.b2_awakeSet);
            B2BodyState[] states = awakeSet.bodyStates.data;

            float threshold = world.restitutionThreshold;
            float inv_h = context.inv_h;
            bool propagate = world.enableRestitutionPropagation;

            B2BodyState dummyState = B2BodyState.Create(b2_identityBodyState);

            for (int i = 0; i < contactCount; ++i)
            {
                ref B2ContactConstraint constraint = ref constraints[i];
                if (propagate == false && constraint.restitution == 0.0f)
                {
                    continue;
                }

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
                float restitution = constraint.restitution;
                int pointCount = constraint.pointCount;

                for (int j = 0; j < pointCount; ++j)
                {
                    ref B2ContactConstraintPoint cp = ref constraint.points[j];

                    B2Vec2 rA = cp.anchorA;
                    B2Vec2 rB = cp.anchorB;

                    // The total normal impulse is 0 for speculative points.
                    float compressionImpulse = cp.totalNormalImpulse - cp.restitutionImpulse;
                    bool armed = restitution > 0.0f && cp.relativeVelocity < -threshold && compressionImpulse > 0.0f;

                    float velocityBias;
                    if (armed)
                    {
                        velocityBias = restitution * cp.relativeVelocity;
                    }
                    else
                    {
                        B2Vec2 ds = b2Add(dp, b2Sub(b2RotateVector(dqB, rB), b2RotateVector(dqA, rA)));
                        float s = cp.baseSeparation + b2Dot(ds, normal);

                        velocityBias = s > 0.0f ? s * inv_h : 0.0f;
                    }

                    B2Vec2 vrA = b2Add(vA, b2CrossSV(wA, rA));
                    B2Vec2 vrB = b2Add(vB, b2CrossSV(wB, rB));
                    float vn = b2Dot(b2Sub(vrB, vrA), normal);

                    float impulse = -cp.normalMass * (vn + velocityBias);

                    float newImpulse = b2MaxFloat(cp.normalImpulse + impulse, 0.0f);
                    impulse = newImpulse - cp.normalImpulse;

                    float approachImpulse = b2MinFloat(b2MaxFloat(-cp.normalMass * vn, 0.0f), b2MaxFloat(impulse, 0.0f));

                    if (armed)
                    {
                        // Poisson kinetic restitution guarantees no energy gain.
                        float allowance = restitution * (compressionImpulse + approachImpulse) - cp.restitutionImpulse;
                        impulse = b2MinFloat(impulse, approachImpulse + b2MaxFloat(allowance, 0.0f));
                    }

                    cp.normalImpulse += impulse;
                    cp.restitutionImpulse += impulse - approachImpulse;
                    cp.totalNormalImpulse += impulse;

                    B2Vec2 P = b2MulSV(impulse, normal);
                    vA = b2MulSub(vA, mA, P);
                    wA -= iA * b2Cross(rA, P);

                    vB = b2MulAdd(vB, mB, P);
                    wB += iB * b2Cross(rB, P);
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

            b2TracyCZoneEnd(B2TracyCZone.overflow_resitution);
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


        // The Vector128 paths below are written to be bit-identical with the scalar fallback,
        // including signed zero and NaN handling, so results stay deterministic across targets.
        // Plain IEEE add/sub/mul/div are exact per lane, so they use the Vector4 operators directly.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2ZeroW()
        {
            return Vector4.Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2SplatW(float scalar)
        {
            return new Vector4(scalar);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2SetW(float a, float b, float c, float d)
        {
            return new Vector4(a, b, c, d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2NegW(in Vector4 a)
        {
#if NET8_0_OR_GREATER
            // Flip the sign bit, like scalar negation. Zero - a would turn -0 into +0.
            return Vector128.Xor(a.AsVector128(), Vector128.Create(-0.0f)).AsVector4();
#else
            return new Vector4(-a.X, -a.Y, -a.Z, -a.W);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2AddW(in Vector4 a, in Vector4 b)
        {
            return a + b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2SubW(in Vector4 a, in Vector4 b)
        {
            return a - b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2MulW(in Vector4 a, in Vector4 b)
        {
            return a * b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2DivW(in Vector4 a, in Vector4 b)
        {
            return a / b;
        }

        // Separate multiply and add (no FMA) to match the scalar rounding.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2MulAddW(in Vector4 a, in Vector4 b, in Vector4 c)
        {
            return a + b * c;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2MulSubW(in Vector4 a, in Vector4 b, in Vector4 c)
        {
            return a - b * c;
        }

        // a <= b ? a : b per lane. Vector4.Min is not used because its handling of
        // equal signed zeros and NaN differs between runtimes.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2MinW(in Vector4 a, in Vector4 b)
        {
#if NET8_0_OR_GREATER
            Vector128<float> va = a.AsVector128();
            Vector128<float> vb = b.AsVector128();
            return Vector128.ConditionalSelect(Vector128.LessThanOrEqual(va, vb), va, vb).AsVector4();
#else
            return new Vector4(
                a.X <= b.X ? a.X : b.X,
                a.Y <= b.Y ? a.Y : b.Y,
                a.Z <= b.Z ? a.Z : b.Z,
                a.W <= b.W ? a.W : b.W
            );
#endif
        }

        // a >= b ? a : b per lane
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2MaxW(in Vector4 a, in Vector4 b)
        {
#if NET8_0_OR_GREATER
            Vector128<float> va = a.AsVector128();
            Vector128<float> vb = b.AsVector128();
            return Vector128.ConditionalSelect(Vector128.GreaterThanOrEqual(va, vb), va, vb).AsVector4();
#else
            return new Vector4(
                a.X >= b.X ? a.X : b.X,
                a.Y >= b.Y ? a.Y : b.Y,
                a.Z >= b.Z ? a.Z : b.Z,
                a.W >= b.W ? a.W : b.W
            );
#endif
        }

        // a = clamp(a, -b, b)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2SymClampW(in Vector4 a, in Vector4 b)
        {
            // a = clamp(a, -b, b)
#if NET8_0_OR_GREATER
            // Same as b2ClampFloat: a < lower ? lower : (a > upper ? upper : a)
            Vector128<float> va = a.AsVector128();
            Vector128<float> upper = b.AsVector128();
            Vector128<float> lower = Vector128.Xor(upper, Vector128.Create(-0.0f));
            Vector128<float> r = Vector128.ConditionalSelect(Vector128.GreaterThan(va, upper), upper, va);
            return Vector128.ConditionalSelect(Vector128.LessThan(va, lower), lower, r).AsVector4();
#else
            return new Vector4(
                b2ClampFloat(a.X, -b.X, b.X),
                b2ClampFloat(a.Y, -b.Y, b.Y),
                b2ClampFloat(a.Z, -b.Z, b.Z),
                b2ClampFloat(a.W, -b.W, b.W)
            );
#endif
        }

        // Masks are stored as 1.0f (true) or 0.0f (false) per lane.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2AndW(in Vector4 a, in Vector4 b)
        {
#if NET8_0_OR_GREATER
            Vector128<float> zero = Vector128<float>.Zero;
            Vector128<float> anyZero = Vector128.Equals(a.AsVector128(), zero) | Vector128.Equals(b.AsVector128(), zero);
            return Vector128.AndNot(Vector128.Create(1.0f), anyZero).AsVector4();
#else
            return new Vector4(
                a.X != 0.0f && b.X != 0.0f ? 1.0f : 0.0f,
                a.Y != 0.0f && b.Y != 0.0f ? 1.0f : 0.0f,
                a.Z != 0.0f && b.Z != 0.0f ? 1.0f : 0.0f,
                a.W != 0.0f && b.W != 0.0f ? 1.0f : 0.0f
            );
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2GreaterThanW(in Vector4 a, in Vector4 b)
        {
#if NET8_0_OR_GREATER
            return (Vector128.GreaterThan(a.AsVector128(), b.AsVector128()) & Vector128.Create(1.0f)).AsVector4();
#else
            return new Vector4(
                a.X > b.X ? 1.0f : 0.0f,
                a.Y > b.Y ? 1.0f : 0.0f,
                a.Z > b.Z ? 1.0f : 0.0f,
                a.W > b.W ? 1.0f : 0.0f
            );
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2EqualsW(in Vector4 a, in Vector4 b)
        {
#if NET8_0_OR_GREATER
            return (Vector128.Equals(a.AsVector128(), b.AsVector128()) & Vector128.Create(1.0f)).AsVector4();
#else
            return new Vector4(
                a.X == b.X ? 1.0f : 0.0f,
                a.Y == b.Y ? 1.0f : 0.0f,
                a.Z == b.Z ? 1.0f : 0.0f,
                a.W == b.W ? 1.0f : 0.0f
            );
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2LessThanW(in Vector4 a, in Vector4 b)
        {
#if NET8_0_OR_GREATER
            return (Vector128.LessThan(a.AsVector128(), b.AsVector128()) & Vector128.Create(1.0f)).AsVector4();
#else
            return new Vector4(
                a.X < b.X ? 1.0f : 0.0f,
                a.Y < b.Y ? 1.0f : 0.0f,
                a.Z < b.Z ? 1.0f : 0.0f,
                a.W < b.W ? 1.0f : 0.0f
            );
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2AnyTrueW(in Vector4 mask)
        {
#if NET8_0_OR_GREATER
            return Vector128.EqualsAll(mask.AsVector128(), Vector128<float>.Zero) == false;
#else
            return mask.X != 0.0f || mask.Y != 0.0f || mask.Z != 0.0f || mask.W != 0.0f;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2AllZeroW(in Vector4 a)
        {
#if NET8_0_OR_GREATER
            return Vector128.EqualsAll(a.AsVector128(), Vector128<float>.Zero);
#else
            return a.X == 0.0f && a.Y == 0.0f && a.Z == 0.0f && a.W == 0.0f;
#endif
        }

        // component-wise returns mask ? b : a
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2BlendW(in Vector4 a, in Vector4 b, in Vector4 mask)
        {
            // component-wise returns mask ? b : a
#if NET8_0_OR_GREATER
            Vector128<float> isZero = Vector128.Equals(mask.AsVector128(), Vector128<float>.Zero);
            return Vector128.ConditionalSelect(isZero, a.AsVector128(), b.AsVector128()).AsVector4();
#else
            return new Vector4(
                mask.X != 0.0f ? b.X : a.X,
                mask.Y != 0.0f ? b.Y : a.Y,
                mask.Z != 0.0f ? b.Z : a.Z,
                mask.W != 0.0f ? b.W : a.W
            );
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2SoftMaskW(ReadOnlySpan<int> indexA, ReadOnlySpan<int> indexB)
        {
#if NET8_0_OR_GREATER
            Vector128<int> zero = Vector128<int>.Zero;
            Vector128<int> mask = Vector128.Equals(Vector128.Create(indexA), zero) | Vector128.Equals(Vector128.Create(indexB), zero);
            return (mask.AsSingle() & Vector128.Create(1.0f)).AsVector4();
#else
            return new Vector4(
                indexA[0] == 0 || indexB[0] == 0 ? 1.0f : 0.0f,
                indexA[1] == 0 || indexB[1] == 0 ? 1.0f : 0.0f,
                indexA[2] == 0 || indexB[2] == 0 ? 1.0f : 0.0f,
                indexA[3] == 0 || indexB[3] == 0 ? 1.0f : 0.0f
            );
#endif
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
        public static Vector4 b2DotW(in B2Vec2W a, in B2Vec2W b)
        {
            return b2AddW(b2MulW(a.X, b.X), b2MulW(a.Y, b.Y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2CrossW(in B2Vec2W a, in B2Vec2W b)
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
            simdBody.v.X = new Vector4(s1.linearVelocity.X, s2.linearVelocity.X, s3.linearVelocity.X, s4.linearVelocity.X);
            simdBody.v.Y = new Vector4(s1.linearVelocity.Y, s2.linearVelocity.Y, s3.linearVelocity.Y, s4.linearVelocity.Y);
            simdBody.w = new Vector4(s1.angularVelocity, s2.angularVelocity, s3.angularVelocity, s4.angularVelocity);
            simdBody.flags = new Vector4((float)s1.flags, (float)s2.flags, (float)s3.flags, (float)s4.flags);
            simdBody.dp.X = new Vector4(s1.deltaPosition.X, s2.deltaPosition.X, s3.deltaPosition.X, s4.deltaPosition.X);
            simdBody.dp.Y = new Vector4(s1.deltaPosition.Y, s2.deltaPosition.Y, s3.deltaPosition.Y, s4.deltaPosition.Y);
            simdBody.dq.C = new Vector4(s1.deltaRotation.c, s2.deltaRotation.c, s3.deltaRotation.c, s4.deltaRotation.c);
            simdBody.dq.S = new Vector4(s1.deltaRotation.s, s2.deltaRotation.s, s3.deltaRotation.s, s4.deltaRotation.s);

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
            B2BodyState[] states = context.states;
            B2ContactPrepareSpan[] spans = context.contactPrepareSpans;
            Span<B2ContactConstraintWide> wideBase = context.wideContactConstraints;

#if DEBUG
            B2Body[] bodies = world.bodies.data;
#endif

            Vector4 zeroW = b2ZeroW();
            Vector4 oneW = b2SplatW(1.0f);
            Vector4 warmStartScale = world.enableWarmStarting ? oneW : zeroW;
            bool anyRestitution = false;

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
                    uint hitEvents = 0;

                    for (int laneIndex = 0; laneIndex < B2_SIMD_WIDTH; ++laneIndex)
                    {
                        int contactIndex = B2_SIMD_WIDTH * localWideIndex + laneIndex;
                        if (contactIndex < colorContactCount)
                        {
                            B2ContactSim c = contactSims[contactIndex];

                            // index base-1
                            cw.indexA[laneIndex] = b2DecodeAwakeIndex(c.encodedBodySimA) + 1;
                            cw.indexB[laneIndex] = b2DecodeAwakeIndex(c.encodedBodySimB) + 1;
                            hitEvents |= (c.simFlags & (uint)B2ContactFlags.b2_simEnableHitEvent);

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

                    cw.restitution = b2SetW(l0.restitution, l1.restitution, l2.restitution, l3.restitution);

                    B2Vec2W tangent = b2RightPerpW(cw.normal);

                    Vector4 pointCounts = b2SetW(l0.manifold.pointCount, l1.manifold.pointCount, l2.manifold.pointCount, l3.manifold.pointCount);
                    Vector4 twoPointMask = b2GreaterThanW(pointCounts, oneW);

                    {
                        // Transpose gather of the first manifold point. Matches the upstream layout:
                        // anchorA, anchorB, separation, normalImpulse, tangentImpulse
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

                        // separation1 = cw.baseSeparation1;

                        Vector4 offset = b2DotW(b2SubVW(cw.anchorB1, cw.anchorA1), cw.normal);
                        cw.baseSeparation1 = b2SubW(cw.baseSeparation1, offset);
                        cw.normalImpulse1 = b2MulW(warmStartScale, cw.normalImpulse1);
                        cw.tangentImpulse1 = b2MulW(warmStartScale, cw.tangentImpulse1);
                        cw.totalNormalImpulse1 = zeroW;
                        cw.relativeVelocity1 = zeroW;
                        cw.restitutionImpulse1 = zeroW;

                        // Effective mass along normal.
                        {
                            Vector4 rnA = b2CrossW(cw.anchorA1, cw.normal);
                            Vector4 rnB = b2CrossW(cw.anchorB1, cw.normal);
                            Vector4 k = b2AddW(cw.invMassA, cw.invMassB);
                            k = b2MulAddW(k, cw.invIA, b2MulW(rnA, rnA));
                            k = b2MulAddW(k, cw.invIB, b2MulW(rnB, rnB));
                            Vector4 test = b2GreaterThanW(k, b2ZeroW());
                            cw.normalMass1 = b2BlendW(b2ZeroW(), b2DivW(oneW, k), test);
                        }

                        // Effective mass along tangent.
                        {
                            Vector4 rtA = b2CrossW(cw.anchorA1, tangent);
                            Vector4 rtB = b2CrossW(cw.anchorB1, tangent);
                            Vector4 k = b2AddW(cw.invMassA, cw.invMassB);
                            k = b2MulAddW(k, cw.invIA, b2MulW(rtA, rtA));
                            k = b2MulAddW(k, cw.invIB, b2MulW(rtB, rtB));
                            Vector4 test = b2GreaterThanW(k, b2ZeroW());
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

                        // separation2 = cw.baseSeparation2;

                        Vector4 offset = b2DotW(b2SubVW(cw.anchorB2, cw.anchorA2), cw.normal);
                        cw.baseSeparation2 = b2SubW(cw.baseSeparation2, offset);
                        cw.normalImpulse2 = b2MulW(warmStartScale, cw.normalImpulse2);
                        cw.tangentImpulse2 = b2MulW(warmStartScale, cw.tangentImpulse2);
                        cw.totalNormalImpulse2 = zeroW;
                        cw.relativeVelocity2 = zeroW;
                        cw.restitutionImpulse2 = zeroW;

                        // Effective mass along normal.
                        {
                            Vector4 rnA = b2CrossW(cw.anchorA2, cw.normal);
                            Vector4 rnB = b2CrossW(cw.anchorB2, cw.normal);
                            Vector4 k = b2AddW(cw.invMassA, cw.invMassB);
                            k = b2MulAddW(k, cw.invIA, b2MulW(rnA, rnA));
                            k = b2MulAddW(k, cw.invIB, b2MulW(rnB, rnB));
                            Vector4 test = b2GreaterThanW(k, b2ZeroW());
                            cw.normalMass2 = b2BlendW(b2ZeroW(), b2DivW(oneW, k), test);
                        }

                        // Effective mass along tangent.
                        {
                            Vector4 rtA = b2CrossW(cw.anchorA2, tangent);
                            Vector4 rtB = b2CrossW(cw.anchorB2, tangent);
                            Vector4 k = b2AddW(cw.invMassA, cw.invMassB);
                            k = b2MulAddW(k, cw.invIA, b2MulW(rtA, rtA));
                            k = b2MulAddW(k, cw.invIB, b2MulW(rtB, rtB));
                            Vector4 test = b2GreaterThanW(k, b2ZeroW());
                            cw.tangentMass2 = b2BlendW(b2ZeroW(), b2DivW(oneW, k), test);
                        }

                        // Zero effective mass on lanes that only have one point.
                        cw.normalMass2 = b2BlendW(zeroW, cw.normalMass2, twoPointMask);
                        cw.tangentMass2 = b2BlendW(zeroW, cw.tangentMass2, twoPointMask);
                    }

                    // Only sample contact point normal velocity if needed.
                    Vector4 restitutionMask = b2GreaterThanW(cw.restitution, zeroW);
                    bool haveRestitution = b2AnyTrueW(restitutionMask);
                    anyRestitution = anyRestitution || haveRestitution;

                    if ((hitEvents & (uint)B2ContactFlags.b2_simEnableHitEvent) != 0 || haveRestitution)
                    {
                        B2BodyStateW bA = b2GatherBodies(states, cw.indexA.AsSpan());
                        B2BodyStateW bB = b2GatherBodies(states, cw.indexB.AsSpan());

                        {
                            B2Vec2W rA = cw.anchorA1;
                            B2Vec2W rB = cw.anchorB1;

                            // Relative velocity at contact
                            Vector4 dvx = b2SubW(b2SubW(bB.v.X, b2MulW(bB.w, rB.Y)), b2SubW(bA.v.X, b2MulW(bA.w, rA.Y)));
                            Vector4 dvy = b2SubW(b2AddW(bB.v.Y, b2MulW(bB.w, rB.X)), b2AddW(bA.v.Y, b2MulW(bA.w, rA.X)));
                            Vector4 vn = b2AddW(b2MulW(dvx, cw.normal.X), b2MulW(dvy, cw.normal.Y));
                            cw.relativeVelocity1 = vn;

                            // Check the point count to avoid a race writing to b2_zeroContactSim.
                            if (l0.manifold.pointCount > 0) l0.manifold.points[0].normalVelocity = vn.X;
                            if (l1.manifold.pointCount > 0) l1.manifold.points[0].normalVelocity = vn.Y;
                            if (l2.manifold.pointCount > 0) l2.manifold.points[0].normalVelocity = vn.Z;
                            if (l3.manifold.pointCount > 0) l3.manifold.points[0].normalVelocity = vn.W;
                        }

                        {
                            B2Vec2W rA = cw.anchorA2;
                            B2Vec2W rB = cw.anchorB2;

                            // Relative velocity at contact
                            Vector4 dvx = b2SubW(b2SubW(bB.v.X, b2MulW(bB.w, rB.Y)), b2SubW(bA.v.X, b2MulW(bA.w, rA.Y)));
                            Vector4 dvy = b2SubW(b2AddW(bB.v.Y, b2MulW(bB.w, rB.X)), b2AddW(bA.v.Y, b2MulW(bA.w, rA.X)));
                            Vector4 vn = b2AddW(b2MulW(dvx, cw.normal.X), b2MulW(dvy, cw.normal.Y));
                            cw.relativeVelocity2 = vn;

                            if (l0.manifold.pointCount > 1) l0.manifold.points[1].normalVelocity = vn.X;
                            if (l1.manifold.pointCount > 1) l1.manifold.points[1].normalVelocity = vn.Y;
                            if (l2.manifold.pointCount > 1) l2.manifold.points[1].normalVelocity = vn.Z;
                            if (l3.manifold.pointCount > 1) l3.manifold.points[1].normalVelocity = vn.W;
                        }
                    }
                }

                // Advance to next color
                colorIndex += 1;
            }

            if (anyRestitution)
            {
                b2AtomicStoreInt(ref context.anyRestitution, 1);
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

                Vector4 tangentX = c.normal.Y;
                Vector4 tangentY = b2SubW(b2ZeroW(), c.normal.X);

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
            Vector4 inv_h = b2SplatW(context.inv_h);
            Vector4 contactSpeed = b2SplatW(-context.world.contactSpeed);
            Vector4 oneW = b2SplatW(1.0f);
            Vector4 zeroW = b2ZeroW();

            Vector4 dynamicBiasRate = b2SplatW(context.contactSoftness.massScale * context.contactSoftness.biasRate);
            Vector4 dynamicMassScale = b2SplatW(context.contactSoftness.massScale);
            Vector4 dynamicImpulseScale = b2SplatW(context.contactSoftness.impulseScale);
            Vector4 staticBiasRate = b2SplatW(context.staticSoftness.massScale * context.staticSoftness.biasRate);
            Vector4 staticMassScale = b2SplatW(context.staticSoftness.massScale);
            Vector4 staticImpulseScale = b2SplatW(context.staticSoftness.impulseScale);

            for (int wideIndex = block.startIndex; wideIndex < block.startIndex + block.count; ++wideIndex)
            {
                ref B2ContactConstraintWide c = ref constraints[wideIndex];

                B2BodyStateW bA = b2GatherBodies(states, c.indexA.AsSpan());
                B2BodyStateW bB = b2GatherBodies(states, c.indexB.AsSpan());

                Vector4 softMask = b2SoftMaskW(c.indexA.AsSpan(), c.indexB.AsSpan());
                Vector4 biasRate = b2BlendW(dynamicBiasRate, staticBiasRate, softMask);
                Vector4 massScale = b2BlendW(dynamicMassScale, staticMassScale, softMask);
                Vector4 impulseScale = b2BlendW(dynamicImpulseScale, staticImpulseScale, softMask);

                B2Vec2W normal = c.normal;
                B2Vec2W dp = new B2Vec2W(b2SubW(bB.dp.X, bA.dp.X), b2SubW(bB.dp.Y, bA.dp.Y));

                Vector4 velocityBias1, massScale1, impulseScale1;
                {
                    // Moving anchors for current separation
                    B2Vec2W rsA = b2RotateVectorW(bA.dq, c.anchorA1);
                    B2Vec2W rsB = b2RotateVectorW(bB.dq, c.anchorB1);

                    // compute current separation
                    // this is subject to round-off error if the anchor is far from the body center of mass
                    B2Vec2W ds = new B2Vec2W(b2AddW(dp.X, b2SubW(rsB.X, rsA.X)), b2AddW(dp.Y, b2SubW(rsB.Y, rsA.Y)));
                    Vector4 s = b2AddW(b2DotW(normal, ds), c.baseSeparation1);

                    // Apply speculative bias if separation is greater than zero, otherwise apply soft constraint bias
                    // The contactSpeed is meant to limit stiffness, not increase it.
                    Vector4 separated = b2GreaterThanW(s, zeroW);

                    // Speculative bias - positive
                    Vector4 specBias = b2MulW(s, inv_h);

                    // Overlap bias - negative when overlapped
                    Vector4 overlapBias = b2MaxW(b2MulW(biasRate, s), contactSpeed);

                    // velocity bias = s > 0 ? spec : overlap
                    velocityBias1 = b2BlendW(overlapBias, specBias, separated);

                    massScale1 = b2BlendW(massScale, oneW, separated);
                    impulseScale1 = b2BlendW(impulseScale, zeroW, separated);
                }

                Vector4 velocityBias2, massScale2, impulseScale2;
                {
                    B2Vec2W rsA = b2RotateVectorW(bA.dq, c.anchorA2);
                    B2Vec2W rsB = b2RotateVectorW(bB.dq, c.anchorB2);

                    B2Vec2W ds = new B2Vec2W(b2AddW(dp.X, b2SubW(rsB.X, rsA.X)), b2AddW(dp.Y, b2SubW(rsB.Y, rsA.Y)));
                    Vector4 s = b2AddW(b2DotW(normal, ds), c.baseSeparation2);

                    Vector4 separated = b2GreaterThanW(s, zeroW);
                    Vector4 specBias = b2MulW(s, inv_h);
                    Vector4 overlapBias = b2MaxW(b2MulW(biasRate, s), contactSpeed);
                    velocityBias2 = b2BlendW(overlapBias, specBias, separated);

                    massScale2 = b2BlendW(massScale, oneW, separated);
                    impulseScale2 = b2BlendW(impulseScale, zeroW, separated);
                }

                Vector4 rnA1 = b2CrossW(c.anchorA1, normal);
                Vector4 rnB1 = b2CrossW(c.anchorB1, normal);
                Vector4 rnA2 = b2CrossW(c.anchorA2, normal);
                Vector4 rnB2 = b2CrossW(c.anchorB2, normal);

                // Coupling of point 2 normal velocity to point 1 normal impulse
                Vector4 k21 = b2AddW(b2AddW(c.invMassA, c.invMassB),
                    b2AddW(b2MulW(b2MulW(c.invIA, rnA1), rnA2), b2MulW(b2MulW(c.invIB, rnB1), rnB2)));

                Vector4 dn = b2DotW(b2SubVW(bB.v, bA.v), normal);
                Vector4 vn1 = b2AddW(dn, b2SubW(b2MulW(bB.w, rnB1), b2MulW(bA.w, rnA1)));
                Vector4 vn2 = b2AddW(dn, b2SubW(b2MulW(bB.w, rnB2), b2MulW(bA.w, rnA2)));

                Vector4 negImpulse1 = b2AddW(b2MulW(c.normalMass1, b2AddW(b2MulW(massScale1, vn1), velocityBias1)),
                    b2MulW(impulseScale1, c.normalImpulse1));
                Vector4 impulse1 = b2MaxW(b2NegW(negImpulse1), b2NegW(c.normalImpulse1));
                c.normalImpulse1 = b2AddW(c.normalImpulse1, impulse1);
                c.totalNormalImpulse1 = b2AddW(c.totalNormalImpulse1, impulse1);

                vn2 = b2MulAddW(vn2, k21, impulse1);

                Vector4 negImpulse2 = b2AddW(b2MulW(c.normalMass2, b2AddW(b2MulW(massScale2, vn2), velocityBias2)),
                    b2MulW(impulseScale2, c.normalImpulse2));
                Vector4 impulse2 = b2MaxW(b2NegW(negImpulse2), b2NegW(c.normalImpulse2));
                c.normalImpulse2 = b2AddW(c.normalImpulse2, impulse2);
                c.totalNormalImpulse2 = b2AddW(c.totalNormalImpulse2, impulse2);

                Vector4 impulse = b2AddW(impulse1, impulse2);
                Vector4 Px = b2MulW(impulse, normal.X);
                Vector4 Py = b2MulW(impulse, normal.Y);
                Vector4 angularA = b2AddW(b2MulW(impulse1, rnA1), b2MulW(impulse2, rnA2));
                Vector4 angularB = b2AddW(b2MulW(impulse1, rnB1), b2MulW(impulse2, rnB2));

                bA.v.X = b2MulSubW(bA.v.X, c.invMassA, Px);
                bA.v.Y = b2MulSubW(bA.v.Y, c.invMassA, Py);
                bA.w = b2MulSubW(bA.w, c.invIA, angularA);

                bB.v.X = b2MulAddW(bB.v.X, c.invMassB, Px);
                bB.v.Y = b2MulAddW(bB.v.Y, c.invMassB, Py);
                bB.w = b2MulAddW(bB.w, c.invIB, angularB);

                b2ScatterBodies(states, c.indexA.AsSpan(), ref bA);
                b2ScatterBodies(states, c.indexB.AsSpan(), ref bB);
            }

            b2TracyCZoneEnd(B2TracyCZone.solve_contact);
        }

        // Solve the normal constraint, friction, and rolling resistance.
        internal static void b2SolveContacts_Wide(B2SolverBlock block, B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.solve_contact, "Solve Contact", B2HexColor.b2_colorAliceBlue, true);

            B2BodyState[] states = context.states;
            int colorIndex = block.colorIndex;
            Span<B2ContactConstraintWide> constraints = context.graph.colors[colorIndex].wideConstraints;
            Vector4 inv_h = b2SplatW(context.inv_h);
            Vector4 zeroW = b2ZeroW();

            for (int wideIndex = block.startIndex; wideIndex < block.startIndex + block.count; ++wideIndex)
            {
                ref B2ContactConstraintWide c = ref constraints[wideIndex];

                B2BodyStateW bA = b2GatherBodies(states, c.indexA.AsSpan());
                B2BodyStateW bB = b2GatherBodies(states, c.indexB.AsSpan());

                B2Vec2W normal = c.normal;
                B2Vec2W tangent = b2RightPerpW(normal);
                B2Vec2W dp = new B2Vec2W(b2SubW(bB.dp.X, bA.dp.X), b2SubW(bB.dp.Y, bA.dp.Y));

                Vector4 velocityBias1, velocityBias2;
                {
                    // Moving anchors for current separation
                    B2Vec2W rsA = b2RotateVectorW(bA.dq, c.anchorA1);
                    B2Vec2W rsB = b2RotateVectorW(bB.dq, c.anchorB1);

                    // compute current separation
                    // this is subject to round-off error if the anchor is far from the body center of mass
                    B2Vec2W ds = new B2Vec2W(b2AddW(dp.X, b2SubW(rsB.X, rsA.X)), b2AddW(dp.Y, b2SubW(rsB.Y, rsA.Y)));
                    Vector4 s = b2AddW(b2DotW(normal, ds), c.baseSeparation1);

                    // Speculative bias, positive if separated and zero if overlapped
                    velocityBias1 = b2MaxW(zeroW, b2MulW(s, inv_h));
                }

                {
                    B2Vec2W rsA = b2RotateVectorW(bA.dq, c.anchorA2);
                    B2Vec2W rsB = b2RotateVectorW(bB.dq, c.anchorB2);

                    B2Vec2W ds = new B2Vec2W(b2AddW(dp.X, b2SubW(rsB.X, rsA.X)), b2AddW(dp.Y, b2SubW(rsB.Y, rsA.Y)));
                    Vector4 s = b2AddW(b2DotW(normal, ds), c.baseSeparation2);

                    velocityBias2 = b2MaxW(zeroW, b2MulW(s, inv_h));
                }

                Vector4 rnA1 = b2CrossW(c.anchorA1, normal);
                Vector4 rnB1 = b2CrossW(c.anchorB1, normal);
                Vector4 rnA2 = b2CrossW(c.anchorA2, normal);
                Vector4 rnB2 = b2CrossW(c.anchorB2, normal);
                Vector4 rtA1 = b2CrossW(c.anchorA1, tangent);
                Vector4 rtB1 = b2CrossW(c.anchorB1, tangent);
                Vector4 rtA2 = b2CrossW(c.anchorA2, tangent);
                Vector4 rtB2 = b2CrossW(c.anchorB2, tangent);

                Vector4 wnA1 = b2MulW(c.invIA, rnA1);
                Vector4 wnB1 = b2MulW(c.invIB, rnB1);
                Vector4 wnA2 = b2MulW(c.invIA, rnA2);
                Vector4 wnB2 = b2MulW(c.invIB, rnB2);
                Vector4 wtA1 = b2MulW(c.invIA, rtA1);
                Vector4 wtB1 = b2MulW(c.invIB, rtB1);

                // Couplings between the sequential impulses: rows are (n1, n2, t1, t2)
                Vector4 invMass = b2AddW(c.invMassA, c.invMassB);
                Vector4 k21 = b2AddW(invMass, b2AddW(b2MulW(wnA1, rnA2), b2MulW(wnB1, rnB2)));
                Vector4 k31 = b2AddW(b2MulW(wnA1, rtA1), b2MulW(wnB1, rtB1));
                Vector4 k32 = b2AddW(b2MulW(wnA2, rtA1), b2MulW(wnB2, rtB1));
                Vector4 k41 = b2AddW(b2MulW(wnA1, rtA2), b2MulW(wnB1, rtB2));
                Vector4 k42 = b2AddW(b2MulW(wnA2, rtA2), b2MulW(wnB2, rtB2));
                Vector4 k43 = b2AddW(invMass, b2AddW(b2MulW(wtA1, rtA2), b2MulW(wtB1, rtB2)));

                B2Vec2W dv = b2SubVW(bB.v, bA.v);
                Vector4 dn = b2DotW(dv, normal);
                Vector4 dt = b2SubW(b2DotW(dv, tangent), c.tangentSpeed);

                Vector4 vn1 = b2AddW(dn, b2SubW(b2MulW(bB.w, rnB1), b2MulW(bA.w, rnA1)));
                Vector4 vn2 = b2AddW(dn, b2SubW(b2MulW(bB.w, rnB2), b2MulW(bA.w, rnA2)));
                Vector4 vt1 = b2AddW(dt, b2SubW(b2MulW(bB.w, rtB1), b2MulW(bA.w, rtA1)));
                Vector4 vt2 = b2AddW(dt, b2SubW(b2MulW(bB.w, rtB2), b2MulW(bA.w, rtA2)));

                Vector4 normalImpulse1 = b2MaxW(b2NegW(b2MulW(c.normalMass1, b2AddW(vn1, velocityBias1))),
                    b2NegW(c.normalImpulse1));
                c.normalImpulse1 = b2AddW(c.normalImpulse1, normalImpulse1);
                c.totalNormalImpulse1 = b2AddW(c.totalNormalImpulse1, normalImpulse1);

                vn2 = b2MulAddW(vn2, k21, normalImpulse1);

                Vector4 normalImpulse2 = b2MaxW(b2NegW(b2MulW(c.normalMass2, b2AddW(vn2, velocityBias2))),
                    b2NegW(c.normalImpulse2));
                c.normalImpulse2 = b2AddW(c.normalImpulse2, normalImpulse2);
                c.totalNormalImpulse2 = b2AddW(c.totalNormalImpulse2, normalImpulse2);

                vt1 = b2MulAddW(b2MulAddW(vt1, k31, normalImpulse1), k32, normalImpulse2);
                vt2 = b2MulAddW(b2MulAddW(vt2, k41, normalImpulse1), k42, normalImpulse2);

                Vector4 angularA = b2AddW(b2MulW(normalImpulse1, rnA1), b2MulW(normalImpulse2, rnA2));
                Vector4 angularB = b2AddW(b2MulW(normalImpulse1, rnB1), b2MulW(normalImpulse2, rnB2));

                // Rolling resistance
                if (b2AllZeroW(c.rollingResistance) == false)
                {
                    Vector4 relativeW = b2SubW(b2SubW(bA.w, bB.w), b2AddW(b2MulW(b2AddW(wnA1, wnB1), normalImpulse1),
                        b2MulW(b2AddW(wnA2, wnB2), normalImpulse2)));
                    Vector4 k = b2AddW(c.invIA, c.invIB);
                    Vector4 mask = b2GreaterThanW(k, zeroW);
                    Vector4 deltaLambda = b2BlendW(zeroW, b2DivW(relativeW, k), mask);
                    Vector4 lambda = c.rollingImpulse;
                    Vector4 maxLambda = b2MulW(c.rollingResistance, b2AddW(c.normalImpulse1, c.normalImpulse2));
                    c.rollingImpulse = b2SymClampW(b2AddW(lambda, deltaLambda), maxLambda);
                    deltaLambda = b2SubW(c.rollingImpulse, lambda);

                    Vector4 wtA2 = b2MulW(c.invIA, rtA2);
                    Vector4 wtB2 = b2MulW(c.invIB, rtB2);
                    vt1 = b2MulAddW(vt1, b2AddW(wtA1, wtB1), deltaLambda);
                    vt2 = b2MulAddW(vt2, b2AddW(wtA2, wtB2), deltaLambda);

                    angularA = b2AddW(angularA, deltaLambda);
                    angularB = b2AddW(angularB, deltaLambda);
                }

                Vector4 maxFriction1 = b2MulW(c.friction, c.normalImpulse1);
                Vector4 tangentImpulse1 = b2NegW(b2MulW(c.tangentMass1, vt1));
                tangentImpulse1 = b2MaxW(b2SubW(b2NegW(maxFriction1), c.tangentImpulse1),
                    b2MinW(tangentImpulse1, b2SubW(maxFriction1, c.tangentImpulse1)));
                c.tangentImpulse1 = b2AddW(c.tangentImpulse1, tangentImpulse1);

                vt2 = b2MulAddW(vt2, k43, tangentImpulse1);

                Vector4 maxFriction2 = b2MulW(c.friction, c.normalImpulse2);
                Vector4 tangentImpulse2 = b2NegW(b2MulW(c.tangentMass2, vt2));
                tangentImpulse2 = b2MaxW(b2SubW(b2NegW(maxFriction2), c.tangentImpulse2),
                    b2MinW(tangentImpulse2, b2SubW(maxFriction2, c.tangentImpulse2)));
                c.tangentImpulse2 = b2AddW(c.tangentImpulse2, tangentImpulse2);

                angularA = b2AddW(angularA, b2AddW(b2MulW(tangentImpulse1, rtA1), b2MulW(tangentImpulse2, rtA2)));
                angularB = b2AddW(angularB, b2AddW(b2MulW(tangentImpulse1, rtB1), b2MulW(tangentImpulse2, rtB2)));

                Vector4 normalImpulse = b2AddW(normalImpulse1, normalImpulse2);
                Vector4 tangentImpulse = b2AddW(tangentImpulse1, tangentImpulse2);
                Vector4 Px = b2AddW(b2MulW(normalImpulse, normal.X), b2MulW(tangentImpulse, tangent.X));
                Vector4 Py = b2AddW(b2MulW(normalImpulse, normal.Y), b2MulW(tangentImpulse, tangent.Y));

                bA.v.X = b2MulSubW(bA.v.X, c.invMassA, Px);
                bA.v.Y = b2MulSubW(bA.v.Y, c.invMassA, Py);
                bA.w = b2MulSubW(bA.w, c.invIA, angularA);

                bB.v.X = b2MulAddW(bB.v.X, c.invMassB, Px);
                bB.v.Y = b2MulAddW(bB.v.Y, c.invMassB, Py);
                bB.w = b2MulAddW(bB.w, c.invIB, angularB);

                b2ScatterBodies(states, c.indexA.AsSpan(), ref bA);
                b2ScatterBodies(states, c.indexB.AsSpan(), ref bB);
            }

            b2TracyCZoneEnd(B2TracyCZone.solve_contact);
        }

        internal static void b2ApplyRestitution_Wide(B2SolverBlock block, B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.restitution, "Restitution", B2HexColor.b2_colorDodgerBlue, true);

            B2BodyState[] states = context.states;
            Span<B2ContactConstraintWide> constraints = context.graph.colors[block.colorIndex].wideConstraints;
            Vector4 inv_h = b2SplatW(context.inv_h);
            Vector4 negRestitutionThreshold = b2SplatW(-context.world.restitutionThreshold);
            Vector4 zeroW = b2ZeroW();
            bool propagate = context.world.enableRestitutionPropagation;

            for (int wideIndex = block.startIndex; wideIndex < block.startIndex + block.count; ++wideIndex)
            {
                ref B2ContactConstraintWide c = ref constraints[wideIndex];
                if (propagate == false && b2AllZeroW(c.restitution))
                {
                    continue;
                }

                B2BodyStateW bA = b2GatherBodies(states, c.indexA.AsSpan());
                B2BodyStateW bB = b2GatherBodies(states, c.indexB.AsSpan());

                Vector4 restitutionMask = b2GreaterThanW(c.restitution, zeroW);
                Vector4 normalMass1 = propagate ? c.normalMass1 : b2BlendW(zeroW, c.normalMass1, restitutionMask);
                Vector4 normalMass2 = propagate ? c.normalMass2 : b2BlendW(zeroW, c.normalMass2, restitutionMask);
                B2Vec2W dp = new B2Vec2W(b2SubW(bB.dp.X, bA.dp.X), b2SubW(bB.dp.Y, bA.dp.Y));

                {
                    B2Vec2W rA = c.anchorA1;
                    B2Vec2W rB = c.anchorB1;

                    Vector4 compressionImpulse = b2SubW(c.totalNormalImpulse1, c.restitutionImpulse1);
                    Vector4 armed = b2AndW(b2AndW(restitutionMask, b2LessThanW(c.relativeVelocity1, negRestitutionThreshold)),
                        b2GreaterThanW(compressionImpulse, zeroW));

                    B2Vec2W rsA = b2RotateVectorW(bA.dq, rA);
                    B2Vec2W rsB = b2RotateVectorW(bB.dq, rB);
                    B2Vec2W ds = new B2Vec2W(b2AddW(dp.X, b2SubW(rsB.X, rsA.X)), b2AddW(dp.Y, b2SubW(rsB.Y, rsA.Y)));
                    Vector4 s = b2AddW(b2DotW(c.normal, ds), c.baseSeparation1);

                    Vector4 specBias = b2MaxW(zeroW, b2MulW(s, inv_h));
                    Vector4 velocityBias = b2BlendW(specBias, b2MulW(c.restitution, c.relativeVelocity1), armed);

                    Vector4 dvx = b2SubW(b2SubW(bB.v.X, b2MulW(bB.w, rB.Y)), b2SubW(bA.v.X, b2MulW(bA.w, rA.Y)));
                    Vector4 dvy = b2SubW(b2AddW(bB.v.Y, b2MulW(bB.w, rB.X)), b2AddW(bA.v.Y, b2MulW(bA.w, rA.X)));
                    Vector4 vn = b2AddW(b2MulW(dvx, c.normal.X), b2MulW(dvy, c.normal.Y));

                    Vector4 negImpulse = b2MulW(normalMass1, b2AddW(vn, velocityBias));

                    Vector4 newImpulse = b2MaxW(b2SubW(c.normalImpulse1, negImpulse), zeroW);
                    Vector4 impulse = b2SubW(newImpulse, c.normalImpulse1);

                    Vector4 approachImpulse = b2MinW(b2MaxW(b2NegW(b2MulW(normalMass1, vn)), zeroW), b2MaxW(impulse, zeroW));
                    Vector4 allowance =
                        b2SubW(b2MulW(c.restitution, b2AddW(compressionImpulse, approachImpulse)), c.restitutionImpulse1);
                    Vector4 maxImpulse = b2AddW(approachImpulse, b2MaxW(allowance, zeroW));
                    impulse = b2BlendW(impulse, b2MinW(impulse, maxImpulse), armed);

                    c.normalImpulse1 = b2AddW(c.normalImpulse1, impulse);
                    c.restitutionImpulse1 = b2AddW(c.restitutionImpulse1, b2SubW(impulse, approachImpulse));
                    c.totalNormalImpulse1 = b2AddW(c.totalNormalImpulse1, impulse);

                    Vector4 Px = b2MulW(impulse, c.normal.X);
                    Vector4 Py = b2MulW(impulse, c.normal.Y);

                    bA.v.X = b2MulSubW(bA.v.X, c.invMassA, Px);
                    bA.v.Y = b2MulSubW(bA.v.Y, c.invMassA, Py);
                    bA.w = b2MulSubW(bA.w, c.invIA, b2SubW(b2MulW(rA.X, Py), b2MulW(rA.Y, Px)));

                    bB.v.X = b2MulAddW(bB.v.X, c.invMassB, Px);
                    bB.v.Y = b2MulAddW(bB.v.Y, c.invMassB, Py);
                    bB.w = b2MulAddW(bB.w, c.invIB, b2SubW(b2MulW(rB.X, Py), b2MulW(rB.Y, Px)));
                }

                {
                    B2Vec2W rA = c.anchorA2;
                    B2Vec2W rB = c.anchorB2;

                    Vector4 compressionImpulse = b2SubW(c.totalNormalImpulse2, c.restitutionImpulse2);
                    Vector4 armed = b2AndW(b2AndW(restitutionMask, b2LessThanW(c.relativeVelocity2, negRestitutionThreshold)),
                        b2GreaterThanW(compressionImpulse, zeroW));

                    B2Vec2W rsA = b2RotateVectorW(bA.dq, rA);
                    B2Vec2W rsB = b2RotateVectorW(bB.dq, rB);
                    B2Vec2W ds = new B2Vec2W(b2AddW(dp.X, b2SubW(rsB.X, rsA.X)), b2AddW(dp.Y, b2SubW(rsB.Y, rsA.Y)));
                    Vector4 s = b2AddW(b2DotW(c.normal, ds), c.baseSeparation2);

                    Vector4 specBias = b2MaxW(zeroW, b2MulW(s, inv_h));
                    Vector4 velocityBias = b2BlendW(specBias, b2MulW(c.restitution, c.relativeVelocity2), armed);

                    Vector4 dvx = b2SubW(b2SubW(bB.v.X, b2MulW(bB.w, rB.Y)), b2SubW(bA.v.X, b2MulW(bA.w, rA.Y)));
                    Vector4 dvy = b2SubW(b2AddW(bB.v.Y, b2MulW(bB.w, rB.X)), b2AddW(bA.v.Y, b2MulW(bA.w, rA.X)));
                    Vector4 vn = b2AddW(b2MulW(dvx, c.normal.X), b2MulW(dvy, c.normal.Y));

                    Vector4 negImpulse = b2MulW(normalMass2, b2AddW(vn, velocityBias));

                    Vector4 newImpulse = b2MaxW(b2SubW(c.normalImpulse2, negImpulse), zeroW);
                    Vector4 impulse = b2SubW(newImpulse, c.normalImpulse2);

                    Vector4 approachImpulse = b2MinW(b2MaxW(b2NegW(b2MulW(normalMass2, vn)), zeroW), b2MaxW(impulse, zeroW));
                    Vector4 allowance =
                        b2SubW(b2MulW(c.restitution, b2AddW(compressionImpulse, approachImpulse)), c.restitutionImpulse2);
                    Vector4 maxImpulse = b2AddW(approachImpulse, b2MaxW(allowance, zeroW));
                    impulse = b2BlendW(impulse, b2MinW(impulse, maxImpulse), armed);

                    c.normalImpulse2 = b2AddW(c.normalImpulse2, impulse);
                    c.restitutionImpulse2 = b2AddW(c.restitutionImpulse2, b2SubW(impulse, approachImpulse));
                    c.totalNormalImpulse2 = b2AddW(c.totalNormalImpulse2, impulse);

                    Vector4 Px = b2MulW(impulse, c.normal.X);
                    Vector4 Py = b2MulW(impulse, c.normal.Y);

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
                    ref readonly Vector4 rollingImpulse = ref c.rollingImpulse;
                    ref readonly Vector4 normalImpulse1 = ref c.normalImpulse1;
                    ref readonly Vector4 normalImpulse2 = ref c.normalImpulse2;
                    ref readonly Vector4 tangentImpulse1 = ref c.tangentImpulse1;
                    ref readonly Vector4 tangentImpulse2 = ref c.tangentImpulse2;
                    ref readonly Vector4 totalNormalImpulse1 = ref c.totalNormalImpulse1;
                    ref readonly Vector4 totalNormalImpulse2 = ref c.totalNormalImpulse2;

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
                        m.rollingImpulse = rollingImpulse.GetAt(laneIndex);

                        m.points[0].normalImpulse = normalImpulse1.GetAt(laneIndex);
                        m.points[0].tangentImpulse = tangentImpulse1.GetAt(laneIndex);
                        m.points[0].totalNormalImpulse = totalNormalImpulse1.GetAt(laneIndex);

                        m.points[1].normalImpulse = normalImpulse2.GetAt(laneIndex);
                        m.points[1].tangentImpulse = tangentImpulse2.GetAt(laneIndex);
                        m.points[1].totalNormalImpulse = totalNormalImpulse2.GetAt(laneIndex);

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
