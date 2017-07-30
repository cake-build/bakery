// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Cake.Core;
using Cake.Core.IO;
using Cake.Scripting.CodeGen;

namespace Cake.Bakery.Scripting
{
    internal sealed class CachingScriptAliasFinder : IScriptAliasFinder
    {
        private readonly IDictionary<FilePath, IReadOnlyCollection<CakeScriptAlias>> _cache;
        private readonly IScriptAliasFinder _scriptAliasFinder;

        public CachingScriptAliasFinder(IScriptAliasFinder scriptAliasFinder, ICakeEnvironment environment)
        {
            _scriptAliasFinder = scriptAliasFinder ?? throw new ArgumentNullException(nameof(scriptAliasFinder));
            _cache = new Dictionary<FilePath, IReadOnlyCollection<CakeScriptAlias>>(new PathComparer(environment));
        }

        public IReadOnlyCollection<CakeScriptAlias> FindAliases(FilePath path)
        {
            if (!_cache.TryGetValue(path, out IReadOnlyCollection<CakeScriptAlias> result))
            {
                result = _scriptAliasFinder.FindAliases(path);
                _cache.Add(path, result);
            }

            return result;
        }
    }
}
