// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cake.Core.IO;
using Cake.Scripting.Abstractions.Models;
using Cake.Scripting.IO;
using Cake.Scripting.Tests.Extensions;
using Cake.Scripting.Tests.Fixtures;
using Cake.Testing;
using Xunit;

namespace Cake.Scripting.Tests.Unit.IO
{
    public sealed class BufferedFileSystemTests
    {
        public sealed class TheUpdateBufferMethod : IClassFixture<BufferedFileSystemFixture>
        {
            private readonly BufferedFileSystemFixture _fixture;

            public TheUpdateBufferMethod(BufferedFileSystemFixture fixture)
            {
                _fixture = fixture;
            }

            [Fact]
            // Test taken from https://github.com/OmniSharp/omnisharp-roslyn/blob/dev/tests/OmniSharp.Roslyn.CSharp.Tests/BufferFacts.cs#L37
            public void ShouldInsertAndRemoveChanges()
            {
                // Given
                var target = _fixture.CreateBufferedFileSystem();
                var path = new FilePath("test.cake");
                target.UpdateFileBuffer(path, "class C {}");

                // When
                var change = new LineChange
                {
                    StartLine = 0,
                    StartColumn = 0,
                    EndLine = 0,
                    EndColumn = 0,
                    NewText = "farboo"
                };
                target.UpdateFileBuffer(path, new[] { change });

                // Then
                Assert.Equal("farbooclass C {}", target.GetFileContent(path));

                // When
                change = new LineChange
                {
                    StartLine = 0,
                    StartColumn = 0,
                    EndLine = 0,
                    EndColumn = 6,
                    NewText = string.Empty
                };
                target.UpdateFileBuffer(path, new[] { change });

                // Then
                Assert.Equal("class C {}", target.GetFileContent(path));

                // When
                change = new LineChange
                {
                    StartLine = 0,
                    StartColumn = 0,
                    EndLine = 0,
                    EndColumn = 5,
                    NewText = "interface"
                };
                target.UpdateFileBuffer(path, new[] { change });

                // Then
                Assert.Equal("interface C {}", target.GetFileContent(path));
            }

            [Fact]
            public void ShouldHandleMultipleChanges()
            {
                // Given
                var target = _fixture.CreateBufferedFileSystem();
                var path = new FilePath("test.cake");
                target.UpdateFileBuffer(path, "class C {}");

                // When
                var changes = new[]
                {
                    // class C {} -> interface C {}
                    new LineChange
                    {
                        StartLine = 0,
                        StartColumn = 0,
                        EndLine = 0,
                        EndColumn = 5,
                        NewText = "interface"
                    },
                    // interface C {} -> interface I {}
                    // note: this change is relative to the previous
                    // change having been applied
                    new LineChange
                    {
                        StartLine = 0,
                        StartColumn = 10,
                        EndLine = 0,
                        EndColumn = 11,
                        NewText = "I"
                    }
                };
                target.UpdateFileBuffer(path, changes);

                // Then
                Assert.Equal("interface I {}", target.GetFileContent(path));
            }

            [Theory]
            [InlineData("\n")]
            [InlineData("\r\n")]
            public void ShouldInsertNewLineAtBeginning(string lineEnding)
            {
                // Given
                var target = _fixture.CreateBufferedFileSystem();
                var path = new FilePath("test.cake");
                target.UpdateFileBuffer(path, "class C {}");

                // When
                var changes = new[]
                {
                    // class C {} -> \nclass C {}
                    new LineChange
                    {
                        StartLine = 0,
                        StartColumn = 0,
                        EndLine = 0,
                        EndColumn = 0,
                        NewText = lineEnding
                    }
                };
                target.UpdateFileBuffer(path, changes);

                // Then
                Assert.Equal("\nclass C {}", target.GetFileContent(path));
            }

            [Theory]
            [InlineData("\n")]
            [InlineData("\r\n")]
            public void ShouldInsertNewLineAtEnd(string lineEnding)
            {
                // Given
                var target = _fixture.CreateBufferedFileSystem();
                var path = new FilePath("test.cake");
                target.UpdateFileBuffer(path, "class C {}");

                // When
                var changes = new[]
                {
                    // class C {} -> \nclass C {}
                    new LineChange
                    {
                        StartLine = 0,
                        StartColumn = 10,
                        EndLine = 0,
                        EndColumn = 10,
                        NewText = lineEnding
                    }
                };
                target.UpdateFileBuffer(path, changes);

                // Then
                Assert.Equal("class C {}\n", target.GetFileContent(path));
            }

            [Theory]
            [InlineData("\n", 10)]
            [InlineData("\r\n", 10)]
            public void ShouldInsertMultipleNewLines(string lineEnding, int count)
            {
                // Given
                var target = _fixture.CreateBufferedFileSystem();
                var path = new FilePath("test.cake");
                target.UpdateFileBuffer(path, string.Empty);

                // When
                for (var i = 0; i < count; i++)
                {
                    var changes = new[]
                    {
                        new LineChange
                        {
                            StartLine = i,
                            StartColumn = 0,
                            EndLine = i,
                            EndColumn = 0,
                            NewText = lineEnding
                        }
                    };
                    target.UpdateFileBuffer(path, changes);
                }

                // Then
                Assert.Equal(string.Join(string.Empty, Enumerable.Repeat("\n", count)), target.GetFileContent(path));
            }
        }
    }
}