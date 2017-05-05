using Cake.Core.IO;
using Cake.ScriptServer.Core.Models;
using System;
using System.Linq;
using Cake.ScriptServer.Extensions;

namespace Cake.ScriptServer.CodeGen
{
    internal sealed class CakeScriptGenerator
    {
        private readonly IFileSystem _fileSystem;
        private readonly CakeCodeGenerator _codeGenerator;

        public CakeScriptGenerator(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _codeGenerator = new CakeCodeGenerator();
        }

        public ScriptResponse Generate(FilePath assembly, bool verify)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }
            if (!_fileSystem.Exist(assembly))
            {
                return ScriptResponse.Empty;
            }

            // Find aliases.
            var aliases = CakeScriptAliasFinder.FindAliases(new[] { assembly });

            // Create the response.
            var response = new ScriptResponse();
            response.Source = _codeGenerator.Generate(aliases);
            response.Usings.AddRange(aliases.SelectMany(a => a.Namespaces));
            response.References.Add(assembly.FullPath);

            // Return the response.
            return response;
        }
    }
}
