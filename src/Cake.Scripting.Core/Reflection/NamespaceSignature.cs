namespace Cake.Scripting.Core.Reflection
{
    public sealed class NamespaceSignature
    {
        public string Name { get; }

        public NamespaceSignature(string name)
        {
            Name = name;
        }
    }
}