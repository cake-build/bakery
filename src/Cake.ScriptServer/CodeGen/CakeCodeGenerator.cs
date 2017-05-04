using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cake.Core.Scripting;
using Cake.Core.Scripting.CodeGen;
using Cake.ScriptServer.Reflection;

namespace Cake.ScriptServer.CodeGen
{
    internal sealed class CakeCodeGenerator
    {
        public string Generate(IEnumerable<CakeScriptAlias> aliases)
        {
            var result = new List<string>();
            var writer = new StringWriter();

            var typeRenderer = new TypeSignatureRenderer();
            var methodRenderer = new MethodSignatureRenderer(typeRenderer);

            foreach (var alias in aliases)
            {
                if (alias.Type == ScriptAliasType.Method)
                {
                    CakeMethodAliasGenerator.Generate(typeRenderer, methodRenderer, writer, alias);
                }
            }

            writer.WriteLine();
            return writer.ToString();
        }
    }
}
