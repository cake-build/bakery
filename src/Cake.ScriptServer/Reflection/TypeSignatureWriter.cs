using System.Collections.Generic;
using System.IO;

namespace Cake.ScriptServer.Reflection
{
    public sealed class TypeSignatureWriter
    {
        public string GetString(TypeSignature signature)
        {
            return GetString(signature, TypeRenderingOptions.Default);
        }

        public string GetString(TypeSignature signature, TypeRenderingOptions option)
        {
            var temp = new StringWriter();
            Write(temp, signature, option);
            return temp.ToString();
        }

        public void Write(TextWriter writer, TypeSignature signature)
        {
            Write(writer, signature, TypeRenderingOptions.Default);
        }

        public void Write(TextWriter writer, TypeSignature signature, TypeRenderingOptions options)
        {
            if (signature.IsGenericArgumentType)
            {
                if ((options & TypeRenderingOptions.Name) == TypeRenderingOptions.Name)
                {
                    writer.Write(signature.Name);
                }
                return;
            }

            var alias = options.HasFlag(TypeRenderingOptions.Aliases) 
                ? CSharpAliasProvider.GetTypeAlias(signature)
                : null;

            // Write type namespace?
            if (options.HasFlag(TypeRenderingOptions.Namespace))
            {
                if (options.HasFlag(TypeRenderingOptions.Name))
                {
                    if (alias == null)
                    {
                        writer.Write(signature.Namespace.Name);
                        writer.Write(".");
                    }
                }
                else
                {
                    writer.Write(signature.Namespace.Name);
                }
            }

            // Write type name?
            if (options.HasFlag(TypeRenderingOptions.Name))
            {
                writer.Write(alias ?? signature.Name);

                // Write generic arguments/parameters?
                if (options.HasFlag(TypeRenderingOptions.GenericParameters))
                {
                    if (signature.GenericArguments.Count != 0)
                    {
                        // Generic arguments
                        writer.Write("<");
                        var result = new List<string>();
                        foreach (var argument in signature.GenericArguments)
                        {
                            result.Add(argument);
                        }
                        writer.Write(string.Join(",", result));
                        writer.Write(">");
                    }
                    else if (signature.GenericParameters.Count != 0)
                    {
                        // Generic parameters
                        writer.Write("<");
                        var parameterIndex = 0;
                        foreach (var parameter in signature.GenericParameters)
                        {
                            parameterIndex++;
                            Write(writer, parameter, options);
                            if (parameterIndex != signature.GenericParameters.Count)
                            {
                                writer.Write(", ");
                            }
                        }
                        writer.Write(">");
                    }
                }

                if (signature.IsArray)
                {
                    // TODO: Handle dimensions
                    writer.Write("[]");
                }
            }
        }
    }
}
