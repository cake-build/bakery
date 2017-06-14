// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Cake.Bakery.Composition.Activators;
using Cake.Core.Composition;

namespace Cake.Bakery.Composition
{
    internal sealed class ContainerRegistrar : ICakeContainerRegistrar
    {
        public ContainerBuilder Builder { get; }

        public ContainerRegistrar()
            : this(null)
        {
        }

        public ContainerRegistrar(ContainerBuilder builder)
        {
            Builder = builder ?? new ContainerBuilder();
        }

        public void RegisterModule(ICakeModule module)
        {
            module.Register(this);
        }

        public ICakeRegistrationBuilder RegisterType(Type type)
        {
            var registration = new ComponentRegistration(type)
            {
                Activator = new ReflectionActivator(type)
            };
            Builder.Register(registry => registry.Register(registration));
            return new RegistrationBuilder(registration);
        }

        public ICakeRegistrationBuilder RegisterInstance<T>(T instance) where T : class
        {
            var registration = new ComponentRegistration(typeof(T))
            {
                Singleton = true,
                Activator = new InstanceActivator(instance)
            };
            Builder.Register(registry => registry.Register(registration));
            return new RegistrationBuilder(registration);
        }

        public Container Build()
        {
            return Builder.Build();
        }
    }
}
