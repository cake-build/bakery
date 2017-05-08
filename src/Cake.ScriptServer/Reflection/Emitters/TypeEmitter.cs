using System.Collections.Generic;
using System.IO;

namespace Cake.ScriptServer.Reflection.Emitters
{
    public sealed class TypeEmitter
    {
        public string GetString(TypeSignature signature)
        {
            return GetString(signature, TypeEmitOptions.Default);
        }

        public string GetString(TypeSignature signature, TypeEmitOptions option)
        {
            var temp = new StringWriter();
            Write(temp, signature, option);
            return temp.ToString();
        }

        public void Write(TextWriter writer, TypeSignature signature)
        {
            Write(writer, signature, TypeEmitOptions.Default);
        }

        public void Write(TextWriter writer, TypeSignature signature, TypeEmitOptions options)
        {
            if (signature.IsGenericArgumentType)
            {
                if ((options & TypeEmitOptions.Name) == TypeEmitOptions.Name)
                {
                    writer.Write(signature.Name);
                }
                return;
            }

            // Write type namespace?
            if (options.HasFlag(TypeEmitOptions.Namespace))
            {
                if (options.HasFlag(TypeEmitOptions.Name))
                {
                    writer.Write(signature.Namespace.Name);
                    writer.Write(".");
                }
                else
                {
                    writer.Write(signature.Namespace.Name);
                }
            }

            // Write type name?
            if (options.HasFlag(TypeEmitOptions.Name))
            {
                writer.Write(signature.Name);

                // Write generic arguments/parameters?
                if (options.HasFlag(TypeEmitOptions.GenericParameters))
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
