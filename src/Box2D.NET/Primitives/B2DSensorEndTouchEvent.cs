// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    /// An end touch event is generated when a shape stops overlapping a sensor shape.
    ///	These include things like setting the transform, destroying a body or shape, or changing
    ///	a filter. You will also get an end event if the sensor or visitor are destroyed.
    ///	Therefore you should always confirm the shape id is valid using b2Shape_IsValid.
    public class b2SensorEndTouchEvent
    {
        /// The id of the sensor shape
        ///	@warning this shape may have been destroyed
        ///	@see b2Shape_IsValid
        public b2ShapeId sensorShapeId;

        /// The id of the dynamic shape that stopped touching the sensor shape
        ///	@warning this shape may have been destroyed
        ///	@see b2Shape_IsValid
        public b2ShapeId visitorShapeId;

        public b2SensorEndTouchEvent()
        {
        }

        public b2SensorEndTouchEvent(b2ShapeId sensorShapeId, b2ShapeId visitorShapeId)
        {
            this.sensorShapeId = sensorShapeId;
            this.visitorShapeId = visitorShapeId;
        }
    }
}
