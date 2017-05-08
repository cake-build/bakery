using Cake.Scripting.Core.Tests.Fixtures;
using Xunit;

namespace Cake.Scripting.Core.Tests.Unit.CodeGen
{
    public sealed class CakePropertyAliasGeneratorTests
    {
        public sealed class TheGenerateMethod : IClassFixture<CakePropertyAliasGeneratorFixture>
        {
            private readonly CakePropertyAliasGeneratorFixture _fixture;

            public TheGenerateMethod(CakePropertyAliasGeneratorFixture fixture)
            {
                _fixture = fixture;
            }

            [Theory]
            [InlineData("NonCached_Value_Type")]
            public void Should_Return_Correct_Generated_Code_For_Non_Cached_Properties(string name)
            {
                // Given
                var expected = _fixture.GetExpectedCode(name);

                // When
                var result = _fixture.Generate(name);

                // Then
                Assert.Equal(expected, result);
            }

            [Theory]
            [InlineData("Cached_Reference_Type")]
            [InlineData("Cached_Value_Type")]
            public void Should_Return_Correct_Generated_Code_For_Cached_Properties(string name)
            {
                // Given
                var expected = _fixture.GetExpectedCode(name);

                // When
                var result = _fixture.Generate(name);

                // Then
                Assert.Equal(expected, result);
            }

            [Theory]
            [InlineData("NonCached_Obsolete_ImplicitWarning_NoMessage")]
            [InlineData("NonCached_Obsolete_ImplicitWarning_WithMessage")]
            [InlineData("NonCached_Obsolete_ExplicitWarning_WithMessage")]
            [InlineData("NonCached_Obsolete_ExplicitError_WithMessage")]
            public void Should_Return_Correct_Generated_Code_For_Non_Cached_Obsolete_Properties(string name)
            {
                // Given
                var expected = _fixture.GetExpectedCode(name);

                // When
                var result = _fixture.Generate(name);

                // Then
                Assert.Equal(expected, result);
            }

            [Theory]
            [InlineData("Cached_Obsolete_ImplicitWarning_NoMessage")]
            [InlineData("Cached_Obsolete_ImplicitWarning_WithMessage")]
            [InlineData("Cached_Obsolete_ExplicitWarning_WithMessage")]
            [InlineData("Cached_Obsolete_ExplicitError_WithMessage")]
            public void Should_Return_Correct_Generated_Code_For_Cached_Obsolete_Properties(string name)
            {
                // Given
                var expected = _fixture.GetExpectedCode(name);

                // When
                var result = _fixture.Generate(name);

                // Then
                Assert.Equal(expected, result);
            }
        }
    }
}
