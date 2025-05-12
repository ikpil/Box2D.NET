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

        // The move set and array are used to track shapes that have moved significantly
        // and need a pair query for new contacts. The array has a deterministic order.
        // todo perhaps just a move set?
        // todo implement a 32bit hash set for faster lookup
        // todo moveSet can grow quite large on the first time step and remain large
        public B2HashSet moveSet;
        public B2Array<int> moveArray;

        // These are the results from the pair query and are used to create new contacts
        // in deterministic order.
        // todo these could be in the step context
        public ArraySegment<B2MoveResult> moveResults;
        public ArraySegment<B2MovePair> movePairs;
        public int movePairCapacity;
        public B2AtomicInt movePairIndex;

        // Tracks shape pairs that have a b2Contact
        // todo pairSet can grow quite large on the first time step and remain large
        public B2HashSet pairSet;

        public void Clear()
        {
            trees = null;
            moveSet = new B2HashSet();
            b2Array_Clear(ref moveArray);
            moveResults = null;
            movePairs = null;
            movePairCapacity = 0;
            movePairIndex = new B2AtomicInt();
            pairSet = new B2HashSet();
        }
    }
}
