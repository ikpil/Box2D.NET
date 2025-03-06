﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// The contact data for two shapes. By convention the manifold normal points
    /// from shape A to shape B.
    /// @see b2Shape_GetContactData() and b2Body_GetContactData()
    public struct B2ContactData
    {
        public B2ShapeId shapeIdA;
        public B2ShapeId shapeIdB;
        public B2Manifold manifold;
    }
}
