using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cake.Core.IO;
using Cake.Scripting.CodeGen;
using Xunit;

namespace Cake.Scripting.Tests.Unit.CodeGen
{
    public sealed class CakeScriptAliasFinderTests
    {
#if NETCORE
        [Fact(Skip = "Assembly resolver not implemented for .NET Core")]
#else
        [Fact]
#endif
        public void Should_Include_Namespaces_For_Alias()
        {
            // Given
            var fileSystem = new FileSystem();
            var finder = new CakeScriptAliasFinder(fileSystem);

            // When
            var aliases = finder.FindAliases(new List<FilePath>
            {
                new FilePath(typeof(CakeScriptAliasFinderTests).GetTypeInfo().Assembly.Location)
            });

            // Then
            var alias = aliases.Single(a => a.Name == "NonGeneric_ExtensionMethodWithNoParameters");

            Assert.NotNull(alias);
            Assert.Equal(4, alias.Namespaces.Count);
            Assert.Contains("Cake.Scripting.Tests.Data", alias.Namespaces);
            Assert.Contains("Foo.Bar.Assembly", alias.Namespaces);
            Assert.Contains("Foo.Bar.Type", alias.Namespaces);
            Assert.Contains("Foo.Bar.Method", alias.Namespaces);
        }
    }
}
