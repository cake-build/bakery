using System.IO;
using Cake.Core.IO;

namespace Cake.Scripting.Tests.Extensions
{
    internal static class FileSystemExtensions
    {
        public static string GetFileContent(this IFileSystem fileSystem, FilePath path)
        {
            using (var stream = fileSystem.GetFile(path).OpenRead())
            using (var reader = new StreamReader(stream))
            {
                stream.Position = 0;
                return reader.ReadToEnd();
            }
        }
    }
}
