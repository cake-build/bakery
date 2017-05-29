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
