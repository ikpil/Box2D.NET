// SPDX-License-Identifier: MIT

using static Box2D.NET.B2ContactSolvers;
using System;
using NUnit.Framework;

namespace Box2D.NET.Test;

public class B2WideArithmeticTests
{
    [Test]
    public void UpperLanesParticipateInMasksAndKeepTheFourLaneConstructor()
    {
        var value = new B2FloatW(1, 2, 3, 4);
        Assert.That(value.AsSpan().Length, Is.EqualTo(8));
        Assert.That(value.H, Is.Zero);
        value = default;
        value.H = 1;
        Assert.That(b2AllZeroW(value), Is.False);
        value[7] = 0;
        Assert.That(b2AllZeroW(value), Is.True);
    }

    [Test]
    public void MatchesScalarArithmeticIncludingEdgeCases()
    {
        var random = new Random(712);
        float[] edge = [0, -0f, 1, -1, float.Epsilon, -float.Epsilon, float.MaxValue, float.MinValue, float.PositiveInfinity, float.NegativeInfinity, float.NaN];
        for (var i = 0; i < 4096; i++)
        {
            var a = new B2FloatW();
            var b = a;
            var c = a;
            for (var lane = 0; lane < Box2D.NET.B2Cores.B2_SIMD_WIDTH; lane++)
            {
                a[lane] = i < edge.Length * edge.Length ? edge[i % edge.Length] : (float)(random.NextDouble() * 200 - 100);
                b[lane] = i < edge.Length * edge.Length ? edge[i / edge.Length] : (float)(random.NextDouble() * 200 - 100);
                c[lane] = i < edge.Length * edge.Length ? edge[(i + lane) % edge.Length] : (float)(random.NextDouble() * 200 - 100);
            }
            var add = b2AddW(a, b);
            var sub = b2SubW(a, b);
            var mul = b2MulW(a, b);
            var madd = b2MulAddW(a, b, c);
            var msub = b2MulSubW(a, b, c);
            var min = b2MinW(a, b);
            var max = b2MaxW(a, b);
            var clamp = b2SymClampW(a, b);
            var or = b2OrW(a, b);
            var greater = b2GreaterThanW(a, b);
            var equal = b2EqualsW(a, b);
            var blend = b2BlendW(a, b, c);
            var zero = true;
            for (var lane = 0; lane < Box2D.NET.B2Cores.B2_SIMD_WIDTH; lane++)
            {
                Equal(add[lane], a[lane] + b[lane]);
                Equal(sub[lane], a[lane] - b[lane]);
                Equal(mul[lane], a[lane] * b[lane]);
                Equal(madd[lane], a[lane] + b[lane] * c[lane]);
                Equal(msub[lane], a[lane] - b[lane] * c[lane]);
                Equal(min[lane], a[lane] <= b[lane] ? a[lane] : b[lane], true);
                Equal(max[lane], a[lane] >= b[lane] ? a[lane] : b[lane], true);
                Equal(clamp[lane], a[lane] < -b[lane] ? -b[lane] : a[lane] > b[lane] ? b[lane] : a[lane], true);
                Equal(or[lane], a[lane] != 0 || b[lane] != 0 ? 1 : 0);
                Equal(greater[lane], a[lane] > b[lane] ? 1 : 0);
                Equal(equal[lane], a[lane] == b[lane] ? 1 : 0);
                Equal(blend[lane], c[lane] != 0 ? b[lane] : a[lane], true);
                zero &= a[lane] == 0;
            }
            if (b2AllZeroW(a) != zero)
                throw new InvalidOperationException("SIMD zero reduction differs from scalar arithmetic.");
        }
    }

    private static void Equal(float actual, float expected, bool preserveBits = false)
    {
        if (BitConverter.SingleToInt32Bits(actual) == BitConverter.SingleToInt32Bits(expected) ||
            !preserveBits && float.IsNaN(actual) && float.IsNaN(expected))
            return;
        throw new InvalidOperationException($"SIMD differs from scalar arithmetic: {actual:R} versus {expected:R}.");
    }
}
