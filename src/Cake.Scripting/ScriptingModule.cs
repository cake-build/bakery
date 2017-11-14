// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Cake.Core.Composition;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Scripting.Abstractions;
using Cake.Scripting.CodeGen;
using Cake.Scripting.CodeGen.Generators;
using Cake.Scripting.IO;

namespace Cake.Scripting
{
    public sealed class ScriptingModule : ICakeModule
    {
        private readonly IFileSystem _fileSystem;
        private readonly ICakeLog _log;

        public ScriptingModule(IFileSystem fileSystem, ICakeLog log)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public void Register(ICakeContainerRegistrar registrar)
        {
            if (registrar == null)
            {
                throw new ArgumentNullException(nameof(registrar));
            }

            // IO
            var bufferedFileSystem = new BufferedFileSystem(_fileSystem, _log);
            registrar.RegisterInstance(bufferedFileSystem)
                .As<IFileSystem>()
                .As<IBufferedFileSystem>();

            // Scripting
            registrar.RegisterType<CakeScriptAliasFinder>().As<IScriptAliasFinder>().Singleton();
            registrar.RegisterType<CakeScriptGenerator>().As<IScriptGenerationService>().Singleton();
            registrar.RegisterType<CakeAliasGenerator>().As<ICakeAliasGenerator>().Singleton();
        }
    }
}
