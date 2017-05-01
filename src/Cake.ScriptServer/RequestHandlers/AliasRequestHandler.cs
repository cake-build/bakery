using System;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Core.Scripting;
using Cake.ScriptServer.CodeGen;
using Cake.ScriptServer.Core.Models;
using Cake.ScriptServer.Core.RequesHandlers;
using Cake.ScriptServer.Documentation;
using Cake.ScriptServer.Reflection;

namespace Cake.ScriptServer.RequestHandlers
{
    internal sealed class AliasRequestHandler : IAliasRequestHandler
    {
        private readonly ICakeLog _log;

        public AliasRequestHandler(ICakeLog log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public ScriptResponse Handle(GenerateAliasRequest request)
        {
            var verify = request.VerifyAssembly;
            var aliasFinder = new ScriptAliasFinder(_log);
            var fileSystem = new FileSystem();
            var assemblyVerifier = new AssemblyVerifier(_log, !verify);
            var assemblyLoader = new AssemblyLoader(fileSystem, assemblyVerifier);
            var documentationProvider = new DocumentationProvider(fileSystem);

            var aliasGenerator = new CakeAliasGenerator(
                aliasFinder, assemblyLoader, fileSystem, documentationProvider);

            return aliasGenerator.Generate(request.AssemblyPath, verify);
        }
    }
}
