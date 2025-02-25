// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.IO;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace Box2D.NET.Samples.Helpers;

public class LogMessageBrokerSink : ILogEventSink
{
    public static event Action<int, string> OnEmitted;

    private readonly ITextFormatter _formatter;

    public LogMessageBrokerSink(ITextFormatter formatter)
    {
        _formatter = formatter;
    }

    public void Emit(LogEvent logEvent)
    {
        using var writer = new StringWriter();
        _formatter.Format(logEvent, writer);
        OnEmitted?.Invoke((int)logEvent.Level, writer.ToString());
    }
}
