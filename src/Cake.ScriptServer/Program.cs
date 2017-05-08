using System.Linq;
using Cake.Core.IO;
using Cake.Core.Text;
using Cake.ScriptServer.Arguments;
using Cake.ScriptServer.Core;
using Cake.ScriptServer.Core.Models;
using Cake.ScriptServer.Polyfill;
using Cake.ScriptServer.RequestHandlers;

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
            var fileSystem = new FileSystem();

            if (args.ContainsKey(Constants.CommandLine.Assembly))
            {
                var request = new GenerateAliasRequest
                {
                    AssemblyPath = args[Constants.CommandLine.Assembly],
                    VerifyAssembly = args.ContainsKey(Constants.CommandLine.Verify)
                };

                var handler = new AliasRequestHandler(fileSystem);
                var serializer = new DataContractSerializer();
                var responseWriter = new ResponseWriter(console.StdOut);
                var response = handler.Handle(request);

                responseWriter.WriteResponse(serializer.Serialize(response));
            }
            else if (args.ContainsKey(Constants.CommandLine.File))
            {

            }
            return 0;
        }
    }
}