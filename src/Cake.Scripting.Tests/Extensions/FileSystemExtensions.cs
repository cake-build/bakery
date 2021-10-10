// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using Cake.Core.IO;

namespace Cake.Scripting.Tests
{
    internal static class FileSystemExtensions
    {
        public static string GetFileContent(this IFileSystem fileSystem, FilePath path)
        {
            var file = fileSystem.GetFile(path);

            return string.Join("\n", file.ReadLines(Encoding.UTF8));
        }
    }
}
