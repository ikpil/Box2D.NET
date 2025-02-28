// SPDX-FileCopyrightText: 2024 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Silk.NET.OpenGL;

public class Shader
{
    public static readonly Shader Shared = new Shader();

    public GL gl;

    public void DumpInfoGL()
    {
        string renderer = gl.GetStringS(StringName.Renderer);
        string vendor = gl.GetStringS(StringName.Vendor);
        string version = gl.GetStringS(StringName.Version);
        string glslVersion = gl.GetStringS(StringName.ShadingLanguageVersion);

        int major = gl.GetInteger(GetPName.MajorVersion);
        int minor = gl.GetInteger(GetPName.MinorVersion);

        Console.WriteLine("-------------------------------------------------------------");
        Console.WriteLine($"GL Vendor    : {vendor}");
        Console.WriteLine($"GL Renderer  : {renderer}");
        Console.WriteLine($"GL Version   : {version}");
        Console.WriteLine($"GL Version   : {major}.{minor}");
        Console.WriteLine($"GLSL Version : {glslVersion}");
        Console.WriteLine("-------------------------------------------------------------");
    }

    public void CheckErrorGL()
    {
        GLEnum errCode = gl.GetError();
        if (errCode != GLEnum.NoError)
        {
            Console.WriteLine($"OpenGL error = {errCode}");
            Debug.Assert(false);
        }
    }

    public void PrintLogGL(uint obj)
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
            Console.WriteLine("PrintLogGL: Not a shader or a program");
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

        Console.WriteLine($"PrintLogGL: {log}");
    }

    public uint sCreateShaderFromString(string source, GLEnum type)
    {
        uint shader = gl.CreateShader(type);
        string[] sources = [source];

        gl.ShaderSource(shader, 1, sources, 0);
        gl.CompileShader(shader);

        Span<int> success = stackalloc int[1];
        gl.GetShader(shader, GLEnum.CompileStatus, success);

        if (success[0] == 0)
        {
            Console.WriteLine("Error compiling shader of type %d!\n", type);
            PrintLogGL(shader);
            gl.DeleteShader(shader);
            return 0;
        }

        return shader;
    }

    public uint CreateProgramFromStrings(string vertexString, string fragmentString)
    {
        uint vertex = sCreateShaderFromString(vertexString, GLEnum.VertexShader);
        if (vertex == 0)
        {
            return 0;
        }

        uint fragment = sCreateShaderFromString(fragmentString, GLEnum.FragmentShader);
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
            Console.WriteLine("glLinkProgram:");
            PrintLogGL(program);
            return 0;
        }

        gl.DeleteShader(vertex);
        gl.DeleteShader(fragment);

        return program;
    }

    public uint sCreateShaderFromFile(string filename, GLEnum type)
    {
        if (!File.Exists(filename))
        {
            Console.WriteLine($"Error opening {filename}");
            return 0;
        }

        byte[] bytes = File.ReadAllBytes(filename);
        var source = Encoding.UTF8.GetString(bytes);


        uint shader = gl.CreateShader(type);
        string[] sources = [source];

        gl.ShaderSource(shader, 1, sources, 0);
        gl.CompileShader(shader);

        Span<int> success = stackalloc int[1];
        gl.GetShader(shader, GLEnum.CompileStatus, success);

        if (success[0] == 0)
        {
            Console.WriteLine($"Error compiling shader of type {type}!");
            PrintLogGL(shader);
        }

        return shader;
    }

    public uint CreateProgramFromFiles(string vertexPath, string fragmentPath)
    {
        uint vertex = sCreateShaderFromFile(vertexPath, GLEnum.VertexShader);
        if (vertex == 0)
        {
            return 0;
        }

        uint fragment = sCreateShaderFromFile(fragmentPath, GLEnum.FragmentShader);
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
            Console.WriteLine("glLinkProgram:");
            PrintLogGL(program);
            return 0;
        }

        gl.DeleteShader(vertex);
        gl.DeleteShader(fragment);

        return program;
    }
}