// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using Box2D.NET.Samples.Primitives;

namespace Box2D.NET.Samples.Graphics;

public struct SolidCapsuleRender
{
    public List<CapsuleData> capsules;

    public uint[] vaoId;
    public uint[] vboIds;
    public uint programId;
    public int projectionUniform;
    public int pixelScaleUniform;

    public SolidCapsuleRender()
    {
        capsules = new List<CapsuleData>();
        vaoId = new uint[1];
        vboIds = new uint[2];
    }
}
