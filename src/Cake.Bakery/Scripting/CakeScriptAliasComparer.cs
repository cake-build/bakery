// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Cake.Scripting.CodeGen;

namespace Cake.Bakery.Scripting
{
    public class CakeScriptAliasComparer : IEqualityComparer<CakeScriptAlias>
    {
        public bool Equals(CakeScriptAlias x, CakeScriptAlias y)
        {
            if (x == null && y == null)
            {
                return true;
            }
            if (x == null || y == null)
            {
                return false;
            }
            if (x.Method == null && y.Method == null)
            {
                return true;
            }
            if (x.Method == null || y.Method == null)
            {
                return false;
            }

            return string.Equals(x.Method.CRef, y.Method.CRef, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(CakeScriptAlias obj)
        {
            if (obj?.Method?.CRef == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            return obj.Method.CRef.ToUpperInvariant().GetHashCode();
        }
    }
}