// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Surface materials allow chain shapes to have per segment surface properties.
    /// @ingroup shape
    public struct B2SurfaceMaterial
    {
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
        public int userMaterialId;

        /// Custom debug draw color.
        public uint customColor;
    }
}
