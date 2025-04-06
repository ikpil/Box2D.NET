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
        /// Location of the contact point in world space. Subject to precision loss at large coordinates.
        /// @note Should only be used for debugging.
        public B2Vec2 point;

        /// Location of the contact point relative to shapeA's origin in world space
        /// @note When used internally to the Box2D solver, this is relative to the body center of mass.
        public B2Vec2 anchorA;

        /// Location of the contact point relative to shapeB's origin in world space
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
        public float totalNormalImpulse;

        /// Relative normal velocity pre-solve. Used for hit events. If the normal impulse is
        /// zero then there was no hit. Negative means shapes are approaching.
        public float normalVelocity;

        /// Uniquely identifies a contact point between two shapes
        public ushort id;

        /// Did this contact point exist the previous step?
        public bool persisted;
    }
}
