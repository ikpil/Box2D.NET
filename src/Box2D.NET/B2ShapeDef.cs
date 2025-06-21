// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Used to create a shape.
    /// This is a temporary object used to bundle shape creation parameters. You may use
    /// the same shape definition to create multiple shapes.
    /// Must be initialized using b2DefaultShapeDef().
    /// @ingroup shape
    public struct B2ShapeDef
    {
        /// Use this to store application specific shape data.
        public object userData;

        /// The surface material for this shape.
        public B2SurfaceMaterial material;

        /// The density, usually in kg/m^2.
        /// This is not part of the surface material because this is for the interior, which may have
        /// other considerations, such as being hollow. For example a wood barrel may be hollow or full of water.
        public float density;

        /// Collision filtering data.
        public B2Filter filter;

        /// A sensor shape generates overlap events but never generates a collision response.
        /// Sensors do not have continuous collision. Instead, use a ray or shape cast for those scenarios.
        /// Sensors still contribute to the body mass if they have non-zero density.
        /// @note Sensor events are disabled by default.
        /// @see enableSensorEvents
        public bool isSensor;

        /// Enable sensor events for this shape. This applies to sensors and non-sensors. False by default, even for sensors.
        public bool enableSensorEvents;

        /// Enable contact events for this shape. Only applies to kinematic and dynamic bodies. Ignored for sensors. False by default.
        public bool enableContactEvents;

        /// Enable hit events for this shape. Only applies to kinematic and dynamic bodies. Ignored for sensors. False by default.
        public bool enableHitEvents;

        /// Enable pre-solve contact events for this shape. Only applies to dynamic bodies. These are expensive
        /// and must be carefully handled due to multithreading. Ignored for sensors.
        public bool enablePreSolveEvents;

        /// When shapes are created they will scan the environment for collision the next time step. This can significantly slow down
        /// static body creation when there are many static shapes.
        /// This is flag is ignored for dynamic and kinematic shapes which always invoke contact creation.
        public bool invokeContactCreation;

        /// Should the body update the mass properties when this shape is created. Default is true.
        public bool updateBodyMass;

        /// Used internally to detect a valid definition. DO NOT SET.
        public int internalValue;
    }
}
