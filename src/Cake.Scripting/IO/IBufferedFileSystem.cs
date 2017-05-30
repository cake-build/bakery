using System.Collections.Generic;
using Cake.Core.IO;
using Cake.Scripting.Abstractions.Models;

namespace Cake.Scripting.IO
{
    public interface IBufferedFileSystem : IFileSystem
    {
        void UpdateFileBuffer(FilePath path, string buffer);

        void UpdateFileBuffer(FilePath path, ICollection<LineChange> lineChanges);

        void RemoveFileFromBuffer(FilePath path);
    }
}
