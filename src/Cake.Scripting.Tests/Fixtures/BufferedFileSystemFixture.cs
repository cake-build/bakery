using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Scripting.IO;
using Cake.Testing;

namespace Cake.Scripting.Tests.Fixtures
{
    public sealed class BufferedFileSystemFixture
    {
        public IFileSystem FileSystem { get; set; }

        public ICakeLog Log { get; set; }

        public BufferedFileSystemFixture()
        {
            FileSystem = new FakeFileSystem(FakeEnvironment.CreateWindowsEnvironment());
            Log = new FakeLog();
        }

        public IBufferedFileSystem CreateBufferedFileSystem()
        {
            return new BufferedFileSystem(FileSystem, Log);
        }
    }
}
