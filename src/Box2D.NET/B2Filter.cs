// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// This is used to filter collision on shapes. It affects shape-vs-shape collision
    /// and shape-versus-query collision (such as b2World_CastRay).
    /// @ingroup shape
    public struct B2Filter
    {
        /// The collision category bits. Normally you would just set one bit. The category bits should
        /// represent your application object types. For example:
        /// @code{.cpp}
        /// enum MyCategories
        /// {
        ///    Static  = 0x00000001,
        ///    Dynamic = 0x00000002,
        ///    Debris  = 0x00000004,
        ///    Player  = 0x00000008,
        ///    // etc
        /// };
        /// @endcode
        public ulong categoryBits;

        /// The collision mask bits. This states the categories that this
        /// shape would accept for collision.
        /// For example, you may want your player to only collide with static objects
        /// and other players.
        /// @code{.c}
        /// maskBits = Static | Player;
        /// @endcode
        public ulong maskBits;

        /// Collision groups allow a certain group of objects to never collide (negative)
        /// or always collide (positive). A group index of zero has no effect. Non-zero group filtering
        /// always wins against the mask bits.
        /// For example, you may want ragdolls to collide with other ragdolls but you don't want
        /// ragdoll self-collision. In this case you would give each ragdoll a unique negative group index
        /// and apply that group index to all shapes on the ragdoll.
        public int groupIndex;

        public B2Filter(ulong categoryBits, ulong maskBits, int groupIndex)
        {
            this.categoryBits = categoryBits;
            this.maskBits = maskBits;
            this.groupIndex = groupIndex;
        }
    }
}
