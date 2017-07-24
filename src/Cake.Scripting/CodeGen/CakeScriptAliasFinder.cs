// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Cake.Core.IO;
using Cake.Core.Scripting;
using Cake.Scripting;
using Cake.Scripting.Documentation;
using Cake.Scripting.Reflection;
using Mono.Cecil;

namespace Cake.Scripting.CodeGen
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
                    if (IsCakeAlias(method, out bool isPropertyAlias))
                    {
                        if (!IsValidCakeAlias(method, isPropertyAlias))
                        {
                            continue;
                        }

                        // Create the alias.
                        var alias = new CakeScriptAlias()
                        {
                            Method = MethodSignature.Create(method),
                            Obsolete = method.GetObsoleteAttribute(),
                            Type = isPropertyAlias ? ScriptAliasType.Property : ScriptAliasType.Method,
                            Name = method.Name,
                            Namespaces = new HashSet<string>(StringComparer.Ordinal)
                        };

                        // Cached property alias?
                        if (alias.Type == ScriptAliasType.Property)
                        {
                            alias.Cached = IsCakePropertyAliasCached(method);
                        }

                        // Get documentation.
                        if (documentation.TryGetValue(alias.Method.CRef, out XElement element))
                        {
                            alias.Documentation = element;
                        }

                        // Get namespaces.
                        alias.Namespaces.Add(type.Namespace);
                        alias.Namespaces.AddRange(assembly.GetCakeNamespaces());
                        alias.Namespaces.AddRange(type.GetCakeNamespaces());
                        alias.Namespaces.AddRange(method.GetCakeNamespaces());

                        // Add it to the results.
                        result.Add(alias);
                    }
                }
            }
        }

        public static bool IsCakeAlias(MethodDefinition method, out bool isPropertyAlias)
        {
            foreach (var attribute in method.CustomAttributes)
            {
                if (attribute.AttributeType != null && (
                        attribute.AttributeType.FullName == "Cake.Core.Annotations.CakeMethodAliasAttribute" ||
                        attribute.AttributeType.FullName == "Cake.Core.Annotations.CakePropertyAliasAttribute"))
                {
                    isPropertyAlias = attribute.AttributeType.FullName == "Cake.Core.Annotations.CakePropertyAliasAttribute";
                    return true;
                }
            }
            isPropertyAlias = false;
            return false;
        }

        public static bool IsValidCakeAlias(MethodDefinition method, bool isPropertyAlias)
        {
            if (!method.IsExtensionMethod())
            {
                return false;
            }
            if (method.Parameters.Count == 0)
            {
                return false;
            }
            if (method.Parameters[0].ParameterType.FullName != "Cake.Core.ICakeContext")
            {
                return false;
            }

            if (isPropertyAlias)
            {
                if (method.HasGenericParameters)
                {
                    return false;
                }
                if (method.ReturnType.FullName == "System.Void")
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsCakePropertyAliasCached(MethodDefinition method)
        {
            foreach (var attribute in method.CustomAttributes)
            {
                if (attribute.AttributeType != null && attribute.AttributeType.FullName == "Cake.Core.Annotations.CakePropertyAliasAttribute")
                {
                    if (attribute.HasProperties)
                    {
                        var property = attribute.Properties.FirstOrDefault(p => p.Name == "Cache");
                        if (property.Argument.Type != null)
                        {
                            return (bool)property.Argument.Value;
                        }
                    }
                }
            }
            return false;
        }

    }
}
