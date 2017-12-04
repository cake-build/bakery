// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Cake.Scripting.CodeGen;

namespace Cake.Bakery.Scripting
{
    internal sealed class CachingCakeAliasGenerator : ICakeAliasGenerator
    {
        private readonly IDictionary<CakeScriptAlias, string> _cache;
        private readonly ICakeAliasGenerator _aliasGenerator;

        public CachingCakeAliasGenerator(ICakeAliasGenerator aliasGenerator)
        {
            _aliasGenerator = aliasGenerator ?? throw new ArgumentNullException(nameof(aliasGenerator));
            _cache = new Dictionary<CakeScriptAlias, string>(new CakeScriptAliasComparer());
        }

        public void Generate(TextWriter writer, CakeScriptAlias alias)
        {
            if (!_cache.TryGetValue(alias, out string result))
            {
                using (var w = new StringWriter())
                {
                    _aliasGenerator.Generate(w, alias);
                    result = w.ToString();
                    _cache.Add(alias, result);
                }
            }

            writer.Write(result);
        }
    }
}