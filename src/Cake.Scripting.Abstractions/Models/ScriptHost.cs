// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Cake.Scripting.Abstractions.Models
{
    public sealed class ScriptHost
    {
        public string TypeName { get; set; }

        public string AssemblyPath { get; set; }
    }
}