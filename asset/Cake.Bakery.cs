using System;
using System.Diagnostics;
using System.IO;

namespace Cake.Bakery
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = string.Concat(
                "\"",
                Path.ChangeExtension(typeof(Program).Assembly.Location, ".dll"),
                "\"",
                " ",
                Environment.CommandLine
                );

            var info = new ProcessStartInfo("dotnet")
            {
                Arguments = path,
                UseShellExecute = false
            };

            var p = Process.Start(info);
            p.WaitForExit();
            Environment.Exit(p.ExitCode);
        }
    }
}
