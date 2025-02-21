// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;

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
    public static class id
    {
        /// Use these to make your identifiers null.
        /// You may also use zero initialization to get null.
        public static readonly b2WorldId b2_nullWorldId = new b2WorldId(0, 0);

        public static readonly b2BodyId b2_nullBodyId = new b2BodyId(0, 0, 0);
        public static readonly b2ShapeId b2_nullShapeId = new b2ShapeId(0, 0, 0);
        public static readonly b2ChainId b2_nullChainId = new b2ChainId(0, 0, 0);
        public static readonly b2JointId b2_nullJointId = new b2JointId(0, 0, 0);

        /// Macro to determine if any id is null.
        public static bool B2_IS_NULL(b2WorldId id) => id.index1 == 0;

        public static bool B2_IS_NULL(b2BodyId id) => id.index1 == 0;
        public static bool B2_IS_NULL(b2ShapeId id) => id.index1 == 0;
        public static bool B2_IS_NULL(b2ChainId id) => id.index1 == 0;
        public static bool B2_IS_NULL(b2JointId id) => id.index1 == 0;

        /// Macro to determine if any id is non-null.
        public static bool B2_IS_NON_NULL(b2WorldId id) => id.index1 != 0;

        public static bool B2_IS_NON_NULL(b2BodyId id) => id.index1 != 0;
        public static bool B2_IS_NON_NULL(b2ShapeId id) => id.index1 != 0;
        public static bool B2_IS_NON_NULL(b2ChainId id) => id.index1 != 0;
        public static bool B2_IS_NON_NULL(b2JointId id) => id.index1 != 0;

        /// Compare two ids for equality. Doesn't work for b2WorldId.
        public static bool B2_ID_EQUALS(b2BodyId id1, b2BodyId id2) => id1.index1 == id2.index1 && id1.world0 == id2.world0 && id1.generation == id2.generation;

        /// Store a body id into a ulong.
        public static ulong b2StoreBodyId(b2BodyId id)
        {
            return ((ulong)id.index1 << 32) | ((ulong)id.world0) << 16 | (ulong)id.generation;
        }

        /// Load a ulong into a body id.
        public static b2BodyId b2LoadBodyId(ulong x)
        {
            b2BodyId id = new b2BodyId((int)(x >> 32), (ushort)(x >> 16), (ushort)(x));
            return id;
        }

        /// Store a shape id into a ulong.
        public static ulong b2StoreShapeId(b2ShapeId id)
        {
            return ((ulong)id.index1 << 32) | ((ulong)id.world0) << 16 | (ulong)id.generation;
        }

        /// Load a ulong into a shape id.
        public static b2ShapeId b2LoadShapeId(ulong x)
        {
            b2ShapeId id = new b2ShapeId((int)(x >> 32), (ushort)(x >> 16), (ushort)(x));
            return id;
        }

        /// Store a chain id into a ulong.
        public static ulong b2StoreChainId(b2ChainId id)
        {
            return ((ulong)id.index1 << 32) | ((ulong)id.world0) << 16 | (ulong)id.generation;
        }

        /// Load a ulong into a chain id.
        public static b2ChainId b2LoadChainId(ulong x)
        {
            b2ChainId id = new b2ChainId((int)(x >> 32), (ushort)(x >> 16), (ushort)(x));
            return id;
        }

        /// Store a joint id into a ulong.
        public static ulong b2StoreJointId(b2JointId id)
        {
            return ((ulong)id.index1 << 32) | ((ulong)id.world0) << 16 | (ulong)id.generation;
        }

        /// Load a ulong into a joint id.
        public static b2JointId b2LoadJointId(ulong x)
        {
            b2JointId id = new b2JointId((int)(x >> 32), (ushort)(x >> 16), (ushort)(x));
            return id;
        }

        /**@}*/
    }
}