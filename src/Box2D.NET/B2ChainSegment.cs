﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2MathFunction;

namespace Box2D.NET
{
    /// A line segment with one-sided collision. Only collides on the right side.
    /// Several of these are generated for a chain shape.
    /// ghost1 -> point1 -> point2 -> ghost2
    public class B2ChainSegment
    {
        /// The tail ghost vertex
        public B2Vec2 ghost1;

        /// The line segment
        public B2Segment segment = new B2Segment(b2Vec2_zero, b2Vec2_zero);

        /// The head ghost vertex
        public B2Vec2 ghost2;

        /// The owning chain shape index (internal usage only)
        public int chainId;

        public B2ChainSegment Clone()
        {
            var chainSegment = new B2ChainSegment();
            chainSegment.ghost1 = ghost1;
            chainSegment.segment = segment.Clone();
            chainSegment.ghost2 = ghost2;
            chainSegment.chainId = chainId;
            return chainSegment;
        }
    }
}
