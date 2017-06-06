// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Cake.Scripting.Abstractions;
using Cake.Scripting.Abstractions.Models;

namespace Cake.Scripting.Transport.Tests.Fixtures.Tcp
{
    internal sealed class ScriptGenerationService : IScriptGenerationService
    {
        public CakeScript Generate(FileChange fileChange)
        {
            return GenerateCallback?.Invoke(fileChange);
        }

        public Func<FileChange, CakeScript> GenerateCallback { get; set; }
    }
}
