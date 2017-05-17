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
using Cake.Scripting;
using Cake.Scripting.Reflection.Emitters;

namespace Cake.Scripting.CodeGen
{
    public sealed class CakeScriptGenerator
    {
        private readonly IFileSystem _fileSystem;
        private readonly ICakeEnvironment _environment;
        private readonly ICakeLog _log;
        private readonly ICakeConfiguration _configuration;
        private readonly IGlobber _globber;
        private readonly IScriptAnalyzer _analyzer;
        private readonly CakeScriptAliasFinder _aliasFinder;
        private readonly CakeMethodAliasGenerator _methodGenerator;
        private readonly CakePropertyAliasGenerator _propertyGenerator;
        private readonly IScriptProcessor _processor;

        public CakeScriptGenerator(
            IFileSystem fileSystem,
            ICakeEnvironment environment,
            IGlobber globber,
            ICakeConfiguration configuration,
            IScriptProcessor processor,
            ICakeLog log,
            IEnumerable<ILoadDirectiveProvider> loadDirectiveProviders = null)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _globber = globber ?? throw new ArgumentNullException(nameof(globber));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));

            _analyzer = new ScriptAnalyzer(_fileSystem, _environment, _log, loadDirectiveProviders);
            _aliasFinder = new CakeScriptAliasFinder(_fileSystem);

            var typeEmitter = new TypeEmitter();
            var parameterEmitter = new ParameterEmitter(typeEmitter);

            _methodGenerator = new CakeMethodAliasGenerator(typeEmitter, parameterEmitter);
            _propertyGenerator = new CakePropertyAliasGenerator(typeEmitter);
        }

        public CakeScript Generate(FilePath scriptPath)
        {
            // ReSharper disable once JoinNullCheckWithUsage
            if (scriptPath == null)
            {
                throw new ArgumentNullException(nameof(scriptPath));
            }

            // Make the script path absolute.
            scriptPath = scriptPath.MakeAbsolute(_environment);

            // Prepare the environment.
            _environment.WorkingDirectory = scriptPath.GetDirectory();

            // Analyze the script file.
            _log.Verbose("Analyzing build script...");
            var result = _analyzer.Analyze(scriptPath.GetFilename());

            // Install tools. This will callback to client
            _log.Verbose("Processing build script...");
            var toolsPath = GetToolPath(scriptPath.GetDirectory());
            try
            {
                _processor.InstallTools(result, toolsPath);
            }
            catch (Exception e)
            {
                // Log and continue if it fails
                _log.Error(e);
            }


            // Install addins. This will callback to client
            var cakeRoot = GetCakePath(toolsPath);
            var addinRoot = GetAddinPath(cakeRoot);
            try
            {
                var addinReferences = _processor.InstallAddins(result, addinRoot);
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

            // Load all references.
            var references = new HashSet<FilePath>(GetDefaultReferences(cakeRoot));
            references.AddRange(result.References.Select(r => new FilePath(r)));

            // Find aliases
            var aliases = _aliasFinder.FindAliases(references);

            //// Import all namespaces.
            var namespaces = new HashSet<string>(result.Namespaces, StringComparer.Ordinal);
            namespaces.AddRange(GetDefaultNamespaces());
            namespaces.AddRange(aliases.SelectMany(alias => alias.Namespaces));

            // Create the response.
            // ReSharper disable once UseObjectOrCollectionInitializer
            var response = new CakeScript();
            response.Source = GenerateSource(aliases) + string.Join("\n", result.Lines);
            response.Usings.AddRange(namespaces);
            response.References.AddRange(references.Select(r => r.FullPath));

            // Return the response.
            return response;
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

#if !NETCORE
            references.Add(typeof(Uri).GetTypeInfo().Assembly.Location); // System
            references.Add(typeof(System.Xml.XmlReader).GetTypeInfo().Assembly.Location); // System.Xml
            references.Add(typeof(System.Xml.Linq.XDocument).GetTypeInfo().Assembly.Location); // System.Xml.Linq
            references.Add(typeof(System.Data.DataTable).GetTypeInfo().Assembly.Location); // System.Data
#endif

            // Return the assemblies.
            return references;
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

            return cakeCorePath?.GetDirectory().MakeAbsolute(_environment);
        }

        private DirectoryPath GetAddinPath(DirectoryPath cakeRoot)
        {
            var addinPath = _configuration.GetValue(Constants.Paths.Addins);
            if (!string.IsNullOrWhiteSpace(addinPath))
            {
                return new DirectoryPath(addinPath).MakeAbsolute(_environment);
            }

            return cakeRoot.Combine("../Addins").Collapse();
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
