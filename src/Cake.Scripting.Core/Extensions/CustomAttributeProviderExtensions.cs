using System.Collections.Generic;
using Mono.Cecil;

// ReSharper disable once CheckNamespace
namespace Cake.Scripting.Core
{
    internal static class CustomAttributeProviderExtensions
    {
        public static IEnumerable<string> GetCakeNamespaces(this ICustomAttributeProvider method)
        {
            foreach (var attribute in method.CustomAttributes)
            {
                if (attribute.AttributeType != null &&
                    attribute.AttributeType.FullName == "Cake.Core.Annotations.CakeNamespaceImportAttribute")
                {
                    if (attribute.HasConstructorArguments)
                    {
                        var ns = attribute.ConstructorArguments[0].Value as string;
                        if (ns != null)
                        {
                            yield return ns;
                        }
                    }
                }
            }
        }
    }
}
