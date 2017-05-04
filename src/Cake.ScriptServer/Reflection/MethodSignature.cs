using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Cake.ScriptServer.Reflection
{
    public sealed class MethodSignature
    {
        public string Name { get; set; }

        public TypeSignature ReturnType { get; }

        public TypeSignature DeclaringType { get; }

        public List<string> GenericParameters { get; }

        public IReadOnlyList<ParameterSignature> Parameters { get; }

        private MethodSignature(
            string name,
            TypeSignature declaringType,
            TypeSignature returnType,
            IEnumerable<string> genericParameters,
            IEnumerable<ParameterSignature> parameters)
        {
            Name = name;
            ReturnType = returnType;
            DeclaringType = declaringType;
            GenericParameters = new List<string>(genericParameters);
            Parameters = new List<ParameterSignature>(parameters);
        }

        public static MethodSignature Create(MethodReference method)
        {
            // Get the method definition.
            var definition = method.Resolve();

            // Get the method Identity and name.
            var name = GetMethodName(definition);

            // Get the declaring type and return type.
            var declaringType = TypeSignature.Create(definition.DeclaringType);
            var returnType = TypeSignature.Create(definition.ReturnType);

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
            var parameters = definition.Parameters.Select(ParameterSignature.Create).ToList();

            // Return the method signature.
            return new MethodSignature(name, declaringType, returnType, genericParameters, parameters);
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
