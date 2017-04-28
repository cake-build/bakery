using Cake.Core.IO;
using Cake.Core.Scripting;

namespace Cake.ScriptServer.Documentation
{
    internal interface IDocumentationProvider
    {
        void SetAssembly(FilePath assemblyFilePath);

        string GetDocumentation(ScriptAlias alias);
    }
}
