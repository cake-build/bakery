using System;
using Cake.ScriptServer.CodeGen;
using Cake.ScriptServer.CodeGen.Generators;
using Cake.ScriptServer.Reflection.Emitters;

namespace Cake.ScriptServer.Tests.Fixtures
{
    public sealed class CakePropertyAliasGeneratorFixture : CakeAliasGeneratorFixture<CakePropertyAliasGenerator>
    {
        protected override string ResourcePath => "Cake.ScriptServer.Tests.Data.Expected.Properties";

        protected override CakePropertyAliasGenerator CreateGenerator()
        {
            return new CakePropertyAliasGenerator(new TypeEmitter());
        }
    }
}