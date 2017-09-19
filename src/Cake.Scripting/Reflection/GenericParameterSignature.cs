using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            }
            if (parameter.HasReferenceTypeConstraint)
            {
                constraints.Add("class");
            }
            if (parameter.HasConstraints)
            {
                var emitter = new TypeEmitter();
                constraints.AddRange(
                    parameter.Constraints.Select(
                        constraint => emitter.GetString(TypeSignature.Create(constraint))));
            }
            if (parameter.HasDefaultConstructorConstraint)
            {
                constraints.Add("new()");
            }

            return new GenericParameterSignature(name, constraints);
        }
    }
}
