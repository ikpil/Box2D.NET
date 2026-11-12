// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
        /**@}*/
    /**
     * @defgroup collision Collision
     * @brief Functions for colliding pairs of shapes
     * @{
     */
    /// A manifold point is a contact point belonging to a contact manifold.
    /// It holds details related to the geometry and dynamics of the contact points.
    /// Box2D uses speculative collision so some contact points may be separated.
    /// You may use the totalNormalImpulse to determine if there was an interaction during
    /// the time step.
    public struct B2ManifoldPoint
    {
        /// Location of the contact point relative to bodyA's center of mass in world space.
        /// This can be converted to a world point using:
        /// b2Pos worldPointA = b2OffsetPos(b2Body_GetWorldCenter(myBodyIdA), anchorA);
        /// @note When used internally to the Box2D solver, this is relative to the body center of mass.
        public B2Vec2 anchorA;

        /// Location of the contact point relative to bodyB's center of mass in world space.
        /// This can be converted to a world point using:
        /// b2Pos worldPointB = b2OffsetPos(b2Body_GetWorldCenter(myBodyIdB), anchorB);
        /// @note When used internally to the Box2D solver, this is relative to the body center of mass.
        public B2Vec2 anchorB;

        /// The separation of the contact point, negative if penetrating
        public float separation;
        
        /// The impulse along the manifold normal vector.
        public float normalImpulse;

        /// The friction impulse
        public float tangentImpulse;

        /// The total normal impulse applied across sub-stepping and restitution. This is important
        /// to identify speculative contact points that had an interaction in the time step.
        /// This includes the warm starting impulse, the sub-step delta impulse, and the restitution
        /// impulse.
        public float totalNormalImpulse;

        /// Relative normal velocity pre-solve. Negative when approaching. This is only
        /// computed if hit events are enabled. It may be computed otherwise as a side effect
        /// of SIMD bundling, but that is not guaranteed.
        public float normalVelocity;

        /// Cached separation used for contact recycling
        public float baseSeparation;

        /// Uniquely identifies a contact point between two shapes
        public ushort id;

        /// Did this contact point exist the previous step?
        public bool persisted;
    }
}
