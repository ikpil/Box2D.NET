// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.IO;
using Newtonsoft.Json;

namespace Box2D.NET.Samples;

// todo add camera and draw and remove globals
public class Settings
{
    public const int MAX_TOKENS = 32;
    public const string fileName = "settings.ini";

    public int sampleIndex { get; set; } = 0;
    public int windowWidth { get; set; } = 1920;
    public int windowHeight { get; set; } = 1080;
    public float hertz { get; set; } = 60.0f;
    public int subStepCount { get; set; } = 4;
    public int workerCount { get; set; } = 1;
    public bool useCameraBounds { get; set; } = false;
    public bool drawShapes { get; set; } = true;
    public bool drawJoints { get; set; } = true;
    public bool drawJointExtras { get; set; } = false;
    public bool drawAABBs { get; set; } = false;
    public bool drawContactPoints { get; set; } = false;
    public bool drawContactNormals { get; set; } = false;
    public bool drawContactImpulses { get; set; } = false;
    public bool drawFrictionImpulses { get; set; } = false;
    public bool drawMass { get; set; } = false;
    public bool drawBodyNames { get; set; } = false;
    public bool drawGraphColors { get; set; } = false;
    public bool drawCounters { get; set; } = false;
    public bool drawProfile { get; set; } = false;
    public bool enableWarmStarting { get; set; } = true;
    public bool enableContinuous { get; set; } = true;
    public bool enableSleep { get; set; } = true;
    public bool pause { get; set; } = false;
    public bool singleStep { get; set; } = false;
    public bool restart { get; set; } = false;

    public void Save()
    {
        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(fileName, json);
    }

    public void Load()
    {
        if (!File.Exists(fileName))
            return;

        string json = File.ReadAllText(fileName);
        var loaded = JsonConvert.DeserializeObject<Settings>(json);
        CopyFrom(loaded);
    }

    public void CopyFrom(Settings other)
    {
        sampleIndex = other.sampleIndex;
        windowWidth = other.windowWidth;
        windowHeight = other.windowHeight;
        hertz = other.hertz;
        subStepCount = other.subStepCount;
        workerCount = other.workerCount;
        useCameraBounds = other.useCameraBounds;
        drawShapes = other.drawShapes;
        drawJoints = other.drawJoints;
        drawJointExtras = other.drawJointExtras;
        drawAABBs = other.drawAABBs;
        drawContactPoints = other.drawContactPoints;
        drawContactNormals = other.drawContactNormals;
        drawContactImpulses = other.drawContactImpulses;
        drawFrictionImpulses = other.drawFrictionImpulses;
        drawMass = other.drawMass;
        drawBodyNames = other.drawBodyNames;
        drawGraphColors = other.drawGraphColors;
        drawCounters = other.drawCounters;
        drawProfile = other.drawProfile;
        enableWarmStarting = other.enableWarmStarting;
        enableContinuous = other.enableContinuous;
        enableSleep = other.enableSleep;
        pause = other.pause;
        singleStep = other.singleStep;
        restart = other.restart;
    }
}
