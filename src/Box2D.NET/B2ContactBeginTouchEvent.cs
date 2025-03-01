// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// A begin touch event is generated when two shapes begin touching.
    public class B2ContactBeginTouchEvent
    {
        /// Id of the first shape
        public B2ShapeId shapeIdA;

        /// Id of the second shape
        public B2ShapeId shapeIdB;

        /// The initial contact manifold. This is recorded before the solver is called,
        /// so all the impulses will be zero.
        public B2Manifold manifold;

        public B2ContactBeginTouchEvent()
        {
        }

        public B2ContactBeginTouchEvent(B2ShapeId shapeIdA, B2ShapeId shapeIdB, ref B2Manifold manifold)
        {
            this.shapeIdA = shapeIdA;
            this.shapeIdB = shapeIdB;
            this.manifold = manifold;
        }
    }
}
