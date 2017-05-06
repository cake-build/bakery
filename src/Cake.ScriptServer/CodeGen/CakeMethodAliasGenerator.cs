using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cake.ScriptServer.Reflection;

namespace Cake.ScriptServer.CodeGen
{
    public sealed class CakeMethodAliasGenerator
    {
        private readonly TypeSignatureWriter _typeWriter;

        public CakeMethodAliasGenerator(TypeSignatureWriter typeWriter)
        {
            _typeWriter = typeWriter;
        }

        public void Generate(TextWriter writer, CakeScriptAlias alias)
        {
            writer.Write("public ");

            // Return type
            if (alias.Method.ReturnType != null)
            {
                if (alias.Method.ReturnType.Namespace.Name == "System" && alias.Method.ReturnType.Name == "Void")
                {
                    writer.Write("void");
                }
                else
                {
                    _typeWriter.Write(writer, alias.Method.ReturnType);

                }
                writer.Write(" ");
            }

            // Render the method signature.
            writer.Write(alias.Method.Name);

            // Generic arguments?
            if (alias.Method.GenericParameters.Count > 0)
            {
                writer.Write("<");
                writer.Write(string.Join(",", alias.Method.GenericParameters));
                writer.Write(">");
            }

            // Arguments
            writer.Write("(");
            WriteMethodParameters(writer, alias, invocation: false);
            writer.Write(")");

            writer.WriteLine();
            writer.WriteLine("{");

            // Method is obsolete?
            var performInvocation = true;
            if (alias.Method.Obsolete != null)
            {
                var message = GetObsoleteMessage(alias.Method);

                if (alias.Method.Obsolete.IsError)
                {
                    performInvocation = false;
                    writer.Write("    ");
                    writer.WriteLine($"throw new Cake.ScriptServer.CakeException(\"{message}\");");
                }
                else
                {
                    writer.Write("    ");
                    writer.WriteLine($"Context.Log.Warning(\"Warning: {message}\");");
                }
            }

            // Render the method invocation?
            if (performInvocation)
            {
                if (alias.Method.Obsolete != null)
                {
                    writer.WriteLine("#pragma warning disable 0618");
                }

                writer.Write("    ");
                WriteInvokation(writer, alias);

                if (alias.Method.Obsolete != null)
                {
                    writer.WriteLine("#pragma warning restore 0618");
                }
            }

            writer.Write("}");
        }

        private void WriteInvokation(TextWriter writer, CakeScriptAlias alias)
        {
            // Has return type?
            var hasReturnValue = !(alias.Method.ReturnType.Namespace.Name == "System" && alias.Method.ReturnType.Name == "Void");
            if (hasReturnValue)
            {
                writer.Write("return ");
            }

            // Method name.
            _typeWriter.Write(writer, alias.Method.DeclaringType);
            writer.Write(".");
            writer.Write(alias.Method.Name);

            // Generic arguments?
            if (alias.Method.GenericParameters.Count > 0)
            {
                writer.Write("<");
                writer.Write(string.Join(",", alias.Method.GenericParameters));
                writer.Write(">");
            }

            // Arguments
            writer.Write("(");
            WriteMethodParameters(writer, alias, invocation: true);
            writer.WriteLine(");");
        }

        private void WriteMethodParameters(TextWriter writer, CakeScriptAlias alias, bool invocation)
        {
            var parameterResult = alias.Method.Parameters
                .Select(p => string.Join(" ", GetParameterTokens(p, invocation)))
                .ToList();

            if (parameterResult.Count > 0)
            {
                parameterResult.RemoveAt(0);
                if (invocation)
                {
                    parameterResult.Insert(0, "Context");
                }
                writer.Write(string.Join(", ", parameterResult));
            }
        }

        private IEnumerable<string> GetParameterTokens(ParameterSignature parameter, bool invokation)
        {
            if (parameter.IsOutParameter)
            {
                yield return "out";
            }
            else if (parameter.IsRefParameter)
            {
                yield return "ref";
            }

            if (!invokation)
            {
                if (parameter.IsParams)
                {
                    yield return "params";
                }
                yield return _typeWriter.GetString(parameter.ParameterType);
            }

            yield return parameter.Name;

            if (!invokation && parameter.IsOptional)
            {
                yield return "=";
                yield return DefaultValueEmitter.GetDefaultValue(parameter, _typeWriter);
            }
        }

        private static string GetObsoleteMessage(MethodSignature method)
        {
            var message = string.Concat(" ", method.Obsolete.Message ?? string.Empty).TrimEnd();
            if (string.IsNullOrWhiteSpace(message))
            {
                message = string.Empty;
            }
            var code = $"The alias {method.Name} has been made obsolete.{message}";
            return code;
        }
    }
}
