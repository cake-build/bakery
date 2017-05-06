// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cake.Core.IO;
using Cake.ScriptServer.CodeGen;
using Cake.ScriptServer.Reflection.Emitters;

namespace Cake.ScriptServer.Tests.Fixtures
{
    public sealed class MethodAliasGeneratorFixture
    {
        private readonly Assembly _assembly;
        private readonly CakeMethodAliasGenerator _generator;
        private readonly CakeScriptAliasFinder _finder;
        private readonly IReadOnlyList<CakeScriptAlias> _aliases;

        public MethodAliasGeneratorFixture()
        {
            // Get all aliases in the current assembly.
            _assembly = typeof(MethodAliasGeneratorFixture).GetTypeInfo().Assembly;
            _finder = new CakeScriptAliasFinder(new FileSystem());

            // Load all Cake aliases.
            // TODO: Not ideal with IO-access here, but we need to load the assembly.
            // See if we can load the information by providing the assembly directly.
            _aliases = _finder.FindAliases(new[] { new FilePath(_assembly.Location) });

            // Create the generator.
            var typeEmitter = new TypeEmitter();
            var parameterEmitter = new ParameterEmitter(typeEmitter);
            _generator = new CakeMethodAliasGenerator(typeEmitter, parameterEmitter);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public string GetExpectedCode(string name)
        {
            var resource = string.Concat("Cake.ScriptServer.Tests.Data.Expected.Methods.", name);
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