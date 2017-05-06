using System;
using System.Collections.Generic;
using System.Globalization;

namespace Cake.ScriptServer.Reflection
{
    public static class DefaultValueEmitter
    {
        private static readonly HashSet<Type> NumericTypes = new HashSet<Type>
        {
            typeof(int), typeof(double), typeof(decimal),
            typeof(long), typeof(short), typeof(sbyte),
            typeof(byte), typeof(ulong), typeof(ushort),
            typeof(uint), typeof(float)
        };

        public static string GetDefaultValue(ParameterSignature parameter, TypeSignatureWriter writer)
        {
            if (parameter.DefaultValue == null)
            {
                return "null";
            }

            var type = parameter.DefaultValue.GetType();
            var value = parameter.DefaultValue;

            // Nullable?
            if (parameter.ParameterType.Namespace.Name == "System" &&
                parameter.ParameterType.Name == "Nullable")
            {
                // This is really only needing to account for char? and bool?
                // Unwrap the type and use the same logic as non-nullable by calling the BuildParameterValueToken method.
                var innerType = parameter.ParameterType.GenericParameters[0];
                return $"({writer.GetString(innerType)}){BuildParameterValueToken(type, value)}";
            }

            // Enum or numeric?
            if (parameter.ParameterType.IsEnum || IsNumeric(type))
            {
                // Nullable numerics are handled in the previous block, so just cast it and use the value.
                return string.Format(CultureInfo.InvariantCulture, "({0}){1}",
                    writer.GetString(parameter.ParameterType),
                    value);
            }

            return BuildParameterValueToken(type, value);
        }

        private static string BuildParameterValueToken(Type type, object value)
        {
            if (type == typeof(bool))
            {
                return value.ToString().ToLower();
            }
            if (type == typeof(string))
            {
                var s = ((string)value).Replace("\"", "\\\"");
                return $"\"{s}\"";
            }
            if (type == typeof(char))
            {
                return $"'{value}'";
            }
            return string.Format(CultureInfo.InvariantCulture, "{0}", value);
        }

        private static bool IsNumeric(Type myType)
        {
            return NumericTypes.Contains(myType);
        }
    }
}
