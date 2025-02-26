// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using static Box2D.NET.array;

namespace Box2D.NET.Primitives
{
    /// The broad-phase is used for computing pairs and performing volume queries and ray casts.
    /// This broad-phase does not persist pairs. Instead, this reports potentially new pairs.
    /// It is up to the client to consume the new pairs and to track subsequent overlap.
    public class b2BroadPhase
    {
        public b2DynamicTree[] trees;
        public int proxyCount;

        // The move set and array are used to track shapes that have moved significantly
        // and need a pair query for new contacts. The array has a deterministic order.
        // todo perhaps just a move set?
        // todo implement a 32bit hash set for faster lookup
        // todo moveSet can grow quite large on the first time step and remain large
        public b2HashSet moveSet;
        public b2Array<int> moveArray;

        // These are the results from the pair query and are used to create new contacts
        // in deterministic order.
        // todo these could be in the step context
        public ArraySegment<b2MoveResult> moveResults;
        public ArraySegment<b2MovePair> movePairs;
        public int movePairCapacity;
        public b2AtomicInt movePairIndex;

        // Tracks shape pairs that have a b2Contact
        // todo pairSet can grow quite large on the first time step and remain large
        public b2HashSet pairSet;

        public void Clear()
        {
            trees = null;
            proxyCount = 0;
            moveSet = null;
            b2Array_Clear(ref moveArray);
            moveResults = null;
            movePairs = null;
            movePairCapacity = 0;
            movePairIndex = new b2AtomicInt();
            pairSet = null;
        }
    }
}
