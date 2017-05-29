using System.Diagnostics;

namespace Cake.Scripting.Transport.Tcp.Client
{
    internal class ScriptGenerationProcess : IScriptGenerationProcess
    {
        private Process _process;

        public ScriptGenerationProcess(string serverExecutablePath)
        {
            ServerExecutablePath = serverExecutablePath;
        }

        public void Dispose()
        {
            _process?.Dispose();
        }

        public void Start(int port)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = ServerExecutablePath,
                Arguments = $"--port={port}"
            };

            _process = Process.Start(startInfo);
        }

        public string ServerExecutablePath { get; set; }
    }
}
