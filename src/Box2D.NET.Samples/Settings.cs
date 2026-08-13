// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Box2D.NET.Samples;

// todo add camera and draw and remove globals
public class Settings
{
    public const int MAX_TOKENS = 32;
    public const string fileName = "settings.ini";

    public int sampleIndex = 0;
    public bool drawShapes = true;
    public bool drawJoints = true;
    public bool showDiagnostics = false;

    public static void Save(SampleContext context)
    {
        var setting = CopyFrom(context);
        
        string json = JsonConvert.SerializeObject(setting, Formatting.Indented);
        File.WriteAllText(fileName, json, Encoding.UTF8);
    }

    public static Settings Load()
    {
        if (!File.Exists(fileName))
        {
            return new Settings();
        }

        string json = File.ReadAllText(fileName);
        var setting = JsonConvert.DeserializeObject<Settings>(json);
        return setting;
    }

    public static Settings CopyFrom(SampleContext context)
    {
        var setting = new Settings();
        setting.sampleIndex = context.sampleIndex;
        setting.drawShapes = context.debugDraw.drawShapes;
        setting.drawJoints = context.debugDraw.drawJoints;
        setting.showDiagnostics = context.showDiagnostics;

        return setting;
    }
}
