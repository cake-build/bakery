// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Cake.Bakery.Arguments;
using Xunit;

namespace Cake.Bakery.Tests.Unit.Arguments
{
    public sealed class ArgumentParserTests
    {
        public sealed class TheParseMethod
        {
            [Theory]
            [InlineData("--file=/path/to/foo", "--verify")]
            [InlineData("--File=/path/to/foo", "--Verify")]
            [InlineData("\"--file=/path/to/foo\"", "\"--verify\"")]
            [InlineData("-file=/path/to/foo", "-verify")]
            [InlineData("-File=/path/to/foo", "-Verify")]
            [InlineData("\"-file=/path/to/foo\"", "\"-verify\"")]
            [InlineData("-file=\"/path/to/foo\"", "-verify")]
            [InlineData("-File=\"/path/to/foo\"", "-Verify")]
            public void Should_Return_Correct_Arguments(string arg1, string arg2)
            {
                // Given
                var expected = new Dictionary<string, string>
                {
                    { "file", "/path/to/foo" },
                    { "verify", string.Empty }
                };

                // When
                var actual = ArgumentParser.Parse(new[] { arg1, arg2 });

                // Then
                Assert.Equal(expected, actual);
            }
        }
    }
}
