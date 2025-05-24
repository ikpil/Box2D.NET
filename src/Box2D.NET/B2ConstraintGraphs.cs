// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

// Solver using graph coloring. Islands are only used for sleep.
// High-Performance Physical Simulations on Next-Generation Architecture with Many Cores
// http://web.eecs.umich.edu/~msmelyan/papers/physsim_onmanycore_itj.pdf

// Kinematic bodies have to be treated like dynamic bodies in graph coloring. Unlike static bodies, we cannot use a dummy solver
// body for kinematic bodies. We cannot access a kinematic body from multiple threads efficiently because the SIMD solver body
// scatter would write to the same kinematic body from multiple threads. Even if these writes don't modify the body, they will
// cause horrible cache stalls. To make this feasible I would need a way to block these writes.


// TODO: @ikpil, check 
// This is used for debugging by making all constraints be assigned to overflow.

#define B2_FORCE_OVERFLOW

using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2BitSets;
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET
{
    public static class B2ConstraintGraphs
    {
        // This holds constraints that cannot fit the graph color limit. This happens when a single dynamic body
        // is touching many other bodies.
        public const int B2_OVERFLOW_INDEX = B2_GRAPH_COLOR_COUNT - 1;

        public static void b2CreateGraph(ref B2ConstraintGraph graph, int bodyCapacity)
        {
            B2_ASSERT(B2_GRAPH_COLOR_COUNT == 12, "graph color count assumed to be 12");
            B2_ASSERT(B2_GRAPH_COLOR_COUNT >= 2, "must have at least two constraint graph colors");
            B2_ASSERT(B2_OVERFLOW_INDEX == B2_GRAPH_COLOR_COUNT - 1, "bad over flow index");

            graph = new B2ConstraintGraph();
            graph.colors = new B2GraphColor[B2_GRAPH_COLOR_COUNT];

            bodyCapacity = b2MaxInt(bodyCapacity, 8);

            // Initialize graph color bit set.
            // No bitset for overflow color.
            for (int i = 0; i < B2_OVERFLOW_INDEX; ++i)
            {
                ref B2GraphColor color = ref graph.colors[i];
                color.bodySet = b2CreateBitSet(bodyCapacity);
                color.contactSims = b2Array_Create<B2ContactSim>();
                color.jointSims = b2Array_Create<B2JointSim>();

                b2SetBitCountAndClear(ref color.bodySet, bodyCapacity);
            }

            // @ikpil, for dummy
            for (int i = B2_OVERFLOW_INDEX; i < B2_GRAPH_COLOR_COUNT; ++i)
            {
                var color = graph.colors[i];
                color.bodySet = new B2BitSet();
                color.contactSims = b2Array_Create<B2ContactSim>();
                color.jointSims = b2Array_Create<B2JointSim>();
            }
        }

        public static void b2DestroyGraph(ref B2ConstraintGraph graph)
        {
            for (int i = 0; i < B2_GRAPH_COLOR_COUNT; ++i)
            {
                ref B2GraphColor color = ref graph.colors[i];

                // The bit set should never be used on the overflow color
                B2_ASSERT(i != B2_OVERFLOW_INDEX || color.bodySet.bits == null);

                b2DestroyBitSet(ref color.bodySet);

                b2Array_Destroy(ref color.contactSims);
                b2Array_Destroy(ref color.jointSims);
            }
        }

        // Contacts are always created as non-touching. They get cloned into the constraint
        // graph once they are found to be touching.
        // todo maybe kinematic bodies should not go into graph
        public static void b2AddContactToGraph(B2World world, B2ContactSim contactSim, B2Contact contact)
        {
            B2_ASSERT(contactSim.manifold.pointCount > 0);
            B2_ASSERT(0 != (contactSim.simFlags & (uint)B2ContactSimFlags.b2_simTouchingFlag));
            B2_ASSERT(0 != (contact.flags & (uint)B2ContactFlags.b2_contactTouchingFlag));

            ref B2ConstraintGraph graph = ref world.constraintGraph;
            int colorIndex = B2_OVERFLOW_INDEX;

            int bodyIdA = contact.edges[0].bodyId;
            int bodyIdB = contact.edges[1].bodyId;
            B2Body bodyA = b2Array_Get(ref world.bodies, bodyIdA);
            B2Body bodyB = b2Array_Get(ref world.bodies, bodyIdB);
            bool staticA = bodyA.setIndex == (int)B2SetType.b2_staticSet;
            bool staticB = bodyB.setIndex == (int)B2SetType.b2_staticSet;
            B2_ASSERT(staticA == false || staticB == false);

#if B2_FORCE_OVERFLOW
            if (staticA == false && staticB == false)
            {
                for (int i = 0; i < B2_OVERFLOW_INDEX; ++i)
                {
                    ref B2GraphColor color0 = ref graph.colors[i];
                    if (b2GetBit(ref color0.bodySet, bodyIdA) || b2GetBit(ref color0.bodySet, bodyIdB))
                    {
                        continue;
                    }

                    b2SetBitGrow(ref color0.bodySet, bodyIdA);
                    b2SetBitGrow(ref color0.bodySet, bodyIdB);
                    colorIndex = i;
                    break;
                }
            }
            else if (staticA == false)
            {
                // No static contacts in color 0
                for (int i = 1; i < B2_OVERFLOW_INDEX; ++i)
                {
                    ref B2GraphColor color0 = ref graph.colors[i];
                    if (b2GetBit(ref color0.bodySet, bodyIdA))
                    {
                        continue;
                    }

                    b2SetBitGrow(ref color0.bodySet, bodyIdA);
                    colorIndex = i;
                    break;
                }
            }
            else if (staticB == false)
            {
                // No static contacts in color 0
                for (int i = 1; i < B2_OVERFLOW_INDEX; ++i)
                {
                    ref B2GraphColor color0 = ref graph.colors[i];
                    if (b2GetBit(ref color0.bodySet, bodyIdB))
                    {
                        continue;
                    }

                    b2SetBitGrow(ref color0.bodySet, bodyIdB);
                    colorIndex = i;
                    break;
                }
            }
#endif

            ref B2GraphColor color = ref graph.colors[colorIndex];
            contact.colorIndex = colorIndex;
            contact.localIndex = color.contactSims.count;

            ref B2ContactSim newContact = ref b2Array_Add(ref color.contactSims);
            //memcpy( newContact, contactSim, sizeof( b2ContactSim ) );
            newContact.CopyFrom(contactSim);

            // todo perhaps skip this if the contact is already awake

            if (staticA)
            {
                newContact.bodySimIndexA = B2_NULL_INDEX;
                newContact.invMassA = 0.0f;
                newContact.invIA = 0.0f;
            }
            else
            {
                B2_ASSERT(bodyA.setIndex == (int)B2SetType.b2_awakeSet);
                B2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_awakeSet);

                int localIndex = bodyA.localIndex;
                newContact.bodySimIndexA = localIndex;

                B2BodySim bodySimA = b2Array_Get(ref awakeSet.bodySims, localIndex);
                newContact.invMassA = bodySimA.invMass;
                newContact.invIA = bodySimA.invInertia;
            }

            if (staticB)
            {
                newContact.bodySimIndexB = B2_NULL_INDEX;
                newContact.invMassB = 0.0f;
                newContact.invIB = 0.0f;
            }
            else
            {
                B2_ASSERT(bodyB.setIndex == (int)B2SetType.b2_awakeSet);
                B2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)B2SetType.b2_awakeSet);

                int localIndex = bodyB.localIndex;
                newContact.bodySimIndexB = localIndex;

                B2BodySim bodySimB = b2Array_Get(ref awakeSet.bodySims, localIndex);
                newContact.invMassB = bodySimB.invMass;
                newContact.invIB = bodySimB.invInertia;
            }
        }

        public static void b2RemoveContactFromGraph(B2World world, int bodyIdA, int bodyIdB, int colorIndex, int localIndex)
        {
            ref B2ConstraintGraph graph = ref world.constraintGraph;

            B2_ASSERT(0 <= colorIndex && colorIndex < B2_GRAPH_COLOR_COUNT);
            ref B2GraphColor color = ref graph.colors[colorIndex];

            if (colorIndex != B2_OVERFLOW_INDEX)
            {
                // might clear a bit for a static body, but this has no effect
                b2ClearBit(ref color.bodySet, (uint)bodyIdA);
                b2ClearBit(ref color.bodySet, (uint)bodyIdB);
            }

            int movedIndex = b2Array_RemoveSwap(ref color.contactSims, localIndex);
            if (movedIndex != B2_NULL_INDEX)
            {
                // Fix index on swapped contact
                B2ContactSim movedContactSim = color.contactSims.data[localIndex];

                // Fix moved contact
                int movedId = movedContactSim.contactId;
                B2Contact movedContact = b2Array_Get(ref world.contacts, movedId);
                B2_ASSERT(movedContact.setIndex == (int)B2SetType.b2_awakeSet);
                B2_ASSERT(movedContact.colorIndex == colorIndex);
                B2_ASSERT(movedContact.localIndex == movedIndex);
                movedContact.localIndex = localIndex;
            }
        }

        public static int b2AssignJointColor(ref B2ConstraintGraph graph, int bodyIdA, int bodyIdB, bool staticA, bool staticB)
        {
            B2_ASSERT(staticA == false || staticB == false);

#if B2_FORCE_OVERFLOW
            if (staticA == false && staticB == false)
            {
                for (int i = 0; i < B2_OVERFLOW_INDEX; ++i)
                {
                    ref B2GraphColor color = ref graph.colors[i];
                    if (b2GetBit(ref color.bodySet, bodyIdA) || b2GetBit(ref color.bodySet, bodyIdB))
                    {
                        continue;
                    }

                    b2SetBitGrow(ref color.bodySet, bodyIdA);
                    b2SetBitGrow(ref color.bodySet, bodyIdB);
                    return i;
                }
            }
            else if (staticA == false)
            {
                for (int i = 0; i < B2_OVERFLOW_INDEX; ++i)
                {
                    ref B2GraphColor color = ref graph.colors[i];
                    if (b2GetBit(ref color.bodySet, bodyIdA))
                    {
                        continue;
                    }

                    b2SetBitGrow(ref color.bodySet, bodyIdA);
                    return i;
                }
            }
            else if (staticB == false)
            {
                for (int i = 0; i < B2_OVERFLOW_INDEX; ++i)
                {
                    ref B2GraphColor color = ref graph.colors[i];
                    if (b2GetBit(ref color.bodySet, bodyIdB))
                    {
                        continue;
                    }

                    b2SetBitGrow(ref color.bodySet, bodyIdB);
                    return i;
                }
            }
#else
            B2_UNUSED(graph, bodyIdA, bodyIdB, staticA, staticB);
#endif

            return B2_OVERFLOW_INDEX;
        }

        public static ref B2JointSim b2CreateJointInGraph(B2World world, B2Joint joint)
        {
            ref B2ConstraintGraph graph = ref world.constraintGraph;

            int bodyIdA = joint.edges[0].bodyId;
            int bodyIdB = joint.edges[1].bodyId;
            B2Body bodyA = b2Array_Get(ref world.bodies, bodyIdA);
            B2Body bodyB = b2Array_Get(ref world.bodies, bodyIdB);
            bool staticA = bodyA.setIndex == (int)B2SetType.b2_staticSet;
            bool staticB = bodyB.setIndex == (int)B2SetType.b2_staticSet;

            int colorIndex = b2AssignJointColor(ref graph, bodyIdA, bodyIdB, staticA, staticB);

            ref B2JointSim jointSim = ref b2Array_Add(ref graph.colors[colorIndex].jointSims);
            //memset( jointSim, 0, sizeof( b2JointSim ) );
            jointSim.Clear();

            joint.colorIndex = colorIndex;
            joint.localIndex = graph.colors[colorIndex].jointSims.count - 1;
            return ref jointSim;
        }

        public static void b2AddJointToGraph(B2World world, B2JointSim jointSim, B2Joint joint)
        {
            B2JointSim jointDst = b2CreateJointInGraph(world, joint);
            //memcpy( jointDst, jointSim, sizeof( b2JointSim ) );
            jointDst.CopyFrom(jointSim);
        }

        public static void b2RemoveJointFromGraph(B2World world, int bodyIdA, int bodyIdB, int colorIndex, int localIndex)
        {
            ref B2ConstraintGraph graph = ref world.constraintGraph;

            B2_ASSERT(0 <= colorIndex && colorIndex < B2_GRAPH_COLOR_COUNT);
            ref B2GraphColor color = ref graph.colors[colorIndex];

            if (colorIndex != B2_OVERFLOW_INDEX)
            {
                // May clear static bodies, no effect
                b2ClearBit(ref color.bodySet, (uint)bodyIdA);
                b2ClearBit(ref color.bodySet, (uint)bodyIdB);
            }

            int movedIndex = b2Array_RemoveSwap(ref color.jointSims, localIndex);
            if (movedIndex != B2_NULL_INDEX)
            {
                // Fix moved joint
                B2JointSim movedJointSim = color.jointSims.data[localIndex];
                int movedId = movedJointSim.jointId;
                B2Joint movedJoint = b2Array_Get(ref world.joints, movedId);
                B2_ASSERT(movedJoint.setIndex == (int)B2SetType.b2_awakeSet);
                B2_ASSERT(movedJoint.colorIndex == colorIndex);
                B2_ASSERT(movedJoint.localIndex == movedIndex);
                movedJoint.localIndex = localIndex;
            }
        }
    }
}