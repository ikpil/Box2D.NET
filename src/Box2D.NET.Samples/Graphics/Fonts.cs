// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using Silk.NET.OpenGL;
using Box2D.NET.Samples.Primitives;
using ImGuiNET;

namespace Box2D.NET.Samples.Graphics;

public static class Fonts
{
    public static Font CreateFont(GL gl, string trueTypeFile, float fontSize)
    {
        Font font = new Font();
        return font;
    }

    public static void DestroyFont(GL gl, ref Font font)
    {
        font.texts.Clear();
    }

    public static void AddText(ref Font font, float x, float y, B2HexColor color, string text)
    {
        var fontText = new FontText();
        fontText.x = x;
        fontText.y = y;
        fontText.color = color;
        fontText.text = text;

        font.texts.Add(fontText);
    }

    public static void FlushText(GL gl, ref Font font, Camera camera)
    {
        if (0 >= font.texts.Count)
            return;
        
        ImGui.Begin("Overlay",
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize |
            ImGuiWindowFlags.NoScrollbar);

        foreach (var text in font.texts)
        {
            int hex = (int)text.color;
            float r = ((hex >> 16) & 0xFF) / 255.0f;
            float g = ((hex >> 8) & 0xFF) / 255.0f;
            float b = (hex & 0xFF) / 255.0f;

            ImGui.SetCursorPos(new Vector2(text.x, text.y));
            ImGui.TextColored(new Vector4(r, g, b, 1.0f), text.text);
        }

        ImGui.End();
        font.texts.Clear();
    }
}