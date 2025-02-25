// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Box2D.NET.Samples.Helpers;

public static class DirectoryUtils
{
    public static string SearchPath(string searchPath, int depth, out bool isDir)
    {
        isDir = false;

        for (int i = 0; i < depth; ++i)
        {
            var relativePath = string.Join("", Enumerable.Range(0, i).Select(x => "../"));
            var searchingPath = Path.Combine(relativePath, searchPath);
            var fullSearchingPath = Path.GetFullPath(searchingPath);

            if (File.Exists(fullSearchingPath))
            {
                return fullSearchingPath;
            }

            if (Directory.Exists(fullSearchingPath))
            {
                isDir = true;
                return fullSearchingPath;
            }
        }

        return string.Empty;
    }

    // only directory
    public static string SearchDirectory(string dirname, int depth = 10)
    {
        var searchingPath = SearchPath(dirname, depth, out var isDir);
        if (isDir)
        {
            return searchingPath;
        }

        var path = Path.GetDirectoryName(searchingPath) ?? string.Empty;
        return path;
    }

    public static string SearchFile(string filename, int depth = 10)
    {
        var searchingPath = SearchPath(filename, depth, out var isDir);
        if (!isDir)
        {
            return searchingPath;
        }

        return string.Empty;
    }

    public static void OpenDirectory(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = path,
                UseShellExecute = true
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "open",
                Arguments = path,
                UseShellExecute = true
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = path,
                UseShellExecute = true
            });
        }
        else
        {
            throw new NotSupportedException("Unsupported OS");
        }
    }
}
