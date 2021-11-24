using System.Reflection;

#pragma warning disable IDE0130
// Temporary polyfill see https://github.com/cake-build/cake/pull/3715
namespace Cake.Core.Scripting
#pragma warning restore IDE0130
{
    /// <summary>
    /// Represents a framework reference assembly resolver.
    /// </summary>
    public interface IReferenceAssemblyResolver
    {
        /// <summary>
        /// Finds framwork reference assemblies.
        /// </summary>
        /// <returns>The resolved reference assemblies.</returns>
        Assembly[] GetReferenceAssemblies();
    }
}