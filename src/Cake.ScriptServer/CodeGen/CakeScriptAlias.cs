using Cake.Core.Scripting;
using System.Collections.Generic;
using Cake.ScriptServer.Reflection;
using Mono.Cecil;

namespace Cake.ScriptServer.CodeGen
{
    internal sealed class CakeScriptAlias
    {
        public string Name { get; set; }

        public MethodSignature Method { get; set; }

        public ScriptAliasType Type { get; set; }

        public ISet<string> Namespaces { get; set; }
    }
}
