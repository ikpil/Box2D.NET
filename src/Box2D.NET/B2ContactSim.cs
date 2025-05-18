// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// The class manages contact between two shapes. A contact exists for each overlapping
    /// AABB in the broad-phase (except if filtered). Therefore a contact object may exist
    /// that has no contact points.
    public class B2ContactSim
    {
        public int contactId;

#if DEBUG
        public int bodyIdA;
        public int bodyIdB;
#endif

        public int bodySimIndexA;
        public int bodySimIndexB;

        public int shapeIdA;
        public int shapeIdB;

        public float invMassA;
        public float invIA;

        public float invMassB;
        public float invIB;

        public B2Manifold manifold;

        // Mixed friction and restitution
        public float friction;
        public float restitution;
        public float rollingResistance;
        public float tangentSpeed;

        // b2ContactSimFlags
        public uint simFlags;

        public B2SimplexCache cache;

        public void CopyFrom(B2ContactSim other)
        {
            contactId = other.contactId;

#if DEBUG
            bodyIdA = other.bodyIdA;
            bodyIdB = other.bodyIdB;
#endif

            bodySimIndexA = other.bodySimIndexA;
            bodySimIndexB = other.bodySimIndexB;

            shapeIdA = other.shapeIdA;
            shapeIdB = other.shapeIdB;

            invMassA = other.invMassA;
            invIA = other.invIA;

            invMassB = other.invMassB;
            invIB = other.invIB;

            manifold = other.manifold;

            friction = other.friction;
            restitution = other.restitution;
            rollingResistance = other.rollingResistance;
            tangentSpeed = other.tangentSpeed;

            simFlags = other.simFlags;

            cache = other.cache;
        }
    }
}
