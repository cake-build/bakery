using System.IO;
using System.Text;
using System.Xml.Linq;

namespace Cake.Scripting.CodeGen.Generators
{
    public abstract class CakeAliasGenerator
    {
        public abstract void Generate(TextWriter writer, CakeScriptAlias alias);

        protected void WriteDocs(TextWriter writer, XElement element)
        {
            if (element != null)
            {
                var builder = new StringBuilder();
                foreach (var xmlDoc in element.Elements())
                {
                    builder.AppendLine($"/// {xmlDoc.ToString().Replace("\r\n", "\r\n///")}");
                }
                writer.Write(builder.ToString());
            }
        }

        protected static string GetObsoleteMessage(CakeScriptAlias alias)
        {
            var message = string.Concat(" ", alias.Obsolete.Message ?? string.Empty).TrimEnd();
            if (string.IsNullOrWhiteSpace(message))
            {
                message = string.Empty;
            }
            message = $"The alias {alias.Method.Name} has been made obsolete.{message}";
            return message;
        }
    }
}
