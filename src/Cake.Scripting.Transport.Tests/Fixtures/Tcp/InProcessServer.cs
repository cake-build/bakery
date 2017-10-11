// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cake.Scripting.Abstractions;
using Cake.Scripting.Transport.Tcp.Client;
using Cake.Scripting.Transport.Tcp.Server;
using Microsoft.Extensions.Logging;

namespace Cake.Scripting.Transport.Tests.Fixtures.Tcp
{
    internal sealed class InProcessServer : IScriptGenerationProcess
    {
        private readonly IScriptGenerationService _service;
        private readonly ILoggerFactory _loggerFactory;
        private ScriptGenerationServer _server;

        public InProcessServer(IScriptGenerationService service, ILoggerFactory loggerFactory)
        {
            _service = service;
            _loggerFactory = loggerFactory;
        }

        public void Dispose()
        {
            _server?.Dispose();
        }

        public string ServerExecutablePath { get; set; }

        public void Start(int port, string workingDirectory)
        {
            _server = new ScriptGenerationServer(_service, port, _loggerFactory);
            _server.Start();
        }
    }
}
