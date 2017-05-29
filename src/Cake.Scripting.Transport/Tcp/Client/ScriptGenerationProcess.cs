using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cake.Scripting.Transport.Tcp.Client
{
    internal class ScriptGenerationProcess : IScriptGenerationProcess
    {
        private Process _process;
        private readonly ILogger _logger;

        public ScriptGenerationProcess(string serverExecutablePath, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger(typeof(ScriptGenerationProcess)) ?? NullLogger.Instance;
            ServerExecutablePath = serverExecutablePath;
        }

        public void Dispose()
        {
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
