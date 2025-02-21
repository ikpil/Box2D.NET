using System;

namespace Box2D.NET.Primitives
{
    public class b2GraphColor
    {
        // This bitset is indexed by bodyId so this is over-sized to encompass static bodies
        // however I never traverse these bits or use the bit count for anything
        // This bitset is unused on the overflow color.
        public b2BitSet bodySet;

        // cache friendly arrays
        public b2Array<b2ContactSim> contactSims;
        public b2Array<b2JointSim> jointSims;

        // TODO: @ikpil, check union
        // transient
        //union
        //{
        public ArraySegment<b2ContactConstraintSIMD> simdConstraints;
        public ArraySegment<b2ContactConstraint> overflowConstraints;
        //};
    }
}