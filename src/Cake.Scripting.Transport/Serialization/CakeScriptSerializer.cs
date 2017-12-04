// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Cake.Scripting.Abstractions.Models;

namespace Cake.Scripting.Transport.Serialization
{
    internal static class CakeScriptSerializer
    {
        public static void Serialize(BinaryWriter writer, CakeScript script, byte version)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }
            if (script == null)
            {
                throw new ArgumentNullException(nameof(script));
            }
            if (version > Constants.Protocol.Latest)
            {
                throw new ArgumentOutOfRangeException(nameof(version));
            }

            // Type and Version
            writer.Write(Constants.CakeScript.WithVersion(version));

            // Host object
            writer.WriteString(script.Host.TypeName);
            writer.WriteString(script.Host.AssemblyPath);

            // Source
            if (version == Constants.Protocol.V2)
            {
                // V2 compress source
                var bytes = Zip(script.Source);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
            else
            {
                // V1 source as string
                writer.Write(script.Source);
            }

            // References
            writer.Write(script.References.Count);
            foreach (var reference in script.References)
            {
                writer.WriteString(reference);
            }

            // Usings
            writer.Write(script.Usings.Count);
            foreach (var @using in script.Usings)
            {
                writer.WriteString(@using);
            }
        }

        public static CakeScript Deserialize(BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            // TypeId and Version
            var typeAndVersion = reader.ReadInt16();
            if (typeAndVersion != Constants.CakeScript.TypeAndVersion)
            {
                throw new InvalidOperationException($"Unsupported type or version: {typeAndVersion}");
            }

            var cakeScript = new CakeScript();

            // Host object
            cakeScript.Host.TypeName = reader.ReadString();
            cakeScript.Host.AssemblyPath = reader.ReadString();

            // Source
            var bytesLength = reader.ReadInt32();
            var bytes = reader.ReadBytes(bytesLength);
            cakeScript.Source = Unzip(bytes);

            // References
            var referencesLength = reader.ReadInt32();
            for (var i = 0; i < referencesLength; i++)
            {
                cakeScript.References.Add(reader.ReadString());
            }

            // Usings
            var usingsLength = reader.ReadInt32();
            for (var i = 0; i < usingsLength; i++)
            {
                cakeScript.Usings.Add(reader.ReadString());
            }

            return cakeScript;
        }

        public static byte[] Zip(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    msi.CopyTo(gs);
                }

                return mso.ToArray();
            }
        }

        public static string Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    gs.CopyTo(mso);
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }
    }
}
