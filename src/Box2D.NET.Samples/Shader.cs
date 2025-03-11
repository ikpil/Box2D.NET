// SPDX-FileCopyrightText: 2024 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Box2D.NET.Samples;
using Silk.NET.OpenGL;

public class Shader
{
    private GL _gl;
    
    public Shader()
    {
    }

    public void SetGL(GL gl)
    {
        _gl = gl;
    }
    
    public void DumpInfoGL()
    {
        string renderer = _gl.GetStringS(StringName.Renderer);
        string vendor = _gl.GetStringS(StringName.Vendor);
        string version = _gl.GetStringS(StringName.Version);
        string glslVersion = _gl.GetStringS(StringName.ShadingLanguageVersion);

        int major = _gl.GetInteger(GetPName.MajorVersion);
        int minor = _gl.GetInteger(GetPName.MinorVersion);

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
        GLEnum errCode = _gl.GetError();
        if (errCode != GLEnum.NoError)
        {
            Console.WriteLine($"OpenGL error = {errCode}");
            Debug.Assert(false);
        }
    }

    public void PrintLogGL(uint obj)
    {
        Span<int> log_length = stackalloc int[1];

        if (_gl.IsShader(obj))
        {
            _gl.GetShader(obj, GLEnum.InfoLogLength, log_length);
        }
        else if (_gl.IsProgram(obj))
        {
            _gl.GetProgram(obj, GLEnum.InfoLogLength, log_length);
        }
        else
        {
            Console.WriteLine("PrintLogGL: Not a shader or a program");
            return;
        }

        string log = string.Empty;

        if (_gl.IsShader(obj))
        {
            log = _gl.GetShaderInfoLog(obj);
        }
        else if (_gl.IsProgram(obj))
        {
            log = _gl.GetProgramInfoLog(obj);
        }

        Console.WriteLine($"PrintLogGL: {log}");
    }

    public uint sCreateShaderFromString(string source, GLEnum type)
    {
        uint shader = _gl.CreateShader(type);

        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);

        Span<int> success = stackalloc int[1];
        _gl.GetShader(shader, GLEnum.CompileStatus, success);

        if (success[0] == 0)
        {
            Console.WriteLine("Error compiling shader of type %d!\n", type);
            PrintLogGL(shader);
            _gl.DeleteShader(shader);
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

        uint program = _gl.CreateProgram();
        _gl.AttachShader(program, vertex);
        _gl.AttachShader(program, fragment);

        _gl.LinkProgram(program);

        Span<int> success = stackalloc int[1];
        _gl.GetProgram(program, GLEnum.LinkStatus, success);
        if (success[0] == 0)
        {
            Console.WriteLine("glLinkProgram:");
            PrintLogGL(program);
            return 0;
        }

        _gl.DeleteShader(vertex);
        _gl.DeleteShader(fragment);

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


        uint shader = _gl.CreateShader(type);

        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);

        Span<int> success = stackalloc int[1];
        _gl.GetShader(shader, GLEnum.CompileStatus, success);

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

        uint program = _gl.CreateProgram();
        _gl.AttachShader(program, vertex);
        _gl.AttachShader(program, fragment);

        _gl.LinkProgram(program);

        Span<int> success = stackalloc int[1];
        _gl.GetProgram(program, GLEnum.LinkStatus, success);
        if (success[0] == 0)
        {
            Console.WriteLine("glLinkProgram:");
            PrintLogGL(program);
            return 0;
        }

        _gl.DeleteShader(vertex);
        _gl.DeleteShader(fragment);

        return program;
    }
}