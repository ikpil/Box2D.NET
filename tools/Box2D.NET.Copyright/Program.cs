// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Box2D.NET.Copyright;

public static class Program
{
    private static readonly string LineBreak = Environment.NewLine;

    private static readonly string HeaderPattern = @"// SPDX-FileCopyrightText:\s*(\d{4})\s+([A-Za-z\s]+(?:\([^\)]+\))?)";

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

    private static bool TryParseYears(string sourcePath, out string erinYear, out string ikpilYear)
    {
        string content = File.ReadAllText(sourcePath);

        erinYear = string.Empty;
        ikpilYear = string.Empty;

        MatchCollection matches = Regex.Matches(content, HeaderPattern);
        foreach (Match match in matches)
        {
            string year = match.Groups[1].Value;
            string author = match.Groups[2].Value;
            if (author.Contains("Erin", StringComparison.OrdinalIgnoreCase) || author.Contains("Catto", StringComparison.OrdinalIgnoreCase))
            {
                erinYear = year;
            }
            else if (author.Contains("ikpil", StringComparison.OrdinalIgnoreCase))
            {
                ikpilYear = year;
            }
        }

        return !string.IsNullOrEmpty(ikpilYear);
    }

    private static void AddHeaderToSourceFile(string sourcePath)
    {
        var hasCopyright = TryParseYears(sourcePath, out var erinYear, out var ikpilYear);

        Console.WriteLine($"{sourcePath}");
        if (hasCopyright)
        {
            Console.WriteLine("Source file already contains notice");
            return;
        }

        var year = "" + DateTime.UtcNow.Year;
        if (string.IsNullOrEmpty(erinYear))
        {
            erinYear = year;
        }

        if (string.IsNullOrEmpty(ikpilYear))
        {
            ikpilYear = year;
        }

        Console.WriteLine("Source file does not contain notice -- adding");
        var source = File.ReadAllText(sourcePath);

        // remove lines
        string pattern = @"^// SPDX-.*\r?\n?";
        string result = Regex.Replace(source, pattern, string.Empty, RegexOptions.Multiline);
        
        // RemoveEmptyLinesUntilNonEmpty
        bool foundNonEmptyLine = false;
        string[] lines = result.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var output = new StringBuilder();

        foreach (var line in lines)
        {
            if (!foundNonEmptyLine)
            {
                // If the line is empty (only whitespace), skip it
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                
                // Once a non-empty line is found, start adding to the output
                foundNonEmptyLine = true;
            }

            output.AppendLine(line);
        }

        string copyright = string.Empty;
        copyright += $"// SPDX-FileCopyrightText: {erinYear} Erin Catto{LineBreak}";
        copyright += $"// SPDX-FileCopyrightText: {ikpilYear} Ikpil Choi(ikpil@naver.com){LineBreak}";
        copyright += $"// SPDX-License-Identifier: MIT{LineBreak}{LineBreak}";

        var content = copyright + output;

        File.WriteAllText(sourcePath, content, Encoding.UTF8);
    }
}