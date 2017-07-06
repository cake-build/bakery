// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Cake.Bakery.Diagnostics;
using Cake.Core.Composition;
using Cake.Core.Configuration;
using Cake.Core.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Cake.Bakery
{
    internal class BakeryModule : ICakeModule
    {
        private readonly ILoggerFactory _loggerFactory;

        public BakeryModule(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public void Register(ICakeContainerRegistrar registrar)
        {
            if (registrar == null)
            {
                throw new ArgumentNullException(nameof(registrar));
            }

            // Configuration
            registrar.RegisterType<CakeConfigurationProvider>().Singleton();

            // Logging
            registrar.RegisterInstance(_loggerFactory).As<ILoggerFactory>();
            registrar.RegisterType<CakeLog>().As<ICakeLog>().Singleton();
        }
    }
}
