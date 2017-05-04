using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cake.ScriptServer.Reflection;
using Mono.Cecil;

namespace Cake.ScriptServer.CodeGen
{
    internal static class CakeMethodAliasGenerator
    {
        public static void Generate(TypeSignatureRenderer typeRenderer, MethodSignatureRenderer renderer, TextWriter writer, CakeScriptAlias alias)
        {
            writer.Write("public");

            if (alias.Method.ReturnType != null)
            {
                writer.Write(" ");
                typeRenderer.Render(writer, alias.Method.ReturnType,
                    TypeRenderOption.Namespace |
                    TypeRenderOption.Name);
            }

            writer.Write(" ");

            // Render the method signature
            renderer.Render(writer, alias.Method, 
                MethodRenderOption.ReturnType |
                MethodRenderOption.Name |
                MethodRenderOption.PropertyAlias |
                MethodRenderOption.Parameters |
                MethodRenderOption.ParameterNames |
                MethodRenderOption.ExtensionMethodInvocation);

            writer.WriteLine();
            writer.WriteLine("{");
            writer.WriteLine("}");
        }
    }
}
