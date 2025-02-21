namespace Box2D.NET.Primitives
{
    // Body simulation data used for integration of position and velocity
    // Transform data used for collision and solver preparation.
    public class b2BodySim
    {
        // todo better to have transform in sim or in @base body? Try both!
        // transform for body origin
        public b2Transform transform;

        // center of mass position in world space
        public b2Vec2 center;

        // previous rotation and COM for TOI
        public b2Rot rotation0;
        public b2Vec2 center0;

        // location of center of mass relative to the body origin
        public b2Vec2 localCenter;

        public b2Vec2 force;
        public float torque;

        // inverse inertia
        public float invMass;
        public float invInertia;

        public float minExtent;
        public float maxExtent;
        public float linearDamping;
        public float angularDamping;
        public float gravityScale;

        // body data can be moved around, the id is stable (used in b2BodyId)
        public int bodyId;

        // This flag is used for debug draw
        public bool isFast;

        public bool isBullet;
        public bool isSpeedCapped;
        public bool allowFastRotation;
        public bool enlargeAABB;

        public void Clear()
        {
            transform = new b2Transform();

            center = new b2Vec2();

            rotation0 = new b2Rot();
            center0 = new b2Vec2();

            localCenter = new b2Vec2();

            force = new b2Vec2();
            torque = 0.0f;

            invMass = 0.0f;
            invInertia = 0.0f;

            minExtent = 0.0f;
            maxExtent = 0.0f;
            linearDamping = 0.0f;
            angularDamping = 0.0f;
            gravityScale = 0.0f;

            bodyId = 0;

            isFast = false;
            isBullet = false;

            isSpeedCapped = false;
            allowFastRotation = false;
            enlargeAABB = false;
        }

        public void CopyFrom(b2BodySim other)
        {
            transform = other.transform;

            center = other.center;

            rotation0 = other.rotation0;
            center0 = other.center0;

            localCenter = other.localCenter;

            force = other.force;
            torque = other.torque;

            invMass = other.invMass;
            invInertia = other.invInertia;

            minExtent = other.minExtent;
            maxExtent = other.maxExtent;
            linearDamping = other.linearDamping;
            angularDamping = other.angularDamping;
            gravityScale = other.gravityScale;

            bodyId = other.bodyId;

            isFast = other.isFast;
            isBullet = other.isBullet;

            isSpeedCapped = other.isSpeedCapped;
            allowFastRotation = other.allowFastRotation;
            enlargeAABB = other.enlargeAABB;
        }
    }
}