namespace Cake.ScriptServer.Reflection
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