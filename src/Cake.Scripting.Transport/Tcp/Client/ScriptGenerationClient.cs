using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Cake.Scripting.Abstractions;
using Cake.Scripting.Abstractions.Models;
using Cake.Scripting.Transport.Serialization;

namespace Cake.Scripting.Transport.Tcp.Client
{
    public sealed class ScriptGenerationClient : IScriptGenerationService, IDisposable
    {
        private readonly object _sendReceiveLock = new object();
        private readonly TcpListener _listener;
        private readonly Process _process;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private BinaryWriter _writer;
        private BinaryReader _reader;

        public ScriptGenerationClient(string serverExecutablePath)
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            _cancellationTokenSource = new CancellationTokenSource();
            RunAsync(_cancellationTokenSource.Token).ConfigureAwait(false);

            var port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            var startInfo = new ProcessStartInfo
            {
                FileName = serverExecutablePath,
                Arguments = $"--port={port}"
            };

            _process = Process.Start(startInfo);
        }

        public CakeScript Generate(FileChange fileChange)
        {
            lock (_sendReceiveLock)
            {
                // Send
                FileChangeSerializer.Serialize(_writer, fileChange);
                _writer.Flush();

                // Receive
                return CakeScriptSerializer.Deserialize(_reader);
            }
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            using (var client = await _listener.AcceptTcpClientAsync())
            {
                _reader = new BinaryReader(client.GetStream());
                _writer = new BinaryWriter(client.GetStream());

                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }

            _listener.Stop();
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel(false);
            _process?.Dispose();
            _writer?.Dispose();
            _reader?.Dispose();
        }
    }
}
