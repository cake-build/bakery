using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Cake.Core.Scripting;
using Cake.Scripting.Reflection;

namespace Cake.Scripting.CodeGen
{
    public sealed class CakeScriptAlias
    {
        public string Name { get; set; }

        public MethodSignature Method { get; set; }

        public XElement Documentation { get; set; }

        public ScriptAliasType Type { get; set; }

        public ObsoleteAttribute Obsolete { get; set; }

        public bool Cached { get; set; }

        public ISet<string> Namespaces { get; set; }
    }
}
