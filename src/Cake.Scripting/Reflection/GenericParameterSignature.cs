// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Cake.Scripting.Reflection.Emitters;
using Mono.Cecil;

namespace Cake.Scripting.Reflection
{
    public sealed class GenericParameterSignature
    {
        public string Name { get; }

        public IReadOnlyList<string> Constraints { get; }

        private GenericParameterSignature(string name, IEnumerable<string> constraints)
        {
            Name = name;
            Constraints = new List<string>(constraints);
        }

        public static GenericParameterSignature Create(GenericParameter parameter)
        {
            var name = parameter.Name;
            var constraints = new List<string>();

            if (parameter.HasNotNullableValueTypeConstraint)
            {
                constraints.Add("struct");

                if (parameter.HasConstraints)
                {
                    var emitter = new TypeEmitter();
                    var parameterConstraints = parameter.Constraints.Select(
                        constraint => emitter.GetString(TypeSignature.Create(constraint.ConstraintType)));
                    constraints.AddRange(
                        parameterConstraints.Where(constraint => !constraint.Equals("System.ValueType")));
                }
            }
            else
            {
                if (parameter.HasReferenceTypeConstraint)
                {
                    constraints.Add("class");
                }
                if (parameter.HasConstraints)
                {
                    var emitter = new TypeEmitter();
                    constraints.AddRange(
                        parameter.Constraints.Select(
                            constraint => emitter.GetString(TypeSignature.Create(constraint.ConstraintType))));
                }
                if (parameter.HasDefaultConstructorConstraint)
                {
                    constraints.Add("new()");
                }
            }

            return new GenericParameterSignature(name, constraints);
        }
    }
}
