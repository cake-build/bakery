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
    public sealed class CakeCodeGenerator
    {
        private readonly CakeMethodAliasGenerator _methodGenerator;

        public CakeCodeGenerator()
        {
            var typeWriter = new TypeSignatureWriter();

            _methodGenerator = new CakeMethodAliasGenerator(typeWriter);
        }

        public string Generate(IEnumerable<CakeScriptAlias> aliases)
        {
            var writer = new StringWriter();

            foreach (var alias in aliases)
            {
                if (alias.Type == ScriptAliasType.Method)
                {
                    _methodGenerator.Generate(writer, alias);
                }

                writer.WriteLine();
                writer.WriteLine();
            }

            return writer.ToString();
        }
    }
}
