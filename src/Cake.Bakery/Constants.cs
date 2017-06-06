// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Cake.Bakery
{
    internal static class Constants
    {
        public static readonly Version LatestBreakingChange = new Version(0, 16, 0);

        public static class CommandLine
        {
            public static readonly string Port = "port";
            public static readonly string Debug = "debug";
        }
    }
}
