using System.IO;
using System.Linq;
using Cake.ScriptServer.Reflection.Emitters;

namespace Cake.ScriptServer.CodeGen.Generators
{
    public sealed class CakeMethodAliasGenerator : CakeAliasGenerator
    {
        private readonly TypeEmitter _typeEmitter;
        private readonly ParameterEmitter _parameterEmitter;

        public CakeMethodAliasGenerator(TypeEmitter typeEmitter, ParameterEmitter parameterEmitter)
        {
            _typeEmitter = typeEmitter;
            _parameterEmitter = parameterEmitter;
        }

        public override void Generate(TextWriter writer, CakeScriptAlias alias)
        {
            Generate(new IndentedTextWriter(writer), alias);
        }

        private void Generate(IndentedTextWriter writer, CakeScriptAlias alias)
        {
            // XML documentation
            WriteDocs(writer, alias.Documentation);

            // Access modifier
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

            // Block start
            writer.WriteLine();
            writer.Write("{");
            using (writer.BeginScope())
            {
                // Method is obsolete?
                var performInvocation = true;
                if (alias.Obsolete != null)
                {
                    var message = GetObsoleteMessage(alias);

                    if (alias.Obsolete.IsError)
                    {
                        // Error
                        performInvocation = false;
                        writer.Write($"throw new Cake.ScriptServer.CakeException(\"{message}\");");
                    }
                    else
                    {
                        // Warning
                        writer.Write($"Context.Log.Warning(\"Warning: {message}\");");
                    }
                }

                // Render the method invocation?
                if (performInvocation)
                {
                    if (alias.Obsolete != null)
                    {
                        writer.WriteLine();
                        writer.Write("#pragma warning disable 0618");
                        writer.WriteLine();
                    }

                    WriteInvokation(writer, alias);

                    if (alias.Obsolete != null)
                    {
                        writer.WriteLine();
                        writer.Write("#pragma warning restore 0618");
                    }
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
            writer.Write(");");
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
    }
}
