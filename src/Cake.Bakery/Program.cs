// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Cake.Bakery.Arguments;
using Cake.Bakery.Composition;
using Cake.Bakery.Polyfill;
using Cake.Core;
using Cake.Core.Configuration;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Core.Modules;
using Cake.Core.Text;
using Cake.NuGet;
using Cake.Scripting;
using Cake.Scripting.Abstractions;
using Cake.Scripting.CodeGen;
using Cake.Scripting.Transport.Tcp.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

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

            // Validate the port argument.
            if (!args.ContainsKey(Constants.CommandLine.Port) ||
                !int.TryParse(args[Constants.CommandLine.Port], out int port))
            {
                throw new ArgumentException("Port not specified or invalid.");
            }

            var loggerFactory = new LoggerFactory();

            var registrar = new ContainerRegistrar();
            registrar.RegisterModule(new CoreModule());
            registrar.RegisterModule(new BakeryModule(loggerFactory));

            // Build the container.
            using (var container = registrar.Build())
            {
                var fileSystem = container.Resolve<IFileSystem>();
                var log = container.Resolve<ICakeLog>();
                var configurationProvider = container.Resolve<CakeConfigurationProvider>();
                var workingDirectory = new DirectoryPath(System.IO.Directory.GetCurrentDirectory());

                var configuration = configurationProvider.CreateConfiguration(workingDirectory, args);

                // Rebuild the container for NuGet and Buffered File System.
                registrar = new ContainerRegistrar();
                registrar.RegisterInstance(configuration);
                registrar.RegisterModule(new NuGetModule(configuration));
                registrar.RegisterModule(new ScriptingModule(fileSystem, log));
                registrar.Builder.Update(container);

                var environment = container.Resolve<ICakeEnvironment>();
                var aliasFinder = container.Resolve<IScriptAliasFinder>();
                var processor = container.Resolve<Core.Scripting.IScriptProcessor>();

                // Rebuild the container for Cached Alias Finder.
                registrar = new ContainerRegistrar();
                registrar.RegisterModule(new CacheModule(aliasFinder, processor, environment));
                registrar.Builder.Update(container);

                // Get Script generator.
                var scriptGenerator = container.Resolve<IScriptGenerationService>();

                environment.WorkingDirectory = workingDirectory;

                try
                {
                    using (var server = new ScriptGenerationServer(scriptGenerator, port, loggerFactory))
                    {
                        server.Start();

                        var cancel = new ManualResetEvent(false);

                        server.OnDisconnected += (sender, e) => { cancel.Set(); };
                        Console.CancelKeyPress += (sender, e) =>
                        {
                            cancel.Set();
                            e.Cancel = true;
                        };

                        cancel.WaitOne();
                        server.Stop();
                    }
                }
                catch (Exception e)
                {
                    loggerFactory.CreateLogger<Program>().LogCritical(0, e, "Unhandled Exception");
                    throw;
                }
            }

            return 0;
        }
    }
}
