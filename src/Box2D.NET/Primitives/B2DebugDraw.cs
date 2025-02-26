// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;

namespace Box2D.NET.Primitives
{
    /// This struct holds callbacks you can implement to draw a Box2D world.
    /// This structure should be zero initialized.
    /// @ingroup world
    public class B2DebugDraw
    {
        /// Draw a closed polygon provided in CCW order.
        //void ( *DrawPolygon )( const b2Vec2* vertices, int vertexCount, b2HexColor color, object context );
        public delegate void DrawPolygonDelegate(ReadOnlySpan<B2Vec2> vertices, int vertexCount, B2HexColor color, object context);

        public DrawPolygonDelegate DrawPolygon;

        /// Draw a solid closed polygon provided in CCW order.
        public delegate void DrawSolidPolygonDelegate(ref B2Transform transform, ReadOnlySpan<B2Vec2> vertices, int vertexCount, float radius, B2HexColor color, object context);

        public DrawSolidPolygonDelegate DrawSolidPolygon;

        /// Draw a circle.
        public delegate void DrawCircleDelegate(B2Vec2 center, float radius, B2HexColor color, object context);

        public DrawCircleDelegate DrawCircle;

        /// Draw a solid circle.
        public delegate void DrawSolidCircleDelegate(ref B2Transform transform, float radius, B2HexColor color, object context);

        public DrawSolidCircleDelegate DrawSolidCircle;

        /// Draw a solid capsule.
        public delegate void DrawSolidCapsuleDelegate(B2Vec2 p1, B2Vec2 p2, float radius, B2HexColor color, object context);

        public DrawSolidCapsuleDelegate DrawSolidCapsule;

        /// Draw a line segment.
        public delegate void DrawSegmentDelegate(B2Vec2 p1, B2Vec2 p2, B2HexColor color, object context);

        public DrawSegmentDelegate DrawSegment;

        /// Draw a transform. Choose your own length scale.
        public delegate void DrawTransformDelegate(B2Transform transform, object context);

        public DrawTransformDelegate DrawTransform;

        /// Draw a point.
        public delegate void DrawPointDelegate(B2Vec2 p, float size, B2HexColor color, object context);

        public DrawPointDelegate DrawPoint;

        /// Draw a string in world space
        public delegate void DrawStringDelegate(B2Vec2 p, string s, B2HexColor color, object context);

        public DrawStringDelegate DrawString;

        /// Bounds to use if restricting drawing to a rectangular region
        public B2AABB drawingBounds;

        /// Option to restrict drawing to a rectangular region. May suffer from unstable depth sorting.
        public bool useDrawingBounds;

        /// Option to draw shapes
        public bool drawShapes;

        /// Option to draw joints
        public bool drawJoints;

        /// Option to draw additional information for joints
        public bool drawJointExtras;

        /// Option to draw the bounding boxes for shapes
        public bool drawAABBs;

        /// Option to draw the mass and center of mass of dynamic bodies
        public bool drawMass;

        /// Option to draw body names
        public bool drawBodyNames;

        /// Option to draw contact points
        public bool drawContacts;

        /// Option to visualize the graph coloring used for contacts and joints
        public bool drawGraphColors;

        /// Option to draw contact normals
        public bool drawContactNormals;

        /// Option to draw contact normal impulses
        public bool drawContactImpulses;

        /// Option to draw contact friction impulses
        public bool drawFrictionImpulses;

        /// User context that is passed as an argument to drawing callback functions
        public object context;
    }
}
