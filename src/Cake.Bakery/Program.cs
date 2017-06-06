// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
using Cake.Scripting.Abstractions.Models;
using Cake.Scripting.CodeGen;
using Cake.Scripting.IO;
using Cake.Scripting.Transport.Tcp.Server;
using Microsoft.Extensions.Logging;

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

            if (args.ContainsKey(Constants.CommandLine.Debug))
            {
                Console.WriteLine($"Attach debugger to process {Process.GetCurrentProcess().Id} to continue. ..");
                while (!Debugger.IsAttached)
                {
                    Thread.Sleep(100);
                }
            }

            if (args.ContainsKey(Constants.CommandLine.Port) &&
                int.TryParse(args[Constants.CommandLine.Port], out int port))
            {
                // Init dependencies
                var loggerFactory = new LoggerFactory()
                    .AddConsole(LogLevel.Trace);
                var log = new CakeLog(loggerFactory);
                var fileSystem = new BufferedFileSystem(new FileSystem(), log);
                var environment = new CakeEnvironment(new CakePlatform(), new CakeRuntime(), log);
                var configuration = new CakeConfiguration();
                var toolRepository = new ToolRepository(environment);
                var globber = new Globber(fileSystem, environment);
                var toolResolutionStrategy = new ToolResolutionStrategy(fileSystem, environment, globber, configuration);
                var toolLocator = new ToolLocator(environment, toolRepository, toolResolutionStrategy);
                var packageInstaller = new DefaultPackageInstaller(environment, fileSystem, globber, log); // <-- TODO: This should callback to client
                var processor = new ScriptProcessor(fileSystem, environment, log, toolLocator, new[] { packageInstaller });
                var scriptGenerator = new CakeScriptGenerator(
                    fileSystem: fileSystem,
                    environment: environment,
                    globber: globber,
                    configuration: configuration,
                    processor: processor,
                    log: log,
                    loadDirectiveProviders: null);

                environment.WorkingDirectory = System.IO.Directory.GetCurrentDirectory();

                try
                {
                    using (var server = new ScriptGenerationServer(scriptGenerator, port, loggerFactory))
                    {
                        var cancel = false;
                        Console.CancelKeyPress += (sender, e) =>
                        {
                            cancel = true;
                        };

                        while (!cancel)
                        {
                            Thread.Sleep(300);
                        }
                    }
                }
                catch (Exception e)
                {
                    loggerFactory.CreateLogger<Program>().LogCritical(0, e, "Unhandled Exception");
                    throw;
                }
            }
            else
            {
                throw new ArgumentException("Port not specified or invalid");
            }
            return 0;
        }
    }
}
