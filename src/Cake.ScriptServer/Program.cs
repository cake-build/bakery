using System;
using System.Linq;
using Cake.Core.IO;
using Cake.Core.Scripting;
using Cake.Core.Text;
using Cake.ScriptServer.Arguments;
using Cake.ScriptServer.CodeGen;
using Cake.ScriptServer.Diagnostics;
using Cake.ScriptServer.Documentation;
using Cake.ScriptServer.Polyfill;
using Cake.ScriptServer.Reflection;

namespace Cake.ScriptServer
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
            var console = new IO.Console();
            var log = new ConsoleLogger(console);

            try
            {
                if (args.ContainsKey(Constants.CommandLine.Assembly))
                {
                    var verify = args.ContainsKey(Constants.CommandLine.Verify);
                    var aliasFinder = new ScriptAliasFinder(log);
                    var fileSystem = new FileSystem();
                    var assemblyVerifier = new AssemblyVerifier(log, !verify);
                    var assemblyLoader = new AssemblyLoader(fileSystem, assemblyVerifier);
                    var documentationProvider = new DocumentationProvider(fileSystem);

                    var aliasGenerator = new CakeAliasGenerator(
                        aliasFinder, assemblyLoader, fileSystem, documentationProvider);

                    var scriptModel = aliasGenerator.Generate(args[Constants.CommandLine.Assembly], verify);
                }
                else if (args.ContainsKey(Constants.CommandLine.File))
                {

                }
            }
            catch (Exception e)
            {
                console.StdErr.WriteLine(e.ToString());
                throw;
            }

            return 0;
        }
    }
}