// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Cake.Scripting.Abstractions.Models;
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
        public void ShouldGenerateFromFile()
        {
            // Given
            var target = _fixture.CreateGenerationService();
            var fileChange = _fixture.GetFileChange("helloworld.cake");

            // When
            var response = target.Generate(fileChange);

            // Then
            // TODO: Better assertion
            Assert.NotNull(response);

            // Cleanup
            target.Dispose();
        }

        [Fact]
        public void ShouldGenerateFromBuffer()
        {
            // Given
            var target = _fixture.CreateGenerationService();
            var buffer = @"var target = Argument(""target"", ""Default"");

Task(""Default"")
  .Does(() =>
{
  Information(""Hello World!"");
});

RunTarget(target);";
            var fileChange = new FileChange
            {
                Buffer = buffer,
                FileName = "foobar.cake",
                FromDisk = false
            };

            // When
            var response = target.Generate(fileChange);

            // Then
            // TODO: Better assertion
            Assert.NotNull(response);

            // Cleanup
            target.Dispose();
        }

        [Fact]
        public void ShouldGenerateWitLineChanges()
        {
            // Given
            var target = _fixture.CreateGenerationService();
            var buffer = @"var target = Argument(""target"", ""Default"");

Task(""Default"")
  .Does(() =>
{
  Information(""Hello World!"");
});

RunTarget(target);";
            var fileChange = new FileChange
            {
                Buffer = buffer,
                FileName = "foobar.cake",
                FromDisk = false
            };
            target.Generate(fileChange);
            fileChange = new FileChange
            {
                LineChanges =
                {
                    new LineChange
                    {
                        StartLine = 0,
                        StartColumn = 33,
                        EndLine = 0,
                        EndColumn = 40,
                        NewText = "Foobar"
                    },
                    new LineChange
                    {
                        StartLine = 2,
                        StartColumn = 6,
                        EndLine = 2,
                        EndColumn = 13,
                        NewText = "Foobar"
                    },
                    new LineChange
                    {
                        StartLine = 5,
                        StartColumn = 2,
                        EndLine = 5,
                        EndColumn = 13,
                        NewText = "Verbose"
                    }
                },
                FileName = "foobar.cake",
                FromDisk = false
            };

            // When
            var response = target.Generate(fileChange);

            // Then
            // TODO: Better assertion
            Assert.NotNull(response);

            // Cleanup
            target.Dispose();
        }
    }
}
