// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Cake.Scripting.Reflection.Emitters
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
