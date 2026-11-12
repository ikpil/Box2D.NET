// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;

namespace Box2D.NET
{
    /// The broad-phase is used for computing pairs and performing volume queries and ray casts.
    /// This broad-phase does not persist pairs. Instead, this reports potentially new pairs.
    /// It is up to the client to consume the new pairs and to track subsequent overlap.
    public class B2BroadPhase
    {
        // One tree for each body type.
        public B2DynamicTree[] trees;

        // The moved siblings gathered from the dynamic body tree.
        public ArraySegment<int> movedSiblings;

        // Tracks shape pairs that have a b2Contact
        public B2HashSet pairSet;

        // Seeds for colliding the dynamic tree against the static and kinematic trees.
        // Upstream keeps this on the stack. It is kept here because the parallel tasks need it.
        public B2NodePair[] crossSeeds;

        public void Clear()
        {
            trees = null;
            movedSiblings = null;
            pairSet = new B2HashSet();
            crossSeeds = null;
        }
    }
}
