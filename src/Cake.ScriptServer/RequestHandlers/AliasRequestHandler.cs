using Cake.Core.IO;
using Cake.ScriptServer.CodeGen;
using Cake.ScriptServer.Core.Models;
using Cake.ScriptServer.Core.RequesHandlers;

namespace Cake.ScriptServer.RequestHandlers
{
    internal sealed class AliasRequestHandler : IAliasRequestHandler
    {
        private readonly CakeScriptGenerator _generator;

        public AliasRequestHandler(IFileSystem fileSystem)
        {
            _generator = new CakeScriptGenerator(fileSystem);
        }

        public ScriptResponse Handle(GenerateAliasRequest request)
        {
            return _generator.Generate(request.AssemblyPath, request.VerifyAssembly);
        }
    }
}
