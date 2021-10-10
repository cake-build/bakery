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
using Cake.Scripting.Abstractions.Models;
using Cake.Scripting.Transport.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cake.Scripting.Transport.Tcp.Server
{
    public class ScriptGenerationServer : IDisposable
    {
        public event EventHandler OnDisconnected;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ILogger _logger;
        private readonly IScriptGenerationService _service;
        private readonly int _port;

        public ScriptGenerationServer(IScriptGenerationService service, int port, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger(typeof(ScriptGenerationServer)) ?? NullLogger.Instance;
            _cancellationTokenSource = new CancellationTokenSource();
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _port = port;
        }

        public void Start()
        {
            RunAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel(false);
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            using (var client = new TcpClient())
            {
                _logger.LogDebug("Connecting to tcp server on port {port}", _port);
                await client.ConnectAsync(IPAddress.Loopback, _port);
                _logger.LogDebug("Connected");

                using (var stream = client.GetStream())
                using (var reader = new BinaryReader(stream))
                using (var writer = new BinaryWriter(stream))
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        while (!stream.DataAvailable)
                        {
                            if (!client.Client.IsConnected())
                            {
                                OnDisconnected?.Invoke(this, EventArgs.Empty);
                                return;
                            }
                            await Task.Delay(10, cancellationToken);
                        }

                        // Request
                        _logger.LogDebug("Received reqeust from client");
                        var fileChange = FileChangeSerializer.Deserialize(reader, out var version);
                        var cakeScript = CakeScript.Empty;

                        try
                        {
                            cakeScript = _service.Generate(fileChange);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(0, e, "Unhandled Exception while processing request.");
                        }

                        // Response
                        _logger.LogDebug("Sending response to client");
                        CakeScriptSerializer.Serialize(writer, cakeScript, version);
                        writer.Flush();
                    }
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
