using System;
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
        private readonly ManualResetEvent _initializedEvent = new ManualResetEvent(false);
        private readonly object _sendReceiveLock = new object();
        private readonly TcpListener _listener;
        private readonly IScriptGenerationProcess _process;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private BinaryWriter _writer;
        private BinaryReader _reader;
        private NetworkStream _stream;

        public ScriptGenerationClient(string serverExecutablePath) :
            this(new ScriptGenerationProcess(serverExecutablePath))
        {
        }

        public ScriptGenerationClient(IScriptGenerationProcess process)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            _cancellationTokenSource = new CancellationTokenSource();

            RunAsync(_cancellationTokenSource.Token).ConfigureAwait(false);

            var port = ((IPEndPoint)_listener.LocalEndpoint).Port;

            _process.Start(port);
        }

        public CakeScript Generate(FileChange fileChange)
        {
            _initializedEvent.WaitOne();

            lock (_sendReceiveLock)
            {
                // Send
                FileChangeSerializer.Serialize(_writer, fileChange);
                _writer.Flush();

                while (!_stream.DataAvailable)
                {
                    Task.Delay(100).Wait();
                }

                // Receive
                return CakeScriptSerializer.Deserialize(_reader);
            }
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            using (var client = await _listener.AcceptTcpClientAsync())
            {
                _stream = client.GetStream();
                _reader = new BinaryReader(_stream);
                _writer = new BinaryWriter(_stream);
                _initializedEvent.Set();

                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }
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
