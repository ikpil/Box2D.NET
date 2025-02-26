﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Core;

namespace Box2D.NET.Primitives
{
    public class B2ContactConstraint
    {
        public int indexA;
        public int indexB;
        public UnsafeArray2<B2ContactConstraintPoint> points;
        public B2Vec2 normal;
        public float invMassA, invMassB;
        public float invIA, invIB;
        public float friction;
        public float restitution;
        public float tangentSpeed;
        public float rollingResistance;
        public float rollingMass;
        public float rollingImpulse;
        public B2Softness softness;
        public int pointCount;
    }
}
