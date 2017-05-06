using System;

namespace Cake.ScriptServer.Reflection.Emitters
{
    [Flags]
    public enum ParameterEmitOptions
    {
        Name = 1 << 0,
        Keywords = 1 << 1,
        Type = 1 << 2,
        Optional = 1 << 3,
        Invocation = 1 << 4,
        Default = Keywords | Type | Name | Optional
    }
}
