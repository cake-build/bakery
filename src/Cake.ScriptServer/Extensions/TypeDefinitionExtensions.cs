using System;
using Mono.Cecil;

// ReSharper disable once CheckNamespace
namespace Cake.ScriptServer
{
    internal static class TypeDefinitionExtensions
    {
        public static TypeDefinition TryResolve(this TypeReference type)
        {
            if (!type.IsDefinition && !type.IsGenericParameter)
            {
                var resolved = type.Resolve();
                if (resolved != null)
                {
                    return resolved;
                }
            }
            return null;
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
