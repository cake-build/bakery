// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Cake.Scripting.Abstractions.Models;

namespace Cake.Scripting.Transport.Serialization
{
    internal static class CakeScriptSerializer
    {
        public static void Serialize(BinaryWriter writer, CakeScript script)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }
            if (script == null)
            {
                throw new ArgumentNullException(nameof(script));
            }

            // Type and Version
            writer.Write(Constants.CakeScript.TypeAndVersion);

            // Source
            writer.WriteString(script.Source);

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
                throw new InvalidOperationException("Type and version does not match");
            }

            // Source
            var cakeScript = new CakeScript
            {
                Source = reader.ReadString()
            };

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
    }
}
