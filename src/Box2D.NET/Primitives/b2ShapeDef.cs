// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    /// Used to create a shape.
    /// This is a temporary object used to bundle shape creation parameters. You may use
    /// the same shape definition to create multiple shapes.
    /// Must be initialized using b2DefaultShapeDef().
    /// @ingroup shape
    public class b2ShapeDef
    {
        /// Use this to store application specific shape data.
        public object userData;

        /// The Coulomb (dry) friction coefficient, usually in the range [0,1].
        public float friction;

        /// The coefficient of restitution (bounce) usually in the range [0,1].
        /// https://en.wikipedia.org/wiki/Coefficient_of_restitution
        public float restitution;

        /// The rolling resistance usually in the range [0,1].
        public float rollingResistance;

        /// The tangent speed for conveyor belts
        public float tangentSpeed;

        /// User material identifier. This is passed with query results and to friction and restitution
        /// combining functions. It is not used internally.
        public int material;

        /// The density, usually in kg/m^2.
        public float density;

        /// Collision filtering data.
        public b2Filter filter;

        /// Custom debug draw color.
        public uint customColor;

        /// A sensor shape generates overlap events but never generates a collision response.
        /// Sensors do not collide with other sensors and do not have continuous collision.
        /// Instead, use a ray or shape cast for those scenarios.
        public bool isSensor;

        /// Enable contact events for this shape. Only applies to kinematic and dynamic bodies. Ignored for sensors.
        public bool enableContactEvents;

        /// Enable hit events for this shape. Only applies to kinematic and dynamic bodies. Ignored for sensors.
        public bool enableHitEvents;

        /// Enable pre-solve contact events for this shape. Only applies to dynamic bodies. These are expensive
        /// and must be carefully handled due to threading. Ignored for sensors.
        public bool enablePreSolveEvents;

        /// Normally shapes on static bodies don't invoke contact creation when they are added to the world. This overrides
        /// that behavior and causes contact creation. This significantly slows down static body creation which can be important
        /// when there are many static shapes.
        /// This is implicitly always true for sensors, dynamic bodies, and kinematic bodies.
        public bool invokeContactCreation;

        /// Should the body update the mass properties when this shape is created. Default is true.
        public bool updateBodyMass;

        /// Used internally to detect a valid definition. DO NOT SET.
        public int internalValue;
    }
}
