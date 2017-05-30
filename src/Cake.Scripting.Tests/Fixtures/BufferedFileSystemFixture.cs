using Cake.Core;
using Cake.Core.IO;
using Cake.Scripting.IO;
using Cake.Testing;

namespace Cake.Scripting.Tests.Fixtures
{
    public sealed class BufferedFileSystemFixture
    {
        public IFileSystem FileSystem { get; set; }

        public BufferedFileSystemFixture()
        {
            FileSystem = new FakeFileSystem(FakeEnvironment.CreateWindowsEnvironment());
        }

        public IBufferedFileSystem CreateBufferedFileSystem()
        {
            return new BufferedFileSystem(FileSystem);
        }
    }
}
