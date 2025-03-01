// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Samples.Primitives;

public struct RGBA8
{
    public byte r, g, b, a;

    public RGBA8(byte r, byte g, byte b, byte a)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    public static RGBA8 MakeRGBA8(B2HexColor c, float alpha)
    {
        return new RGBA8((byte)((uint)c >> 16 & 0xFF), (byte)((uint)c >> 8 & 0xFF), (byte)((uint)c & 0xFF), (byte)(0xFF * alpha));
    }
}
