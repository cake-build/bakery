// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Cake.Bakery.Scripting;
using Cake.Core;
using Cake.Core.Composition;
using Cake.Scripting.CodeGen;

namespace Cake.Bakery
{
    internal sealed class CacheModule : ICakeModule
    {
        private readonly IScriptAliasFinder _aliasFinder;
        private readonly ICakeEnvironment _environment;

        public CacheModule(IScriptAliasFinder aliasFinder, ICakeEnvironment environment)
        {
            _aliasFinder = aliasFinder ?? throw new ArgumentNullException(nameof(aliasFinder));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public void Register(ICakeContainerRegistrar registrar)
        {
            if (registrar == null)
            {
                throw new ArgumentNullException(nameof(registrar));
            }

            // Scripting
            var aliasFinder = new CachingScriptAliasFinder(_aliasFinder, _environment);
            registrar.RegisterInstance(aliasFinder)
                .As<IScriptAliasFinder>();
        }
    }
}
