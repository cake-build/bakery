using System;
using System.Collections.Generic;
using Cake.Core.Packaging;

namespace Cake.Bakery.Scripting
{
    internal sealed class PackageReferenceComparer : IEqualityComparer<PackageReference>
    {
        public bool Equals(PackageReference x, PackageReference y)
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

        public int GetHashCode(PackageReference obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            return obj.OriginalString.ToUpperInvariant().GetHashCode();
        }
    }
}