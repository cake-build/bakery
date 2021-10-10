// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Xml;
using System.Xml.Linq;
using Cake.Core;
using Cake.Core.Configuration;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Core.Scripting;
using Cake.Core.Scripting.Analysis;
using Cake.Core.Scripting.Processors.Loading;
using Cake.Scripting.Abstractions;
using Cake.Scripting.Abstractions.Models;
using Cake.Scripting.IO;
using ScriptHost = Cake.Scripting.Abstractions.Models.ScriptHost;

namespace Cake.Scripting.CodeGen
{
    public sealed class CakeScriptGenerator : IScriptGenerationService
    {
        private readonly ICakeEnvironment _environment;
        private readonly ICakeLog _log;
        private readonly ICakeConfiguration _configuration;
        private readonly IGlobber _globber;
        private readonly IScriptAnalyzer _analyzer;
        private readonly IScriptProcessor _processor;
        private readonly IBufferedFileSystem _fileSystem;
        private readonly IScriptAliasFinder _aliasFinder;
        private readonly ICakeAliasGenerator _aliasGenerator;
        private readonly DirectoryPath _addinRoot;
        private readonly DirectoryPath _cakeRoot;
        private readonly ScriptHost _hostObject;
        private readonly Lazy<ISet<FilePath>> _defaultReferences;

        public CakeScriptGenerator(
            IBufferedFileSystem fileSystem,
            ICakeEnvironment environment,
            IGlobber globber,
            ICakeConfiguration configuration,
            IScriptProcessor processor,
            IScriptAliasFinder aliasFinder,
            ICakeAliasGenerator aliasGenerator,
            ICakeLog log,
            IEnumerable<ILoadDirectiveProvider> loadDirectiveProviders = null)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _globber = globber ?? throw new ArgumentNullException(nameof(globber));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _aliasFinder = aliasFinder ?? throw new ArgumentNullException(nameof(aliasFinder));
            _aliasGenerator = aliasGenerator ?? throw new ArgumentNullException(nameof(aliasGenerator));
            _analyzer = new ScriptAnalyzer(_fileSystem, _environment, _log, loadDirectiveProviders);

            _addinRoot = GetAddinPath(_environment.WorkingDirectory);
            _cakeRoot = GetCakePath(GetToolPath(_environment.WorkingDirectory));
            _hostObject = GetHostObject(_cakeRoot);
            _defaultReferences = new Lazy<ISet<FilePath>>(() => GetDefaultReferences(_cakeRoot));
        }

        public CakeScript Generate(FileChange fileChange)
        {
            if (fileChange == null)
            {
                throw new ArgumentNullException(nameof(fileChange));
            }

            // Make the script path absolute.
            var scriptPath = new FilePath(fileChange.FileName).MakeAbsolute(_environment);

            // Prepare the file changes
            _log.Verbose("Handling file change...");
            HandleFileChange(scriptPath, fileChange);

            // Analyze the script file.
            _log.Verbose("Analyzing build script...");
            var result = _analyzer.Analyze(scriptPath, new ScriptAnalyzerSettings
            {
                Mode = ScriptAnalyzerMode.Everything,
            });

            // Install addins.
            foreach (var addin in result.Addins)
            {
                try
                {
                    _log.Verbose("Installing addins...");
                    var addinReferences = _processor.InstallAddins(new[] { addin }, _addinRoot);
                    foreach (var addinReference in addinReferences)
                    {
                        result.References.Add(addinReference.FullPath);
                    }
                }
                catch (Exception e)
                {
                    // Log and continue if it fails
                    _log.Error(e);
                }
            }

            // Load all references.
            _log.Verbose("Adding references...");
            var references = new HashSet<FilePath>(_defaultReferences.Value);
            references.AddRange(result.References.Select(r => new FilePath(r)));

            // Find aliases
            _log.Verbose("Finding aliases...");
            var aliases = new List<CakeScriptAlias>();
            foreach (var reference in references)
            {
                if (_fileSystem.Exist(reference))
                {
                    aliases.AddRange(_aliasFinder.FindAliases(reference));
                }
            }

            // Import all namespaces.
            _log.Verbose("Importing namespaces...");
            var namespaces = new HashSet<string>(result.Namespaces, StringComparer.Ordinal);
            namespaces.AddRange(GetDefaultNamespaces());
            namespaces.AddRange(aliases.SelectMany(alias => alias.Namespaces));

            // Create the response.
            // ReSharper disable once UseObjectOrCollectionInitializer
            _log.Verbose("Creating response...");
            var response = new CakeScript();
            response.Host.TypeName = _hostObject.TypeName;
            response.Host.AssemblyPath = _hostObject.AssemblyPath;
            response.Source = string.Join("\n", result.Defines) +
                              string.Join("\n", result.UsingAliases) +
                              string.Join("\n", result.UsingStaticDirectives) +
                              GenerateSource(aliases) +
                              string.Join("\n", result.Lines);
            response.Usings.AddRange(namespaces);
            response.References.AddRange(references.Select(r => r.FullPath));

            // Return the response.
            return response;
        }

        private void HandleFileChange(FilePath path, FileChange fileChange)
        {
            if (fileChange.FromDisk)
            {
                _fileSystem.RemoveFileFromBuffer(path);
                return;
            }
            if (fileChange.LineChanges != null && fileChange.LineChanges.Any())
            {
                _fileSystem.UpdateFileBuffer(path, fileChange.LineChanges);
                return;
            }

            _fileSystem.UpdateFileBuffer(path, fileChange.Buffer);
        }

        // TODO: Move to conventions
        private IEnumerable<string> GetDefaultNamespaces()
        {
            return new List<string>
            {
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "System.Text",
                "System.Threading.Tasks",
                "System.IO",
                "Cake.Core",
                "Cake.Core.IO",
                "Cake.Core.Scripting",
                "Cake.Core.Diagnostics"
            };
        }

        private ISet<FilePath> GetDefaultReferences(DirectoryPath root)
        {
            // Prepare the default assemblies.
            var references = new HashSet<FilePath>();
            references.Add(typeof(Action).GetTypeInfo().Assembly.Location); // mscorlib or System.Private.Core
            references.Add(typeof(IQueryable).GetTypeInfo().Assembly.Location); // System.Core or System.Linq.Expressions

            references.Add(root.CombineWithFilePath("Cake.Core.dll").FullPath);
            references.Add(root.CombineWithFilePath("Cake.Common.dll").FullPath);

            references.Add(typeof(Uri).GetTypeInfo().Assembly.Location); // System
            references.Add(typeof(XmlReader).GetTypeInfo().Assembly.Location); // System.Xml
            references.Add(typeof(XDocument).GetTypeInfo().Assembly.Location); // System.Xml.Linq
            references.Add(typeof(DataTable).GetTypeInfo().Assembly.Location); // System.Data
            references.Add(typeof(ZipArchive).GetTypeInfo().Assembly.Location); // System.IO.Compression
            references.Add(typeof(ZipFile).GetTypeInfo().Assembly.Location); // System.IO.Compression.FileSystem
            references.Add(typeof(HttpClient).GetTypeInfo().Assembly.Location); // System.Net.Http
            references.Add(typeof(DataContractJsonSerializer).GetTypeInfo().Assembly.Location); // System.Runtime.Serialization

            // Return the assemblies.
            return references;
        }

        private static ScriptHost GetHostObject(DirectoryPath root)
        {
            return new ScriptHost
            {
                AssemblyPath = root.CombineWithFilePath("Cake.Core.dll").FullPath,
                TypeName = typeof(IScriptHost).AssemblyQualifiedName
            };
        }

        private DirectoryPath GetToolPath(DirectoryPath root)
        {
            var toolPath = _configuration.GetValue(Constants.Paths.Tools);
            if (!string.IsNullOrWhiteSpace(toolPath))
            {
                return new DirectoryPath(toolPath).MakeAbsolute(_environment);
            }

            return root.Combine("tools");
        }

        private DirectoryPath GetCakePath(DirectoryPath toolPath)
        {
            // Check for local cake in tools path
            var pattern = string.Concat(toolPath.FullPath, "/**/Cake.exe");
            var cakeCorePath = _globber.GetFiles(pattern).FirstOrDefault();
            if (cakeCorePath != null)
            {
                return cakeCorePath.GetDirectory().MakeAbsolute(_environment);
            }
            pattern = string.Concat(toolPath.FullPath, "/**/Cake.dll");
            cakeCorePath = _globber.GetFiles(pattern).FirstOrDefault();
            if (cakeCorePath != null)
            {
                return cakeCorePath.GetDirectory().MakeAbsolute(_environment);
            }

            // get .dotnet/tools default installation folder
            var userDotNetToolsPaths = new[] { "USERPROFILE", "HOME" }
                .Select(env => _environment.GetEnvironmentVariable(env))
                .Where(x => x != null)
                .Distinct()
                .Select(x => ((DirectoryPath)x).Combine(".dotnet/tools"))
                .Where(x => _fileSystem.Exist(x))
                .ToArray();

            // get all directories in PATH
            var separatorChar = _environment.Platform.Family == PlatformFamily.Windows ? ';' : ':';
            var directoriesInPath = (_environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                .Split(new[] { separatorChar }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => (DirectoryPath)x).ToArray();

            // Check DotNetToolsPath and PATH for dotnet-cake[.exe]
            var dotnetCakePath = new[] { "dotnet-cake.exe", "dotnet-cake" }
                .SelectMany(exe =>
                    userDotNetToolsPaths
                        .Union(directoriesInPath)
                        .Select(dir => dir.CombineWithFilePath(exe)))
                .FirstOrDefault(x => _fileSystem.Exist(x))?.GetDirectory();
            if (dotnetCakePath != null)
            {
                pattern = string.Concat(dotnetCakePath.FullPath, "/.store/**/^{netcoreapp3.1,netcoreapp2.1}/**/Cake.dll");
                var cakeDllPath = _globber.GetFiles(pattern).LastOrDefault();

                if (cakeDllPath != null)
                {
                    return cakeDllPath.GetDirectory().MakeAbsolute(_environment);
                }
            }

            // Check PATH for cake.exe
            var cakeExePath = directoriesInPath.FirstOrDefault(x => _fileSystem.Exist(x.CombineWithFilePath("Cake.exe")));
            if (cakeExePath != null)
            {
                return cakeExePath.MakeAbsolute(_environment);
            }

            return toolPath.Combine("Cake").Collapse();
        }

        private DirectoryPath GetAddinPath(DirectoryPath root)
        {
            var addinPath = _configuration.GetValue(Constants.Paths.Addins);
            if (!string.IsNullOrWhiteSpace(addinPath))
            {
                return new DirectoryPath(addinPath).MakeAbsolute(_environment);
            }

            var toolPath = GetToolPath(root);
            return toolPath.Combine("Addins").Collapse();
        }

        private string GenerateSource(IEnumerable<CakeScriptAlias> aliases)
        {
            var writer = new StringWriter();

            foreach (var alias in aliases)
            {
                _aliasGenerator.Generate(writer, alias);

                writer.WriteLine();
                writer.WriteLine();
            }

            return writer.ToString();
        }
    }
}
