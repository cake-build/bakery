using System;
using System.IO;

namespace Cake.Scripting.Transport
{
    internal static class BinaryWriterExtensions
    {
        public static void WriteString(this BinaryWriter writer, string value)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            writer.Write(value ?? string.Empty);
        }
    }
}
