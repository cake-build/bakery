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
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Core.Modules;
using Cake.Core.Text;
using Cake.NuGet;
using Cake.Scripting;
using Cake.Scripting.Abstractions;
using Cake.Scripting.Transport.Tcp.Server;
using Microsoft.Extensions.Logging;
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

            if (args.ContainsKey(Constants.CommandLine.Port) &&
                int.TryParse(args[Constants.CommandLine.Port], out int port))
            {
                var loggerFactory = new LoggerFactory()
                    .AddConsole(LogLevel.Trace);

                var registrar = new ContainerRegistrar();
                registrar.RegisterModule(new CoreModule());
                registrar.RegisterModule(new BakeryModule(loggerFactory));
                registrar.RegisterModule(new NuGetModule());

                // Build the container.
                using (var container = registrar.Build())
                {
                    var fileSystem = container.Resolve<IFileSystem>();
                    var log = container.Resolve<ICakeLog>();

                    // Rebuild the container.
                    registrar = new ContainerRegistrar();
                    registrar.RegisterModule(new ScriptingModule(fileSystem, log));
                    registrar.Builder.Update(container);

                    var environment = container.Resolve<ICakeEnvironment>();
                    var scriptGenerator = container.Resolve<IScriptGenerationService>();

                    environment.WorkingDirectory = System.IO.Directory.GetCurrentDirectory();

                    try
                    {
                        using (var server = new ScriptGenerationServer(scriptGenerator, port, loggerFactory))
                        {
                            var cancel = false;
                            server.Start();
                            server.OnDisconnected += (sender, e) => { cancel = true; };
                            Console.CancelKeyPress += (sender, e) => { cancel = true; };

                            while (!cancel)
                            {
                                Thread.Sleep(50);
                            }
                            server.Stop();
                        }
                    }
                    catch (Exception e)
                    {
                        loggerFactory.CreateLogger<Program>().LogCritical(0, e, "Unhandled Exception");
                        throw;
                    }
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
