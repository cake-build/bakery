using System;
using System.IO;
using Cake.ScriptServer.Reflection.Emitters;

namespace Cake.ScriptServer.CodeGen.Generators
{
    public sealed class CakePropertyAliasGenerator : CakeAliasGenerator
    {
        private readonly TypeEmitter _typeEmitter;

        public CakePropertyAliasGenerator(TypeEmitter typeEmitter)
        {
            _typeEmitter = typeEmitter;
        }

        public override void Generate(TextWriter writer, CakeScriptAlias alias)
        {
            if (alias == null)
            {
                throw new ArgumentNullException(nameof(alias));
            }

            Generate(new IndentedTextWriter(writer), alias);
        }

        private void Generate(IndentedTextWriter writer, CakeScriptAlias alias)
        {
            // XML documentation
            WriteDocs(writer, alias.Documentation);

            var shouldThrow = alias.Obsolete?.IsError ?? false;

            if (alias.Cached && !shouldThrow)
            {
                WriteBackingField(writer, alias);
            }

            // Access modifier
            writer.Write("public ");

            // Return type
            _typeEmitter.Write(writer, alias.Method.ReturnType);
            writer.Write(" ");

            // Name
            writer.Write(alias.Method.Name);
            writer.WriteLine();

            writer.Write("{");
            using (writer.BeginScope())
            {
                writer.Write("get");
                writer.WriteLine();

                writer.Write("{");
                using (writer.BeginScope())
                {
                    // Obsolete warning?
                    if (alias.Obsolete != null)
                    {
                        var message = GetObsoleteMessage(alias);
                        if (!alias.Obsolete.IsError)
                        {
                            writer.Write($"Context.Log.Warning(\"Warning: {message}\");");
                            writer.WriteLine();
                        }
                    }

                    if (shouldThrow)
                    {
                        var message = GetObsoleteMessage(alias);
                        writer.Write($"throw new Cake.ScriptServer.CakeException(\"{message}\");");
                    }
                    else if (alias.Cached)
                    {
                        WriteCachedInvocation(writer, alias);

                        // Return
                        writer.WriteLine();
                        writer.Write("return ");
                        writer.Write("_");
                        writer.Write(alias.Method.Name);
                        if (alias.Method.ReturnType.IsValueType)
                        {
                            writer.Write(".Value");
                        }
                        writer.Write(";");
                    }
                    else
                    {
                        // Return
                        writer.Write("return ");
                        WriteInvocation(writer, alias);
                    }
                }
                writer.Write("}");
            }
            writer.Write("}");
        }

        private void WriteBackingField(TextWriter writer, CakeScriptAlias alias)
        {
            // Access modifier
            writer.Write("private ");

            // Return type
            _typeEmitter.Write(writer, alias.Method.ReturnType);
            if (alias.Method.ReturnType.IsValueType)
            {
                writer.Write("?");
            }
            writer.Write(" ");

            // Name
            writer.Write("_");
            writer.Write(alias.Method.Name);
            writer.Write(";");
            writer.WriteLine();
        }

        private void WriteCachedInvocation(IndentedTextWriter writer, CakeScriptAlias alias)
        {
            // If
            writer.Write("if (_");
            writer.Write(alias.Method.Name);
            writer.Write("==null)");
            writer.WriteLine();

            writer.Write("{");
            using (writer.BeginScope())
            {
                // Assignment
                writer.Write("_");
                writer.Write(alias.Method.Name);
                writer.Write(" = ");
                WriteInvocation(writer, alias);
            }
            writer.Write("}");
        }

        private void WriteInvocation(IndentedTextWriter writer, CakeScriptAlias alias)
        {
            // Declaring type
            _typeEmitter.Write(writer, alias.Method.DeclaringType);
            writer.Write(".");

            // Method name
            writer.Write(alias.Method.Name);

            // Generic arguments
            if (alias.Method.GenericParameters.Count > 0)
            {
                writer.Write("<");
                writer.Write(string.Join(",", alias.Method.GenericParameters));
                writer.Write(">");
            }

            // Arguments
            writer.Write("(Context);");
        }
    }
}