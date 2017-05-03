using Cake.Core.Scripting;
using System.Collections.Generic;
using Mono.Cecil;

namespace Cake.ScriptServer.CodeGen
{
    internal sealed class CakeScriptAlias
    {
        public string Name { get; set; }

        public MethodDefinition Method { get; set; }

        public ScriptAliasType Type { get; set; }

        public ISet<string> Namespaces { get; set; }
    }
}
