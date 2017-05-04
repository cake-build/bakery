using System;

namespace Cake.ScriptServer.Reflection
{
    [Flags]
    public enum TypeRenderOption
    {
        None = 1 << 0,
        Namespace = 1 << 1,
        Name = 1 << 2,
        Default = Namespace | Name
    }
}