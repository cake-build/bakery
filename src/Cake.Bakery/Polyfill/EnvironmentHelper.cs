using System;

namespace Cake.Bakery.Polyfill
{
    internal static class EnvironmentHelper
    {
        public static string GetCommandLine()
        {
#if NETCORE
            return string.Join(" ", Environment.GetCommandLineArgs());
#else
            return Environment.CommandLine;
#endif
        }
    }
}
