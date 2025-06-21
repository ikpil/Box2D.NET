// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// A begin touch event is generated when two shapes begin touching.
    public struct B2ContactBeginTouchEvent
    {
        /// Id of the first shape
        public B2ShapeId shapeIdA;

        /// Id of the second shape
        public B2ShapeId shapeIdB;

        /// The transient contact id. This contact maybe destroyed automatically when the world is modified or simulated.
        /// Used b2Contact_IsValid before using this id.
        public B2ContactId contactId;

        public B2ContactBeginTouchEvent(B2ShapeId shapeIdA, B2ShapeId shapeIdB, B2ContactId contactId)
        {
            this.shapeIdA = shapeIdA;
            this.shapeIdB = shapeIdB;
            this.contactId = contactId;
        }
    }
}
