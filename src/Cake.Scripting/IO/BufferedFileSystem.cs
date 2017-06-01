using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Scripting.Abstractions.Models;

namespace Cake.Scripting.IO
{
    public class BufferedFileSystem : IBufferedFileSystem
    {
        private readonly IFileSystem _fileSystem;
        private readonly ICakeLog _log;
        private readonly ConcurrentDictionary<string, IFile> _buffer;

        public BufferedFileSystem(IFileSystem fileSystem, ICakeLog log)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _buffer = new ConcurrentDictionary<string, IFile>();
        }

        public IDirectory GetDirectory(DirectoryPath path)
        {
            _log.Verbose("Returning directory \"{0}\" from underlying filesystem", path.FullPath);
            return _fileSystem.GetDirectory(path);
        }

        public IFile GetFile(FilePath path)
        {
            if (_buffer.TryGetValue(path.FullPath, out IFile file))
            {
                _log.Verbose("Returning file \"{0}\" from buffer", path.FullPath);
                return file;
            }

            _log.Verbose("Returning file \"{0}\" from underlying filesystem", path.FullPath);
            return _fileSystem.GetFile(path);
        }

        public void UpdateFileBuffer(FilePath path, string buffer)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var file = new BufferedFile(path, buffer);
            _log.Verbose("Adding file \"{0}\" to buffer", path.FullPath);
            _buffer.AddOrUpdate(path.FullPath, file, (k, v) => file);
        }

        public void UpdateFileBuffer(FilePath path, ICollection<LineChange> lineChanges)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (lineChanges == null)
            {
                throw new ArgumentNullException(nameof(lineChanges));
            }
            if (!lineChanges.Any())
            {
                return;
            }

            var lines = ReadLines(path);
            var buffer = string.Join("\n", lines);

            foreach (var lineChange in lineChanges)
            {
                var startIndex = GetPosition(lines, lineChange.StartLine, lineChange.StartColumn);
                var stopIndex = GetPosition(lines, lineChange.EndLine, lineChange.EndColumn);
                var length = stopIndex - startIndex;
                var bufferBuilder = new StringBuilder();

                var newTextLength = lineChange.NewText?.Length ?? 0;

                // ignore changes that don't change anything
                if (length == 0 && newTextLength == 0)
                {
                    continue;
                }

                // if we've skipped a range, add
                if (startIndex > 0)
                {
                    var subText = buffer.Substring(0, startIndex);
                    bufferBuilder.Append(subText);
                }

                if (newTextLength > 0)
                {
                    bufferBuilder.Append(lineChange.NewText);
                }

                // no changes actually happend?
                if (stopIndex == 0 && bufferBuilder.Length == 0)
                {
                    continue;
                }

                if (stopIndex < buffer.Length)
                {
                    var subText = buffer.Substring(stopIndex, buffer.Length - stopIndex);
                    bufferBuilder.Append(subText);
                }

                // Update buffer for next change
                buffer = bufferBuilder.ToString();
                lines = buffer.Split('\n');
            }

            UpdateFileBuffer(path, buffer);
        }

        public void RemoveFileFromBuffer(FilePath path)
        {
            _log.Verbose("Removing file \"{0}\" from buffer", path.FullPath);
            _buffer.TryRemove(path.FullPath, out IFile file);
        }

        private string[] ReadLines(FilePath path)
        {
            if (!_buffer.TryGetValue(path.FullPath, out IFile file))
            {
                file = _fileSystem.GetFile(path);
            }

            return file.ReadLines(Encoding.UTF8).ToArray();
        }

        private static int GetPosition(string[] lines, int line, int column)
        {
            var position = 0;

            for (var i = 0; i < line; i++)
            {
                position += lines[i].Length + 1;
            }

            return position + column;
        }
    }
}
