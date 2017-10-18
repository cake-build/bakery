using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Cake.Bakery.Diagnostics
{
    public class Logger : ILogger
    {
        private readonly TextWriter _stdErr;
        private readonly TextWriter _stdOut;

        public Logger()
        {
            _stdErr = Console.Error;
            _stdOut = Console.Out;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);

            if (logLevel > LogLevel.Warning)
            {
                _stdErr.WriteLine(message);
            }
            else
            {
                _stdOut.WriteLine(message);
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new DisposableScope();
        }

        private class DisposableScope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}