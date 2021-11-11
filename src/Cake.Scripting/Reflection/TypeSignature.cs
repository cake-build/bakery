﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Cake.Scripting.Documentation;
using Mono.Cecil;

namespace Cake.Scripting.Reflection
{
    public sealed class TypeSignature
    {
        public string CRef { get; }

        public string Name { get; }

        public bool IsGenericArgumentType { get; }

        public bool IsArray { get; }

        public bool IsEnum { get; }

        public bool IsValueType { get; }

        public NamespaceSignature Namespace { get; }

        public IReadOnlyList<string> GenericArguments { get; }

        public IReadOnlyList<TypeSignature> GenericParameters { get; }

        private TypeSignature(
            string cref, string name,
            bool isArray, bool isEnum, bool isValueType,
            NamespaceSignature @namespace,
            IEnumerable<string> genericArguments,
            IEnumerable<TypeSignature> genericParameters)
        {
            CRef = cref;
            Name = name;
            IsArray = isArray;
            IsEnum = isEnum;
            IsValueType = isValueType;
            Namespace = @namespace;
            IsGenericArgumentType = string.IsNullOrWhiteSpace(Namespace.Name);
            GenericArguments = new List<string>(genericArguments);
            GenericParameters = new List<TypeSignature>(genericParameters);
        }

        public static TypeSignature Create(TypeReference type)
        {
            var isArray = false;
            if (type.IsArray)
            {
                isArray = true;
                if (type is ArrayType arrayType)
                {
                    type = arrayType.ElementType;
                }
            }

            var isEnum = false;
            var definition = type.TryResolve();
            if (definition != null)
            {
                isEnum = definition.IsEnum;
            }

            if (type.IsByReference)
            {
                if (type is ByReferenceType byReferenceType)
                {
                    type = byReferenceType.ElementType;
                }
            }

            var cref = CRefGenerator.GetTypeCRef(type);

            // Get the namespace of the type.
            var @namespace = new NamespaceSignature(type.Namespace);

            // Get the type name.
            var name = type.Name;
            var index = name.IndexOf('`');
            if (index != -1)
            {
                name = name.Substring(0, index);
            }
            if (name.EndsWith("&"))
            {
                name = name.TrimEnd('&');
            }

            // Get generic parameters and arguments.
            var genericParameters = new List<string>();
            var genericArguments = new List<TypeSignature>();

            if (type.IsGenericInstance)
            {
                // Generic arguments
                var genericInstanceType = type as GenericInstanceType;
                if (genericInstanceType != null)
                {
                    genericArguments.AddRange(genericInstanceType.GenericArguments.Select(Create));
                }
            }
            if (type.HasGenericParameters)
            {
                // Generic parameters
                genericParameters.AddRange(
                    type.GenericParameters.Select(
                        genericParameter => genericParameter.Name));
            }

            // Return the type description.
            return new TypeSignature(cref, name, isArray, isEnum, type.IsValueType, @namespace, genericParameters, genericArguments);
        }
    }
}
