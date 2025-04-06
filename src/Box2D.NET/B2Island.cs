// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Persistent island for awake bodies, joints, and contacts
    // https://en.wikipedia.org/wiki/Component_(graph_theory)
    // https://en.wikipedia.org/wiki/Dynamic_connectivity
    // map from int to solver set and index
    public class B2Island
    {
        // index of solver set stored in b2World
        // may be B2_NULL_INDEX
        public int setIndex;

        // island index within set
        // may be B2_NULL_INDEX
        public int localIndex;

        public int islandId;

        public int headBody;
        public int tailBody;
        public int bodyCount;

        public int headContact;
        public int tailContact;
        public int contactCount;

        public int headJoint;
        public int tailJoint;
        public int jointCount;

        // Union find
        // todo this could go away if islands are merged immediately with b2LinkJoint and b2LinkContact
        public int parentIsland;

        // Keeps track of how many contacts have been removed from this island.
        // This is used to determine if an island is a candidate for splitting.
        public int constraintRemoveCount;
    }
}
