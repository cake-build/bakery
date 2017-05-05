using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Cake.ScriptServer.Reflection
{
    internal sealed class ParameterFormatter
    {
        private static readonly HashSet<string> Keywords;

        static ParameterFormatter()
        {
            // https://msdn.microsoft.com/en-us/library/x53a06bb.aspx
            // reserved keywords are a no-go, contextual keywords were added in later versions
            // and were designed to allow for legacy code that might utilize them as variables/parameters
            // to work.  i.e., where, yield, etc.
            Keywords = new HashSet<string>(StringComparer.Ordinal)
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
        }

        public static string FormatName(ParameterReference parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            return FormatName(parameter.Name);
        }

        public static string FormatName(string parameterName)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                throw new ArgumentException("Parameter name cannot be null or whitespace", nameof(parameterName));
            }
            return Keywords.Contains(parameterName) 
                ? $"@{parameterName}" : parameterName;
        }
    }
}
