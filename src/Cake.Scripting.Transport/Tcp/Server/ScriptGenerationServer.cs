// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Cake.Scripting.Abstractions;
using Cake.Scripting.Transport.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cake.Scripting.Transport.Tcp.Server
{
    public class ScriptGenerationServer : IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ILogger _logger;
        private readonly IScriptGenerationService _service;

        public ScriptGenerationServer(IScriptGenerationService service, int port, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger(typeof(ScriptGenerationServer)) ?? NullLogger.Instance;
            _cancellationTokenSource = new CancellationTokenSource();
            _service = service ?? throw new ArgumentNullException(nameof(service));

            RunAsync(port, _cancellationTokenSource.Token).ConfigureAwait(false);
        }

        private async Task RunAsync(int port, CancellationToken cancellationToken)
        {
            using (var client = new TcpClient())
            {
                _logger.LogDebug("Connecting to tcp server on port {port}", port);
                await client.ConnectAsync(IPAddress.Loopback, port);
                _logger.LogDebug("Connected");

                using (var stream = client.GetStream())
                using (var reader = new BinaryReader(stream))
                using (var writer = new BinaryWriter(stream))
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        while (!stream.DataAvailable)
                        {
                            await Task.Delay(100, cancellationToken);
                        }

                        // Request
                        _logger.LogDebug("Received reqeust from client");
                        var fileChange = FileChangeSerializer.Deserialize(reader);
                        var cakeScript = _service.Generate(fileChange);

                        // Response
                        _logger.LogDebug("Sending response to client");
                        CakeScriptSerializer.Serialize(writer, cakeScript);
                        writer.Flush();
                    }
                }
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel(false);
        }
    }
}
