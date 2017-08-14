// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Cake.Bakery.Polyfill
{
    internal static class EnvironmentHelper
    {
        public static string GetCommandLine()
        {
            return Environment.CommandLine;
        }
    }
}
