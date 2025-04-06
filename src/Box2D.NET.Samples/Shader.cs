// SPDX-FileCopyrightText: 2024 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Box2D.NET.Samples;
using Serilog;
using Silk.NET.OpenGL;

public static class Shader
{
    private static readonly ILogger Logger = Log.ForContext(typeof(Shader));

    public static void DumpInfoGL(this GL gl)
    {
        string renderer = gl.GetStringS(StringName.Renderer);
        string vendor = gl.GetStringS(StringName.Vendor);
        string version = gl.GetStringS(StringName.Version);
        string glslVersion = gl.GetStringS(StringName.ShadingLanguageVersion);

        int major = gl.GetInteger(GetPName.MajorVersion);
        int minor = gl.GetInteger(GetPName.MinorVersion);

        Logger.Information("-------------------------------------------------------------");
        Logger.Information($"GL Vendor    : {vendor}");
        Logger.Information($"GL Renderer  : {renderer}");
        Logger.Information($"GL Version   : {version}");
        Logger.Information($"GL Version   : {major}.{minor}");
        Logger.Information($"GLSL Version : {glslVersion}");
        Logger.Information("-------------------------------------------------------------");
    }

    public static void CheckOpenGL(this GL gl)
    {
        GLEnum errCode = gl.GetError();
        if (errCode != GLEnum.NoError)
        {
            Logger.Information($"OpenGL error = {errCode}");
            Debug.Assert(false);
        }
    }

    public static void PrintLogGL(this GL gl, uint obj)
    {
        Span<int> log_length = stackalloc int[1];

        if (gl.IsShader(obj))
        {
            gl.GetShader(obj, GLEnum.InfoLogLength, log_length);
        }
        else if (gl.IsProgram(obj))
        {
            gl.GetProgram(obj, GLEnum.InfoLogLength, log_length);
        }
        else
        {
            Logger.Information("PrintLogGL: Not a shader or a program");
            return;
        }

        string log = string.Empty;

        if (gl.IsShader(obj))
        {
            log = gl.GetShaderInfoLog(obj);
        }
        else if (gl.IsProgram(obj))
        {
            log = gl.GetProgramInfoLog(obj);
        }

        Logger.Information($"PrintLogGL: {log}");
    }

    public static uint sCreateShaderFromString(this GL gl, string source, GLEnum type)
    {
        uint shader = gl.CreateShader(type);

        gl.ShaderSource(shader, source);
        gl.CompileShader(shader);

        Span<int> success = stackalloc int[1];
        gl.GetShader(shader, GLEnum.CompileStatus, success);

        if (success[0] == 0)
        {
            Logger.Information("Error compiling shader of type %d!\n", type);
            gl.PrintLogGL(shader);
            gl.DeleteShader(shader);
            return 0;
        }

        return shader;
    }

    public static uint CreateProgramFromStrings(this GL gl, string vertexString, string fragmentString)
    {
        uint vertex = gl.sCreateShaderFromString(vertexString, GLEnum.VertexShader);
        if (vertex == 0)
        {
            return 0;
        }

        uint fragment = gl.sCreateShaderFromString(fragmentString, GLEnum.FragmentShader);
        if (fragment == 0)
        {
            return 0;
        }

        uint program = gl.CreateProgram();
        gl.AttachShader(program, vertex);
        gl.AttachShader(program, fragment);

        gl.LinkProgram(program);

        Span<int> success = stackalloc int[1];
        gl.GetProgram(program, GLEnum.LinkStatus, success);
        if (success[0] == 0)
        {
            Logger.Information("glLinkProgram:");
            gl.PrintLogGL(program);
            return 0;
        }

        gl.DeleteShader(vertex);
        gl.DeleteShader(fragment);

        return program;
    }

    public static uint sCreateShaderFromFile(this GL gl, string filename, GLEnum type)
    {
        if (!File.Exists(filename))
        {
            Logger.Information($"Error opening {filename}");
            return 0;
        }

        byte[] bytes = File.ReadAllBytes(filename);
        var source = Encoding.UTF8.GetString(bytes);


        uint shader = gl.CreateShader(type);

        gl.ShaderSource(shader, source);
        gl.CompileShader(shader);

        Span<int> success = stackalloc int[1];
        gl.GetShader(shader, GLEnum.CompileStatus, success);

        if (success[0] == 0)
        {
            Logger.Information($"Error compiling shader of type {type}!");
            gl.PrintLogGL(shader);
        }

        return shader;
    }

    public static uint CreateProgramFromFiles(this GL gl, string vertexPath, string fragmentPath)
    {
        uint vertex = gl.sCreateShaderFromFile(vertexPath, GLEnum.VertexShader);
        if (vertex == 0)
        {
            return 0;
        }

        uint fragment = gl.sCreateShaderFromFile(fragmentPath, GLEnum.FragmentShader);
        if (fragment == 0)
        {
            return 0;
        }

        uint program = gl.CreateProgram();
        gl.AttachShader(program, vertex);
        gl.AttachShader(program, fragment);

        gl.LinkProgram(program);

        Span<int> success = stackalloc int[1];
        gl.GetProgram(program, GLEnum.LinkStatus, success);
        if (success[0] == 0)
        {
            Logger.Information("glLinkProgram:");
            gl.PrintLogGL(program);
            return 0;
        }

        gl.DeleteShader(vertex);
        gl.DeleteShader(fragment);

        return program;
    }
}