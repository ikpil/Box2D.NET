// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.IO;
using System.Reflection;

namespace Box2D.NET.Samples;

// Embedded from data/*.vs and *.fs. Do not edit the shader strings here.
public static class EmbeddedShaders
{
    public static readonly string k_background_vs = Load("background.vs");
    public static readonly string k_background_fs = Load("background.fs");
    public static readonly string k_circle_vs = Load("circle.vs");
    public static readonly string k_circle_fs = Load("circle.fs");
    public static readonly string k_line_vs = Load("line.vs");
    public static readonly string k_line_fs = Load("line.fs");
    public static readonly string k_point_vs = Load("point.vs");
    public static readonly string k_point_fs = Load("point.fs");
    public static readonly string k_solid_capsule_vs = Load("solid_capsule.vs");
    public static readonly string k_solid_capsule_fs = Load("solid_capsule.fs");
    public static readonly string k_solid_circle_vs = Load("solid_circle.vs");
    public static readonly string k_solid_circle_fs = Load("solid_circle.fs");
    public static readonly string k_solid_polygon_vs = Load("solid_polygon.vs");
    public static readonly string k_solid_polygon_fs = Load("solid_polygon.fs");

    private static string Load(string name)
    {
        Assembly assembly = typeof(EmbeddedShaders).Assembly;
        using Stream stream = assembly.GetManifestResourceStream($"Box2D.NET.Samples.Shaders.{name}")!;
        using StreamReader reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
