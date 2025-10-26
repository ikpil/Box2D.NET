// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Samples.Primitives;

namespace Box2D.NET.Samples.Graphics;

public struct Font
{
    public float fontSize;
    public B2Array<FontVertex> vertices;
    //public stbtt_bakedchar* characters;
    public byte[] characters;
    public uint[] textureId;
    public uint[] vaoId;
    public uint[] vboId;
    public uint programId;

    public Font()
    {
        textureId = new uint[1];
        vaoId = new uint[1];
        vboId = new uint[1];
    }
}
