using System.Collections.Generic;
using Box2D.NET.Samples.Primitives;

namespace Box2D.NET.Samples.Samples;

public class SampleFactory
{
    public static readonly SampleFactory Shared = new SampleFactory();

    private readonly List<SampleEntry> _sampleEntries;
    public int SampleCount => _sampleEntries.Count;

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
        var entry = GetInternal(index);
        var sample = entry.CreateFcn.Invoke(setting);
        return sample;
    }

    public string GetCategory(int index)
    {
        var entry = GetInternal(index);
        return entry.Category;
    }
    
    public string GetName(int index)
    {
        var entry = GetInternal(index);
        return entry.Name;
    }

    private SampleEntry GetInternal(int index)
    {
        var sample = _sampleEntries[index];
        return sample;
    }
}