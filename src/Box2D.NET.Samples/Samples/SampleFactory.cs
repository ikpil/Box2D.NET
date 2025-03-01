using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Box2D.NET.Samples.Primitives;
using Box2D.NET.Samples.Samples.Benchmarks;

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

    private static int CompareSamples(SampleEntry sa, SampleEntry sb)
    {
        int result = string.Compare(sa.Category, sb.Category, StringComparison.InvariantCultureIgnoreCase);
        if (result == 0)
        {
            result = string.Compare(sa.Name, sb.Name, StringComparison.InvariantCultureIgnoreCase);
        }

        return result;
    }

    public void LoadSamples()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        var sampleTypes = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Sample)) && !t.IsAbstract)
            .ToList();

        int index = 0;
        foreach (var type in sampleTypes)
        {
            // Attributes = {MethodAttributes} Private | Static | HideBySig | SpecialName | RTSpecialName
            // BindingFlags = {BindingFlags} Static | NonPublic
                
            var method = type.GetConstructors(BindingFlags.Static | BindingFlags.NonPublic);
            if (0 >= method.Length)
            {
                Console.WriteLine($"can not load sample - {type.Name}");
                continue;
            }

            method[0].Invoke(null, null);
            var title = GetTitle(index);
            Console.WriteLine($"loaded sample - {title}");

            ++index;
        }
    }

    public void SortSamples()
    {
        _sampleEntries.Sort(CompareSamples);
    }

    public string GetTitle(int index)
    {
        var entry = GetInternal(index);
        return entry.Title;
    }
}