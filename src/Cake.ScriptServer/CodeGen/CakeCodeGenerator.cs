using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cake.Core.Scripting;
using Cake.Core.Scripting.CodeGen;

namespace Cake.ScriptServer.CodeGen
{
    internal sealed class CakeCodeGenerator
    {
        public string Generate(IEnumerable<CakeScriptAlias> aliases)
        {
            var result = new List<string>();

            foreach (var alias in aliases)
            {
                // TODO: Create documentation.
                var documentation = string.Empty;

                var code = alias.Type == ScriptAliasType.Method
                    ? CakeMethodAliasGenerator.Generate(alias)
                    : CakePropertyAliasGenerator.Generate(alias);

                result.Add(documentation + code);
            }
            return string.Join("\r\n", result);
        }
    }
}
