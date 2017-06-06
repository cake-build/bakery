// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
