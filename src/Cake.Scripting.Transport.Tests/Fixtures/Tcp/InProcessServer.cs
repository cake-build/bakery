using Cake.Scripting.Abstractions;
using Cake.Scripting.Transport.Tcp.Client;
using Cake.Scripting.Transport.Tcp.Server;

namespace Cake.Scripting.Transport.Tests.Fixtures.Tcp
{
    internal sealed class InProcessServer : IScriptGenerationProcess
    {
        private readonly IScriptGenerationService _service;
        private ScriptGenerationServer _server;

        public InProcessServer(IScriptGenerationService service)
        {
            _service = service;
        }

        public void Dispose()
        {
            _server?.Dispose();
        }

        public string ServerExecutablePath { get; set; }

        public void Start(int port)
        {
            _server = new ScriptGenerationServer(_service, port);
        }
    }
}
