// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Solvers;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Geometries;

namespace Box2D.NET
{
    public static class B2WeldJoints
    {
#if B2_WELD_BLOCK_SOLVE
        public struct B2Vec3
        {
            public float X;
            public float Y;
            public float Z;

            public B2Vec3(float x, float y, float z)
            {
                X = x;
                Y = y;
                Z = z;
            }
        }

        // A 3-by-3 matrix. Stored in column-major order.
        public struct B2Mat33
        {
            public B2Vec3 cx;
            public B2Vec3 cy;
            public B2Vec3 cz;

            public B2Mat33(B2Vec3 cx, B2Vec3 cy, B2Vec3 cz)
            {
                this.cx = cx;
                this.cy = cy;
                this.cz = cz;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float b2Dot3(B2Vec3 a, B2Vec3 b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static B2Vec3 b2Cross3(B2Vec3 a, B2Vec3 b)
        {
            return new B2Vec3()
            {
                X = a.Y * b.Z - a.Z * b.Y,
                Y = a.Z * b.X - a.X * b.Z,
                Z = a.X * b.Y - a.Y * b.X,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static B2Vec3 b2Solve33(ref B2Mat33 m, B2Vec3 b)
        {
            float det = b2Dot3(m.cx, b2Cross3(m.cy, m.cz));
            if (det != 0.0f)
            {
                det = 1.0f / det;
            }

            B2Vec3 x;
            x.X = det * b2Dot3(b, b2Cross3(m.cy, m.cz));
            x.Y = det * b2Dot3(m.cx, b2Cross3(b, m.cz));
            x.Z = det * b2Dot3(m.cx, b2Cross3(m.cy, b));
            return x;
        }
#endif

        public static void b2WeldJoint_SetLinearHertz(B2JointId jointId, float hertz)
        {
            B2_ASSERT(b2IsValidFloat(hertz) && hertz >= 0.0f);
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_weldJoint);
            joint.uj.weldJoint.linearHertz = hertz;
        }

        public static float b2WeldJoint_GetLinearHertz(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_weldJoint);
            return joint.uj.weldJoint.linearHertz;
        }

        public static void b2WeldJoint_SetLinearDampingRatio(B2JointId jointId, float dampingRatio)
        {
            B2_ASSERT(b2IsValidFloat(dampingRatio) && dampingRatio >= 0.0f);
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_weldJoint);
            joint.uj.weldJoint.linearDampingRatio = dampingRatio;
        }

        public static float b2WeldJoint_GetLinearDampingRatio(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_weldJoint);
            return joint.uj.weldJoint.linearDampingRatio;
        }

        public static void b2WeldJoint_SetAngularHertz(B2JointId jointId, float hertz)
        {
            B2_ASSERT(b2IsValidFloat(hertz) && hertz >= 0.0f);
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_weldJoint);
            joint.uj.weldJoint.angularHertz = hertz;
        }

        public static float b2WeldJoint_GetAngularHertz(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_weldJoint);
            return joint.uj.weldJoint.angularHertz;
        }

        public static void b2WeldJoint_SetAngularDampingRatio(B2JointId jointId, float dampingRatio)
        {
            B2_ASSERT(b2IsValidFloat(dampingRatio) && dampingRatio >= 0.0f);
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_weldJoint);
            joint.uj.weldJoint.angularDampingRatio = dampingRatio;
        }

        public static float b2WeldJoint_GetAngularDampingRatio(B2JointId jointId)
        {
            B2JointSim joint = b2GetJointSimCheckType(jointId, B2JointType.b2_weldJoint);
            return joint.uj.weldJoint.angularDampingRatio;
        }

        internal static B2Vec2 b2GetWeldJointForce(B2World world, B2JointSim @base)
        {
            B2Vec2 force = b2MulSV(world.inv_h, @base.uj.weldJoint.linearImpulse);
            return force;
        }

        internal static float b2GetWeldJointTorque(B2World world, B2JointSim @base)
        {
            return world.inv_h * @base.uj.weldJoint.angularImpulse;
        }

        // Point-to-point constraint
        // C = p2 - p1
        // Cdot = v2 - v1
        //      = v2 + cross(w2, r2) - v1 - cross(w1, r1)
        // J = [-E -r1_skew E r2_skew ]
        // Identity used:
        // w k % (rx i + ry j) = w * (-ry i + rx j)

        // Angle constraint
        // C = angle2 - angle1 - referenceAngle
        // Cdot = w2 - w1
        // J = [0 0 -1 0 0 1]
        // K = invI1 + invI2

        // 3x3 Block
        // K = [J1] * invM * [J1T J2T]
        //     [J2]
        //   = [J1] * [invM * J1T invM * J2T]
        //     [J2]
        //   = [J1 * invM * J1T J1 * invM * J2T]
        //     [J2 * invM * J1T J2 * invM * J2T]

        internal static void b2PrepareWeldJoint(B2JointSim @base, B2StepContext context)
        {
            B2_ASSERT(@base.type == B2JointType.b2_weldJoint);

            // chase body id to the solver set where the body lives
            int idA = @base.bodyIdA;
            int idB = @base.bodyIdB;

            B2World world = context.world;

            B2Body bodyA = b2Array_Get(ref world.bodies, idA);
            B2Body bodyB = b2Array_Get(ref world.bodies, idB);

            B2_ASSERT(bodyA.setIndex == (int)B2SetType.b2_awakeSet || bodyB.setIndex == (int)B2SetType.b2_awakeSet);
            B2SolverSet setA = b2Array_Get(ref world.solverSets, bodyA.setIndex);
            B2SolverSet setB = b2Array_Get(ref world.solverSets, bodyB.setIndex);

            int localIndexA = bodyA.localIndex;
            int localIndexB = bodyB.localIndex;

            B2BodySim bodySimA = b2Array_Get(ref setA.bodySims, localIndexA);
            B2BodySim bodySimB = b2Array_Get(ref setB.bodySims, localIndexB);

            float mA = bodySimA.invMass;
            float iA = bodySimA.invInertia;
            float mB = bodySimB.invMass;
            float iB = bodySimB.invInertia;

            @base.invMassA = mA;
            @base.invMassB = mB;
            @base.invIA = iA;
            @base.invIB = iB;

            ref B2WeldJoint joint = ref @base.uj.weldJoint;
            joint.indexA = bodyA.setIndex == (int)B2SetType.b2_awakeSet ? localIndexA : B2_NULL_INDEX;
            joint.indexB = bodyB.setIndex == (int)B2SetType.b2_awakeSet ? localIndexB : B2_NULL_INDEX;

            // Compute joint anchor frames with world space rotation, relative to center of mass
            joint.frameA.q = b2MulRot(bodySimA.transform.q, @base.localFrameA.q);
            joint.frameA.p = b2RotateVector(bodySimA.transform.q, b2Sub(@base.localFrameA.p, bodySimA.localCenter));
            joint.frameB.q = b2MulRot(bodySimB.transform.q, @base.localFrameB.q);
            joint.frameB.p = b2RotateVector(bodySimB.transform.q, b2Sub(@base.localFrameB.p, bodySimB.localCenter));

            // Compute the initial center delta. Incremental position updates are relative to this.
            joint.deltaCenter = b2Sub(bodySimB.center, bodySimA.center);

            float ka = iA + iB;
            joint.axialMass = ka > 0.0f ? 1.0f / ka : 0.0f;

            if (joint.linearHertz == 0.0f)
            {
                joint.linearSpring = @base.constraintSoftness;
            }
            else
            {
                joint.linearSpring = b2MakeSoft(joint.linearHertz, joint.linearDampingRatio, context.h);
            }

            if (joint.angularHertz == 0.0f)
            {
                joint.angularSpring = @base.constraintSoftness;
            }
            else
            {
                joint.angularSpring = b2MakeSoft(joint.angularHertz, joint.angularDampingRatio, context.h);
            }

            if (context.enableWarmStarting == false)
            {
                joint.linearImpulse = b2Vec2_zero;
                joint.angularImpulse = 0.0f;
            }
        }

        internal static void b2WarmStartWeldJoint(B2JointSim @base, B2StepContext context)
        {
            float mA = @base.invMassA;
            float mB = @base.invMassB;
            float iA = @base.invIA;
            float iB = @base.invIB;

            // dummy state for static bodies
            B2BodyState dummyState = B2BodyState.Create(b2_identityBodyState);

            ref readonly B2WeldJoint joint = ref @base.uj.weldJoint;

            B2BodyState stateA = joint.indexA == B2_NULL_INDEX ? dummyState : context.states[joint.indexA];
            B2BodyState stateB = joint.indexB == B2_NULL_INDEX ? dummyState : context.states[joint.indexB];

            B2Vec2 rA = b2RotateVector(stateA.deltaRotation, joint.frameA.p);
            B2Vec2 rB = b2RotateVector(stateB.deltaRotation, joint.frameB.p);

            if (0 != (stateA.flags & (uint)B2BodyFlags.b2_dynamicFlag))
            {
                stateA.linearVelocity = b2MulSub(stateA.linearVelocity, mA, joint.linearImpulse);
                stateA.angularVelocity -= iA * (b2Cross(rA, joint.linearImpulse) + joint.angularImpulse);
            }

            if (0 != (stateB.flags & (uint)B2BodyFlags.b2_dynamicFlag))
            {
                stateB.linearVelocity = b2MulAdd(stateB.linearVelocity, mB, joint.linearImpulse);
                stateB.angularVelocity += iB * (b2Cross(rB, joint.linearImpulse) + joint.angularImpulse);
            }
        }

        internal static void b2SolveWeldJoint(B2JointSim @base, B2StepContext context, bool useBias)
        {
            B2_ASSERT(@base.type == B2JointType.b2_weldJoint);

            float mA = @base.invMassA;
            float mB = @base.invMassB;
            float iA = @base.invIA;
            float iB = @base.invIB;

            // dummy state for static bodies
            B2BodyState dummyState = B2BodyState.Create(b2_identityBodyState);

            ref B2WeldJoint joint = ref @base.uj.weldJoint;

            B2BodyState stateA = joint.indexA == B2_NULL_INDEX ? dummyState : context.states[joint.indexA];
            B2BodyState stateB = joint.indexB == B2_NULL_INDEX ? dummyState : context.states[joint.indexB];

            B2Vec2 vA = stateA.linearVelocity;
            float wA = stateA.angularVelocity;
            B2Vec2 vB = stateB.linearVelocity;
            float wB = stateB.angularVelocity;

            // Block solve doesn't work correctly with mixed stiffness values
#if B2_WELD_BLOCK_SOLVE
            // J = [-I -r1_skew I r2_skew]
            //     [ 0       -1 0       1]
            // r_skew = [-ry; rx]

            // Matlab
            // K = [ mA+r1y^2*iA+mB+r2y^2*iB,  -r1y*iA*r1x-r2y*iB*r2x,          -r1y*iA-r2y*iB]
            //     [  -r1y*iA*r1x-r2y*iB*r2x, mA+r1x^2*iA+mB+r2x^2*iB,           r1x*iA+r2x*iB]
            //     [          -r1y*iA-r2y*iB,           r1x*iA+r2x*iB,                   iA+iB]
            B2Vec2 rA = b2RotateVector(stateA.deltaRotation, joint.frameA.p);
            B2Vec2 rB = b2RotateVector(stateB.deltaRotation, joint.frameB.p);

            B2Mat33 K;
            K.cx.X = mA + mB + rA.Y * rA.Y * iA + rB.Y * rB.Y * iB;
            K.cy.X = -rA.Y * rA.X * iA - rB.Y * rB.X * iB;
            K.cz.X = -rA.Y * iA - rB.Y * iB;
            K.cx.Y = K.cy.X;
            K.cy.Y = mA + mB + rA.X * rA.X * iA + rB.X * rB.X * iB;
            K.cz.Y = rA.X * iA + rB.X * iB;
            K.cx.Z = K.cz.X;
            K.cy.Z = K.cz.Y;
            K.cz.Z = iA + iB;

            B2Vec3 bias = new B2Vec3(0.0f, 0.0f, 0.0f);
            float linearMassScale = 1.0f;
            float linearImpulseScale = 0.0f;
            if (useBias || joint.linearHertz > 0.0f)
            {
                // linear
                B2Vec2 dcA = stateA.deltaPosition;
                B2Vec2 dcB = stateB.deltaPosition;
                B2Vec2 jointTranslation = b2Add(b2Add(b2Sub(dcB, dcA), b2Sub(rB, rA)), joint.deltaCenter);

                bias.X = joint.linearSpring.biasRate * jointTranslation.X;
                bias.Y = joint.linearSpring.biasRate * jointTranslation.Y;

                linearMassScale = joint.linearSpring.massScale;
                linearImpulseScale = joint.linearSpring.impulseScale;
            }

            float angularMassScale = 1.0f;
            float angularImpulseScale = 0.0f;
            if (useBias || joint.angularHertz > 0.0f)
            {
                // angular
                B2Rot qA = b2MulRot(stateA.deltaRotation, joint.frameA.q);
                B2Rot qB = b2MulRot(stateB.deltaRotation, joint.frameB.q);
                B2Rot relQ = b2InvMulRot(qA, qB);
                float jointAngle = b2Rot_GetAngle(relQ);

                bias.Z = joint.angularSpring.biasRate * jointAngle;

                angularMassScale = joint.angularSpring.massScale;
                angularImpulseScale = joint.angularSpring.impulseScale;
            }

            B2Vec2 Cdot1 = b2Sub(b2Add(vB, b2CrossSV(wB, rB)), b2Add(vA, b2CrossSV(wA, rA)));
            float Cdot2 = wB - wA;

            B2Vec3 Cdot = new B2Vec3(Cdot1.X + bias.X, Cdot1.Y + bias.Y, Cdot2 + bias.Z);

            B2Vec3 b = b2Solve33(ref K, Cdot);

            B2Vec2 linearImpulse = new B2Vec2(
                -linearMassScale * b.X - linearImpulseScale * joint.linearImpulse.X,
                -linearMassScale * b.Y - linearImpulseScale * joint.linearImpulse.Y
            );
            joint.linearImpulse = b2Add(joint.linearImpulse, linearImpulse);

            float angularImpulse = -angularMassScale * b.Z - angularImpulseScale * joint.angularImpulse;
            joint.angularImpulse += angularImpulse;

            vA = b2MulSub(vA, mA, linearImpulse);
            wA -= iA * (b2Cross(rA, linearImpulse) + angularImpulse);
            vB = b2MulAdd(vB, mB, linearImpulse);
            wB += iB * (b2Cross(rB, linearImpulse) + angularImpulse);

            // todo debugging
            Cdot1 = b2Sub(b2Add(vB, b2CrossSV(wB, rB)), b2Add(vA, b2CrossSV(wA, rA)));
            Cdot2 = wB - wA;

            if (useBias == false && b2Length(Cdot1) > 0.0001f)
            {
                Cdot1.X += 0.0f;
            }

            if (useBias == false && b2AbsFloat(Cdot2) > 0.0001f)
            {
                Cdot2 += 0.0f;
            }
#else
            // angular constraint
            {
                B2Rot qA = b2MulRot(stateA.deltaRotation, joint.frameA.q);
                B2Rot qB = b2MulRot(stateB.deltaRotation, joint.frameB.q);
                B2Rot relQ = b2InvMulRot(qA, qB);
                float jointAngle = b2Rot_GetAngle(relQ);

                float bias = 0.0f;
                float massScale = 1.0f;
                float impulseScale = 0.0f;
                if (useBias || joint.angularHertz > 0.0f)
                {
                    float C = jointAngle;
                    bias = joint.angularSpring.biasRate * C;
                    massScale = joint.angularSpring.massScale;
                    impulseScale = joint.angularSpring.impulseScale;
                }

                float Cdot = wB - wA;
                float impulse = -massScale * joint.axialMass * (Cdot + bias) - impulseScale * joint.angularImpulse;
                joint.angularImpulse += impulse;

                wA -= iA * impulse;
                wB += iB * impulse;
            }

            // linear constraint
            {
                B2Vec2 rA = b2RotateVector(stateA.deltaRotation, joint.frameA.p);
                B2Vec2 rB = b2RotateVector(stateB.deltaRotation, joint.frameB.p);

                B2Vec2 bias = b2Vec2_zero;
                float massScale = 1.0f;
                float impulseScale = 0.0f;
                if (useBias || joint.linearHertz > 0.0f)
                {
                    B2Vec2 dcA = stateA.deltaPosition;
                    B2Vec2 dcB = stateB.deltaPosition;
                    B2Vec2 C = b2Add(b2Add(b2Sub(dcB, dcA), b2Sub(rB, rA)), joint.deltaCenter);

                    bias = b2MulSV(joint.linearSpring.biasRate, C);
                    massScale = joint.linearSpring.massScale;
                    impulseScale = joint.linearSpring.impulseScale;
                }

                B2Vec2 Cdot = b2Sub(b2Add(vB, b2CrossSV(wB, rB)), b2Add(vA, b2CrossSV(wA, rA)));

                B2Mat22 K;
                K.cx.X = mA + mB + rA.Y * rA.Y * iA + rB.Y * rB.Y * iB;
                K.cy.X = -rA.Y * rA.X * iA - rB.Y * rB.X * iB;
                K.cx.Y = K.cy.X;
                K.cy.Y = mA + mB + rA.X * rA.X * iA + rB.X * rB.X * iB;
                B2Vec2 b = b2Solve22(K, b2Add(Cdot, bias));

                B2Vec2 impulse = new B2Vec2(
                    -massScale * b.X - impulseScale * joint.linearImpulse.X,
                    -massScale * b.Y - impulseScale * joint.linearImpulse.Y
                );

                joint.linearImpulse = b2Add(joint.linearImpulse, impulse);

                vA = b2MulSub(vA, mA, impulse);
                wA -= iA * b2Cross(rA, impulse);
                vB = b2MulAdd(vB, mB, impulse);
                wB += iB * b2Cross(rB, impulse);
            }
#endif

            B2_ASSERT(b2IsValidVec2(vA));
            B2_ASSERT(b2IsValidFloat(wA));
            B2_ASSERT(b2IsValidVec2(vB));
            B2_ASSERT(b2IsValidFloat(wB));

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

#if FALSE
        internal static void b2DumpWeldJoint()
        {
            int32 indexA = bodyA->islandIndex;
            int32 indexB = bodyB->islandIndex;

            b2Dump("  b2WeldJointDef jd;\n");
            b2Dump("  jd.bodyA = sims[%d];\n", indexA);
            b2Dump("  jd.bodyB = sims[%d];\n", indexB);
            b2Dump("  jd.collideConnected = bool(%d);\n", collideConnected);
            b2Dump("  jd.localAnchorA.Set(%.9g, %.9g);\n", localAnchorA.x, localAnchorA.y);
            b2Dump("  jd.localAnchorB.Set(%.9g, %.9g);\n", localAnchorB.x, localAnchorB.y);
            b2Dump("  jd.referenceAngle = %.9g;\n", referenceAngle);
            b2Dump("  jd.stiffness = %.9g;\n", stiffness);
            b2Dump("  jd.damping = %.9g;\n", damping);
            b2Dump("  joints[%d] = world->CreateJoint(&jd);\n", index);        
        }
#endif

        internal static void b2DrawWeldJoint(B2DebugDraw draw, B2JointSim @base, B2Transform transformA, B2Transform transformB, float drawScale)
        {
            B2_ASSERT(@base.type == B2JointType.b2_weldJoint);

            B2Transform frameA = b2MulTransforms(transformA, @base.localFrameA);
            B2Transform frameB = b2MulTransforms(transformB, @base.localFrameB);

            B2Polygon box = b2MakeBox(0.25f * drawScale, 0.125f * drawScale);

            B2FixedArray4<B2Vec2> points = new B2FixedArray4<B2Vec2>();

            for (int i = 0; i < 4; ++i)
            {
                points[i] = b2TransformPoint(frameA, box.vertices[i]);
            }

            draw.DrawPolygonFcn(points.AsSpan(), 4, B2HexColor.b2_colorDarkOrange, draw.context);

            for (int i = 0; i < 4; ++i)
            {
                points[i] = b2TransformPoint(frameB, box.vertices[i]);
            }

            draw.DrawPolygonFcn(points.AsSpan(), 4, B2HexColor.b2_colorDarkCyan, draw.context);
        }
    }
}