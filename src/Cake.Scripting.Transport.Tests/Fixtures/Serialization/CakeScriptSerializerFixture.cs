// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using Cake.Scripting.Abstractions.Models;

namespace Cake.Scripting.Transport.Tests.Fixtures.Serialization
{
    public sealed class CakeScriptSerializerFixture : SerializerFixture
    {
        private readonly Assembly _assembly;
        private readonly string _resourcePath;

        public CakeScriptSerializerFixture()
        {
            _assembly = typeof(CakeScriptSerializerFixture).GetTypeInfo().Assembly;
            _resourcePath = "Cake.Scripting.Transport.Tests.Data";
        }

        public CakeScript CreateCakeScriptFromResource(string name, int referencesLength, int usingsLength)
        {
            var resource = string.Concat($"{_resourcePath}.", name);
            using (var stream = _assembly.GetManifestResourceStream(resource))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("Could not load manifest resource stream.");
                }
                using (var reader = new StreamReader(stream))
                {
                    return CreateCakeScript(reader.ReadToEnd(), referencesLength, usingsLength);
                }
            }
        }

        public CakeScript CreateCakeScript(string source, int referencesLength, int usingsLength)
        {
            var cakeScript = new CakeScript
            {
                Source = source
            };

            for (var i = 0; i < referencesLength; i++)
            {
                cakeScript.References.Add(Guid.NewGuid().ToString());
            }
            for (var i = 0; i < usingsLength; i++)
            {
                cakeScript.Usings.Add(Guid.NewGuid().ToString());
            }

            return cakeScript;
        }
    }
}
