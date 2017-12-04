// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Cake.Scripting.Abstractions.Models;

namespace Cake.Scripting.Transport.Serialization
{
    internal static class FileChangeSerializer
    {
        public static void Serialize(BinaryWriter writer, FileChange fileChange, byte version)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }
            if (fileChange == null)
            {
                throw new ArgumentNullException(nameof(fileChange));
            }

            // Type and Version
            writer.Write(Constants.FileChange.WithVersion(version));

            // From disk
            writer.Write(fileChange.FromDisk);

            // Buffer
            writer.WriteString(fileChange.Buffer);

            // Filename
            writer.WriteString(fileChange.FileName);

            // LineChanges
            writer.Write(fileChange.LineChanges.Count);
            foreach (var lineChange in fileChange.LineChanges)
            {
                // Protect from null
                var change = lineChange ?? new LineChange();
                writer.Write(change.StartLine);
                writer.Write(change.EndLine);
                writer.Write(change.StartColumn);
                writer.Write(change.EndColumn);
                writer.WriteString(change.NewText);
            }
        }

        public static FileChange Deserialize(BinaryReader reader, out byte version)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            // TypeId and Version
            var typeAndVersion = reader.ReadInt16();
            version = (byte)(typeAndVersion & 0x00FF);
            if (typeAndVersion != Constants.FileChange.TypeAndVersion)
            {
                var type = (byte)((typeAndVersion & 0xFF00) >> 8);
                if (type != Constants.FileChange.TypeId)
                {
                    throw new InvalidOperationException($"Unsupported type: {type}");
                }
                if (version > Constants.Protocol.Latest)
                {
                    throw new InvalidOperationException($"Unsupported version: {version}");
                }
            }

            var fileChange = new FileChange();

            // From disk
            fileChange.FromDisk = reader.ReadBoolean();

            // Buffer
            fileChange.Buffer = reader.ReadString();

            // Filename
            fileChange.FileName = reader.ReadString();

            // LineChanges
            var lineChanges = reader.ReadInt32();
            for (var i = 0; i < lineChanges; i++)
            {
                var lineChange = new LineChange();
                lineChange.StartLine = reader.ReadInt32();
                lineChange.EndLine = reader.ReadInt32();
                lineChange.StartColumn = reader.ReadInt32();
                lineChange.EndColumn = reader.ReadInt32();
                lineChange.NewText = reader.ReadString();
                fileChange.LineChanges.Add(lineChange);
            }

            return fileChange;
        }
    }
}
