// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Cake.Scripting.Reflection.Emitters
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
