using System;

namespace Cake.Scripting.Transport.Tcp.Client
{
    public interface IScriptGenerationProcess : IDisposable
    {
        string ServerExecutablePath { get; set; }

        void Start(int port);
    }
}
