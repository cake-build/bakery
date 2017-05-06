using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Cake.ScriptServer.Reflection.Emitters
{
    public sealed class ParameterEmitter
    {
        private readonly TypeEmitter _typeEmitter;
        private readonly HashSet<string> _keywords;
        private readonly HashSet<Type> _numericTypes;

        public ParameterEmitter(TypeEmitter typeEmitter)
        {
            _typeEmitter = typeEmitter;
            _keywords = new HashSet<string>(StringComparer.Ordinal)
            {
                "abstract", "as", "base", "bool", "break", "byte", "case",
                "catch", "char", "checked", "class", "const", "continue",
                "decimal", "default", "delegate", "do", "double", "else", "enum",
                "event", "explicit", "extern", "false", "finally", "fixed", "float",
                "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
                "internal", "is", "lock", "long", "namespace", "new", "null", "object",
                "operator", "out", "override", "params", "private", "protected",
                "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
                "sizeof", "stackalloc", "static", "string", "struct", "switch",
                "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
                "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
            };

            _numericTypes = new HashSet<Type>
            {
                typeof(int), typeof(double), typeof(decimal),
                typeof(long), typeof(short), typeof(sbyte),
                typeof(byte), typeof(ulong), typeof(ushort),
                typeof(uint), typeof(float)
            };
        }

        public string GetString(ParameterSignature signature)
        {
            return GetString(signature, ParameterEmitOptions.Default);
        }

        public string GetString(ParameterSignature signature, ParameterEmitOptions option)
        {
            var temp = new StringWriter();
            Write(temp, signature, option);
            return temp.ToString();
        }

        public void Write(TextWriter writer, ParameterSignature parameter, ParameterEmitOptions options)
        {
            writer.Write(string.Join(" ", GetParameterTokens(parameter, options)));
        }

        private IEnumerable<string> GetParameterTokens(ParameterSignature parameter, ParameterEmitOptions options)
        {
            if (parameter.IsOutParameter)
            {
                if (options.HasFlag(ParameterEmitOptions.Keywords))
                {
                    yield return "out";
                }
            }
            else if (parameter.IsRefParameter)
            {
                if (options.HasFlag(ParameterEmitOptions.Keywords))
                {
                    yield return "ref";
                }
            }

            if (!options.HasFlag(ParameterEmitOptions.Invocation))
            {
                if (parameter.IsParams)
                {
                    yield return "params";
                }

                yield return _typeEmitter.GetString(parameter.ParameterType);
            }

            if (options.HasFlag(ParameterEmitOptions.Name))
            {
                yield return _keywords.Contains(parameter.Name)
                    ? $"@{parameter.Name}"
                    : parameter.Name;
            }

            if (parameter.IsOptional)
            {
                if (!options.HasFlag(ParameterEmitOptions.Invocation) &&
                    options.HasFlag(ParameterEmitOptions.Optional))
                {
                    yield return "=";
                    yield return GetDefaultValue(parameter, _typeEmitter);
                }
            }
        }

        private string GetDefaultValue(ParameterSignature parameter, TypeEmitter writer)
        {
            if (!parameter.IsOptional)
            {
                return string.Empty;
            }

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
            if (parameter.ParameterType.IsEnum || _numericTypes.Contains(type))
            {
                // Nullable numerics are handled in the previous block, so just cast it and use the value.
                return string.Format(CultureInfo.InvariantCulture, "({0}){1}",
                    writer.GetString(parameter.ParameterType),
                    value);
            }

            return BuildParameterValueToken(type, value);
        }

        private string BuildParameterValueToken(Type type, object value)
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
    }
}
