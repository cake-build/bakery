using System;
using System.Linq;
using Cake.Core.IO;
using Cake.Core.Reflection;
using Cake.Core.Scripting;
using Cake.ScriptServer.Core.Models;
using Cake.ScriptServer.Documentation;
using Cake.ScriptServer.Extensions;

namespace Cake.ScriptServer.CodeGen.Old
{
    internal class OldCakeAliasGenerator
    {
        private readonly IScriptAliasFinder _aliasFinder;
        private readonly IAssemblyLoader _assemblyLoader;
        private readonly IFileSystem _fileSystem;
        private readonly IDocumentationProvider _documentationProvider;
        private readonly OldCodeGenerator _oldCodeGenerator;

        public OldCakeAliasGenerator(
            IScriptAliasFinder aliasFinder,
            IAssemblyLoader assemblyLoader,
            IFileSystem fileSystem,
            IDocumentationProvider documentationProvider)
        {
            _aliasFinder = aliasFinder ?? throw new ArgumentNullException(nameof(aliasFinder));
            _assemblyLoader = assemblyLoader ?? throw new ArgumentNullException(nameof(assemblyLoader));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _documentationProvider = documentationProvider ?? throw new ArgumentNullException(nameof(documentationProvider));
            _oldCodeGenerator = new OldCodeGenerator(_documentationProvider);
        }

        public ScriptResponse Generate(FilePath assemblyFilePath, bool verify)
        {
            if (assemblyFilePath == null)
            {
                throw new ArgumentNullException(nameof(assemblyFilePath));
            }

            var script = new ScriptResponse();

            if (!_fileSystem.Exist(assemblyFilePath))
            {
                return script;
            }

            _documentationProvider.SetAssembly(assemblyFilePath);

            var assembly = _assemblyLoader.Load(assemblyFilePath, verify);
            var aliases = _aliasFinder.FindAliases(new[] {assembly});

            script.Source = _oldCodeGenerator.Generate(aliases);
            script.Usings.AddRange(aliases.SelectMany(a => a.Namespaces));
            script.References.Add(assemblyFilePath.FullPath);

            return script;
        }
    }
}
