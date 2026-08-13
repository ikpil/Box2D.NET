// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Globalization;
using Box2D.NET.Samples;
using NUnit.Framework;

namespace Box2D.NET.Test;

public class SampleTextTests
{
    [TestCase(1.2345678f, "1.23457")]
    [TestCase(0.0000001f, "1e-07")]
    [TestCase(10000000.0f, "1e+07")]
    [TestCase(float.PositiveInfinity, "inf")]
    [TestCase(float.NegativeInfinity, "-inf")]
    public void FormatFloatMatchesPrintfGeneralFormat(float value, string expected)
    {
        Assert.That(SampleText.FormatFloat(value), Is.EqualTo(expected));
    }

    [Test]
    public void FormatFloatDoesNotUseCurrentCulture()
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            Assert.That(SampleText.FormatFloat(1.25f), Is.EqualTo("1.25"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [TestCase(1.26f, 2, "1.3")]
    [TestCase(123.456f, 2, "1.2e+02")]
    [TestCase(0.001234f, 2, "0.0012")]
    public void FormatFloatSupportsPrintfGeneralFormatPrecision(float value, int precision, string expected)
    {
        Assert.That(SampleText.FormatFloat(value, precision), Is.EqualTo(expected));
    }
}
