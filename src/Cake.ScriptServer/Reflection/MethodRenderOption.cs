using System;

namespace Cake.ScriptServer.Reflection
{
    [Flags]
    public enum MethodRenderOption
    {
        None = 1 << 0,
        Name = 1 << 1,
        Parameters = 1 << 2,
        ReturnType = 1 << 3,
        DeclaringTypeName = 1 << 4,
        TypeFullName = 1 << 5,
        ExtensionMethodInvocation = 1 << 6,
        PropertyAlias = 1 << 7,
        ParameterNames = 1 << 8,
        Default = Name | Parameters
    }
}