﻿// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Cores;
using static Box2D.NET.B2MathFunction;

namespace Box2D.NET
{
    public static class B2Types
    {
        /// Use this to initialize your world definition
        /// @ingroup world
        public static B2WorldDef b2DefaultWorldDef()
        {
            B2WorldDef def = new B2WorldDef();
            def.gravity.X = 0.0f;
            def.gravity.Y = -10.0f;
            def.hitEventThreshold = 1.0f * b2_lengthUnitsPerMeter;
            def.restitutionThreshold = 1.0f * b2_lengthUnitsPerMeter;
            def.contactPushMaxSpeed = 3.0f * b2_lengthUnitsPerMeter;
            def.contactHertz = 30.0f;
            def.contactDampingRatio = 10.0f;
            def.jointHertz = 60.0f;
            def.jointDampingRatio = 2.0f;
            // 400 meters per second, faster than the speed of sound
            def.maximumLinearSpeed = 400.0f * b2_lengthUnitsPerMeter;
            def.enableSleep = true;
            def.enableContinuous = true;
            def.internalValue = B2_SECRET_COOKIE;
            return def;
        }

        /// Use this to initialize your body definition
        /// @ingroup body
        public static B2BodyDef b2DefaultBodyDef()
        {
            B2BodyDef def = new B2BodyDef();
            def.type = B2BodyType.b2_staticBody;
            def.rotation = b2Rot_identity;
            def.sleepThreshold = 0.05f * b2_lengthUnitsPerMeter;
            def.gravityScale = 1.0f;
            def.enableSleep = true;
            def.isAwake = true;
            def.isEnabled = true;
            def.internalValue = B2_SECRET_COOKIE;
            return def;
        }

        /// Use this to initialize your filter
        /// @ingroup shape
        public static B2Filter b2DefaultFilter()
        {
            B2Filter filter = new B2Filter(B2_DEFAULT_CATEGORY_BITS, B2_DEFAULT_MASK_BITS, 0);
            return filter;
        }

        /// Use this to initialize your query filter
        /// @ingroup shape
        public static B2QueryFilter b2DefaultQueryFilter()
        {
            B2QueryFilter filter = new B2QueryFilter(B2_DEFAULT_CATEGORY_BITS, B2_DEFAULT_MASK_BITS);
            return filter;
        }

        /// Use this to initialize your shape definition
        /// @ingroup shape
        public static B2ShapeDef b2DefaultShapeDef()
        {
            B2ShapeDef def = new B2ShapeDef();
            def.friction = 0.6f;
            def.density = 1.0f;
            def.filter = b2DefaultFilter();
            def.updateBodyMass = true;
            def.internalValue = B2_SECRET_COOKIE;
            return def;
        }

        /// Use this to initialize your surface material
        /// @ingroup shape
        public static B2SurfaceMaterial b2DefaultSurfaceMaterial()
        {
            B2SurfaceMaterial material = new B2SurfaceMaterial();
            material.friction = 0.6f;

            return material;
        }
        
        /// Use this to initialize your chain definition
        /// @ingroup shape
        public static B2ChainDef b2DefaultChainDef()
        {
            B2SurfaceMaterial defaultMaterial = new B2SurfaceMaterial();
            defaultMaterial.friction = 0.6f;

            B2ChainDef def = new B2ChainDef();
            def.materials = new B2SurfaceMaterial[] { defaultMaterial };
            def.materialCount = 1;
            def.filter = b2DefaultFilter();
            def.internalValue = B2_SECRET_COOKIE;
            return def;
        }

        public static void b2EmptyDrawPolygon(ReadOnlySpan<B2Vec2> vertices, int vertexCount, B2HexColor color, object context)
        {
            B2_UNUSED((B2Vec2[])null, vertexCount, color, context);
        }

        public static void b2EmptyDrawSolidPolygon(ref B2Transform transform, ReadOnlySpan<B2Vec2> vertices, int vertexCount, float radius, B2HexColor color, object context)
        {
            B2_UNUSED(transform, (B2Vec2[])null, vertexCount, radius, color, context);
        }

        public static void b2EmptyDrawCircle(B2Vec2 center, float radius, B2HexColor color, object context)
        {
            B2_UNUSED(center, radius, color, context);
        }

        public static void b2EmptyDrawSolidCircle(ref B2Transform transform, float radius, B2HexColor color, object context)
        {
            B2_UNUSED(transform, radius, color, context);
        }

        public static void b2EmptyDrawSolidCapsule(B2Vec2 p1, B2Vec2 p2, float radius, B2HexColor color, object context)
        {
            B2_UNUSED(p1, p2, radius, color, context);
        }

        public static void b2EmptyDrawSegment(B2Vec2 p1, B2Vec2 p2, B2HexColor color, object context)
        {
            B2_UNUSED(p1, p2, color, context);
        }

        public static void b2EmptyDrawTransform(B2Transform transform, object context)
        {
            B2_UNUSED(transform, context);
        }

        public static void b2EmptyDrawPoint(B2Vec2 p, float size, B2HexColor color, object context)
        {
            B2_UNUSED(p, size, color, context);
        }

        public static void b2EmptyDrawString(B2Vec2 p, string s, B2HexColor color, object context)
        {
            B2_UNUSED(p, s, color, context);
        }

        /// Use this to initialize your drawing interface. This allows you to implement a sub-set
        /// of the drawing functions.
        public static B2DebugDraw b2DefaultDebugDraw()
        {
            B2DebugDraw draw = new B2DebugDraw();

            // These allow the user to skip some implementations and not hit null exceptions.
            draw.DrawPolygon = b2EmptyDrawPolygon;
            draw.DrawSolidPolygon = b2EmptyDrawSolidPolygon;
            draw.DrawCircle = b2EmptyDrawCircle;
            draw.DrawSolidCircle = b2EmptyDrawSolidCircle;
            draw.DrawSolidCapsule = b2EmptyDrawSolidCapsule;
            draw.DrawSegment = b2EmptyDrawSegment;
            draw.DrawTransform = b2EmptyDrawTransform;
            draw.DrawPoint = b2EmptyDrawPoint;
            draw.DrawString = b2EmptyDrawString;
            return draw;
        }
    }
}
