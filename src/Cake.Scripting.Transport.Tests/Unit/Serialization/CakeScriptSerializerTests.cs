// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Cake.Scripting.Abstractions.Models;
using Cake.Scripting.Transport.Serialization;
using Cake.Scripting.Transport.Tests.Fixtures.Serialization;
using Xunit;

namespace Cake.Scripting.Transport.Tests.Unit.Serialization
{
    public sealed class CakeScriptSerializerTests
    {
        public sealed class TheSerializeMethod : IClassFixture<CakeScriptSerializerFixture>
        {
            private readonly CakeScriptSerializerFixture _fixture;

            public TheSerializeMethod(CakeScriptSerializerFixture fixture)
            {
                _fixture = fixture;
                _fixture.ResetStream();
            }

            [Fact]
            public void ShouldThrowIfWriterIsNull()
            {
                // Given, When
                var exception = Record.Exception(() => CakeScriptSerializer.Serialize(null, CakeScript.Empty)) as ArgumentNullException;

                // Then
                Assert.NotNull(exception);
                Assert.Equal("writer", exception.ParamName);
            }

            [Fact]
            public void ShouldThrowIfCakeScriptIsNull()
            {
                // Given, When
                var exception = Record.Exception(() => CakeScriptSerializer.Serialize(_fixture.Writer, null)) as ArgumentNullException;

                // Then
                Assert.NotNull(exception);
                Assert.Equal("script", exception.ParamName);
            }
        }

        public sealed class TheDeserializeMethod : IClassFixture<CakeScriptSerializerFixture>
        {
            private readonly CakeScriptSerializerFixture _fixture;

            public TheDeserializeMethod(CakeScriptSerializerFixture fixture)
            {
                _fixture = fixture;
                _fixture.ResetStream();
            }

            [Fact]
            public void ShouldThrowIfReaderIsNull()
            {
                // Given, When
                var exception = Record.Exception(() => CakeScriptSerializer.Deserialize(null)) as ArgumentNullException;

                // Then
                Assert.NotNull(exception);
                Assert.Equal("reader", exception.ParamName);
            }
        }

        public sealed class TheSerializeAndDeserializeMethods : IClassFixture<CakeScriptSerializerFixture>
        {
            private readonly CakeScriptSerializerFixture _fixture;

            public TheSerializeAndDeserializeMethods(CakeScriptSerializerFixture fixture)
            {
                _fixture = fixture;
                _fixture.ResetStream();
            }

            [Theory]
            [InlineData("UTF-8-demo.txt")]
            [InlineData("UTF-8-test.txt")]
            public void ShouldSerializeAndSerializeUtf8TestData(string resource)
            {
                // Given
                var expected = _fixture.CreateCakeScriptFromResource(resource, 0, 0);

                // When
                CakeScriptSerializer.Serialize(_fixture.Writer, expected);
                _fixture.ResetStreamPosition();
                var actual = CakeScriptSerializer.Deserialize(_fixture.Reader);

                // Then
                Assert.NotNull(actual);
                Assert.Equal(expected.Source, actual.Source);
                Assert.Equal(expected.References, actual.References);
                Assert.Equal(expected.Usings, actual.Usings);
            }
        }
    }
}
