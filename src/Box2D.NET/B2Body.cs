// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Body organizational details that are not used in the solver.
    public class B2Body
    {
        public string name;

        public object userData;

        // index of solver set stored in b2World
        // may be B2_NULL_INDEX
        public int setIndex;

        // body sim and state index within set
        // may be B2_NULL_INDEX
        public int localIndex;

        // [31 : contactId | 1 : edgeIndex]
        public int headContactKey;
        public int contactCount;

        // todo maybe move this to the body sim
        public int headShapeId;
        public int shapeCount;

        public int headChainId;

        // [31 : jointId | 1 : edgeIndex]
        public int headJointKey;
        public int jointCount;

        // All enabled dynamic and kinematic bodies are in an island.
        public int islandId;

        // doubly-linked island list
        public int islandPrev;
        public int islandNext;

        public float mass;

        // Rotational inertia about the center of mass.
        public float inertia;

        public float sleepThreshold;
        public float sleepTime;

        // this is used to adjust the fellAsleep flag in the body move array
        public int bodyMoveIndex;

        public int id;

        // b2BodyFlags
        public uint flags;

        public B2BodyType type;

        // This is monotonically advanced when a body is allocated in this slot
        // Used to check for invalid b2BodyId
        public ushort generation;

        // todo move into flags
        public bool enableSleep;
        public bool isSpeedCapped;
        public bool isMarked;
    }
}
