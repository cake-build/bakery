// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cake.ScriptServer.CodeGen.Generators;
using Cake.ScriptServer.Reflection.Emitters;

namespace Cake.ScriptServer.Tests.Fixtures
{
    public sealed class CakeMethodAliasGeneratorFixture : CakeAliasGeneratorFixture<CakeMethodAliasGenerator>
    {
        protected override string ResourcePath => "Cake.ScriptServer.Tests.Data.Expected.Methods";

        protected override CakeMethodAliasGenerator CreateGenerator()
        {
            var typeEmitter = new TypeEmitter();
            var parameterEmitter = new ParameterEmitter(typeEmitter);
            return new CakeMethodAliasGenerator(typeEmitter, parameterEmitter);
        }
    }
}