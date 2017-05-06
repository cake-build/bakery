using System;
using System.Linq;
using Mono.Cecil;

// ReSharper disable once CheckNamespace
namespace Cake.ScriptServer
{
    internal static class MethodDefinitionExtensions
    {
        public static bool IsCakeAlias(this MethodDefinition method, out bool isPropertyAlias)
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

        public static ObsoleteAttribute GetObsoleteAttribute(this MethodDefinition method)
        {
            var obsoleteAttribute = method.CustomAttributes
                .SingleOrDefault(a => a.AttributeType.FullName == typeof(ObsoleteAttribute).FullName);

            if (obsoleteAttribute != null)
            {
                // Get message
                var message = obsoleteAttribute.ConstructorArguments.Count > 0
                    ? obsoleteAttribute.ConstructorArguments[0].Value as string
                    : null;

                // Error or warning?
                var error = obsoleteAttribute.ConstructorArguments.Count == 2
                    ? (bool?)obsoleteAttribute.ConstructorArguments[1].Value
                    : null;

                if (message == null)
                {
                    return new ObsoleteAttribute();
                }

                if (error == null)
                {
                    return new ObsoleteAttribute(message);
                }

                return new ObsoleteAttribute(message, error.Value);
            }
            return null;
        }
    }
}
