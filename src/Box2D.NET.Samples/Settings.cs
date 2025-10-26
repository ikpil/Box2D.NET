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

    public int windowWidth = 1920;
    public int windowHeight = 1080;
    
    //
    public float uiScale = 1.0f;
    public float hertz = 60.0f;
    public float jointScale = 1.0f;
    public float forceScale = 1.0f;
    public int subStepCount = 4;
    public int workerCount = 1;
    
    //
    public bool singleStep = false;
    public bool drawJointExtras = false;
    public bool drawBounds = false;
    public bool drawMass = false;
    public bool drawBodyNames = false;
    public bool drawContactPoints = false;
    public bool drawContactNormals = false;
    public bool drawContactFeatures = false;
    public bool drawContactForces = false;
    public bool drawFrictionForces = false;
    public bool drawIslands = false;
    public bool drawGraphColors = false;
    public bool drawCounters = false;
    public bool drawProfile = false;
    public bool enableWarmStarting = true;
    public bool enableContinuous = true;
    public bool enableSleep = true;
    
    //
    public int sampleIndex = 0;
    public bool drawShapes = true;
    public bool drawJoints = true;

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
        setting.windowWidth = (int)context.camera.width;
        setting.windowHeight = (int)context.camera.height;
        
        setting.sampleIndex = context.sampleIndex;
        //
        setting.uiScale = context.uiScale;
        setting.hertz = context.hertz;
        setting.subStepCount = context.subStepCount;
        setting.workerCount = context.workerCount;

        //
        setting.drawCounters = context.drawCounters;
        setting.drawProfile = context.drawProfile;
        setting.enableWarmStarting = context.enableWarmStarting;
        setting.enableContinuous = context.enableContinuous;
        setting.enableSleep = context.enableSleep;
        setting.singleStep = context.singleStep;
        
        //
        setting.drawShapes = context.debugDraw.drawShapes;
        setting.drawJoints = context.debugDraw.drawJoints;
        setting.drawJointExtras = context.debugDraw.drawJointExtras;
        setting.drawBounds = context.debugDraw.drawBounds;
        setting.drawMass = context.debugDraw.drawMass;
        setting.drawBodyNames = context.debugDraw.drawBodyNames;
        setting.drawContactPoints = context.debugDraw.drawContactPoints;
        setting.drawContactNormals = context.debugDraw.drawContactNormals;
        setting.drawContactForces = context.debugDraw.drawContactForces;
        setting.drawContactFeatures = context.debugDraw.drawContactFeatures;
        setting.drawFrictionForces = context.debugDraw.drawFrictionForces;
        setting.drawIslands = context.debugDraw.drawIslands;
        setting.drawGraphColors = context.debugDraw.drawGraphColors;

        return setting;
    }
}