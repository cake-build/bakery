using System;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Scripting;

namespace Cake.ScriptServer.Documentation
{
    internal class DocumentationProvider : IDocumentationProvider
    {
        private readonly IFileSystem _fileSystem;
        private XDocument _documentation;

        public DocumentationProvider(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _documentation = null;
        }

        public void SetAssembly(FilePath assemblyFilePath)
        {
            var documentationPath = assemblyFilePath.ChangeExtension("xml");

            if (!_fileSystem.Exist(documentationPath))
            {
                _documentation = null;

                return;
            }

            using (var stream = _fileSystem.GetFile(documentationPath).OpenRead())
            using (var xmlReader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit }))
            {
                _documentation = XDocument.Load(xmlReader);
            }
        }

        public string GetDocumentation(ScriptAlias alias)
        {
            var name = GetName(alias);

            var element = (from doc in _documentation.Elements("doc")
                from members in doc.Elements("members")
                from member in members.Elements("member")
                let nameAttribute = member.Attribute("name")
                where nameAttribute != null && nameAttribute.Value.Equals(name, StringComparison.Ordinal)
                select member).FirstOrDefault();

            if (element == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();

            foreach (var xmlDoc in element.Elements())
            {
                builder.AppendLine($"/// {xmlDoc.ToString().Replace("\r\n", "\r\n///")}");
            }

            return builder.ToString();
        }

        private static string GetName(ScriptAlias alias)
        {
            var builder = new StringBuilder();
            builder.Append(alias.Type == ScriptAliasType.Method ? "M:" : "P:");
            builder.Append(alias.Method.GetFullName());
            builder.Append("(");
            builder.Append(string.Join(",", alias.Method.GetParameters().Select(p => p.ParameterType.FullName)));
            builder.Append(")");

            return builder.ToString();
        }
    }
}
