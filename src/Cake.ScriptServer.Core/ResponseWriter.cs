using System;
using System.IO;

namespace Cake.ScriptServer.Core
{
    public sealed class ResponseWriter : IResponseWriter
    {
        private readonly TextWriter _writer;

        public ResponseWriter(TextWriter writer)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        public void WriteResponse(string response)
        {
            _writer.WriteLine(response);
        }
    }
}
