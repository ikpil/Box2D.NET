// SPDX-FileCopyrightText: 2023 Erin Catto
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

using System;
using System.Diagnostics;
using Box2D.NET.Primitives;
using static Box2D.NET.array;
using static Box2D.NET.constants;
using static Box2D.NET.math_function;
using static Box2D.NET.bitset;

namespace Box2D.NET
{
    public class b2GraphColor
    {
        // This bitset is indexed by bodyId so this is over-sized to encompass static bodies
        // however I never traverse these bits or use the bit count for anything
        // This bitset is unused on the overflow color.
        public b2BitSet bodySet;

        // cache friendly arrays
        public b2Array<b2ContactSim> contactSims;
        public b2Array<b2JointSim> jointSims;

        // TODO: @ikpil, check
        // transient
        //union
        //{
        public ArraySegment<b2ContactConstraintSIMD> simdConstraints;

        public ArraySegment<b2ContactConstraint> overflowConstraints;
        //};
    }

    public class b2ConstraintGraph
    {
        // including overflow at the end
        public b2GraphColor[] colors;
    }

    public class constraint_graph
    {
        // This holds constraints that cannot fit the graph color limit. This happens when a single dynamic body
        // is touching many other bodies.
        public const int B2_OVERFLOW_INDEX = B2_GRAPH_COLOR_COUNT - 1;

        public static void b2CreateGraph(ref b2ConstraintGraph graph, int bodyCapacity)
        {
            Debug.Assert(B2_GRAPH_COLOR_COUNT == 12, "graph color count assumed to be 12");
            Debug.Assert(B2_GRAPH_COLOR_COUNT >= 2, "must have at least two constraint graph colors");
            Debug.Assert(B2_OVERFLOW_INDEX == B2_GRAPH_COLOR_COUNT - 1, "bad over flow index");

            // @ikpil, new b2ConstraintGraph
            graph = new b2ConstraintGraph();
            graph.colors = new b2GraphColor[B2_GRAPH_COLOR_COUNT];
            for (int i = 0; i < graph.colors.Length; ++i)
            {
                graph.colors[i] = new b2GraphColor();
            }

            bodyCapacity = b2MaxInt(bodyCapacity, 8);

            // Initialize graph color bit set.
            // No bitset for overflow color.
            for (int i = 0; i < B2_OVERFLOW_INDEX; ++i)
            {
                b2GraphColor color = graph.colors[i];
                color.bodySet = b2CreateBitSet(bodyCapacity);
                color.contactSims = b2Array_Create<b2ContactSim>();
                color.jointSims = b2Array_Create<b2JointSim>();

                b2SetBitCountAndClear(color.bodySet, bodyCapacity);
            }

            // @ikpil, for dummy
            for (int i = B2_OVERFLOW_INDEX; i < B2_GRAPH_COLOR_COUNT; ++i)
            {
                var color = graph.colors[i];
                color.bodySet = new b2BitSet();
                color.contactSims = b2Array_Create<b2ContactSim>();
                color.jointSims = b2Array_Create<b2JointSim>();
            }
        }

        public static void b2DestroyGraph(b2ConstraintGraph graph)
        {
            for (int i = 0; i < B2_GRAPH_COLOR_COUNT; ++i)
            {
                b2GraphColor color = graph.colors[i];

                // The bit set should never be used on the overflow color
                Debug.Assert(i != B2_OVERFLOW_INDEX || color.bodySet.bits == null);

                b2DestroyBitSet(color.bodySet);

                b2Array_Destroy(color.contactSims);
                b2Array_Destroy(color.jointSims);
            }
        }

        // Contacts are always created as non-touching. They get cloned into the constraint
        // graph once they are found to be touching.
        // todo maybe kinematic bodies should not go into graph
        public static void b2AddContactToGraph(b2World world, b2ContactSim contactSim, b2Contact contact)
        {
            Debug.Assert(contactSim.manifold.pointCount > 0);
            Debug.Assert(0 != (contactSim.simFlags & (uint)b2ContactSimFlags.b2_simTouchingFlag));
            Debug.Assert(0 != (contact.flags & (uint)b2ContactFlags.b2_contactTouchingFlag));

            b2ConstraintGraph graph = world.constraintGraph;
            int colorIndex = B2_OVERFLOW_INDEX;

            int bodyIdA = contact.edges[0].bodyId;
            int bodyIdB = contact.edges[1].bodyId;
            b2Body bodyA = b2Array_Get(world.bodies, bodyIdA);
            b2Body bodyB = b2Array_Get(world.bodies, bodyIdB);
            bool staticA = bodyA.setIndex == (int)b2SetType.b2_staticSet;
            bool staticB = bodyB.setIndex == (int)b2SetType.b2_staticSet;
            Debug.Assert(staticA == false || staticB == false);

#if B2_FORCE_OVERFLOW
            if (staticA == false && staticB == false)
            {
                for (int i = 0; i < B2_OVERFLOW_INDEX; ++i)
                {
                    b2GraphColor color0 = graph.colors[i];
                    if (b2GetBit(color0.bodySet, bodyIdA) || b2GetBit(color0.bodySet, bodyIdB))
                    {
                        continue;
                    }

                    b2SetBitGrow(color0.bodySet, bodyIdA);
                    b2SetBitGrow(color0.bodySet, bodyIdB);
                    colorIndex = i;
                    break;
                }
            }
            else if (staticA == false)
            {
                // No static contacts in color 0
                for (int i = 1; i < B2_OVERFLOW_INDEX; ++i)
                {
                    b2GraphColor color0 = graph.colors[i];
                    if (b2GetBit(color0.bodySet, bodyIdA))
                    {
                        continue;
                    }

                    b2SetBitGrow(color0.bodySet, bodyIdA);
                    colorIndex = i;
                    break;
                }
            }
            else if (staticB == false)
            {
                // No static contacts in color 0
                for (int i = 1; i < B2_OVERFLOW_INDEX; ++i)
                {
                    b2GraphColor color0 = graph.colors[i];
                    if (b2GetBit(color0.bodySet, bodyIdB))
                    {
                        continue;
                    }

                    b2SetBitGrow(color0.bodySet, bodyIdB);
                    colorIndex = i;
                    break;
                }
            }
#endif

            b2GraphColor color = graph.colors[colorIndex];
            contact.colorIndex = colorIndex;
            contact.localIndex = color.contactSims.count;

            b2ContactSim newContact = b2Array_Add(color.contactSims);
            //memcpy( newContact, contactSim, sizeof( b2ContactSim ) );

            // todo perhaps skip this if the contact is already awake

            if (staticA)
            {
                newContact.bodySimIndexA = B2_NULL_INDEX;
                newContact.invMassA = 0.0f;
                newContact.invIA = 0.0f;
            }
            else
            {
                Debug.Assert(bodyA.setIndex == (int)b2SetType.b2_awakeSet);
                b2SolverSet awakeSet = b2Array_Get(world.solverSets, (int)b2SetType.b2_awakeSet);

                int localIndex = bodyA.localIndex;
                newContact.bodySimIndexA = localIndex;

                b2BodySim bodySimA = b2Array_Get(awakeSet.bodySims, localIndex);
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
                Debug.Assert(bodyB.setIndex == (int)b2SetType.b2_awakeSet);
                b2SolverSet awakeSet = b2Array_Get(world.solverSets, (int)b2SetType.b2_awakeSet);

                int localIndex = bodyB.localIndex;
                newContact.bodySimIndexB = localIndex;

                b2BodySim bodySimB = b2Array_Get(awakeSet.bodySims, localIndex);
                newContact.invMassB = bodySimB.invMass;
                newContact.invIB = bodySimB.invInertia;
            }
        }

        public static void b2RemoveContactFromGraph(b2World world, int bodyIdA, int bodyIdB, int colorIndex, int localIndex)
        {
            b2ConstraintGraph graph = world.constraintGraph;

            Debug.Assert(0 <= colorIndex && colorIndex < B2_GRAPH_COLOR_COUNT);
            b2GraphColor color = graph.colors[colorIndex];

            if (colorIndex != B2_OVERFLOW_INDEX)
            {
                // might clear a bit for a static body, but this has no effect
                b2ClearBit(color.bodySet, (uint)bodyIdA);
                b2ClearBit(color.bodySet, (uint)bodyIdB);
            }

            int movedIndex = b2Array_RemoveSwap(color.contactSims, localIndex);
            if (movedIndex != B2_NULL_INDEX)
            {
                // Fix index on swapped contact
                b2ContactSim movedContactSim = color.contactSims.data[localIndex];

                // Fix moved contact
                int movedId = movedContactSim.contactId;
                b2Contact movedContact = b2Array_Get(world.contacts, movedId);
                Debug.Assert(movedContact.setIndex == (int)b2SetType.b2_awakeSet);
                Debug.Assert(movedContact.colorIndex == colorIndex);
                Debug.Assert(movedContact.localIndex == movedIndex);
                movedContact.localIndex = localIndex;
            }
        }

        public static int b2AssignJointColor(b2ConstraintGraph graph, int bodyIdA, int bodyIdB, bool staticA, bool staticB)
        {
            Debug.Assert(staticA == false || staticB == false);

#if B2_FORCE_OVERFLOW
            if (staticA == false && staticB == false)
            {
                for (int i = 0; i < B2_OVERFLOW_INDEX; ++i)
                {
                    b2GraphColor color = graph.colors[i];
                    if (b2GetBit(color.bodySet, bodyIdA) || b2GetBit(color.bodySet, bodyIdB))
                    {
                        continue;
                    }

                    b2SetBitGrow(color.bodySet, bodyIdA);
                    b2SetBitGrow(color.bodySet, bodyIdB);
                    return i;
                }
            }
            else if (staticA == false)
            {
                for (int i = 0; i < B2_OVERFLOW_INDEX; ++i)
                {
                    b2GraphColor color = graph.colors[i];
                    if (b2GetBit(color.bodySet, bodyIdA))
                    {
                        continue;
                    }

                    b2SetBitGrow(color.bodySet, bodyIdA);
                    return i;
                }
            }
            else if (staticB == false)
            {
                for (int i = 0; i < B2_OVERFLOW_INDEX; ++i)
                {
                    b2GraphColor color = graph.colors[i];
                    if (b2GetBit(color.bodySet, bodyIdB))
                    {
                        continue;
                    }

                    b2SetBitGrow(color.bodySet, bodyIdB);
                    return i;
                }
            }
#else
	B2_UNUSED( graph, bodyIdA, bodyIdB );
#endif

            return B2_OVERFLOW_INDEX;
        }

        public static b2JointSim b2CreateJointInGraph(b2World world, b2Joint joint)
        {
            b2ConstraintGraph graph = world.constraintGraph;

            int bodyIdA = joint.edges[0].bodyId;
            int bodyIdB = joint.edges[1].bodyId;
            b2Body bodyA = b2Array_Get(world.bodies, bodyIdA);
            b2Body bodyB = b2Array_Get(world.bodies, bodyIdB);
            bool staticA = bodyA.setIndex == (int)b2SetType.b2_staticSet;
            bool staticB = bodyB.setIndex == (int)b2SetType.b2_staticSet;

            int colorIndex = b2AssignJointColor(graph, bodyIdA, bodyIdB, staticA, staticB);

            b2JointSim jointSim = b2Array_Add(graph.colors[colorIndex].jointSims);
            //memset( jointSim, 0, sizeof( b2JointSim ) );

            joint.colorIndex = colorIndex;
            joint.localIndex = graph.colors[colorIndex].jointSims.count - 1;
            return jointSim;
        }

        public static void b2AddJointToGraph(b2World world, b2JointSim jointSim, b2Joint joint)
        {
            b2JointSim jointDst = b2CreateJointInGraph(world, joint);
            //memcpy( jointDst, jointSim, sizeof( b2JointSim ) );
            jointDst.CopyFrom(jointSim);
        }

        public static void b2RemoveJointFromGraph(b2World world, int bodyIdA, int bodyIdB, int colorIndex, int localIndex)
        {
            b2ConstraintGraph graph = world.constraintGraph;

            Debug.Assert(0 <= colorIndex && colorIndex < B2_GRAPH_COLOR_COUNT);
            b2GraphColor color = graph.colors[colorIndex];

            if (colorIndex != B2_OVERFLOW_INDEX)
            {
                // May clear static bodies, no effect
                b2ClearBit(color.bodySet, (uint)bodyIdA);
                b2ClearBit(color.bodySet, (uint)bodyIdB);
            }

            int movedIndex = b2Array_RemoveSwap(color.jointSims, localIndex);
            if (movedIndex != B2_NULL_INDEX)
            {
                // Fix moved joint
                b2JointSim movedJointSim = color.jointSims.data[localIndex];
                int movedId = movedJointSim.jointId;
                b2Joint movedJoint = b2Array_Get(world.joints, movedId);
                Debug.Assert(movedJoint.setIndex == (int)b2SetType.b2_awakeSet);
                Debug.Assert(movedJoint.colorIndex == colorIndex);
                Debug.Assert(movedJoint.localIndex == movedIndex);
                movedJoint.localIndex = localIndex;
            }
        }
    }
}