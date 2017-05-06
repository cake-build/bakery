using System;

namespace Cake.ScriptServer.Reflection.Emitters
{
    [Flags]
    public enum TypeEmitOptions
    {
        None = 1 << 0,
        Namespace = 1 << 1,
        Name = 1 << 2,
        GenericParameters = 1 << 3,
        Aliases = 1 << 4,
        Default = Namespace | Name | GenericParameters
    }
}