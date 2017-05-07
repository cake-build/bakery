using System;
using System.Collections.Generic;
using System.Linq;
using Cake.ScriptServer.Documentation;
using Mono.Cecil;

namespace Cake.ScriptServer.Reflection
{
    public sealed class MethodSignature
    {
        public string CRef { get; }
        public string Name { get; set; }
        public TypeSignature ReturnType { get; }
        public TypeSignature DeclaringType { get; }
        public List<string> GenericParameters { get; }
        public IReadOnlyList<ParameterSignature> Parameters { get; }

        private MethodSignature(
            string cref, string name,
            TypeSignature declaringType,
            TypeSignature returnType,
            IEnumerable<string> genericParameters,
            IEnumerable<ParameterSignature> parameters)
        {
            CRef = cref;
            Name = name;
            ReturnType = returnType;
            DeclaringType = declaringType;
            GenericParameters = new List<string>(genericParameters);
            Parameters = new List<ParameterSignature>(parameters);
        }

        public static MethodSignature Create(MethodDefinition method)
        {
            // Get the method Identity and name.
            var cref = CRefGenerator.GetMethodCRef(method);
            var name = GetMethodName(method);

            // Get the declaring type and return type.
            var declaringType = TypeSignature.Create(method.DeclaringType);
            var returnType = TypeSignature.Create(method.ReturnType);

            // Get generic parameters and arguments.
            var genericParameters = new List<string>();
            if (method.HasGenericParameters)
            {
                // Generic parameters
                genericParameters.AddRange(
                    method.GenericParameters.Select(
                        genericParameter => genericParameter.Name));
            }

            // Get all parameters.
            var parameters = method.Parameters.Select(ParameterSignature.Create).ToList();

            // Return the method signature.
            return new MethodSignature(
                cref, name,
                declaringType, returnType, 
                genericParameters, parameters);
        }

        private static string GetMethodName(MethodDefinition definition)
        {
            if (definition.IsConstructor)
            {
                var name = definition.DeclaringType.Name;
                var index = name.IndexOf('`');
                if (index != -1)
                {
                    name = name.Substring(0, index);
                }
                return name;
            }
            return definition.Name;
        }
    }
}
