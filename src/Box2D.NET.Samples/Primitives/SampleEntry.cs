// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Samples.Samples;

namespace Box2D.NET.Samples.Primitives;

public delegate Sample SampleCreateFcn(Settings settings);

public readonly struct SampleEntry
{
    public readonly string category;
    public readonly string name;
    public readonly SampleCreateFcn createFcn;

    public SampleEntry(string category, string name, SampleCreateFcn createFcn)
    {
        this.category = category;
        this.name = name;
        this.createFcn = createFcn;
    }
}