using System.Diagnostics;

namespace Box2D.NET.Primitives
{
    /// Surface materials allow chain shapes to have per segment surface properties.
    /// @ingroup shape
    public class b2SurfaceMaterial // todo: @ikpil, class or struct
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
        public int material;

        /// Custom debug draw color.
        public uint customColor;

        public b2SurfaceMaterial Clone()
        {
            Debug.Assert(false);
            return null;
        }
    }
}