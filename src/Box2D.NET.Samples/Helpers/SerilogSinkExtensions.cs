// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace Box2D.NET.Samples.Helpers;

public static class SerilogSinkExtensions
{
    public static LoggerConfiguration LogMessageBroker(
        this LoggerSinkConfiguration sinkConfiguration,
        LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose,
        string outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    {
        var formatter = new MessageTemplateTextFormatter(outputTemplate);
        return sinkConfiguration.Sink(new LogMessageBrokerSink(formatter));
    }
}
