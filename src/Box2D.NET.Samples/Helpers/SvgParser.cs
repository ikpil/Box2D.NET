// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Globalization;
using static Box2D.NET.B2Cores;

namespace Box2D.NET.Samples.Helpers;

public static class SvgParser
{
    // Parse an SVG path element with only straight lines. Example:
    // "M 47.625004,185.20833 H 161.39585 l 29.10417,-2.64583 26.45834,-7.9375 26.45833,-13.22917 23.81251,-21.16666 h "
    // "13.22916 v 44.97916 H 592.66669 V 0 h 21.16671 v 206.375 l -566.208398,-1e-5 z"
    public static int ParsePath(string svgPath, B2Vec2 offset, Span<B2Vec2> points, int capacity, float scale, bool reverseOrder)
    {
        int pointCount = 0;
        B2Vec2 currentPoint = new B2Vec2();
        int ptr = 0;
        char command = svgPath[ptr];

        while (ptr < svgPath.Length)
        {
            if (!char.IsDigit(svgPath[ptr]) && svgPath[ptr] != '-')
            {
                // note: command can be implicitly repeated
                command = svgPath[ptr];

                if (command == 'M' || command == 'L' || command == 'H' || command == 'V' || command == 'm' || command == 'l' ||
                    command == 'h' || command == 'v')
                {
                    ptr += 2; // Skip the command character and space
                }

                if (command == 'z')
                {
                    break;
                }
            }

            B2_ASSERT(char.IsDigit(svgPath[ptr]) || svgPath[ptr] == '-');


            float x = 0.0f;
            float y = 0.0f;
            switch (command)
            {
                case 'M':
                case 'L':
                    if (Sscanf(svgPath, ref ptr, out x, out y))
                    {
                        currentPoint.X = x;
                        currentPoint.Y = y;
                    }
                    else
                    {
                        B2_ASSERT(false);
                    }

                    break;
                case 'H':
                    if (Sscanf(svgPath, ref ptr, out x))
                    {
                        currentPoint.X = x;
                    }
                    else
                    {
                        B2_ASSERT(false);
                    }

                    break;
                case 'V':
                    if (Sscanf(svgPath, ref ptr, out y))
                    {
                        currentPoint.Y = y;
                    }
                    else
                    {
                        B2_ASSERT(false);
                    }

                    break;
                case 'm':
                case 'l':
                    if (Sscanf(svgPath, ref ptr, out x, out y))
                    {
                        currentPoint.X += x;
                        currentPoint.Y += y;
                    }
                    else
                    {
                        B2_ASSERT(false);
                    }

                    break;
                case 'h':
                    if (Sscanf(svgPath, ref ptr, out x))
                    {
                        currentPoint.X += x;
                    }
                    else
                    {
                        B2_ASSERT(false);
                    }

                    break;
                case 'v':
                    if (Sscanf(svgPath, ref ptr, out y))
                    {
                        currentPoint.Y += y;
                    }
                    else
                    {
                        B2_ASSERT(false);
                    }

                    break;

                default:
                    B2_ASSERT(false);
                    break;
            }

            points[pointCount] = new B2Vec2(scale * (currentPoint.X + offset.X), -scale * (currentPoint.Y + offset.Y));
            pointCount += 1;
            if (pointCount == capacity)
            {
                break;
            }

            // Move to the next space or end of string
            while (ptr < svgPath.Length && !char.IsWhiteSpace(svgPath[ptr]))
            {
                ptr++;
            }

            // Skip contiguous spaces
            while (ptr < svgPath.Length && char.IsWhiteSpace(svgPath[ptr]))
            {
                ptr++;
            }

            ptr += 0;
        }

        if (pointCount == 0)
        {
            return 0;
        }

        if (reverseOrder)
        {
        }

        return pointCount;
    }

    private static bool Sscanf(string svgPath, ref int ptrIndex, out float x, out float y)
    {
        // Parse the coordinates in the form "x,y"
        x = 0;
        y = 0;
        int startIdx = ptrIndex;
        while (ptrIndex < svgPath.Length && (char.IsDigit(svgPath[ptrIndex]) || svgPath[ptrIndex] == '.' || svgPath[ptrIndex] == '-' || svgPath[ptrIndex] == ',' || svgPath[ptrIndex] == 'e' || svgPath[ptrIndex] == 'E'))
        {
            ptrIndex++;
        }

        var segment = svgPath.Substring(startIdx, ptrIndex - startIdx).Split(',');
        if (segment.Length == 2)
        {
            return float.TryParse(segment[0], CultureInfo.InvariantCulture, out x) && float.TryParse(segment[1], CultureInfo.InvariantCulture, out y);
        }

        return false;
    }

    private static bool Sscanf(string svgPath, ref int ptrIndex, out float value)
    {
        value = 0;
        int startIdx = ptrIndex;
        while (ptrIndex < svgPath.Length && (char.IsDigit(svgPath[ptrIndex]) || svgPath[ptrIndex] == '.' || svgPath[ptrIndex] == '-'))
        {
            ptrIndex++;
        }

        var segment = svgPath.Substring(startIdx, ptrIndex - startIdx);
        return float.TryParse(segment, CultureInfo.InvariantCulture, out value);
    }
}
