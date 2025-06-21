// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Constants;

namespace Box2D.NET
{
    public static class B2Cores
    {
        // note: I tried width of 1 and got no performance change
        public const int B2_SIMD_WIDTH = 4;

        /// Get the current version of Box2D
        public static B2Version b2GetVersion()
        {
            return new B2Version(
                major: 3,
                minor: 2,
                revision: 0
            );
        }

        // This allows the user to change the length units at runtime
        public static float b2_lengthUnitsPerMeter = 1.0f;

        /// Box2D bases all length units on meters, but you may need different units for your game.
        /// You can set this value to use different units. This should be done at application startup
        /// and only modified once. Default value is 1.
        /// For example, if your game uses pixels for units you can use pixels for all length values
        /// sent to Box2D. There should be no extra cost. However, Box2D has some internal tolerances
        /// and thresholds that have been tuned for meters. By calling this function, Box2D is able
        /// to adjust those tolerances and thresholds to improve accuracy.
        /// A good rule of thumb is to pass the height of your player character to this function. So
        /// if your player character is 32 pixels high, then pass 32 to this function. Then you may
        /// confidently use pixels for all the length values sent to Box2D. All length values returned
        /// from Box2D will also be pixels because Box2D does not do any scaling internally.
        /// However, you are now on the hook for coming up with good values for gravity, density, and
        /// forces.
        /// @warning This must be modified before any calls to Box2D
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void b2SetLengthUnitsPerMeter(float lengthUnits)
        {
            B2_ASSERT(b2IsValidFloat(lengthUnits) && lengthUnits > 0.0f);
            b2_lengthUnitsPerMeter = lengthUnits;
        }

        /// Get the current length units per meter.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float b2GetLengthUnitsPerMeter()
        {
            return b2_lengthUnitsPerMeter;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void B2_CHECK_DEF(ref B2WheelJointDef def)
        {
            B2_ASSERT(def.internalValue == B2_SECRET_COOKIE);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void B2_CHECK_DEF(ref B2WeldJointDef def)
        {
            B2_ASSERT(def.internalValue == B2_SECRET_COOKIE);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void B2_CHECK_DEF(ref B2PrismaticJointDef def)
        {
            B2_ASSERT(def.internalValue == B2_SECRET_COOKIE);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void B2_CHECK_DEF(ref B2RevoluteJointDef def)
        {
            B2_ASSERT(def.internalValue == B2_SECRET_COOKIE);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void B2_CHECK_DEF(ref b2FilterJointDef def)
        {
            B2_ASSERT(def.internalValue == B2_SECRET_COOKIE);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void B2_CHECK_DEF(ref B2MouseJointDef def)
        {
            B2_ASSERT(def.internalValue == B2_SECRET_COOKIE);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void B2_CHECK_DEF(ref B2MotorJointDef def)
        {
            B2_ASSERT(def.internalValue == B2_SECRET_COOKIE);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void B2_CHECK_DEF(ref B2DistanceJointDef def)
        {
            B2_ASSERT(def.internalValue == B2_SECRET_COOKIE);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void B2_CHECK_DEF(ref B2ChainDef def)
        {
            B2_ASSERT(def.internalValue == B2_SECRET_COOKIE);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void B2_CHECK_DEF(ref B2BodyDef def)
        {
            B2_ASSERT(def.internalValue == B2_SECRET_COOKIE);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void B2_CHECK_DEF(ref B2WorldDef def)
        {
            B2_ASSERT(def.internalValue == B2_SECRET_COOKIE);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void B2_CHECK_DEF(ref B2ShapeDef def)
        {
            B2_ASSERT(def.internalValue == B2_SECRET_COOKIE);
        }
    }
}