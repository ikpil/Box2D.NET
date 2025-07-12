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
    public int windowWidth = 1920;
    public int windowHeight = 1080;

    public float uiScale = 1.0f;
    public float hertz = 60.0f;
    public int subStepCount = 4;
    public int workerCount = 1;

    public bool drawShapes = true;
    public bool drawJoints = true;
    public bool drawJointExtras = false;
    public bool drawBounds = false;
    public bool drawMass = false;
    public bool drawBodyNames = false;
    public bool drawContactPoints = false;
    public bool drawContactNormals = false;
    public bool drawContactImpulses = false;
    public bool drawContactFeatures = false;
    public bool drawFrictionImpulses = false;
    public bool drawIslands = false;
    public bool drawGraphColors = false;
    public bool drawCounters = false;
    public bool drawProfile = false;
    public bool enableWarmStarting = true;
    public bool enableContinuous = true;
    public bool enableSleep = true;
    public bool pause = false;
    public bool singleStep = false;
    public bool restart = false;

    public void Save()
    {
        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(fileName, json, Encoding.UTF8);
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

        uiScale = other.uiScale;
        hertz = other.hertz;
        subStepCount = other.subStepCount;
        workerCount = other.workerCount;

        drawShapes = other.drawShapes;
        drawJoints = other.drawJoints;
        drawJointExtras = other.drawJointExtras;
        drawBounds = other.drawBounds;
        drawMass = other.drawMass;
        drawBodyNames = other.drawBodyNames;
        drawContactPoints = other.drawContactPoints;
        drawContactNormals = other.drawContactNormals;
        drawContactImpulses = other.drawContactImpulses;
        drawContactFeatures = other.drawContactFeatures;
        drawFrictionImpulses = other.drawFrictionImpulses;
        drawIslands = other.drawIslands;
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