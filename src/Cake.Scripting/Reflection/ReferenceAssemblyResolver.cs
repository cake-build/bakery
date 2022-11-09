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
                foreach (var reference in
#if NET6_0
            Basic.Reference.Assemblies.Net60.References.All)
#else
            Basic.Reference.Assemblies.Net70.References.All)
#endif
                {
                    Assembly assembly;
                    try
                    {
                        assembly = Assembly.Load(System.IO.Path.GetFileNameWithoutExtension(reference.FilePath));
                    }
                    catch (Exception ex)
                    {
                        _log.Debug(log => log("Failed to load {0}\r\n{1}", reference.FilePath, ex));
                        continue;
                    }

                    if (assembly == null)
                    {
                        continue;
                    }

                    yield return assembly;

                    foreach (var assemblyRefName in assembly.GetReferencedAssemblies())
                    {
                        Assembly assemblyRef;
                        try
                        {
                            assemblyRef = Assembly.Load(assemblyRefName);
                        }
                        catch (Exception ex)
                        {
                            _log.Debug(log => log("Failed to load {0}\r\n{1}", reference.FilePath, ex));
                            continue;
                        }

                        if (assemblyRef == null)
                        {
                            continue;
                        }

                        yield return assemblyRef;
                    }
                }
            }

            return TryGetReferenceAssemblies().ToArray();
        }
    }
}
