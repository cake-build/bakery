using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cake.Core.IO;
using Cake.Scripting.Core.CodeGen;
using Cake.Scripting.Core.CodeGen.Generators;

namespace Cake.Scripting.Core.Tests.Fixtures
{
    public abstract class CakeAliasGeneratorFixture<T>
        where T : CakeAliasGenerator
    {
        private readonly Assembly _assembly;
        private readonly T _generator;
        private readonly IReadOnlyList<CakeScriptAlias> _aliases;

        protected abstract string ResourcePath { get; }

        protected abstract T CreateGenerator();

        protected CakeAliasGeneratorFixture()
        {
            // Get all aliases in the current assembly.
            _assembly = typeof(CakeAliasGeneratorFixture<T>).GetTypeInfo().Assembly;

            // Load all Cake aliases.
            // TODO: Not ideal with IO-access here, but we need to load the assembly.
            // See if we can load the information by providing the assembly directly.
            var finder = new CakeScriptAliasFinder(new FileSystem());
            _aliases = finder.FindAliases(new[] { new FilePath(_assembly.Location) });

            // Create the generator.
            // ReSharper disable once VirtualMemberCallInConstructor
            _generator = CreateGenerator();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public string GetExpectedCode(string name)
        {
            var resource = string.Concat($"{ResourcePath}.", name);
            using (var stream = _assembly.GetManifestResourceStream(resource))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("Could not load manifest resource stream.");
                }
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd().NormalizeGeneratedCode();
                }
            }
        }

        public string Generate(string name)
        {
            // Find the alias.
            var alias = _aliases.FirstOrDefault(x => x.Name == name);
            if (alias == null)
            {
                throw new InvalidOperationException($"Could not find alias '{name}'.");
            }

            using (var writer = new StringWriter())
            {
                _generator.Generate(writer, alias);

                // Return the generated code.
                return writer.ToString();
            }
        }
    }
}