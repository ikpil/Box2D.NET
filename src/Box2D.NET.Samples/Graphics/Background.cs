// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT


namespace Box2D.NET.Samples.Graphics;

public struct Background
{
    public uint[] vaoId;
    public uint[] vboId;
    public uint programId;
    public int timeUniform;
    public int resolutionUniform;
    public int baseColorUniform;

    public Background()
    {
        vaoId = new uint[1];
        vboId = new uint[1];
    }
}