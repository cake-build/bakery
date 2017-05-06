using System.Collections.Generic;
using System.IO;
using Cake.Core.Scripting;
using Cake.ScriptServer.Reflection;
using Cake.ScriptServer.Reflection.Emitters;

namespace Cake.ScriptServer.CodeGen
{
    public sealed class CakeCodeGenerator
    {
        private readonly CakeMethodAliasGenerator _methodGenerator;

        public CakeCodeGenerator()
        {
            var typeEmitter = new TypeEmitter();
            var parameterEmitter = new ParameterEmitter(typeEmitter);

            _methodGenerator = new CakeMethodAliasGenerator(typeEmitter, parameterEmitter);
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
