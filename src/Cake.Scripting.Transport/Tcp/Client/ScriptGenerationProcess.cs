// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cake.Scripting.Transport.Tcp.Client
{
    internal class ScriptGenerationProcess : IScriptGenerationProcess
    {
        private readonly ILogger _logger;
        private Process _process;

        public ScriptGenerationProcess(string serverExecutablePath, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger(typeof(ScriptGenerationProcess)) ?? NullLogger.Instance;
            ServerExecutablePath = serverExecutablePath;
        }

        public void Dispose()
        {
            _process?.Kill();
            _process?.WaitForExit();
            _process?.Dispose();
        }

        public void Start(int port, string workingDirectory)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = ServerExecutablePath,
                Arguments = $"--port={port}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = workingDirectory
            };

            _logger.LogDebug("Starting \"{fileName}\" with arguments \"{arguments}\"", startInfo.FileName, startInfo.Arguments);
            _process = Process.Start(startInfo);
            _process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    _logger.LogDebug(e.Data);
                }
            };
            _process.BeginErrorReadLine();
            _process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    _logger.LogDebug(e.Data);
                }
            };
            _process.BeginOutputReadLine();
        }

        public string ServerExecutablePath { get; set; }
    }
}
