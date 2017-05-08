using System;
using Cake.Core.Diagnostics;

namespace Cake.Bakery.Diagnostics
{
    internal sealed class ConsoleLogger : ICakeLog
    {
        public Verbosity Verbosity { get; set; }

        public void Write(Verbosity verbosity, LogLevel level, string format, params object[] args)
        {
            // TODO: Check verbosity
            // Write to STDERR so that STDIO protocol can read from STDOUT
            Console.Error.WriteLine(format, args);
        }
    }
}
