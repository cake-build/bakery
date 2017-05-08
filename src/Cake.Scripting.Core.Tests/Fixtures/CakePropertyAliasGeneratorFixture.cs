using Cake.Scripting.Core.CodeGen.Generators;
using Cake.Scripting.Core.Reflection.Emitters;

namespace Cake.Scripting.Core.Tests.Fixtures
{
    public sealed class CakePropertyAliasGeneratorFixture : CakeAliasGeneratorFixture<CakePropertyAliasGenerator>
    {
        protected override string ResourcePath => "Cake.Scripting.Core.Tests.Data.Expected.Properties";

        protected override CakePropertyAliasGenerator CreateGenerator()
        {
            return new CakePropertyAliasGenerator(new TypeEmitter());
        }
    }
}