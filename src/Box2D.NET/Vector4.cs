// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Box2D.NET
{
    public static class Vector4Extensions
    {
        public static float GetAt(this in Vector4 v, int index) => index switch
        {
            0 => v.X,
            1 => v.Y,
            2 => v.Z,
            3 => v.W,
            _ => throw new IndexOutOfRangeException("Index must be between 0 and 3.")
        };

        public static void SetAt(this ref Vector4 v, int index, float value)
        {
            switch (index)
            {
                case 0: v.X = value; break;
                case 1: v.Y = value; break;
                case 2: v.Z = value; break;
                case 3: v.W = value; break;
                default: throw new IndexOutOfRangeException("Index must be between 0 and 3.");
            }
        }
    }
}