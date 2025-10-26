// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using ImGuiNET;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;

namespace Box2D.NET.Samples.Graphics;

// This class implements Box2D debug drawing callbacks
public class Draw
{
    public Glfw glfw;
    public GL gl;
    public Camera camera;

    public Background background;
    public PointRender points;
    public LineRender lines;
    public CircleRender hollowCircles;
    public SolidCircleRender circles;
    public SolidCapsuleRender capsules;
    public SolidPolygonRender polygons;
    public Font font;

}