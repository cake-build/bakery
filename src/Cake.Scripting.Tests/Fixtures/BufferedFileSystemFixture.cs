// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Scripting.IO;
using Cake.Testing;

namespace Cake.Scripting.Tests.Fixtures
{
    public sealed class BufferedFileSystemFixture
    {
        private static bool IsWindows => System.IO.Path.DirectorySeparatorChar == '\\';

        public IFileSystem FileSystem { get; set; }

        public ICakeLog Log { get; set; }

        public BufferedFileSystemFixture()
        {
            FileSystem = new FakeFileSystem(IsWindows ?
                FakeEnvironment.CreateWindowsEnvironment() :
                FakeEnvironment.CreateUnixEnvironment());
            Log = new FakeLog();
        }

        public IBufferedFileSystem CreateBufferedFileSystem()
        {
            return new BufferedFileSystem(FileSystem, Log);
        }
    }
}
