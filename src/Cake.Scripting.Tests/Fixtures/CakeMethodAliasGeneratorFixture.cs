// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cake.Scripting.CodeGen.Generators;
using Cake.Scripting.Reflection.Emitters;

namespace Cake.Scripting.Tests.Fixtures
{
    public sealed class CakeMethodAliasGeneratorFixture : CakeAliasGeneratorFixture<CakeMethodAliasGenerator>
    {
        protected override string ResourcePath => "Cake.Scripting.Tests.Data.Expected.Methods";

        protected override CakeMethodAliasGenerator CreateGenerator()
        {
            var typeEmitter = new TypeEmitter();
            var parameterEmitter = new ParameterEmitter(typeEmitter);
            return new CakeMethodAliasGenerator(typeEmitter, parameterEmitter);
        }
    }
}
