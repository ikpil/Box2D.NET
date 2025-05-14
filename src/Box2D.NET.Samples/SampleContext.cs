// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.IO;
using System.Runtime.CompilerServices;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;

namespace Box2D.NET.Samples;

public class SampleContext
{
    //
    public readonly string Signature;
    public readonly Glfw glfw;
    public readonly Camera camera;
    public readonly Draw draw;
    
    public readonly Settings settings;
    
    //
    public GL gl;
    public unsafe WindowHandle* window;

    private static string CreateSignature(string member, string file, int line)
    {
        return $"{member}() {Path.GetFileName(file)}:{line}";
    }

    public static SampleContext Create([CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        // for windows - https://learn.microsoft.com/ko-kr/cpp/windows/latest-supported-vc-redist
        var glfw = Glfw.GetApi();
        var sig = CreateSignature(member, file, line);
        return CreateFor(sig, glfw);
    }

    public static SampleContext CreateWithoutGLFW([CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        var sig = CreateSignature(member, file, line);
        return CreateFor(sig, null);
    }

    public static SampleContext CreateFor(string sig, Glfw glfw)
    {
        var context = new SampleContext(sig, glfw);
        return context;
    }

    private SampleContext(string signature, Glfw glfw)
    {
        Signature = signature;
        this.glfw = glfw;
        camera = new Camera();
        draw = new Draw();
        settings = new Settings();
    }
}
