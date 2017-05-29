using System;
using Xunit;

namespace Cake.Bakery.IntegrationTests
{
    public sealed class IntegrationTests : IClassFixture<IntegrationFixture>
    {
        private readonly IntegrationFixture _fixture;

        public IntegrationTests(IntegrationFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void ShouldGenerate()
        {
            // Given
            var target = _fixture.CreateGenerationService();
            var fileChange = _fixture.GetFileChange("helloworld.cake");

            // When
            var response = target.Generate(fileChange);

            // Then
            Assert.NotNull(response);
        }
    }
}
