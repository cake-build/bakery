// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cake.Core;
using Cake.Core.Configuration;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Core.Scripting;
using Cake.Core.Scripting.Analysis;
using Cake.Core.Scripting.Processors.Loading;
using Cake.Scripting.Abstractions.Models;
using Cake.Scripting.CodeGen.Generators;
using Cake.Scripting.Abstractions;
using Cake.Scripting.IO;
using Cake.Scripting.Reflection.Emitters;

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
        private readonly CakeMethodAliasGenerator _methodGenerator;
        private readonly CakePropertyAliasGenerator _propertyGenerator;

        public CakeScriptGenerator(
            IBufferedFileSystem fileSystem,
            ICakeEnvironment environment,
            IGlobber globber,
            ICakeConfiguration configuration,
            IScriptProcessor processor,
            IScriptAliasFinder aliasFinder,
            ICakeLog log,
            IEnumerable<ILoadDirectiveProvider> loadDirectiveProviders = null)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _globber = globber ?? throw new ArgumentNullException(nameof(globber));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _aliasFinder = aliasFinder ?? throw new ArgumentNullException(nameof(fileSystem));

            _analyzer = new ScriptAnalyzer(_fileSystem, _environment, _log, loadDirectiveProviders);

            var typeEmitter = new TypeEmitter();
            var parameterEmitter = new ParameterEmitter(typeEmitter);

            _methodGenerator = new CakeMethodAliasGenerator(typeEmitter, parameterEmitter);
            _propertyGenerator = new CakePropertyAliasGenerator(typeEmitter);
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
            var result = _analyzer.Analyze(scriptPath.GetFilename());

            // Install tools.
            _log.Verbose("Processing build script...");
            var toolsPath = GetToolPath(scriptPath.GetDirectory());
            foreach (var tool in result.Tools)
            {
                try
                {
                    _log.Verbose("Installing tools...");
                    _processor.InstallTools(new []{ tool }, toolsPath);
                }
                catch (Exception e)
                {
                    // Log and continue if it fails
                    _log.Error(e);
                }
            }

            // Install addins.
            var cakeRoot = GetCakePath(toolsPath);
            var addinRoot = GetAddinPath(scriptPath.GetDirectory());
            foreach (var addin in result.Addins)
            {
                try
                {
                    _log.Verbose("Installing addins...");
                    var addinReferences = _processor.InstallAddins(new []{ addin }, addinRoot);
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
            var references = new HashSet<FilePath>(GetDefaultReferences(cakeRoot));
            references.AddRange(result.References.Select(r => new FilePath(r)));

            // Find aliases
            _log.Verbose("Finding aliases...");
            var aliases = new List<CakeScriptAlias>();
            foreach (var reference in references)
            {
                aliases.AddRange(_aliasFinder.FindAliases(reference));
            }

            // Import all namespaces.
            _log.Verbose("Importing namespaces...");
            var namespaces = new HashSet<string>(result.Namespaces, StringComparer.Ordinal);
            namespaces.AddRange(GetDefaultNamespaces());
            namespaces.AddRange(aliases.SelectMany(alias => alias.Namespaces));

            // Set Host object
            _log.Verbose("Setting host object...");
            var hostObject = GetHostObject(cakeRoot);

            // Create the response.
            // ReSharper disable once UseObjectOrCollectionInitializer
            _log.Verbose("Creating response...");
            var response = new CakeScript();
            response.Host.TypeName = hostObject.TypeName;
            response.Host.AssemblyPath = hostObject.AssemblyPath;
            response.Source = GenerateSource(aliases) + string.Join("\n", result.Lines);
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

        // TODO: Move to conventions
        private IEnumerable<FilePath> GetDefaultReferences(DirectoryPath root)
        {
            // Prepare the default assemblies.
            var references = new HashSet<FilePath>();
            references.Add(typeof(Action).GetTypeInfo().Assembly.Location); // mscorlib or System.Private.Core
            references.Add(typeof(IQueryable).GetTypeInfo().Assembly.Location); // System.Core or System.Linq.Expressions

            references.Add(root.CombineWithFilePath("Cake.Core.dll").FullPath);
            references.Add(root.CombineWithFilePath("Cake.Common.dll").FullPath);

            references.Add(typeof(Uri).GetTypeInfo().Assembly.Location); // System
            references.Add(typeof(System.Xml.XmlReader).GetTypeInfo().Assembly.Location); // System.Xml
            references.Add(typeof(System.Xml.Linq.XDocument).GetTypeInfo().Assembly.Location); // System.Xml.Linq
            references.Add(typeof(System.Data.DataTable).GetTypeInfo().Assembly.Location); // System.Data

            // Return the assemblies.
            return references;
        }

        private static Abstractions.Models.ScriptHost GetHostObject(DirectoryPath root)
        {
            return new Abstractions.Models.ScriptHost
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
            var pattern = string.Concat(toolPath.FullPath, "/**/Cake.Core.dll");
            var cakeCorePath = _globber.GetFiles(pattern).FirstOrDefault();

            return cakeCorePath?.GetDirectory().MakeAbsolute(_environment) ?? toolPath.Combine("Cake").Collapse();
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
                if (alias.Type == ScriptAliasType.Method)
                {
                    _methodGenerator.Generate(writer, alias);
                }
                else
                {
                    _propertyGenerator.Generate(writer, alias);
                }

                writer.WriteLine();
                writer.WriteLine();
            }

            return writer.ToString();
        }
    }
}
