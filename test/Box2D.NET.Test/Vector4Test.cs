// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using NUnit.Framework;
using static Box2D.NET.B2ContactSolvers;

namespace Box2D.NET.Test;

public class Vector4Test
{
    public static void Source(out Vector<float> v1, out Vector<float> v2, out Vector<float> v3, out Vector4 b1, out Vector4 b2, out Vector4 b3)
    {
        var random = new Random((int)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond));
        var source = new float[Vector<float>.Count * 3];
        for (int i = 0; i < source.Length; ++i)
        {
            source[i] = random.NextSingle();
        }

        var a = source.AsSpan(0, Vector<float>.Count);
        var b = source.AsSpan(Vector<float>.Count, Vector<float>.Count);
        var c = source.AsSpan(Vector<float>.Count * 2, Vector<float>.Count);

        a[0] = -a[0];
        c[2] = -c[2];

        a[3] = 0;
        b[3] = 0;
        c[3] = 0;

        // for SIMD
        v1 = new Vector<float>(a);
        v2 = new Vector<float>(b);
        v3 = new Vector<float>(c);

        // for b2
        b1 = new Vector4(a[0], a[1], a[2], a[3]);
        b2 = new Vector4(b[0], b[1], b[2], b[3]);
        b3 = new Vector4(c[0], c[1], c[2], c[3]);
    }

    [Test]
    public void TestOperators()
    {
        Source(out var v1, out var v2, out var v3, out var b1, out var b2, out var b3);

        // add
        {
            Vector4 b = b2AddW(b1, b2);
            Vector<float> v = b2AddW(v1, v2);

            Assert.That(b.X, Is.EqualTo(v[0]));
            Assert.That(b.Y, Is.EqualTo(v[1]));
            Assert.That(b.Z, Is.EqualTo(v[2]));
            Assert.That(b.W, Is.EqualTo(v[3]));
        }

        // sub
        {
            Vector4 b = b2SubW(b1, b2);
            Vector<float> v = b2SubW(v1, v2);

            Assert.That(b.X, Is.EqualTo(v[0]));
            Assert.That(b.Y, Is.EqualTo(v[1]));
            Assert.That(b.Z, Is.EqualTo(v[2]));
            Assert.That(b.W, Is.EqualTo(v[3]));
        }

        // mul
        {
            Vector4 b = b2MulW(b1, b2);
            Vector<float> v = b2MulW(v1, v2);

            Assert.That(b.X, Is.EqualTo(v[0]));
            Assert.That(b.Y, Is.EqualTo(v[1]));
            Assert.That(b.Z, Is.EqualTo(v[2]));
            Assert.That(b.W, Is.EqualTo(v[3]));
        }

        // mul and add
        {
            Vector4 b = b2MulAddW(b1, b2, b3);
            Vector<float> v = b2MulAddW(v1, v2, v3);

            Assert.That(b.X, Is.EqualTo(v[0]));
            Assert.That(b.Y, Is.EqualTo(v[1]));
            Assert.That(b.Z, Is.EqualTo(v[2]));
            Assert.That(b.W, Is.EqualTo(v[3]));
        }

        // mul and sub
        {
            Vector4 b = b2MulSubW(b1, b2, b3);
            Vector<float> v = b2MulSubW(v1, v2, v3);

            Assert.That(b.X, Is.EqualTo(v[0]));
            Assert.That(b.Y, Is.EqualTo(v[1]));
            Assert.That(b.Z, Is.EqualTo(v[2]));
            Assert.That(b.W, Is.EqualTo(v[3]));
        }

        // min
        {
            Vector4 b = b2MinW(b1, b2);
            Vector<float> v = b2MinW(v1, v2);

            Assert.That(b.X, Is.EqualTo(v[0]));
            Assert.That(b.Y, Is.EqualTo(v[1]));
            Assert.That(b.Z, Is.EqualTo(v[2]));
            Assert.That(b.W, Is.EqualTo(v[3]));
        }

        // max 
        {
            Vector4 b = b2MaxW(b1, b2);
            Vector<float> v = b2MaxW(v1, v2);

            Assert.That(b.X, Is.EqualTo(v[0]));
            Assert.That(b.Y, Is.EqualTo(v[1]));
            Assert.That(b.Z, Is.EqualTo(v[2]));
            Assert.That(b.W, Is.EqualTo(v[3]));
        }

        // clamp
        {
            Vector4 b = b2SymClampW(b1, b2);
            Vector<float> v = b2SymClampW(v1, v2);

            Assert.That(b.X, Is.EqualTo(v[0]));
            Assert.That(b.Y, Is.EqualTo(v[1]));
            Assert.That(b.Z, Is.EqualTo(v[2]));
            Assert.That(b.W, Is.EqualTo(v[3]));
        }

        // greater than
        {
            Vector4 b = b2GreaterThanW(b1, b2);
            Vector<float> v = b2GreaterThanW(v1, v2);

            Assert.That(b.X, Is.EqualTo(v[0]));
            Assert.That(b.Y, Is.EqualTo(v[1]));
            Assert.That(b.Z, Is.EqualTo(v[2]));
            Assert.That(b.W, Is.EqualTo(v[3]));
        }

        // equals
        {
            Vector4 b = b2EqualsW(b1, b2);
            Vector<float> v = b2EqualsW(v1, v2);

            Assert.That(b.X, Is.EqualTo(v[0]));
            Assert.That(b.Y, Is.EqualTo(v[1]));
            Assert.That(b.Z, Is.EqualTo(v[2]));
            Assert.That(b.W, Is.EqualTo(v[3]));
        }

        // blend
        {
            Vector4 b = b2BlendW(b1, b2, b3);
            Vector<float> v = b2BlendW(v1, v2, v3);

            Assert.That(b.X, Is.EqualTo(v[0]));
            Assert.That(b.Y, Is.EqualTo(v[1]));
            Assert.That(b.Z, Is.EqualTo(v[2]));
            Assert.That(b.W, Is.EqualTo(v[3]));
        }
    }
}
