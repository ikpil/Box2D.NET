// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using static Box2D.NET.Samples.Graphics.Cameras;

namespace Box2D.NET.Samples.Graphics;

public static partial class Draws
{
    public static void DrawScreenString(Draw draw, float x, float y, B2HexColor color, string message)
    {
        message = message.Length > 255 ? message[..255] : message;
        uint hex = (uint)color;

        // b2HexColor packs as 0xRRGGBB; matches MakeRGBA8's byte order. Force alpha to opaque.
        uint col = ImGui.ColorConvertFloat4ToU32(new Vector4(((hex >> 16) & 0xFF) / 255.0f, ((hex >> 8) & 0xFF) / 255.0f,
            (hex & 0xFF) / 255.0f, 1.0f));

        // Old STB path treated y as the text baseline; ImGui::AddText treats y as the top of the
        // glyph. Shift up by the font ascent so existing callers work.
        float ascent = ImGui.GetFont().Ascent;
        Vector2 viewportPosition = ImGui.GetMainViewport().Pos;
        ImGui.GetBackgroundDrawList().AddText(viewportPosition + new Vector2(x, y - ascent), col, message);
    }

    public static void DrawWorldString(Draw draw, Camera camera, B2Vec2 p, B2HexColor color, string message)
    {
        message = message.Length > 255 ? message[..255] : message;
        B2Vec2 ps = ConvertWorldToScreen(camera, p);
        DrawScreenString(draw, ps.X, ps.Y, color, message);
    }
}
