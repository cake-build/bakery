using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Cake.Core.IO;

namespace Cake.ScriptServer.Documentation
{
    public sealed class DocumentationProvider
    {
        private readonly IFileSystem _fileSystem;

        public DocumentationProvider(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public IDictionary<string, XElement> Load(FilePath path)
        {
            var document = LoadXml(path);
            if (document == null)
            {
                return new Dictionary<string, XElement>(StringComparer.OrdinalIgnoreCase);
            }
            return Build(document);
        }

        private static Dictionary<string, XElement> Build(XDocument document)
        {
            var result = new Dictionary<string, XElement>(StringComparer.Ordinal);

            var elements = from doc in document.Elements("doc")
                from members in doc.Elements("members")
                from member in members.Elements("member")
                let nameAttribute = member.Attribute("name")
                select Tuple.Create(nameAttribute.Value, member);

            foreach (var element in elements)
            {
                result.Add(element.Item1, element.Item2);
            }

            return result;
        }

        private XDocument LoadXml(FilePath path)
        {
            if (!_fileSystem.Exist(path))
            {
                return null;
            }

            using (var stream = _fileSystem.GetFile(path).OpenRead())
            using (var xmlReader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit }))
            {
                return XDocument.Load(xmlReader);
            }
        }
    }
}
