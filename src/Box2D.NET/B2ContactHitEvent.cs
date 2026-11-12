// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// A hit touch event is generated when two shapes collide with a speed faster than the hit speed threshold.
    /// This may be reported for speculative contacts that have a confirmed impulse.
    public struct B2ContactHitEvent
    {
        /// Id of the first shape
        public B2ShapeId shapeIdA;

        /// Id of the second shape
        public B2ShapeId shapeIdB;

        /// Id of the contact.
        /// @warning this contact may have been destroyed
        /// @see b2Contact_IsValid
        public B2ContactId contactId;
        
        /// Point where the shapes hit. This is a mid-point between the two surfaces. It could be
        /// a speculative point where the shapes were not touching at the beginning of the time
        /// step. The point is reconstructed after the step, so when both bodies are moving it may
        /// trail the impact by up to one step of motion.
        public B2Pos point;

        /// Normal vector pointing from shape A to shape B
        public B2Vec2 normal;

        /// The speed the shapes are approaching. Always positive. Typically in meters per second.
        public float approachSpeed;
    }
}
