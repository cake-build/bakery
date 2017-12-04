// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Cake.Scripting.Transport.Serialization
{
    internal static class Constants
    {
        public static class Protocol
        {
            public static readonly byte V1 = 0x00;
            public static readonly byte V2 = 0x01;

            public static byte Latest => V2;
        }

        public static class CakeScript
        {
            public static readonly byte TypeId = 0x00;
            public static readonly short TypeAndVersion = WithVersion(Protocol.Latest);

            public static short WithVersion(byte version) => (short)(TypeId << 8 | version);
        }

        public static class FileChange
        {
            public static readonly byte TypeId = 0x01;
            public static readonly short TypeAndVersion = WithVersion(Protocol.Latest);

            public static short WithVersion(byte version) => (short)(TypeId << 8 | version);
        }
    }
}
