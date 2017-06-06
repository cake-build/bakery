// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Cake.Scripting.Abstractions.Models
{
    public sealed class FileChange
    {
        private Collection<LineChange> _lineChanges;

        public bool FromDisk { get; set; }

        public string Buffer { get; set; }

        public string FileName { get; set; }

        public ICollection<LineChange> LineChanges => _lineChanges ?? (_lineChanges = new Collection<LineChange>());

        public static FileChange Empty => new FileChange();
    }
}
