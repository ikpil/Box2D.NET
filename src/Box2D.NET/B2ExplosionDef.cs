// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// The explosion definition is used to configure options for explosions. Explosions
    /// consider shape geometry when computing the impulse.
    /// @ingroup world
    public struct B2ExplosionDef
    {
        /// Mask bits to filter shapes
        public ulong maskBits;

        /// The center of the explosion in world space
        public B2Vec2 position;

        /// The radius of the explosion
        public float radius;

        /// The falloff distance beyond the radius. Impulse is reduced to zero at this distance.
        public float falloff;

        /// Impulse per unit length. This applies an impulse according to the shape perimeter that
        /// is facing the explosion. Explosions only apply to circles, capsules, and polygons. This
        /// may be negative for implosions.
        public float impulsePerLength;
    }
}
