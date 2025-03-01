using System.Collections.Generic;
using Box2D.NET.Samples.Primitives;

namespace Box2D.NET.Samples.Samples;

public class SampleFactory
{
    public static readonly SampleFactory Shared = new SampleFactory();

    private readonly List<SampleEntry> _sampleEntries;


    private SampleFactory()
    {
        _sampleEntries = new List<SampleEntry>();
    }
    
    public int RegisterSample(string category, string name, SampleCreateFcn fcn)
    {
        int index = _sampleEntries.Count;
        var entry = new SampleEntry(category, name, fcn);
        _sampleEntries.Add(entry);
        return index;
    }

    public Sample Create(int index, Settings setting)
    {
        var sample = _sampleEntries[index].createFcn.Invoke(setting);
        return sample;
    }
}