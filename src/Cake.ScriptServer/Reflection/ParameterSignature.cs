using System;
using System.Linq;
using Mono.Cecil;

namespace Cake.ScriptServer.Reflection
{
    public sealed class ParameterSignature
    {
        public string Name { get; }
        public TypeSignature ParameterType { get; }
        public bool IsParams { get; set; }
        public bool IsOutParameter { get; }
        public bool IsRefParameter { get; }

        private ParameterSignature(string name, TypeSignature parameterType, bool isParams, bool isOutParameter, bool isRefParameter)
        {
            Name = name;
            ParameterType = parameterType;
            IsParams = isParams;
            IsOutParameter = isOutParameter;
            IsRefParameter = isRefParameter;
        }

        public static ParameterSignature Create(ParameterDefinition parameter)
        {
            var isParams = parameter.CustomAttributes.Any(attribute => attribute.AttributeType.FullName == typeof(ParamArrayAttribute).FullName);

            return new ParameterSignature(
                ParameterFormatter.FormatName(parameter),
                TypeSignature.Create(parameter.ParameterType),
                isParams,
                parameter.IsOut,
                parameter.ParameterType is ByReferenceType);
        }
    }
}