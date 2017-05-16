using Cake.Scripting.CodeGen.Generators;
using Cake.Scripting.Reflection.Emitters;

namespace Cake.Scripting.Tests.Fixtures
{
    public sealed class CakePropertyAliasGeneratorFixture : CakeAliasGeneratorFixture<CakePropertyAliasGenerator>
    {
        protected override string ResourcePath => "Cake.Scripting.Tests.Data.Expected.Properties";

        protected override CakePropertyAliasGenerator CreateGenerator()
        {
            return new CakePropertyAliasGenerator(new TypeEmitter());
        }
    }
}