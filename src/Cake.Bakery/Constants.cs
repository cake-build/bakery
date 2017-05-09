using System;

namespace Cake.Bakery
{
    internal static class Constants
    {
        public static readonly Version LatestBreakingChange = new Version(0, 16, 0);

        public static class CommandLine
        {
            public static readonly string Assembly = "assembly";
            public static readonly string File = "file";
            public static readonly string Verify = "verify";
        }
    }
}
