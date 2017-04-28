using System;
using System.Linq;
using Cake.Core.IO;
using Cake.Core.Reflection;
using Cake.Core.Scripting;
using Cake.ScriptServer.Core.Models;
using Cake.ScriptServer.Documentation;
using Cake.ScriptServer.Extensions;

namespace Cake.ScriptServer.CodeGen
{
    internal class CakeAliasGenerator
    {
        private readonly IScriptAliasFinder _aliasFinder;
        private readonly IAssemblyLoader _assemblyLoader;
        private readonly IFileSystem _fileSystem;
        private readonly IDocumentationProvider _documentationProvider;
        private readonly CodeGenerator _codeGenerator;

        public CakeAliasGenerator(
            IScriptAliasFinder aliasFinder,
            IAssemblyLoader assemblyLoader,
            IFileSystem fileSystem,
            IDocumentationProvider documentationProvider)
        {
            _aliasFinder = aliasFinder ?? throw new ArgumentNullException(nameof(aliasFinder));
            _assemblyLoader = assemblyLoader ?? throw new ArgumentNullException(nameof(assemblyLoader));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _documentationProvider = documentationProvider ?? throw new ArgumentNullException(nameof(documentationProvider));
            _codeGenerator = new CodeGenerator(_documentationProvider);
        }

        public ScriptModel Generate(FilePath assemblyFilePath, bool verify)
        {
            if (assemblyFilePath == null)
            {
                throw new ArgumentNullException(nameof(assemblyFilePath));
            }

            var scriptModel = new ScriptModel();

            if (!_fileSystem.Exist(assemblyFilePath))
            {
                return scriptModel;
            }

            _documentationProvider.SetAssembly(assemblyFilePath);

            var assembly = _assemblyLoader.Load(assemblyFilePath, verify);
            var aliases = _aliasFinder.FindAliases(new[] {assembly});

            scriptModel.Source = _codeGenerator.Generate(aliases);
            scriptModel.Usings.AddRange(aliases.SelectMany(a => a.Namespaces));
            scriptModel.References.Add(assemblyFilePath.FullPath);

            return scriptModel;
        }
    }
}
