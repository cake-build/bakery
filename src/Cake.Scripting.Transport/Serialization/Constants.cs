// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Cake.Scripting.Transport.Serialization
{
    internal static class Constants
    {
        public static class CakeScript
        {
            public static readonly byte TypeId = 0x00;
            public static readonly byte Version = 0x01;
            public static readonly short TypeAndVersion = (short)(TypeId << 8 | Version);
        }

        public static class FileChange
        {
            public static readonly byte TypeId = 0x01;
            public static readonly byte Version = 0x00;
            public static readonly short TypeAndVersion = (short)(TypeId << 8 | Version);
        }
    }
}
