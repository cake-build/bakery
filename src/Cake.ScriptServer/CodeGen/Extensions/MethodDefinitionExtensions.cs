using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
