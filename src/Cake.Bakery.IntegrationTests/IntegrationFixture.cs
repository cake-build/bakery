using System.Diagnostics;
using System.IO;
using Cake.Scripting.Abstractions;
using Cake.Scripting.Abstractions.Models;
using Cake.Scripting.Transport.Tcp.Client;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Cake.Bakery.IntegrationTests
{
    public sealed class IntegrationFixture
    {
        static IntegrationFixture()
        {
            // TODO: Just a silly step to bootstrap integration tests
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = ".\\build.ps1 -Script helloworld.cake",
                WorkingDirectory = GetWorkingDirectory()
            };
            Process.Start(startInfo).WaitForExit();
        }

        public IntegrationFixture()
        {
            var testDirectory = GetTestDirectory();
            ServerExecutablePath = Path.Combine(testDirectory, "Cake.Bakery.exe");
        }

        public string ServerExecutablePath { get; set; }

        public IScriptGenerationService CreateGenerationService()
        {
            var loggerFactory = new LoggerFactory()
                .AddDebug(LogLevel.Trace);
            return new ScriptGenerationClient(ServerExecutablePath, GetWorkingDirectory(), loggerFactory);
        }

        public FileChange GetFileChange(string fileName)
        {
            var testDirectory = GetTestDirectory();

            return new FileChange
            {
                FileName = Path.Combine(testDirectory, "Data", fileName),
                FromDisk = true
            };
        }

        private static string GetWorkingDirectory()
        {
            return Path.Combine(GetTestDirectory(), "Data");
        }

        private static string GetTestDirectory()
        {
            var location = typeof(IntegrationFixture).GetTypeInfo().Assembly.CodeBase;
            return Path.GetDirectoryName(location).Replace("file:\\", string.Empty);
        }
    }
}
