// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using Box2D.NET.Samples.Primitives;

namespace Box2D.NET.Samples.Graphics;

public struct LineRender
{
    public List<VertexData> m_points;

    public uint[] m_vaoId;
    public uint[] m_vboId;
    public uint m_programId;
    public int m_projectionUniform;

    public LineRender()
    {
        m_points = new();

        m_vaoId = new uint[1];
        m_vboId = new uint[1];
    }
}
