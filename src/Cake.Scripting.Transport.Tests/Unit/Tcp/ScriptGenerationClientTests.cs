using Cake.Scripting.Abstractions.Models;
using Cake.Scripting.Transport.Tests.Fixtures.Tcp;
using Xunit;

namespace Cake.Scripting.Transport.Tests.Unit.Tcp
{
    public sealed class ScriptGenerationClientTests
    {
        public sealed class TheGenerateMethod : IClassFixture<TcpCommunicationFixture>
        {
            private readonly TcpCommunicationFixture _fixture;

            public TheGenerateMethod(TcpCommunicationFixture fixture)
            {
                _fixture = fixture;
            }

            [Fact]
            public void ShouldCallTheServer()
            {
                // Given
                var expected = new CakeScript { Source = "Test" };
                _fixture.ServerCallback = change => expected;

                // When
                var actual = _fixture.Client.Generate(new FileChange());

                // Then
                Assert.Equal(expected.Source, actual.Source);
            }
        }
    }
}
