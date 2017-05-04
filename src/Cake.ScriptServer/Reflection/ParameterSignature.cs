using Mono.Cecil;

namespace Cake.ScriptServer.Reflection
{
    public sealed class ParameterSignature
    {
        public string Name { get; }
        public TypeSignature ParameterType { get; }
        public bool IsOutParameter { get; }
        public bool IsRefParameter { get; }

        private ParameterSignature(string name, TypeSignature parameterType, bool isOutParameter, bool isRefParameter)
        {
            Name = name;
            ParameterType = parameterType;
            IsOutParameter = isOutParameter;
            IsRefParameter = isRefParameter;
        }

        public static ParameterSignature Create(ParameterDefinition parameter)
        {
            return new ParameterSignature(
                parameter.Name,
                TypeSignature.Create(parameter.ParameterType),
                parameter.IsOut,
                parameter.ParameterType is ByReferenceType);
        }
    }
}