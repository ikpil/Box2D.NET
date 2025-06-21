// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// An end touch event is generated when a shape stops overlapping a sensor shape.
    ///	These include things like setting the transform, destroying a body or shape, or changing
    ///	a filter. You will also get an end event if the sensor or visitor are destroyed.
    ///	Therefore you should always confirm the shape id is valid using b2Shape_IsValid.
    public struct B2SensorEndTouchEvent
    {
        /// The id of the sensor shape
        ///	@warning this shape may have been destroyed
        ///	@see b2Shape_IsValid
        public B2ShapeId sensorShapeId;

        /// The id of the shape that stopped touching the sensor shape
        ///	@warning this shape may have been destroyed
        ///	@see b2Shape_IsValid
        public B2ShapeId visitorShapeId;

        public B2SensorEndTouchEvent(B2ShapeId sensorShapeId, B2ShapeId visitorShapeId)
        {
            this.sensorShapeId = sensorShapeId;
            this.visitorShapeId = visitorShapeId;
        }
    }
}
