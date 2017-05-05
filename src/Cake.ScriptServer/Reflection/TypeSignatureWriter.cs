using System.Collections.Generic;
using System.IO;

namespace Cake.ScriptServer.Reflection
{
    public sealed class TypeSignatureWriter
    {
        public string GetString(TypeSignature signature)
        {
            return GetString(signature, TypeRenderOption.Default);
        }

        public string GetString(TypeSignature signature, TypeRenderOption option)
        {
            var temp = new StringWriter();
            Write(temp, signature, option);
            return temp.ToString();
        }

        public void Write(TextWriter writer, TypeSignature signature)
        {
            Write(writer, signature, TypeRenderOption.Default);
        }

        public void Write(TextWriter writer, TypeSignature signature, TypeRenderOption options)
        {
            if (signature.IsGenericArgumentType)
            {
                if ((options & TypeRenderOption.Name) == TypeRenderOption.Name)
                {
                    writer.Write(signature.Name);
                }
                return;
            }

            // Write type namespace?
            if ((options & TypeRenderOption.Namespace) == TypeRenderOption.Namespace)
            {
                writer.Write(signature.Namespace.Name);
            }

            // Write type name?
            if ((options & TypeRenderOption.Name) == TypeRenderOption.Name)
            {
                if ((options & TypeRenderOption.Namespace) == TypeRenderOption.Namespace)
                {
                    writer.Write(".");
                }
                writer.Write(signature.Name);

                // Write generic arguments/parameters?
                if (options.HasFlag(TypeRenderOption.GenericParameters))
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
                                writer.Write(",");
                            }
                        }
                        writer.Write(">");
                    }
                }
            }
        }
    }
}
