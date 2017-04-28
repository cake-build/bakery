using System;
using System.IO;
using Cake.Core.Diagnostics;
using Cake.ScriptServer.IO;

namespace Cake.ScriptServer.Diagnostics
{
    internal class ConsoleLogger : ICakeLog
    {
        private readonly TextWriter _textWriter;

        public ConsoleLogger(IConsole console)
        {
            _textWriter = console?.StdErr ?? throw new ArgumentNullException(nameof(console));
        }

        public void Write(Verbosity verbosity, LogLevel level, string format, params object[] args)
        {
            // TODO: Check verbosity
            _textWriter.WriteLine(format, args);
        }

        public Verbosity Verbosity { get; set; }
    }
}
