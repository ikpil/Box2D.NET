// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Box2D.NET.Test;

public class B2PublicApiTests
{
    private static IEnumerable<TestCaseData> UpstreamPublicApis()
    {
        yield return PublicApi(typeof(B2Cores), nameof(B2Cores.b2SetLengthUnitsPerMeter), typeof(void), 1, false);
        yield return PublicApi(typeof(B2Cores), nameof(B2Cores.b2GetLengthUnitsPerMeter), typeof(float), 0, false);
        yield return PublicApi(typeof(B2Geometries), nameof(B2Geometries.b2IsValidRay), typeof(bool), 1, false);
        yield return PublicApi(typeof(B2Geometries), nameof(B2Geometries.b2ShapeCastCircle), typeof(B2CastOutput), 2, false);
        yield return PublicApi(typeof(B2Geometries), nameof(B2Geometries.b2ShapeCastCapsule), typeof(B2CastOutput), 2, false);
        yield return PublicApi(typeof(B2Geometries), nameof(B2Geometries.b2ShapeCastSegment), typeof(B2CastOutput), 2, false);
        yield return PublicApi(typeof(B2Geometries), nameof(B2Geometries.b2ShapeCastPolygon), typeof(B2CastOutput), 2, false);
        yield return PublicApi(typeof(B2DynamicTrees), nameof(B2DynamicTrees.b2DynamicTree_ShapeCast), typeof(B2TreeStats), 5, true);
        yield return PublicApi(typeof(B2DynamicTrees), nameof(B2DynamicTrees.b2DynamicTree_ValidateNoEnlarged), typeof(void), 1, false);
    }

    private static TestCaseData PublicApi(Type declaringType, string methodName, Type returnType, int parameterCount, bool isGeneric)
    {
        return new TestCaseData(declaringType, methodName, returnType, parameterCount, isGeneric)
            .SetName($"PublicApi_{methodName}");
    }

    [TestCaseSource(nameof(UpstreamPublicApis))]
    public void UpstreamB2ApiMethodIsPublic(Type declaringType, string methodName, Type returnType, int parameterCount, bool isGeneric)
    {
        MethodInfo[] methods = declaringType
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.Name == methodName)
            .ToArray();

        Assert.That(methods, Has.Length.EqualTo(1), $"Expected exactly one C# counterpart for upstream B2_API {methodName}");

        MethodInfo method = methods[0];
        Assert.That(declaringType.IsPublic, Is.True, $"Declaring type {declaringType.FullName} must be public");
        Assert.That(method.IsPublic, Is.True, $"Upstream B2_API {methodName} must remain public in C#");
        Assert.That(method.IsStatic, Is.True, $"Upstream free function {methodName} must remain static in C#");
        Assert.That(method.ReturnType, Is.EqualTo(returnType), $"Return type changed for {methodName}");
        Assert.That(method.GetParameters(), Has.Length.EqualTo(parameterCount), $"Parameter count changed for {methodName}");
        Assert.That(method.IsGenericMethodDefinition, Is.EqualTo(isGeneric), $"Generic adaptation changed for {methodName}");
    }
}
