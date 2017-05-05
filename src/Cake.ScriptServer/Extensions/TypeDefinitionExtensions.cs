using System;
using Mono.Cecil;

// ReSharper disable once CheckNamespace
namespace Cake.ScriptServer
{
    internal static class TypeDefinitionExtensions
    {
        public static TypeReference TryResolve(this TypeReference type)
        {
            var resolved = type;
            if (!type.IsDefinition && !type.IsGenericParameter)
            {
                resolved = resolved.Resolve();
                if (resolved == null)
                {
                    resolved = type;
                }
            }
            return resolved;
        }

        public static bool IsAnonymousType(this TypeDefinition type)
        {
            return type.Name.StartsWith("<>", StringComparison.OrdinalIgnoreCase)
                   || type.Name.StartsWith("VB$", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsStatic(this TypeDefinition type)
        {
            return type.IsAbstract && type.IsSealed;
        }
    }
}
