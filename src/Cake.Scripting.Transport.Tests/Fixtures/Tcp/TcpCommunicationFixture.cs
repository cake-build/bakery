// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Cake.Scripting.Abstractions.Models;
using Cake.Scripting.Transport.Tcp.Client;

namespace Cake.Scripting.Transport.Tests.Fixtures.Tcp
{
    public sealed class TcpCommunicationFixture : IDisposable
    {
        private readonly ScriptGenerationService _service;

        public TcpCommunicationFixture()
        {
            _service = new ScriptGenerationService();

            Client = new ScriptGenerationClient(new InProcessServer(_service, null), string.Empty, null);
        }

        public ScriptGenerationClient Client { get; }

        public Func<FileChange, CakeScript> ServerCallback
        {
            get => _service.GenerateCallback;
            set => _service.GenerateCallback = value;
        }

        public void Dispose()
        {
            Client?.Dispose();
        }
    }
}
