using System;
using System.Linq;
using Cake.Bakery.Arguments;
using Cake.Bakery.Configuration;
using Cake.Bakery.Diagnostics;
using Cake.Bakery.Packaging;
using Cake.Bakery.Polyfill;
using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Packaging;
using Cake.Core.Scripting;
using Cake.Core.Text;
using Cake.Core.Tooling;
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
                var environment = new CakeEnvironment(new CakePlatform(), new CakeRuntime(), log);
                var configuration = new CakeConfiguration();
                var toolRepository = new ToolRepository(environment);
                var globber = new Globber(fileSystem, environment);
                var toolResolutionStrategy = new ToolResolutionStrategy(fileSystem, environment, globber, configuration);
                var toolLocator = new ToolLocator(environment, toolRepository, toolResolutionStrategy);
                var packageInstaller = new DefaultPackageInstaller(environment, fileSystem, globber, log); // <-- TODO: This should callback to client
                var processor = new ScriptProcessor(fileSystem, environment, log, toolLocator, new []{ packageInstaller });
                var scriptGenerator = new CakeScriptGenerator(
                    fileSystem: fileSystem,
                    environment: environment,
                    globber: globber,
                    configuration: configuration,
                    processor: processor,
                    log: log,
                    loadDirectiveProviders: null);

                environment.WorkingDirectory = System.IO.Directory.GetCurrentDirectory();

                var cakeScript = scriptGenerator.Generate(scriptPath);
            }
            return 0;
        }
    }
}