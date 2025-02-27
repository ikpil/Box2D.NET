// SPDX-FileCopyrightText: 2024 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Silk.NET.OpenGL;

public class B2GL
{
    public static readonly B2GL Shared = new B2GL();

    public GL Gl;
    
    public void DumpInfoGL()
    {
        string renderer = Gl.GetStringS(StringName.Renderer);
        string vendor = Gl.GetStringS(StringName.Vendor);
        string version = Gl.GetStringS(StringName.Version);
        string glslVersion = Gl.GetStringS(StringName.ShadingLanguageVersion);

        int major = Gl.GetInteger(GetPName.MajorVersion);
        int minor = Gl.GetInteger(GetPName.MinorVersion);

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
        GLEnum errCode = Gl.GetError();
        if (errCode != GLEnum.NoError)
        {
            Console.WriteLine($"OpenGL error = {errCode}");
            Debug.Assert(false);
        }
    }

    public void PrintLogGL(uint obj)
    {
        Span<int> log_length = stackalloc int[1];

        if (Gl.IsShader(obj))
        {
            Gl.GetShader(obj, GLEnum.InfoLogLength, log_length);
        }
        else if (Gl.IsProgram(obj))
        {
            Gl.GetProgram(obj, GLEnum.InfoLogLength, log_length);
        }
        else
        {
            Console.WriteLine("PrintLogGL: Not a shader or a program");
            return;
        }

        string log = string.Empty;

        if (Gl.IsShader(obj))
        {
            log = Gl.GetShaderInfoLog(obj);
        }
        else if (Gl.IsProgram(obj))
        {
            log = Gl.GetProgramInfoLog(obj);
        }

        Console.WriteLine($"PrintLogGL: {log}");
    }

    public uint sCreateShaderFromString(string source, GLEnum type)
    {
        uint shader = Gl.CreateShader(type);
        string[] sources = [source];

        Gl.ShaderSource(shader, 1, sources, 0);
        Gl.CompileShader(shader);

        Span<int> success = stackalloc int[1];
        Gl.GetShader(shader, GLEnum.CompileStatus, success);

        if (success[0] == 0)
        {
            Console.WriteLine("Error compiling shader of type %d!\n", type);
            PrintLogGL(shader);
            Gl.DeleteShader(shader);
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

        uint program = Gl.CreateProgram();
        Gl.AttachShader(program, vertex);
        Gl.AttachShader(program, fragment);

        Gl.LinkProgram(program);

        Span<int> success = stackalloc int[1];
        Gl.GetProgram(program, GLEnum.LinkStatus, success);
        if (success[0] == 0)
        {
            Console.WriteLine("glLinkProgram:");
            PrintLogGL(program);
            return 0;
        }

        Gl.DeleteShader(vertex);
        Gl.DeleteShader(fragment);

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


        uint shader = Gl.CreateShader(type);
        string[] sources = [source];

        Gl.ShaderSource(shader, 1, sources, 0);
        Gl.CompileShader(shader);

        Span<int> success = stackalloc int[1];
        Gl.GetShader(shader, GLEnum.CompileStatus, success);

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

        uint program = Gl.CreateProgram();
        Gl.AttachShader(program, vertex);
        Gl.AttachShader(program, fragment);

        Gl.LinkProgram(program);

        Span<int> success = stackalloc int[1];
        Gl.GetProgram(program, GLEnum.LinkStatus, success);
        if (success[0] == 0)
        {
            Console.WriteLine("glLinkProgram:");
            PrintLogGL(program);
            return 0;
        }

        Gl.DeleteShader(vertex);
        Gl.DeleteShader(fragment);

        return program;
    }
}