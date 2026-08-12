// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using static Box2D.NET.B2Arrays;

namespace Box2D.NET
{
    /// The broad-phase is used for computing pairs and performing volume queries and ray casts.
    /// This broad-phase does not persist pairs. Instead, this reports potentially new pairs.
    /// It is up to the client to consume the new pairs and to track subsequent overlap.
    public class B2BroadPhase
    {
        public B2DynamicTree[] trees;

        // Per body-type bit sets indexed by proxyId, marking proxies moved this step.
        // Paired with moveArray which preserves deterministic insertion order for pair queries.
        public B2BitSet[] movedProxies;
        public B2Array<int> moveArray;

        // These are the results from the pair query and are used to create new contacts
        // in deterministic order. There is a move result linked list for each moving shape and
        // these follow the dynamic tree query order for determinism.
        public ArraySegment<B2MoveResult> moveResults;
        public ArraySegment<B2MovePair> movePairs;
        public int movePairCapacity;
        public B2AtomicInt movePairIndex;

        // Tracks shape pairs that have a b2Contact
        public B2HashSet pairSet;

        public void Clear()
        {
            trees = null;
            movedProxies = null;
            b2Array_Clear(ref moveArray);
            moveResults = null;
            movePairs = null;
            movePairCapacity = 0;
            movePairIndex = new B2AtomicInt();
            pairSet = new B2HashSet();
        }
    }
}
