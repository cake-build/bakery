using System.IO;
using Cake.Scripting.Abstractions;
using Cake.Scripting.Abstractions.Models;
using Cake.Scripting.Transport.Tcp.Client;
using System.Reflection;

namespace Cake.Bakery.IntegrationTests
{
    public sealed class IntegrationFixture
    {
        public IntegrationFixture()
        {
            var testDirectory = GetTestDirectory();
            ServerExecutablePath = Path.Combine(testDirectory, "Cake.Bakery.exe");
        }

        public string ServerExecutablePath { get; set; }

        public IScriptGenerationService CreateGenerationService()
        {
            return new ScriptGenerationClient(ServerExecutablePath);
        }

        public FileChange GetFileChange(string fileName)
        {
            var testDirectory = GetTestDirectory();

            return new FileChange
            {
                FileName = Path.Combine(testDirectory, "Data", fileName)
            };
        }

        private static string GetTestDirectory()
        {
            var location = typeof(IntegrationFixture).GetTypeInfo().Assembly.CodeBase;
            return Path.GetDirectoryName(location);
        }
    }
}
