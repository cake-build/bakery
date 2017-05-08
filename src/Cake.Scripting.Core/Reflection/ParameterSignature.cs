using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;

namespace Cake.Scripting.Core.Reflection
{
    public sealed class ParameterSignature
    {
        public string Name { get; }
        public TypeSignature ParameterType { get; }
        public bool IsOptional { get; }
        public object DefaultValue { get; }
        public bool IsParams { get; set; }
        public bool IsOutParameter { get; }
        public bool IsRefParameter { get; }

        private ParameterSignature(string name, TypeSignature parameterType, 
            bool isOptional, object defaultValue,
            bool isParams, bool isOutParameter, bool isRefParameter)
        {
            Name = name;
            ParameterType = parameterType;
            IsOptional = isOptional;
            DefaultValue = defaultValue;
            IsParams = isParams;
            IsOutParameter = isOutParameter;
            IsRefParameter = isRefParameter;
        }

        public static ParameterSignature Create(ParameterDefinition parameter)
        {
            var isParams = parameter.CustomAttributes.Any(attribute => attribute.AttributeType.FullName == typeof(ParamArrayAttribute).FullName);

            object defaultValue = null;
            if (parameter.IsOptional)
            {
                if (parameter.HasConstant)
                {
                    defaultValue = parameter.Constant;
                }
                else
                {
                    // Decimal?
                    if (TryParseDecimal(parameter, out decimal value))
                    {
                        defaultValue = value;
                    }
                }
            }

            return new ParameterSignature(
                parameter.Name,
                TypeSignature.Create(parameter.ParameterType),
                parameter.IsOptional,
                defaultValue,
                isParams,
                parameter.IsOut,
                parameter.ParameterType is ByReferenceType);
        }

        private static bool TryParseDecimal(ICustomAttributeProvider parameter, out decimal value)
        {
            var name = typeof(DecimalConstantAttribute).Name;

            var attribute = parameter.CustomAttributes.FirstOrDefault(x => x.AttributeType.Name == name);
            if (attribute != null)
            {
                // Instanciate the attribute.
                var args = attribute.ConstructorArguments;
                var obj = new DecimalConstantAttribute(
                    (byte)args[0].Value, (byte)args[1].Value, (uint)args[2].Value, 
                    (uint)args[3].Value, (uint)args[4].Value);

                value = obj.Value;
                return true;
            }

            value = 0;
            return false;
        }
    }
}