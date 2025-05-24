// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// This struct holds callbacks you can implement to draw a Box2D world.
    /// This structure should be zero initialized.
    /// @ingroup world
    public class B2DebugDraw
    {
        public DrawPolygonFcn DrawPolygonFcn;
        public DrawSolidPolygonFcn DrawSolidPolygonFcn;
        public DrawCircleFcn DrawCircleFcn;
        public DrawSolidCircleFcn DrawSolidCircleFcn;
        public DrawSolidCapsuleFcn DrawSolidCapsuleFcn;
        public DrawSegmentFcn DrawSegmentFcn;
        public DrawTransformFcn DrawTransformFcn;
        public DrawPointFcn DrawPointFcn;
        public DrawStringFcn DrawStringFcn;
        
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
        public bool drawBounds;

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

        /// Option to draw contact feature ids
        public bool drawContactFeatures;

        /// Option to draw contact friction impulses
        public bool drawFrictionImpulses;

        /// Option to draw islands as bounding boxes
        public bool drawIslands;

        /// User context that is passed as an argument to drawing callback functions
        public object context;
    }
}
