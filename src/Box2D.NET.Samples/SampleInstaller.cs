using System.Diagnostics;
using Box2D.NET.Primitives;
using Box2D.NET.Samples.Primitives;

namespace Box2D.NET.Samples;

public static class SampleInstaller
{
#if DEBUG
    public const bool g_sampleDebug = true;
#else
    public const bool g_sampleDebug = false;
#endif
    public const int k_maxContactPoints = 12 * 2048;

    public const int MAX_SAMPLES = 256;
    public static SampleEntry[] g_sampleEntries = new SampleEntry[MAX_SAMPLES];
    public static int g_sampleCount = 0;

    public static int RegisterSample(string category, string name, SampleCreateFcn fcn)
    {
        int index = g_sampleCount;
        if (index < MAX_SAMPLES)
        {
            g_sampleEntries[index] = new SampleEntry(category, name, fcn);
            ++g_sampleCount;
            return index;
        }

        return -1;
    }
}