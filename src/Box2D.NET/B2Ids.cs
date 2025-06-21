// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;

namespace Box2D.NET
{
    /**
     * @defgroup id Ids
     * These ids serve as handles to internal Box2D objects.
     * These should be considered opaque data and passed by value.
     * Include this header if you need the id types and not the whole Box2D API.
     * All ids are considered null if initialized to zero.
     *
     * For example in C++:
     *
     * @code{.cxx}
     * b2WorldId worldId = {};
     * @endcode
     *
     * Or in C:
     *
     * @code{.c}
     * b2WorldId worldId = {0};
     * @endcode
     *
     * These are both considered null.
     *
     * @warning Do not use the internals of these ids. They are subject to change. Ids should be treated as opaque objects.
     * @warning You should use ids to access objects in Box2D. Do not access files within the src folder. Such usage is unsupported.
     * @{
     */
    public static class B2Ids
    {
        /// Use these to make your identifiers null.
        /// You may also use zero initialization to get null.
        public static readonly B2WorldId b2_nullWorldId = new B2WorldId(0, 0);
        public static readonly B2BodyId b2_nullBodyId = new B2BodyId(0, 0, 0);
        public static readonly B2ShapeId b2_nullShapeId = new B2ShapeId(0, 0, 0);
        public static readonly B2ChainId b2_nullChainId = new B2ChainId(0, 0, 0);
        public static readonly B2JointId b2_nullJointId = new B2JointId(0, 0, 0);
        public static readonly B2ContactId b2_nullContactId = new B2ContactId(0, 0, 0, 0);


        /// Macro to determine if any id is null.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool B2_IS_NULL(B2WorldId id) => id.index1 == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool B2_IS_NULL(B2BodyId id) => id.index1 == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool B2_IS_NULL(B2ShapeId id) => id.index1 == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool B2_IS_NULL(B2ChainId id) => id.index1 == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool B2_IS_NULL(B2JointId id) => id.index1 == 0;

        /// Macro to determine if any id is non-null.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool B2_IS_NON_NULL(B2WorldId id) => id.index1 != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool B2_IS_NON_NULL(B2BodyId id) => id.index1 != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool B2_IS_NON_NULL(B2ShapeId id) => id.index1 != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool B2_IS_NON_NULL(B2ChainId id) => id.index1 != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool B2_IS_NON_NULL(B2JointId id) => id.index1 != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool B2_IS_NON_NULL(B2ContactId id) => id.index1 != 0;

        /// Compare two ids for equality. Doesn't work for b2WorldId. Don't mix types.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool B2_ID_EQUALS(B2BodyId id1, B2BodyId id2) => id1.index1 == id2.index1 && id1.world0 == id2.world0 && id1.generation == id2.generation;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool B2_ID_EQUALS(B2ShapeId id1, B2ShapeId id2) => id1.index1 == id2.index1 && id1.world0 == id2.world0 && id1.generation == id2.generation;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool B2_ID_EQUALS(B2ContactId id1, B2ContactId id2) => id1.index1 == id2.index1 && id1.world0 == id2.world0 && id1.generation == id2.generation;

        /// Store a world id into a uint32_t.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint b2StoreWorldId(B2WorldId id)
        {
            return ((uint)id.index1 << 16) | (uint)id.generation;
        }

        /// Load a uint32_t into a world id.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2WorldId b2LoadWorldId(uint x)
        {
            B2WorldId id = new B2WorldId((ushort)(x >> 16), (ushort)(x));
            return id;
        }

        /// Store a body id into a ulong.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong b2StoreBodyId(B2BodyId id)
        {
            return ((ulong)id.index1 << 32) | ((ulong)id.world0) << 16 | (ulong)id.generation;
        }

        /// Load a ulong into a body id.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2BodyId b2LoadBodyId(ulong x)
        {
            B2BodyId id = new B2BodyId((int)(x >> 32), (ushort)(x >> 16), (ushort)(x));
            return id;
        }

        /// Store a shape id into a ulong.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong b2StoreShapeId(B2ShapeId id)
        {
            return ((ulong)id.index1 << 32) | ((ulong)id.world0) << 16 | (ulong)id.generation;
        }

        /// Load a ulong into a shape id.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2ShapeId b2LoadShapeId(ulong x)
        {
            B2ShapeId id = new B2ShapeId((int)(x >> 32), (ushort)(x >> 16), (ushort)(x));
            return id;
        }

        /// Store a chain id into a ulong.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong b2StoreChainId(B2ChainId id)
        {
            return ((ulong)id.index1 << 32) | ((ulong)id.world0) << 16 | (ulong)id.generation;
        }

        /// Load a ulong into a chain id.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2ChainId b2LoadChainId(ulong x)
        {
            B2ChainId id = new B2ChainId((int)(x >> 32), (ushort)(x >> 16), (ushort)(x));
            return id;
        }

        /// Store a joint id into a ulong.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong b2StoreJointId(B2JointId id)
        {
            return ((ulong)id.index1 << 32) | ((ulong)id.world0) << 16 | (ulong)id.generation;
        }

        /// Load a ulong into a joint id.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2JointId b2LoadJointId(ulong x)
        {
            B2JointId id = new B2JointId((int)(x >> 32), (ushort)(x >> 16), (ushort)(x));
            return id;
        }

        /// Store a contact id into 16 bytes
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void b2StoreContactId(B2ContactId id, Span<uint> values)
        {
            values[0] = (uint)id.index1;
            values[1] = (uint)id.world0;
            values[2] = (uint)id.generation;
        }

        /// Load a two uint64_t into a contact id.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2ContactId b2LoadContactId(Span<uint> values)
        {
            B2ContactId id = new B2ContactId((int)values[0], (ushort)values[1], 0, values[2]);
            // id.index1 = (int32_t)values[0];
            // id.world0 = (uint16_t)values[1];
            // id.padding = 0;
            // id.generation = (uint32_t)values[2];
            return id;
        }

        /**@}*/
    }
}