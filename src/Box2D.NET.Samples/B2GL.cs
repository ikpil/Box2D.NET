// SPDX-FileCopyrightText: 2024 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Diagnostics;

public static class B2GL
{
    public static void DumpInfoGL()
    {
        string renderer = (string)glGetString(GL_RENDERER);
        string vendor = (string)glGetString(GL_VENDOR);
        string version = (string)glGetString(GL_VERSION);
        string glslVersion = (string)glGetString(GL_SHADING_LANGUAGE_VERSION);

        int major, minor;
        glGetIntegerv(GL_MAJOR_VERSION, &major);
        glGetIntegerv(GL_MINOR_VERSION, &minor);

        printf("-------------------------------------------------------------\n");
        printf("GL Vendor    : %s\n", vendor);
        printf("GL Renderer  : %s\n", renderer);
        printf("GL Version   : %s\n", version);
        printf("GL Version   : %d.%d\n", major, minor);
        printf("GLSL Version : %s\n", glslVersion);
        printf("-------------------------------------------------------------\n");
    }

    public static void CheckErrorGL()
    {
        GLenum errCode = glGetError();
        if (errCode != GL_NO_ERROR)
        {
            printf("OpenGL error = %d\n", errCode);
            Debug.Assert(false);
        }
    }

    public static void PrintLogGL(uint obj )
    {
        GLint log_length = 0;
        if (glIsShader(obj))
        {
            glGetShaderiv(obj, GL_INFO_LOG_LENGTH, &log_length);
        }
        else if (glIsProgram(obj))
        {
            glGetProgramiv(obj, GL_INFO_LOG_LENGTH, &log_length);
        }
        else
        {
            printf("PrintLogGL: Not a shader or a program\n");
            return;
        }

        char* log = (char*)malloc(log_length);

        if (glIsShader(obj))
        {
            glGetShaderInfoLog(obj, log_length, nullptr, log);
        }
        else if (glIsProgram(obj))
        {
            glGetProgramInfoLog(obj, log_length, nullptr, log);
        }

        printf("PrintLogGL: %s", log);
        free(log);
    }

    public static GLuint sCreateShaderFromString(string source, GLenum type)
    {
        GLuint shader = glCreateShader(type);
        string sources[] =  {
            source
        }
        ;

        glShaderSource(shader, 1, sources, nullptr);
        glCompileShader(shader);

        int success = GL_FALSE;
        glGetShaderiv(shader, GL_COMPILE_STATUS, &success);

        if (success == GL_FALSE)
        {
            printf("Error compiling shader of type %d!\n", type);
            PrintLogGL(shader);
            glDeleteShader(shader);
            return 0;
        }

        return shader;
    }

    public static uint CreateProgramFromStrings(string vertexString, string fragmentString)
    {
        GLuint vertex = sCreateShaderFromString(vertexString, GL_VERTEX_SHADER);
        if (vertex == 0)
        {
            return 0;
        }

        GLuint fragment = sCreateShaderFromString(fragmentString, GL_FRAGMENT_SHADER);
        if (fragment == 0)
        {
            return 0;
        }

        GLuint program = glCreateProgram();
        glAttachShader(program, vertex);
        glAttachShader(program, fragment);

        glLinkProgram(program);

        int success = GL_FALSE;
        glGetProgramiv(program, GL_LINK_STATUS, &success);
        if (success == GL_FALSE)
        {
            printf("glLinkProgram:");
            PrintLogGL(program);
            return 0;
        }

        glDeleteShader(vertex);
        glDeleteShader(fragment);

        return program;
    }

    public static GLuint sCreateShaderFromFile(string filename, GLenum type)
    {
        FILE* file = fopen(filename, "rb");
        if (file == nullptr)
        {
            fprintf(stderr, "Error opening %s\n", filename);
            return 0;
        }

        fseek(file, 0, SEEK_END);
        long size = ftell(file);
        fseek(file, 0, SEEK_SET);

        char* source = static_cast<char*>(malloc(size + 1));
        size_t count = fread(source, size, 1, file);
        B2_UNUSED(count);
        fclose(file);

        source[size] = 0;

        GLuint shader = glCreateShader(type);
        string sources[] =  {
            source
        }
        ;

        glShaderSource(shader, 1, sources, nullptr);
        glCompileShader(shader);

        int success = GL_FALSE;
        glGetShaderiv(shader, GL_COMPILE_STATUS, &success);

        if (success == GL_FALSE)
        {
            fprintf(stderr, "Error compiling shader of type %d!\n", type);
            PrintLogGL(shader);
        }

        free(source);
        return shader;
    }

    public static uint CreateProgramFromFiles(string vertexPath, string fragmentPath)
    {
        GLuint vertex = sCreateShaderFromFile(vertexPath, GL_VERTEX_SHADER);
        if (vertex == 0)
        {
            return 0;
        }

        GLuint fragment = sCreateShaderFromFile(fragmentPath, GL_FRAGMENT_SHADER);
        if (fragment == 0)
        {
            return 0;
        }

        GLuint program = glCreateProgram();
        glAttachShader(program, vertex);
        glAttachShader(program, fragment);

        glLinkProgram(program);

        int success = GL_FALSE;
        glGetProgramiv(program, GL_LINK_STATUS, &success);
        if (success == GL_FALSE)
        {
            printf("glLinkProgram:");
            PrintLogGL(program);
            return 0;
        }

        glDeleteShader(vertex);
        glDeleteShader(fragment);

        return program;
    }
}
