// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;

namespace Box2D.NET
{
    public struct B2GraphColor
    {
        // This bitset is indexed by bodyId so this is over-sized to encompass static bodies
        // however I never traverse these bits or use the bit count for anything
        // This bitset is unused on the overflow color.
        // todo consider having a uint_16 per body that tracks the graph color membership
        public B2BitSet bodySet;

        // cache friendly arrays
        public B2Array<B2ContactSim> contactSims;
        public B2Array<B2JointSim> jointSims;

        // TODO: @ikpil, check union
        // transient
        // union
        //{
        public ArraySegment<B2ContactConstraintSIMD> simdConstraints;
        public ArraySegment<B2ContactConstraint> overflowConstraints;
        //};
    }
}
