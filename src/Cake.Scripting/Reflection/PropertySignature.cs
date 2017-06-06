// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Cecil;

namespace Cake.Scripting.Reflection
{
    public sealed class PropertySignature
    {
        public string Name { get; }

        public TypeSignature DeclaringType { get; }

        public TypeSignature PropertyType { get; }

        private PropertySignature(
            string name,
            TypeSignature declaringType,
            TypeSignature propertyType)
        {
            Name = name;
            DeclaringType = declaringType;
            PropertyType = propertyType;
        }

        public static PropertySignature Create(PropertyReference property)
        {
            // Get the property definition.
            var definition = property.Resolve();

            // Get the property Identity and name.
            var name = definition.Name;
            var declaringType = TypeSignature.Create(definition.DeclaringType);
            var propertyType = TypeSignature.Create(definition.PropertyType);

            return new PropertySignature(name, declaringType, propertyType);
        }
    }
}
