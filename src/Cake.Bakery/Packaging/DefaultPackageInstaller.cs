using System;
using System.Collections.Generic;
using System.Linq;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Core.Packaging;

namespace Cake.Bakery.Packaging
{
    internal sealed class DefaultPackageInstaller : IPackageInstaller
    {
        private readonly ICakeEnvironment _environment;
        private readonly IFileSystem _fileSystem;
        private readonly IGlobber _globber;
        private readonly ICakeLog _log;

        public DefaultPackageInstaller(ICakeEnvironment environment, IFileSystem fileSystem, IGlobber globber, ICakeLog log)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _globber = globber ?? throw new ArgumentNullException(nameof(globber));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public bool CanInstall(PackageReference package, PackageType type)
        {
            // TODO: Callback to client and check for installation
            return true;
        }

        public IReadOnlyCollection<IFile> Install(PackageReference package, PackageType type, DirectoryPath path)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            path = path.MakeAbsolute(_environment);

            var packagePath = path.Combine(package.Package);

            // Fetch available content from disc.
            var content = GetFiles(packagePath, package, type);
            if (!content.Any())
            {
                // TODO: Callback to client
            }

            return content;
        }

        private IReadOnlyCollection<IFile> GetFiles(DirectoryPath path, PackageReference package, PackageType type)
        {
            if (type == PackageType.Addin)
            {
                return GetAddinAssemblies(path);
            }
            if (type == PackageType.Tool)
            {
                return GetToolFiles(path, package);
            }

            return new List<IFile>();
        }

        private IReadOnlyCollection<IFile> GetAddinAssemblies(DirectoryPath packageDirectory)
        {
            if (!_fileSystem.Exist(packageDirectory))
            {
                return new List<IFile>();
            }

            // TODO: just assume net45 for now
            var packageAssemblies = GetAllPackageAssemblies(packageDirectory.Combine("lib/net45"));
            if (!packageAssemblies.Any())
            {
                return new List<IFile>();
            }

            var resolvedAssemblyFiles = ResolveAssemblyFiles(packageAssemblies);

            return resolvedAssemblyFiles;
        }

        private FilePath[] GetAllPackageAssemblies(DirectoryPath packageDirectory)
        {
            return _fileSystem.GetDirectory(packageDirectory).GetFiles("*.dll", SearchScope.Recursive)
                .Where(
                    file =>
                        !"Cake.Core.dll".Equals(file.Path.GetFilename().FullPath, StringComparison.OrdinalIgnoreCase))
                .Select(a => a.Path)
                .ToArray();
        }

        private IReadOnlyCollection<IFile> ResolveAssemblyFiles(IEnumerable<FilePath> compatibleAssemblyPaths)
        {
            return compatibleAssemblyPaths.Select(_fileSystem.GetFile).ToList().AsReadOnly();
        }

        private IReadOnlyCollection<IFile> GetToolFiles(DirectoryPath path, PackageReference package)
        {
            var result = new List<IFile>();
            var toolDirectory = _fileSystem.GetDirectory(path);
            if (toolDirectory.Exists)
            {
                result.AddRange(GetFiles(path, package));
            }
            return result;
        }

        private IEnumerable<IFile> GetFiles(DirectoryPath path, PackageReference package)
        {
            var collection = new FilePathCollection(new PathComparer(_environment));

            // Get default files (exe and dll).
            var patterns = new[] { path.FullPath + "/**/*.exe", path.FullPath + "/**/*.dll" };
            foreach (var pattern in patterns)
            {
                collection.Add(_globber.GetFiles(pattern));
            }

            // Include files.
            if (package.Parameters.ContainsKey("include"))
            {
                foreach (var include in package.Parameters["include"])
                {
                    var includePath = string.Concat(path.FullPath, "/", include.TrimStart('/'));
                    collection.Add(_globber.GetFiles(includePath));
                }
            }

            // Exclude files.
            if (package.Parameters.ContainsKey("exclude"))
            {
                foreach (var exclude in package.Parameters["exclude"])
                {
                    var excludePath = string.Concat(path.FullPath, "/", exclude.TrimStart('/'));
                    collection.Remove(_globber.GetFiles(excludePath));
                }
            }

            // Return the files.
            return collection.Select(p => _fileSystem.GetFile(p)).ToArray();
        }
    }
}
