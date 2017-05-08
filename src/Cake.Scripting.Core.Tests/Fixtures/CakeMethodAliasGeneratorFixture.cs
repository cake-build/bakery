// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cake.Scripting.Core.CodeGen.Generators;
using Cake.Scripting.Core.Reflection.Emitters;

namespace Cake.Scripting.Core.Tests.Fixtures
{
    public sealed class CakeMethodAliasGeneratorFixture : CakeAliasGeneratorFixture<CakeMethodAliasGenerator>
    {
        protected override string ResourcePath => "Cake.Scripting.Core.Tests.Data.Expected.Methods";

        protected override CakeMethodAliasGenerator CreateGenerator()
        {
            var typeEmitter = new TypeEmitter();
            var parameterEmitter = new ParameterEmitter(typeEmitter);
            return new CakeMethodAliasGenerator(typeEmitter, parameterEmitter);
        }
    }
}