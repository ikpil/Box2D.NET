namespace Box2D.NET.Samples.Test;

public static class Helpers
{
    public static Settings CreateSettings()
    {
        var settings = new Settings();
        settings.sampleIndex = 0;
        settings.windowWidth = 1920;
        settings.windowHeight = 1080;
        settings.hertz = 60.0f;
        settings.subStepCount = 4;
        settings.workerCount = 1;
        settings.useCameraBounds = false;
        settings.drawShapes = true;
        settings.drawJoints = true;
        settings.drawJointExtras = false;
        settings.drawAABBs = false;
        settings.drawContactPoints = false;
        settings.drawContactNormals = false;
        settings.drawContactImpulses = false;
        settings.drawFrictionImpulses = false;
        settings.drawMass = false;
        settings.drawBodyNames = false;
        settings.drawGraphColors = false;
        settings.drawCounters = false;
        settings.drawProfile = false;
        settings.enableWarmStarting = true;
        settings.enableContinuous = true;
        settings.enableSleep = true;
        settings.pause = false;
        settings.singleStep = false;
        settings.restart = false;

        return settings;
    }
 
}