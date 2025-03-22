// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using Box2D.NET.Samples.Samples;

namespace Box2D.NET.Samples.Primitives;

public class SampleEntry
{
    public readonly string Category;
    public readonly string Name;
    public readonly string Title;
    public readonly Func<SampleAppContext, Settings, Sample> CreateFcn;

    public SampleEntry(string category, string name, Func<SampleAppContext, Settings, Sample> createFcn)
    {
        Category = category;
        Name = name;
        Title = $"{Category} : {Name}";
        CreateFcn = createFcn;
    }
}