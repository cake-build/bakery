using System.Collections.Generic;
using System.IO;

namespace Cake.ScriptServer.Reflection
{
    public sealed class TypeSignatureRenderer
    {
        public string Render(TypeSignature signature)
        {
            return Render(signature, TypeRenderOption.Default);
        }

        public string Render(TypeSignature signature, TypeRenderOption option)
        {
            var temp = new StringWriter();
            Render(temp, signature, option);
            return temp.ToString();
        }

        public void Render(TextWriter writer, TypeSignature signature)
        {
            Render(writer, signature, TypeRenderOption.Default);
        }

        public void Render(TextWriter writer, TypeSignature signature, TypeRenderOption options)
        {
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

                if (signature.GenericArguments.Count != 0)
                {
                    // Write generic arguments.
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
                    // Write generic parameters.
                    writer.Write("<");
                    var parameterIndex = 0;
                    foreach (var parameter in signature.GenericParameters)
                    {
                        parameterIndex++;
                        Render(writer, parameter, options);
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
