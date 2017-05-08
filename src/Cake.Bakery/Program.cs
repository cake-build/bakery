using System;
using System.Linq;
using Cake.Bakery.Arguments;
using Cake.Bakery.Polyfill;
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
            var fileSystem = new FileSystem();
            var scriptGenerator = new CakeScriptAliasGenerator(fileSystem);

            if (args.ContainsKey(Constants.CommandLine.Assembly))
            {
                var assemblyPath = args[Constants.CommandLine.Assembly];
                var verifyAssembly = args.ContainsKey(Constants.CommandLine.Verify);

                var cakeScript = scriptGenerator.Generate(assemblyPath, verifyAssembly);

                Console.WriteLine($"Script: {cakeScript.Source}");
                Console.WriteLine($"Usings: {string.Join(";", cakeScript.Usings)}");
                Console.WriteLine($"References: {string.Join(";", cakeScript.References)}");
            }
            else if (args.ContainsKey(Constants.CommandLine.File))
            {

            }
            return 0;
        }
    }
}