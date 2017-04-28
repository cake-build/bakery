using System;
using System.Collections.Generic;
using Cake.Core.Scripting;
using Cake.Core.Scripting.CodeGen;
using Cake.ScriptServer.Documentation;

namespace Cake.ScriptServer.CodeGen
{
    internal class CodeGenerator
    {
        private readonly IDocumentationProvider _documentationProvider;

        public CodeGenerator(IDocumentationProvider documentationProvider)
        {
            _documentationProvider = documentationProvider ?? throw new ArgumentNullException(nameof(documentationProvider));
        }

        public string Generate(Script script)
        {
            var usingDirectives = string.Join("\r\n", script.UsingAliasDirectives);
            var aliases = Generate(script.Aliases);
            var code = string.Join("\r\n", script.Lines);
            return string.Join("\r\n", usingDirectives, aliases, code);
        }

        public string Generate(IEnumerable<ScriptAlias> aliases)
        {
            var result = new List<string>();

            foreach (var alias in aliases)
            {
                var documentation = _documentationProvider.GetDocumentation(alias);

                var code = alias.Type == ScriptAliasType.Method
                    ? MethodAliasGenerator.Generate(alias.Method)
                    : PropertyAliasGenerator.Generate(alias.Method);

                result.Add(documentation + code);
            }
            return string.Join("\r\n", result);
        }
    }
}
