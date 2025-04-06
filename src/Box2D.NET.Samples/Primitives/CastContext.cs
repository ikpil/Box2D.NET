﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Samples.Primitives;

// Context for ray cast callbacks. Do what you want with this.
public class CastContext
{
    public B2FixedArray3<B2Vec2> points;
    public B2FixedArray3<B2Vec2> normals;
    public B2FixedArray3<float> fractions;
    public int count;
};
