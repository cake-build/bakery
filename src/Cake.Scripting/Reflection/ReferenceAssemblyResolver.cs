using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cake.Core.Diagnostics;
using Cake.Core.Scripting;

namespace Cake.Scripting.Reflection
{
    public sealed class ReferenceAssemblyResolver : IReferenceAssemblyResolver
    {
        private readonly ICakeLog _log;

        public ReferenceAssemblyResolver(ICakeLog log)
        {
            _log = log;
        }

        public Assembly[] GetReferenceAssemblies()
        {
            IEnumerable<Assembly> TryGetReferenceAssemblies()
            {
                foreach (var reference in Basic.Reference.Assemblies.Net60.All)
                {
                    Assembly name;
                    try
                    {
                        name = Assembly.Load(System.IO.Path.GetFileNameWithoutExtension(reference.FilePath));
                    }
                    catch (Exception ex)
                    {
                        _log.Debug(log => log("Failed to load {0}\r\n{1}", reference.FilePath, ex));
                        continue;
                    }

                    yield return name;
                }
            }

            return TryGetReferenceAssemblies().ToArray();
        }
    }
}
