// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Map from b2JointId to b2Joint in the solver sets
    public class B2Joint
    {
        public object userData;

        // index of simulation set stored in b2World
        // B2_NULL_INDEX when slot is free
        public int setIndex;

        // index into the constraint graph color array, may be B2_NULL_INDEX for sleeping/disabled joints
        // B2_NULL_INDEX when slot is free
        public int colorIndex;

        // joint index within set or graph color
        // B2_NULL_INDEX when slot is free
        public int localIndex;

        public B2FixedArray2<B2JointEdge> edges;

        public int jointId;
        public int islandId;
        public int islandPrev;
        public int islandNext;

        public float drawScale;

        public B2JointType type;

        // This is monotonically advanced when a body is allocated in this slot
        // Used to check for invalid b2JointId
        public ushort generation;

        public bool isMarked;
        public bool collideConnected;
    }
}
