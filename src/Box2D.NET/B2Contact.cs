// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Cold contact data. Used as a persistent handle and for persistent island
    // connectivity.
    public class B2Contact
    {
        // index of simulation set stored in b2World
        // B2_NULL_INDEX when slot is free
        public int setIndex;

        // index into the constraint graph color array
        // B2_NULL_INDEX for non-touching or sleeping contacts
        // B2_NULL_INDEX when slot is free
        public int colorIndex;

        // contact index within set or graph color
        // B2_NULL_INDEX when slot is free
        public int localIndex;

        public B2FixedArray2<B2ContactEdge> edges;
        public int shapeIdA;
        public int shapeIdB;

        // A contact only belongs to an island if touching, otherwise B2_NULL_INDEX.
        public int islandPrev;
        public int islandNext;
        public int islandId;

        public int contactId;

        // b2ContactFlags
        public uint flags;
        
        // This is monotonically advanced when a contact is allocated in this slot
        // Used to check for invalid b2ContactId
        public uint generation;

        public bool isMarked;
    }
}
