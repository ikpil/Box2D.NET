// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Used to create a chain of line segments. This is designed to eliminate ghost collisions with some limitations.
    /// - chains are one-sided
    /// - chains have no mass and should be used on static bodies
    /// - chains have a counter-clockwise winding order (normal points right of segment direction)
    /// - chains are either a loop or open
    /// - a chain must have at least 4 points
    /// - the distance between any two points must be greater than B2_LINEAR_SLOP
    /// - a chain shape should not self intersect (this is not validated)
    /// - an open chain shape has NO COLLISION on the first and final edge
    /// - you may overlap two open chains on their first three and/or last three points to get smooth collision
    /// - a chain shape creates multiple line segment shapes on the body
    /// https://en.wikipedia.org/wiki/Polygonal_chain
    /// Must be initialized using b2DefaultChainDef().
    /// @warning Do not use chain shapes unless you understand the limitations. This is an advanced feature.
    /// @ingroup shape    
    public struct B2ChainDef
    {
        /// Use this to store application specific shape data.
        public object userData;

        /// An array of at least 4 points. These are cloned and may be temporary.
        public B2Vec2[] points;

        /// The point count, must be 4 or more.
        public int count;

        /// Surface materials for each segment. These are cloned.
        public B2SurfaceMaterial[] materials;

        /// The material count. Must be 1 or count. This allows you to provide one
        /// material for all segments or a unique material per segment.
        public int materialCount;

        /// Contact filtering data.
        public B2Filter filter;

        /// Indicates a closed chain formed by connecting the first and last points
        public bool isLoop;
        
        /// Enable sensors to detect this chain. False by default.
        public bool enableSensorEvents;

        /// Used internally to detect a valid definition. DO NOT SET.
        public int internalValue;
    }
}
