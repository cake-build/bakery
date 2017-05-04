using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cake.ScriptServer.Reflection
{
    public class MethodSignatureRenderer
    {
        private readonly TypeSignatureRenderer _renderer;

        public MethodSignatureRenderer(TypeSignatureRenderer renderer)
        {
            _renderer = renderer;
        }

        public void Render(
            TextWriter writer,
            MethodSignature signature,
            MethodRenderOption options)
        {
            // Declaring type (with or without namespace).
            if ((options & MethodRenderOption.DeclaringTypeName) == MethodRenderOption.DeclaringTypeName ||
                (options & MethodRenderOption.TypeFullName) == MethodRenderOption.TypeFullName)
            {
                var onlyTypeName = (options & MethodRenderOption.DeclaringTypeName) == MethodRenderOption.DeclaringTypeName;
                var typeOptions = onlyTypeName ? TypeRenderOption.Name : TypeRenderOption.Namespace;
                _renderer.Render(writer, signature.DeclaringType, typeOptions);
            }

            // Name
            if ((options & MethodRenderOption.Name) == MethodRenderOption.Name)
            {
                if ((options & MethodRenderOption.DeclaringTypeName) == MethodRenderOption.DeclaringTypeName ||
                    (options & MethodRenderOption.TypeFullName) == MethodRenderOption.TypeFullName)
                {

                    writer.Write(".");
                }
                writer.Write(signature.Name);
            }

            // TODO: Generic arguments

            // Parameters
            if((options & MethodRenderOption.Parameters) == MethodRenderOption.Parameters)
            {
                writer.Write("(");

                var includeParameterNames = (options & MethodRenderOption.ParameterNames) == MethodRenderOption.ParameterNames;

                var parameterResult = signature.Parameters
                    .Select(p => BuildParameter(p, includeParameterNames))
                    .ToList();

                if (parameterResult.Count > 0)
                {
                    // Extension method?
                    // TODO: Add extension method information to method signature and add "Invokation" to render options.
                    var isExtensionMethod = (options & MethodRenderOption.ExtensionMethodInvocation) == MethodRenderOption.ExtensionMethodInvocation;
                    if (isExtensionMethod)
                    {
                        // Remove first parameter.
                        parameterResult.RemoveAt(0);
                    }
                    writer.Write(string.Join(", ", parameterResult));
                }

                writer.Write(")");
            }
        }

        private string BuildParameter(ParameterSignature parameter, bool includeName)
        {
            var kind = parameter.IsOutParameter ? "out " : parameter.IsRefParameter ? "ref " : string.Empty;
            var type = _renderer.Render(parameter.ParameterType, TypeRenderOption.Name);
            if (includeName)
            {
                return $"{kind}{type} {parameter.Name}".Trim();
            }
            return $"{kind}{type}".Trim();
        }
    }
}
