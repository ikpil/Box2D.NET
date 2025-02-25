// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

using System.IO;
using System.Threading;
using Box2D.NET.Samples.Helpers;
using Serilog;
using Box2D.NET.Samples;

public static class Program
{
    public const int IMGUI_DISABLE_OBSOLETE_FUNCTIONS = 1; // todo: @ikpil check!?

    private static void InitializeLogger()
    {
        var format = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj} [{ThreadName}:{ThreadId}]{NewLine}{Exception}";
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.WithThreadId()
            .Enrich.WithThreadName()
            .WriteTo.Async(c => c.LogMessageBroker(outputTemplate: format))
            .WriteTo.Async(c => c.Console(outputTemplate: format))
            .WriteTo.Async(c => c.File(
                "logs/log.log",
                rollingInterval: RollingInterval.Hour,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: null,
                outputTemplate: format)
            )
            .CreateLogger();
    }

    private static void InitializeWorkingDirectory()
    {
        var path = DirectoryUtils.SearchFile("LICENSE");
        if (!string.IsNullOrEmpty(path))
        {
            var workingDirectory = Path.GetDirectoryName(path) ?? string.Empty;
            workingDirectory = Path.GetFullPath(workingDirectory);
            Directory.SetCurrentDirectory(workingDirectory);
        }
    }


    public static int Main(string[] args)
    {
        Thread.CurrentThread.Name ??= "main";
        InitializeWorkingDirectory();
        InitializeLogger();

        Thread.Sleep(1);

        var app = new SampleApp();
        var status = app.Run();

        return status;
    }
}