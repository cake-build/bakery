// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Cake.Core.Scripting;
using Cake.Scripting.Reflection.Emitters;

namespace Cake.Scripting.CodeGen.Generators
{
    public class CakeAliasGenerator : ICakeAliasGenerator
    {
        private readonly CakeMethodAliasGenerator _methodGenerator;
        private readonly CakePropertyAliasGenerator _propertyGenerator;

        public CakeAliasGenerator()
        {
            var typeEmitter = new TypeEmitter();
            var parameterEmitter = new ParameterEmitter(typeEmitter);

            _methodGenerator = new CakeMethodAliasGenerator(typeEmitter, parameterEmitter);
            _propertyGenerator = new CakePropertyAliasGenerator(typeEmitter);
        }

        public void Generate(TextWriter writer, CakeScriptAlias alias)
        {
            if (alias.Type == ScriptAliasType.Method)
            {
                _methodGenerator.Generate(writer, alias);
            }
            else
            {
                _propertyGenerator.Generate(writer, alias);
            }
        }
    }
}
