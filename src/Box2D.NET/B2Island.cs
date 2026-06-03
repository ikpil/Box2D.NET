// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Deterministic solver
    //
    // Collide all awake contacts
    // Use bit array to emit start/stop touching events in defined order, per thread. Try using contact index, assuming contacts are
    // created in a deterministic order. bit-wise OR together bit arrays and issue changes:
    // - start touching: merge islands - temporary linked list - mark root island dirty - wake all - largest island is root
    // - stop touching: increment constraintRemoveCount
    public class B2Island
    {
        // index of solver set stored in b2World
        // may be B2_NULL_INDEX
        public int setIndex;

        // island index within set
        // may be B2_NULL_INDEX
        public int localIndex;

        public int islandId;

        // Keeps track of how many contacts have been removed from this island.
        // This is used to determine if an island is a candidate for splitting.
        public int constraintRemoveCount;

        // I tried using a stack array for this but the data pointer goes out of
        // sync when the world island array grows.
        public B2Array<int> bodies;

        // Contacts and joints that belong to this island. May connect to static
        // bodies not in the island.
        // Each link has the two body ids so that b2SplitIsland's union-find pass
        // never needs to touch b2Contact/b2Joint.
        public B2Array<B2ContactLink> contacts;
        public B2Array<B2JointLink> joints;
    }
}
