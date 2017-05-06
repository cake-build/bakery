using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Cake.ScriptServer.Reflection.Emitters;

namespace Cake.ScriptServer.CodeGen
{
    public sealed class CakeMethodAliasGenerator
    {
        private readonly TypeEmitter _typeEmitter;
        private readonly ParameterEmitter _parameterEmitter;

        public CakeMethodAliasGenerator(TypeEmitter typeEmitter, ParameterEmitter parameterEmitter)
        {
            _typeEmitter = typeEmitter;
            _parameterEmitter = parameterEmitter;
        }

        public void Generate(TextWriter writer, CakeScriptAlias alias)
        {
            WriteDocs(writer, alias.Documentation);

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
                    _typeEmitter.Write(writer, alias.Method.ReturnType);
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
                var message = GetObsoleteMessage(alias);

                if (alias.Method.Obsolete.IsError)
                {
                    // Error
                    performInvocation = false;
                    writer.Write("    ");
                    writer.WriteLine($"throw new Cake.ScriptServer.CakeException(\"{message}\");");
                }
                else
                {
                    // Warning
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
            _typeEmitter.Write(writer, alias.Method.DeclaringType);
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
            var options = ParameterEmitOptions.Default;
            if (invocation)
            {
                options = options | ParameterEmitOptions.Invocation;
            }

            var parameterResult = alias.Method.Parameters
                .Select(p => string.Join(" ", _parameterEmitter.GetString(p, options)))
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

        private void WriteDocs(TextWriter writer, XElement element)
        {
            if (element != null)
            {
                var builder = new StringBuilder();
                foreach (var xmlDoc in element.Elements())
                {
                    builder.AppendLine($"/// {xmlDoc.ToString().Replace("\r\n", "\r\n///")}");
                }
                writer.Write(builder.ToString());
            }
        }

        private static string GetObsoleteMessage(CakeScriptAlias alias)
        {
            var message = string.Concat(" ", alias.Method.Obsolete.Message ?? string.Empty).TrimEnd();
            if (string.IsNullOrWhiteSpace(message))
            {
                message = string.Empty;
            }
            message = $"The alias {alias.Method.Name} has been made obsolete.{message}";
            return message;
        }
    }
}
