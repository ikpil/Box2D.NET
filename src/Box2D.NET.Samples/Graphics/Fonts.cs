// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using Silk.NET.OpenGL;
using Box2D.NET.Samples.Helpers;
using Box2D.NET.Samples.Primitives;
using ImGuiNET;
using static Box2D.NET.Samples.Graphics.Cameras;
using static Box2D.NET.B2Arrays;

namespace Box2D.NET.Samples.Graphics;

public static class Fonts
{
    public const int FONT_FIRST_CHARACTER = 32;
    public const int FONT_CHARACTER_COUNT = 96;
    public const int FONT_ATLAS_WIDTH = 512;
    public const int FONT_ATLAS_HEIGHT = 512;

    // The number of vertices the vbo can hold. Must be a multiple of 6.
    public const int FONT_BATCH_SIZE = (6 * 10000);

    public static Font CreateFont(GL gl, string trueTypeFile, float fontSize)
    {
        Font font = new Font();

        // FILE* file = fopen(trueTypeFile, "rb");
        // if (file == NULL)
        // {
        //     assert(false);
        //     return font;
        // }
        //
        // font.vertices = b2Array_Create<FontVertex>(FONT_BATCH_SIZE);
        // font.fontSize = fontSize;
        // font.characters = malloc(FONT_CHARACTER_COUNT * sizeof(stbtt_bakedchar));
        //
        // int fileBufferCapacity = 1 << 20;
        // unsigned char* fileBuffer = (unsigned char *)malloc(fileBufferCapacity * sizeof(unsigned char) );
        // fread(fileBuffer, 1, fileBufferCapacity, file);
        //
        // int pw = FONT_ATLAS_WIDTH;
        // int ph = FONT_ATLAS_HEIGHT;
        // byte[] tempBitmap = new byte[pw * ph * sizeof(byte)];
        // stbtt_BakeFontBitmap(fileBuffer, 0, font.fontSize, tempBitmap, pw, ph, FONT_FIRST_CHARACTER, FONT_CHARACTER_COUNT,
        //     font.characters);
        //
        // gl.GenTextures(1, font.textureId);
        // gl.BindTexture(GLEnum.Texture2D, font.textureId[0]);
        // gl.TexImage2D(GLEnum.Texture2D, 0, GLEnum.R8, pw, ph, 0, GLEnum.Red, GLEnum.UnsignedByte, tempBitmap);
        // gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, GLEnum.Linear);
        //
        // // for debugging
        // // stbi_write_png( "build/fontAtlas.png", pw, ph, 1, tempBitmap, pw );
        //
        // fclose(file);
        // free(fileBuffer);
        // free(tempBitmap);
        // fileBuffer = NULL;
        // tempBitmap = NULL;
        //
        // font.programId = gl.CreateProgramFromFiles("samples/data/font.vs", "samples/data/font.fs");
        // if (font.programId == 0)
        // {
        //     return font;
        // }
        //
        // // Setting up the VAO and VBO
        // gl.GenBuffers(1, font.vboId);
        // gl.BindBuffer(GLEnum.ArrayBuffer, font.vboId[0]);
        // gl.BufferData(GLEnum.ArrayBuffer, FONT_BATCH_SIZE * SizeOf<FontVertex>.Size, IntPtr.Zero, GLEnum.DynamicDraw);
        //
        // gl.GenVertexArrays(1, font.vaoId);
        // gl.BindVertexArray(font.vaoId[0]);
        //
        // // position attribute
        // gl.VertexAttribPointer(0, 2, GLEnum.Float, GLEnum.False, SizeOf<FontVertex>.Size, (void*)offsetof(FontVertex, position));
        // gl.EnableVertexAttribArray(0);
        //
        // // uv attribute
        // gl.VertexAttribPointer(1, 2, GLEnum.Float, GLEnum.False, SizeOf<FontVertex>.Size, (void*)offsetof(FontVertex, uv));
        // gl.EnableVertexAttribArray(1);
        //
        // // color attribute will be expanded to floats using normalization
        // gl.VertexAttribPointer(2, 4, GLEnum.UnsignedByte, GLEnum.True, SizeOf<FontVertex>.Size, (void*)offsetof(FontVertex, color));
        // gl.EnableVertexAttribArray(2);
        //
        // gl.BindVertexArray(0);
        //
        // gl.CheckOpenGL();

        return font;
    }

    public static void DestroyFont(GL gl, ref Font font)
    {
        // if (font.programId != 0)
        // {
        //     gl.DeleteProgram(font.programId);
        // }
        //
        // gl.DeleteBuffers(1, font.vboId);
        // gl.DeleteVertexArrays(1, font.vaoId);
        //
        // if (font.textureId[0] != 0)
        // {
        //     gl.DeleteTextures(1, font.textureId);
        // }
        //
        // free(font.characters);
        //
        // b2Array_Destroy(ref font.vertices);
    }

    public static void AddText(ref Font font, float x, float y, B2HexColor color, string text)
    {
        // // if (m_showUI == false)
        // //{
        // //	return;
        // // }
        //
        // ImGui.Begin("Overlay",
        //     ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize |
        //     ImGuiWindowFlags.NoScrollbar);
        // ImGui.PushFont(draw.m_regularFont);
        // ImGui.SetCursorPos(new Vector2(x, y));
        // ImGui.TextColored(new Vector4(230, 153, 153, 255), message);
        // ImGui.PopFont();
        // ImGui.End();

        ImGui.Begin("Overlay",
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize |
            ImGuiWindowFlags.NoScrollbar);
        ImGui.SetCursorPos(new Vector2(x, y));
        ImGui.TextColored(new Vector4(230, 230, 230, 255), text);
        ImGui.End();

    //     if (text == null)
    //     {
    //         return;
    //     }
    //
    //     B2Vec2 position = new B2Vec2(x, y);
    //     RGBA8 c = RGBA8.MakeRGBA8(color, 1.0f);
    //     int pw = FONT_ATLAS_WIDTH;
    //     int ph = FONT_ATLAS_HEIGHT;
    //
    //     int i = 0;
    //     while (text[i] != 0)
    //     {
    //         int index = (int)text[i] - FONT_FIRST_CHARACTER;
    //
    //         if (0 <= index && index < FONT_CHARACTER_COUNT)
    //         {
    //             // 1=opengl
    //             stbtt_aligned_quad q;
    //             stbtt_GetBakedQuad(font.characters, pw, ph, index, &position.x, &position.y, &q, 1);
    //
    //             FontVertex v1 = new FontVertex(new B2Vec2(q.x0, q.y0), new B2Vec2(q.s0, q.t0), c);
    //             FontVertex v2 = new FontVertex(new B2Vec2(q.x1, q.y0), new B2Vec2(q.s1, q.t0), c);
    //             FontVertex v3 = new FontVertex(new B2Vec2(q.x1, q.y1), new B2Vec2(q.s1, q.t1), c);
    //             FontVertex v4 = new FontVertex(new B2Vec2(q.x0, q.y1), new B2Vec2(q.s0, q.t1), c);
    //
    //             b2Array_Push(ref font.vertices, v1);
    //             b2Array_Push(ref font.vertices, v3);
    //             b2Array_Push(ref font.vertices, v2);
    //             b2Array_Push(ref font.vertices, v1);
    //             b2Array_Push(ref font.vertices, v4);
    //             b2Array_Push(ref font.vertices, v3);
    //         }
    //
    //         i += 1;
    //     }
    }
    
    public static void FlushText(GL gl, ref Font font, Camera camera)
    {
    //     var tempProjectionMatrix = new B2FixedArray16<float>();
    //     Span<float> projectionMatrix = tempProjectionMatrix.AsSpan();
    //     MakeOrthographicMatrix(projectionMatrix, 0.0f, camera.width, camera.height, 0.0f, -1.0f, 1.0f);
    //
    //     gl.UseProgram(font.programId);
    //
    //     gl.Enable(GLEnum.Blend);
    //     gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
    //     // glDisable( GL_DEPTH_TEST );
    //
    //     int slot = 0;
    //     gl.ActiveTexture(GLEnum.Texture0 + slot);
    //     gl.BindTexture(GLEnum.Texture2D, font.textureId[0]);
    //
    //     gl.BindVertexArray(font.vaoId[0]);
    //     gl.BindBuffer(GLEnum.ArrayBuffer, font.vboId[0]);
    //
    //     int textureUniform = gl.GetUniformLocation(font.programId, "FontAtlas");
    //     gl.Uniform1(textureUniform, slot);
    //
    //     int matrixUniform = gl.GetUniformLocation(font.programId, "ProjectionMatrix");
    //     gl.UniformMatrix4(matrixUniform, 1, false, projectionMatrix);
    //
    //     int totalVertexCount = font.vertices.count;
    //     int drawCallCount = (totalVertexCount / FONT_BATCH_SIZE) + 1;
    //
    //     for (int i = 0; i < drawCallCount; i++)
    //     {
    //         Span<FontVertex> data = font.vertices.data.AsSpan(i * FONT_BATCH_SIZE);
    //
    //         int vertexCount;
    //         if (i == drawCallCount - 1)
    //         {
    //             vertexCount = totalVertexCount % FONT_BATCH_SIZE;
    //         }
    //         else
    //         {
    //             vertexCount = FONT_BATCH_SIZE;
    //         }
    //
    //         gl.BufferSubData<FontVertex>(GLEnum.ArrayBuffer, 0, data);
    //         gl.DrawArrays(GLEnum.Triangles, 0, (uint)vertexCount);
    //     }
    //
    //     gl.BindBuffer(GLEnum.ArrayBuffer, 0);
    //     gl.BindVertexArray(0);
    //     gl.BindTexture(GLEnum.Texture2D, 0);
    //
    //     gl.Disable(GLEnum.Blend);
    //     // glEnable( GL_DEPTH_TEST );
    //
    //     gl.CheckOpenGL();
    //
    //     font.vertices.count = 0;
    }
}
