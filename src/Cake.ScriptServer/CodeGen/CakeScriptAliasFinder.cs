using System;
using System.Collections.Generic;
using System.Linq;
using Cake.Core.IO;
using Cake.Core.Scripting;
using Cake.ScriptServer.Extensions;
using Cake.ScriptServer.Reflection;
using Mono.Cecil;

namespace Cake.ScriptServer.CodeGen
{
    internal class CakeScriptAliasFinder
    {
        public IReadOnlyList<CakeScriptAlias> FindAliases(ICollection<FilePath> paths)
        {
            var parameters = new ReaderParameters
            {
                AssemblyResolver = GetResolver(paths),
            };

            // Load all assembly definitions.
            var assemblies = paths
                .Select(path => AssemblyDefinition.ReadAssembly(path.FullPath, parameters))
                .ToList();

            // Find all aliases in the loaded assembly definitions.
            var result = new List<CakeScriptAlias>();
            foreach (var assembly in assemblies)
            {
                InspectAssembly(assembly, result);
            }

            // Return the result.
            return result;
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

        private static void InspectAssembly(AssemblyDefinition assembly, ICollection<CakeScriptAlias> result)
        {
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

                    InspectType(assembly, type, result);
                }
            }
        }

        private static void InspectType(AssemblyDefinition assembly, TypeDefinition type, ICollection<CakeScriptAlias> result)
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
