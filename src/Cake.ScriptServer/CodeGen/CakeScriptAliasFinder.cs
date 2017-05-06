using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Cake.Core.IO;
using Cake.Core.Scripting;
using Cake.ScriptServer.Documentation;
using Cake.ScriptServer.Extensions;
using Cake.ScriptServer.Reflection;
using Mono.Cecil;

namespace Cake.ScriptServer.CodeGen
{
    public sealed class CakeScriptAliasFinder
    {
        private readonly DocumentationProvider _documentation;

        public CakeScriptAliasFinder(IFileSystem fileSystem)
        {
            _documentation = new DocumentationProvider(fileSystem);
        }

        public IReadOnlyList<CakeScriptAlias> FindAliases(ICollection<FilePath> paths)
        {
            var parameters = new ReaderParameters
            {
                AssemblyResolver = GetResolver(paths),
            };

            // Load all assembly definitions.
            var assemblies = paths
                .Select(path => Tuple.Create(AssemblyDefinition.ReadAssembly(path.FullPath, parameters), path))
                .ToList();

            return FindAliases(assemblies);
        }

        private static IAssemblyResolver GetResolver(IEnumerable<FilePath> paths)
        {
#if NETCORE
            throw new NotImplementedException("Assembly resolver not implemented for .NET Core");
#else
            var resolver = new DefaultAssemblyResolver();
            foreach (var path in paths)
            {
                resolver.AddSearchDirectory(path.GetDirectory().FullPath);
            }
            return resolver;
#endif
        }

        private IReadOnlyList<CakeScriptAlias> FindAliases(IEnumerable<Tuple<AssemblyDefinition, FilePath>> assemblies)
        {
            // Find all aliases in the loaded assembly definitions.
            var result = new List<CakeScriptAlias>();
            foreach (var assembly in assemblies)
            {
                InspectAssembly(assembly.Item1, assembly.Item2, result);
            }

            // Return the result.
            return result;
        }

        private void InspectAssembly(AssemblyDefinition assembly, FilePath path, ICollection<CakeScriptAlias> result)
        {
            var documentation = _documentation.Load(path.ChangeExtension("xml"));

            foreach (var module in assembly.Modules)
            {
                foreach (var type in module.Types)
                {
                    if (type.IsSpecialName)
                    {
                        continue;
                    }
                    if (type.Name == "<Module>")
                    {
                        continue;
                    }
                    if (type.Name.StartsWith("_"))
                    {
                        continue;
                    }
                    if (type.IsAnonymousType())
                    {
                        continue;
                    }

                    InspectType(assembly, type, documentation, result);
                }
            }
        }

        private void InspectType(
            AssemblyDefinition assembly,
            TypeDefinition type,
            IDictionary<string, XElement> documentation,
            ICollection<CakeScriptAlias> result)
        {
            if (type.IsStatic())
            {
                foreach (var method in type.Methods)
                {
                    if (method.IsCakeAlias(out bool isPropertyAlias))
                    {
                        // Create the alias.
                        var alias = new CakeScriptAlias()
                        {
                            Method = MethodSignature.Create(method),
                            Type = isPropertyAlias ? ScriptAliasType.Property : ScriptAliasType.Method,
                            Name = method.Name,
                            Namespaces = new HashSet<string>(StringComparer.Ordinal)
                        };

                        // Get documentation.
                        if (documentation.TryGetValue(alias.Method.CRef, out XElement element))
                        {
                            alias.Documentation = element;
                        }

                        // Get namespaces.
                        alias.Namespaces.AddRange(assembly.GetCakeNamespaces());
                        alias.Namespaces.AddRange(type.GetCakeNamespaces());
                        alias.Namespaces.AddRange(method.GetCakeNamespaces());

                        // Add it to the results.
                        result.Add(alias);
                    }
                }
            }
        }
    }
}
