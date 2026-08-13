// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Globalization;

namespace Box2D.NET.Samples;

public static class SampleText
{
    public static string FormatFloat(float value, int precision = 6)
    {
        if (float.IsNaN(value))
        {
            return "nan";
        }

        if (float.IsPositiveInfinity(value))
        {
            return "inf";
        }

        if (float.IsNegativeInfinity(value))
        {
            return "-inf";
        }

        // C printf("%g") defaults to six significant digits and is not affected by the current .NET culture.
        return value.ToString($"g{precision}", CultureInfo.InvariantCulture);
    }
}
