// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// An end touch event is generated when two shapes stop touching.
    ///	You will get an end event if you do anything that destroys contacts previous to the last
    ///	world step. These include things like setting the transform, destroying a body
    ///	or shape, or changing a filter or body type.
    public struct B2ContactEndTouchEvent
    {
        /// Id of the first shape
        ///	@warning this shape may have been destroyed
        ///	@see b2Shape_IsValid
        public B2ShapeId shapeIdA;

        /// Id of the second shape
        ///	@warning this shape may have been destroyed
        ///	@see b2Shape_IsValid
        public B2ShapeId shapeIdB;
        
        /// Id of the contact.
        ///	@warning this contact may have been destroyed
        ///	@see b2Contact_IsValid
        public B2ContactId contactId;

        public B2ContactEndTouchEvent(B2ShapeId shapeIdA, B2ShapeId shapeIdB, B2ContactId contactId)
        {
            this.shapeIdA = shapeIdA;
            this.shapeIdB = shapeIdB;
            this.contactId = contactId;
        }
    }
}
