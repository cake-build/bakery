using System;
using System.Collections.Generic;
using Cake.Core.Scripting.Processors.Loading;

namespace Cake.Bakery.Scripting
{
    internal sealed class LoadReferenceComparer : IEqualityComparer<LoadReference>
    {
        public bool Equals(LoadReference x, LoadReference y)
        {
            if (x == null && y == null)
            {
                return true;
            }
            if (x == null || y == null)
            {
                return false;
            }

            return string.Equals(x.OriginalString, y.OriginalString, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(LoadReference obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            return obj.OriginalString.ToUpperInvariant().GetHashCode();
        }
    }
}