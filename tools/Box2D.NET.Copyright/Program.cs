// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Box2D.NET.Copyright;

public static class Program
{
    private static readonly string LineBreak = Environment.NewLine;

    private static readonly string HeaderPattern = @"// SPDX-FileCopyrightText:\s*(\d{4})\s+([A-Za-z\s]+(?:\([^\)]+\))?)";
    private static readonly string NoticeTemplate = $"// Copyright (c) Microsoft. All rights reserved.{LineBreak}" +
                                                    $"// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com){LineBreak}" +
                                                    $"// SPDX-License-Identifier: MIT{LineBreak}{LineBreak}";

    private static int Main(string[] args)
    {
        var currentDir = Directory.GetCurrentDirectory();
        var sulutionFilePath = FindSolutionRoot(currentDir, "Box2D.NET.sln");
        if (string.IsNullOrEmpty(sulutionFilePath))
        {
            Console.WriteLine($"not found solution root - {currentDir}");
            return -1;
        }

        var parent = Directory.GetParent(sulutionFilePath)!.FullName;
        
        ProcessFiles(Path.Combine(parent, "src"), "*.cs");
        ProcessFiles(Path.Combine(parent, "test"), "*.cs");
        ProcessFiles(Path.Combine(parent, "tools"), "*.cs");

        return 0;
    }

    public static string FindSolutionRoot(string currentPath, string fileName)
    {
        if (string.IsNullOrEmpty(currentPath))
        {
            return null;
        }

        string filePath = Path.Combine(currentPath, fileName);
        if (File.Exists(filePath))
        {
            return filePath;
        }

        string parentPath = Directory.GetParent(currentPath)?.FullName;

        if (parentPath != null && parentPath != currentPath)
        {
            return FindSolutionRoot(parentPath, fileName);
        }

        return string.Empty;
    }

    private static void ProcessFiles(string path, string searchPattern)
    {
        var files = Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories)
            .Where(x => !x.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(x => !x.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .ToList();
            
                
        foreach (var file in files)
        {
            AddHeaderToSourceFile(file);
        }
    }

    private static bool SourceFileContainsNotice(string sourcePath)
    {
        string content = File.ReadAllText(sourcePath);
        return Regex.IsMatch(content, HeaderPattern);
    }

    private static void AddHeaderToSourceFile(string sourcePath)
    {
        var containsNotice = SourceFileContainsNotice(sourcePath);

        Console.WriteLine($"{sourcePath}");
        if (containsNotice)
        {
            Console.WriteLine("Source file already contains notice -- not adding");
        }
        else
        {
            Console.WriteLine("Source file does not contain notice -- adding");
            var fileLines = File.ReadAllText(sourcePath);
            var content = NoticeTemplate + fileLines;

            File.WriteAllText(sourcePath, content);
        }
    }
}