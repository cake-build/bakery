// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Cake.Core.Diagnostics;
using Microsoft.Extensions.Logging;
using LogLevel = Cake.Core.Diagnostics.LogLevel;
using MSLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Cake.Bakery.Diagnostics
{
    internal sealed class CakeLog : ICakeLog
    {
        private readonly ILogger _logger;

        private static readonly Dictionary<LogLevel, MSLogLevel> ToLogLevel = new Dictionary<LogLevel, MSLogLevel>
        {
            { LogLevel.Fatal, MSLogLevel.Critical },
            { LogLevel.Error, MSLogLevel.Error },
            { LogLevel.Warning, MSLogLevel.Warning },
            { LogLevel.Information, MSLogLevel.Information },
            { LogLevel.Verbose, MSLogLevel.Debug },
            { LogLevel.Debug, MSLogLevel.Debug }
        };

        private static string FormatLogValues(LogValues values, Exception ex)
        {
            return string.Format(values.Format, values.Args);
        }

        public CakeLog(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger("CakeLog") ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public Verbosity Verbosity { get; set; }

        public void Write(Verbosity verbosity, LogLevel level, string format, params object[] args)
        {
            // TODO: Check verbosity
            _logger.Log(ToLogLevel[level], 0, new LogValues(format, args), null, FormatLogValues);
        }

        private struct LogValues
        {
            public readonly string Format;
            public readonly object[] Args;

            public LogValues(string format, params object[] args)
            {
                Format = format;
                Args = args;
            }
        }
    }
}
