using System;
using System.Linq;
using Cake.Bakery.Arguments;
using Cake.Bakery.Configuration;
using Cake.Bakery.Diagnostics;
using Cake.Bakery.Polyfill;
using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Text;
using Cake.Scripting.Core.CodeGen;

namespace Cake.Bakery
{
    public class Program
    {
        public static int Main()
        {
            // Parse arguments.
            var args = ArgumentParser.Parse(
                QuoteAwareStringSplitter
                .Split(EnvironmentHelper.GetCommandLine())
                .Skip(1));

            // Init dependencies
            var log = new ConsoleLogger();
            var fileSystem = new FileSystem();

            if (args.ContainsKey(Constants.CommandLine.Assembly))
            {
                var assemblyPath = args[Constants.CommandLine.Assembly];
                var verifyAssembly = args.ContainsKey(Constants.CommandLine.Verify);
                var scriptGenerator = new CakeScriptAliasGenerator(fileSystem);

                var cakeScript = scriptGenerator.Generate(assemblyPath, verifyAssembly);

                Console.WriteLine($"Script: {cakeScript.Source}");
                Console.WriteLine($"Usings: {string.Join(";", cakeScript.Usings)}");
                Console.WriteLine($"References: {string.Join(";", cakeScript.References)}");
            }
            else if (args.ContainsKey(Constants.CommandLine.File))
            {
                var scriptPath = args[Constants.CommandLine.File];
                var rootDir = args[Constants.CommandLine.Root];
                var environment = new CakeEnvironment(new CakePlatform(), new CakeRuntime(), log);
                var configuration = new CakeConfiguration();
                var scriptGenerator = new CakeScriptGenerator(fileSystem, environment, configuration, log);
                environment.WorkingDirectory = rootDir;

                var cakeScript = scriptGenerator.Generate(scriptPath);
            }
            return 0;
        }
    }
}