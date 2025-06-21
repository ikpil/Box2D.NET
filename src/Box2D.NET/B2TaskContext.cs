// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Per thread task storage
    public class B2TaskContext
    {
        // Collect per thread sensor continuous hit events.
        public B2Array<B2SensorHit> sensorHits;
        
        // These bits align with the contact id capacity and signal a change in contact status
        public B2BitSet contactStateBitSet;

        // These bits align with the joint id capacity and signal a change in contact status
        public B2BitSet jointStateBitSet;

        // Used to track bodies with shapes that have enlarged AABBs. This avoids having a bit array
        // that is very large when there are many static shapes.
        public B2BitSet enlargedSimBitSet;

        // Used to put islands to sleep
        public B2BitSet awakeIslandBitSet;

        // Per worker split island candidate
        public float splitSleepTime;
        public int splitIslandId;
    }
}
