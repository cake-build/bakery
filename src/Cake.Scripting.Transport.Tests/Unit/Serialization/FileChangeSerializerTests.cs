// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Cake.Scripting.Abstractions.Models;
using Cake.Scripting.Transport.Serialization;
using Cake.Scripting.Transport.Tests.Fixtures.Serialization;
using Xunit;

namespace Cake.Scripting.Transport.Tests.Unit.Serialization
{
    public sealed class FileChangeSerializerTests
    {
        public sealed class TheSerializeMethod : IClassFixture<FileChangeSerializerFixture>
        {
            private readonly FileChangeSerializerFixture _fixture;

            public TheSerializeMethod(FileChangeSerializerFixture fixture)
            {
                _fixture = fixture;
                _fixture.ResetStream();
            }

            [Fact]
            public void ShouldThrowIfWriterIsNull()
            {
                // Given, When
                var exception = Record.Exception(() => FileChangeSerializer.Serialize(null, FileChange.Empty, Constants.Protocol.Latest)) as ArgumentNullException;

                // Then
                Assert.NotNull(exception);
                Assert.Equal("writer", exception.ParamName);
            }

            [Fact]
            public void ShouldThrowIfCakeScriptIsNull()
            {
                // Given, When
                var exception = Record.Exception(() => FileChangeSerializer.Serialize(_fixture.Writer, null, Constants.Protocol.Latest)) as ArgumentNullException;

                // Then
                Assert.NotNull(exception);
                Assert.Equal("fileChange", exception.ParamName);
            }
        }

        public sealed class TheDeserializeMethod : IClassFixture<FileChangeSerializerFixture>
        {
            private readonly FileChangeSerializerFixture _fixture;

            public TheDeserializeMethod(FileChangeSerializerFixture fixture)
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

        public sealed class TheSerializeAndDeserializeMethods : IClassFixture<FileChangeSerializerFixture>
        {
            private readonly FileChangeSerializerFixture _fixture;

            public TheSerializeAndDeserializeMethods(FileChangeSerializerFixture fixture)
            {
                _fixture = fixture;
                _fixture.ResetStream();
            }

            [Fact]
            public void ShouldSerializeAndSerializeFileChange()
            {
                // Given
                var expected = new FileChange
                {
                    Buffer = "buffer",
                    FileName = "fileName",
                    FromDisk = true
                };
                expected.LineChanges.Add(new LineChange
                {
                    StartColumn = 1,
                    EndColumn = 2,
                    StartLine = 3,
                    EndLine = 4,
                    NewText = "5"
                });

                // When
                FileChangeSerializer.Serialize(_fixture.Writer, expected, Constants.Protocol.Latest);
                _fixture.ResetStreamPosition();
                var actual = FileChangeSerializer.Deserialize(_fixture.Reader, out var version);

                // Then
                Assert.NotNull(actual);
                Assert.Equal(Constants.Protocol.V2, version);
                Assert.Equal(expected.Buffer, actual.Buffer);
                Assert.Equal(expected.FileName, actual.FileName);
                Assert.Equal(expected.FromDisk, actual.FromDisk);
                Assert.Equal(expected.LineChanges, actual.LineChanges);
            }

            [Fact]
            public void ShouldSerializeNullLineChangeAsEmpty()
            {
                // Given
                var expected = new FileChange();
                expected.LineChanges.Add(new LineChange
                {
                    StartColumn = 1,
                    EndColumn = 2,
                    StartLine = 3,
                    EndLine = 4,
                    NewText = "5"
                });
                expected.LineChanges.Add(null);
                expected.LineChanges.Add(new LineChange
                {
                    StartColumn = 6,
                    EndColumn = 7,
                    StartLine = 8,
                    EndLine = 9,
                    NewText = "10"
                });

                // When
                FileChangeSerializer.Serialize(_fixture.Writer, expected, Constants.Protocol.Latest);
                _fixture.ResetStreamPosition();
                var actual = FileChangeSerializer.Deserialize(_fixture.Reader, out var version);

                // Then
                Assert.NotNull(actual);
                Assert.Equal(Constants.Protocol.V2, version);
                Assert.Equal(expected.LineChanges.ElementAt(0), actual.LineChanges.ElementAt(0));
                Assert.Equal(new LineChange { NewText = string.Empty }, actual.LineChanges.ElementAt(1));
                Assert.Equal(expected.LineChanges.ElementAt(2), actual.LineChanges.ElementAt(2));
            }

            [Fact]
            public void ShouldSerializeProtocolV1()
            {
                // Given
                var expected = new FileChange
                {
                    Buffer = "buffer",
                    FileName = "fileName",
                    FromDisk = true
                };
                expected.LineChanges.Add(new LineChange
                {
                    StartColumn = 1,
                    EndColumn = 2,
                    StartLine = 3,
                    EndLine = 4,
                    NewText = "5"
                });

                // When
                FileChangeSerializer.Serialize(_fixture.Writer, expected, Constants.Protocol.V1);
                _fixture.ResetStreamPosition();
                var actual = FileChangeSerializer.Deserialize(_fixture.Reader, out var version);

                // Then
                Assert.NotNull(actual);
                Assert.Equal(Constants.Protocol.V1, version);
                Assert.Equal(expected.Buffer, actual.Buffer);
                Assert.Equal(expected.FileName, actual.FileName);
                Assert.Equal(expected.FromDisk, actual.FromDisk);
                Assert.Equal(expected.LineChanges, actual.LineChanges);
            }
        }
    }
}
