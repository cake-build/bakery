using Microsoft.Extensions.Logging;

namespace Cake.Bakery.Diagnostics
{
    public class LoggerProvider : ILoggerProvider
    {
        private readonly ILogger _logger;

        public LoggerProvider()
        {
            _logger = new Logger();
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _logger;
        }

        public void Dispose()
        {
        }
    }
}