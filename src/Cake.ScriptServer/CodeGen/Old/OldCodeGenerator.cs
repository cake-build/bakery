using System;
using System.Collections.Generic;
using Cake.Core.Scripting;
using Cake.Core.Scripting.CodeGen;
using Cake.ScriptServer.Documentation;

namespace Cake.ScriptServer.CodeGen.Old
{
    internal class OldCodeGenerator
    {
        private readonly IDocumentationProvider _documentationProvider;

        public OldCodeGenerator(IDocumentationProvider documentationProvider)
        {
            _documentationProvider = documentationProvider ?? throw new ArgumentNullException(nameof(documentationProvider));
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
