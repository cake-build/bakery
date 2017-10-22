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

namespace Cake.Scripting.Transport.Tcp.Client
{
    public sealed class ScriptGenerationClient : IScriptGenerationService, IDisposable
    {
        private readonly ManualResetEvent _initializedEvent = new ManualResetEvent(false);
        private readonly object _sendReceiveLock = new object();
        private readonly TcpListener _listener;
        private readonly IScriptGenerationProcess _process;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ILogger _logger;
        private BinaryWriter _writer;
        private BinaryReader _reader;
        private NetworkStream _stream;

        public ScriptGenerationClient(string serverExecutablePath, string workingDirectory, ILoggerFactory loggerFactory)
            : this(new ScriptGenerationProcess(serverExecutablePath, loggerFactory), workingDirectory, loggerFactory)
        {
        }

        public ScriptGenerationClient(IScriptGenerationProcess process, string workingDirectory, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger(typeof(ScriptGenerationClient)) ?? NullLogger.Instance;
            _process = process ?? throw new ArgumentNullException(nameof(process));
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            _cancellationTokenSource = new CancellationTokenSource();

            RunAsync(_cancellationTokenSource.Token).ConfigureAwait(false);

            var port = ((IPEndPoint)_listener.LocalEndpoint).Port;

            _process.Start(port, workingDirectory);
        }

        public CakeScript Generate(FileChange fileChange)
        {
            _initializedEvent.WaitOne();

            lock (_sendReceiveLock)
            {
                // Send
                _logger.LogDebug("Sending request to server");
                FileChangeSerializer.Serialize(_writer, fileChange);
                _writer.Flush();

                while (!_stream.DataAvailable)
                {
                    Task.Delay(10).Wait();
                }

                // Receive
                _logger.LogDebug("Received response from server");
                return CakeScriptSerializer.Deserialize(_reader);
            }
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Waiting for server to connect");
            using (var client = await _listener.AcceptTcpClientAsync())
            {
                _logger.LogDebug("Server connected");
                _stream = client.GetStream();
                _reader = new BinaryReader(_stream);
                _writer = new BinaryWriter(_stream);
                _initializedEvent.Set();

                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }
            _logger.LogDebug("Shutting down");
            _initializedEvent.Reset();

            _listener.Stop();
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel(false);
            _process.Dispose();
            _stream?.Dispose();
            _writer?.Dispose();
            _reader?.Dispose();
        }
    }
}
