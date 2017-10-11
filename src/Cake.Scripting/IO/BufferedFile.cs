// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Cake.Core.IO;

namespace Cake.Scripting.IO
{
    internal class BufferedFile : IFile
    {
        private readonly string _content;

        public BufferedFile(FilePath filePath, string content)
        {
            Path = filePath;
            _content = content;
        }

        public FilePath Path { get; }

        public long Length => _content.Length;

        public FileAttributes Attributes
        {
            get { return FileAttributes.Normal; }
            set { }
        }

        public bool Exists => true;

        public bool Hidden => false;

        Core.IO.Path IFileSystemInfo.Path => Path;

        public void Copy(FilePath destination, bool overwrite)
        {
            return;
        }

        public void Delete()
        {
            return;
        }

        public void Move(FilePath destination)
        {
            return;
        }

        public Stream Open(FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(_content);
            writer.Flush();
            stream.Position = 0;

            return stream;
        }
    }
}
