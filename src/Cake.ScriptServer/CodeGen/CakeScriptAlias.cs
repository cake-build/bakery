using Cake.Core.Scripting;
using System.Collections.Generic;
using Cake.ScriptServer.Reflection;

namespace Cake.ScriptServer.CodeGen
{
    public sealed class CakeScriptAlias
    {
        public string Name { get; set; }

        public MethodSignature Method { get; set; }

        public ScriptAliasType Type { get; set; }

        public ISet<string> Namespaces { get; set; }
    }
}
